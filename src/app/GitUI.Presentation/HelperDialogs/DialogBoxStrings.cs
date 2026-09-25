using GitUI.Presentation.Translations;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>
///  Strings of the message boxes and task dialogs of Avalonia, shown off Windows instead of the native ones, which have the
///  captions of the system (docs/avalonia-port/CROSS-PLATFORM.md, phase 2). No WinForms form had them: their own category.
/// </summary>
public sealed class DialogBoxStrings : ViewStrings
{
    public DialogBoxStrings()
        : base("DialogBoxes")
    {
        Ok = Add("ok", "Text", "OK");
        Cancel = Add("cancel", "Text", "Cancel");
        Abort = Add("abort", "Text", "&Abort");
        Retry = Add("retry", "Text", "&Retry");
        Ignore = Add("ignore", "Text", "&Ignore");
        Yes = Add("yes", "Text", "&Yes");
        No = Add("no", "Text", "&No");
        Close = Add("close", "Text", "&Close");
        Help = Add("help", "Text", "Help");
        TryAgain = Add("tryAgain", "Text", "&Try Again");
        Continue = Add("continue", "Text", "C&ontinue");
        ShowDetails = Add("showDetails", "Text", "Show details");
        HideDetails = Add("hideDetails", "Text", "Hide details");
        ColorTitle = Add("colorTitle", "Text", "Color");
        FontTitle = Add("fontTitle", "Text", "Font");
        FontFamily = Add("fontFamily", "Text", "&Font:");
        FontSize = Add("fontSize", "Text", "&Size:");
        Bold = Add("bold", "Text", "&Bold");
        Italic = Add("italic", "Text", "&Italic");
        Sample = Add("sample", "Text", "Sample");
    }

    public TranslatedText ColorTitle { get; }

    public TranslatedText FontTitle { get; }

    public TranslatedText FontFamily { get; }

    public TranslatedText FontSize { get; }

    public TranslatedText Bold { get; }

    public TranslatedText Italic { get; }

    public TranslatedText Sample { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText Abort { get; }

    public TranslatedText Retry { get; }

    public TranslatedText Ignore { get; }

    public TranslatedText Yes { get; }

    public TranslatedText No { get; }

    public TranslatedText Close { get; }

    public TranslatedText Help { get; }

    public TranslatedText TryAgain { get; }

    public TranslatedText Continue { get; }

    public TranslatedText ShowDetails { get; }

    public TranslatedText HideDetails { get; }
}
