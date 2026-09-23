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
    private DiffBackgroundRenderer? _diffBackground;

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

    /// <summary>A diff shows its line numbers in the old and new file, and its added and removed lines colored.</summary>
    private void ShowDiff(IReadOnlyList<DiffLine>? lines)
    {
        TextView textView = editor.TextArea.TextView;
        bool isDiff = lines is not null;
        editor.ShowLineNumbers = !isDiff;
        if (!isDiff)
        {
            editor.TextArea.LeftMargins.Remove(_diffLineNumbers);
            if (_diffBackground is not null)
            {
                textView.BackgroundRenderers.Remove(_diffBackground);
                _diffBackground = null;
            }

            return;
        }

        if (!editor.TextArea.LeftMargins.Contains(_diffLineNumbers))
        {
            editor.TextArea.LeftMargins.Insert(0, _diffLineNumbers);
        }

        if (_diffBackground is null)
        {
            _diffBackground = new DiffBackgroundRenderer(CreateDiffBrushes());
            textView.BackgroundRenderers.Add(_diffBackground);
        }

        _diffLineNumbers.Lines = lines!;
        _diffBackground.Lines = lines!;
        _diffBackground.Markers = _viewModel!.InlineDiffMarkers;
        textView.InvalidateLayer(KnownLayer.Background);
    }

    /// <summary>
    ///  The colors of a patch without git's colors, as <c>DiffHighlightService</c>: the lines (<c>HighlightAddedAndDeletedLines</c>),
    ///  their identical parts twice dimmed and the anchors of insertions and deletions (<c>AddInlineDifferenceMarkers</c>).
    /// </summary>
    private DiffBrushes CreateDiffBrushes()
    {
        Color? added = AppColorResources.GetColor(this, AppColor.AnsiTerminalGreenBackNormal);
        Color? removed = AppColorResources.GetColor(this, AppColor.AnsiTerminalRedBackNormal);
        Color background = AppColorResources.GetColor(this, AppColor.EditorBackground) ?? Colors.White;
        return new DiffBrushes(
            Added: ToBrush(added),
            Removed: ToBrush(removed),
            Header: AppColorResources.GetBrush(this, AppColor.DiffSection),
            DimmedAdded: ToBrush(Dim(added)),
            DimmedRemoved: ToBrush(Dim(removed)),
            AddedAnchor: AppColorResources.GetBrush(this, AppColor.AnsiTerminalGreenForeBold),
            RemovedAnchor: AppColorResources.GetBrush(this, AppColor.AnsiTerminalRedForeBold));

        Color? Dim(Color? color)
            => color is { } c ? AppColorResources.Dim(AppColorResources.Dim(c, background), background) : null;

        static IBrush? ToBrush(Color? color) => color is { } c ? new SolidColorBrush(c) : null;
    }

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
