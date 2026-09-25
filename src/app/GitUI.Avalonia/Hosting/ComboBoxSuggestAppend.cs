using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  The auto-completion of an editable <see cref="ComboBox"/> as WinForms <c>AutoCompleteMode.SuggestAppend</c> with its items
///  as the source: typing at the end appends the rest of the first item starting with the text, selected
///  (<c>h:ComboBoxSuggestAppend.IsEnabled="True"</c>).
/// </summary>
public static class ComboBoxSuggestAppend
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ComboBox, bool>("IsEnabled", typeof(ComboBoxSuggestAppend));

    // The text before the last change, to tell typing at the end from other changes.
    private static readonly AttachedProperty<string?> PreviousTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>("PreviousText", typeof(ComboBoxSuggestAppend));

    // The text completed last, whose change (raised later) is not typing.
    private static readonly AttachedProperty<string?> CompletedTextProperty =
        AvaloniaProperty.RegisterAttached<TextBox, string?>("CompletedText", typeof(ComboBoxSuggestAppend));

    static ComboBoxSuggestAppend()
    {
        IsEnabledProperty.Changed.AddClassHandler<ComboBox>((comboBox, e) =>
        {
            comboBox.TemplateApplied -= OnTemplateApplied;
            if (e.NewValue is true)
            {
                comboBox.TemplateApplied += OnTemplateApplied;
                if (FindTextBox(comboBox) is { } textBox)
                {
                    Attach(textBox);
                }
            }
        });
    }

    public static bool GetIsEnabled(ComboBox comboBox) => comboBox.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(ComboBox comboBox, bool value) => comboBox.SetValue(IsEnabledProperty, value);

    private static void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is ComboBox comboBox && (e.NameScope.Find<TextBox>("PART_EditableTextBox") ?? FindTextBox(comboBox)) is { } textBox)
        {
            Attach(textBox);
        }
    }

    private static TextBox? FindTextBox(ComboBox comboBox) => comboBox.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();

    private static void Attach(TextBox textBox)
    {
        textBox.TextChanged -= OnTextChanged;
        textBox.TextChanged += OnTextChanged;
        textBox.SetValue(PreviousTextProperty, textBox.Text);
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        string text = textBox.Text ?? "";
        if (textBox.GetValue(CompletedTextProperty) is { } completedText)
        {
            textBox.SetValue(CompletedTextProperty, null);
            if (text == completedText)
            {
                return;
            }
        }

        string previous = textBox.GetValue(PreviousTextProperty) ?? "";
        textBox.SetValue(PreviousTextProperty, text);

        // Only typing (not a deletion or an item chosen); at the end, once the caret moved.
        if (text.Length <= previous.Length || !text.StartsWith(previous, StringComparison.Ordinal)
            || textBox.FindAncestorOfType<ComboBox>() is not { } comboBox || !GetIsEnabled(comboBox) || !textBox.IsKeyboardFocusWithin)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => Append(textBox, comboBox, text), DispatcherPriority.Input);
    }

    private static void Append(TextBox textBox, ComboBox comboBox, string text)
    {
        if (textBox.Text != text || textBox.CaretIndex != text.Length)
        {
            return;
        }

        string? match = comboBox.Items.OfType<object>().Select(item => item.ToString())
            .FirstOrDefault(item => item is not null && item.Length > text.Length && item.StartsWith(text, StringComparison.CurrentCultureIgnoreCase));
        if (match is null)
        {
            return;
        }

        // The typed text stays the previous one: typing on over the selection completes again.
        string completed = text + match[text.Length..];
        textBox.SetValue(CompletedTextProperty, completed);
        textBox.Text = completed;
        textBox.SelectionStart = text.Length;
        textBox.SelectionEnd = completed.Length;
    }
}
