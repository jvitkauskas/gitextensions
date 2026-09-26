using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaHex;
using AvaloniaHex.Document;
using GitCommands.Settings;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The Avalonia file viewer (the viewing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md, phase 3):
///  the text editor for texts and diffs, or an image, with the context menu and the hotkeys of <c>FileViewer</c>.
/// </summary>
public partial class FileViewerView : UserControl, IHotkeyControl
{
    private FileViewerViewModel? _viewModel;
    private TopLevel? _topLevel;
    private bool _altPressed;

    public FileViewerView()
    {
        InitializeComponent();

        // As internalFileViewer.MouseMove and MouseLeave: the toolbar shows while the mouse is over the text, unless the search
        // panel is open (which it would cover; the WinForms search is a separate window).
        textView.PointerMoved += (_, _) => toolbar.IsVisible = !textView.Search.IsOpened;
        PointerExited += (_, _) => toolbar.IsVisible = false;
        nextChangeButton.Click += (_, _) => GoToChange(backwards: false);
        previousChangeButton.Click += (_, _) => GoToChange(backwards: true);

        ContextMenu menu = new();
        menu.Opening += (_, _) => FillContextMenu();
        textView.Editor.ContextMenu = menu;

        // As TextArea_MouseWheel and PictureBox_MouseWheel: the wheel at the end (or start) scrolls on into the next (or
        // previous) file; before the editor scrolls.
        textView.Editor.AddHandler(PointerWheelChangedEvent, OnEditorWheel, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        imageViewer.AddHandler(PointerWheelChangedEvent, (_, e) => OnWheel(e, atTop: true, atBottom: true), global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // The width of the output of difftastic depends on the width of the viewer (GetDifftasticArguments).
        textView.SizeChanged += (_, e) =>
        {
            if (_viewModel is not null)
            {
                _viewModel.ViewerWidth = e.NewSize.Width;
            }
        };
    }

    private void OnEditorWheel(object? sender, global::Avalonia.Input.PointerWheelEventArgs e)
    {
        AvaloniaEdit.TextEditor editor = textView.Editor;
        OnWheel(e, atTop: editor.VerticalOffset <= 0, atBottom: editor.VerticalOffset + editor.ViewportHeight >= editor.ExtentHeight - 1);
    }

    private void OnWheel(global::Avalonia.Input.PointerWheelEventArgs e, bool atTop, bool atBottom)
    {
        // Shift scrolls horizontally.
        if (_viewModel is null || e.KeyModifiers.HasFlag(global::Avalonia.Input.KeyModifiers.Shift))
        {
            return;
        }

        bool withAlt = _altPressed || e.KeyModifiers.HasFlag(global::Avalonia.Input.KeyModifiers.Alt);
        if (e.Delta.Y > 0 && atTop)
        {
            _viewModel.OnScrollReached(bottom: false, withAlt);
        }
        else if (e.Delta.Y < 0 && atBottom)
        {
            _viewModel.OnScrollReached(bottom: true, withAlt);
        }
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The wheel events of Windows do not have Alt among their modifiers (Control.ModifierKeys in the WinForms viewer).
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKey, global::Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        _topLevel?.AddHandler(KeyUpEvent, OnTopLevelKey, global::Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
        (_topLevel as WindowBase)?.Deactivated += OnTopLevelDeactivated;
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKey);
        _topLevel?.RemoveHandler(KeyUpEvent, OnTopLevelKey);
        (_topLevel as WindowBase)?.Deactivated -= OnTopLevelDeactivated;
        _topLevel = null;
        _altPressed = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnTopLevelKey(object? sender, global::Avalonia.Input.KeyEventArgs e)
        => _altPressed = e.Key is global::Avalonia.Input.Key.LeftAlt or global::Avalonia.Input.Key.RightAlt
            ? e.RoutedEvent == KeyDownEvent
            : e.KeyModifiers.HasFlag(global::Avalonia.Input.KeyModifiers.Alt);

    private void OnTopLevelDeactivated(object? sender, EventArgs e) => _altPressed = false;

    /// <summary>The text editor, e.g. for tests.</summary>
    public TextEditorView TextView => textView;

    /// <summary>The image shown instead of the text, e.g. for tests.</summary>
    public Image ImageView => image;

    /// <summary>The bytes of a binary file shown instead of the text, e.g. for tests.</summary>
    public HexEditor HexView => hexEditor;

    /// <summary>The toolbar of the options, e.g. for tests.</summary>
    public Border Toolbar => toolbar;

    /// <summary>Shows the go to line dialog and returns the chosen line (replaced by tests).</summary>
    public Func<FileViewerView, int, int?> AskLineNumber { get; set; } = ShowGoToLineDialog;

    /// <summary>
    ///  As <c>NextChangeButtonClick</c> and <c>PreviousChangeButtonClick</c>: the caret goes to the next (or previous) change,
    ///  shown below its lines of context.
    /// </summary>
    public void GoToChange(bool backwards)
    {
        AvaloniaEdit.TextEditor editor = textView.Editor;
        if (_viewModel?.GetChangeLine(editor.TextArea.Caret.Line, backwards) is not int line)
        {
            return;
        }

        editor.TextArea.Caret.Line = line;
        editor.TextArea.Caret.Column = 1;
        textView.ScrollToFirstVisibleLine(Math.Max(1, line - _viewModel.Settings.NumberOfContextLines - 1));
        editor.TextArea.Focus();
    }

    /// <summary>
    ///  As <c>GoToLine</c>: the caret goes to the line, which is scrolled into view; in a diff, the line of the new file (or the one
    ///  after it, <c>GetCaretOffset</c>).
    /// </summary>
    public void GoToLine(int line)
    {
        AvaloniaEdit.TextEditor editor = textView.Editor;
        if (_viewModel?.Editor.DiffLines is { } diffLines)
        {
            line = ViewPositionCache.GetLineInDiff(diffLines, line, rightFile: true);
        }

        line = Math.Clamp(line, 1, editor.Document.LineCount);
        editor.TextArea.Caret.Line = line;
        editor.TextArea.Caret.Column = 1;
        editor.ScrollToLine(line);
        editor.TextArea.Focus();
    }

    /// <summary>
    ///  As <c>ContextMenu_Opening</c> and <c>SetVisibilityDiffContextMenu</c>: the items that apply to what is shown.
    /// </summary>
    /// <returns>The items (e.g. for tests).</returns>
    public IReadOnlyList<object> FillContextMenu()
    {
        ContextMenu menu = textView.Editor.ContextMenu!;
        menu.Items.Clear();
        if (_viewModel is not { } viewModel)
        {
            return [];
        }

        FileViewerStrings strings = viewModel.Strings;
        FileViewerMenuState state = viewModel.GetMenuState();
        AvaloniaEdit.TextEditor editor = textView.Editor;
        if (state.CanStage)
        {
            Add(strings.StageSelectedLines, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.StageLines), icon: "Stage");
        }

        if (state.CanUnstage)
        {
            Add(strings.UnstageSelectedLines, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.UnstageLines), icon: "Unstage");
        }

        if (state.CanReset)
        {
            Add(strings.ResetSelectedLines, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.ResetLines), icon: "ResetWorkingDirChanges");
        }

        Add(strings.Copy, () => viewModel.Copy(editor.SelectedText, editor.SelectionStart));
        if (state.CanCopyPatch)
        {
            Add(strings.CopyPatch, () => viewModel.CopyPatch(editor.SelectedText));
            Add(strings.CopyNewVersion, () => viewModel.CopyVersion(editor.SelectedText, editor.SelectionStart, newVersion: true));
            Add(strings.CopyOldVersion, () => viewModel.CopyVersion(editor.SelectedText, editor.SelectionStart, newVersion: false));
        }

        menu.Items.Add(new Separator());
        if (viewModel.CanChangeContextLines)
        {
            Add(strings.IncreaseContextLinesMenu, () => viewModel.IncreaseContextLinesCommand.Execute(null), isEnabled: !viewModel.Settings.ShowEntireFile, icon: "NumberOfLinesIncrease");
            Add(strings.DecreaseContextLinesMenu, () => viewModel.DecreaseContextLinesCommand.Execute(null), isEnabled: !viewModel.Settings.ShowEntireFile, icon: "NumberOfLinesDecrease");
            Add(strings.ShowEntireFileMenu, () => viewModel.ToggleShowEntireFileCommand.Execute(null), isChecked: viewModel.Settings.ShowEntireFile);
        }

        Add(strings.ShowNonPrintingCharsMenu, () => viewModel.ToggleNonPrintingCharsCommand.Execute(null), isChecked: viewModel.Settings.ShowNonPrintingChars);
        if (viewModel.CanChangeContextLines)
        {
            Add(strings.ShowSyntaxHighlightingMenu, () => viewModel.ToggleSyntaxHighlightingCommand.Execute(null), isChecked: viewModel.Settings.ShowSyntaxHighlighting);
        }

        if (viewModel.CanIgnoreWhitespaceAtEol)
        {
            Add(strings.IgnoreWhitespaceAtEolMenu, () => viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.Eol), isChecked: viewModel.IgnoresWhitespaceAtEol);
        }

        if (viewModel.CanIgnoreWhitespaceChanges)
        {
            Add(strings.IgnoreWhitespaceChangesMenu, () => viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.Change), isChecked: viewModel.IgnoresWhitespaceChanges);
            Add(strings.IgnoreAllWhitespaceChangesMenu, () => viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.AllSpace), isChecked: viewModel.IgnoresAllWhitespace);
        }

        if (viewModel.IsDiffAppearanceVisible)
        {
            // As diffAppearanceToolStripMenuItem: the patch, git's word diff or difftastic (if configured).
            MenuItem appearance = Add(strings.DiffAppearance, action: null, icon: "Diff");
            AddTo(appearance.Items, strings.ShowPatch, () => viewModel.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.Patch),
                isChecked: viewModel.DiffAppearance is not (DiffDisplayAppearance.GitWordDiff or DiffDisplayAppearance.Difftastic));
            AddTo(appearance.Items, strings.ShowGitWordColoring, () => viewModel.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.GitWordDiff),
                isChecked: viewModel.DiffAppearance == DiffDisplayAppearance.GitWordDiff);
            AddTo(appearance.Items, strings.ShowDifftastic, () => viewModel.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.Difftastic),
                isEnabled: viewModel.IsDifftasticEnabled, isChecked: viewModel.DiffAppearance == DiffDisplayAppearance.Difftastic);
        }

        menu.Items.Add(new Separator());
        if (state.IsDiff)
        {
            Add(strings.TreatAllFilesAsText, () => viewModel.ToggleTreatAllFilesAsTextCommand.Execute(null), isChecked: viewModel.TreatAllFilesAsText);
        }

        if (viewModel.EnableAutomaticContinuousScroll)
        {
            Add(strings.AutomaticContinuousScroll, () => viewModel.ToggleAutomaticContinuousScrollCommand.Execute(null), isChecked: viewModel.Settings.AutomaticContinuousScroll, icon: "UiScrollBar");
        }

        Add(strings.Find, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.Find), icon: "Preview");
        if (!editor.IsReadOnly)
        {
            Add(strings.Replace, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.Replace));
        }

        Add(strings.GoToLine, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.GoToLine));
        return [.. menu.Items.OfType<object>()];

        MenuItem Add(TranslatedText header, Action? action, bool isEnabled = true, bool? isChecked = null, string? icon = null)
            => AddTo(menu.Items, header, action, isEnabled, isChecked, icon);

        MenuItem AddTo(ItemCollection items, TranslatedText header, Action? action, bool isEnabled = true, bool? isChecked = null, string? icon = null)
        {
            MenuItem item = new()
            {
                Header = header.AccessKeyText,
                IsEnabled = isEnabled,
                InputGesture = GetGesture(header, strings),
            };
            if (isChecked is bool check)
            {
                item.ToggleType = MenuItemToggleType.CheckBox;
                item.IsChecked = check;
            }

            if (icon is not null)
            {
                item.Icon = new Image { Width = 16, Height = 16, Source = new Bitmap(AssetLoader.Open(new Uri($"avares://GitUI.Avalonia/Assets/{icon}.png"))) };
            }

            if (action is not null)
            {
                item.Click += (_, _) => action();
            }

            items.Add(item);
            return item;
        }
    }

    /// <summary>As <c>ProcessHotkey</c> of the viewer: the configured "FileViewer" hotkeys.</summary>
    public bool ProcessHotkey(int keyData)
    {
        if (_viewModel?.Hotkeys.FirstOrDefault(h => h.KeyData == keyData) is not { } hotkey)
        {
            return false;
        }

        // In an editable text, the keys that edit it are not hotkeys.
        if (!textView.Editor.IsReadOnly && KeyMapping.IsTextEditKey(keyData))
        {
            return false;
        }

        return ExecuteHotkeyCommand((FileViewerHotkeyCommand)hotkey.CommandCode);
    }

    /// <summary>As <c>ExecuteCommand</c> of <c>FileViewer</c>.</summary>
    /// <returns><see langword="false"/> if the command does not apply (the key is then not handled).</returns>
    public bool ExecuteHotkeyCommand(FileViewerHotkeyCommand command)
    {
        if (_viewModel is not { } viewModel)
        {
            return false;
        }

        AvaloniaEdit.TextEditor editor = textView.Editor;
        switch (command)
        {
            // As Find and FindNextAsync of FileViewerInternal (FindAndReplaceForm).
            case FileViewerHotkeyCommand.Find:
                textView.OpenSearch(replace: false);
                toolbar.IsVisible = false;
                return true;
            case FileViewerHotkeyCommand.Replace when !editor.IsReadOnly:
                textView.OpenSearch(replace: true);
                toolbar.IsVisible = false;
                return true;
            case FileViewerHotkeyCommand.FindNextOrOpenWithDifftool:
                // As FindNextAsync: without a search, the changes open in the difftool.
                if (viewModel.CanOpenWithDifftool && string.IsNullOrEmpty(textView.Search.SearchPattern))
                {
                    viewModel.OpenWithDifftool();
                    return true;
                }

                textView.FindNext(backward: false);
                return true;
            case FileViewerHotkeyCommand.FindPrevious:
                textView.FindNext(backward: true);
                return true;
            case FileViewerHotkeyCommand.GoToLine:
                // As MaxLineNumber: in a diff, the last line number of the files.
                int maxLineNumber = viewModel.Editor.DiffLines is { Count: > 0 } diffLines
                    ? Math.Max(1, diffLines.Max(l => Math.Max(l.LeftLineNumber, l.RightLineNumber)))
                    : editor.Document.LineCount;
                if (AskLineNumber(this, maxLineNumber) is int line)
                {
                    GoToLine(line);
                }

                return true;
            case FileViewerHotkeyCommand.IncreaseNumberOfVisibleLines when viewModel.CanChangeContextLines && !viewModel.Settings.ShowEntireFile:
                viewModel.IncreaseContextLinesCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.DecreaseNumberOfVisibleLines when viewModel.CanChangeContextLines && !viewModel.Settings.ShowEntireFile:
                viewModel.DecreaseContextLinesCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.ShowEntireFile when viewModel.CanChangeContextLines:
                viewModel.ToggleShowEntireFileCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.ShowSyntaxHighlighting when viewModel.CanChangeContextLines:
                viewModel.ToggleSyntaxHighlightingCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.ShowGitWordColoring when viewModel.IsDiffAppearanceVisible:
                viewModel.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.GitWordDiff);
                return true;
            case FileViewerHotkeyCommand.ShowDifftastic when viewModel.IsDiffAppearanceVisible && viewModel.IsDifftasticEnabled:
                viewModel.ChangeDiffAppearanceCommand.Execute(DiffDisplayAppearance.Difftastic);
                return true;
            case FileViewerHotkeyCommand.TreatFileAsText when viewModel.IsDiff:
                viewModel.ToggleTreatAllFilesAsTextCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.NextOccurrence:
                textView.GoToOccurrence(backwards: false);
                return true;
            case FileViewerHotkeyCommand.PreviousOccurrence:
                textView.GoToOccurrence(backwards: true);
                return true;
            case FileViewerHotkeyCommand.NextChange when viewModel.IsDiff:
                GoToChange(backwards: false);
                return true;
            case FileViewerHotkeyCommand.PreviousChange when viewModel.IsDiff:
                GoToChange(backwards: true);
                return true;
            case FileViewerHotkeyCommand.IgnoreAllWhitespace when viewModel.CanIgnoreWhitespaceChanges:
                viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.AllSpace);
                return true;
            case FileViewerHotkeyCommand.StageLines:
                return viewModel.ApplyLinePatch(LinePatchOperation.Stage, editor.SelectionStart, editor.SelectionLength);
            case FileViewerHotkeyCommand.UnstageLines:
                return viewModel.ApplyLinePatch(LinePatchOperation.Unstage, editor.SelectionStart, editor.SelectionLength);
            case FileViewerHotkeyCommand.ResetLines:
                return viewModel.ApplyLinePatch(LinePatchOperation.Reset, editor.SelectionStart, editor.SelectionLength);
            default:
                return false;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as FileViewerViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.ViewerWidth = textView.Bounds.Width;
        ShowImage();
    }

    /// <summary>The hotkey of a menu item, shown beside it (as the <c>ShortcutKeyDisplayString</c> of <c>ReloadHotkeys</c>).</summary>
    private global::Avalonia.Input.KeyGesture? GetGesture(TranslatedText header, FileViewerStrings strings)
    {
        FileViewerHotkeyCommand? command = header == strings.StageSelectedLines ? FileViewerHotkeyCommand.StageLines
            : header == strings.UnstageSelectedLines ? FileViewerHotkeyCommand.UnstageLines
            : header == strings.ResetSelectedLines ? FileViewerHotkeyCommand.ResetLines
            : header == strings.Find ? FileViewerHotkeyCommand.Find
            : header == strings.Replace ? FileViewerHotkeyCommand.Replace
            : header == strings.GoToLine ? FileViewerHotkeyCommand.GoToLine
            : header == strings.IncreaseContextLinesMenu ? FileViewerHotkeyCommand.IncreaseNumberOfVisibleLines
            : header == strings.DecreaseContextLinesMenu ? FileViewerHotkeyCommand.DecreaseNumberOfVisibleLines
            : header == strings.ShowEntireFileMenu ? FileViewerHotkeyCommand.ShowEntireFile
            : header == strings.IgnoreAllWhitespaceChangesMenu ? FileViewerHotkeyCommand.IgnoreAllWhitespace
            : header == strings.ShowSyntaxHighlightingMenu ? FileViewerHotkeyCommand.ShowSyntaxHighlighting
            : header == strings.ShowGitWordColoring ? FileViewerHotkeyCommand.ShowGitWordColoring
            : header == strings.ShowDifftastic ? FileViewerHotkeyCommand.ShowDifftastic
            : header == strings.TreatAllFilesAsText ? FileViewerHotkeyCommand.TreatFileAsText
            : null;
        HotkeyBinding? hotkey = command is null ? null : _viewModel?.Hotkeys.FirstOrDefault(h => h.CommandCode == (int)command);
        return hotkey is null ? null : KeyMapping.ToKeyGesture(hotkey.KeyData);
    }

    private static int? ShowGoToLineDialog(FileViewerView view, int maxLineNumber)
    {
        GoToLineViewModel viewModel = new(ViewStrings.Load<GoToLineStrings>(), maxLineNumber);
        TopLevel? topLevel = TopLevel.GetTopLevel(view);
        nint owner = topLevel is DialogWindow window ? window.OwnerHandle : topLevel?.TryGetPlatformHandle()?.Handle ?? 0;
        return AvaloniaDialogHost.ShowDialog(new GoToLineWindow { DataContext = viewModel }, owner) ? viewModel.LineNumber : null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FileViewerViewModel.Image) or nameof(FileViewerViewModel.BinaryData))
        {
            ShowImage();
        }
    }

    private void ShowImage()
    {
        (image.Source as IDisposable)?.Dispose();
        image.Source = null;
        if (_viewModel?.Image is { } bytes)
        {
            try
            {
                using MemoryStream stream = new(bytes);
                image.Source = new Bitmap(stream);
            }
            catch (Exception)
            {
                // Not an image Avalonia can decode (as FileViewer.CreateImage, which shows the text then).
            }
        }

        // A binary file is read-only: its bytes are shown, not edited.
        byte[]? data = _viewModel?.BinaryData;
        hexEditor.Document = data is null ? null : new MemoryBinaryDocument(data, isReadOnly: true);
        binaryViewer.IsVisible = data is not null;

        imageViewer.IsVisible = image.Source is not null;
        textView.IsVisible = !imageViewer.IsVisible && !binaryViewer.IsVisible;
    }
}
