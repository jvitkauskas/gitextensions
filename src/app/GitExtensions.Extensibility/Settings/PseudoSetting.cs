namespace GitExtensions.Extensibility.Settings;

/// <summary>
/// Not a real setting (as it save no setting value). It is used to display a text that is not a setting.
/// </summary>
/// <remarks>For a link, use <see cref="ActionSetting"/>.</remarks>
public class PseudoSetting : ISetting
{
    /// <param name="text">The text to show.</param>
    /// <param name="caption">The caption of the text.</param>
    /// <param name="height">The height of a multiline text, in pixels at 96 DPI; <see langword="null"/> for a single line.</param>
    public PseudoSetting(string text, string caption = "    ", int? height = null)
    {
        Text = text;
        Caption = caption;
        Height = height;
    }

    public string Name { get; } = "PseudoSetting";
    public string Caption { get; }

    /// <summary>The text shown (translated by the translations of the plugin, hence settable).</summary>
    public string Text { get; private set; }

    /// <summary>The height of a multiline text, in pixels at 96 DPI; <see langword="null"/> for a single line.</summary>
    public int? Height { get; }
}
