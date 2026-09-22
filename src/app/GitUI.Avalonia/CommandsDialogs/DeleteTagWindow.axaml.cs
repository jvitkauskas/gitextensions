using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormDeleteTag</c>.</summary>
public partial class DeleteTagWindow : DialogWindow
{
    public DeleteTagWindow()
    {
        InitializeComponent();
        Opened += (_, _) => tagsComboBox.Focus();
    }
}
