using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class AppearanceFontsSettingsPageView : UserControl
{
    public AppearanceFontsSettingsPageView()
    {
        InitializeComponent();
    }
}

/// <summary>
///  As <c>SetFontButtonText</c>: a text block shown in a font of the settings (<see cref="SettingsFont"/>); without font, in
///  the font of its parent.
/// </summary>
public static class SettingsFontView
{
    public static readonly AttachedProperty<SettingsFont?> FontProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, SettingsFont?>("Font", typeof(SettingsFontView));

    static SettingsFontView()
    {
        FontProperty.Changed.AddClassHandler<TextBlock>((textBlock, e) => Apply(textBlock, e.GetNewValue<SettingsFont?>()));
    }

    public static SettingsFont? GetFont(TextBlock element) => element.GetValue(FontProperty);

    public static void SetFont(TextBlock element, SettingsFont? value) => element.SetValue(FontProperty, value);

    private static void Apply(TextBlock textBlock, SettingsFont? font)
    {
        if (font is null)
        {
            textBlock.ClearValue(TextBlock.FontFamilyProperty);
            textBlock.ClearValue(TextBlock.FontSizeProperty);
            textBlock.ClearValue(TextBlock.FontWeightProperty);
            textBlock.ClearValue(TextBlock.FontStyleProperty);
            return;
        }

        textBlock.FontFamily = new FontFamily(font.FamilyName);
        textBlock.FontSize = font.DisplaySize;
        textBlock.FontWeight = font.IsBold ? FontWeight.Bold : FontWeight.Normal;
        textBlock.FontStyle = font.IsItalic ? FontStyle.Italic : FontStyle.Normal;
    }
}
