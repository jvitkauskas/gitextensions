using System.Text;
using GitCommands;
using GitCommands.Logging;
using GitExtensions.Extensibility;
using GitUI;
using Microsoft.VisualStudio.Threading;

namespace GitCommandsTests;

public sealed class ExecutableTests
{
    [SetUp]
    public void SetUp()
    {
    }

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public async Task Process_shall_be_killed_on_cancellation()
    {
        TimeSpan cancelDelay = TimeSpan.FromSeconds(1);

        await TaskScheduler.Default;

        using CancellationTokenSource cts = new();

        // start a process running for seconds
        (string fileName, string arguments) = WaitCommand(cancelDelay.TotalSeconds + 60);
        IExecutable executable = new Executable(fileName);
        IProcess process = executable.Start(arguments, cancellationToken: cts.Token);
        DateTime startedAt = DateTime.Now;

        // cancel after delay
        await Task.Delay(cancelDelay);
        await cts.CancelAsync();

        // the process is killed on dispose but not before
        using (CancellationTokenSource ctsWaitExit = new())
        {
            try
            {
                ctsWaitExit.CancelAfter(cancelDelay);
                int exitCode = await process.WaitForExitAsync(ctsWaitExit.Token);
                Assert.Fail($"should not have exited, received: {exitCode}");
            }
            catch (OperationCanceledException)
            {
            }
        }

        process.Dispose();

        TimeSpan durationWaitTimeout = DateTime.Now - startedAt;
        durationWaitTimeout.Should().BeGreaterThan(1.5 * cancelDelay).And.BeLessThan(4 * cancelDelay);

        CommandLogEntry? cmd = CommandLog.Commands.LastOrDefault();
        (cmd?.Exception?.Message).Should().Be("Process killed");
    }

    [Test]
    public async Task WaitForProcessExitAsync_shall_return_latest_after_timeout()
    {
        TimeSpan halfRuntime = TimeSpan.FromSeconds(3);

        await TaskScheduler.Default;

        // start a process running for seconds
        (string fileName, string arguments) = WaitCommand((halfRuntime.TotalSeconds * 2) + 1);
        IExecutable executable = new Executable(fileName);
        using IProcess process = executable.Start(arguments);

        // wait for process exit, but cancel the wait while the process is still running
        using CancellationTokenSource cts = new();
        DateTime startedAt = DateTime.Now;
        cts.CancelAfter(halfRuntime);
        try
        {
            await process.WaitForExitAsync(cts.Token); // may or may not throw on cancel
        }
        catch (OperationCanceledException)
        {
            // ignore
        }

        TimeSpan durationWaitTimeout = DateTime.Now - startedAt;
        durationWaitTimeout.Should().BeGreaterThan(0.5 * halfRuntime).And.BeLessThan(1.5 * halfRuntime);

        // wait for process exit without cancellation
        await process.WaitForExitAsync(CancellationToken.None);

        TimeSpan durationExit = DateTime.Now - startedAt;
        durationExit.Should().BeGreaterThan(1.5 * halfRuntime).And.BeLessThan(2.5 * halfRuntime);
    }

    [Test]
    public async Task ExecuteAsync_shall_return_latest_after_timeout([Values(true, false)] bool throughShell)
    {
        const int cancelDelay = 1000;
        const int exitDelay = cancelDelay;
        const int minRuntime = cancelDelay + exitDelay;
        (string fileName, string arguments) = WaitCommand((minRuntime / 1000) + 2);
        if (throughShell)
        {
            // cmd.exe with no arguments exits immediately when stdin is not a terminal (e.g., on CI runners).
            // Run a subcommand that blocks for the required duration instead.
            (fileName, arguments) = OperatingSystem.IsWindows()
                ? ("cmd.exe", $"/c ping {arguments}")
                : ("sh", $"-c \"sleep {arguments}\"");
        }

        using CancellationTokenSource cancellationTokenSource = new();
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        IExecutable executable = new Executable(fileName);

        Exception? exception = null;
        ExecutionResult? executionResult = null;
        async Task ExecuteAsync()
        {
            try
            {
                await TaskScheduler.Default.SwitchTo(alwaysYield: true);
                executionResult = await executable.ExecuteAsync(arguments, outputEncoding: Encoding.Default, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        _ = ThreadHelper.JoinableTaskFactory.RunAsync(ExecuteAsync, JoinableTaskCreationOptions.LongRunning);

        await Task.Delay(cancelDelay);
        await cancellationTokenSource.CancelAsync();
        await Task.Delay(exitDelay);

        exception.Should().NotBeNull();
        exception.GetType().Should().Be<OperationCanceledException>();
        executionResult.Should().BeNull();
    }

    /// <summary>
    ///  A command that runs for about <paramref name="seconds"/> seconds: <c>ping.exe</c> on Windows, <c>sleep</c> elsewhere.
    /// </summary>
    private static (string FileName, string Arguments) WaitCommand(double seconds)
        => OperatingSystem.IsWindows()
            ? ("ping.exe", $"-n {seconds} 127.0.0.1")
            : ("sleep", $"{seconds}");
}
