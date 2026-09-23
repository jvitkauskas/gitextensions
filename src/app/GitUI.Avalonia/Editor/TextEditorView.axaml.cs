using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The Avalonia text editor on AvaloniaEdit (the editing mode of the WinForms <c>FileViewer</c>; docs/avalonia-port/PLAN.md,
///  phase 3): line numbers, syntax highlighting by file extension, whitespace markers and search (Ctrl+F).
/// </summary>
public partial class TextEditorView : UserControl
{
    private TextEditorViewModel? _viewModel;
    private bool _updatingText;
    private readonly DarkThemeHighlightingAdapter _darkThemeAdapter = new();
    private readonly DiffLineNumberMargin _diffLineNumbers = new();
    private readonly DiffColorizer _diffColorizer = new();
    private DiffBrushes? _diffBrushes;
    private DiffBackgroundRenderer? _diffBackground;
    private DiffAnchorRenderer? _diffAnchors;

    public TextEditorView()
    {
        InitializeComponent();
        SearchPanel.Install(editor);
        ActualThemeVariantChanged += (_, _) =>
        {
            if (_viewModel is not null)
            {
                ApplyOptions();
            }
        };
        editor.TextChanged += (_, _) =>
        {
            if (!_updatingText && _viewModel is not null)
            {
                _updatingText = true;
                try
                {
                    _viewModel.Text = editor.Text;
                }
                finally
                {
                    _updatingText = false;
                }
            }
        };
    }

    /// <summary>The AvaloniaEdit editor, e.g. for tests.</summary>
    public AvaloniaEdit.TextEditor Editor => editor;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.TextLoaded -= OnTextLoaded;
        _viewModel = DataContext as TextEditorViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel?.TextLoaded += OnTextLoaded;

        if (_viewModel is not null)
        {
            ApplyOptions();
            OnTextLoaded(this, EventArgs.Empty);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TextEditorViewModel.Text) when !_updatingText:
                SetText(_viewModel!.Text);
                break;

            case nameof(TextEditorViewModel.ShowLineNumbers):
                editor.ShowLineNumbers = _viewModel!.DiffLines is null && _viewModel.ShowLineNumbers;
                break;

            case nameof(TextEditorViewModel.IsReadOnly) or nameof(TextEditorViewModel.ShowWhitespace) or nameof(TextEditorViewModel.FileName):
                ApplyOptions();
                break;
        }
    }

    private void ApplyOptions()
    {
        TextEditorViewModel viewModel = _viewModel!;
        editor.IsReadOnly = viewModel.IsReadOnly;
        editor.Options.ShowSpaces = viewModel.ShowWhitespace;
        editor.Options.ShowTabs = viewModel.ShowWhitespace;
        editor.Options.ShowEndOfLine = viewModel.ShowWhitespace;
        editor.SyntaxHighlighting = viewModel.FileName is { } fileName && Path.GetExtension(fileName) is { Length: > 0 } extension
            ? HighlightingManager.Instance.GetDefinitionByExtension(extension)
            : null;

        // After the highlighting, which setting it may have added.
        editor.TextArea.TextView.LineTransformers.Remove(_darkThemeAdapter);
        if (editor.SyntaxHighlighting is not null && ActualThemeVariant == ThemeVariant.Dark)
        {
            editor.TextArea.TextView.LineTransformers.Add(_darkThemeAdapter);
        }
    }

    private void OnTextLoaded(object? sender, EventArgs e)
    {
        SetText(_viewModel!.Text);
        ShowDiff(_viewModel.DiffLines);

        // Show the requested line with the caret on it, or the start.
        if (_viewModel.LineToShow is int line && line > 0 && line <= editor.Document.LineCount)
        {
            editor.TextArea.Caret.Line = line;
            editor.ScrollToLine(line);
        }
        else
        {
            editor.TextArea.Caret.Offset = 0;
            editor.ScrollToHome();
        }
    }

    /// <summary>
    ///  A diff shows its line numbers in the old and new file, its added and removed lines colored (by git, or as
    ///  <c>DiffHighlightService.HighlightAddedAndDeletedLines</c>) and the in-line differences of the matching lines.
    /// </summary>
    private void ShowDiff(IReadOnlyList<DiffLine>? lines)
    {
        TextView textView = editor.TextArea.TextView;
        bool isDiff = lines is not null;
        editor.ShowLineNumbers = !isDiff && _viewModel!.ShowLineNumbers;
        editor.TextArea.LeftMargins.Remove(_diffLineNumbers);
        if (_diffBackground is not null)
        {
            textView.BackgroundRenderers.Remove(_diffBackground);
        }

        if (_diffAnchors is not null)
        {
            textView.BackgroundRenderers.Remove(_diffAnchors);
        }

        textView.LineTransformers.Remove(_diffColorizer);
        if (!isDiff)
        {
            return;
        }

        TextEditorViewModel viewModel = _viewModel!;
        editor.TextArea.LeftMargins.Insert(0, _diffLineNumbers);
        _diffLineNumbers.Lines = lines!;

        _diffBrushes ??= CreateDiffBrushes();
        _diffBackground ??= new DiffBackgroundRenderer(_diffBrushes.Added, _diffBrushes.Removed, _diffBrushes.Header);
        _diffAnchors ??= new DiffAnchorRenderer(_diffBrushes.AddedAnchor, _diffBrushes.RemovedAnchor);
        if (viewModel.GitColoring is null)
        {
            _diffBackground.Lines = lines!;
            textView.BackgroundRenderers.Add(_diffBackground);
        }

        // As DiffHighlightService.AddInlineDifferenceMarkers: git's colors on the text are dimmed unless git colors the background.
        bool dimBackground = viewModel.GitColoring is not { Reverse: false };
        _diffColorizer.Update(viewModel.GitColoring, viewModel.InlineDiffMarkers, dimBackground ? _diffBrushes.DimmedBack : _diffBrushes.DimmedFore);
        textView.LineTransformers.Add(_diffColorizer);
        _diffAnchors.Markers = viewModel.InlineDiffMarkers;
        textView.BackgroundRenderers.Add(_diffAnchors);
        textView.Redraw();
    }

    /// <summary>
    ///  The colors of a diff, as <c>DiffHighlightService</c>: the lines without git's colors (<c>HighlightAddedAndDeletedLines</c>),
    ///  and their identical parts twice dimmed on the background, or once dimmed on the text (<c>CreateDimmedMarker</c>).
    /// </summary>
    private DiffBrushes CreateDiffBrushes()
    {
        Color? addedBack = AppColorResources.GetColor(this, AppColor.AnsiTerminalGreenBackNormal);
        Color? removedBack = AppColorResources.GetColor(this, AppColor.AnsiTerminalRedBackNormal);
        Color? addedFore = AppColorResources.GetColor(this, AppColor.AnsiTerminalGreenForeBold);
        Color? removedFore = AppColorResources.GetColor(this, AppColor.AnsiTerminalRedForeBold);
        Color background = AppColorResources.GetColor(this, AppColor.EditorBackground) ?? Colors.White;
        return new DiffBrushes(
            Added: ToBrush(addedBack),
            Removed: ToBrush(removedBack),
            Header: AppColorResources.GetBrush(this, AppColor.DiffSection),
            DimmedBack: new InlineDiffBrushes(ToBrush(Dim(Dim(addedBack))), ToBrush(Dim(Dim(removedBack))), AddedFore: null, RemovedFore: null),
            DimmedFore: new InlineDiffBrushes(new SolidColorBrush(background), new SolidColorBrush(background), ToBrush(Dim(addedFore)), ToBrush(Dim(removedFore))),
            AddedAnchor: ToBrush(addedFore),
            RemovedAnchor: ToBrush(removedFore));

        Color? Dim(Color? color) => color is { } c ? AppColorResources.Dim(c, background) : null;

        static IBrush? ToBrush(Color? color) => color is { } c ? new SolidColorBrush(c) : null;
    }

    private sealed record DiffBrushes(
        IBrush? Added,
        IBrush? Removed,
        IBrush? Header,
        InlineDiffBrushes DimmedBack,
        InlineDiffBrushes DimmedFore,
        IBrush? AddedAnchor,
        IBrush? RemovedAnchor);

    private void SetText(string text)
    {
        if (editor.Text == text)
        {
            return;
        }

        _updatingText = true;
        try
        {
            editor.Text = text;
        }
        finally
        {
            _updatingText = false;
        }
    }
}
