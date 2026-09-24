using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.ConsoleEmulation;
using GitUI.ConsoleEmulation.PlainText;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Avalonia port of <c>FormProcess.ShowDialog</c> and <c>FormProcess.ReadDialog</c> (<see cref="ProcessDialogs"/>).
    /// </summary>
    /// <param name="environment">The environment variables of the process (<c>FormProcess.ProcessEnvVariables</c>).</param>
    public static bool TryShowProcess(
        IWin32Window? owner,
        IGitUICommands commands,
        ArgumentString arguments,
        string workingDirectory,
        string? input,
        bool useDialogSettings,
        string? process,
        out bool success,
        out string output,
        Dictionary<string, string>? environment = null)
    {
        success = false;
        output = "";

        // FormProcess does not support process input either; keep that case on the WinForms path.
        if (!string.IsNullOrEmpty(input))
        {
            return false;
        }

        (string resolvedProcess, string resolvedArguments) = ResolveProcess(process, arguments, workingDirectory);

        using ConsoleProcess console = new(
            commands.GetRequiredService<IConsoleEmulatorsRegistry>().CreateCommandController(),
            commands,
            resolvedProcess,
            resolvedArguments,
            workingDirectory,
            environment);

        ProcessViewModel? viewModel = null;
        ShowDialog(
            () =>
            {
                ProcessWindow window = new();
                viewModel = new ProcessViewModel(
                    ViewStrings.Load<ProcessStrings>(),
                    PathUtil.GetDisplayPath(workingDirectory),
                    console,
                    new ProcessDialogHost(commands, console),
                    new MessageBoxService(window),
                    useDialogSettings,
                    console.PostToUiThread);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "FormProcess");

        success = !viewModel!.ErrorOccurred;
        output = viewModel.Output;
        return true;
    }

    /// <summary>Avalonia port of <c>FormStatus.ShowErrorDialog</c>.</summary>
    public static bool TryShowErrorDialog(IWin32Window owner, IGitUICommands commands, string text, string[]? output)
    {
        using ConsoleProcess console = new(new PlainTextConsoleCommandRunner(), commands, process: "", arguments: "", workingDirectory: "");

        ShowDialog(
            () =>
            {
                ProcessWindow window = new();
                ProcessViewModel viewModel = new(
                    ViewStrings.Load<ProcessStrings>(),
                    displayPath: "",
                    console,
                    new ProcessDialogHost(commands, console),
                    new MessageBoxService(window),
                    useDialogSettings: true,
                    console.PostToUiThread);
                viewModel.ShowAsErrorDialog(text, output ?? []);
                window.DataContext = viewModel;
                return window;
            },
            owner,
            positionName: "FormStatus");
        return true;
    }

    /// <summary>
    ///  The process to run and its arguments, as <c>FormProcess</c>'s constructor determines them
    ///  (git by default, through <c>wsl.exe</c> for repositories in a WSL distribution).
    /// </summary>
    private static (string Process, string Arguments) ResolveProcess(string? process, ArgumentString arguments, string workingDirectory)
    {
        if (process is null)
        {
            string wslDistro = AppSettings.WslGitEnabled ? PathUtil.GetWslDistro(workingDirectory) : "";
            if (!string.IsNullOrEmpty(wslDistro))
            {
                // Keep in sync with FormProcess: --cd passes the working directory, --exec bypasses the distro's login shell.
                return (AppSettings.WslCommand, $"-d {wslDistro} --cd {workingDirectory.RemoveTrailingPathSeparator().Quote()} --exec {AppSettings.WslGitCommand} {arguments}");
            }
        }

        return (process ?? AppSettings.GitCommand, arguments);
    }
}
