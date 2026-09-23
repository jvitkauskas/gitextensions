using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormGitAttributes</c> and <c>FormMailMap</c>.</summary>
public partial class RepoFileEditorWindow : DialogWindow
{
    public RepoFileEditorWindow()
    {
        InitializeComponent();
        Opened += (_, _) => editorView.Editor.Focus();
    }
}
