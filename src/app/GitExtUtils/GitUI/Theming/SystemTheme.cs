using Microsoft.Win32;

namespace GitExtUtils.GitUI.Theming;

/// <summary>The light or dark mode of the applications in the Windows settings (as the WinForms <c>Application.SystemColorMode</c>).</summary>
public static class SystemTheme
{
    /// <summary>Whether the applications are dark in the Windows settings ("Choose your app mode").</summary>
    public static bool IsDarkMode
    {
        get
        {
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
}
