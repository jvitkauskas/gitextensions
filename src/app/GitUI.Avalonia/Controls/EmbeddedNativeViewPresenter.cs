using Avalonia;
using Avalonia.Controls;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  Shows an <see cref="IEmbeddedView"/> that may be set later (e.g. a terminal created when its tab is first shown): the
///  control of an <see cref="IEmbeddedControlView"/>, or a new <see cref="EmbeddedNativeViewHost"/> for each
///  <see cref="IEmbeddedNativeView"/>, since the host creates its child window once.
/// </summary>
public class EmbeddedNativeViewPresenter : ContentControl
{
    public static readonly StyledProperty<IEmbeddedView?> ViewProperty =
        AvaloniaProperty.Register<EmbeddedNativeViewPresenter, IEmbeddedView?>(nameof(View));

    public IEmbeddedView? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ContentControl);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewProperty)
        {
            Content = View switch
            {
                IEmbeddedControlView view => (Control)view.Control,
                IEmbeddedNativeView view => new EmbeddedNativeViewHost { View = view },
                _ => null,
            };
        }
    }
}
