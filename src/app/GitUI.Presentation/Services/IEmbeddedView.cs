namespace GitUI.Presentation.Services;

/// <summary>
///  A view that a host control of an Avalonia window shows, e.g. a terminal emulator: a native child window
///  (<see cref="IEmbeddedNativeView"/>) or a control of the window itself (<see cref="IEmbeddedControlView"/>).
/// </summary>
public interface IEmbeddedView
{
}

/// <summary>
///  A view that is an Avalonia control (e.g. the built-in terminal), which the host shows as its content.
/// </summary>
public interface IEmbeddedControlView : IEmbeddedView
{
    /// <summary>The Avalonia control (typed <see cref="object"/>, as this assembly does not reference Avalonia).</summary>
    object Control { get; }
}
