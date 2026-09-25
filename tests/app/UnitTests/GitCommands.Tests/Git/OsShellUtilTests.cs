using System.Text;
using GitCommands;
using GitExtensions.Extensibility;
using NSubstitute;

namespace GitCommandsTests.Git;
public class OsShellUtilTests
{
    private IExecutable _executable = null!;
    private IProcess _process = null!;

    [SetUp]
    public void SetUp()
    {
        _process = Substitute.For<IProcess>();
        _executable = Substitute.For<IExecutable>();
        _executable.Start(
            Arg.Any<ArgumentString>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(_process);

        OsShellUtil.TestAccessor.MockExecutable = _executable;
        OsShellUtil.TestAccessor.Commands.Clear();

        // The tests are of Windows unless they say otherwise.
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.Windows;
        OsShellUtil.TestAccessor.Synchronous = true;
    }

    [TearDown]
    public void TearDown()
    {
        _process?.Dispose();
        OsShellUtil.TestAccessor.MockExecutable = null;
        OsShellUtil.TestAccessor.Platform = null;
        OsShellUtil.TestAccessor.Synchronous = false;
    }

    [Test]
    public void Open_should_start_with_shell_execute()
    {
        OsShellUtil.Open("test.txt");

        _executable.Received(1).Start(
            Arg.Is<ArgumentString>(a => a.Length == 0),
            createWindow: false,
            redirectInput: false,
            redirectOutput: false,
            outputEncoding: null,
            useShellExecute: true,
            throwOnErrorExit: false,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Open_should_fall_back_to_OpenAs_when_Start_throws()
    {
        int callCount = 0;
        _executable.Start(
            Arg.Any<ArgumentString>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("test");
                }

                return _process;
            });

        OsShellUtil.Open("test.txt");

        _executable.Received(2).Start(
            Arg.Any<ArgumentString>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void OpenAs_should_start_rundll32_with_correct_arguments()
    {
        OsShellUtil.OpenAs("test.txt");

        _executable.Received(1).Start(
            Arg.Is<ArgumentString>(a => ((string)a) == "shell32.dll,OpenAs_RunDLL test.txt"),
            createWindow: false,
            redirectInput: false,
            redirectOutput: true,
            Arg.Is<Encoding>(e => e == Encoding.UTF8),
            useShellExecute: false,
            throwOnErrorExit: true,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void SelectPathInFileExplorer_should_pass_quoted_path_with_select()
    {
        OsShellUtil.SelectPathInFileExplorer(@"C:\some\file.txt");

        _executable.Received(1).Start(
            Arg.Is<ArgumentString>(a => ((string)a) == @"/select, ""C:\some\file.txt"""),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void OpenWithFileExplorer_should_quote_arguments_by_default()
    {
        OsShellUtil.OpenWithFileExplorer(@"C:\some\folder");

        _executable.Received(1).Start(
            Arg.Is<ArgumentString>(a => ((string)a) == @"""C:\some\folder"""),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void OpenWithFileExplorer_should_not_quote_when_quote_is_false()
    {
        OsShellUtil.OpenWithFileExplorer(@"/select, ""C:\some\file.txt""", quote: false);

        _executable.Received(1).Start(
            Arg.Is<ArgumentString>(a => ((string)a) == @"/select, ""C:\some\file.txt"""),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void OpenUrlInDefaultBrowser_should_start_with_shell_execute()
    {
        OsShellUtil.OpenUrlInDefaultBrowser("https://example.com");

        _executable.Received(1).Start(
            Arg.Is<ArgumentString>(a => a.Length == 0),
            createWindow: false,
            redirectInput: false,
            redirectOutput: false,
            outputEncoding: null,
            useShellExecute: true,
            throwOnErrorExit: false,
            Arg.Any<CancellationToken>());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void OpenUrlInDefaultBrowser_should_not_start_when_url_is_null_or_whitespace(string? url)
    {
        OsShellUtil.OpenUrlInDefaultBrowser(url);

        _executable.DidNotReceive().Start(
            Arg.Any<ArgumentString>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<Encoding?>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void Off_Windows_Open_does_not_fall_back_to_OpenAs()
    {
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.FreeDesktop;
        _executable.Start(Arg.Any<ArgumentString>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Encoding?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("test"));

        OsShellUtil.Open("test.txt");

        OsShellUtil.TestAccessor.Commands.Should().Equal("test.txt");
        OsShellUtil.CanOpenAs.Should().BeFalse();
    }

    [Test]
    public void Off_Windows_OpenAs_opens_the_file_with_its_default_application()
    {
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.FreeDesktop;

        OsShellUtil.OpenAs("test.txt");

        OsShellUtil.TestAccessor.Commands.Should().Equal("test.txt");
        _executable.Received(1).Start(Arg.Any<ArgumentString>(), createWindow: false, redirectInput: false, redirectOutput: false, outputEncoding: null, useShellExecute: true, throwOnErrorExit: false, Arg.Any<CancellationToken>());
    }

    [Test]
    public void On_Linux_a_folder_opens_with_xdg_open()
    {
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.FreeDesktop;

        OsShellUtil.OpenWithFileExplorer("/home/user/my repo");

        OsShellUtil.TestAccessor.Commands.Should().Equal("xdg-open");
        _executable.Received(1).Start(Arg.Is<ArgumentString>(a => (string)a == "\"/home/user/my repo\""), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Encoding?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void On_Linux_the_file_manager_shows_the_file_selected()
    {
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.FreeDesktop;
        _process.WaitForExitAsync().Returns(0);

        OsShellUtil.SelectPathInFileExplorer("/home/user/repo/a file.txt");

        OsShellUtil.TestAccessor.Commands.Should().Equal("dbus-send");
    }

    [Test]
    public void On_Linux_the_folder_opens_when_the_file_manager_cannot_select_the_file()
    {
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.FreeDesktop;
        _process.WaitForExitAsync().Returns(1);

        OsShellUtil.SelectPathInFileExplorer("/home/user/repo/a.txt");

        OsShellUtil.TestAccessor.Commands.Should().Equal("dbus-send", "xdg-open");
        string folder = $"\"{Path.GetDirectoryName("/home/user/repo/a.txt")}\"";
        _executable.Received(1).Start(Arg.Is<ArgumentString>(a => (string)a == folder), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Encoding?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Platform(Exclude = "Win")]
    public void The_file_manager_is_asked_for_the_file_as_a_uri()
    {
        OsShellUtil.GetShowItemsArguments("/home/user/a, b.txt").Should().Be(
            "--session --print-reply --dest=org.freedesktop.FileManager1 --type=method_call /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems \"array:string:file:///home/user/a%2C%20b.txt\" string:\"\"");
    }

    [Test]
    public void On_macOS_the_Finder_reveals_the_file_and_opens_folders()
    {
        OsShellUtil.TestAccessor.Platform = OsShellUtil.ShellPlatform.MacOS;

        OsShellUtil.SelectPathInFileExplorer("/Users/user/a.txt");
        OsShellUtil.OpenWithFileExplorer("/Users/user");

        OsShellUtil.TestAccessor.Commands.Should().Equal("open", "open");
        _executable.Received(1).Start(Arg.Is<ArgumentString>(a => (string)a == "-R \"/Users/user/a.txt\""), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Encoding?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        _executable.Received(1).Start(Arg.Is<ArgumentString>(a => (string)a == "\"/Users/user\""), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<Encoding?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
