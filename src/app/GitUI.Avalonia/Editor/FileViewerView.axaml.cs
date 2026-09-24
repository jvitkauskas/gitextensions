using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    public FileViewerView()
    {
        InitializeComponent();

        // As internalFileViewer.MouseMove and MouseLeave: the toolbar shows while the mouse is over the text.
        textView.PointerMoved += (_, _) => toolbar.IsVisible = true;
        PointerExited += (_, _) => toolbar.IsVisible = false;
        nextChangeButton.Click += (_, _) => GoToChange(backwards: false);
        previousChangeButton.Click += (_, _) => GoToChange(backwards: true);

        ContextMenu menu = new();
        menu.Opening += (_, _) => FillContextMenu();
        textView.Editor.ContextMenu = menu;
    }

    /// <summary>The text editor, e.g. for tests.</summary>
    public TextEditorView TextView => textView;

    /// <summary>The image shown instead of the text, e.g. for tests.</summary>
    public Image ImageView => image;

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
        int firstVisibleLine = Math.Max(1, line - _viewModel.Settings.NumberOfContextLines - 1);
        editor.ScrollToVerticalOffset(editor.TextArea.TextView.GetVisualTopByDocumentLine(firstVisibleLine));
        editor.TextArea.Focus();
    }

    /// <summary>As <c>GoToLine</c>: the caret goes to the line, which is scrolled into view.</summary>
    public void GoToLine(int line)
    {
        AvaloniaEdit.TextEditor editor = textView.Editor;
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
        if (state.IsDiff)
        {
            Add(strings.IncreaseContextLinesMenu, () => viewModel.IncreaseContextLinesCommand.Execute(null), isEnabled: !viewModel.Settings.ShowEntireFile);
            Add(strings.DecreaseContextLinesMenu, () => viewModel.DecreaseContextLinesCommand.Execute(null), isEnabled: !viewModel.Settings.ShowEntireFile);
            Add(strings.ShowEntireFileMenu, () => viewModel.ToggleShowEntireFileCommand.Execute(null), isChecked: viewModel.Settings.ShowEntireFile);
        }

        Add(strings.ShowNonPrintingCharsMenu, () => viewModel.ToggleNonPrintingCharsCommand.Execute(null), isChecked: viewModel.Settings.ShowNonPrintingChars);
        if (state.IsDiff)
        {
            Add(strings.IgnoreWhitespaceAtEolMenu, () => viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.Eol), isChecked: viewModel.IgnoresWhitespaceAtEol);
            Add(strings.IgnoreWhitespaceChangesMenu, () => viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.Change), isChecked: viewModel.IgnoresWhitespaceChanges);
            Add(strings.IgnoreAllWhitespaceChangesMenu, () => viewModel.ToggleIgnoreWhitespaceCommand.Execute(IgnoreWhitespaceKind.AllSpace), isChecked: viewModel.IgnoresAllWhitespace);
        }

        menu.Items.Add(new Separator());
        Add(strings.Find, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.Find));
        if (!editor.IsReadOnly)
        {
            Add(strings.Replace, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.Replace));
        }

        Add(strings.GoToLine, () => ExecuteHotkeyCommand(FileViewerHotkeyCommand.GoToLine));
        return [.. menu.Items.OfType<object>()];

        void Add(TranslatedText header, Action action, bool isEnabled = true, bool? isChecked = null, string? icon = null)
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

            item.Click += (_, _) => action();
            menu.Items.Add(item);
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
                return true;
            case FileViewerHotkeyCommand.Replace when !editor.IsReadOnly:
                textView.OpenSearch(replace: true);
                return true;
            case FileViewerHotkeyCommand.FindNextOrOpenWithDifftool:
                textView.FindNext(backward: false);
                return true;
            case FileViewerHotkeyCommand.FindPrevious:
                textView.FindNext(backward: true);
                return true;
            case FileViewerHotkeyCommand.GoToLine:
                if (AskLineNumber(this, editor.Document.LineCount) is int line)
                {
                    GoToLine(line);
                }

                return true;
            case FileViewerHotkeyCommand.IncreaseNumberOfVisibleLines when viewModel.IsDiff && !viewModel.Settings.ShowEntireFile:
                viewModel.IncreaseContextLinesCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.DecreaseNumberOfVisibleLines when viewModel.IsDiff && !viewModel.Settings.ShowEntireFile:
                viewModel.DecreaseContextLinesCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.ShowEntireFile when viewModel.IsDiff:
                viewModel.ToggleShowEntireFileCommand.Execute(null);
                return true;
            case FileViewerHotkeyCommand.NextChange when viewModel.IsDiff:
                GoToChange(backwards: false);
                return true;
            case FileViewerHotkeyCommand.PreviousChange when viewModel.IsDiff:
                GoToChange(backwards: true);
                return true;
            case FileViewerHotkeyCommand.IgnoreAllWhitespace when viewModel.IsDiff:
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
            : null;
        HotkeyBinding? hotkey = command is null ? null : _viewModel?.Hotkeys.FirstOrDefault(h => h.CommandCode == (int)command);
        return hotkey is null ? null : KeyMapping.ToKeyGesture(hotkey.KeyData);
    }

    private static int? ShowGoToLineDialog(FileViewerView view, int maxLineNumber)
    {
        GoToLineViewModel viewModel = new(ViewStrings.Load<GoToLineStrings>(), maxLineNumber);
        nint owner = TopLevel.GetTopLevel(view)?.TryGetPlatformHandle()?.Handle ?? 0;
        return AvaloniaDialogHost.ShowDialog(new GoToLineWindow { DataContext = viewModel }, owner) ? viewModel.LineNumber : null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileViewerViewModel.Image))
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

        imageViewer.IsVisible = image.Source is not null;
        textView.IsVisible = !imageViewer.IsVisible;
    }
}
