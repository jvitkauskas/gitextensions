using System.Text;
using GitExtensions.Extensibility;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>Runs the background work of view models synchronously, on the test thread.</summary>
internal sealed class SynchronousBackgroundRunner : IBackgroundRunner
{
    public Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(work());
    }

    public void Post(Action action) => action();
}

/// <summary>
///  A git executable answering from a table of arguments (the same arguments can be run several times), which records the
///  commands it ran. Unknown arguments give an empty output.
/// </summary>
internal sealed class FakeGitExecutable : IExecutable
{
    private readonly Dictionary<string, (string Output, int ExitCode)> _outputs = [];

    public List<string> Commands { get; } = [];

    public string WorkingDir => ".";

    public string Command => "git.exe";

    public string PrefixArguments => "";

    public FakeGitExecutable Returns(string arguments, string output, int exitCode = 0)
    {
        _outputs[arguments] = (output, exitCode);
        return this;
    }

    public IProcess Start(
        ArgumentString arguments = default,
        bool createWindow = false,
        bool redirectInput = false,
        bool redirectOutput = false,
        Encoding? outputEncoding = null,
        bool useShellExecute = false,
        bool throwOnErrorExit = true,
        CancellationToken cancellationToken = default)
    {
        string command = arguments.ToString();
        Commands.Add(command);
        (string output, int exitCode) = _outputs.TryGetValue(command, out (string, int) known) ? known : ("", 0);
        return new FakeProcess(output, exitCode);
    }

    private sealed class FakeProcess(string output, int exitCode) : IProcess
    {
        public StreamWriter StandardInput { get; } = new(new MemoryStream());

        public StreamReader StandardOutput { get; } = new(new MemoryStream(Encoding.UTF8.GetBytes(output)));

        public string StandardError => "";

        public void Kill(bool entireProcessTree = false)
        {
        }

        public int WaitForExit() => exitCode;

        public Task<int> WaitForExitAsync() => Task.FromResult(exitCode);

        public Task<int> WaitForExitAsync(CancellationToken token) => Task.FromResult(exitCode);

        public void WaitForInputIdle()
        {
        }

        public void Dispose()
        {
            StandardInput.Dispose();
            StandardOutput.Dispose();
        }
    }
}
