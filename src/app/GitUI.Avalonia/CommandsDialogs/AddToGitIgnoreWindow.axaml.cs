using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormAddToGitIgnore</c>.</summary>
public partial class AddToGitIgnoreWindow : DialogWindow
{
    public AddToGitIgnoreWindow()
    {
        InitializeComponent();
        Opened += (_, _) => patternsTextBox.Focus();
    }
}
