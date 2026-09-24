using Avalonia;
using Avalonia.Controls;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  Shows an <see cref="IEmbeddedNativeView"/> that may be set later (e.g. a terminal created when its tab is first shown): a
///  new <see cref="EmbeddedNativeViewHost"/> for each view, since the host creates its child window once.
/// </summary>
public class EmbeddedNativeViewPresenter : ContentControl
{
    public static readonly StyledProperty<IEmbeddedNativeView?> ViewProperty =
        AvaloniaProperty.Register<EmbeddedNativeViewPresenter, IEmbeddedNativeView?>(nameof(View));

    public IEmbeddedNativeView? View
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
            Content = View is { } view ? new EmbeddedNativeViewHost { View = view } : null;
        }
    }
}
