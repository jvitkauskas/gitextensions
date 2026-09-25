using GitCommands;

namespace GitCommandsTests.Git;

/// <summary>The program of SSH_ASKPASS off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 4).</summary>
public sealed class AskPassScriptTests
{
    [Test]
    public void The_script_runs_the_askpass_mode_of_Git_Extensions_with_the_prompt()
    {
        AskPassScript.GetContent("/opt/git extensions/GitExtensions")
            .Should().Be("#!/bin/sh\nexec '/opt/git extensions/GitExtensions' askpass \"$@\"\n");
        AskPassScript.GetContent("/opt/it's/GitExtensions")
            .Should().Be("#!/bin/sh\nexec '/opt/it'\\''s/GitExtensions' askpass \"$@\"\n");
    }

    [Test]
    public void The_script_is_written_once_and_executable()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"AskPassScriptTests-{Guid.NewGuid():N}");
        try
        {
            string path = AskPassScript.Write(folder, "/usr/bin/GitExtensions");

            File.ReadAllText(path).Should().Be(AskPassScript.GetContent("/usr/bin/GitExtensions"));
            if (!OperatingSystem.IsWindows())
            {
                File.GetUnixFileMode(path).Should().HaveFlag(UnixFileMode.UserExecute);
            }

            DateTime written = File.GetLastWriteTimeUtc(path);
            AskPassScript.Write(folder, "/usr/bin/GitExtensions").Should().Be(path);
            File.GetLastWriteTimeUtc(path).Should().Be(written, "an unchanged script is not written again");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
