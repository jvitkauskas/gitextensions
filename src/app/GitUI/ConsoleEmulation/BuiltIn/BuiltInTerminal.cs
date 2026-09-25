using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Presentation.Services;
using Iciclecreek.Terminal;
using Porta.Pty;
using XTerm.Options;

namespace GitUI.ConsoleEmulation.BuiltIn;

/// <summary>
///  The terminal control of the built-in terminal (<see cref="BuiltInTerminalEmulator"/>), shown by the terminal tab
///  and the progress dialog as a control of their window, and the process running in it.
/// </summary>
/// <remarks>
///  The process is spawned here in a pseudo console and attached to the control, rather than launched by the control:
///  the control launches its configured process again whenever it is loaded without one, which would run a finished
///  git command a second time. The emulator of the control is built once the control is loaded, so what is written to
///  it and the process wait for that (<see cref="WhenLoaded"/>).
/// </remarks>
internal sealed class BuiltInTerminal : IEmbeddedControlView, IDisposable
{
    private readonly TerminalControl _control;
    private readonly BuiltInTerminalTheme _theme;
    private readonly List<Action> _whenLoaded = [];
    private IPtyConnection? _connection;
    private bool _isDisposed;
    private bool _isThemed;

    public BuiltInTerminal(ConsoleEmulatorSettings settings)
    {
        _theme = BuiltInTerminalTheme.Get(settings.Theme);
        _control = new TerminalControl
        {
            BufferSize = 10000,
            CursorBlink = true,
            Foreground = new SolidColorBrush(global::Avalonia.Media.Color.Parse(_theme.Options.Foreground!)),
            Background = new SolidColorBrush(global::Avalonia.Media.Color.Parse(_theme.Options.Background!)),
        };

        if (settings.Font is { } font)
        {
            _control.FontFamily = new global::Avalonia.Media.FontFamily(font.FamilyName);
            _control.FontSize = font.SizeInPixels;
        }

        _control.OutputReceived += (_, e) => OutputReceived?.Invoke(this, e.Bytes);
        _control.ProcessExited += (_, e) => ProcessExited?.Invoke(this, e.ExitCodeKnown ? e.ExitCode : -1);
        _control.Loaded += OnLoaded;
    }

    public object Control => _control;

    /// <summary>Whether a process runs in the terminal.</summary>
    public bool IsProcessRunning => _connection is not null && _control.IsLive;

    /// <summary>Raised on the UI thread with each chunk of the output of the process, as the pseudo console renders it.</summary>
    public event EventHandler<ReadOnlyMemory<byte>>? OutputReceived;

    /// <summary>Raised on the UI thread with the exit code of the process (-1 if unknown), after its output.</summary>
    public event EventHandler<int>? ProcessExited;

    /// <summary>Runs <paramref name="action"/> once the control is loaded (at once if it is).</summary>
    public void WhenLoaded(Action action)
    {
        if (_control.IsLoaded)
        {
            action();
        }
        else
        {
            _whenLoaded.Add(action);
        }
    }

    /// <summary>Starts <paramref name="executable"/> with <paramref name="arguments"/> (a command line) in the terminal.</summary>
    public void Start(string executable, string arguments, string workingDirectory, IDictionary<string, string> environment)
    {
        WhenLoaded(() => ThreadHelper.FileAndForget(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Stop();

            // Windows takes the command line as it is; elsewhere the process gets the arguments one by one.
            PtyOptions options = new()
            {
                Name = executable,
                App = executable,
                CommandLine = string.IsNullOrEmpty(arguments) ? [] : OperatingSystem.IsWindows() ? [arguments] : SplitArguments(arguments),
                VerbatimCommandLine = true,
                Cwd = workingDirectory,
                Cols = Math.Max(_control.Terminal.Cols, 20),
                Rows = Math.Max(_control.Terminal.Rows, 5),
                Environment = new Dictionary<string, string>(environment)
                {
                    // As the control sets them when it launches a process itself.
                    ["TERM"] = "xterm-256color",
                    ["COLORTERM"] = "truecolor",
                },
            };

            IPtyConnection connection;
            try
            {
                connection = await PtyProvider.SpawnAsync(options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                WriteLine($"Error launching {executable}: {ex.Message}");
                ProcessExited?.Invoke(this, -1);
                return;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_isDisposed)
            {
                connection.Kill();
                connection.Dispose();
                return;
            }

            _connection = connection;
            _control.AttachConnection(connection);
        }));
    }

    /// <summary>Writes a line of text (not output of the process) to the terminal.</summary>
    public void WriteLine(string text) => WhenLoaded(() => _control.Terminal.Write(text.Replace("\n", "\r\n") + "\r\n"));

    /// <summary>Types <paramref name="text"/> into the process.</summary>
    public void SendInput(string text)
    {
        if (IsProcessRunning)
        {
            ThreadHelper.FileAndForget(() => _control.SendInputAsync(text, CancellationToken.None));
        }
    }

    /// <summary>Kills the process, if any.</summary>
    public void Stop()
    {
        if (_connection is not { } connection)
        {
            return;
        }

        _connection = null;
        _control.DetachConnection();
        try
        {
            connection.Kill();
        }
        catch (Exception ex)
        {
            // It may have exited meanwhile.
            System.Diagnostics.Trace.WriteLine(ex);
        }

        connection.Dispose();
    }

    /// <summary>Clears the screen and the scrollback of the terminal.</summary>
    public void Clear() => WhenLoaded(_control.Terminal.Reset);

    public void Focus() => WhenLoaded(() => _control.Focus());

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _whenLoaded.Clear();
        Stop();
        _control.Dispose();
    }

    private void OnLoaded(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!_isThemed)
        {
            // The palette of the emulator, which is built when the control is first loaded.
            _isThemed = true;
            _control.Terminal.Colors.ApplyTheme(_theme.Options);
            _control.Terminal.Options.MinimumContrastRatio = 3;
        }

        Action[] actions = [.. _whenLoaded];
        _whenLoaded.Clear();
        foreach (Action action in actions)
        {
            action();
        }
    }

    /// <summary>
    ///  The arguments of a command line (quoted as on Windows, as the arguments of git commands are), split as .NET splits
    ///  <c>ProcessStartInfo.Arguments</c> off Windows: white space separates the arguments outside double quotes; <c>""</c>
    ///  in double quotes is a quote; 2n backslashes before a quote are n backslashes, 2n+1 are n and a literal quote.
    /// </summary>
    internal static string[] SplitArguments(string arguments)
    {
        List<string> result = [];
        StringBuilder current = new();
        bool inQuotes = false;
        bool hasArgument = false;
        for (int i = 0; i < arguments.Length; i++)
        {
            char c = arguments[i];
            if (c == '\\')
            {
                int backslashes = 1;
                while (i + backslashes < arguments.Length && arguments[i + backslashes] == '\\')
                {
                    backslashes++;
                }

                bool beforeQuote = i + backslashes < arguments.Length && arguments[i + backslashes] == '"';
                current.Append('\\', beforeQuote ? backslashes / 2 : backslashes);
                i += backslashes - 1;
                if (beforeQuote && backslashes % 2 == 1)
                {
                    current.Append('"');
                    i++;
                }

                hasArgument = true;
            }
            else if (c == '"')
            {
                if (inQuotes && i + 1 < arguments.Length && arguments[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                hasArgument = true;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasArgument)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasArgument = false;
                }
            }
            else
            {
                current.Append(c);
                hasArgument = true;
            }
        }

        if (hasArgument)
        {
            result.Add(current.ToString());
        }

        return [.. result];
    }
}
