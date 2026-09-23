using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the checkout revision dialog; ids match <c>FormCheckoutRevision</c>.</summary>
public sealed class CheckoutRevisionStrings : ViewStrings
{
    public CheckoutRevisionStrings()
        : base("FormCheckoutRevision")
    {
        Title = Add("$this", "Text", "Checkout revision");
        CheckoutThisRevision = Add("label2", "Text", "Checkout this &revision");
        Force = Add("Force", "Text", "&Force (reset local changes)");
        Checkout = Add("OkCheckout", "Text", "&Checkout");
        NoRevisionSelected = Add("_noRevisionSelectedMsgBox", "Text", "Select 1 revision to checkout.");
        NoRevisionSelectedCaption = Add("_noRevisionSelectedMsgBoxCaption", "Text", "Checkout");
    }

    public TranslatedText Title { get; }

    public TranslatedText CheckoutThisRevision { get; }

    public TranslatedText Force { get; }

    public TranslatedText Checkout { get; }

    public TranslatedText NoRevisionSelected { get; }

    public TranslatedText NoRevisionSelectedCaption { get; }
}

/// <summary>Operations of the checkout revision dialog that need the host (git, event scripts).</summary>
public interface ICheckoutRevisionHost
{
    /// <summary>Checks out the commit; returns <see langword="false"/> if cancelled by an event script.</summary>
    bool Checkout(ObjectId objectId, bool force);
}

/// <summary>View model of the checkout revision dialog (port of <c>FormCheckoutRevision</c>).</summary>
public sealed partial class CheckoutRevisionViewModel(
    CheckoutRevisionStrings strings,
    CommitPickerViewModel commitPicker,
    ICheckoutRevisionHost host,
    IMessageBoxService messageBoxes) : DialogViewModel
{
    public CheckoutRevisionStrings Strings { get; } = strings;

    public CommitPickerViewModel CommitPicker { get; } = commitPicker;

    [ObservableProperty]
    public partial bool Force { get; set; }

    [RelayCommand]
    private void Checkout()
    {
        ObjectId objectId = CommitPicker.SelectedObjectId;
        if (objectId.IsZero)
        {
            messageBoxes.ShowError(Strings.NoRevisionSelected.Text, Strings.NoRevisionSelectedCaption.Text);
            return;
        }

        if (host.Checkout(objectId, Force))
        {
            Close(accepted: true);
        }
    }
}

/// <summary>Strings of the compare to branch dialog; ids match <c>FormCompareToBranch</c>.</summary>
public sealed class CompareToBranchStrings : ViewStrings
{
    public CompareToBranchStrings()
        : base("FormCompareToBranch")
    {
        Title = Add("$this", "Text", "Compare to branch");
        Compare = Add("btnCompare", "Text", "Compare");
    }

    public TranslatedText Title { get; }

    public TranslatedText Compare { get; }
}

/// <summary>View model of the compare to branch dialog (port of <c>FormCompareToBranch</c>).</summary>
public sealed partial class CompareToBranchViewModel(CompareToBranchStrings strings, LocalRemoteBranchSelectorViewModel branchSelector) : DialogViewModel
{
    public CompareToBranchStrings Strings { get; } = strings;

    public LocalRemoteBranchSelectorViewModel BranchSelector { get; } = branchSelector;

    /// <summary>The branch to compare to, once accepted.</summary>
    public string? BranchName { get; private set; }

    [RelayCommand]
    private void Compare()
    {
        if (!string.IsNullOrWhiteSpace(BranchSelector.BranchName))
        {
            BranchName = BranchSelector.BranchName;
            Close(accepted: true);
        }
    }
}

/// <summary>Strings of the bisect dialog; ids match <c>FormBisect</c>.</summary>
public sealed class BisectStrings : ViewStrings
{
    public BisectStrings()
        : base("FormBisect")
    {
        Title = Add("$this", "Text", "Bisect");
        Start = Add("Start", "Text", "Start bisect");
        Good = Add("Good", "Text", "Mark current revision &good");
        Bad = Add("Bad", "Text", "Mark current revision &bad");
        Skip = Add("btnSkip", "Text", "&Skip current revision");
        Stop = Add("Stop", "Text", "Stop bisect");
        BisectStart = Add("_bisectStart", "Text", "Mark selected revisions as start bisect range?");
    }

    public TranslatedText Title { get; }

    public TranslatedText Start { get; }

    public TranslatedText Good { get; }

    public TranslatedText Bad { get; }

    public TranslatedText Skip { get; }

    public TranslatedText Stop { get; }

    public TranslatedText BisectStart { get; }
}

/// <summary>The answers to the current revision of a bisect (mirrors <c>GitBisectOption</c>).</summary>
public enum BisectMark
{
    Good,
    Bad,
    Skip,
}

/// <summary>Operations of the bisect dialog that need the host (git, the revision grid).</summary>
public interface IBisectHost
{
    bool IsInTheMiddleOfBisect();

    void Start();

    /// <summary>Whether a range of revisions is selected in the revision grid.</summary>
    bool HasSelectedRange();

    /// <summary>Marks the first selected revision good and the last bad.</summary>
    void MarkSelectedRange();

    void Mark(BisectMark mark);

    void Stop();
}

/// <summary>View model of the bisect dialog (port of <c>FormBisect</c>).</summary>
public sealed partial class BisectViewModel : DialogViewModel
{
    private readonly IBisectHost _host;
    private readonly IMessageBoxService _messageBoxes;

    public BisectViewModel(BisectStrings strings, IBisectHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        IsInTheMiddleOfBisect = host.IsInTheMiddleOfBisect();
    }

    public BisectStrings Strings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    public partial bool IsInTheMiddleOfBisect { get; private set; }

    public bool CanStart => !IsInTheMiddleOfBisect;

    [RelayCommand]
    private void Start()
    {
        _host.Start();
        IsInTheMiddleOfBisect = _host.IsInTheMiddleOfBisect();

        if (_host.HasSelectedRange() && _messageBoxes.Confirm(Strings.BisectStart.Text, Strings.Title.Text))
        {
            _host.MarkSelectedRange();
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void Mark(BisectMark mark)
    {
        _host.Mark(mark);
        Close(accepted: true);
    }

    [RelayCommand]
    private void Stop()
    {
        _host.Stop();
        Close(accepted: true);
    }
}

/// <summary>Strings of the go to commit dialog; ids match <c>FormGoToCommit</c>.</summary>
public sealed class GoToCommitStrings : ViewStrings
{
    public GoToCommitStrings()
        : base("FormGoToCommit")
    {
        Title = Add("$this", "Text", "Go to commit");
        CommitExpression = Add("label1", "Text", "Commit expression:");
        GoToTag = Add("label3", "Text", "Go to tag:");
        GoToBranch = Add("label4", "Text", "Go to branch:");
        Help = Add("groupBox1", "Text", "Help");
        Examples = Add(
            "label2",
            "Text",
            "Commit expression examples:\r\n- complete commit hash: e. g.: 8eab51fcb9c4538eb74c4dcd4c31ffd693ad25c9\r\n- partial commit hash (if unique): e. g.: 8eab51fcb9c453\r\n- tag name\r\n- branch name");
        RevParseLink = Add("linkGitRevParse", "Text", "More see git-rev-parse");
        Go = Add("goButton", "Text", "Go");
    }

    public TranslatedText Title { get; }

    public TranslatedText CommitExpression { get; }

    public TranslatedText GoToTag { get; }

    public TranslatedText GoToBranch { get; }

    public TranslatedText Help { get; }

    public TranslatedText Examples { get; }

    public TranslatedText RevParseLink { get; }

    public TranslatedText Go { get; }
}

/// <summary>A branch or tag to go to.</summary>
public sealed record GitRefItem(string Name, string Guid)
{
    public override string ToString() => Name;
}

/// <summary>The input of the go to commit dialog that supplies the revision: the one last focused.</summary>
public enum GoToCommitSource
{
    Expression,
    Tag,
    Branch,
}

/// <summary>View model of the go to commit dialog (port of <c>FormGoToCommit</c>).</summary>
public sealed partial class GoToCommitViewModel : DialogViewModel
{
    private readonly Action _openRevParseHelp;

    /// <param name="clipboardRevision">The clipboard text, if it is a valid revision (offered as expression).</param>
    public GoToCommitViewModel(
        GoToCommitStrings strings,
        IReadOnlyList<GitRefItem> tags,
        IReadOnlyList<GitRefItem> branches,
        string? clipboardRevision,
        Action openRevParseHelp)
    {
        Strings = strings;
        Tags = tags;
        Branches = branches;
        CommitExpression = clipboardRevision ?? "";
        _openRevParseHelp = openRevParseHelp;
    }

    public GoToCommitStrings Strings { get; }

    public IReadOnlyList<GitRefItem> Tags { get; }

    public IReadOnlyList<GitRefItem> Branches { get; }

    [ObservableProperty]
    public partial string CommitExpression { get; set; }

    [ObservableProperty]
    public partial string TagText { get; set; } = "";

    [ObservableProperty]
    public partial string BranchText { get; set; } = "";

    /// <summary>The input that supplies the revision, set by the view when an input gets the focus.</summary>
    [ObservableProperty]
    public partial GoToCommitSource Source { get; set; }

    /// <summary>The revision to go to, from <see cref="Source"/>; as <c>FormGoToCommit.SetSelectedRevisionByFocusedControl</c>.</summary>
    public string SelectedRevision => Source switch
    {
        GoToCommitSource.Tag => Tags.FirstOrDefault(t => t.Name == TagText)?.Guid ?? "",
        GoToCommitSource.Branch => Branches.FirstOrDefault(b => b.Name == BranchText)?.Guid ?? "",
        _ => CommitExpression.Trim(),
    };

    /// <summary>Goes to a branch or tag picked from a list, as the WinForms combo boxes did on selection.</summary>
    public void GoTo(GoToCommitSource source, GitRefItem item)
    {
        Source = source;
        if (source == GoToCommitSource.Tag)
        {
            TagText = item.Name;
        }
        else
        {
            BranchText = item.Name;
        }

        Go();
    }

    [RelayCommand]
    private void Go() => Close(accepted: true);

    [RelayCommand]
    private void OpenRevParseHelp() => _openRevParseHelp();
}
