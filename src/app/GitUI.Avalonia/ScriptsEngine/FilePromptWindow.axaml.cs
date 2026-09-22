using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.ScriptsEngine;

/// <summary>Avalonia port of <c>FormFilePrompt</c>.</summary>
public partial class FilePromptWindow : DialogWindow
{
    public FilePromptWindow()
    {
        InitializeComponent();
        Opened += (_, _) => filePathTextBox.Focus();
    }
}
