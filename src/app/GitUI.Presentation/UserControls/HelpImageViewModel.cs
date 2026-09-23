using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls;

/// <summary>Strings of the help image panel; ids match <c>HelpImageDisplayUserControl</c>.</summary>
public sealed class HelpImageStrings : ViewStrings
{
    public HelpImageStrings()
        : base("HelpImageDisplayUserControl")
    {
        HideHelp = Add("linkLabelHide", "Text", "Hide help");
        ShowHelp = Add("linkLabelShowHelp", "Text", "Show\r\nhelp");
    }

    public TranslatedText HideHelp { get; }

    public TranslatedText ShowHelp { get; }
}

/// <summary>
///  View model of a collapsible panel with a help image, which can show a second image while hovered
///  (port of <c>HelpImageDisplayUserControl</c>). The view supplies the images.
/// </summary>
public sealed partial class HelpImageViewModel : ObservableObject
{
    private readonly Action<bool> _saveIsExpanded;

    /// <param name="isExpanded">Whether the panel is expanded (persisted per panel, see <c>UniqueIsExpandedSettingsId</c>).</param>
    /// <param name="saveIsExpanded">Persists <see cref="IsExpanded"/>.</param>
    public HelpImageViewModel(HelpImageStrings strings, bool isVisible, bool isExpanded, Action<bool> saveIsExpanded)
    {
        Strings = strings;
        IsVisible = isVisible;
        IsExpanded = isExpanded;
        _saveIsExpanded = saveIsExpanded;
    }

    public HelpImageStrings Strings { get; }

    /// <summary>Whether help images are shown at all (<c>AppSettings.DontShowHelpImages</c>).</summary>
    public bool IsVisible { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Whether hovering shows the second image, e.g. the fast forward scenario of a merge.</summary>
    [ObservableProperty]
    public partial bool IsOnHoverShowImage2 { get; set; }

    /// <summary>The notice shown below the image while <see cref="IsOnHoverShowImage2"/>.</summary>
    [ObservableProperty]
    public partial string HoverNotice { get; set; } = "";

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    partial void OnIsExpandedChanged(bool value) => _saveIsExpanded?.Invoke(value);
}
