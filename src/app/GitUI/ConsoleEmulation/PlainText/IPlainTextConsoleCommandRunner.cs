namespace GitUI.ConsoleEmulation.PlainText;

internal interface IPlainTextConsoleCommandRunner : IConsoleCommandRunner
{
    /// <summary>
    ///  Occurs when text is to be shown in the console (the command line, the output written by
    ///  <see cref="WriteOutputText"/>), which the progress dialog shows.
    /// </summary>
    event EventHandler<string>? OutputTextWritten;

    void WriteOutputText(string text);
}
