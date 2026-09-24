using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>
///  Strings of the log window (the <c>viewdiff</c> verb); the category is <c>FormDiff</c>, the <c>Name</c> of <c>FormLog</c>
///  under which its strings are translated.
/// </summary>
public sealed class LogStrings : ViewStrings
{
    public LogStrings()
        : base("FormDiff")
    {
        Title = Add("$this", "Text", "Diff");
    }

    public TranslatedText Title { get; }
}

/// <summary>Operations of the log window that need the host (git and the other dialogs).</summary>
public interface ILogHost
{
    /// <summary>The files of the selected revisions (as <c>FileStatusList.SetDiffs</c>).</summary>
    Task<IReadOnlyList<FileStatusGroup>> GetDiffsAsync(IReadOnlyList<GitRevision> revisions, CancellationToken cancellationToken);

    /// <summary>As <c>RevisionGridControl.ViewSelectedRevisions</c> (a double click on a revision): the commit diff, modeless.</summary>
    void ViewRevisions(IReadOnlyList<GitRevision> revisions);
}

/// <summary>
///  View model of the log window (port of <c>FormLog</c>, the <c>viewdiff</c> verb): the revision grid, the files of the
///  selected revisions and the diff of the selected file.
/// </summary>
public sealed partial class LogViewModel : DialogViewModel, IDisposable
{
    private readonly ILogHost _host;
    private readonly ObjectId? _initialRevision;
    private CancellationTokenSource? _loadingDiffs;

    public LogViewModel(
        LogStrings strings,
        ILogHost host,
        RevisionGridViewModel grid,
        IFileViewerHost fileViewerHost,
        FileStatusListStrings fileStatusListStrings,
        FileStatusTreeOptions fileStatusTreeOptions,
        ObjectId? initialRevision = null)
    {
        Strings = strings;
        _host = host;
        _initialRevision = initialRevision;
        Grid = grid;
        Files = new FileStatusListViewModel(fileStatusListStrings, fileStatusTreeOptions);
        Viewer = new FileViewerViewModel(fileViewerHost);

        // As RevisionGridSelectionChanged and DiffFilesSelectedIndexChanged.
        Grid.SelectionChanged += OnGridSelectionChanged;
        Files.SelectionChanged += OnFilesSelectionChanged;

        // As the DoubleClick of FileStatusList without a handler: the history of the file.
        Files.SelectionActivated += OnFilesActivated;
    }

    public LogStrings Strings { get; }

    public RevisionGridViewModel Grid { get; }

    public FileStatusListViewModel Files { get; }

    public FileViewerViewModel Viewer { get; }

    /// <summary>As <c>FormDiffLoad</c>: loads the revisions, selecting the initial one (the current checkout, as the grid does).</summary>
    public void Initialize() => Grid.Load(_initialRevision);

    /// <summary>As <c>RevisionGridControl.ViewSelectedRevisions</c>.</summary>
    public void ViewSelectedRevisions()
    {
        IReadOnlyList<GitRevision> revisions = Grid.GetSelectedRevisionsLatestSelectedFirst();
        if (revisions.Count > 0 && !revisions[0].IsArtificial)
        {
            _host.ViewRevisions(revisions);
        }
    }

    /// <summary>Shows the files of the selected revisions (as <c>FileStatusList.SetDiffs(RevisionGrid.GetSelectedRevisions())</c>).</summary>
    public async Task ShowSelectedRevisionsAsync()
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingDiffs?.Cancel();
#pragma warning restore VSTHRD103
        _loadingDiffs = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadingDiffs.Token;

        IReadOnlyList<GitRevision> revisions = Grid.GetSelectedRevisionsLatestSelectedFirst();
        if (revisions.Count == 0)
        {
            Files.Clear();
            return;
        }

        Files.SetLoading();
        IReadOnlyList<FileStatusGroup> groups;
        try
        {
            groups = await _host.GetDiffsAsync(revisions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            Files.SetGroups(groups);
        }
    }

    public void Dispose()
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _loadingDiffs?.Cancel();
#pragma warning restore VSTHRD103
        Grid.SelectionChanged -= OnGridSelectionChanged;
        Files.SelectionChanged -= OnFilesSelectionChanged;
        Files.SelectionActivated -= OnFilesActivated;
        Grid.Dispose();
    }

    private void OnGridSelectionChanged(object? sender, EventArgs e) => _ = ShowSelectedRevisionsAsync();

    private void OnFilesSelectionChanged(object? sender, EventArgs e) => _ = Viewer.ShowChangesAsync(Files.SelectedEntry);

    private void OnFilesActivated(object? sender, EventArgs e)
    {
        if (Files.ShowFileHistoryCommand.CanExecute(false))
        {
            Files.ShowFileHistoryCommand.Execute(false);
        }
    }
}
