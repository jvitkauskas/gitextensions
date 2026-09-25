using System.Net;
using System.Text;
using GitCommands;
using GitCommands.ExternalLinks;
using GitCommands.Git;
using GitCommands.Remotes;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI;
using GitUI.Avalonia.Hosting;
using GitUI.CommitInfo;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;
using ResourceManager.CommitDataRenders;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The commit info for the Avalonia view: a port of the loading of the WinForms <c>CommitInfo</c> (<c>ReloadCommitInfo</c>,
///  <c>UpdateRevisionInfo</c>) and <c>CommitInfoHeader</c>, with their renderers (docs/avalonia-port/PLAN.md, phase 5); keep it
///  in sync.
/// </summary>
internal sealed class CommitInfoHost : ICommitInfoHost
{
    private readonly IGitUICommands _commands;
    private readonly CommitInfoStrings _strings = ViewStrings.Load<CommitInfoStrings>();

    public CommitInfoStrings Strings => _strings;
    private readonly ILinkFactory _linkFactory;
    private readonly ICommitDataManager _commitDataManager;
    private readonly ICommitDataHeaderRenderer _commitDataHeaderRenderer;
    private readonly ICommitDataBodyRenderer _commitDataBodyRenderer;
    private readonly RefsFormatter _refsFormatter;
    private readonly IGitRevisionExternalLinksParser _gitRevisionExternalLinksParser;
    private readonly GitDescribeProvider _gitDescribeProvider;
    private IDictionary<string, int>? _tagsOrderDict;

    /// <param name="linkFactory">The link factory of the body and the refs (the service of the commands by default, as <c>CommitInfo</c>).</param>
    public CommitInfoHost(IGitUICommands commands, ILinkFactory? linkFactory = null)
    {
        _commands = commands;
        _linkFactory = linkFactory ?? commands.GetRequiredService<ILinkFactory>();
        _commitDataManager = new CommitDataManager(() => Module);

        // As CommitInfoHeader, which creates its own link factory.
        _commitDataHeaderRenderer = new CommitDataHeaderRenderer(new TabbedHeaderLabelFormatter(), new DateFormatter(), new TabbedHeaderRenderStyleProvider(), new LinkFactory());
        _commitDataBodyRenderer = new CommitDataBodyRenderer(() => Module, _linkFactory);
        _refsFormatter = new RefsFormatter(_linkFactory);
        _gitRevisionExternalLinksParser = new GitRevisionExternalLinksParser(
            new ConfiguredLinkDefinitionsProvider(new ExternalLinksStorage()),
            new ExternalLinkRevisionParser(new ConfigFileRemoteSettingsManager(() => Module)));
        _gitDescribeProvider = new GitDescribeProvider(() => Module);
    }

    private IGitModule Module => _commands.Module;

    /// <summary>As <c>CommitInfoHeader.ShowCommitInfo</c> and <c>GetFixCommitMessage</c>.</summary>
    public CommitInfoContent Render(GitRevision revision, IReadOnlyList<ObjectId>? children, bool showRevisionsAsLinks)
    {
        CommitData data = _commitDataManager.CreateFromRevision(revision, children);
        string header = _commitDataHeaderRenderer.Render(data, showRevisionsAsLinks);

        // The labels are followed by tabs (TabbedHeaderLabelFormatter), which the view aligns as the tab stops of the RichTextBox.
        List<CommitInfoHeaderLine> lines = [];
        foreach (string line in header.Split('\n').Select(l => l.TrimEnd('\r')))
        {
            int tab = line.IndexOf('\t');
            lines.Add(tab < 0 ? new CommitInfoHeaderLine("", line) : new CommitInfoHeaderLine(line[..tab], line[tab..].TrimStart('\t')));
        }

        return new CommitInfoContent(lines, _commitDataBodyRenderer.Render(data, showRevisionsAsLinks: false));
    }

    /// <summary>As <c>UpdateCommitMessageAsync</c>.</summary>
    public async Task<string> LoadMessageAsync(GitRevision revision, IReadOnlyList<ObjectId>? children, bool showRevisionsAsLinks, CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;
        cancellationToken.ThrowIfCancellationRequested();
        if (revision.Body is null || (revision.Notes is null && (AppSettings.ShowGitNotesColumn.Value || AppSettings.ShowGitNotes)))
        {
            _commitDataManager.UpdateBodyAndNotes(revision);
        }

        CommitData data = _commitDataManager.CreateFromRevision(revision, children);
        string message = _commitDataBodyRenderer.Render(data, showRevisionsAsLinks);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return message;
    }

    /// <summary>As <c>StartAsyncDataLoad</c> and <c>UpdateRevisionInfo</c>.</summary>
    public async Task<string> LoadRevisionInfoAsync(GitRevision revision, bool showBranchesAsLinks, IReadOnlySet<string> showAll, CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;
        cancellationToken.ThrowIfCancellationRequested();

        // As LoadSortedTagsAsync (once).
        _tagsOrderDict ??= GetSortedTags();

        string linksInfo = Module.GetEffectiveSettings() is DistributedSettings settings ? GetLinksForRevision(revision, settings, cancellationToken) : "";
        string annotatedTagsInfo = "";
        string branchInfo = "";
        string tagInfo = "";
        string gitDescribeInfo = "";

        if (AppSettings.ShowAnnotatedTagsMessages && GetAnnotatedTagsMessages(revision.Refs, cancellationToken) is { Count: > 0 } annotatedTagsMessages)
        {
            // having both lightweight & annotated tags in thisRevisionTagNames, but GetAnnotatedTagsInfo will process annotated only:
            List<string> thisRevisionTagNames = [.. revision.Refs.Where(r => r.IsTag).Select(r => r.LocalName)];
            thisRevisionTagNames.Sort(new GitUI.CommitInfo.TagsComparer(_tagsOrderDict));
            annotatedTagsInfo = GetAnnotatedTagsInfo(thisRevisionTagNames, annotatedTagsMessages);
        }

        if (AppSettings.CommitInfoShowContainedInBranches)
        {
            // Include local branches if explicitly requested or when needed to decide whether to show remotes
            bool getLocal = AppSettings.CommitInfoShowContainedInBranchesLocal || AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;
            bool getRemote = AppSettings.CommitInfoShowContainedInBranchesRemote || AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;
            string[] branches = [.. Module.GetAllBranchesWhichContainGivenCommit(revision.ObjectId, getLocal, getRemote, cancellationToken)];
            Array.Sort(branches, new GitUI.CommitInfo.BranchComparer(branches, Module.GetSelectedBranch()));
            branchInfo = _refsFormatter.FormatBranches(branches, showBranchesAsLinks, limit: !showAll.Contains("branches"));
        }

        if (AppSettings.CommitInfoShowContainedInTags)
        {
            string[] tags = [.. Module.GetAllTagsWhichContainGivenCommit(revision.ObjectId, cancellationToken)];
            Array.Sort(tags, new GitUI.CommitInfo.TagsComparer(_tagsOrderDict));
            tagInfo = _refsFormatter.FormatTags(tags, showBranchesAsLinks, limit: !showAll.Contains("tags"));
        }

        if (AppSettings.CommitInfoShowTagThisCommitDerivesFrom)
        {
            gitDescribeInfo = GetDescribeInfoForRevision(revision.ObjectId, showBranchesAsLinks, cancellationToken);
        }

        string body = string.Join(Environment.NewLine + Environment.NewLine,
            new[] { annotatedTagsInfo, linksInfo, branchInfo, tagInfo, gitDescribeInfo }.Where(s => !string.IsNullOrEmpty(s)));

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return body;
    }

    public void ExecuteLink(string uri, Action<string, string?>? internalCommand, Action<string?> showAll)
    {
        try
        {
            _linkFactory.ExecuteLink(uri, internalCommand is null ? null : args => internalCommand(args.Command, args.Data), showAll);
        }
        catch (Exception ex)
        {
            AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(null, ex.Message, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    /// <summary>As <c>CommitInfo.GetSortedTags</c> (a broken ref gives no order rather than the message box).</summary>
    public CommitInfoDisplayOptions Options
    {
        get => new(
            AppSettings.CommitInfoShowContainedInBranchesLocal,
            AppSettings.CommitInfoShowContainedInBranchesRemote,
            AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal,
            AppSettings.CommitInfoShowContainedInTags,
            AppSettings.ShowAnnotatedTagsMessages,
            AppSettings.CommitInfoShowTagThisCommitDerivesFrom);
        set
        {
            AppSettings.CommitInfoShowContainedInBranchesLocal = value.ShowContainedInBranchesLocal;
            AppSettings.CommitInfoShowContainedInBranchesRemote = value.ShowContainedInBranchesRemote;
            AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal = value.ShowContainedInBranchesRemoteIfNoLocal;
            AppSettings.CommitInfoShowContainedInTags = value.ShowContainedInTags;
            AppSettings.ShowAnnotatedTagsMessages = value.ShowAnnotatedTagsMessages;
            AppSettings.CommitInfoShowTagThisCommitDerivesFrom = value.ShowTagThisCommitDerivesFrom;
        }
    }

    public bool ShowAvatar => AppSettings.ShowAuthorAvatarInCommitInfo;

    public int AvatarSize => AppSettings.AuthorImageSizeInCommitInfo;

    public bool HasAvatarMenu => true;

    public AvatarProvider AvatarProvider
    {
        get => AppSettings.AvatarProvider;
        set => AppSettings.AvatarProvider = value;
    }

    public AvatarFallbackType AvatarFallbackType
    {
        get => AppSettings.AvatarFallbackType;
        set => AppSettings.AvatarFallbackType = value;
    }

    // As AvatarControl.ClearCache: the provider of the settings, then its cache cleared.
    public async Task ClearAvatarCacheAsync()
    {
        GitUI.Avatars.AvatarService.UpdateAvatarProvider();
        await GitUI.Avatars.AvatarService.CacheCleaner.ClearCacheAsync();
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
    }

    public void OpenUrl(string url) => OsShellUtil.OpenUrlInDefaultBrowser(url);

    private EventHandler? _avatarsCleared;

    // As AvatarControl.OnCacheCleared (the cache may be cleared by another control, e.g. the settings dialog).
    public event EventHandler? AvatarsCleared
    {
        add
        {
            if (_avatarsCleared is null)
            {
                GitUI.Avatars.AvatarService.CacheCleaner.CacheCleared += OnAvatarCacheCleared;
            }

            _avatarsCleared += value;
        }

        remove
        {
            _avatarsCleared -= value;
            if (_avatarsCleared is null)
            {
                GitUI.Avatars.AvatarService.CacheCleaner.CacheCleared -= OnAvatarCacheCleared;
            }
        }
    }

    private void OnAvatarCacheCleared(object? sender, EventArgs e)
        => ThreadHelper.FileAndForget(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _avatarsCleared?.Invoke(this, EventArgs.Empty);
        });

    /// <summary>As <c>AvatarControl.UpdateAvatarAsync</c>: the avatar of the provider, or the default image.</summary>
    public async Task<byte[]?> GetAvatarAsync(string? email, string? name, CancellationToken cancellationToken)
    {
        byte[]? image = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            int size = DpiUtil.Scale(AvatarSize);
            image = await GitUI.Avatars.AvatarService.DefaultProvider.GetAvatarAsync(email, name, size);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return image ?? GitUI.Avatars.AvatarService.UserImage;
    }

    public void EditNotes(ObjectId objectId) => Module.EditNotes(objectId);

    /// <summary>As <c>copyCommitInfoToolStripMenuItem_Click</c> (with <c>CommitInfoHeader.GetPlainText</c>).</summary>
    public string GetCopyText(string header, string message)
        => $"{_commitDataHeaderRenderer.GetPlainText(header)}{Environment.NewLine}{Environment.NewLine}{message}";

    public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);

    private Dictionary<string, int> GetSortedTags()
    {
        GitArgumentBuilder args = new("for-each-ref")
        {
            @"--sort=""-taggerdate""",
            @"--format=""%(refname)""",
            "refs/tags/"
        };

        string tree = Module.GitExecutable.GetOutput(args);
        Dictionary<string, int> dict = [];
        if (tree.Contains("warning:"))
        {
            return dict;
        }

        int i = 0;
        foreach (string entry in tree.LazySplit('\n'))
        {
            if (dict.TryAdd(entry, i))
            {
                ++i;
            }
        }

        return dict;
    }

    private string GetLinksForRevision(GitRevision revision, DistributedSettings settings, CancellationToken cancellationToken)
    {
        IEnumerable<ExternalLink> links = _gitRevisionExternalLinksParser.Parse(revision, settings);
        cancellationToken.ThrowIfCancellationRequested();
        string result = string.Join(", ", links.Distinct().Select(link => _linkFactory.CreateLink(link.Caption, link.Uri)));
        return string.IsNullOrEmpty(result) ? "" : $"{WebUtility.HtmlEncode(_strings.LinksRelatedToRevision.Text)} {result}";
    }

    private Dictionary<string, string>? GetAnnotatedTagsMessages(IReadOnlyList<IGitRef>? refs, CancellationToken cancellationToken)
    {
        if (refs is null)
        {
            return null;
        }

        // Annotated tags are the dereferencing refs of the revision (see the note in CommitInfo.LoadAnnotatedTagInfoAsync).
        Dictionary<string, string> result = [];
        foreach (IGitRef gitRef in refs)
        {
            if (gitRef is { IsTag: true, IsDereference: true }
                && WebUtility.HtmlEncode(Module.GetTagMessage(gitRef.LocalName, cancellationToken)) is string content)
            {
                result.Add(gitRef.LocalName, content);
            }
        }

        return result;
    }

    private static string GetAnnotatedTagsInfo(IEnumerable<string> tagNames, IDictionary<string, string> annotatedTagsMessages)
    {
        StringBuilder result = new();
        foreach (string tag in tagNames)
        {
            if (annotatedTagsMessages.TryGetValue(tag, out string? annotatedContents))
            {
                result.Append("<u>").Append(tag).Append("</u>: ").Append(annotatedContents).AppendLine();
            }
        }

        return result.ToString().TrimEnd();
    }

    private string GetDescribeInfoForRevision(ObjectId commitId, bool showBranchesAsLinks, CancellationToken cancellationToken)
    {
        (string precedingTag, string commitCount) = _gitDescribeProvider.Get(commitId, cancellationToken);

        StringBuilder gitDescribeInfo = new();
        if (!string.IsNullOrEmpty(precedingTag))
        {
            string tagString = showBranchesAsLinks ? _linkFactory.CreateTagLink(precedingTag) : WebUtility.HtmlEncode(precedingTag);
            gitDescribeInfo.Append(WebUtility.HtmlEncode(_strings.DerivesFromTag.Text)).Append(' ').Append(tagString);
            if (!string.IsNullOrEmpty(commitCount))
            {
                gitDescribeInfo.Append(" + ").Append(commitCount).Append(' ').Append(WebUtility.HtmlEncode(_strings.PlusCommits.Text));
            }
        }
        else
        {
            gitDescribeInfo.Append(WebUtility.HtmlEncode(_strings.DerivesFromNoTag.Text));
        }

        return gitDescribeInfo.ToString();
    }
}
