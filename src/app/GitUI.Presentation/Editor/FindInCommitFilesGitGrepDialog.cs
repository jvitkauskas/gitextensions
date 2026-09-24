using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.Editor;

/// <summary>Strings of the git grep prompt; ids match <c>FormFindInCommitFilesGitGrep</c>.</summary>
public sealed class FindInCommitFilesGitGrepStrings : ViewStrings
{
    public FindInCommitFilesGitGrepStrings()
        : base("FormFindInCommitFilesGitGrep")
    {
        Title = Add("$this", "Text", "Find in commit files using git-grep");
        FindWhat = Add("label1", "Text", "Fi&nd what:");
        Watermark = Add("cboFindInCommitFilesGitGrep", "Watermark", "git-grep regular expression...");
        Options = Add("lblOptions", "Text", "&Options:");
        MatchCase = Add("chkMatchCase", "Text", "Match &case");
        MatchWholeWord = Add("chkMatchWholeWord", "Text", "Match &whole word");
        ShowSearchBox = Add("chkShowSearchBox", "Text", "&Show 'Find in commit files using git-grep'");
        Find = Add("btnSearch", "Text", "&Find");
    }

    public TranslatedText Title { get; }

    public TranslatedText FindWhat { get; }

    public TranslatedText Watermark { get; }

    public TranslatedText Options { get; }

    public TranslatedText MatchCase { get; }

    public TranslatedText MatchWholeWord { get; }

    public TranslatedText ShowSearchBox { get; }

    public TranslatedText Find { get; }
}

/// <summary>
///  The file status list that searches its files with git grep (the <c>FilesGitGrepLocator</c> and
///  <c>FindInCommitFilesGitGrepToggle</c> actions of <c>FormFindInCommitFilesGitGrep</c>), and the git grep settings.
/// </summary>
public interface IFindInCommitFilesGitGrepHost
{
    /// <summary>Searches the files of the list with the expression; an empty one ends the search.</summary>
    void Search(string expression);

    /// <summary>Shows or hides the git grep search box of the list.</summary>
    void SetSearchBoxVisible(bool visible);

    /// <summary>The additional arguments of git grep (<c>AppSettings.GitGrepUserArguments</c>).</summary>
    string UserArguments { get; set; }

    /// <summary><c>AppSettings.GitGrepIgnoreCase</c>.</summary>
    bool IgnoreCase { get; set; }

    /// <summary><c>AppSettings.GitGrepMatchWholeWord</c>.</summary>
    bool MatchWholeWord { get; set; }

    /// <summary>Whether the list shows its git grep search box (<c>AppSettings.ShowFindInCommitFilesGitGrep</c>).</summary>
    bool ShowSearchBox { get; set; }
}

/// <summary>View model of the git grep prompt of the file status list (port of <c>FormFindInCommitFilesGitGrep</c>), a modeless window.</summary>
public sealed partial class FindInCommitFilesGitGrepViewModel : DialogViewModel
{
    private readonly IFindInCommitFilesGitGrepHost _host;
    private bool _updating;

    public FindInCommitFilesGitGrepViewModel(FindInCommitFilesGitGrepStrings strings, IFindInCommitFilesGitGrepHost host)
    {
        Strings = strings;
        _host = host;

        // As OnShown.
        _updating = true;
        try
        {
            UserArguments = host.UserArguments;
            MatchCase = !host.IgnoreCase;
            MatchWholeWord = host.MatchWholeWord;
            ShowSearchBox = host.ShowSearchBox;
        }
        finally
        {
            _updating = false;
        }
    }

    public FindInCommitFilesGitGrepStrings Strings { get; }

    /// <summary>The git grep expression (<c>GitGrepExpressionText</c>).</summary>
    [ObservableProperty]
    public partial string Expression { get; set; } = "";

    /// <summary>The previous expressions of the search box of the list (<c>SetSearchItems</c>).</summary>
    public ObservableCollection<string> SearchItems { get; } = [];

    [ObservableProperty]
    public partial string UserArguments { get; set; } = "";

    [ObservableProperty]
    public partial bool MatchCase { get; set; }

    [ObservableProperty]
    public partial bool MatchWholeWord { get; set; }

    [ObservableProperty]
    public partial bool ShowSearchBox { get; set; }

    /// <summary>Raised when the expression should get the focus (the <c>ActiveControl</c> of <c>GitGrepExpressionText</c>).</summary>
    public event EventHandler? FocusExpressionRequested;

    /// <summary>
    ///  As <c>ShowFindInCommitFileGitGrepDialog</c> each time it shows the prompt: the expression (kept if <see langword="null"/>),
    ///  the previous expressions and the visibility of the search box of the list.
    /// </summary>
    public void SetState(string? expression, IEnumerable<string> searchItems, bool showSearchBox)
    {
        if (expression is not null)
        {
            Expression = expression;
        }

        SearchItems.Clear();
        foreach (string item in searchItems)
        {
            SearchItems.Add(item);
        }

        // As SetShowFindInCommitFilesGitGrep: stores the setting, without toggling the box of the list.
        _updating = true;
        try
        {
            ShowSearchBox = showSearchBox;
            _host.ShowSearchBox = showSearchBox;
        }
        finally
        {
            _updating = false;
        }

        FocusExpressionRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>As <c>FormFindInCommitFilesGitGrep_FormClosing</c>: the search ends if its box is hidden or its expression empty.</summary>
    public override bool CanClose()
    {
        if (string.IsNullOrEmpty(Expression) || !ShowSearchBox)
        {
            _host.Search("");
        }

        return true;
    }

    [RelayCommand]
    private void Search() => _host.Search(Expression ?? "");

    partial void OnUserArgumentsChanged(string value)
    {
        if (!_updating)
        {
            _host.UserArguments = value;
        }
    }

    partial void OnMatchCaseChanged(bool value)
    {
        if (!_updating)
        {
            _host.IgnoreCase = !value;
        }
    }

    partial void OnMatchWholeWordChanged(bool value)
    {
        if (!_updating)
        {
            _host.MatchWholeWord = value;
        }
    }

    partial void OnShowSearchBoxChanged(bool value)
    {
        if (_updating)
        {
            return;
        }

        _host.ShowSearchBox = value;
        _host.SetSearchBoxVisible(value);
    }
}
