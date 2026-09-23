using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GitUI.Avalonia.Controls.FileStatusList;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Translations;

namespace GitUI.Avalonia.CommandsDialogs.CommitDialog;

/// <summary>Avalonia port of <c>FormCommit</c>.</summary>
public partial class CommitWindow : DialogWindow
{
    private CommitViewModel? _viewModel;
    private CommitMessageColorizer? _colorizer;

    /// <summary>As <c>_insertScopeParentheses</c>: the Conventional Commits menu was opened by the hotkey with the scope.</summary>
    private bool _insertScope;

    /// <summary>As the throttling of <c>_selectionFilterSubject</c>.</summary>
    private readonly DispatcherTimer _selectionFilterTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };

    public CommitWindow()
    {
        InitializeComponent();

        // As OnShown: the files and the message are loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = _viewModel?.InitializeAsync());
        Activated += (_, _) => _viewModel?.OnActivated();
        Closed += (_, _) =>
        {
            _selectionFilterTimer.Stop();
            _viewModel?.SpellCheck?.CancelAutoComplete();
        };
        _selectionFilterTimer.Tick += (_, _) =>
        {
            _selectionFilterTimer.Stop();
            _viewModel?.ApplySelectionFilter();
        };

        // As OnSelectionFilterIndexChanged: a remembered filter applies at once.
        selectionFilter.SelectionChanged += (_, _) =>
        {
            if (selectionFilter.SelectedItem is not null)
            {
                Dispatcher.UIThread.Post(() => _viewModel?.ApplySelectionFilter());
            }
        };

        AvaloniaEdit.TextEditor editor = message.Editor;
        editor.TextChanged += (_, _) => UpdateWatermark();
        editor.GotFocus += (_, _) => _viewModel?.OnMessageEntered();
        editor.TextArea.Caret.PositionChanged += (_, _) => UpdateCaret();

        // As Message_KeyDown: Ctrl+Enter commits.
        editor.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.Control && _viewModel is not null)
            {
                _viewModel.CommitCommand.Execute(null);
                e.Handled = true;
            }
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // As Message_TextChanged: the message is formatted as it is typed (not when undoing) and when set.
        editor.TextArea.TextEntered += (_, _) => FormatMessage();

        author.LostFocus += (_, _) => _viewModel?.UpdateAuthorInfo();
        ((MenuFlyout)commitMessageButton.Flyout!).Opening += (_, _) => FillCommitMessageMenu();
        ((MenuFlyout)commitTemplatesButton.Flyout!).Opening += (_, _) => FillCommitTemplatesMenu();
        ((MenuFlyout)commitTemplatesButton.Flyout!).Closed += (_, _) => _insertScope = false;
    }

    public FileStatusListView UnstagedFiles => unstagedFiles;

    public FileStatusListView StagedFiles => stagedFiles;

    public TextEditorView MessageEditor => message;

    public FileViewerView DiffViewer => diff;

    public TextBlock Watermark => messageWatermark;

    /// <summary>The items of the templates menu, as last filled (e.g. for tests).</summary>
    public IReadOnlyList<object> CommitTemplatesMenuItems => [.. ((MenuFlyout)commitTemplatesButton.Flyout!).Items.OfType<object>()];

    /// <summary>The items of the message menu (e.g. for tests).</summary>
    public IReadOnlyList<object> CommitMessageMenuItems => [.. ((MenuFlyout)commitMessageButton.Flyout!).Items.OfType<object>()];

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.Message.TextLoaded -= OnMessageLoaded;
        _viewModel?.Unstaged.PropertyChanged -= OnListPropertyChanged;
        _viewModel?.Staged.PropertyChanged -= OnListPropertyChanged;
        _viewModel = DataContext as CommitViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.Message.TextLoaded += OnMessageLoaded;
        _viewModel?.Unstaged.PropertyChanged += OnListPropertyChanged;
        _viewModel?.Staged.PropertyChanged += OnListPropertyChanged;
        if (_viewModel is null)
        {
            return;
        }

        CommitDialogOptions options = _viewModel.Options;
        message.Editor.TextArea.TextView.LineTransformers.Remove(_colorizer);
        _colorizer = new CommitMessageColorizer(options.MaxFirstLineLength, options.MaxLineLength, options.SecondLineMustBeEmpty, Brushes.Red);
        message.Editor.TextArea.TextView.LineTransformers.Add(_colorizer);
        message.Editor.WordWrap = false;
        UseMessageMenu();
        UpdateMessageEditable();
        UpdateFilterImages();
        UpdateWatermark();
        UpdateCaret();
    }

    /// <summary>As <c>ExecuteCommand</c>: the hotkeys of the view; the others are executed by the view model.</summary>
    protected override bool ExecuteHotkeyCommand(int commandCode)
    {
        if (_viewModel is null)
        {
            return false;
        }

        switch ((CommitHotkeyCommand)commandCode)
        {
            case CommitHotkeyCommand.FocusUnstagedFiles:
                return FocusFiles(unstagedFiles);
            case CommitHotkeyCommand.FocusStagedFiles:
                return FocusFiles(stagedFiles);
            case CommitHotkeyCommand.FocusSelectedDiff:
                diff.TextView.Editor.TextArea.Focus();
                return true;
            case CommitHotkeyCommand.FocusCommitMessage:
                message.Editor.TextArea.Focus();
                return true;
            case CommitHotkeyCommand.ToggleSelectionFilter:
                ToggleSelectionFilter();
                return true;
            case CommitHotkeyCommand.AddSelectionToCommitMessage:
                return AddSelectionToCommitMessage();
            case CommitHotkeyCommand.ConventionalCommit_PrefixMessage:
            case CommitHotkeyCommand.ConventionalCommit_PrefixMessageWithScope:
                OpenConventionalCommitMenu(insertScope: commandCode == (int)CommitHotkeyCommand.ConventionalCommit_PrefixMessageWithScope);
                return true;
            case CommitHotkeyCommand.SelectNext:
            case CommitHotkeyCommand.SelectNext_AlternativeHotkey1:
            case CommitHotkeyCommand.SelectNext_AlternativeHotkey2:
            case CommitHotkeyCommand.SelectPrevious:
            case CommitHotkeyCommand.SelectPrevious_AlternativeHotkey1:
            case CommitHotkeyCommand.SelectPrevious_AlternativeHotkey2:
                _viewModel.MoveSelection(
                    backwards: commandCode >= (int)CommitHotkeyCommand.SelectPrevious,
                    messageFocused: message.IsKeyboardFocusWithin);
                return true;
            default:
                return base.ExecuteHotkeyCommand(commandCode);
        }
    }

    private static bool FocusFiles(FileStatusListView list)
    {
        // The selected file has the focus, as the focused node of the WinForms tree.
        TreeView tree = list.Tree;
        Control? item = tree.SelectedItem is { } selected ? tree.ContainerFromItem(selected) : null;
        (item ?? tree).Focus(NavigationMethod.Tab);
        return true;
    }

    /// <summary>As <c>ToggleSelectionFilter</c>: the filter gets the focus when shown, gives it to the unstaged files when hidden.</summary>
    private void ToggleSelectionFilter()
    {
        bool visible = !_viewModel!.IsSelectionFilterVisible;
        if (!visible && selectionFilter.IsKeyboardFocusWithin)
        {
            FocusFiles(unstagedFiles);
        }

        _viewModel.IsSelectionFilterVisible = visible;
        if (visible)
        {
            Dispatcher.UIThread.Post(() => selectionFilter.Focus(), DispatcherPriority.Loaded);
        }
    }

    /// <summary>As <c>AddSelectionToCommitMessage</c>: the text selected in the diff replaces the selection of the message.</summary>
    private bool AddSelectionToCommitMessage()
    {
        AvaloniaEdit.TextEditor diffEditor = diff.TextView.Editor;
        if (!diff.IsKeyboardFocusWithin || _viewModel?.IsMessageEditable != true)
        {
            return false;
        }

        string selectedText = diffEditor.SelectedText;
        if (string.IsNullOrEmpty(selectedText))
        {
            return false;
        }

        AvaloniaEdit.TextEditor editor = message.Editor;
        if (editor.SelectionLength == 0)
        {
            selectedText += '\n';
        }

        int selectionStart = editor.SelectionStart;
        editor.Document.Replace(selectionStart, editor.SelectionLength, selectedText);
        editor.Select(selectionStart + selectedText.Length, 0);
        return true;
    }

    /// <summary>As <c>OpenConventionalCommitMenu</c>: the Conventional Commits menu, with <c>feat</c> selected.</summary>
    public void OpenConventionalCommitMenu(bool insertScope)
    {
        if (_viewModel?.IsMessageEditable != true)
        {
            return;
        }

        _insertScope = insertScope;
        MenuFlyout flyout = (MenuFlyout)commitTemplatesButton.Flyout!;
        flyout.ShowAt(commitTemplatesButton);
        Dispatcher.UIThread.Post(
            () =>
            {
                if (flyout.Items.OfType<MenuItem>().LastOrDefault(i => i.Items.Count > 0) is not { } conventional)
                {
                    return;
                }

                conventional.IsSelected = true;
                conventional.AddHandler(MenuItem.SubmenuOpenedEvent, OnSubmenuOpened);
                conventional.IsSubMenuOpen = true;

                void OnSubmenuOpened(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
                {
                    conventional.RemoveHandler(MenuItem.SubmenuOpenedEvent, OnSubmenuOpened);
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            if (conventional.Items.OfType<MenuItem>().FirstOrDefault(i => (string?)i.Header == ConventionalCommits.Feat) is not { } feat)
                            {
                                return;
                            }

                            feat.IsSelected = true;
                            if (TopLevel.GetTopLevel(feat) is not null)
                            {
                                feat.Focus(NavigationMethod.Directional);
                                return;
                            }

                            // The popup of the submenu is shown after its layout.
                            feat.AttachedToVisualTree += OnAttached;

                            void OnAttached(object? sender, global::Avalonia.VisualTreeAttachmentEventArgs e)
                            {
                                feat.AttachedToVisualTree -= OnAttached;
                                Dispatcher.UIThread.Post(() => feat.Focus(NavigationMethod.Directional), DispatcherPriority.Loaded);
                            }
                        },
                        DispatcherPriority.Loaded);
                }
            },
            DispatcherPriority.Loaded);
    }

    /// <summary>The spell checking of the message, if the view model has it.</summary>
    public SpellCheckController? SpellCheck { get; private set; }

    /// <summary>
    ///  The context menu of the message: the one of <c>EditNetSpell</c> with the item of <c>Message_ContextMenuPopulating</c>,
    ///  or only the edit items without spell checking.
    /// </summary>
    private void UseMessageMenu()
    {
        AvaloniaEdit.TextEditor editor = message.Editor;
        if (SpellCheck is not null)
        {
            return;
        }

        if (_viewModel?.SpellCheck is { } spellCheck)
        {
            SpellCheck = new SpellCheckController(editor, spellCheck);
            SpellCheck.Pasted += (_, _) => FormatMessage();
            SpellCheck.ContextMenuPopulating += (_, menu) =>
            {
                int insertAt = menu.Items.OfType<Separator>().FirstOrDefault() is { } firstSeparator ? menu.Items.IndexOf(firstSeparator) : 0;
                MenuItem wordWrapItem = new() { Header = _viewModel?.Strings.WordWrapCommitMessageBody.AccessKeyText };
                wordWrapItem.Click += (_, _) => WordWrapBody();
                menu.Items.Insert(insertAt, wordWrapItem);
            };
            return;
        }

        ContextMenu messageMenu = new();
        MenuItem wordWrap = new();
        wordWrap.Click += (_, _) => WordWrapBody();
        MenuItem cutItem = new() { Header = "Cu_t" };
        cutItem.Click += (_, _) => editor.Cut();
        MenuItem copyItem = new() { Header = "_Copy" };
        copyItem.Click += (_, _) => editor.Copy();
        MenuItem pasteItem = new() { Header = "_Paste" };
        pasteItem.Click += (_, _) =>
        {
            editor.Paste();
            FormatMessage();
        };
        messageMenu.Items.Add(wordWrap);
        messageMenu.Items.Add(new Separator());
        messageMenu.Items.Add(cutItem);
        messageMenu.Items.Add(copyItem);
        messageMenu.Items.Add(pasteItem);
        messageMenu.Opening += (_, _) => wordWrap.Header = _viewModel?.Strings.WordWrapCommitMessageBody.AccessKeyText;
        editor.ContextMenu = messageMenu;
    }

    /// <summary>As <c>FormatAllText</c>: the edits of <see cref="CommitMessageFormatter"/>, as one undo step.</summary>
    public void FormatMessage()
    {
        if (_viewModel is { IsMessageEditable: true } viewModel)
        {
            ApplyEdits(CommitMessageFormatter.GetEdits(message.Editor.Text, viewModel.FormatOptions));
        }
    }

    /// <summary>As <c>WordWrapCommitMessageBody</c> (the item of the message's context menu).</summary>
    public void WordWrapBody()
    {
        if (_viewModel is { IsMessageEditable: true } viewModel)
        {
            ApplyEdits(CommitMessageFormatter.GetBodyWrapEdits(message.Editor.Text, viewModel.Options.MaxLineLength));
        }
    }

    private void ApplyEdits(IReadOnlyList<TextEdit> edits)
    {
        if (edits.Count == 0)
        {
            return;
        }

        AvaloniaEdit.Document.TextDocument document = message.Editor.Document;
        document.BeginUpdate();
        try
        {
            foreach (TextEdit edit in edits.OrderByDescending(e => e.Offset))
            {
                document.Replace(edit.Offset, edit.Length, edit.Text);
            }
        }
        finally
        {
            document.EndUpdate();
        }
    }

    private void OnMessageLoaded(object? sender, EventArgs e) => Dispatcher.UIThread.Post(FormatMessage);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CommitViewModel.IsMessageEditable):
                UpdateMessageEditable();
                break;
            case nameof(CommitViewModel.IsGpgSignSelected):
                UpdateCommitImage();
                break;
            case nameof(CommitViewModel.SelectionFilter):
                // As OnSelectionFilterTextChanged.
                _selectionFilterTimer.Stop();
                _selectionFilterTimer.Start();
                break;
        }
    }

    private void OnListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Presentation.UserControls.FileStatusList.FileStatusListViewModel.Filter))
        {
            UpdateFilterImages();
        }
    }

    /// <summary>As <c>gpgSignCommitChanged</c>.</summary>
    private void UpdateCommitImage()
        => commitImage.Source = new Bitmap(AssetLoader.Open(new Uri($"avares://GitUI.Avalonia/Assets/{(_viewModel?.IsGpgSignSelected == true ? "Key" : "RepoStateClean")}.png")));

    private void UpdateMessageEditable() => message.Editor.IsReadOnly = _viewModel?.IsMessageEditable != true;

    private void UpdateWatermark() => messageWatermark.IsVisible = string.IsNullOrEmpty(message.Editor.Text);

    private void UpdateCaret()
    {
        // As Message_SelectionChanged: the line and column of the status bar.
        AvaloniaEdit.Editing.Caret caret = message.Editor.TextArea.Caret;
        if (_viewModel is not null)
        {
            _viewModel.MessageCaret = (caret.Line, caret.Column);
            this.caret.Text = $"{_viewModel.Strings.CursorLine.Text} {caret.Line}  {_viewModel.Strings.CursorColumn.Text} {caret.Column}";
        }
    }

    /// <summary>As <c>Unstaged_FilterChanged</c> and <c>Staged_FilterChanged</c>: the buttons stage or unstage what is shown.</summary>
    private void UpdateFilterImages()
    {
        if (_viewModel is null)
        {
            return;
        }

        bool unstagedFiltered = _viewModel.Unstaged.IsFilterActive;
        stageAllImage.Source = LoadAsset(unstagedFiltered ? "StageAllFiltered" : "StageAll");
        ToolTip.SetTip(stageAllButton, (unstagedFiltered ? _viewModel.Strings.StageFiltered : _viewModel.Strings.StageAll).Text);
        bool stagedFiltered = _viewModel.Staged.IsFilterActive;
        unstageAllImage.Source = LoadAsset(stagedFiltered ? "UnstageAllFiltered" : "UnstageAll");
        ToolTip.SetTip(unstageAllButton, (stagedFiltered ? _viewModel.Strings.UnstageFiltered : _viewModel.Strings.UnstageAll).Text);

        static Bitmap LoadAsset(string name) => new(AssetLoader.Open(new Uri($"avares://GitUI.Avalonia/Assets/{name}.png")));
    }

    /// <summary>
    ///  As <c>commitTemplatesToolStripMenuItem_DropDownOpening</c>: the templates of the plugins and of the settings, the
    ///  Conventional Commits and the settings.
    /// </summary>
    private void FillCommitTemplatesMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        CommitViewModel viewModel = _viewModel;
        bool insertScope = _insertScope;
        MenuFlyout flyout = (MenuFlyout)commitTemplatesButton.Flyout!;
        flyout.Items.Clear();
        (IReadOnlyList<GitCommands.CommitTemplateItem> registered, IReadOnlyList<GitCommands.CommitTemplateItem> fromSettings) = viewModel.GetCommitTemplates();
        foreach (IReadOnlyList<GitCommands.CommitTemplateItem> templates in new[] { registered, fromSettings })
        {
            foreach (GitCommands.CommitTemplateItem template in templates)
            {
                MenuItem item = new() { Header = template.Name.Replace("_", "__") };
                item.Click += (_, _) => viewModel.ApplyTemplate(template);
                flyout.Items.Add(item);
            }

            if (templates.Count > 0)
            {
                flyout.Items.Add(new Separator());
            }
        }

        MenuItem conventional = new() { Header = viewModel.Strings.ConventionalCommit.AccessKeyText, Icon = CreateIcon("GitCommandLog") };
        foreach (string type in ConventionalCommits.HeaderTypes)
        {
            MenuItem item = new() { Header = type };
            item.Click += (_, _) => viewModel.ApplyConventionalType(type, insertScope);
            conventional.Items.Add(item);
        }

        conventional.Items.Add(new Separator());
        foreach (string footer in ConventionalCommits.FooterKeywords)
        {
            MenuItem item = new() { Header = footer };
            item.Click += (_, _) => viewModel.AddConventionalFooter($"{footer}: ");
            conventional.Items.Add(item);
        }

        MenuItem skipCi = new() { Header = ConventionalCommits.SkipCi };
        skipCi.Click += (_, _) => viewModel.AddConventionalFooter(ConventionalCommits.SkipCi, keepCursorPosition: true);
        conventional.Items.Add(skipCi);
        conventional.Items.Add(new Separator());
        conventional.Items.Add(new MenuItem
        {
            Header = viewModel.Strings.ConventionalCommitDocumentation.AccessKeyText,
            Icon = CreateIcon("Information"),
            Command = viewModel.OpenConventionalCommitsDocumentationCommand,
        });
        flyout.Items.Add(conventional);
        flyout.Items.Add(new Separator());
        flyout.Items.Add(new MenuItem
        {
            Header = viewModel.Strings.CommitMessageSettings.AccessKeyText,
            Icon = CreateIcon("Settings"),
            Command = viewModel.EditCommitTemplateSettingsCommand,
        });

        static Image CreateIcon(string name) => new() { Width = 16, Height = 16, Source = new Bitmap(AssetLoader.Open(new Uri($"avares://GitUI.Avalonia/Assets/{name}.png"))) };
    }

    /// <summary>The items of the templates menu (e.g. for tests), as when opened.</summary>
    public IReadOnlyList<object> OpenCommitTemplatesMenu()
    {
        FillCommitTemplatesMenu();
        return [.. ((MenuFlyout)commitTemplatesButton.Flyout!).Items.OfType<object>()];
    }

    /// <summary>As <c>CommitMessageToolStripMenuItemDropDownOpening</c>: the previous messages, and whose to show.</summary>
    private void FillCommitMessageMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        const int maxLabelLength = 72;
        MenuFlyout flyout = (MenuFlyout)commitMessageButton.Flyout!;
        flyout.Items.Clear();
        foreach (string previous in _viewModel.GetPreviousMessages())
        {
            string label = previous;
            int newlineIndex = label.IndexOf('\n');
            if (newlineIndex != -1)
            {
                label = label[..newlineIndex];
            }

            if (label.Length > maxLabelLength)
            {
                label = label[..(maxLabelLength - 3)] + "...";
            }

            MenuItem item = new() { Header = label.Replace("_", "__") };
            item.Click += (_, _) => _viewModel?.UsePreviousMessage(previous);
            flyout.Items.Add(item);
        }

        flyout.Items.Add(new Separator());
        MenuItem onlyMine = new()
        {
            Header = TranslatedText.ToAccessKeyText(_viewModel.Strings.ShowOnlyMyMessages.Text),
            ToggleType = MenuItemToggleType.CheckBox,
            IsChecked = _viewModel.ShowOnlyMyMessages,
        };
        onlyMine.Click += (_, _) => _viewModel?.ShowOnlyMyMessages = !_viewModel.ShowOnlyMyMessages;
        flyout.Items.Add(onlyMine);
    }
}
