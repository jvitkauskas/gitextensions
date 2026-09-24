using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.BugReporter;

/// <summary>Strings of the bug report dialog; ids match <c>BugReportForm</c> (and its <c>ExceptionDetails</c> control).</summary>
public sealed class BugReportStrings : ViewStrings
{
    public BugReportStrings()
        : base("BugReportForm")
    {
        Title = Add("_title", "Text", "Error Report");
        SubmitGitHubMessage = Add("_submitGitHubMessage", "Text", """
            Give as much as information as possible please to help the developers solve this issue. Otherwise, your issue ticket may be closed without any follow-up from the developers.

            Because of this, make sure to fill in all the fields in the report template please.

            Send report?
            """.ReplaceLineEndings("\r\n"));
        ToolTipCopy = Add("_toolTipCopy", "Text", "Copy the issue details into clipboard");
        ToolTipSendQuit = Add("_toolTipSendQuit", "Text", "Report the issue to GitHub and quit application.\r\nA valid GitHub account is required");
        ToolTipQuit = Add("_toolTipQuit", "Text", "Quit application without reporting the issue");
        NoReproStepsSupplied = Add("_noReproStepsSuppliedErrorMessage", "Text", "Please provide as much as information as possible to help the developers solve this issue.");
        GeneralTab = Add("generalTabPage", "Text", "General");
        ExceptionTab = Add("exceptionTabPage", "Text", "Exception");
        Warning = Add("warningLabel", "Text", "The application has crashed and it will now be terminated. If you click Quit, the application will close immediately. If you click Send and Quit, the application will close and a bug report will be sent.");
        ExceptionType = Add("exceptionTypeLabel", "Text", "Exception Type:");
        TargetSite = Add("targetSiteLabel", "Text", "Target Site:");
        Application = Add("applicationLabel", "Text", "Application:");
        DateTime = Add("dateTimeLabel", "Text", "Date/Time:");
        ErrorDescription = Add("errorDescriptionLabel", "Text", "Please add a brief description (in English) of how we can reproduce the error:");
        SendAndQuit = Add("sendAndQuitButton", "Text", "&Send and Quit");
        Quit = Add("quitButton", "Text", "&Quit");
        Copy = Add("btnCopy", "Text", "&Copy");
        Ignore = Add("IgnoreButton", "Text", "&Ignore");
        Exceptions = Add("exceptionLabel", "Text", "Exception(s):", category: "ExceptionDetails");
        ExceptionDetails = Add("exceptionDetailsLabel", "Text", "Exception Details (double click on items to see details):", category: "ExceptionDetails");
        Property = Add("propertyColumnHeader", "Text", "Property", category: "ExceptionDetails");
        Information = Add("informationColumnHeader", "Text", "Information", category: "ExceptionDetails");
    }

    public TranslatedText Title { get; }

    public TranslatedText SubmitGitHubMessage { get; }

    public TranslatedText ToolTipCopy { get; }

    public TranslatedText ToolTipSendQuit { get; }

    public TranslatedText ToolTipQuit { get; }

    public TranslatedText NoReproStepsSupplied { get; }

    public TranslatedText GeneralTab { get; }

    public TranslatedText ExceptionTab { get; }

    public TranslatedText Warning { get; }

    public TranslatedText ExceptionType { get; }

    public TranslatedText TargetSite { get; }

    public TranslatedText Application { get; }

    public TranslatedText DateTime { get; }

    public TranslatedText ErrorDescription { get; }

    public TranslatedText SendAndQuit { get; }

    public TranslatedText Quit { get; }

    public TranslatedText Copy { get; }

    public TranslatedText Ignore { get; }

    public TranslatedText Exceptions { get; }

    public TranslatedText ExceptionDetails { get; }

    public TranslatedText Property { get; }

    public TranslatedText Information { get; }

    /// <summary>The label of the CLR version (not translated, as <c>_NO_TRANSLATE_ClrLabel</c>).</summary>
    public string Clr => "CLR:";

    /// <summary>The label of the git version (not translated, as <c>_NO_TRANSLATE_GitLabel</c>).</summary>
    public string Git => "Git:";
}

/// <summary>A property of an exception shown in the details of the bug report (a row of <c>ExceptionDetails</c>).</summary>
/// <param name="IsExtended">Whether it is extended information of Git Extensions, shown in bold.</param>
public sealed record BugReportProperty(string Name, string Value, bool IsExtended = false);

/// <summary>An exception of the bug report with its properties and inner exceptions (a node of <c>ExceptionDetails</c>).</summary>
public sealed record BugReportException(string Type, IReadOnlyList<BugReportProperty> Properties, IReadOnlyList<BugReportException> InnerExceptions);

/// <summary>The general information of the bug report (the 'General' tab).</summary>
public sealed record BugReportInfo(
    string HostApplication,
    string ExceptionType,
    string ExceptionMessage,
    string TargetSite,
    string Application,
    string GitVersion,
    string DateTime,
    string ClrVersion);

/// <summary>The services of the bug report dialog.</summary>
public interface IBugReportHost
{
    /// <summary>The URL of a new GitHub issue with the report and <paramref name="description"/>.</summary>
    string? BuildReportUrl(string description);

    /// <summary>The report with <paramref name="description"/> as text, to copy.</summary>
    string GetReportText(string description);

    void OpenUrl(string url);

    void CopyToClipboard(string text);
}

/// <summary>How the bug report dialog was closed (the dialog result of <c>BugReportForm</c>).</summary>
public enum BugReportResult
{
    /// <summary>Closed (<c>DialogResult.Cancel</c>).</summary>
    Closed,

    /// <summary>Quit, or sent and quit (<c>DialogResult.Abort</c>).</summary>
    Quit,

    /// <summary>Ignored (<c>DialogResult.Ignore</c>).</summary>
    Ignore,
}

/// <summary>The bug report dialog: a port of <c>BugReportForm</c>.</summary>
public sealed partial class BugReportViewModel : DialogViewModel
{
    private readonly IBugReportHost _host;
    private readonly IMessageBoxService _messageBox;
    private readonly bool _canIgnore;
    private bool _closing;

    /// <param name="canIgnore">Whether the application can go on (not terminating): the dialog can then be closed or ignored.</param>
    /// <param name="showIgnore">Whether the Ignore button is shown (for an external operation).</param>
    /// <param name="focusDetails">Whether the details are shown first, and the report cannot be sent (a user external operation).</param>
    public BugReportViewModel(BugReportStrings strings, IBugReportHost host, IMessageBoxService messageBox, BugReportInfo info, BugReportException exception,
        bool canIgnore, bool showIgnore, bool focusDetails)
    {
        Strings = strings;
        _host = host;
        _messageBox = messageBox;
        _canIgnore = canIgnore;
        Info = info;
        Exceptions = [exception];
        SelectedException = exception;
        IsIgnoreVisible = showIgnore;
        IsIgnoreEnabled = showIgnore && canIgnore;
        CanSend = !focusDetails;
        SelectedTabIndex = focusDetails ? 1 : 0;
    }

    public BugReportStrings Strings { get; }

    public BugReportInfo Info { get; }

    public string Title => $"{Info.HostApplication} {Strings.Title.Text}";

    public string ApplicationAndVersion => Info.Application;

    /// <summary>The exception and its inner exceptions (the tree of <c>ExceptionDetails</c>).</summary>
    public IReadOnlyList<BugReportException> Exceptions { get; }

    [ObservableProperty]
    public partial BugReportException? SelectedException { get; set; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = "";

    public bool IsIgnoreVisible { get; }

    public bool IsIgnoreEnabled { get; }

    /// <summary>Whether the report can be sent (hold back users from reporting user external operations directly).</summary>
    public bool CanSend { get; }

    public BugReportResult Result { get; private set; } = BugReportResult.Closed;

    /// <summary>The dialog of a terminating application closes only with its buttons (as <c>ControlBox = canIgnore</c>).</summary>
    public override bool CanClose() => _closing || _canIgnore;

    /// <summary>Whether <paramref name="input"/> has more than whitespace (<c>CheckContainsInfo</c>).</summary>
    public static bool ContainsInfo(string? input)
        => !string.IsNullOrWhiteSpace(WhitespaceRegex().Replace(input ?? "", string.Empty));

    [RelayCommand]
    private void Quit() => CloseWith(BugReportResult.Quit);

    [RelayCommand]
    private void Ignore() => CloseWith(BugReportResult.Ignore);

    [RelayCommand]
    private void SendAndQuit()
    {
        if (!ContainsInfo(Description))
        {
            _messageBox.ShowError(Strings.NoReproStepsSupplied.Text, Strings.Title.Text);
            return;
        }

        if (!_messageBox.Confirm(Strings.SubmitGitHubMessage.Text, Strings.Title.Text, defaultNo: true))
        {
            return;
        }

        if (_host.BuildReportUrl(Description) is { } url)
        {
            _host.OpenUrl(url);
        }

        CloseWith(BugReportResult.Quit);
    }

    [RelayCommand]
    private void Copy()
    {
        string report = _host.GetReportText(Description);
        if (!string.IsNullOrWhiteSpace(report))
        {
            _host.CopyToClipboard(report);
        }
    }

    private void CloseWith(BugReportResult result)
    {
        Result = result;
        _closing = true;
        Close(accepted: result != BugReportResult.Closed);
    }

    [GeneratedRegex(@"\s|\r|\n", RegexOptions.ExplicitCapture)]
    private static partial Regex WhitespaceRegex();
}
