using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormApplyPatch</c>.</summary>
public partial class ApplyPatchWindow : DialogWindow
{
    private ApplyPatchViewModel? _viewModel;

    public ApplyPatchWindow()
    {
        InitializeComponent();

        // As MergePatch_Load: the patch file gets the focus; then the state of the patches is loaded.
        Opened += (_, _) =>
        {
            patchFileTextBox.Focus();
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

        _viewModel = DataContext as ApplyPatchViewModel;
        if (_viewModel is not null)
        {
            _viewModel.FocusDefaultButtonRequested += OnFocusDefaultButtonRequested;
        }
    }

    /// <summary>As <c>EnableButtons</c>: the solve conflicts or conflicts resolved button gets the focus (once enabled by the bindings).</summary>
    private void OnFocusDefaultButtonRequested(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel?.IsMergetoolDefault == true)
            {
                mergetoolButton.Focus();
            }
            else if (_viewModel?.IsResolvedDefault == true)
            {
                resolvedButton.Focus();
            }
        });
}
