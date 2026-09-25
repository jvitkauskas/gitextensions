using Avalonia.Input;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  A shortcut of the XAML written with Ctrl, e.g. <c>Gesture="{h:PlatformGesture Ctrl+S}"</c>: with Cmd on macOS
///  (<see cref="KeyMapping.CommandModifier"/>).
/// </summary>
public sealed class PlatformGestureExtension(string gesture)
{
    public KeyGesture ProvideValue(IServiceProvider serviceProvider) => KeyMapping.ToPlatformGesture(gesture);
}
