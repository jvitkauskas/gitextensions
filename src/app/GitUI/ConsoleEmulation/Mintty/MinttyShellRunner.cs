using GitCommands;
using GitUI.Presentation.Services;

namespace GitUI.ConsoleEmulation.Mintty;

internal sealed class MinttyShellRunner : IConsoleShellRunner
{
    private readonly string _minttyPath;
    private readonly string _bashPath;
    private readonly ConsoleEmulatorSettings _settings;
    private readonly NativeHostWindow _window = new();
    private readonly MinttyControl _control;

    internal MinttyShellRunner(string minttyPath, string bashPath, ConsoleEmulatorSettings settings)
    {
        _minttyPath = minttyPath;
        _bashPath = bashPath;
        _settings = settings;
        _control = new MinttyControl(_window);
    }

    public bool IsShellRunning => _control.IsShellRunning;

    /// <summary>The window in which mintty runs (in place of the WinForms panel).</summary>
    public IEmbeddedNativeView View => _window;

    public void StartShell(string workDir)
    {
        _control.StartInteractiveShell(_minttyPath, _bashPath, workDir, _settings);
    }

    public void ChangeWorkingDirectory(string path)
    {
        string changeDirCommand = $"cd \"{path.ToMountPath("/")}\"";
        _control.SendConsoleInput($"\x1\xb{changeDirCommand}\n");
    }

    public void FocusTerminal()
    {
        _control.Focus();
    }

    public void Dispose()
    {
        _control.Dispose();
        _window.Dispose();
    }
}
