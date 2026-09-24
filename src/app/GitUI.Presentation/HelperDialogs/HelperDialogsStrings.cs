using GitUI.Presentation.Translations;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>Strings of the SSH authentication error dialog; ids match <c>FormPuttyError</c>.</summary>
public sealed class PuttyErrorStrings : ViewStrings
{
    public PuttyErrorStrings()
        : base("FormPuttyError")
    {
        Title = Add("$this", "Text", "Authentication error");
        PleaseLoadKey = Add("lblPleaseLoadKey", "Text", "Please load your SSH private key");
        MustAuthenticate = Add("lblMustAuthenticate", "Text", "You must authenticate to run this command.");
        LoadSshKey = Add("LoadSSHKey", "Text", "Load SSH key");
        Retry = Add("Retry", "Text", "Retry");
        Cancel = Add("Cancel", "Text", "Cancel");
    }

    public TranslatedText Title { get; }

    public TranslatedText PleaseLoadKey { get; }

    public TranslatedText MustAuthenticate { get; }

    public TranslatedText LoadSshKey { get; }

    public TranslatedText Retry { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>Strings of the branch multi-selection dialog; ids match <c>FormSelectMultipleBranches</c>.</summary>
public sealed class SelectMultipleBranchesStrings : ViewStrings
{
    public SelectMultipleBranchesStrings()
        : base("FormSelectMultipleBranches")
    {
        Title = Add("$this", "Text", "Select multiple branches");
        SelectBranches = Add("selectBranchesLabel", "Text", "Select branches");
        Ok = Add("okButton", "Text", "OK");
    }

    public TranslatedText Title { get; }

    public TranslatedText SelectBranches { get; }

    public TranslatedText Ok { get; }
}

/// <summary>Strings of the search window of the files (find file); ids match <c>SearchWindow</c>.</summary>
public sealed class SearchWindowStrings : ViewStrings
{
    public SearchWindowStrings()
        : base("SearchWindow")
    {
        EnterFileName = Add("lblEnterFileName", "Text", "Enter File Name");
    }

    public TranslatedText EnterFileName { get; }
}
