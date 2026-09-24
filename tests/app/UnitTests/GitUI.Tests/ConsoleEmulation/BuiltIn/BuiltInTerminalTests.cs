using System.Text;
using GitUI.ConsoleEmulation.BuiltIn;

namespace GitUITests.ConsoleEmulation.BuiltIn;

[TestFixture]
public sealed class BuiltInTerminalTests
{
    [Test]
    public void Output_lines_end_with_line_feed_without_the_carriage_return_of_the_pseudo_console()
    {
        Process("first\r\nsecond\r\n").Should().Equal("first\n", "second\n");
    }

    [Test]
    public void Output_progress_ends_with_carriage_return()
    {
        Process("Receiving objects:  45%\rReceiving objects: 100%\r\n").Should().Equal("Receiving objects:  45%\r", "Receiving objects: 100%\n");
    }

    [Test]
    public void Output_escape_sequences_are_removed()
    {
        Process("\x1B[?25l\x1B[32mgreen\x1B[0m \x1B]0;title\x07text\x1B[K\r\n\x1B=\x1B(Bend\r\n")
            .Should().Equal("green text\n", "end\n");
    }

    [Test]
    public void Output_split_in_chunks_keeps_lines_escape_sequences_and_line_endings_together()
    {
        List<string> lines = [];
        TerminalOutputProcessor processor = new(lines.Add);

        foreach (string chunk in new[] { "par", "tial\r", "\nnext \x1B[3", "1mred\x1B", "[0m\r", "\n", "prompt: " })
        {
            processor.Process(chunk);
        }

        lines.Should().Equal("partial\n", "next red\n");

        processor.Flush();
        lines.Should().Equal("partial\n", "next red\n", "prompt: ");
    }

    [Test]
    public void Output_decodes_characters_split_between_chunks()
    {
        List<string> lines = [];
        TerminalOutputProcessor processor = new(lines.Add);
        byte[] bytes = Encoding.UTF8.GetBytes("čž\r\n");

        processor.Process(bytes.AsSpan(0, 1));
        processor.Process(bytes.AsSpan(1));

        lines.Should().Equal("čž\n");
    }

    [TestCase(@"""C:\Program Files\Git\bin\bash.exe"" --login -i", @"C:\Program Files\Git\bin\bash.exe", "--login -i")]
    [TestCase(@"""C:\Windows\System32\cmd.exe""", @"C:\Windows\System32\cmd.exe", "")]
    [TestCase("cmd.exe", "cmd.exe", "")]
    [TestCase("pwsh.exe -NoLogo", "pwsh.exe", "-NoLogo")]
    public void Shell_command_line_is_split_into_the_executable_and_the_arguments(string commandLine, string executable, string arguments)
    {
        BuiltInTerminalShellRunner.SplitCommandLine(commandLine).Should().Be((executable, arguments));
    }

    private static List<string> Process(string output)
    {
        List<string> lines = [];
        TerminalOutputProcessor processor = new(lines.Add);
        processor.Process(output);
        return lines;
    }
}
