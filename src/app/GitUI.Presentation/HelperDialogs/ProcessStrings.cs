using GitUI.Presentation.Translations;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>Strings of the progress dialog; ids match <c>FormStatus</c> and its <c>PasswordInput</c> control.</summary>
public sealed class ProcessStrings : ViewStrings
{
    public ProcessStrings()
        : base("FormStatus")
    {
        Title = Add("$this", "Text", "Process");
        Ok = Add("Ok", "Text", "OK");
        Abort = Add("Abort", "Text", "&Abort");
        KeepDialogOpen = Add("KeepDialogOpen", "Text", "&Keep dialog open");
        ShowPassword = Add("ShowPassword", "Text", "Show &password input");
        SendInput = Add("SendInput", "Text", "Send input", category: "PasswordInput");
    }

    public TranslatedText Title { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Abort { get; }

    public TranslatedText KeepDialogOpen { get; }

    public TranslatedText ShowPassword { get; }

    public TranslatedText SendInput { get; }

    // Not translated in FormProcess either.
    public string FailedToRunCommand => "Failed to run command";
}
