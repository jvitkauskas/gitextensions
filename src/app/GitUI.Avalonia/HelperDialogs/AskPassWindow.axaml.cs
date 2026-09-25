using GitUI.Avalonia.Hosting;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.Avalonia.HelperDialogs;

/// <summary>The prompt of ssh and git off Windows (<see cref="AskPassViewModel"/>).</summary>
public partial class AskPassWindow : DialogWindow
{
    public AskPassWindow()
    {
        InitializeComponent();
        Opened += (_, _) => inputTextBox.Focus();
        DataContextChanged += (_, _) =>
        {
            // A secret is hidden.
            inputTextBox.PasswordChar = DataContext is AskPassViewModel { IsSecret: true } ? '●' : default;
        };
    }
}
