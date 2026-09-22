using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>Strings of the commit message settings dialog; ids match <c>FormCommitTemplateSettings</c>.</summary>
public sealed class CommitTemplateSettingsStrings : ViewStrings
{
    public CommitTemplateSettingsStrings()
        : base("FormCommitTemplateSettings")
    {
        Title = Add("$this", "Text", "Commit message settings");
        EmptyTemplate = Add("_emptyTemplate", "Text", "empty");
        Ok = Add("buttonOk", "Text", "OK");
        Cancel = Add("buttonCancel", "Text", "Cancel");
        CommitTemplatesTab = Add("tabPage1", "Text", "Commit templates");
        CommitValidationTab = Add("tabPage2", "Text", "Commit validation");
        TemplateName = Add("labelCommitTemplateName", "Text", "Name:");
        TemplateText = Add("labelCommitTemplate", "Text", "Commit template:");
        EnableRegex = Add("checkBoxRegexEnabled", "Text", "Enable regex");
        EnableRegexTooltip = Add("checkBoxRegexEnabled", "toolTipRegex",
            """
                Use {{regex}}[regex group number] to extract branch name part
                Group number is optional, default is 1

                Examples on branch name: "feature/ABC-4587-commitMessageRegex"
                "Commit from: {{^feature/(.*)$}} branch" -> "Commit from: ABC-4587-commitMessageRegex branch"
                "{{([A-Z]+-\d+)}}: My message is" -> "ABC-4587: My message is "
                "Name: {{([A-Z]+-\d+)-(.*)}}[2], issue: {{([A-Z]+-\d+)-(.*)}}[1]" -> "Name: commitMessageRegex, issue: ABC-4587"
                """);
        MaxFirstLineLength = Add("labelMaxFirstLineLength", "Text", "Maximum number of characters in the first line (0 = check disabled):");
        MaxLineLength = Add("labelMaxLineLength", "Text", "Maximum number of characters per line (0 = check disabled):");
        AutoWrap = Add("labelAutoWrap", "Text", "Auto-wrap commit message (except subject line)");
        ValidationRegex = Add("labelRegExCheck", "Text", "Commit must match following RegEx (Empty = check disabled):");
        IndentAfterFirstLine = Add("labelUseIndent", "Text", "Indent lines after the first line:");
        SecondLineMustBeEmpty = Add("labelSecondLineEmpty", "Text", "Second line must be empty:");
    }

    public TranslatedText Title { get; }

    public TranslatedText EmptyTemplate { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText CommitTemplatesTab { get; }

    public TranslatedText CommitValidationTab { get; }

    public TranslatedText TemplateName { get; }

    public TranslatedText TemplateText { get; }

    public TranslatedText EnableRegex { get; }

    public TranslatedText EnableRegexTooltip { get; }

    public TranslatedText MaxFirstLineLength { get; }

    public TranslatedText MaxLineLength { get; }

    public TranslatedText AutoWrap { get; }

    public TranslatedText ValidationRegex { get; }

    public TranslatedText IndentAfterFirstLine { get; }

    public TranslatedText SecondLineMustBeEmpty { get; }
}
