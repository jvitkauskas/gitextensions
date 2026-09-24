using Avalonia;
using Avalonia.Threading;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Appearance settings the host passes to the Avalonia UI.
/// </summary>
/// <param name="IsDarkTheme">Whether the Git Extensions theme is dark.</param>
/// <param name="FontFamily">The application font family (<c>AppSettings.Font</c>).</param>
/// <param name="FontSize">The application font size in device-independent pixels (1/96 inch).</param>
/// <param name="Colors">
///  Theme colors as ARGB: the system colors <see cref="ThemeColors.Control"/>, <see cref="ThemeColors.Highlight"/> etc.
///  (as the Git Extensions theme defines them) and every <c>AppColor</c> under <c>"AppColor.&lt;name&gt;"</c>.
/// </param>
/// <param name="MonospaceFontFamily">The font family of hashes (<c>AppSettings.MonospaceFont</c>).</param>
/// <param name="EditorFontFamily">The font family of the file viewer and editor (<c>AppSettings.FixedWidthFont</c>).</param>
/// <param name="EditorFontSize">The font size of the file viewer and editor, in device-independent pixels.</param>
public sealed record AvaloniaUiOptions(
    bool IsDarkTheme,
    string? FontFamily,
    double FontSize,
    IReadOnlyDictionary<string, uint>? Colors = null,
    string? MonospaceFontFamily = null,
    string? EditorFontFamily = null,
    double EditorFontSize = 0);

/// <summary>Keys of <see cref="AvaloniaUiOptions.Colors"/>.</summary>
public static class ThemeColors
{
    public const string Control = nameof(Control);
    public const string ControlText = nameof(ControlText);
    public const string Window = nameof(Window);
    public const string WindowText = nameof(WindowText);
    public const string Highlight = nameof(Highlight);
    public const string HighlightText = nameof(HighlightText);
    public const string GrayText = nameof(GrayText);
    public const string HotTrack = nameof(HotTrack);

    /// <summary>Prefix of the keys of <c>AppColor</c> values; they become brush resources with the same key.</summary>
    public const string AppColorPrefix = "AppColor.";
}

/// <summary>
///  Runs Avalonia inside the WinForms process, on the same UI thread (docs/avalonia-port/PLAN.md, "hybrid process").
/// </summary>
public static class AvaloniaUi
{
    /// <summary>Environment variable selecting which dialogs use the Avalonia UI.</summary>
    /// <remarks>
    ///  Unset or <c>all</c>: every ported dialog; <c>none</c>: none (WinForms fallback);
    ///  otherwise a comma-separated list of WinForms form names, e.g. <c>FormAbout,FormRenameBranch</c>.
    /// </remarks>
    public const string EnvironmentVariable = "GE_AVALONIA";

    /// <summary>The host's (WinForms) synchronization context, captured when Avalonia was set up.</summary>
    private static SynchronizationContext? _hostContext;

    public static bool IsInitialized => Application.Current is GitExtensionsAvaloniaApp;

    /// <summary>
    ///  Reports the exceptions that escape the jobs of the Avalonia dispatcher (the bug report of the application), instead
    ///  of ending the process. Set by the host before the first Avalonia window.
    /// </summary>
    public static Action<Exception>? UnhandledExceptionHandler { get; set; }

    /// <summary>Whether <see cref="RunMainLoop"/> is running, i.e. Avalonia runs the message loop of the process.</summary>
    public static bool IsMainLoopRunning { get; private set; }

    /// <summary>
    ///  Runs the message loop of the process with the Avalonia dispatcher until <paramref name="cancellationToken"/> is
    ///  cancelled (e.g. the last main window closed): Avalonia owns the lifetime of the application (docs/avalonia-port/PLAN.md,
    ///  phase 7, "switch the host"). WinForms dialogs shown meanwhile run their own modal loops as before.
    /// </summary>
    public static void RunMainLoop(CancellationToken cancellationToken)
    {
        IsMainLoopRunning = true;
        try
        {
            Dispatcher.UIThread.MainLoop(cancellationToken);
        }
        finally
        {
            IsMainLoopRunning = false;
        }
    }

    /// <summary>
    ///  Runs host (WinForms) code from Avalonia code, e.g. a view model callback that opens a WinForms dialog.
    /// </summary>
    /// <remarks>
    ///  Inside Avalonia's message loop <see cref="SynchronizationContext.Current"/> is Avalonia's context, so
    ///  <c>await</c> continuations in WinForms code would be queued to Avalonia's dispatcher. Avalonia does not run
    ///  those while the dispatcher job that opened a WinForms modal dialog is still on the stack, so the WinForms
    ///  dialog would wait forever (e.g. FormProcess never learning that git has exited). WinForms code therefore
    ///  runs with the WinForms context it expects.
    /// </remarks>
    public static T RunInHostContext<T>(Func<T> hostCode)
    {
        SynchronizationContext? current = SynchronizationContext.Current;
        if (_hostContext is null || ReferenceEquals(current, _hostContext))
        {
            return hostCode();
        }

        SynchronizationContext.SetSynchronizationContext(_hostContext);
        try
        {
            return hostCode();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
        }
    }

    /// <inheritdoc cref="RunInHostContext{T}(Func{T})"/>
    public static void RunInHostContext(Action hostCode)
        => RunInHostContext(() =>
        {
            hostCode();
            return true;
        });

    /// <summary>
    ///  Returns whether the Avalonia port of the dialog replacing the given WinForms form should be used.
    /// </summary>
    /// <remarks>Read on every call (it is cheap), so that tests can switch between the UIs.</remarks>
    public static bool IsEnabledFor(string winFormsFormName)
        => ParseEnabledDialogs() is not { } enabled || enabled.Contains(winFormsFormName);

    /// <summary>
    ///  Whether an unfinished port is enabled: only when named in the environment variable (e.g.
    ///  <c>GE_AVALONIA=all,FormSettings</c> for all the ports with it), not by default.
    /// </summary>
    public static bool IsExplicitlyEnabledFor(string winFormsFormName) => IsNamed(winFormsFormName);

    /// <summary>
    ///  Sets up Avalonia on the current (UI) thread on first use. Initialising lazily keeps startup unchanged
    ///  while no ported dialog has been opened.
    /// </summary>
    public static void EnsureInitialized(Func<AvaloniaUiOptions> getOptions)
    {
        if (IsInitialized)
        {
            return;
        }

        // WinForms installs its SynchronizationContext on the UI thread, and JoinableTaskFactory captured it at
        // startup. Avalonia's setup replaces it, so restore it: WinForms stays the owner of the process.
        SynchronizationContext? winFormsContext = SynchronizationContext.Current;
        _hostContext = winFormsContext;
        try
        {
            AppBuilder.Configure<GitExtensionsAvaloniaApp>()
                .UseWin32()
                .UseSkia()
                .UseHarfBuzz()
                .SetupWithoutStarting();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(winFormsContext);
        }

        ((GitExtensionsAvaloniaApp)Application.Current!).ApplyOptions(getOptions());
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            if (UnhandledExceptionHandler is { } report)
            {
                report(e.Exception);
                e.Handled = true;
            }
        };
    }

    private static HashSet<string>? ParseEnabledDialogs()
    {
        string? value = Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim();
        if (string.IsNullOrEmpty(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        HashSet<string> names = [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

        // "all,FormSettings": all the ports, and the unfinished ones named.
        return names.Contains("all", StringComparer.OrdinalIgnoreCase) ? null : names;
    }

    private static bool IsNamed(string winFormsFormName)
        => Environment.GetEnvironmentVariable(EnvironmentVariable)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(winFormsFormName) == true;

    internal static void VerifyUiThread() => Dispatcher.UIThread.VerifyAccess();

    /// <summary>
    ///  Remembers the current context as the host's, unless called from Avalonia code.
    /// </summary>
    internal static void RememberHostContext()
    {
        if (SynchronizationContext.Current is { } current and not AvaloniaSynchronizationContext)
        {
            _hostContext = current;
        }
    }
}
