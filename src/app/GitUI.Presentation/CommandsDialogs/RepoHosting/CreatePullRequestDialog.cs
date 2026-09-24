using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Plugins;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.SpellChecker;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.RepoHosting;

/// <summary>Strings of the create pull request dialog; ids match <c>CreatePullRequestForm</c> and <c>TranslatedStrings</c>.</summary>
public sealed class CreatePullRequestStrings : ViewStrings
{
    public CreatePullRequestStrings()
        : base("CreatePullRequestForm")
    {
        Title = Add("$this", "Text", "Create Pull Request");
        TargetRepository = Add("label3", "Text", "Target repository:");
        YourBranch = Add("label4", "Text", "Your branch:");
        TargetBranch = Add("label5", "Text", "Target branch:");
        PullRequestData = Add("groupBox1", "Text", "Pull request data");
        TitleLabel = Add("label1", "Text", "Title:");
        Body = Add("label2", "Text", "Body:");
        Create = Add("_createBtn", "Text", "Create");
        Loading = Add("_strLoading", "Text", "Loading...");
        YouMustSpecifyATitle = Add("_strYouMustSpecifyATitle", "Text", "You must specify a title.");
        PullRequest = Add("_strPullRequest", "Text", "Pull request");
        FailedToCreatePullRequest = Add("_strFailedToCreatePullRequest", "Text", "Failed to create pull request.");
        PleaseCloneGitHubRep = Add("_strPleaseCloneGitHubRep", "Text", "Please clone GitHub repository before pull request.");
        Done = Add("_strDone", "Text", "Done");
        RemoteFailToLoadBranches = Add("_strRemoteFailToLoadBranches", "Text", "Fail to load target branches");
        FailedToLoadTemplate = Add("_strFailedToLoadTemplate", "Text", "Failed to load PR template from file.");
        Error = Add("_error", "Text", "Error", category: "TranslatedStrings");
        RemoteInError = Add("_remoteInError", "Text", "{0}\n\nRemote: {1}", category: "TranslatedStrings");
    }

    public TranslatedText Title { get; }

    public TranslatedText TargetRepository { get; }

    public TranslatedText YourBranch { get; }

    public TranslatedText TargetBranch { get; }

    public TranslatedText PullRequestData { get; }

    public TranslatedText TitleLabel { get; }

    public TranslatedText Body { get; }

    public TranslatedText Create { get; }

    public TranslatedText Loading { get; }

    public TranslatedText YouMustSpecifyATitle { get; }

    public TranslatedText PullRequest { get; }

    public TranslatedText FailedToCreatePullRequest { get; }

    public TranslatedText PleaseCloneGitHubRep { get; }

    public TranslatedText Done { get; }

    public TranslatedText RemoteFailToLoadBranches { get; }

    public TranslatedText FailedToLoadTemplate { get; }

    public TranslatedText Error { get; }

    public TranslatedText RemoteInError { get; }
}

/// <summary>Operations of the create pull request dialog that need the repository.</summary>
public interface ICreatePullRequestHost
{
    /// <summary>The subject of the last commit of <paramref name="revision"/> (<c>GetPreviousCommitMessages</c>), if any.</summary>
    string? GetLastCommitSubject(string revision);

    /// <summary>
    ///  As <c>LoadPRTemplate</c>: the content of <c>.github/PULL_REQUEST_TEMPLATE.md</c>, or <see langword="null"/> without template;
    ///  throws if it cannot be read.
    /// </summary>
    Task<string?> LoadTemplateAsync();

    /// <summary>Reports that the template could not be read (as the <c>UserExternalOperationException</c> of the form).</summary>
    void ReportTemplateError(Exception exception);
}

/// <summary>A remote of the repository in the lists of the dialog (<c>DisplayMember = DisplayData</c>).</summary>
public sealed record HostedRemoteItem(IHostedRemote Remote)
{
    public override string ToString() => Remote.DisplayData;
}

/// <summary>View model of the create pull request dialog (port of <c>CreatePullRequestForm</c>).</summary>
public sealed partial class CreatePullRequestViewModel : DialogViewModel
{
    private readonly IRepositoryHostPlugin _repoHost;
    private readonly string? _chooseRemote;
    private readonly ICreatePullRequestHost _host;
    private readonly IBackgroundRunner _backgroundRunner;
    private readonly IMessageBoxService _messageBoxes;
    private IReadOnlyList<IHostedRemote> _hostedRemotes = [];
    private string? _previousTitle;

    /// <param name="chooseRemote">The remote to select as the target, if any.</param>
    /// <param name="spellCheckHost">The spell checking of the body (<c>EditNetSpell</c>), if any.</param>
    public CreatePullRequestViewModel(
        CreatePullRequestStrings strings,
        IRepositoryHostPlugin repoHost,
        string? chooseRemote,
        ICreatePullRequestHost host,
        IBackgroundRunner backgroundRunner,
        IMessageBoxService messageBoxes,
        ISpellCheckHost? spellCheckHost = null)
    {
        Strings = strings;
        _repoHost = repoHost;
        _chooseRemote = chooseRemote;
        _host = host;
        _backgroundRunner = backgroundRunner;
        _messageBoxes = messageBoxes;
        SpellCheck = spellCheckHost is null ? null : new SpellCheckViewModel(ViewStrings.Load<SpellCheckStrings>(), spellCheckHost);
        _previousTitle = PullRequestTitle;
    }

    public CreatePullRequestStrings Strings { get; }

    /// <summary>The body (<c>_bodyTB</c>, an <c>EditNetSpell</c>).</summary>
    public TextEditorViewModel Body { get; } = new() { ShowLineNumbers = false };

    public SpellCheckViewModel? SpellCheck { get; }

    public ObservableCollection<HostedRemoteItem> TargetRemotes { get; } = [];

    public ObservableCollection<string> YourBranches { get; } = [];

    public ObservableCollection<string> TargetBranches { get; } = [];

    [ObservableProperty]
    public partial HostedRemoteItem? SelectedTargetRemote { get; set; }

    [ObservableProperty]
    public partial string? SelectedYourBranch { get; set; }

    [ObservableProperty]
    public partial string? SelectedTargetBranch { get; set; }

    [ObservableProperty]
    public partial string PullRequestTitle { get; set; } = "";

    /// <summary>Whether the remotes are being loaded (the WinForms form was masked).</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateCommand))]
    public partial bool CanCreate { get; private set; }

    private IHostedRemote? MyRemote => _hostedRemotes.FirstOrDefault(r => r.IsOwnedByMe);

    /// <summary>As <c>CreatePullRequestForm_Load</c>.</summary>
    public async Task LoadAsync()
    {
        CanCreate = false;
        _hostedRemotes = _repoHost.GetHostedRemotesForModule();
        IsLoading = true;
        IHostedRemote[] foreignHostedRemotes = await _backgroundRunner.RunAsync(() => _hostedRemotes.Where(r => !r.IsOwnedByMe).ToArray());
        if (foreignHostedRemotes.Length == 0)
        {
            _messageBoxes.ShowError(Strings.FailedToCreatePullRequest.Text + Environment.NewLine + Strings.PleaseCloneGitHubRep.Text, "");
            Close(accepted: false);
            return;
        }

        IsLoading = false;
        LoadRemotes(foreignHostedRemotes);
        await Task.WhenAll(LoadMyBranchesAsync(), LoadTemplateAsync());
    }

    /// <summary>As <c>_pullReqTargetsCB_SelectedIndexChanged</c>.</summary>
    partial void OnSelectedTargetRemoteChanged(HostedRemoteItem? value)
    {
        TargetBranches.Clear();
        if (value is not null)
        {
            _ = PopulateBranchesAsync(value.Remote, TargetBranches, branch => SelectedTargetBranch = branch);
        }
    }

    partial void OnSelectedYourBranchChanged(string? value) => SuggestTitle();

    partial void OnSelectedTargetBranchChanged(string? value) => SuggestTitle();

    /// <summary>As <c>LoadRemotes</c>.</summary>
    private void LoadRemotes(IHostedRemote[] foreignHostedRemotes)
    {
        TargetRemotes.Clear();
        foreach (IHostedRemote remote in foreignHostedRemotes)
        {
            TargetRemotes.Add(new HostedRemoteItem(remote));
        }

        SelectedTargetRemote = _chooseRemote is not null
            ? TargetRemotes.FirstOrDefault(item => item.Remote.Name == _chooseRemote)
            : TargetRemotes.FirstOrDefault();
    }

    /// <summary>As <c>LoadMyBranches</c>.</summary>
    private Task LoadMyBranchesAsync()
    {
        YourBranches.Clear();
        return MyRemote is { } myRemote
            ? PopulateBranchesAsync(myRemote, YourBranches, branch => SelectedYourBranch = branch)
            : Task.CompletedTask;
    }

    /// <summary>As <c>LoadPRTemplate</c>.</summary>
    private async Task LoadTemplateAsync()
    {
        try
        {
            if (await _host.LoadTemplateAsync() is { } template)
            {
                Body.Text = template;
            }
        }
        catch (Exception ex)
        {
            _host.ReportTemplateError(ex);
        }
    }

    /// <summary>As <c>PopulateBranchesComboAndEnableCreateButton</c>: the branches, the default branch selected.</summary>
    private async Task PopulateBranchesAsync(IHostedRemote remote, ObservableCollection<string> branches, Action<string> select)
    {
        try
        {
            (IReadOnlyList<IHostedBranch> hostedBranches, string defaultBranch) = await _backgroundRunner.RunAsync(() =>
            {
                IHostedRepository hostedRepository = remote.GetHostedRepository();
                return (hostedRepository.GetBranches(), hostedRepository.GetDefaultBranch());
            });

            branches.Clear();
            foreach (IHostedBranch branch in hostedBranches)
            {
                branches.Add(branch.Name);
            }

            if (branches.Count > 0)
            {
                select(branches.Contains(defaultBranch) ? defaultBranch : branches[0]);
            }

            CanCreate = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _messageBoxes.ShowError(string.Format(Strings.RemoteInError.Text, ex.Message, remote.DisplayData), Strings.RemoteFailToLoadBranches.Text);
        }
    }

    /// <summary>As <c>_yourBranchCB_SelectedIndexChanged</c>: the subject of the last commit of the branch, while the title was not edited.</summary>
    private void SuggestTitle()
    {
        if (_previousTitle == PullRequestTitle && !string.IsNullOrWhiteSpace(SelectedYourBranch) && MyRemote is { } myRemote)
        {
            string? lastMessage = _host.GetLastCommitSubject($"{myRemote.Name}/{SelectedYourBranch}");
            PullRequestTitle = lastMessage is null ? "" : lastMessage.Split('\n')[0];
            _previousTitle = PullRequestTitle;
        }
    }

    /// <summary>As <c>_createBtn_Click</c>.</summary>
    [RelayCommand(CanExecute = nameof(CanCreate))]
    private void Create()
    {
        if (SelectedTargetRemote is not { } target)
        {
            return;
        }

        string title = PullRequestTitle.Trim();
        string body = Body.Text.Trim();
        if (title.Length == 0)
        {
            _messageBoxes.ShowError(Strings.YouMustSpecifyATitle.Text, Strings.Error.Text);
            return;
        }

        try
        {
            IHostedRepository hostedRepository = target.Remote.GetHostedRepository();
            hostedRepository.CreatePullRequest(SelectedYourBranch ?? "", SelectedTargetBranch ?? "", title, body);
            _messageBoxes.ShowInformation(Strings.Done.Text, Strings.PullRequest.Text);
            Close(accepted: true);
        }
        catch (Exception ex)
        {
            _messageBoxes.ShowError(Strings.FailedToCreatePullRequest.Text + Environment.NewLine + ex.Message, Strings.Error.Text);
        }
    }
}
