using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  Hosts an <see cref="IEmbeddedNativeView"/> (e.g. a WinForms terminal control) as a child window of an Avalonia window.
/// </summary>
/// <remarks>
///  Used during the port to reuse WinForms controls that are not ported yet, such as the ConEmu/Mintty console
///  (docs/avalonia-port/PLAN.md). The native view owns its window; the host only parents and sizes it.
/// </remarks>
public class EmbeddedNativeViewHost : NativeControlHost
{
    public static readonly StyledProperty<IEmbeddedNativeView?> ViewProperty =
        AvaloniaProperty.Register<EmbeddedNativeViewHost, IEmbeddedNativeView?>(nameof(View));

    public IEmbeddedNativeView? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (View is not { } view)
        {
            return base.CreateNativeControlCore(parent);
        }

        return new PlatformHandle(view.Attach(parent.Handle), "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (View is { } view)
        {
            // The view owns its window (and destroys it when disposed); do not let Avalonia destroy it.
            view.Detach();
            return;
        }

        base.DestroyNativeControlCore(control);
    }
}
