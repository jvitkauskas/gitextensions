using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace GitUI.Avalonia.Editor;

/// <summary>
///  The options toolbar of a text editor, over the top right of its parent (the panel of the editor): shown while the mouse
///  moves over it, as the toolbar of <see cref="FileViewerView"/>.
/// </summary>
public partial class EditorOptionsToolbar : UserControl
{
    private Control? _area;

    public EditorOptionsToolbar()
    {
        InitializeComponent();
    }

    /// <summary>The toolbar itself, e.g. for tests.</summary>
    public Border Toolbar => toolbar;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _area = Parent as Control;
        _area?.PointerMoved += OnAreaPointerMoved;
        _area?.PointerExited += OnAreaPointerExited;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _area?.PointerMoved -= OnAreaPointerMoved;
        _area?.PointerExited -= OnAreaPointerExited;
        _area = null;
        base.OnDetachedFromVisualTree(e);
    }

    // Not over the search panel of the editor, which it would cover.
    private void OnAreaPointerMoved(object? sender, PointerEventArgs e)
        => toolbar.IsVisible = DataContext is not null && _area?.GetLogicalDescendants().OfType<TextEditorView>().FirstOrDefault()?.Search.IsOpened != true;

    private void OnAreaPointerExited(object? sender, PointerEventArgs e) => toolbar.IsVisible = false;
}
