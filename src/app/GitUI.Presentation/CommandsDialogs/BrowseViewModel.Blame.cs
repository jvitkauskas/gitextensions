using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.Blame;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>What the blame of the diff and file tree tabs needs from the application.</summary>
public interface IBrowseBlameHost
{
    /// <summary>A blame without its commit info (<c>BlameControl.HideCommitInfo</c>).</summary>
    BlameViewModel CreateBlame();

    /// <summary>Whether the blame of the diff tab is shown in the tab (<c>UseDiffViewerForBlame</c>), else in the file tree tab.</summary>
    bool UseDiffViewerForBlame { get; }

    /// <summary>The revision with its real parents (a filtered grid rewrites them).</summary>
    GitRevision GetActualRevision(GitRevision revision);

    /// <summary>The revision from git, if the grid does not list it.</summary>
    GitRevision? GetRevision(ObjectId objectId);

    /// <summary>The checked out commit, blamed for the files of the artificial commits.</summary>
    ObjectId? GetCurrentCheckout();
}

/// <summary>
///  The blame of the diff and file tree tabs (the <c>BlameControl</c> of <c>RevisionDiffControl</c>): "Blame" of the menu of
///  the files shows it instead of the viewer.
/// </summary>
public sealed partial class BrowseViewModel
{
    private IBrowseBlameHost? _blameHost;

    // As _selectedBlameItem: the file whose blame is shown in the diff tab; selecting another file shows the diff again.
    private string? _selectedBlameItem;

    // The line of the file to show once the file tree shows the blame (_lastExplicitlySelectedItemLine).
    private int? _pendingTreeLine;

    /// <summary>The blame of the diff tab, if the application has one.</summary>
    public BlameViewModel? DiffBlame { get; private set; }

    /// <summary>The blame of the file tree tab, if the application has one.</summary>
    public BlameViewModel? TreeBlame { get; private set; }

    /// <summary>Whether the diff tab shows the blame instead of the viewer.</summary>
    [ObservableProperty]
    public partial bool IsDiffBlameVisible { get; private set; }

    /// <summary>Whether the file tree tab shows the blame instead of the viewer.</summary>
    [ObservableProperty]
    public partial bool IsTreeBlameVisible { get; private set; }

    private void InitializeBlame()
    {
        _blameHost = _host as IBrowseBlameHost;
        if (_blameHost is null)
        {
            return;
        }

        DiffBlame = _blameHost.CreateBlame();
        DiffBlame.RevisionGrid = new BlameGrid(this);
        Files.BlameAction = () => BlameFile(Files, DiffBlame, Viewer, fileTree: false);
        if (FileTree is not null)
        {
            TreeBlame = _blameHost.CreateBlame();
            TreeBlame.RevisionGrid = new BlameGrid(this);
            FileTree.BlameAction = () => BlameFile(FileTree, TreeBlame, TreeViewer!, fileTree: true);
        }
    }

    // As RevisionDiffControl.BlameFile: the blame toggled in the tab (the file tree, or the diff tab with UseDiffViewerForBlame),
    // else shown in the file tree tab.
    private void BlameFile(FileStatusListViewModel files, BlameViewModel blame, FileViewerViewModel viewer, bool fileTree)
    {
        if (files.SelectedEntry is not { } entry || !entry.Item.IsTracked)
        {
            return;
        }

        int? line = files.IsBlameShown ? blame.CurrentLine : viewer.Editor.GetCurrentFilePosition()?.Line;
        if (fileTree || _blameHost!.UseDiffViewerForBlame)
        {
            files.IsBlameShown = !files.IsBlameShown;
            if (fileTree)
            {
                _pendingTreeLine = line;
                ShowTreeFile();
            }
            else
            {
                _selectedBlameItem = files.IsBlameShown ? entry.Item.Name : null;
                ShowDiffFile(line);
            }

            return;
        }

        // As OpenInFileTreeTab(requestBlame: true).
        files.IsBlameShown = false;
        if (FileTree is null)
        {
            return;
        }

        FileTree.IsBlameShown = true;
        _pendingTreeLine = line;
        bool alreadySelected = FileTree.SelectedEntry?.Item.Name == entry.Item.Name;
        ShowInFileTree(files);
        if (alreadySelected)
        {
            ShowTreeFile();
        }
    }

    // As ShowSelectedFile of the diff tab: the blame of the file if requested, else its diff.
    private void ShowDiffFile(int? line = null)
    {
        FileStatusEntry? entry = Files.SelectedEntry;

        // As DiffFiles_SelectedIndexChanged: the diff again when another file is selected (not in the file tree).
        if (Files.IsBlameShown && entry?.Item.Name != _selectedBlameItem)
        {
            Files.IsBlameShown = false;
            _selectedBlameItem = null;
        }

        if (Files.IsBlameShown && DiffBlame is not null && entry is not null && Files.SelectedFolder is null)
        {
            IsDiffBlameVisible = true;
            _ = LoadBlameAsync(DiffBlame, entry, line);
            return;
        }

        IsDiffBlameVisible = false;
        _ = Viewer.ShowChangesAsync(entry);
    }

    // As ShowSelectedFileBlameAsync: the files of the artificial commits are blamed in the checked out commit.
    private async Task LoadBlameAsync(BlameViewModel blame, FileStatusEntry entry, int? line)
    {
        GitRevision? revision = entry.SecondRevision;
        if (revision.IsArtificial)
        {
            revision = _blameHost!.GetCurrentCheckout() is { } head
                ? Grid.GetRevision(head) is { } listed ? _blameHost.GetActualRevision(listed) : _blameHost.GetRevision(head)
                : null;
        }

        if (revision is not null)
        {
            await blame.LoadAsync(revision, children: null, entry.Item.Name, line);
        }
    }

    /// <summary>The grid of the blame of the main window: blaming a revision selects it.</summary>
    private sealed class BlameGrid(BrowseViewModel owner) : IBlameRevisionGrid
    {
        public GitRevision? GetRevision(ObjectId objectId) => owner.Grid.GetRevision(objectId);

        public GitRevision GetActualRevision(GitRevision revision) => owner._blameHost!.GetActualRevision(revision);

        public GitRevision? GetActualRevision(ObjectId objectId)
            => GetRevision(objectId) is { } revision ? GetActualRevision(revision) : owner._blameHost!.GetRevision(objectId);

        public bool SelectFileInRevision(ObjectId objectId, string fileName) => owner.Grid.SelectRevision(objectId);
    }
}
