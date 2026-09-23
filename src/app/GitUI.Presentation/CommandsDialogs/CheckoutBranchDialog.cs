using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the checkout branch dialog; ids match <c>FormCheckoutBranch</c>.</summary>
public sealed class CheckoutBranchStrings : ViewStrings
{
    public CheckoutBranchStrings()
        : base("FormCheckoutBranch")
    {
        Title = Add("$this", "Text", "Checkout branch");
        LocalBranch = Add("LocalBranch", "Text", "Local &branch");
        RemoteBranch = Add("Remotebranch", "Text", "Remote &branch");
        SelectBranch = Add("label1", "Text", "&Select branch");
        InvalidBranchName = Add("_invalidBranchName", "Text", "An existing branch must be selected.");
        DontCreate = Add("rbDontCreate", "Text", "Ch&eckout the commit (in detached head)");
        ResetBranch = Add("rbResetBranch", "Text", "R&eset local branch with the name:");
        CreateBranch = Add("_createBranch", "Text", "Cr&eate local branch with same name:");
        CreateBranchWithCustomName = Add("rbCreateBranchWithCustomName", "Text", "Cr&eate local branch with custom name:");
        LocalChanges = Add("localChangesGB", "Text", "Local changes");
        DontChange = Add("rbDontChange", "Text", "Do&n't change");
        Merge = Add("rbMerge", "Text", "&Merge");
        Stash = Add("rbStash", "Text", "S&tash");
        Reset = Add("rbReset", "Text", "&Reset");
        SetAsDefault = Add("chkSetLocalChangesActionAsDefault", "Text", "Set as &default");
        Checkout = Add("Ok", "Text", "&Checkout");
        CustomBranchNameIsEmpty = Add("_customBranchNameIsEmpty", "Text", "Custom branch name is empty.\nEnter valid branch name or select predefined value.");
        CustomBranchNameIsNotValid = Add("_customBranchNameIsNotValid", "Text", "“{0}” is not valid branch name.\nEnter valid branch name or select predefined value.");
        ResetNonFastForwardBranch = Add(
            "_resetNonFastForwardBranch",
            "Text",
            "You are going to reset the “{0}” branch to a new location discarding ALL the commited changes since the {1} revision.\n\nAre you sure?");
        ResetCaption = Add("_resetCaption", "Text", "Reset branch");
        ApplyStashedItemsAgain = Add("_applyStashedItemsAgain", "Text", "Apply stashed items to working directory again?");
        ApplyStashedItemsAgainCaption = Add("_applyStashedItemsAgainCaption", "Text", "Auto stash");
    }

    public TranslatedText Title { get; }

    public TranslatedText LocalBranch { get; }

    public TranslatedText RemoteBranch { get; }

    public TranslatedText SelectBranch { get; }

    public TranslatedText InvalidBranchName { get; }

    public TranslatedText DontCreate { get; }

    public TranslatedText ResetBranch { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText CreateBranchWithCustomName { get; }

    public TranslatedText LocalChanges { get; }

    public TranslatedText DontChange { get; }

    public TranslatedText Merge { get; }

    public TranslatedText Stash { get; }

    public TranslatedText Reset { get; }

    public TranslatedText SetAsDefault { get; }

    public TranslatedText Checkout { get; }

    public TranslatedText CustomBranchNameIsEmpty { get; }

    public TranslatedText CustomBranchNameIsNotValid { get; }

    public TranslatedText ResetNonFastForwardBranch { get; }

    public TranslatedText ResetCaption { get; }

    public TranslatedText ApplyStashedItemsAgain { get; }

    public TranslatedText ApplyStashedItemsAgainCaption { get; }
}

/// <summary>How a checkout ended (the dialog results of <c>FormCheckoutBranch.PerformCheckout</c>).</summary>
public enum CheckoutOutcome
{
    /// <summary>Checked out (<c>DialogResult.OK</c>).</summary>
    Succeeded,

    /// <summary>Not checked out, the dialog stays open (<c>DialogResult.None</c>).</summary>
    Failed,

    /// <summary>Cancelled by an event script (<c>DialogResult.Cancel</c>).</summary>
    Cancelled,
}

/// <summary>How the checkout branch dialog starts (the constructor arguments and settings of <c>FormCheckoutBranch</c>).</summary>
/// <param name="IsDirtyDir">Whether the working directory has changes, <see langword="null"/> if not checked (too slow).</param>
public sealed record CheckoutBranchOptions(
    string? Branch,
    bool Remote,
    bool? IsDirtyDir,
    LocalChangesAction ChangesMode,
    bool CreateLocalBranchForRemote,
    bool AlwaysShowDialog,
    bool UseDefaultAction);

/// <summary>Operations of the checkout branch dialog that need the host (git, settings, stashes, event scripts).</summary>
public interface ICheckoutBranchHost
{
    /// <summary>The local or remote branches to choose from (only those containing given commits, if the host limits them).</summary>
    IReadOnlyList<string> GetBranches(bool remote);

    /// <summary>Whether the branches are limited to those containing given commits.</summary>
    bool HasContainFilter { get; }

    IReadOnlyList<string> GetRemoteNames();

    /// <summary>The name of the local branch tracking the remote branch, if any.</summary>
    string? GetLocalTrackingBranchName(string remote, string remoteBranch);

    /// <summary>The commit of a local or remote branch; zero if there is no such branch.</summary>
    ObjectId GetBranchObjectId(string branch, bool remote);

    ObjectId GetMergeBase(ObjectId a, ObjectId b);

    /// <summary>Computes (in the background) how the branch relates to the current checkout and reports it on the UI thread.</summary>
    void RequestCommitCount(string branch, Action<string> report);

    bool IsValidBranchName(string branchName);

    bool IsDirtyDir();

    void SaveDefaultLocalChangesAction(LocalChangesAction action);

    void StashSave();

    /// <summary>
    ///  Checks out the branch (running event scripts, and offering to apply an auto stash again if <paramref name="stashed"/>).
    /// </summary>
    CheckoutOutcome Checkout(string branch, bool remote, LocalChangesAction localChanges, CheckoutNewBranchMode newBranchMode, string? newBranchName, bool stashed);
}

/// <summary>View model of the checkout branch dialog (port of <c>FormCheckoutBranch</c>).</summary>
public sealed partial class CheckoutBranchViewModel : DialogViewModel
{
    private readonly CheckoutBranchOptions _options;
    private readonly IGitBranchNameNormaliser _branchNameNormaliser;
    private readonly GitBranchNameOptions _branchNameOptions;
    private readonly bool _autoNormalise;
    private readonly ICheckoutBranchHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly bool _isLoading;
    private bool? _isDirtyDir;
    private string _localBranchName = "";

    public CheckoutBranchViewModel(
        CheckoutBranchStrings strings,
        CheckoutBranchOptions options,
        IGitBranchNameNormaliser branchNameNormaliser,
        GitBranchNameOptions branchNameOptions,
        bool autoNormalise,
        ICheckoutBranchHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _options = options;
        _branchNameNormaliser = branchNameNormaliser;
        _branchNameOptions = branchNameOptions;
        _autoNormalise = autoNormalise;
        _host = host;
        _messageBoxes = messageBoxes;
        _isDirtyDir = options.IsDirtyDir;
        ResetBranchText = strings.ResetBranch.AccessKeyText;

        // As the FormCheckoutBranch constructor.
        _isLoading = true;
        IsRemote = options.Remote;
        PopulateBranches();
        if (!string.IsNullOrEmpty(options.Branch))
        {
            if (!Branches.Contains(options.Branch))
            {
                Branches = [.. Branches, options.Branch];
            }

            Branch = options.Branch;
        }

        if (host.HasContainFilter && Branches.Count == 0)
        {
            IsRemote = !options.Remote;
            PopulateBranches();
        }

        ChangesMode = options.ChangesMode;
        IsCreateBranchWithCustomName = options.CreateLocalBranchForRemote;
        _isLoading = false;
        UpdateRemoteBranchNames();
    }

    public CheckoutBranchStrings Strings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocal))]
    public partial bool IsRemote { get; set; }

    public bool IsLocal
    {
        get => !IsRemote;
        set => IsRemote = !value;
    }

    [ObservableProperty]
    public partial IReadOnlyList<string> Branches { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBranchInvalid))]
    public partial string Branch { get; set; } = "";

    /// <summary>Whether the entered branch is not one of <see cref="Branches"/> (highlighted, as the WinForms error provider).</summary>
    public bool IsBranchInvalid => Branch.Length > 0 && !Branches.Contains(Branch);

    [ObservableProperty]
    public partial string CommitCountText { get; private set; } = "";

    /// <summary>Whether the working directory may have changes, which shows the local changes options.</summary>
    public bool HasUncommittedChanges => _isDirtyDir ?? true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDontChange), nameof(IsMerge), nameof(IsStash), nameof(IsReset), nameof(CanSetAsDefault))]
    public partial LocalChangesAction ChangesMode { get; set; }

    public bool IsDontChange
    {
        get => ChangesMode == LocalChangesAction.DontChange;
        set => SetChangesMode(value, LocalChangesAction.DontChange);
    }

    public bool IsMerge
    {
        get => ChangesMode == LocalChangesAction.Merge;
        set => SetChangesMode(value, LocalChangesAction.Merge);
    }

    public bool IsStash
    {
        get => ChangesMode == LocalChangesAction.Stash;
        set => SetChangesMode(value, LocalChangesAction.Stash);
    }

    public bool IsReset
    {
        get => ChangesMode == LocalChangesAction.Reset;
        set => SetChangesMode(value, LocalChangesAction.Reset);
    }

    /// <summary>A reset is never remembered as the default.</summary>
    public bool CanSetAsDefault => ChangesMode != LocalChangesAction.Reset;

    [ObservableProperty]
    public partial bool SetAsDefault { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResetBranch), nameof(IsCreateBranchWithCustomName))]
    public partial bool IsDontCreate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDontCreate), nameof(IsCreateBranchWithCustomName))]
    public partial bool IsResetBranch { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDontCreate), nameof(IsResetBranch))]
    public partial bool IsCreateBranchWithCustomName { get; set; }

    /// <summary>"Reset local branch" if the tracking branch exists, "create local branch" otherwise.</summary>
    [ObservableProperty]
    public partial string ResetBranchText { get; private set; }

    /// <summary>The local tracking branch, quoted.</summary>
    [ObservableProperty]
    public partial string LocalBranchNameText { get; private set; } = "''";

    [ObservableProperty]
    public partial string CustomBranchName { get; set; } = "";

    private void SetChangesMode(bool isChecked, LocalChangesAction mode)
    {
        if (isChecked)
        {
            ChangesMode = mode;
        }
    }

    partial void OnChangesModeChanged(LocalChangesAction value)
    {
        if (value == LocalChangesAction.Reset)
        {
            SetAsDefault = false;
        }
    }

    // The three remote options are mutually exclusive radio buttons.
    partial void OnIsDontCreateChanged(bool value) => ExcludeOthers(value, dontCreate: true);

    partial void OnIsResetBranchChanged(bool value) => ExcludeOthers(value, resetBranch: true);

    partial void OnIsCreateBranchWithCustomNameChanged(bool value) => ExcludeOthers(value, custom: true);

    private void ExcludeOthers(bool isChecked, bool dontCreate = false, bool resetBranch = false, bool custom = false)
    {
        if (!isChecked)
        {
            return;
        }

        IsDontCreate = dontCreate;
        IsResetBranch = resetBranch;
        IsCreateBranchWithCustomName = custom;
    }

    partial void OnIsRemoteChanged(bool value)
    {
        if (_isLoading)
        {
            return;
        }

        PopulateBranches();
        UpdateRemoteBranchNames();
    }

    partial void OnBranchChanged(string value)
    {
        if (!_isLoading)
        {
            UpdateRemoteBranchNames();
        }
    }

    private void PopulateBranches()
    {
        // As FormCheckoutBranch.PopulateBranches.
        Branches = [.. _host.GetBranches(IsRemote).Where(name => !string.IsNullOrWhiteSpace(name))];
        Branch = _host.HasContainFilter && Branches.Count == 1 ? Branches[0] : "";
    }

    private void UpdateRemoteBranchNames()
    {
        // As FormCheckoutBranch.Branches_SelectedIndexChanged.
        CommitCountText = "";
        string branch = Branch;
        string newLocalBranchName = "";
        if (string.IsNullOrWhiteSpace(branch) || !IsRemote)
        {
            _localBranchName = "";
        }
        else
        {
            string remoteName = GitRefName.GetRemoteName(branch, _host.GetRemoteNames());
            _localBranchName = _host.GetLocalTrackingBranchName(remoteName, branch) ?? "";
            string remoteBranchName = remoteName.Length > 0 ? branch[(remoteName.Length + 1)..] : branch;
            newLocalBranchName = string.Concat(remoteName, "_", remoteBranchName);
            int i = 2;
            while (LocalBranchExists(newLocalBranchName))
            {
                newLocalBranchName = string.Concat(remoteName, "_", _localBranchName, "_", i.ToString());
                i++;
            }
        }

        ResetBranchText = LocalBranchExists(_localBranchName) ? Strings.ResetBranch.AccessKeyText : Strings.CreateBranch.AccessKeyText;
        LocalBranchNameText = "'" + _localBranchName + "'";
        CustomBranchName = newLocalBranchName;

        if (!string.IsNullOrWhiteSpace(branch))
        {
            _host.RequestCommitCount(branch, text =>
            {
                if (Branch == branch)
                {
                    CommitCountText = text;
                }
            });
        }
    }

    private bool LocalBranchExists(string name) => !_host.GetBranchObjectId(name, remote: false).IsZero;

    /// <summary>
    ///  Whether the checkout can be done without showing the dialog, as <c>FormCheckoutBranch.DoDefaultActionOrShow</c>.
    /// </summary>
    public bool CanCheckoutWithoutDialog
        => !_options.AlwaysShowDialog
            && !string.IsNullOrWhiteSpace(Branch) && !IsRemote
            && (!HasUncommittedChanges || _options.UseDefaultAction);

    /// <summary>
    ///  Normalises <see cref="CustomBranchName"/> if auto-normalisation is enabled; done when the name box loses focus.
    /// </summary>
    public void NormaliseCustomBranchName()
    {
        if (!_autoNormalise || !CustomBranchName.Any(PathUtil.IsValidPathChar))
        {
            return;
        }

        CustomBranchName = _branchNameNormaliser.Normalise(CustomBranchName, _branchNameOptions);
    }

    /// <summary>Checks out, as <c>FormCheckoutBranch.PerformCheckout</c>.</summary>
    /// <param name="isVisible">Whether the dialog is shown (the checkout without dialog uses the default local changes action).</param>
    public CheckoutOutcome PerformCheckout(bool isVisible)
    {
        string branchName = Branch.Trim();
        bool isRemote = IsRemote;
        string? newBranchName = null;
        CheckoutNewBranchMode newBranchMode = CheckoutNewBranchMode.DontCreate;

        if (isRemote)
        {
            if (IsCreateBranchWithCustomName)
            {
                NormaliseCustomBranchName();
                newBranchName = CustomBranchName.Trim();
                newBranchMode = CheckoutNewBranchMode.Create;
                if (string.IsNullOrWhiteSpace(newBranchName))
                {
                    _messageBoxes.ShowError(Strings.CustomBranchNameIsEmpty.Text, Strings.Title.PlainText);
                    return CheckoutOutcome.Failed;
                }

                if (!_host.IsValidBranchName(newBranchName))
                {
                    _messageBoxes.ShowError(string.Format(Strings.CustomBranchNameIsNotValid.Text, newBranchName), Strings.Title.PlainText);
                    return CheckoutOutcome.Failed;
                }
            }
            else if (IsResetBranch)
            {
                ObjectId localId = _host.GetBranchObjectId(_localBranchName, remote: false);
                ObjectId remoteId = _host.GetBranchObjectId(branchName, remote: true);
                if (!localId.IsZero && !remoteId.IsZero)
                {
                    ObjectId mergeBaseId = _host.GetMergeBase(localId, remoteId);
                    if (localId != mergeBaseId)
                    {
                        string mergeBaseText = mergeBaseId.IsZero ? "merge base" : mergeBaseId.ToShortString();
                        if (!_messageBoxes.Confirm(string.Format(Strings.ResetNonFastForwardBranch.Text, _localBranchName, mergeBaseText), Strings.ResetCaption.Text))
                        {
                            return CheckoutOutcome.Failed;
                        }
                    }
                }

                newBranchMode = CheckoutNewBranchMode.Reset;
                newBranchName = _localBranchName;
            }
        }

        LocalChangesAction localChanges = ChangesMode;
        if (localChanges != LocalChangesAction.Reset && SetAsDefault)
        {
            _host.SaveDefaultLocalChangesAction(localChanges);
        }

        if ((!isVisible && !_options.UseDefaultAction) || !HasUncommittedChanges)
        {
            localChanges = LocalChangesAction.DontChange;
        }

        bool stash = false;
        if (localChanges == LocalChangesAction.Stash)
        {
            if (_isDirtyDir is null && isVisible)
            {
                _isDirtyDir = _host.IsDirtyDir();
            }

            stash = _isDirtyDir == true;
            if (stash)
            {
                _host.StashSave();
            }
        }

        return _host.Checkout(branchName, isRemote, localChanges, newBranchMode, newBranchName, stash);
    }

    [RelayCommand]
    private void Checkout()
    {
        switch (PerformCheckout(isVisible: true))
        {
            case CheckoutOutcome.Succeeded:
                Close(accepted: true);
                break;
            case CheckoutOutcome.Cancelled:
                Close(accepted: false);
                break;
        }
    }
}
