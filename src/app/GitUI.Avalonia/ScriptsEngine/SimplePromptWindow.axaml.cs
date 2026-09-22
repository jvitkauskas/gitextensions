using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.ScriptsEngine;

/// <summary>Avalonia port of <c>SimplePrompt</c>.</summary>
public partial class SimplePromptWindow : DialogWindow
{
    public SimplePromptWindow()
    {
        InitializeComponent();
        Opened += (_, _) => inputTextBox.Focus();

        // As in SimplePrompt: Escape first clears a selection, and only then cancels the prompt.
        inputTextBox.AddHandler(KeyDownEvent, OnInputKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && inputTextBox.SelectionStart != inputTextBox.SelectionEnd)
        {
            inputTextBox.SelectionEnd = inputTextBox.SelectionStart;
            e.Handled = true;
        }
    }
}
