using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.SpellChecker;
using GitUI.Presentation.Translations;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The view of <see cref="SpellCheckViewModel"/> on an AvaloniaEdit editor (port of <c>EditNetSpell</c> and
///  <c>SpellCheckEditControl</c>): the waves under the misspelled words and the marks of the ill-formed lines, the
///  context menu with the suggestions and the auto-completion list.
/// </summary>
public sealed class SpellCheckController
{
    private readonly TextEditor _editor;
    private readonly SpellCheckViewModel _viewModel;
    private readonly DispatcherTimer _spellCheckTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _autoCompleteTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer _toolTipTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly MarkRenderer _illFormedLinesRenderer;
    private readonly MarkRenderer _mistakesRenderer;
    private CompletionWindow? _completionWindow;
    private bool _autoCompleteWasUserActivated;
    private bool _disableAutoCompleteTriggerOnTextUpdate = true;

    public SpellCheckController(TextEditor editor, SpellCheckViewModel viewModel)
    {
        _editor = editor;
        _viewModel = viewModel;
        _illFormedLinesRenderer = new MarkRenderer(KnownLayer.Background, () => _viewModel.IllFormedLines, wave: false);
        _mistakesRenderer = new MarkRenderer(KnownLayer.Selection, () => _viewModel.Mistakes, wave: true);
        TextView textView = editor.TextArea.TextView;
        textView.BackgroundRenderers.Add(_illFormedLinesRenderer);
        textView.BackgroundRenderers.Add(_mistakesRenderer);
        viewModel.PropertyChanged += (_, _) =>
        {
            textView.InvalidateLayer(KnownLayer.Background);
            textView.InvalidateLayer(KnownLayer.Selection);
        };

        _spellCheckTimer.Tick += (_, _) =>
        {
            _spellCheckTimer.Stop();
            CheckSpelling();
        };
        _autoCompleteTimer.Tick += (_, _) =>
        {
            _autoCompleteTimer.Stop();
            UpdateOrShowAutoComplete(calledByUser: false);
        };
        _toolTipTimer.Tick += (_, _) =>
        {
            _toolTipTimer.Stop();
            ToolTip.SetIsOpen(_editor, false);
        };

        editor.TextChanged += (_, _) => OnTextChanged();
        editor.TextArea.TextEntering += OnTextEntering;
        editor.TextArea.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        editor.TextArea.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        editor.TextArea.LostFocus += (_, _) =>
        {
            if (_completionWindow is { IsKeyboardFocusWithin: false })
            {
                CloseAutoComplete();
            }
        };

        ContextMenu menu = new();
        menu.Opening += (_, _) => FillContextMenu();
        editor.ContextMenu = menu;

        // As OnRuntimeLoad: the text is checked once loaded.
        _spellCheckTimer.Start();
    }

    /// <summary>
    ///  As <c>ContextMenuPopulating</c>: raised once the items are added, e.g. to insert more items before the first
    ///  separator.
    /// </summary>
    public event EventHandler<ContextMenu>? ContextMenuPopulating;

    /// <summary>Raised after pasting through the context menu.</summary>
    public event EventHandler? Pasted;

    public SpellCheckViewModel ViewModel => _viewModel;

    /// <summary>The open auto-completion list (e.g. for tests).</summary>
    public CompletionWindow? CompletionWindow => _completionWindow;

    /// <summary>The ranges drawn (e.g. for tests): the misspelled words and the ill-formed lines.</summary>
    public (IReadOnlyList<TextSpan> Mistakes, IReadOnlyList<TextSpan> IllFormedLines) Marks => (_viewModel.Mistakes, _viewModel.IllFormedLines);

    /// <summary>As <c>CheckSpelling</c>.</summary>
    public void CheckSpelling() => _viewModel.Check(_editor.Text);

    /// <summary>As <c>UpdateOrShowAutoComplete</c>.</summary>
    public void UpdateOrShowAutoComplete(bool calledByUser)
    {
        AutoCompletion completion = _viewModel.GetCompletions(_editor.Text, _editor.CaretOffset, calledByUser, _autoCompleteWasUserActivated);
        switch (completion.Kind)
        {
            case AutoCompletionKind.NotLoaded:
                ToolTip.SetTip(_editor, SpellCheckViewModel.AutoCompleteNotAvailableText);
                ToolTip.SetIsOpen(_editor, true);
                _toolTipTimer.Stop();
                _toolTipTimer.Start();
                return;
            case AutoCompletionKind.None:
                CloseAutoComplete();
                return;
            case AutoCompletionKind.Accept:
                CloseAutoComplete();
                Replace(completion.Start, _editor.CaretOffset - completion.Start, completion.Words![0]);
                return;
        }

        ToolTip.SetIsOpen(_editor, false);
        if (calledByUser)
        {
            _autoCompleteWasUserActivated = true;
        }

        if (_completionWindow is not null)
        {
            // The open list filters itself while typing.
            return;
        }

        CompletionWindow window = new(_editor.TextArea) { StartOffset = completion.Start, EndOffset = _editor.CaretOffset };
        foreach (string word in completion.Words!)
        {
            window.CompletionList.CompletionData.Add(new WordCompletion(word));
        }

        window.Closed += (_, _) =>
        {
            if (_completionWindow == window)
            {
                _completionWindow = null;
                _autoCompleteWasUserActivated = false;
            }
        };
        _completionWindow = window;
        window.Show();

        // As the list box of EditNetSpell: the first word is selected, which Tab and Enter accept.
        window.CompletionList.SelectedItem = window.CompletionList.CompletionData[0];
    }

    private void CloseAutoComplete()
    {
        _completionWindow?.Close();
        _completionWindow = null;
        _autoCompleteWasUserActivated = false;
    }

    /// <summary>As <c>TextBoxTextChanged</c>.</summary>
    private void OnTextChanged()
    {
        if (!_disableAutoCompleteTriggerOnTextUpdate)
        {
            // Only shown after typing a letter.
            _disableAutoCompleteTriggerOnTextUpdate = true;
            _autoCompleteTimer.Stop();
            _autoCompleteTimer.Start();
        }

        if (_completionWindow is not null)
        {
            // Once the caret follows the change: the list closes without a matching word.
            Dispatcher.UIThread.Post(() =>
            {
                if (_completionWindow is not null
                    && _viewModel.GetCompletions(_editor.Text, _editor.CaretOffset, calledByUser: false, _autoCompleteWasUserActivated).Kind == AutoCompletionKind.None)
                {
                    CloseAutoComplete();
                }
            });
        }

        _spellCheckTimer.Stop();
        if (_viewModel.OnTextChanged(_editor.Text))
        {
            _spellCheckTimer.Start();
        }
    }

    /// <summary>As <c>TextBox_KeyPress</c>: a separator closes the list, a letter opens it.</summary>
    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        bool isSeparator = WordAtCursor.IsSeparator(e.Text[0]);
        _disableAutoCompleteTriggerOnTextUpdate = isSeparator;
        if (isSeparator)
        {
            CloseAutoComplete();
        }
    }

    /// <summary>As <c>TextBox_KeyDown</c>: Ctrl+Space completes the word; Backspace after a separator closes the list.</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control && _viewModel.ProvideAutoCompletion)
        {
            UpdateOrShowAutoComplete(calledByUser: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Back && e.KeyModifiers == KeyModifiers.None)
        {
            int caret = _editor.CaretOffset;
            if (caret <= 1 || WordAtCursor.IsSeparator(_editor.Text[caret - 2]))
            {
                CloseAutoComplete();
            }
        }
    }

    /// <summary>As <c>TextBox_MouseDown</c>: the right button moves the cursor, where the menu applies.</summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_editor.TextArea).Properties.IsRightButtonPressed)
        {
            return;
        }

        TextViewPosition? position = _editor.GetPositionFromPoint(e.GetPosition(_editor));
        if (position is null)
        {
            return;
        }

        int offset = _editor.Document.GetOffset(position.Value.Location);
        if (_editor.SelectionLength == 0 || offset < _editor.SelectionStart || offset > _editor.SelectionStart + _editor.SelectionLength)
        {
            _editor.Select(offset, 0);
        }
    }

    /// <summary>As <c>SpellCheckContextMenuOpening</c>: the items of the context menu, for the cursor.</summary>
    /// <returns>The items (e.g. for tests).</returns>
    public IReadOnlyList<object> FillContextMenu()
    {
        ContextMenu menu = _editor.ContextMenu!;
        SpellCheckStrings strings = _viewModel.Strings;
        string text = _editor.Text;
        int position = _editor.CaretOffset;
        bool editable = !_editor.IsReadOnly;
        menu.Items.Clear();

        // As AddWordSuggestions.
        if (editable && _viewModel.GetSuggestions(text, position) is { } suggestions)
        {
            foreach (string suggestion in suggestions.Suggestions)
            {
                MenuItem item = new() { Header = suggestion.Replace("_", "__"), FontWeight = FontWeight.Bold };
                item.Click += (_, _) => ApplyAndCheck(_viewModel.ReplaceWord(text, position, suggestion));
                menu.Items.Add(item);
            }

            MenuItem addToDictionary = new() { Header = TranslatedText.ToAccessKeyText(strings.AddToDictionary.Text) };
            addToDictionary.Click += (_, _) => _viewModel.AddToDictionary(text, position);
            menu.Items.Add(addToDictionary);
            MenuItem ignoreWord = new() { Header = TranslatedText.ToAccessKeyText(strings.IgnoreWord.Text) };
            ignoreWord.Click += (_, _) => _viewModel.IgnoreWord(text, position);
            menu.Items.Add(ignoreWord);
            MenuItem removeWord = new() { Header = TranslatedText.ToAccessKeyText(strings.RemoveWord.Text) };
            removeWord.Click += (_, _) => ApplyAndCheck(_viewModel.DeleteWord(text, position));
            menu.Items.Add(removeWord);
            if (suggestions.Suggestions.Count > 0)
            {
                menu.Items.Add(new Separator());
            }
        }

        AddItem(strings.Cut, editable, () =>
        {
            _editor.Cut();
            CheckSpelling();
        });
        AddItem(strings.Copy, isEnabled: true, _editor.Copy);
        AddItem(strings.Paste, editable, () =>
        {
            _editor.Paste();
            Pasted?.Invoke(this, EventArgs.Empty);
            CheckSpelling();
        });
        AddItem(strings.Delete, editable, () =>
        {
            _editor.SelectedText = "";
            CheckSpelling();
        });
        AddItem(strings.SelectAll, isEnabled: true, _editor.SelectAll);
        menu.Items.Add(new Separator());

        // As AddDictionaries.
        MenuItem dictionaries = new() { Header = TranslatedText.ToAccessKeyText(strings.Dictionary.Text) };
        string current = _viewModel.Dictionary;
        foreach (string dictionary in (IEnumerable<string>)[SpellCheckViewModel.NoDictionary, .. _viewModel.GetDictionaries()])
        {
            MenuItem item = new() { Header = dictionary.Replace("_", "__"), ToggleType = MenuItemToggleType.CheckBox, IsChecked = dictionary == current };
            item.Click += (_, _) => _viewModel.SelectDictionary(dictionary, _editor.Text);
            dictionaries.Items.Add(item);
        }

        menu.Items.Add(dictionaries);
        menu.Items.Add(new Separator());

        MenuItem markIllFormedLines = new()
        {
            Header = TranslatedText.ToAccessKeyText(strings.MarkIllFormedLines.Text),
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = _viewModel.MarkIllFormedLines,
        };
        markIllFormedLines.Click += (_, _) => _viewModel.ToggleMarkIllFormedLines(_editor.Text);
        menu.Items.Add(markIllFormedLines);
        MenuItem autoCompletion = new()
        {
            Header = TranslatedText.ToAccessKeyText(strings.AutoCompletion.Text),
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = _viewModel.ProvideAutoCompletion,
        };
        autoCompletion.Click += (_, _) =>
        {
            _viewModel.ToggleAutoCompletion();
            if (!_viewModel.ProvideAutoCompletion)
            {
                CloseAutoComplete();
            }
        };
        menu.Items.Add(autoCompletion);

        ContextMenuPopulating?.Invoke(this, menu);
        return [.. menu.Items.OfType<object>()];

        void AddItem(TranslatedText header, bool isEnabled, Action action)
        {
            MenuItem item = new() { Header = TranslatedText.ToAccessKeyText(header.Text), IsEnabled = isEnabled };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }
    }

    private void ApplyAndCheck(TextEdit? edit)
    {
        if (edit is not null)
        {
            // As SpellingReplacedWord / SpellingDeletedWord: the selection is kept where possible.
            int start = _editor.SelectionStart;
            int length = _editor.SelectionLength;
            Replace(edit.Offset, edit.Length, edit.Text);
            int textLength = _editor.Document.TextLength;
            start = Math.Min(start, textLength);
            _editor.Select(start, start + length > textLength ? 0 : length);
        }

        CheckSpelling();
    }

    private void Replace(int offset, int length, string text)
    {
        _editor.Document.Replace(offset, length, text);
        _editor.CaretOffset = Math.Min(offset + text.Length, _editor.Document.TextLength);
    }

    /// <summary>A word of the auto-completion list; it replaces the typed word (as <c>AcceptAutoComplete</c>).</summary>
    private sealed class WordCompletion(string word) : ICompletionData
    {
        public IImage? Image => null;

        public string Text => word;

        public object Content => word;

        public object? Description => null;

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            => textArea.Document.Replace(completionSegment, word);
    }

    /// <summary>
    ///  As <c>SpellCheckEditControl</c>: a red wave under the misspelled words (<c>DrawWave</c>), a translucent yellow mark
    ///  behind the ill-formed text (<c>DrawMark</c>).
    /// </summary>
    private sealed class MarkRenderer(KnownLayer layer, Func<IReadOnlyList<TextSpan>> getRanges, bool wave) : IBackgroundRenderer
    {
        public KnownLayer Layer => layer;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            IReadOnlyList<TextSpan> ranges = getRanges();
            if (ranges.Count == 0 || !textView.VisualLinesValid)
            {
                return;
            }

            bool isDark = textView.ActualThemeVariant == ThemeVariant.Dark;
            int textLength = textView.Document.TextLength;
            Pen wavePen = new(isDark ? new SolidColorBrush(Color.FromRgb(0xFF, 0x60, 0x60)) : Brushes.Red, 1);
            IBrush markBrush = new SolidColorBrush(isDark ? Color.FromArgb(90, 160, 160, 0) : Color.FromArgb(120, 255, 255, 0));
            foreach (TextSpan range in ranges)
            {
                if (range.End > textLength)
                {
                    continue;
                }

                TextSegment segment = new() { StartOffset = range.Start, Length = range.Length };
                foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
                {
                    if (wave)
                    {
                        DrawWave(drawingContext, wavePen, rect);
                    }
                    else
                    {
                        drawingContext.FillRectangle(markBrush, rect);
                    }
                }
            }
        }

        private static void DrawWave(DrawingContext drawingContext, Pen pen, Rect rect)
        {
            const double waveWidth = 4;
            double y = rect.Bottom - 1;
            StreamGeometry geometry = new();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(new Point(rect.Left, y - (waveWidth / 2)), isFilled: false);
                bool down = true;
                for (double x = rect.Left + (waveWidth / 2); x <= rect.Right; x += waveWidth / 2)
                {
                    context.LineTo(new Point(x, down ? y : y - (waveWidth / 2)));
                    down = !down;
                }

                context.EndFigure(isClosed: false);
            }

            drawingContext.DrawGeometry(null, pen, geometry);
        }
    }
}
