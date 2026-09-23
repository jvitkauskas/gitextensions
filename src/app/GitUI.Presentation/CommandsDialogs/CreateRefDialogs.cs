using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Git;
using GitCommands.Git.Extensions;
using GitCommands.Git.Tag;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the create branch dialog; ids match <c>FormCreateBranch</c>.</summary>
public sealed class CreateBranchStrings : ViewStrings
{
    public CreateBranchStrings()
        : base("FormCreateBranch")
    {
        Title = Add("$this", "Text", "Create branch");
        BranchName = Add("label1", "Text", "&Branch name");
        CreateBranchAtRevision = Add("lblCreateBranch", "Text", "Create b&ranch at this revision");
        CreatingOrphanBranch = Add("_creatingOrphanBranch", "Text", "Creating orphan branch (repository has no commits)");
        CheckoutAfterCreate = Add("chkCheckoutAfterCreate", "Text", "Checkout &after create");
        Orphan = Add("grpOrphan", "Text", "Orphan");
        CreateOrphan = Add("chkCreateOrphan", "Text", "Create or&phan");
        CreateOrphanTooltip = Add("chkCreateOrphan", "toolTip", "New branch will have NO parents");
        ClearOrphan = Add("chkClearOrphan", "Text", "Clear &working directory and index");
        ClearOrphanTooltip = Add("chkClearOrphan", "toolTip", "Remove files from the working directory and from the index");
        CreateBranch = Add("cmdOk", "Text", "&Create branch");
        NoRevisionSelected = Add("_noRevisionSelected", "Text", "Select 1 revision to create the branch on.");
        BranchNameIsEmpty = Add("_branchNameIsEmpty", "Text", "Enter branch name.");
        BranchNameIsNotValid = Add("_branchNameIsNotValid", "Text", "“{0}” is not valid branch name.");
    }

    public TranslatedText Title { get; }

    public TranslatedText BranchName { get; }

    public TranslatedText CreateBranchAtRevision { get; }

    public TranslatedText CreatingOrphanBranch { get; }

    public TranslatedText CheckoutAfterCreate { get; }

    public TranslatedText Orphan { get; }

    public TranslatedText CreateOrphan { get; }

    public TranslatedText CreateOrphanTooltip { get; }

    public TranslatedText ClearOrphan { get; }

    public TranslatedText ClearOrphanTooltip { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText NoRevisionSelected { get; }

    public TranslatedText BranchNameIsEmpty { get; }

    public TranslatedText BranchNameIsNotValid { get; }
}

/// <summary>How the create branch dialog starts (the options of <c>FormCreateBranch</c>).</summary>
/// <param name="IsOrphanOnly">Whether the repository has no commits, so that only an orphan branch can be created.</param>
public sealed record CreateBranchOptions(
    string? BranchName,
    bool IsOrphanOnly = false,
    bool CheckoutAfterCreation = true,
    bool UserAbleToChangeRevision = true,
    bool CouldBeOrphan = true);

/// <summary>Operations of the create branch dialog that need the host (git).</summary>
public interface ICreateBranchHost
{
    /// <summary>What the commit summary shows of the commit.</summary>
    CommitSummary GetSummary(ObjectId objectId);

    bool IsValidBranchName(string branchName);

    /// <summary>Creates the branch in the progress dialog; returns whether it succeeded.</summary>
    /// <param name="objectId">The commit to create the branch at; ignored for an orphan branch.</param>
    bool CreateBranch(string branchName, ObjectId objectId, bool checkout, bool orphan, bool clearOrphan);
}

/// <summary>View model of the create branch dialog (port of <c>FormCreateBranch</c>).</summary>
public sealed partial class CreateBranchViewModel : DialogViewModel
{
    private readonly IGitBranchNameNormaliser _branchNameNormaliser;
    private readonly GitBranchNameOptions _branchNameOptions;
    private readonly bool _autoNormalise;
    private readonly ICreateBranchHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private bool _userAbleToChangeRevision = true;

    public CreateBranchViewModel(
        CreateBranchStrings strings,
        CommitSummaryStrings summaryStrings,
        CommitPickerViewModel commitPicker,
        CreateBranchOptions options,
        IGitBranchNameNormaliser branchNameNormaliser,
        GitBranchNameOptions branchNameOptions,
        bool autoNormalise,
        ICreateBranchHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        CommitPicker = commitPicker;
        Summary = new CommitSummaryViewModel(summaryStrings);
        _branchNameNormaliser = branchNameNormaliser;
        _branchNameOptions = branchNameOptions;
        _autoNormalise = autoNormalise;
        _host = host;
        _messageBoxes = messageBoxes;

        IsOrphanOnly = options.IsOrphanOnly;
        BranchName = options.BranchName ?? "";
        CanCreateOrphan = options.CouldBeOrphan || options.IsOrphanOnly;
        CommitPicker.IsEnabled = options.UserAbleToChangeRevision;
        CheckoutAfterCreate = options.CheckoutAfterCreation;
        if (IsOrphanOnly)
        {
            // As FormCreateBranch.ConfigureForOrphanBranch.
            CreateOrphan = true;
            ClearOrphan = false;
        }

        CommitPicker.SelectedObjectIdChanged += (_, _) => Summary.Summary = CommitPicker.SelectedObjectId.IsZero ? null : _host.GetSummary(CommitPicker.SelectedObjectId);
    }

    public CreateBranchStrings Strings { get; }

    public CommitPickerViewModel CommitPicker { get; }

    public CommitSummaryViewModel Summary { get; }

    /// <summary>Whether the repository has no commits, so that only an orphan branch can be created.</summary>
    public bool IsOrphanOnly { get; }

    public string CreateBranchCaption => IsOrphanOnly ? Strings.CreatingOrphanBranch.Text : Strings.CreateBranchAtRevision.AccessKeyText;

    /// <summary>Whether the orphan options are available.</summary>
    public bool CanCreateOrphan { get; }

    [ObservableProperty]
    public partial string BranchName { get; set; }

    [ObservableProperty]
    public partial bool CheckoutAfterCreate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChangeCheckout), nameof(CanChangeClearOrphan))]
    public partial bool CreateOrphan { get; set; }

    [ObservableProperty]
    public partial bool ClearOrphan { get; set; } = true;

    /// <summary>An orphan branch is always checked out.</summary>
    public bool CanChangeCheckout => !CreateOrphan;

    public bool CanChangeClearOrphan => CreateOrphan;

    public bool CanChangeCreateOrphan => CanCreateOrphan && !IsOrphanOnly;

    partial void OnCreateOrphanChanged(bool value)
    {
        if (value)
        {
            CheckoutAfterCreate = true;
            _userAbleToChangeRevision = CommitPicker.IsEnabled;
            CommitPicker.IsEnabled = false;
        }
        else
        {
            CommitPicker.IsEnabled = _userAbleToChangeRevision;
        }
    }

    /// <summary>
    ///  Normalises <see cref="BranchName"/> if auto-normalisation is enabled.
    ///  Done when the name box loses focus, and before creating the branch.
    /// </summary>
    public void NormaliseBranchName()
    {
        if (!_autoNormalise || !BranchName.Any(PathUtil.IsValidPathChar))
        {
            return;
        }

        BranchName = _branchNameNormaliser.Normalise(BranchName, _branchNameOptions);
    }

    [RelayCommand]
    private void Create()
    {
        NormaliseBranchName();

        ObjectId objectId = default;
        if (!CreateOrphan)
        {
            objectId = CommitPicker.SelectedObjectId;
            if (objectId.IsZeroOrArtificial)
            {
                _messageBoxes.ShowError(Strings.NoRevisionSelected.Text, Strings.Title.PlainText);
                return;
            }
        }

        string branchName = BranchName.Trim();
        if (string.IsNullOrWhiteSpace(branchName))
        {
            _messageBoxes.ShowError(Strings.BranchNameIsEmpty.Text, Strings.Title.PlainText);
            return;
        }

        if (!_host.IsValidBranchName(branchName))
        {
            _messageBoxes.ShowError(string.Format(Strings.BranchNameIsNotValid.Text, branchName), Strings.Title.PlainText);
            return;
        }

        if (_host.CreateBranch(branchName, objectId, CheckoutAfterCreate, CreateOrphan, ClearOrphan))
        {
            Close(accepted: true);
        }
    }
}

/// <summary>Strings of the create tag dialog; ids match <c>FormCreateTag</c>.</summary>
public sealed class CreateTagStrings : ViewStrings
{
    public CreateTagStrings()
        : base("FormCreateTag")
    {
        Title = Add("$this", "Text", "Create tag");
        TagName = Add("label1", "Text", "Tag name");
        CreateTagAtRevision = Add("label3", "Text", "Create tag at this revision");
        Force = Add("ForceTag", "Text", "Force");
        Lightweight = Add("_trsLightweight", "Text", "Lightweight tag");
        Annotated = Add("_trsAnnotated", "Text", "Annotated tag");
        SignDefault = Add("_trsSignDefault", "Text", "Sign with default GPG");
        SignSpecificKey = Add("_trsSignSpecificKey", "Text", "Sign with specific GPG");
        KeyId = Add("keyIdLbl", "Text", "Specific Key Id");
        Message = Add("label2", "Text", "Message");
        PushTo = Add("_pushToCaption", "Text", "Push tag to '{0}'");
        CreateTag = Add("Ok", "Text", "Create tag");
        MessageCaption = Add("_messageCaption", "Text", "Tag");
        NoRevisionSelected = Add("_noRevisionSelected", "Text", "Select 1 revision to create the tag on.");
    }

    public TranslatedText Title { get; }

    public TranslatedText TagName { get; }

    public TranslatedText CreateTagAtRevision { get; }

    public TranslatedText Force { get; }

    public TranslatedText Lightweight { get; }

    public TranslatedText Annotated { get; }

    public TranslatedText SignDefault { get; }

    public TranslatedText SignSpecificKey { get; }

    public TranslatedText KeyId { get; }

    public TranslatedText Message { get; }

    public TranslatedText PushTo { get; }

    public TranslatedText CreateTag { get; }

    public TranslatedText MessageCaption { get; }

    public TranslatedText NoRevisionSelected { get; }
}

/// <summary>Operations of the create tag dialog that need the host (git, remotes, event scripts).</summary>
public interface ICreateTagHost
{
    /// <summary>Creates the tag; returns whether it succeeded (git errors are shown by the host).</summary>
    bool CreateTag(GitCreateTagArgs args);

    /// <summary>Pushes the tag to the remote.</summary>
    void PushTag(string remote, string tagName);
}

/// <summary>View model of the create tag dialog (port of <c>FormCreateTag</c>).</summary>
public sealed partial class CreateTagViewModel : DialogViewModel
{
    private readonly string _remote;
    private readonly ICreateTagHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    /// <param name="remote">The remote to push the tag to.</param>
    public CreateTagViewModel(
        CreateTagStrings strings,
        CommitPickerViewModel commitPicker,
        string remote,
        ICreateTagHost host,
        IMessageBoxService messageBoxes,
        string errorCaption)
    {
        Strings = strings;
        CommitPicker = commitPicker;
        _remote = remote;
        _host = host;
        _messageBoxes = messageBoxes;
        _errorCaption = errorCaption;
        Kinds = [strings.Lightweight.Text, strings.Annotated.Text, strings.SignDefault.Text, strings.SignSpecificKey.Text];
        PushToText = string.Format(strings.PushTo.Text, remote);
    }

    public CreateTagStrings Strings { get; }

    public CommitPickerViewModel CommitPicker { get; }

    /// <summary>The kinds of tags, in the order of <see cref="GetOperation"/>.</summary>
    public IReadOnlyList<string> Kinds { get; }

    public string PushToText { get; }

    [ObservableProperty]
    public partial string TagName { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEnterKeyId), nameof(CanEnterMessage))]
    public partial int SelectedKindIndex { get; set; }

    public TagOperation Operation => GetOperation(SelectedKindIndex);

    public bool CanEnterKeyId => Operation == TagOperation.SignWithSpecificKey;

    public bool CanEnterMessage => Operation.CanProvideMessage();

    [ObservableProperty]
    public partial string KeyId { get; set; } = "";

    [ObservableProperty]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial bool Force { get; set; }

    [ObservableProperty]
    public partial bool PushTag { get; set; }

    /// <summary>The tag operation of the kind at the index, as <c>FormCreateTag.GetSelectedOperation</c>.</summary>
    public static TagOperation GetOperation(int index) => index switch
    {
        0 => TagOperation.Lightweight,
        1 => TagOperation.Annotate,
        2 => TagOperation.SignWithDefaultKey,
        3 => TagOperation.SignWithSpecificKey,
        _ => throw new NotSupportedException("Invalid dropdownSelection")
    };

    [RelayCommand]
    private void Create()
    {
        try
        {
            ObjectId objectId = CommitPicker.SelectedObjectId;
            if (objectId.IsZero)
            {
                _messageBoxes.ShowError(Strings.NoRevisionSelected.Text, Strings.MessageCaption.Text);
                return;
            }

            if (!_host.CreateTag(new GitCreateTagArgs(TagName, objectId, Operation, Message, KeyId, Force)))
            {
                return;
            }

            if (PushTag && !string.IsNullOrEmpty(TagName))
            {
                _host.PushTag(_remote, TagName);
            }

            Close(accepted: true);
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(ex.Message, _errorCaption);
        }
    }
}
