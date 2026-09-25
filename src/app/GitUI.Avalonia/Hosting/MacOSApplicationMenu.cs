using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  The application menu of macOS (the menu named after the application in the menu bar): "About" and "Settings" (Cmd+,)
///  of the main window that was activated last, before the items of the system that Avalonia adds (Services, Hide, Quit)
///  (docs/avalonia-port/CROSS-PLATFORM.md, phase 5). The main window keeps its own menu too.
/// </summary>
[SupportedOSPlatform("macos")]
public static class MacOSApplicationMenu
{
    private static readonly NativeMenuItem _about = new();
    private static readonly NativeMenuItem _settings = new() { Gesture = new KeyGesture(Key.OemComma, KeyModifiers.Meta) };
    private static readonly NativeMenu _menu = new() { Items = { _about, new NativeMenuItemSeparator(), _settings } };
    private static Action? _showAbout;
    private static Action? _showSettings;

    static MacOSApplicationMenu()
    {
        _about.Click += (_, _) => _showAbout?.Invoke();
        _settings.Click += (_, _) => _showSettings?.Invoke();
    }

    /// <summary>Makes the items run the commands of a main window (when it is activated).</summary>
    public static void Attach(string aboutText, Action showAbout, string settingsText, Action showSettings)
    {
        _about.Header = aboutText;
        _settings.Header = settingsText;
        _showAbout = showAbout;
        _showSettings = showSettings;
        if (Application.Current is { } application && NativeMenu.GetMenu(application) != _menu)
        {
            NativeMenu.SetMenu(application, _menu);
        }
    }
}
