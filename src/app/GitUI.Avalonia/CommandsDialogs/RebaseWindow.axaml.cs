using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormRebase</c>.</summary>
public partial class RebaseWindow : DialogWindow
{
    private RebaseViewModel? _viewModel;

    public RebaseWindow()
    {
        InitializeComponent();

        // As FormRebase.OnShown: the branch box gets the focus, then the state of the rebase is loaded (and the rebase started if asked to).
        Opened += (_, _) =>
        {
            branchesComboBox.Focus();
            Dispatcher.UIThread.Post(() => _viewModel?.InitializeView());
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.FocusDefaultButtonRequested -= OnFocusDefaultButtonRequested;
        }

        _viewModel = DataContext as RebaseViewModel;
        if (_viewModel is not null)
        {
            _viewModel.FocusDefaultButtonRequested += OnFocusDefaultButtonRequested;
        }
    }

    /// <summary>As <c>EnableButtons</c>: the solve conflicts or continue button gets the focus (once shown by the bindings).</summary>
    private void OnFocusDefaultButtonRequested(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel?.IsSolveConflictsDefault == true)
            {
                solveConflictsButton.Focus();
            }
            else if (_viewModel?.IsContinueDefault == true)
            {
                continueButton.Focus();
            }
        });
}
