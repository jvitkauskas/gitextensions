using GitCommands;

namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>
///  Snapshot of the settings edited by the commit message settings dialog.
/// </summary>
public sealed record CommitMessageSettings
{
    public int MaxFirstLineLength { get; init; }

    public int MaxLineLength { get; init; }

    public bool SecondLineMustBeEmpty { get; init; }

    public bool IndentAfterFirstLine { get; init; } = true;

    public bool AutoWrap { get; init; } = true;

    public string ValidationRegex { get; init; } = string.Empty;

    /// <summary>The configured templates, or <see langword="null"/> if none were ever saved.</summary>
    public IReadOnlyList<CommitTemplate>? Templates { get; init; }
}

/// <summary>A user-defined commit message template.</summary>
public sealed record CommitTemplate(string Name, string Text, bool IsRegex)
{
    public static CommitTemplate Empty { get; } = new(string.Empty, string.Empty, IsRegex: false);
}

/// <summary>Loads and persists <see cref="CommitMessageSettings"/>.</summary>
public interface ICommitMessageSettingsStore
{
    CommitMessageSettings Load();

    void Save(CommitMessageSettings settings);
}

/// <summary>
///  Stores <see cref="CommitMessageSettings"/> in <see cref="AppSettings"/>, exactly like <c>FormCommitTemplateSettings</c>.
/// </summary>
public sealed class AppSettingsCommitMessageSettingsStore : ICommitMessageSettingsStore
{
    public CommitMessageSettings Load()
        => new()
        {
            MaxFirstLineLength = AppSettings.CommitValidationMaxCntCharsFirstLine,
            MaxLineLength = AppSettings.CommitValidationMaxCntCharsPerLine,
            SecondLineMustBeEmpty = AppSettings.CommitValidationSecondLineMustBeEmpty,
            IndentAfterFirstLine = AppSettings.CommitValidationIndentAfterFirstLine,
            AutoWrap = AppSettings.CommitValidationAutoWrap,
            ValidationRegex = AppSettings.CommitValidationRegEx,
            Templates = CommitTemplateItem.LoadFromSettings()?.Select(t => new CommitTemplate(t.Name, t.Text, t.IsRegex)).ToList(),
        };

    public void Save(CommitMessageSettings settings)
    {
        AppSettings.CommitValidationMaxCntCharsFirstLine = settings.MaxFirstLineLength;
        AppSettings.CommitValidationMaxCntCharsPerLine = settings.MaxLineLength;
        AppSettings.CommitValidationSecondLineMustBeEmpty = settings.SecondLineMustBeEmpty;
        AppSettings.CommitValidationIndentAfterFirstLine = settings.IndentAfterFirstLine;
        AppSettings.CommitValidationRegEx = settings.ValidationRegex;
        CommitTemplateItem.SaveToSettings(settings.Templates?.Select(t => new CommitTemplateItem(t.Name, t.Text, icon: null, t.IsRegex)).ToArray());
        AppSettings.CommitValidationAutoWrap = settings.AutoWrap;
    }
}
