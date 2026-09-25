using System.Diagnostics;
using Microsoft.Win32;

namespace GitExtUtils.GitUI.Theming;

/// <summary>
///  The light or dark mode of the applications in the settings of the system (as the WinForms <c>Application.SystemColorMode</c>):
///  "Choose your app mode" on Windows, the appearance on macOS, the color scheme of GNOME (or a dark GTK theme) on Linux.
/// </summary>
public static class SystemTheme
{
    private static readonly Lazy<bool> _isDarkModeOffWindows = new(ReadDarkModeOffWindows);

    /// <summary>Whether the applications are dark in the settings of the system.</summary>
    public static bool IsDarkMode
    {
        get
        {
            // Off Windows the system is asked once: the theme is read before the UI is set up (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
            if (!OperatingSystem.IsWindows())
            {
                return _isDarkModeOffWindows.Value;
            }

            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int useLightTheme && useLightTheme == 0;
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                return false;
            }
        }
    }

    /// <summary>Whether the output of the tools of the system says that the applications are dark (light if they cannot tell).</summary>
    internal static bool IsDarkModeOutput(string? macOSAppearance, string? gnomeColorScheme, string? gtkTheme)
        => string.Equals(macOSAppearance?.Trim(), "Dark", StringComparison.OrdinalIgnoreCase)
            || gnomeColorScheme?.Contains("prefer-dark", StringComparison.OrdinalIgnoreCase) == true
            || gtkTheme?.Contains("-dark", StringComparison.OrdinalIgnoreCase) == true;

    private static bool ReadDarkModeOffWindows()
        => OperatingSystem.IsMacOS()
            ? IsDarkModeOutput(Run("defaults", "read -g AppleInterfaceStyle"), null, null)
            : IsDarkModeOutput(null, Run("gsettings", "get org.gnome.desktop.interface color-scheme"), Run("gsettings", "get org.gnome.desktop.interface gtk-theme"));

    /// <summary>The output of a tool of the system, or <see langword="null"/> if it is missing, fails or takes too long.</summary>
    private static string? Run(string fileName, string arguments)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (process is null)
            {
                return null;
            }

            Task<string> output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(milliseconds: 1000))
            {
                process.Kill();
                return null;
            }

#pragma warning disable VSTHRD002 // The process has exited, so its output is complete.
            return process.ExitCode == 0 ? output.GetAwaiter().GetResult() : null;
#pragma warning restore VSTHRD002
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
    }
}
