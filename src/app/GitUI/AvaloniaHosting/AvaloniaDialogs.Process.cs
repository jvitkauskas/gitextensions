using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.ConsoleEmulation;
using GitUI.ConsoleEmulation.PlainText;
using GitUI.HelperDialogs;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Avalonia port of <see cref="FormProcess.ShowDialog(IWin32Window?, IGitUICommands, ArgumentString, string, string?, bool, out string, string?)"/>
    ///  and <see cref="FormProcess.ReadDialog"/>.
    /// </summary>
    public static bool TryShowProcess(
        IWin32Window? owner,
        IGitUICommands commands,
        ArgumentString arguments,
        string workingDirectory,
        string? input,
        bool useDialogSettings,
        string? process,
        out bool success,
        out string output)
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
            workingDirectory);

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
            positionName: nameof(FormProcess));

        success = !viewModel!.ErrorOccurred;
        output = viewModel.Output;
        return true;
    }

    /// <summary>Avalonia port of <see cref="FormStatus.ShowErrorDialog"/>.</summary>
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
            positionName: nameof(FormStatus));
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
