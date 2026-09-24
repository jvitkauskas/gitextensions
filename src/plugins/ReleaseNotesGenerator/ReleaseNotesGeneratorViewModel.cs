using System.Net;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Presentation;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.ReleaseNotesGenerator;

/// <summary>Strings of the Avalonia port of <see cref="ReleaseNotesGeneratorForm"/>; ids match the form.</summary>
public sealed class ReleaseNotesGeneratorStrings : ViewStrings
{
    public ReleaseNotesGeneratorStrings()
        : base("ReleaseNotesGeneratorForm")
    {
        Title = Add("$this", "Text", "Release Notes Generator");
        From = Add("label2", "Text", "Commit expression \"From\" (excluding):");
        To = Add("label1", "Text", "Commit expression \"To\" (including):");
        ExpressionsHint = Add("label7", "Text", "(Commit expressions can be commit hashes,\nbranch names, tag names)");
        GitLogArguments = Add("label3", "Text", "git log arguments:");
        ArgumentsHint = Add("label4", "Text", "{0} = from; {1} = to");
        Generate = Add("buttonGenerate", "Text", "Generate");
        Result = Add("groupBox1", "Text", "Result");
        RevisionsCount = Add("label6", "Text", "Revisions count = ");
        NotAvailable = Add("labelRevCount", "Text", "n/a");
        Sorting = Add("label5", "Text", "Sorting: Most recent revisions are listed on top.");
        CopyToClipboard = Add("groupBoxCopy", "Text", "Copy to clipboard");
        CopyOriginalOutput = Add("buttonCopyOrigOutput", "Text", "original output");
        AsIs = Add("label8", "Text", "as is");
        CopyAsTextTableTab = Add("buttonCopyAsTextTableTab", "Text", "as text table (tabs)");
        SeparateWithTabs = Add("label9", "Text", "separate columns with tabs");
        CopyAsTextTableSpace = Add("buttonCopyAsTextTableSpace", "Text", "as text table (spaces)");
        SeparateWithSpaces = Add("label10", "Text", "separate columns with spaces");
        CopyAsHtml = Add("buttonCopyAsHtml", "Text", "as HTML table");
        HtmlHint = Add("label11", "Text", "Clipboard will contain HTML code (plain text) and HTML format\nwhich can be pasted to programs like MS Word or LibreOffice Writer.");
        CommitLogFrom = Add("_commitLogFrom", "Text", "Commit log from '{0}' to '{1}' ({2}):");
        FromCommitNotSpecified = Add("_fromCommitNotSpecified", "Text", "'From' commit must be specified");
        ToCommitNotSpecified = Add("_toCommitNotSpecified", "Text", "'To' commit must be specified");
        InvalidInput = Add("_caption", "Text", "Invalid input");
    }

    public TranslatedText Title { get; }

    public TranslatedText From { get; }

    public TranslatedText To { get; }

    public TranslatedText ExpressionsHint { get; }

    public TranslatedText GitLogArguments { get; }

    public TranslatedText ArgumentsHint { get; }

    public TranslatedText Generate { get; }

    public TranslatedText Result { get; }

    public TranslatedText RevisionsCount { get; }

    public TranslatedText NotAvailable { get; }

    public TranslatedText Sorting { get; }

    public TranslatedText CopyToClipboard { get; }

    public TranslatedText CopyOriginalOutput { get; }

    public TranslatedText AsIs { get; }

    public TranslatedText CopyAsTextTableTab { get; }

    public TranslatedText SeparateWithTabs { get; }

    public TranslatedText CopyAsTextTableSpace { get; }

    public TranslatedText SeparateWithSpaces { get; }

    public TranslatedText CopyAsHtml { get; }

    public TranslatedText HtmlHint { get; }

    public TranslatedText CommitLogFrom { get; }

    public TranslatedText FromCommitNotSpecified { get; }

    public TranslatedText ToCommitNotSpecified { get; }

    public TranslatedText InvalidInput { get; }
}

/// <summary>The clipboard of the Avalonia port of <see cref="ReleaseNotesGeneratorForm"/>.</summary>
public interface IReleaseNotesClipboard
{
    void CopyText(string text);

    /// <summary>Copies an HTML fragment as HTML and as text (<c>HtmlFragment.CopyToClipboard</c>).</summary>
    void CopyHtml(string htmlFragment);
}

/// <summary>The inputs of the release notes dialog that a failed validation focuses.</summary>
public enum ReleaseNotesInput
{
    From,
    To,
}

/// <summary>View model of the Avalonia port of <see cref="ReleaseNotesGeneratorForm"/>.</summary>
public sealed partial class ReleaseNotesGeneratorViewModel : DialogViewModel
{
    private const string MostRecentHint = "most recent changes are listed on top";

    private readonly IExecutable _gitExecutable;
    private readonly IReleaseNotesClipboard _clipboard;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IGitLogLineParser _gitLogLineParser = new GitLogLineParser();
    private IReadOnlyList<LogLine> _lastGeneratedLogLines = [];

    public ReleaseNotesGeneratorViewModel(ReleaseNotesGeneratorStrings strings, IExecutable gitExecutable, IReleaseNotesClipboard clipboard, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _gitExecutable = gitExecutable;
        _clipboard = clipboard;
        _messageBoxes = messageBoxes;
        RevisionCount = strings.NotAvailable.Text;
    }

    /// <summary>Raised when a validation fails, to focus the input.</summary>
    public event EventHandler<ReleaseNotesInput>? FocusRequested;

    public ReleaseNotesGeneratorStrings Strings { get; }

    [ObservableProperty]
    public partial string RevisionFrom { get; set; } = "";

    [ObservableProperty]
    public partial string RevisionTo { get; set; } = "HEAD";

    [ObservableProperty]
    public partial string GitLogArguments { get; set; } = "--pretty=\"format:%h@%s%b\" --abbrev-commit {0}..{1}";

    [ObservableProperty]
    public partial string Result { get; private set; } = "";

    [ObservableProperty]
    public partial string RevisionCount { get; private set; }

    /// <summary>Whether the copy buttons are enabled: log lines were generated (as <c>textBoxResult_TextChanged</c>).</summary>
    [ObservableProperty]
    public partial bool CanCopy { get; private set; }

    /// <summary>As <c>buttonGenerate_Click</c>.</summary>
    [RelayCommand]
    private void Generate()
    {
        Result = string.Empty;

        if (string.IsNullOrWhiteSpace(RevisionFrom))
        {
            _messageBoxes.ShowError(Strings.FromCommitNotSpecified.Text, Strings.InvalidInput.Text);
            FocusRequested?.Invoke(this, ReleaseNotesInput.From);
            return;
        }

        if (string.IsNullOrWhiteSpace(RevisionTo))
        {
            _messageBoxes.ShowError(Strings.ToCommitNotSpecified.Text, Strings.InvalidInput.Text);
            FocusRequested?.Invoke(this, ReleaseNotesInput.To);
            return;
        }

        GitArgumentBuilder args = new("log")
        {
            string.Format(GitLogArguments, RevisionFrom, RevisionTo)
        };

        string result = _gitExecutable.GetOutput(args);

        // As the lines of the WinForms text box.
        string[] lines = result.Split([Environment.NewLine], StringSplitOptions.None).SelectMany(l => l.Split('\n')).ToArray();
        Result = string.Join(Environment.NewLine, lines);
        try
        {
            _lastGeneratedLogLines = [.. _gitLogLineParser.Parse(lines)];
            RevisionCount = _lastGeneratedLogLines.Count.ToString();
        }
        catch
        {
            RevisionCount = "n/a";
        }

        CanCopy = _lastGeneratedLogLines.Count > 0;
    }

    /// <summary>As <c>buttonCopyOrigOutput_Click</c>.</summary>
    [RelayCommand]
    private void CopyOriginalOutput() => _clipboard.CopyText(Result);

    /// <summary>As <c>buttonCopyAsPlainText_Click</c>.</summary>
    [RelayCommand]
    private void CopyAsTextTableTab() => _clipboard.CopyText(CreateTextTable(_lastGeneratedLogLines, suppressEmptyLines: true, separateColumnWithTabInsteadOfSpaces: true));

    /// <summary>As <c>buttonCopyAsTextTableSpace_Click</c>.</summary>
    [RelayCommand]
    private void CopyAsTextTableSpace() => _clipboard.CopyText(CreateTextTable(_lastGeneratedLogLines, suppressEmptyLines: true, separateColumnWithTabInsteadOfSpaces: false));

    /// <summary>As <c>buttonCopyAsHtml_Click</c>.</summary>
    [RelayCommand]
    private void CopyAsHtml()
    {
        string headerHtml = string.Format("<p>Commit log from '{0}' to '{1}' ({2}):</p>", RevisionFrom, RevisionTo, MostRecentHint);
        _clipboard.CopyHtml(headerHtml + CreateHtmlTable(_lastGeneratedLogLines));
    }

    /// <summary>As <c>CreateTextTable</c>.</summary>
    private string CreateTextTable(IEnumerable<LogLine> logLines, bool suppressEmptyLines, bool separateColumnWithTabInsteadOfSpaces)
    {
        string headerText = string.Format(Strings.CommitLogFrom.Text, RevisionFrom, RevisionTo, MostRecentHint);

        string colSeparatorFirstLine = separateColumnWithTabInsteadOfSpaces ? "\t" : " ";
        string colSeparatorRestLines = separateColumnWithTabInsteadOfSpaces ? "\t" : "        ";

        StringBuilder stringBuilder = new();

        foreach (LogLine logLine in logLines)
        {
            string message = string.Join(
                Environment.NewLine + colSeparatorRestLines,
                logLine.MessageLines.Where(a => !suppressEmptyLines || !string.IsNullOrWhiteSpace(a)));
            stringBuilder.AppendFormat("{0}{1}{2}{3}", logLine.Commit, colSeparatorFirstLine, message, Environment.NewLine);
        }

        return headerText + Environment.NewLine + stringBuilder;
    }

    /// <summary>As <c>CreateHtmlTable</c>.</summary>
    private static string CreateHtmlTable(IEnumerable<LogLine> logLines)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append("<table>\r\n");
        foreach (LogLine logLine in logLines)
        {
            string message = string.Join("<br/>", logLine.MessageLines.Select(a => WebUtility.HtmlEncode(a)));
            stringBuilder.AppendFormat("<tr>\r\n  <td>{0}</td>\r\n  <td>{1}</td>\r\n</tr>\r\n", logLine.Commit, message);
        }

        stringBuilder.Append("</table>");
        return stringBuilder.ToString();
    }
}
