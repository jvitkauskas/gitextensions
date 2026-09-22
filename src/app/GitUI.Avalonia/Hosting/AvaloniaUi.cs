using Avalonia;
using Avalonia.Threading;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Appearance settings the host passes to the Avalonia UI.
/// </summary>
/// <param name="IsDarkTheme">Whether the Git Extensions theme is dark.</param>
/// <param name="FontFamily">The application font family (<c>AppSettings.Font</c>).</param>
/// <param name="FontSize">The application font size in device-independent pixels (1/96 inch).</param>
public sealed record AvaloniaUiOptions(bool IsDarkTheme, string? FontFamily, double FontSize);

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

        return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

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
