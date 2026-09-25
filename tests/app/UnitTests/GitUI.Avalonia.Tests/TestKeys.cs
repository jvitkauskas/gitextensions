using Avalonia.Input;

namespace GitUI.AvaloniaTests;

/// <summary>The modifiers the tests press for the shortcuts (docs/avalonia-port/CROSS-PLATFORM.md, phase 5).</summary>
internal static class TestKeys
{
    /// <summary>The modifier of the shortcuts: Cmd on macOS, Ctrl elsewhere (as <c>KeyMapping.CommandModifier</c>).</summary>
    public static RawInputModifiers Command => OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;
}
