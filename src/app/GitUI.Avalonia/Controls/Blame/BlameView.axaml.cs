using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;
using GitUI.Presentation.UserControls.Blame;

namespace GitUI.Avalonia.Controls.Blame;

/// <summary>
///  The Avalonia blame (port of <c>BlameControl</c>): the author gutter is a margin of the file's editor, so both scroll
///  together (the WinForms control synchronizes two viewers).
/// </summary>
public partial class BlameView : UserControl
{
    // As BlameControl.GetAgeBucketGradientColors (https://colorbrewer2.org/#type=sequential&scheme=Greens&n=7).
    private static readonly Color[] AgeBucketColors =
    [
        Color.FromRgb(247, 252, 245),
        Color.FromRgb(199, 233, 192),
        Color.FromRgb(161, 217, 155),
        Color.FromRgb(116, 196, 118),
        Color.FromRgb(65, 171, 93),
        Color.FromRgb(35, 139, 69),
        Color.FromRgb(0, 68, 27),
    ];

    private readonly BlameMargin _margin = new();
    private readonly BlameHighlightRenderer _highlight = new();
    private BlameViewModel? _viewModel;
    private int _menuLine;

    public BlameView()
    {
        InitializeComponent();

        TextEditor editor = file.Editor;

        // After the line numbers (which AvaloniaEdit inserts first), as in the WinForms gutter.
        editor.TextArea.LeftMargins.Add(_margin);
        editor.TextArea.TextView.BackgroundRenderers.Add(_highlight);
        editor.TextArea.Caret.PositionChanged += (_, _) => _viewModel?.SelectLine(editor.TextArea.Caret.Line);
        editor.TextArea.TextView.PointerMoved += (_, e) => _viewModel?.HoverLine(GetLineAt(e));
        editor.TextArea.TextView.PointerExited += (_, _) => _viewModel?.HoverLine(0);
        _margin.PointerMoved += OnMarginPointerMoved;
        _margin.PointerExited += (_, _) =>
        {
            _viewModel?.HoverLine(0);
            ToolTip.SetIsOpen(_margin, false);
        };
        _margin.PointerPressed += OnMarginPointerPressed;
        file.ContextRequested += (_, e) => _menuLine = e.TryGetPosition(editor.TextArea.TextView, out Point position)
            ? _margin.GetLineAt(position.Y)
            : editor.TextArea.Caret.Line;
        menu.Opening += (_, _) => UpdateMenu();
        blameRevisionItem.Click += (_, _) => _viewModel?.BlameRevisionOf(_menuLine);
        blamePreviousRevisionItem.Click += (_, _) => _viewModel?.BlamePreviousRevisionOf(_menuLine);
        showChangesItem.Click += (_, _) => _viewModel?.ShowChangesOf(_menuLine);
        copyHashItem.Click += (_, _) => _viewModel?.CopyCommitHash(_menuLine);
        copyMessageItem.Click += (_, _) => _viewModel?.CopyCommitMessage(_menuLine);
        copyAllItem.Click += (_, _) => _viewModel?.CopyAllCommitInfo(_menuLine);
        ActualThemeVariantChanged += (_, _) => UpdateBrushes();
        UpdateBrushes();
    }

    /// <summary>The author gutter, e.g. for tests.</summary>
    internal BlameMargin Gutter => _margin;

    /// <summary>The file's editor, e.g. for tests.</summary>
    public TextEditor Editor => file.Editor;

    public ContextMenu Menu => menu;

    /// <summary>Opens the context menu for <paramref name="line"/> (1-based), as a right click on it.</summary>
    public void OpenMenuFor(int line)
    {
        _menuLine = line;
        UpdateMenu();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as BlameViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        if (_viewModel is null)
        {
            return;
        }

        BlameStrings strings = _viewModel.Strings;
        blameRevisionItem.Header = strings.BlameRevision.AccessKeyText;
        blamePreviousRevisionItem.Header = strings.BlameActualPreviousRevision.AccessKeyText;
        showChangesItem.Header = strings.ShowChanges.AccessKeyText;
        copyToClipboardItem.Header = strings.CopyToClipboard.AccessKeyText;
        copyHashItem.Header = strings.CommitHash.AccessKeyText;
        copyMessageItem.Header = strings.CommitMessage.AccessKeyText;
        copyAllItem.Header = strings.AllCommitInfo.AccessKeyText;
        if (!_viewModel.ShowCommitInfo)
        {
            commitInfoBorder.IsVisible = false;
            commitInfoSplitter.IsVisible = false;
            layout.RowDefinitions[0].Height = new GridLength(0);
        }

        UpdateBlame();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BlameViewModel.AuthorLines) or nameof(BlameViewModel.AgeBuckets):
                UpdateBlame();
                break;

            case nameof(BlameViewModel.HighlightedCommit):
                _margin.HighlightedCommit = _viewModel!.HighlightedCommit;
                _highlight.HighlightedCommit = _viewModel.HighlightedCommit;
                file.Editor.TextArea.TextView.InvalidateLayer(_highlight.Layer);
                break;
        }
    }

    private void UpdateBlame()
    {
        BlameViewModel viewModel = _viewModel!;
        IReadOnlyList<GitExtensions.Extensibility.Git.GitBlameLine> lines = viewModel.Blame?.Lines ?? [];
        _margin.Update(lines, viewModel.AuthorLines, viewModel.AgeBuckets);
        _highlight.Lines = lines;
        _highlight.HighlightedCommit = null;
        _margin.HighlightedCommit = null;
    }

    private void UpdateMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        BlameMenuState state = _viewModel.GetMenuState(_menuLine);
        bool hasCommit = _viewModel.GetCommit(_menuLine) is not null;
        blameRevisionItem.IsEnabled = state.CanBlameRevision;
        blamePreviousRevisionItem.IsEnabled = state.CanBlamePreviousRevision;
        blamePreviousRevisionItem.Header = (state.PreviousIsActual ? _viewModel.Strings.BlameActualPreviousRevision : _viewModel.Strings.BlameVisiblePreviousRevision).AccessKeyText;
        showChangesItem.IsEnabled = hasCommit;
        copyToClipboardItem.IsEnabled = hasCommit;
    }

    private int GetLineAt(PointerEventArgs e) => _margin.GetLineAt(e.GetPosition(file.Editor.TextArea.TextView).Y);

    private void OnMarginPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        int line = GetLineAt(e);
        _viewModel.HoverLine(line);
        string? tip = _viewModel.GetToolTip(line);
        if (!Equals(ToolTip.GetTip(_margin), tip))
        {
            ToolTip.SetTip(_margin, tip);
            ToolTip.SetIsOpen(_margin, tip is not null);
        }
    }

    private void OnMarginPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel is null || !e.GetCurrentPoint(_margin).Properties.IsLeftButtonPressed)
        {
            return;
        }

        int line = GetLineAt(e);
        if (line > 0)
        {
            file.Editor.TextArea.Caret.Line = line;
        }

        // As the WinForms author viewer: a double click blames the revision of the selected line.
        if (e.ClickCount == 2)
        {
            _viewModel.BlameSelectedLineRevision();
        }

        e.Handled = true;
    }

    private void UpdateBrushes()
    {
        bool dark = ActualThemeVariant == ThemeVariant.Dark;
        _margin.AgeBrushes = [.. AgeBucketColors.Select(color => new SolidColorBrush(dark ? InvertLightness(color) : color))];

        // As BlameControl._commitHighlightColor: a little lighter than the editor on dark themes (whatever its color), ControlLight otherwise.
        IBrush highlight = new SolidColorBrush(dark ? Color.FromArgb(24, 255, 255, 255) : Color.FromRgb(227, 227, 227));
        _margin.HighlightBrush = highlight;
        _highlight.Brush = highlight;
        _margin.InvalidateVisual();
        file.Editor.TextArea.TextView.InvalidateLayer(_highlight.Layer);
    }

    /// <summary>The dark theme's counterpart of a light background (as <c>ColorHelper.AdaptBackColor</c> roughly does).</summary>
    private static Color InvertLightness(Color color)
    {
        global::Avalonia.Media.HslColor hsl = color.ToHsl();
        return global::Avalonia.Media.HslColor.ToRgb(hsl.H, hsl.S, 1 - hsl.L, 1);
    }
}
