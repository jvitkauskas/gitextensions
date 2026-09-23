using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormClone</c>.</summary>
public partial class CloneWindow : DialogWindow
{
    public CloneWindow()
    {
        InitializeComponent();
        Opened += (_, _) => fromComboBox.Focus();
    }

    private void BranchComboBox_DropDownOpened(object? sender, EventArgs e)
    {
        if (DataContext is CloneViewModel viewModel)
        {
            viewModel.LoadBranchesCommand.Execute(null);
        }
    }
}
