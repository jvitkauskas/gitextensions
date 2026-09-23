using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormEditor</c>, the file editor also used as git's editor.</summary>
public partial class FileEditorWindow : DialogWindow
{
    public FileEditorWindow()
    {
        InitializeComponent();
        Opened += (_, _) => editorView.Editor.Focus();
    }
}
