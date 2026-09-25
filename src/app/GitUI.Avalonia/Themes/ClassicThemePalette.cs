using Avalonia.Controls;
using Avalonia.Media;
using Classic.CommonControls;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia;

/// <summary>Host colors for Classic's system brushes, including its raised and sunken edges.</summary>
internal static class ClassicThemePalette
{
    internal static ResourceDictionary Create(bool dark, IReadOnlyDictionary<string, uint>? colors)
    {
        // Classic also ships Simple-compatible resources, used by its DataGrid and color picker.
        ResourceDictionary resources = SimpleThemePalette.Create(dark, colors);
        Color surface = PaletteColor("ThemeBackground");
        Color foreground = PaletteColor("ThemeForeground");
        Color accent = PaletteColor("Highlight");
        Color accentText = PaletteColor("HighlightForeground");
        Color window = Get(ThemeColors.Window, dark ? 0xFF18191B : 0xFFFFFFFF);
        Color windowText = Get(ThemeColors.WindowText, foreground.ToUInt32());

        Set(SystemColors.ControlColorKey, SystemColors.ControlBrushKey, surface);
        Set(SystemColors.ControlTextColorKey, SystemColors.ControlTextBrushKey, foreground);
        Set(SystemColors.WindowColorKey, SystemColors.WindowBrushKey, window);
        Set(SystemColors.WindowTextColorKey, SystemColors.WindowTextBrushKey, windowText);
        Set(SystemColors.ControlLightColorKey, SystemColors.ControlLightBrushKey, Mix(surface, Colors.White, dark ? 0.05 : 0.35));
        Set(SystemColors.ControlLightLightColorKey, SystemColors.ControlLightLightBrushKey, Mix(surface, Colors.White, dark ? 0.15 : 0.80));
        Set(SystemColors.ControlDarkColorKey, SystemColors.ControlDarkBrushKey, Mix(surface, Colors.Black, dark ? 0.35 : 0.28));
        Set(SystemColors.ControlDarkDarkColorKey, SystemColors.ControlDarkDarkBrushKey, Mix(surface, Colors.Black, dark ? 0.65 : 0.50));
        Set(SystemColors.WindowFrameColorKey, SystemColors.WindowFrameBrushKey, PaletteColor("SimpleFocus"));
        Set(SystemColors.ActiveBorderColorKey, SystemColors.ActiveBorderBrushKey, PaletteColor("ThemeBorderMid"));
        Set(SystemColors.InactiveBorderColorKey, SystemColors.InactiveBorderBrushKey, PaletteColor("ThemeBorderLow"));
        Set(SystemColors.HighlightColorKey, SystemColors.HighlightBrushKey, accent);
        Set(SystemColors.HighlightTextColorKey, SystemColors.HighlightTextBrushKey, accentText);
        Set(SystemColors.GrayTextColorKey, SystemColors.GrayTextBrushKey, PaletteColor("ThemeBorderHigh"));
        Set(SystemColors.HotTrackColorKey, SystemColors.HotTrackBrushKey, Get(ThemeColors.HotTrack, PaletteColor("SimpleFocus").ToUInt32()));
        Set(SystemColors.MenuColorKey, SystemColors.MenuBrushKey, surface);
        Set(SystemColors.MenuBarColorKey, SystemColors.MenuBarBrushKey, surface);
        Set(SystemColors.MenuTextColorKey, SystemColors.MenuTextBrushKey, foreground);
        Set(SystemColors.MenuHighlightColorKey, SystemColors.MenuHighlightBrushKey, accent);
        Set(SystemColors.ScrollBarColorKey, SystemColors.ScrollBarBrushKey, PaletteColor("ThemeControlLow"));
        Set(SystemColors.AppWorkspaceColorKey, SystemColors.AppWorkspaceBrushKey, window);
        Set(SystemColors.DesktopColorKey, SystemColors.DesktopBrushKey, window);
        Set(SystemColors.ActiveCaptionColorKey, SystemColors.ActiveCaptionBrushKey, accent);
        Set(SystemColors.ActiveCaptionTextColorKey, SystemColors.ActiveCaptionTextBrushKey, accentText);
        Set(SystemColors.GradientActiveCaptionColorKey, SystemColors.GradientActiveCaptionBrushKey, accent);
        Set(SystemColors.InactiveCaptionColorKey, SystemColors.InactiveCaptionBrushKey, PaletteColor("ThemeControlMid"));
        Set(SystemColors.InactiveCaptionTextColorKey, SystemColors.InactiveCaptionTextBrushKey, foreground);
        Set(SystemColors.GradientInactiveCaptionColorKey, SystemColors.GradientInactiveCaptionBrushKey, PaletteColor("ThemeControlMid"));
        Set(SystemColors.InfoColorKey, SystemColors.InfoBrushKey, Color.FromUInt32(dark ? 0xFF3D3B24 : 0xFFFFFFE1));
        Set(SystemColors.InfoTextColorKey, SystemColors.InfoTextBrushKey, foreground);
        resources[SystemColors.InactiveSelectionHighlightBrushKey] = resources["ThemeAccentBrush3"];
        resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = resources["ThemeForegroundBrush"];
        return resources;

        Color PaletteColor(string name) => (Color)resources[name + "Color"]!;

        Color Get(string key, uint fallback) => Color.FromUInt32(colors is not null && colors.TryGetValue(key, out uint value) ? value : fallback);

        void Set(SystemResourceKey colorKey, SystemResourceKey brushKey, Color color)
        {
            resources[colorKey] = color;
            resources[brushKey] = new SolidColorBrush(color);
        }
    }

    private static Color Mix(Color background, Color foreground, double amount)
    {
        return Color.FromRgb(Channel(background.R, foreground.R), Channel(background.G, foreground.G), Channel(background.B, foreground.B));

        byte Channel(byte from, byte to) => (byte)Math.Round(from + ((to - from) * amount));
    }
}
