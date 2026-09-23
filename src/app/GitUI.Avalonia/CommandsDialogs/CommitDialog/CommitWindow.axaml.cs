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

    public CommitWindow()
    {
        InitializeComponent();

        // As OnShown: the files and the message are loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = _viewModel?.InitializeAsync());
        Activated += (_, _) => _viewModel?.OnActivated();

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
        ContextMenu messageMenu = new();
        MenuItem wordWrapItem = new();
        wordWrapItem.Click += (_, _) => WordWrapBody();
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
        messageMenu.Items.Add(wordWrapItem);
        messageMenu.Items.Add(new Separator());
        messageMenu.Items.Add(cutItem);
        messageMenu.Items.Add(copyItem);
        messageMenu.Items.Add(pasteItem);
        messageMenu.Opening += (_, _) => wordWrapItem.Header = _viewModel?.Strings.WordWrapCommitMessageBody.AccessKeyText;
        editor.ContextMenu = messageMenu;

        author.LostFocus += (_, _) => _viewModel?.UpdateAuthorInfo();
        ((MenuFlyout)commitMessageButton.Flyout!).Opening += (_, _) => FillCommitMessageMenu();
        ((MenuFlyout)commitTemplatesButton.Flyout!).Opening += (_, _) => FillCommitTemplatesMenu();
    }

    public FileStatusListView UnstagedFiles => unstagedFiles;

    public FileStatusListView StagedFiles => stagedFiles;

    public TextEditorView MessageEditor => message;

    public TextBlock Watermark => messageWatermark;

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
        UpdateMessageEditable();
        UpdateFilterImages();
        UpdateWatermark();
        UpdateCaret();
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
        if (e.PropertyName == nameof(CommitViewModel.IsMessageEditable))
        {
            UpdateMessageEditable();
        }
    }

    private void OnListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Presentation.UserControls.FileStatusList.FileStatusListViewModel.Filter))
        {
            UpdateFilterImages();
        }
    }

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
            item.Click += (_, _) => viewModel.ApplyConventionalType(type);
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
