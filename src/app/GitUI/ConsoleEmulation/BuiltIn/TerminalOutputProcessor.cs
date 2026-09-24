using System.Text;
using System.Text.RegularExpressions;

namespace GitUI.ConsoleEmulation.BuiltIn;

/// <summary>
///  Turns the output of the pseudo console into the lines of text the progress dialog expects (as
///  <see cref="ConEmu.ConsoleCommandLineOutputProcessor"/> does for ConEmu): decoded, without the escape sequences of the
///  terminal, and each line ending with its line feed, or with a carriage return for transient progress.
/// </summary>
/// <remarks>
///  The pseudo console renders the screen: each line feed of the process arrives as CR LF, which is turned back into
///  a line feed, and the colors and cursor movements arrive as escape sequences, which are removed.
/// </remarks>
internal sealed partial class TerminalOutputProcessor(Action<string> lineReceived)
{
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly StringBuilder _pending = new();

    // CSI (e.g. colors, cursor movements), OSC (e.g. the title) terminated by BEL or ST, DCS/SOS/PM/APC strings,
    // character set selections, and the other two-character escape sequences (all printable characters but the
    // introducers of the longer ones, so that the start of an incomplete sequence does not match).
    [GeneratedRegex(@"\x1B(?:\[[0-?]*[ -/]*[@-~]|\][^\x07\x1B]*(?:\x07|\x1B\\)|[PX^_][^\x1B]*\x1B\\|[()*+][0-9A-Za-z]|[ -',-OQ-WYZ\\`-~])", RegexOptions.ExplicitCapture)]
    private static partial Regex EscapeSequenceRegex { get; }

    /// <summary>Processes a chunk of the output.</summary>
    public void Process(ReadOnlySpan<byte> chunk)
    {
        char[] chars = new char[_decoder.GetCharCount(chunk, flush: false)];
        int count = _decoder.GetChars(chunk, chars, flush: false);
        Process(new string(chars, 0, count));
    }

    /// <summary>Processes a chunk of the decoded output.</summary>
    public void Process(string text)
    {
        _pending.Append(text);
        string pending = _pending.ToString();

        // An escape sequence or a CR LF may continue in the next chunk.
        int keep = IncompleteTailLength(pending);
        string complete = pending[..^keep];
        _pending.Clear().Append(pending[^keep..]);

        string output = EscapeSequenceRegex.Replace(complete, "").Replace("\r\n", "\n");
        int start = 0;
        for (int i = 0; i < output.Length; i++)
        {
            if (output[i] is '\n' or '\r')
            {
                lineReceived(output[start..(i + 1)]);
                start = i + 1;
            }
        }

        // The rest of the line comes with the next chunk.
        _pending.Insert(0, output[start..]);
    }

    /// <summary>Raises the rest of the output, a last line without line feed (e.g. when the process exited).</summary>
    public void Flush()
    {
        string rest = EscapeSequenceRegex.Replace(_pending.ToString(), "");
        _pending.Clear();
        if (rest.Length > 0)
        {
            lineReceived(rest);
        }
    }

    /// <summary>The length of the end of <paramref name="text"/> that may be the start of a CR LF or an escape sequence.</summary>
    private static int IncompleteTailLength(string text)
    {
        if (text.EndsWith('\r'))
        {
            return 1;
        }

        int escape = text.LastIndexOf('\x1B');
        if (escape < 0)
        {
            return 0;
        }

        Match match = EscapeSequenceRegex.Match(text, escape);
        return match.Success && match.Index == escape ? 0 : text.Length - escape;
    }
}
