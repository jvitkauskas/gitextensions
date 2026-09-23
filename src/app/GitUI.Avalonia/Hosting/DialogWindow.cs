using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using GitUI.Presentation;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Base class of Avalonia dialogs; the counterpart of <c>GitExtensionsForm</c> / <c>GitExtensionsDialog</c>.
/// </summary>
/// <remarks>
///  <list type="bullet">
///   <item>Closes itself when its <see cref="DialogViewModel"/> requests it, and on Escape (like <c>GitExtensionsForm</c>).</item>
///   <item>Restores and saves its size and position under <see cref="PositionName"/> (like <c>WindowPositionManager</c>).</item>
///   <item>Executes configured <see cref="Hotkeys"/> through <see cref="DialogViewModel.ExecuteHotkeyCommand"/>.</item>
///  </list>
/// </remarks>
public class DialogWindow : Window
{
    private static readonly Lazy<WindowIcon> _applicationIcon = new(
        () => new WindowIcon(AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/git-extensions-logo-256px.png"))));

    private DialogViewModel? _viewModel;
    private PixelRect? _lastNormalBounds;

    /// <summary>Size of the window frame (title bar, borders): saved positions are outer bounds, Width/Height are client size.</summary>
    private Size _frameThickness;

    public DialogWindow()
    {
        CanMinimize = false;
        Icon = _applicationIcon.Value;

        if (OperatingSystem.IsWindows())
        {
            Win32Properties.AddWndProcHookCallback(this, WndProcHook);
        }
    }

    /// <summary>
    ///  <see langword="true"/> if the dialog was accepted (the equivalent of <c>DialogResult.OK</c>).
    /// </summary>
    public bool DialogResult { get; private set; }

    /// <summary>
    ///  The native window handle, available once the window has been created; used to parent WinForms dialogs.
    /// </summary>
    public nint NativeHandle => TryGetPlatformHandle()?.Handle ?? 0;

    /// <summary>
    ///  The name under which the window position is persisted (the name of the WinForms form it replaces),
    ///  or <see langword="null"/> to not persist it.
    /// </summary>
    public string? PositionName { get; set; }

    /// <summary>The store for <see cref="PositionName"/>; set by the host.</summary>
    public IWindowPositionStore? PositionStore { get; set; }

    /// <summary>
    ///  Set by the host when the dialog is centered over an owner: only the size is restored then, not the location.
    /// </summary>
    public bool IsCenteredOnOwner { get; set; }

    /// <summary>The configured hotkeys of this dialog.</summary>
    public IReadOnlyList<HotkeyBinding> Hotkeys { get; set; } = [];

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as DialogViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnOpened(EventArgs e)
    {
        // Before the base raises Opened, where the host centers the dialog with its final size.
        _frameThickness = FrameSize is { } frameSize ? new Size(Math.Max(0, frameSize.Width - ClientSize.Width), Math.Max(0, frameSize.Height - ClientSize.Height)) : default;
        RestorePosition();
        base.OnOpened(e);
        RememberNormalBounds();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_viewModel?.CanClose() == false)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);

        if (!e.Cancel)
        {
            SavePosition();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ClientSizeProperty || change.Property == WindowStateProperty)
        {
            RememberNormalBounds();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            CloseDialog(accepted: false);
            return;
        }

        if (Hotkeys.Count > 0 && _viewModel is not null)
        {
            int keyData = KeyMapping.ToKeyData(e.Key, e.KeyModifiers);
            HotkeyBinding? hotkey = keyData == 0 ? null : Hotkeys.FirstOrDefault(h => h.KeyData == keyData);
            if (hotkey is not null && _viewModel.ExecuteHotkeyCommand(hotkey.CommandCode))
            {
                e.Handled = true;
            }
        }
    }

    /// <summary>
    ///  A dialog whose height follows its content can only be resized horizontally (as the WinForms forms that fix
    ///  their height through <c>MinimumSize</c> / <c>MaximumSize</c>).
    /// </summary>
    private nint WndProcHook(nint handle, uint msg, nint wordParameter, nint longParameter, ref bool handled)
    {
        if (!CanResize || SizeToContent != SizeToContent.Height)
        {
            return 0;
        }

        switch (msg)
        {
            case NativeMethods.WM_NCHITTEST:
                return HitTestHorizontalBordersOnly(handle, msg, wordParameter, longParameter, ref handled);
            case NativeMethods.WM_WINDOWPOSCHANGING:
                KeepContentHeight(handle, longParameter);
                return 0;
            default:
                return 0;
        }
    }

    /// <summary>The top and bottom borders do not resize the dialog.</summary>
    private static nint HitTestHorizontalBordersOnly(nint handle, uint msg, nint wordParameter, nint longParameter, ref bool handled)
    {
        nint hit = NativeMethods.DefWindowProc(handle, msg, wordParameter, longParameter);
        nint horizontalHit = hit switch
        {
            NativeMethods.HTTOP or NativeMethods.HTBOTTOM => NativeMethods.HTBORDER,
            NativeMethods.HTTOPLEFT or NativeMethods.HTBOTTOMLEFT => NativeMethods.HTLEFT,
            NativeMethods.HTTOPRIGHT or NativeMethods.HTBOTTOMRIGHT => NativeMethods.HTRIGHT,
            _ => hit
        };

        handled = horizontalHit != hit;
        return horizontalHit;
    }

    /// <summary>
    ///  Other resizes that are not the layout sizing the dialog to its content (e.g. Win+Shift+Up, or sizing
    ///  with the keyboard) keep its height, and then its vertical position.
    /// </summary>
    /// <remarks>
    ///  Avalonia would otherwise drop <see cref="SizeToContent.Height"/> on such a resize.
    /// </remarks>
    private void KeepContentHeight(nint handle, nint windowPosPointer)
    {
        NativeMethods.WINDOWPOS windowPos = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(windowPosPointer);
        if ((windowPos.Flags & NativeMethods.SWP_NOSIZE) != 0
            || DesiredSize.Height <= 0
            || !NativeMethods.GetWindowRect(handle, out NativeMethods.RECT windowRect)
            || !NativeMethods.GetClientRect(handle, out NativeMethods.RECT clientRect))
        {
            return;
        }

        int frameHeight = (windowRect.Bottom - windowRect.Top) - (clientRect.Bottom - clientRect.Top);
        int contentHeight = (int)Math.Round(DesiredSize.Height * RenderScaling) + frameHeight;

        // Allow for rounding: the layout resizes the window to its content, anything else is vetoed.
        if (Math.Abs(windowPos.Cy - contentHeight) <= 2)
        {
            return;
        }

        windowPos.Cy = windowRect.Bottom - windowRect.Top;
        if ((windowPos.Flags & NativeMethods.SWP_NOMOVE) == 0)
        {
            windowPos.Y = windowRect.Top;
        }

        Marshal.StructureToPtr(windowPos, windowPosPointer, fDeleteOld: false);
    }

    private void OnCloseRequested(object? sender, bool accepted) => CloseDialog(accepted);

    private void CloseDialog(bool accepted)
    {
        DialogResult = accepted;
        Close();
    }

    private int CurrentDpi => (int)Math.Round(DesktopScaling * 96);

    private void RestorePosition()
    {
        if (PositionName is null || PositionStore?.Load(PositionName) is not { } placement || placement.Dpi <= 0)
        {
            return;
        }

        // Dialogs restore only the dimensions that do not follow their content: a size saved by the WinForms form
        // (or for other content) would crop a layout sized to content.
        // Stored in pixels at the DPI of the time; device-independent units are pixels * 96 / DPI.
        if (CanResize && !SizeToContent.HasFlag(SizeToContent.Width))
        {
            Width = Math.Min(MaxWidth, Math.Max(MinWidth, (placement.Width * 96.0 / placement.Dpi) - _frameThickness.Width));
        }

        if (CanResize && !SizeToContent.HasFlag(SizeToContent.Height))
        {
            Height = Math.Min(MaxHeight, Math.Max(MinHeight, (placement.Height * 96.0 / placement.Dpi) - _frameThickness.Height));
        }

        if (!IsCenteredOnOwner)
        {
            double scale = (double)CurrentDpi / placement.Dpi;
            PixelRect bounds = new((int)(placement.X * scale), (int)(placement.Y * scale), (int)(placement.Width * scale), (int)(placement.Height * scale));
            if (IsVisibleOnAScreen(bounds))
            {
                Position = bounds.Position;
            }
        }

        if (placement.IsMaximized && CanResize)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void RememberNormalBounds()
    {
        if (WindowState == WindowState.Normal)
        {
            Size outerSize = new(ClientSize.Width + _frameThickness.Width, ClientSize.Height + _frameThickness.Height);
            _lastNormalBounds = new PixelRect(Position, PixelSize.FromSize(outerSize, DesktopScaling));
        }
    }

    private void SavePosition()
    {
        if (PositionName is null || PositionStore is null)
        {
            return;
        }

        RememberNormalBounds();
        if (_lastNormalBounds is not { } bounds)
        {
            return;
        }

        PositionStore.Save(
            PositionName,
            new WindowPlacement(bounds.X, bounds.Y, bounds.Width, bounds.Height, CurrentDpi, WindowState == WindowState.Maximized));
    }

    /// <summary>
    ///  As <c>WindowPositionManager.FitWindowOnScreen</c>: a window counts as visible if at least 10% of a screen
    ///  (or the whole window, if smaller) is covered from its top-left corner.
    /// </summary>
    private bool IsVisibleOnAScreen(PixelRect window)
    {
        foreach (Screen screen in Screens.All)
        {
            PixelRect area = screen.WorkingArea;
            int requiredWidth = Math.Min(area.Width / 10, window.Width);
            int requiredHeight = Math.Min(area.Height / 10, window.Height);
            if (area.Contains(window.Position) && area.Contains(new PixelPoint(window.X + requiredWidth, window.Y + requiredHeight)))
            {
                return true;
            }
        }

        return false;
    }
}
