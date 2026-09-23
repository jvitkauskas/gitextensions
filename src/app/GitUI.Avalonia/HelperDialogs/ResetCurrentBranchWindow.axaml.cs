using Avalonia.Input;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>Avalonia port of <c>FormResetCurrentBranch</c>.</summary>
public partial class ResetCurrentBranchWindow : DialogWindow
{
    public ResetCurrentBranchWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Instead of the WinForms help button in the title bar.
        if (e.Key == Key.F1 && DataContext is ResetCurrentBranchViewModel viewModel)
        {
            viewModel.OpenHelpCommand.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
