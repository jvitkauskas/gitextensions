using Avalonia.Controls;
using Avalonia.VisualTree;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.Editor;

/// <summary>Avalonia port of <c>FormGoToLine</c>.</summary>
public partial class GoToLineWindow : DialogWindow
{
    public GoToLineWindow()
    {
        InitializeComponent();

        // As in FormGoToLine: the number is selected, so that typing replaces it.
        Opened += (_, _) =>
        {
            if (lineNumberUpDown.FindDescendantOfType<TextBox>() is { } textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        };
    }
}
