using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GitCommands;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using Microsoft.Web.WebView2.Core;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The web browser of the build report tab of the Avalonia main window: WebView2 (Edge) when its runtime is installed,
///  else none.
/// </summary>
internal static class BrowseWebViews
{
    /// <summary>
    ///  The user data folder of WebView2 (its cache and cookies) in the tests, instead of <see cref="DefaultUserDataFolder"/>.
    /// </summary>
    internal static string? UserDataFolderForTests { get; set; }

    /// <summary>
    ///  The user data folder of WebView2: in the local application data of the user, never next to the executable (which
    ///  may not be writable, and is not the place for a browser cache), also for the portable version.
    /// </summary>
    internal static string DefaultUserDataFolder
        => Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppSettings.ApplicationId, "WebView2");

    internal static string UserDataFolder => UserDataFolderForTests ?? DefaultUserDataFolder;

    /// <summary>The version of the installed WebView2 runtime; throws <see cref="WebView2RuntimeNotFoundException"/> if none.</summary>
    internal static string? GetAvailableBrowserVersion() => CoreWebView2Environment.GetAvailableBrowserVersionString();

    /// <summary>Whether the WebView2 runtime is installed (and its loader can be loaded).</summary>
    internal static bool IsWebView2Available(Func<string?> getAvailableBrowserVersion)
    {
        try
        {
            return !string.IsNullOrEmpty(getAvailableBrowserVersion());
        }
        catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or DllNotFoundException or BadImageFormatException or FileNotFoundException)
        {
            Trace.WriteLine($"WebView2 is not available, the build report tab has no browser: {ex.Message}");
            return false;
        }
    }

    /// <summary>WebView2 if its runtime is available, else none (the report is then opened in the default browser).</summary>
    internal static IBrowseWebView? Create(Func<string?> getAvailableBrowserVersion, Func<IBrowseWebView> createWebView2)
        => IsWebView2Available(getAvailableBrowserVersion) ? createWebView2() : null;
}

/// <summary>What <see cref="WebViewNavigationQueue"/> drives once the browser is ready.</summary>
internal interface IWebViewNavigation
{
    void Navigate(string url);

    void Clear();
}

/// <summary>
///  The navigation of a web browser that is created asynchronously: the requests made before it is ready are applied
///  once it is. Only the last URL is kept (a navigation replaces the previous one), and clearing drops it.
/// </summary>
internal sealed class WebViewNavigationQueue
{
    private IWebViewNavigation? _browser;

    /// <summary>The URL to navigate to once the browser is ready.</summary>
    public string? PendingUrl { get; private set; }

    public bool IsReady => _browser is not null;

    public void Navigate(string url)
    {
        if (_browser is { } browser)
        {
            browser.Navigate(url);
        }
        else
        {
            PendingUrl = url;
        }
    }

    public void Clear()
    {
        PendingUrl = null;
        _browser?.Clear();
    }

    /// <summary>The browser is ready: the pending navigation is applied.</summary>
    public void SetReady(IWebViewNavigation browser)
    {
        _browser = browser;
        if (PendingUrl is { } url)
        {
            PendingUrl = null;
            browser.Navigate(url);
        }
    }

    /// <summary>The browser is gone: no more navigation.</summary>
    public void Reset()
    {
        _browser = null;
        PendingUrl = null;
    }
}

/// <summary>
///  The build report in WebView2, with the Core API only (no WinForms): a Win32 child window, created when the view is
///  first attached, hosts the WebView2 controller, whose bounds follow the size of the window (<c>WM_SIZE</c>), which the
///  Avalonia <c>NativeControlHost</c> sets. The controller is created asynchronously; the navigations requested until
///  then are queued (<see cref="WebViewNavigationQueue"/>).
/// </summary>
/// <remarks>All the members are called on the UI thread, where WebView2 must be used.</remarks>
internal sealed class WebView2BrowseWebView : IBrowseWebView, IEmbeddedNativeView
{
    private const string WindowClassName = "GitExtensions.WebView2Host";
    private static readonly nint HWND_MESSAGE = -3;

    // The web views by their host window, for the window procedure.
    private static readonly Dictionary<nint, WebView2BrowseWebView> _viewsByWindow = [];
    private static bool _windowClassRegistered;

    private readonly string _userDataFolder;
    private readonly Action<string>? _openUrlInBrowser;
    private readonly WebViewNavigationQueue _navigation = new();
    private nint _window;
    private bool _attached;
    private bool _disposed;
    private CoreWebView2Controller? _controller;
    private TaskCompletionSource? _initialization;

    /// <param name="userDataFolder">The user data folder of WebView2 (<see cref="BrowseWebViews.UserDataFolder"/>).</param>
    /// <param name="openUrlInBrowser">Opens the links that would open a new window (e.g. <c>target="_blank"</c>) in the default browser.</param>
    public WebView2BrowseWebView(string userDataFolder, Action<string>? openUrlInBrowser)
    {
        _userDataFolder = userDataFolder;
        _openUrlInBrowser = openUrlInBrowser;
    }

    public IEmbeddedNativeView View => this;

    /// <summary>Completes when the creation of the controller (started by the first attachment) ended, successfully or not.</summary>
    internal Task Initialization => _initialization?.Task ?? Task.CompletedTask;

    /// <summary>Whether the controller is created.</summary>
    internal bool IsReady => _navigation.IsReady;

    /// <summary>Why the controller could not be created, if it failed.</summary>
    internal Exception? InitializationError { get; private set; }

    /// <summary>The URL of the last successful navigation.</summary>
    internal string? LastCompletedNavigationUrl { get; private set; }

    internal string? DocumentTitle => _controller?.CoreWebView2.DocumentTitle;

    /// <summary>The bounds of the controller in its host window, in pixels.</summary>
    internal System.Drawing.Rectangle? Bounds => _controller?.Bounds;

    public void Navigate(string url)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _navigation.Navigate(url);
    }

    public void Clear()
    {
        if (!_disposed)
        {
            _navigation.Clear();
        }
    }

    public nint Attach(nint parentWindow)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _attached = true;
        if (_window == 0)
        {
            _window = CreateHostWindow(parentWindow);
            _viewsByWindow[_window] = this;
            _initialization = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ThreadHelper.JoinableTaskFactory.RunAsync(InitializeAsync).FileAndForget();
        }
        else
        {
            NativeMethods.SetParent(_window, parentWindow);
            NativeMethods.ShowWindow(_window, NativeMethods.SW_SHOW);
        }

        if (_controller is { } controller)
        {
            controller.IsVisible = true;
            UpdateBounds();
            controller.NotifyParentWindowPositionChanged();
        }

        return _window;
    }

    public void Detach()
    {
        // The tab is not shown (or the window is closing): park the window so that it is not destroyed with its parent.
        _attached = false;
        if (_window != 0)
        {
            _controller?.IsVisible = false;
            NativeMethods.ShowWindow(_window, NativeMethods.SW_HIDE);
            NativeMethods.SetParent(_window, HWND_MESSAGE);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _navigation.Reset();
        if (_controller is { } controller)
        {
            _controller = null;
            controller.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
            controller.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            controller.Close();
        }

        if (_window != 0)
        {
            _viewsByWindow.Remove(_window);
            NativeMethods.DestroyWindow(_window);
            _window = 0;
        }
    }

    private async Task InitializeAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        try
        {
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(browserExecutableFolder: null, _userDataFolder);
            if (_disposed)
            {
                return;
            }

            CoreWebView2Controller controller = await environment.CreateCoreWebView2ControllerAsync(_window);
            if (_disposed)
            {
                controller.Close();
                return;
            }

            _controller = controller;
            controller.IsVisible = _attached;
            UpdateBounds();

            CoreWebView2 webView = controller.CoreWebView2;
            webView.NewWindowRequested += OnNewWindowRequested;
            webView.NavigationCompleted += OnNavigationCompleted;
            _navigation.SetReady(new Navigation(webView));
        }
        catch (Exception ex)
        {
            // As the WinForms build report tab: no propagation to the user if the report fails; the tab stays empty.
            InitializationError = ex;
            Trace.WriteLine($"The WebView2 browser of the build report tab could not be created: {ex}");
        }
        finally
        {
            _initialization!.TrySetResult();
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess && _controller is { } controller)
        {
            LastCompletedNavigationUrl = controller.CoreWebView2.Source;
        }
    }

    // The links that open a new window are opened in the default browser, not in a WebView2 popup window.
    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            _openUrlInBrowser?.Invoke(uri.AbsoluteUri);
        }
    }

    private void UpdateBounds()
    {
        if (_controller is { } controller && NativeMethods.GetClientRect(_window, out NativeMethods.RECT rect))
        {
            controller.Bounds = new System.Drawing.Rectangle(0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
    }

    private static unsafe nint CreateHostWindow(nint parentWindow)
    {
        nint instance = NativeMethods.GetModuleHandleW(null);
        if (!_windowClassRegistered)
        {
            fixed (char* className = WindowClassName)
            {
                NativeMethods.WNDCLASSEXW windowClass = new()
                {
                    cbSize = (uint)sizeof(NativeMethods.WNDCLASSEXW),
                    lpfnWndProc = (nint)(delegate* unmanaged<nint, uint, nint, nint, nint>)&WindowProc,
                    hInstance = instance,
                    lpszClassName = (nint)className,
                };

                // The class name is copied by RegisterClassEx.
                if (NativeMethods.RegisterClassExW(&windowClass) == 0 && Marshal.GetLastPInvokeError() != NativeMethods.ERROR_CLASS_ALREADY_EXISTS)
                {
                    throw new Win32Exception();
                }
            }

            _windowClassRegistered = true;
        }

        nint window = NativeMethods.CreateWindowExW(
            0,
            WindowClassName,
            null,
            NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS,
            0,
            0,
            0,
            0,
            parentWindow,
            0,
            instance,
            0);
        if (window == 0)
        {
            throw new Win32Exception();
        }

        return window;
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint window, uint message, nint wparam, nint lparam)
    {
        try
        {
            if (_viewsByWindow.TryGetValue(window, out WebView2BrowseWebView? view) && view._controller is { } controller)
            {
                switch (message)
                {
                    case NativeMethods.WM_SIZE:
                        view.UpdateBounds();
                        break;
                    case NativeMethods.WM_WINDOWPOSCHANGED:
                        // For the position of the popups of the page (e.g. of a select element).
                        controller.NotifyParentWindowPositionChanged();
                        break;
                    case NativeMethods.WM_SETFOCUS:
                        controller.MoveFocus(CoreWebView2MoveFocusReason.Programmatic);
                        break;
                }
            }

            if (message == NativeMethods.WM_ERASEBKGND)
            {
                // WebView2 paints the whole window.
                return 1;
            }
        }
        catch (Exception ex)
        {
            // No exception may cross the native frames.
            Trace.WriteLine($"The WebView2 host window failed to process the message {message}: {ex}");
        }

        return NativeMethods.DefWindowProcW(window, message, wparam, lparam);
    }

    private sealed class Navigation(CoreWebView2 webView) : IWebViewNavigation
    {
        public void Navigate(string url)
        {
            try
            {
                webView.Navigate(url);
            }
            catch (ArgumentException ex)
            {
                // As the WinForms build report tab: no propagation to the user if the report fails (e.g. an invalid URL).
                Trace.WriteLine($"The build report could not be shown: {ex.Message}");
            }
        }

        public void Clear()
        {
            webView.Stop();
            webView.Navigate("about:blank");
        }
    }

    private static class NativeMethods
    {
        public const int ERROR_CLASS_ALREADY_EXISTS = 1410;
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_SETFOCUS = 0x0007;
        public const uint WM_ERASEBKGND = 0x0014;
        public const uint WM_WINDOWPOSCHANGED = 0x0047;
        public const uint WS_CHILD = 0x40000000;
        public const uint WS_VISIBLE = 0x10000000;
        public const uint WS_CLIPCHILDREN = 0x02000000;
        public const uint WS_CLIPSIBLINGS = 0x04000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEXW
        {
            public uint cbSize;
            public uint style;
            public nint lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public nint lpszMenuName;
            public nint lpszClassName;
            public nint hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern nint GetModuleHandleW(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern unsafe ushort RegisterClassExW(WNDCLASSEXW* lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint CreateWindowExW(
            uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight,
            nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

        [DllImport("user32.dll")]
        public static extern nint DefWindowProcW(nint hWnd, uint msg, nint wparam, nint lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(nint hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetClientRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern nint SetParent(nint hWndChild, nint hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(nint hWnd, int nCmdShow);
    }
}
