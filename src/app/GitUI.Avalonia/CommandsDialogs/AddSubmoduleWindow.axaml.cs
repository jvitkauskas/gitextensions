using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormAddSubmodule</c>.</summary>
public partial class AddSubmoduleWindow : DialogWindow
{
    public AddSubmoduleWindow()
    {
        InitializeComponent();
        Opened += (_, _) => directoryComboBox.Focus();
    }

    private void BranchComboBox_DropDownOpened(object? sender, EventArgs e)
    {
        if (DataContext is AddSubmoduleViewModel viewModel)
        {
            viewModel.LoadRemoteBranchesCommand.Execute(null);
        }
    }
}
