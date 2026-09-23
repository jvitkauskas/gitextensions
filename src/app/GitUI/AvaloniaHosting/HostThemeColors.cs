using GitExtUtils.GitUI.Theming;
using GitUI.Presentation.Editor;
using GitUI.Theming;

namespace GitUI.AvaloniaHosting;

/// <summary>The theme colors of the running app for the Avalonia diff viewer, as the WinForms viewer gets them.</summary>
internal sealed class HostThemeColors : IThemeColors
{
    public static HostThemeColors Instance { get; } = new();

    public Color GetColor(AppColor color) => color.GetThemeColor();

    public bool IsDarkMode => Application.IsDarkModeEnabled;
}
