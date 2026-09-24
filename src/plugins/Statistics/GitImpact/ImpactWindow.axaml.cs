using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.GitImpact;

/// <summary>Avalonia port of <see cref="FormImpact"/>; the data lives in <see cref="ImpactViewModel"/>, the drawing in <see cref="ImpactGraphView"/>.</summary>
public partial class ImpactWindow : DialogWindow
{
    /// <summary>
    ///  How far the view is from the right end of the graph: as <c>UpdateScrollbar</c>, the graph stays scrolled to the most
    ///  recent weeks while it grows, unless scrolled.
    /// </summary>
    private double _distanceFromRight;

    public ImpactWindow()
    {
        InitializeComponent();

        graph.PointerMoved += (_, e) =>
        {
            // As Impact_MouseMove: the author stays selected outside the areas.
            if (graph.HitTestAuthor(e.GetPosition(graph)) is { } author && DataContext is ImpactViewModel viewModel)
            {
                viewModel.SelectedAuthor = author;
            }
        };

        // As ImpactControl_MouseWheel: the wheel scrolls horizontally.
        graphScroller.AddHandler(PointerWheelChangedEvent, (_, e) =>
        {
            graphScroller.Offset = new Vector(Math.Max(0, graphScroller.Offset.X + (e.Delta.Y * 120)), 0);
            e.Handled = true;
        }, handledEventsToo: false);

        graphScroller.ScrollChanged += (_, e) =>
        {
            if (e.ExtentDelta.X != 0 || e.ViewportDelta.X != 0)
            {
                graphScroller.Offset = new Vector(Math.Max(0, graphScroller.Extent.Width - graphScroller.Viewport.Width - _distanceFromRight), 0);
            }
            else
            {
                _distanceFromRight = Math.Max(0, graphScroller.Extent.Width - graphScroller.Viewport.Width - graphScroller.Offset.X);
            }
        };

        Opened += (_, _) => (DataContext as ImpactViewModel)?.Start();
        Closed += (_, _) => (DataContext as ImpactViewModel)?.Dispose();
        ActualThemeVariantChanged += (_, _) => UpdateAuthorColor();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ImpactViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateAuthorColor();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImpactViewModel.SelectedAuthor))
        {
            UpdateAuthorColor();
        }
    }

    /// <summary>As <c>UpdateAuthorInfo</c>: the color of the author (<c>pnlAuthorColor</c>).</summary>
    private void UpdateAuthorColor()
    {
        if (DataContext is ImpactViewModel { HasSelectedAuthor: true } viewModel)
        {
            authorColor.Background = new SolidColorBrush(ImpactGraphView.GetAuthorColor(viewModel.SelectedAuthor, ActualThemeVariant == ThemeVariant.Dark));
        }
    }
}
