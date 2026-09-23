using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>Strings of the commit diff dialog; ids match <c>FormCommitDiff</c>.</summary>
public sealed class CommitDiffStrings : ViewStrings
{
    public CommitDiffStrings()
        : base("FormCommitDiff")
    {
        Title = Add("$this", "Text", "Diff");
    }

    public TranslatedText Title { get; }
}

/// <summary>Operations of the commit diff that need the host (git).</summary>
public interface ICommitDiffHost
{
    /// <summary>
    ///  The revision with its real parents (as <c>CommitDiff.SetRevision</c>: not the revision of a filtered grid), or
    ///  <see langword="null"/> if it cannot be read.
    /// </summary>
    GitRevision? GetRevision(ObjectId objectId);

    /// <summary>The files of the revision compared to its parents (as <c>FileStatusList.SetDiffs</c>).</summary>
    Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(GitRevision revision, CancellationToken cancellationToken);

    /// <summary>The working directory, shown in the title.</summary>
    string WorkingDirectory { get; }
}

/// <summary>View model of the commit diff dialog (port of <c>FormCommitDiff</c> and the <c>CommitDiff</c> control).</summary>
public sealed partial class CommitDiffViewModel : DialogViewModel
{
    private readonly ICommitDiffHost _host;
    private readonly ObjectId _objectId;
    private readonly string? _fileToSelect;
    private CancellationTokenSource? _loading;

    public CommitDiffViewModel(
        CommitDiffStrings strings,
        ICommitDiffHost host,
        IFileViewerHost fileViewerHost,
        ICommitInfoHost commitInfoHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        ObjectId objectId,
        string? fileToSelect = null)
    {
        Strings = strings;
        _host = host;
        _objectId = objectId;
        _fileToSelect = fileToSelect;
        Title = strings.Title.PlainText;
        CommitInfo = new CommitInfoViewModel(commitInfoHost);
        Files = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions);
        Viewer = new FileViewerViewModel(fileViewerHost);
        Files.SelectionChanged += (_, _) => _ = Viewer.ShowChangesAsync(Files.SelectedEntry);
    }

    public CommitDiffStrings Strings { get; }

    public CommitInfoViewModel CommitInfo { get; }

    public FileStatusListViewModel Files { get; }

    public FileViewerViewModel Viewer { get; }

    /// <summary>As <c>CommitDiff.Text</c>: the commit, its date, its author and the repository.</summary>
    [ObservableProperty]
    public partial string Title { get; private set; }

    /// <summary>Shows the commit of the dialog.</summary>
    public Task InitializeAsync() => SetRevisionAsync(_objectId, _fileToSelect);

    /// <summary>As <c>CommitDiff.SetRevision</c>: shows the commit, with <paramref name="fileToSelect"/> selected if it changed.</summary>
    public async Task SetRevisionAsync(ObjectId objectId, string? fileToSelect)
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loading?.Cancel();
#pragma warning restore VSTHRD103
        _loading = new CancellationTokenSource();
        CancellationToken cancellationToken = _loading.Token;
        if (_host.GetRevision(objectId) is not { } revision)
        {
            CommitInfo.SetRevision(null);
            Files.SetGroups([]);
            return;
        }

        Title = $"{Strings.Title.PlainText} - {revision.ObjectId.ToShortString()} - {revision.AuthorDate} - {revision.Author} - {_host.WorkingDirectory}";
        CommitInfo.SetRevision(revision);
        Files.SetLoading();
        IReadOnlyList<FileStatusGroup> groups;
        try
        {
            groups = await _host.GetDiffsAsync(revision, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Files.SetGroups(groups);
        if (fileToSelect is not null && Files.AllEntries.Any(entry => entry.Item.Name == fileToSelect))
        {
            Files.Select(entry => entry.Item.Name == fileToSelect);
        }
    }
}
