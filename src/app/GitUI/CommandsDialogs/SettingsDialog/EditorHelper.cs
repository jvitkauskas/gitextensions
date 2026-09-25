using GitCommands;

namespace GitUI.CommandsDialogs.SettingsDialog;

public static class EditorHelper
{
    public static string[] GetEditors()
        => OperatingSystem.IsWindows()
            ? [
                AppSettings.FileEditorCommand,
                "vi",
                "notepad",
                GetNotepadPlusPlus(),
                GetSublimeText(),
                GetVsCode(),
                GetZed(),
            ]
            : [AppSettings.FileEditorCommand, .. GetUnixEditors(OperatingSystem.IsMacOS(), name => PathUtil.TryFindFullPath(name, out _))];

    /// <summary>
    ///  The editors of Linux and macOS whose command is on the PATH (docs/avalonia-port/CROSS-PLATFORM.md, phase 3), with
    ///  the options that make them wait for the file to be closed, as git needs. vi and nano run in the built-in terminal.
    /// </summary>
    internal static IEnumerable<string> GetUnixEditors(bool isMacOS, Func<string, bool> isOnPath)
    {
        yield return "vi";

        (string Command, string Arguments)[] editors =
        [
            ("nano", ""),
            ("code", "--new-window --wait"),
            ("subl", "--new-window --wait"),
            ("zed", "--wait"),
            ("gedit", "--standalone"),
            ("kate", "--block"),
        ];
        foreach ((string command, string arguments) in editors)
        {
            if (isOnPath(command))
            {
                yield return arguments.Length == 0 ? command : $"{command} {arguments}";
            }
        }

        if (isMacOS)
        {
            // TextEdit, in a new instance that git waits for.
            yield return "open -W -n -e";
        }
    }

    private static string GetNotepadPlusPlus()
        => GetEditorCommandLine("notepad++.exe", "-multiInst -nosession", "notepad++");

    private static string GetVsCode()
        => GetEditorCommandLine("code.exe", "--new-window --wait", "Microsoft VS Code");

    private static string GetZed()
        => GetEditorCommandLine("zed.exe", "--wait", "Zed.dev", @"Zed\bin");

    // http://stackoverflow.com/questions/8951275/git-config-core-editor-how-to-make-sublime-text-the-default-editor-for-git-on
    private static string GetSublimeText()
        => GetEditorCommandLine("sublime_text.exe", "--new-window --wait", "Sublime Text");

    private static string GetEditorCommandLine(string executableName, string commandLineParameter, params string[] installFolders)
    {
        string exec = executableName.FindInFolders(installFolders);

        if (string.IsNullOrEmpty(exec))
        {
            // hoping the tool is available in the PATH
            exec = Path.GetExtension(executableName) == ".exe" ? Path.GetFileNameWithoutExtension(executableName) : executableName;
        }
        else
        {
            exec = $"\"{exec}\"";
        }

        return $"{exec} {commandLineParameter}";
    }
}
