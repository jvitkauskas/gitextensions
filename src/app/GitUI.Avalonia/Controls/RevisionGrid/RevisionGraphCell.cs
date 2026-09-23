using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;

namespace GitUI.Avalonia.Controls.RevisionGrid;

/// <summary>The graph cell of a revision grid row; draws the row once its graph is laid out.</summary>
public sealed class RevisionGraphCell : Control
{
    public static readonly StyledProperty<RevisionGraph?> GraphProperty =
        AvaloniaProperty.Register<RevisionGraphCell, RevisionGraph?>(nameof(Graph));

    public static readonly StyledProperty<RevisionGraphRenderer?> RendererProperty =
        AvaloniaProperty.Register<RevisionGraphCell, RevisionGraphRenderer?>(nameof(Renderer));

    static RevisionGraphCell()
    {
        AffectsRender<RevisionGraphCell>(GraphProperty, RendererProperty);
    }

    public RevisionGraph? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    public RevisionGraphRenderer? Renderer
    {
        get => GetValue(RendererProperty);
        set => SetValue(RendererProperty, value);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (DataContext is not RevisionGridRow row || Graph is not { } graph || Renderer is not { } renderer)
        {
            return;
        }

        using (context.PushClip(new Rect(Bounds.Size)))
        {
            renderer.DrawRow(
                context,
                graph.Config.MergeGraphLanesHavingCommonParent,
                graph.Config.RenderGraphWithDiagonals,
                row.Index,
                Bounds.Height,
                graph.GetSegmentsForRow,
                RevisionGraphDrawStyle.Normal,
                graph.HeadId);
        }
    }
}
