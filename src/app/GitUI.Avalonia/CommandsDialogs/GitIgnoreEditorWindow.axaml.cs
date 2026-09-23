using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormGitIgnore</c> (.gitignore and .git/info/exclude).</summary>
public partial class GitIgnoreEditorWindow : DialogWindow
{
    public GitIgnoreEditorWindow()
    {
        InitializeComponent();
    }
}
