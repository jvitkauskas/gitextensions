using Avalonia.Threading;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormLog</c> (the <c>viewdiff</c> verb): the revision grid, the files and the diff.</summary>
public partial class LogWindow : DialogWindow
{
    public LogWindow()
    {
        InitializeComponent();

        // As FormDiffLoad: the revisions are loaded once the window is shown.
        Opened += (_, _) =>
        {
            revisionGrid.Focus();
            Dispatcher.UIThread.Post(() => (DataContext as LogViewModel)?.Initialize());
        };
        Closed += (_, _) => (DataContext as LogViewModel)?.Dispose();

        // As the double click of RevisionGridControl: the commit diff of the revision.
        revisionGrid.RevisionActivated += (_, _) => (DataContext as LogViewModel)?.ViewSelectedRevisions();
    }

    public RevisionGridView RevisionGrid => revisionGrid;
}
