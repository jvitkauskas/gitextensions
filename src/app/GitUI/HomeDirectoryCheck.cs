using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.Translations;

namespace GitUI;

/// <summary>Whether HOME has a git configuration, else the dialog to fix it (moved out of the WinForms <c>FormFixHome</c>).</summary>
internal static class HomeDirectoryCheck
{
    /// <summary>As <c>FormFixHome.CheckHomePath</c>.</summary>
    public static void CheckHomePath()
    {
        EnvironmentConfiguration.SetEnvironmentVariables();

        if (IsFixHome())
        {
            // As ShowIfUserWant.
            FixHomeStrings strings = ViewStrings.Load<FixHomeStrings>();
            if (MessageBoxes.Show(string.Format(strings.GitGlobalConfigNotFound.Text, Environment.GetEnvironmentVariable("HOME")),
                    strings.GitGlobalConfigNotFoundCaption.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
            {
                AvaloniaHosting.AvaloniaDialogs.TryShowFixHome(owner: null);
            }
        }
    }

    internal static bool IsFixHome()
    {
        try
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home) || !Directory.Exists(home))
            {
                return true;
            }

            if (HasGlobalGitConfig(home))
            {
                return false;
            }

            string?[] candidates = [
                Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH"),
                Environment.GetEnvironmentVariable("USERPROFILE"),
                Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            ];

            foreach (string? candidate in candidates)
            {
                if (HasGlobalGitConfig(candidate))
                {
                    return true;
                }
            }

            // No (better) candidates for HOME directory available
            return false;
        }
        catch
        {
            // Exception occurred while checking for home dir.
            // Could be a security issue. Just return true to let the user fix
            // this manually.
            return true;
        }
    }

    internal static bool HasGlobalGitConfig(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }

        // Check default Git config location
        string gitConfigFile = Path.Join(path, ".gitconfig");
        if (CanReadFile(gitConfigFile))
        {
            return true;
        }

        // Check presence of XDG config directory
        string xdgConfigDir = Path.Join(path, ".config");
        if (!Directory.Exists(xdgConfigDir))
        {
            return false;
        }

        // Check whether the XDG_CONFIG_HOME is compatible (unset or matching) with "path" being tested as potential HOME directory
        // and contains a git config file in the according subfolder
        // (refer to https://git-scm.com/docs/git-config#Documentation/git-config.txt-XDGCONFIGHOMEgitconfig)
        // Make issues with casing a "user problem" (case-insensitive equality would depend on file system type)
        string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(xdgConfigHome) || xdgConfigHome == xdgConfigDir)
        {
            // Consider alternative Git config file
            string xdgGitConfigFile = Path.Join(xdgConfigDir, "git", "config");
            if (CanReadFile(xdgGitConfigFile))
            {
                return true;
            }
        }

        return false;

        static bool CanReadFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
