using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>Strings of the choose commit dialog; ids match <c>FormChooseCommit</c>.</summary>
public sealed class ChooseCommitStrings : ViewStrings
{
    public ChooseCommitStrings()
        : base("FormChooseCommit")
    {
        Title = Add("$this", "Text", "Choose Commit");
        Ok = Add("btnOK", "Text", "OK");
        FindSpecificCommit = Add("label1", "Text", "Find specific commit:");
        GoToCommit = Add("buttonGotoCommit", "Text", "Go to commit...");
        Parents = Add("labelParents", "Text", "Parent(s):");
    }

    public TranslatedText Title { get; }

    public TranslatedText Ok { get; }

    public TranslatedText FindSpecificCommit { get; }

    public TranslatedText GoToCommit { get; }

    public TranslatedText Parents { get; }
}

/// <summary>A parent of the selected commit, shown as a link that selects it.</summary>
public sealed record ParentLink(ObjectId ObjectId)
{
    public string ShortId => ObjectId.ToShortString();
}

/// <summary>Operations of the choose commit dialog that need the host (the other dialogs).</summary>
public interface IChooseCommitHost
{
    /// <summary>Shows the go to commit dialog; returns the commit to go to (zero if it cannot be found), or <see langword="null"/> if cancelled.</summary>
    ObjectId? ChooseCommitToGoTo();

    /// <summary>Tells the user that the commit cannot be found (<c>MessageBoxes.CannotFindGitRevision</c>).</summary>
    void ShowRevisionNotFound();

    /// <summary>Tells the user that the commit is not in the grid (<c>MessageBoxes.RevisionFilteredInGrid</c>).</summary>
    void ShowRevisionFiltered(ObjectId objectId);
}

/// <summary>View model of the choose commit dialog (port of <c>FormChooseCommit</c>).</summary>
public sealed partial class ChooseCommitViewModel : DialogViewModel, IDisposable
{
    private readonly IChooseCommitHost _host;

    public ChooseCommitViewModel(ChooseCommitStrings strings, RevisionGridViewModel grid, IChooseCommitHost host)
    {
        Strings = strings;
        Grid = grid;
        _host = host;
        Grid.PropertyChanged += OnGridPropertyChanged;
    }

    public ChooseCommitStrings Strings { get; }

    public RevisionGridViewModel Grid { get; }

    /// <summary>The chosen commit, once accepted.</summary>
    public GitRevision? SelectedRevision { get; private set; }

    /// <summary>The first two parents of the selected commit, as <c>FormChooseCommit</c> shows them.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<ParentLink> Parents { get; private set; } = [];

    [ObservableProperty]
    public partial bool IsCommitSelected { get; private set; }

    private void OnGridPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RevisionGridViewModel.SelectedRow))
        {
            return;
        }

        // As FormChooseCommit.revisionGrid_SelectionChanged.
        GitRevision? revision = Grid.SelectedRow?.Revision;
        IsCommitSelected = revision is not null;
        Parents = revision?.ParentIds is { Count: > 0 } parents ? [.. parents.Take(2).Select(id => new ParentLink(id))] : [];
        OkCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Accepts a revision activated in the grid (double click or Enter).</summary>
    public void Accept(RevisionGridRow row)
    {
        SelectedRevision = row.Revision;
        Close(accepted: true);
    }

    [RelayCommand(CanExecute = nameof(IsCommitSelected))]
    private void Ok()
    {
        if (Grid.SelectedRow is { } row)
        {
            Accept(row);
        }
    }

    [RelayCommand]
    private void GoToCommit()
    {
        // As RevisionGridMenuCommands.GotoCommitExecute.
        if (_host.ChooseCommitToGoTo() is not { } objectId)
        {
            return;
        }

        if (objectId.IsZero)
        {
            _host.ShowRevisionNotFound();
        }
        else
        {
            SelectCommit(objectId);
        }
    }

    [RelayCommand]
    private void SelectParent(ParentLink parent) => SelectCommit(parent.ObjectId);

    private void SelectCommit(ObjectId objectId)
    {
        if (!Grid.SelectRevision(objectId))
        {
            _host.ShowRevisionFiltered(objectId);
        }
    }

    public void Dispose()
    {
        Grid.PropertyChanged -= OnGridPropertyChanged;
        Grid.Dispose();
    }
}
