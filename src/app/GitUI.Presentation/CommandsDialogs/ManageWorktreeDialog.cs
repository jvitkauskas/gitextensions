using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the worktree list; ids match <c>FormManageWorktree</c>.</summary>
public sealed class ManageWorktreeStrings : ViewStrings
{
    public ManageWorktreeStrings()
        : base("FormManageWorktree")
    {
        Title = Add("$this", "Text", "Existing worktrees");
        Path = Add("Path", "HeaderText", "Path");
        Type = Add("Type", "HeaderText", "Type");
        Branch = Add("Branch", "HeaderText", "Branch");
        Sha1 = Add("Sha1", "HeaderText", "SHA-1");
        Create = Add("buttonCreateNewWorktree", "Text", "&Create...");
        Delete = Add("buttonDeleteSelectedWorktree", "Text", "&Delete selected");
        Open = Add("buttonOpenSelectedWorktree", "Text", "&Open selected");
        Prune = Add("buttonPruneWorktrees", "Text", "&Prune deleted worktrees");
    }

    public TranslatedText Title { get; }

    public TranslatedText Path { get; }

    public TranslatedText Type { get; }

    public TranslatedText Branch { get; }

    public TranslatedText Sha1 { get; }

    public TranslatedText Create { get; }

    public TranslatedText Delete { get; }

    public TranslatedText Open { get; }

    public TranslatedText Prune { get; }
}

/// <summary>Operations of the worktree list that need the host (git and the other dialogs).</summary>
public interface IManageWorktreeHost
{
    IReadOnlyList<GitWorktree> LoadWorktrees();

    /// <summary>Whether <paramref name="path"/> is the worktree opened in the application.</summary>
    bool IsCurrentWorktree(string path);

    /// <summary>Runs <c>git worktree prune</c>.</summary>
    void Prune();

    /// <summary>Deletes the worktree; returns whether it was deleted.</summary>
    bool Delete(string path);

    /// <summary>Opens the worktree in the application; returns whether it was opened.</summary>
    bool Switch(string path);

    /// <summary>Shows the create worktree dialog; returns whether a worktree was created.</summary>
    bool Create(string mainWorktreePath);
}

/// <summary>View model of the worktree list (port of <c>FormManageWorktree</c>).</summary>
public sealed partial class ManageWorktreeViewModel : DialogViewModel
{
    private readonly IManageWorktreeHost _host;
    private readonly string _workingDir;

    /// <param name="workingDir">The working directory of the repository, the base of a new worktree without any.</param>
    public ManageWorktreeViewModel(ManageWorktreeStrings strings, string workingDir, IManageWorktreeHost host)
    {
        Strings = strings;
        _workingDir = workingDir;
        _host = host;
        Reload();
    }

    public ManageWorktreeStrings Strings { get; }

    public ObservableCollection<GitWorktree> Worktrees { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand), nameof(DeleteCommand))]
    public partial GitWorktree? SelectedWorktree { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PruneCommand))]
    public partial bool HasDeletedWorktrees { get; private set; }

    /// <summary>Whether a worktree was created, so the revision grid shows its branch.</summary>
    public bool ShouldRefreshRevisionGrid { get; private set; }

    private void Reload()
    {
        IReadOnlyList<GitWorktree> worktrees = _host.LoadWorktrees();
        Worktrees.Clear();
        foreach (GitWorktree worktree in worktrees)
        {
            Worktrees.Add(worktree);
        }

        SelectedWorktree = Worktrees.FirstOrDefault();

        // The first worktree is the main one, which cannot be pruned.
        HasDeletedWorktrees = worktrees.Skip(1).Any(w => w.IsDeleted);
    }

    /// <summary>As <c>FormManageWorktree.CanActOnSelectedWorkspace</c>.</summary>
    private bool CanActOnSelectedWorktree()
        => Worktrees.Count > 1 && SelectedWorktree is { IsDeleted: false } worktree && !_host.IsCurrentWorktree(worktree.Path);

    private bool CanDeleteSelectedWorktree()
        => CanActOnSelectedWorktree() && Worktrees.IndexOf(SelectedWorktree!) != 0;

    [RelayCommand(CanExecute = nameof(CanActOnSelectedWorktree))]
    private void Open()
    {
        if (CanActOnSelectedWorktree() && _host.Switch(SelectedWorktree!.Path))
        {
            Close(accepted: true);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedWorktree))]
    private void Delete()
    {
        if (CanDeleteSelectedWorktree() && _host.Delete(SelectedWorktree!.Path))
        {
            Reload();
        }
    }

    [RelayCommand(CanExecute = nameof(HasDeletedWorktrees))]
    private void Prune()
    {
        _host.Prune();
        Reload();
    }

    [RelayCommand]
    private void Create()
    {
        string basePath = Worktrees.Count > 0 ? Worktrees[0].Path : _workingDir;
        if (_host.Create(basePath))
        {
            ShouldRefreshRevisionGrid = true;
            Reload();
        }
    }
}
