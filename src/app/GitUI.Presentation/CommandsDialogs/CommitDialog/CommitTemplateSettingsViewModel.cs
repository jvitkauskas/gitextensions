using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GitUI.Presentation.CommandsDialogs.CommitDialog;

/// <summary>
///  View model of the commit message settings dialog (port of <c>GitUI.CommandsDialogs.CommitDialog.FormCommitTemplateSettings</c>).
/// </summary>
public sealed partial class CommitTemplateSettingsViewModel : DialogViewModel
{
    /// <summary>Number of template slots offered to the user.</summary>
    public const int TemplateSlotCount = 10;

    private readonly ICommitMessageSettingsStore _store;

    public CommitTemplateSettingsViewModel(CommitTemplateSettingsStrings strings, ICommitMessageSettingsStore store)
    {
        Strings = strings;
        _store = store;

        CommitMessageSettings settings = store.Load();

        MaxFirstLineLength = settings.MaxFirstLineLength;
        MaxLineLength = settings.MaxLineLength;
        SecondLineMustBeEmpty = settings.SecondLineMustBeEmpty;
        IndentAfterFirstLine = settings.IndentAfterFirstLine;
        AutoWrap = settings.AutoWrap;
        ValidationRegex = settings.ValidationRegex;

        Templates = new ReadOnlyObservableCollection<CommitTemplateSlotViewModel>(new(CreateSlots(settings.Templates, strings.EmptyTemplate.Text)));
        SelectedTemplate = Templates[0];
    }

    public CommitTemplateSettingsStrings Strings { get; }

    public ReadOnlyObservableCollection<CommitTemplateSlotViewModel> Templates { get; }

    [ObservableProperty]
    public partial CommitTemplateSlotViewModel SelectedTemplate { get; set; }

    /// <summary>Maximum characters in the first line; 0 disables the check.</summary>
    [ObservableProperty]
    public partial int MaxFirstLineLength { get; set; }

    /// <summary>Maximum characters per line; 0 disables the check.</summary>
    [ObservableProperty]
    public partial int MaxLineLength { get; set; }

    [ObservableProperty]
    public partial bool AutoWrap { get; set; }

    /// <summary>Regex the commit message must match; empty disables the check.</summary>
    [ObservableProperty]
    public partial string ValidationRegex { get; set; }

    [ObservableProperty]
    public partial bool IndentAfterFirstLine { get; set; }

    [ObservableProperty]
    public partial bool SecondLineMustBeEmpty { get; set; }

    [RelayCommand]
    private void Save()
    {
        _store.Save(new CommitMessageSettings
        {
            MaxFirstLineLength = MaxFirstLineLength,
            MaxLineLength = MaxLineLength,
            SecondLineMustBeEmpty = SecondLineMustBeEmpty,
            IndentAfterFirstLine = IndentAfterFirstLine,
            AutoWrap = AutoWrap,
            ValidationRegex = ValidationRegex,
            Templates = [.. Templates.Select(t => t.ToModel())],
        });

        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    private static IEnumerable<CommitTemplateSlotViewModel> CreateSlots(IReadOnlyList<CommitTemplate>? stored, string emptyText)
    {
        stored ??= [];

        // Migration, as in the WinForms dialog: keep what is configured and pad with empty slots.
        int count = Math.Max(stored.Count, TemplateSlotCount);
        for (int i = 0; i < count; i++)
        {
            yield return new CommitTemplateSlotViewModel(i, i < stored.Count ? stored[i] : CommitTemplate.Empty, emptyText);
        }
    }
}

/// <summary>
///  One of the numbered template slots shown in the template selector.
/// </summary>
public sealed partial class CommitTemplateSlotViewModel : ObservableObject
{
    /// <summary>Longest name the user may enter.</summary>
    public const int MaxNameLength = 80;

    /// <summary>Names are truncated to this length in the selector.</summary>
    public const int MaxDisplayedNameLength = 50;

    private readonly string _emptyText;

    public CommitTemplateSlotViewModel(int index, CommitTemplate template, string emptyText = "empty")
    {
        Index = index;
        Name = template.Name;
        Text = template.Text;
        IsRegex = template.IsRegex;
        _emptyText = emptyText;
    }

    /// <summary>Zero-based slot index.</summary>
    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial bool IsRegex { get; set; }

    /// <summary>Text shown in the selector, e.g. <c>"3 : Bug fix"</c> or <c>"4 : &lt;empty&gt;"</c>.</summary>
    public string DisplayName
        => $"{Index + 1} : {(string.IsNullOrEmpty(Name) ? $"<{_emptyText}>" : Name.ShortenTo(MaxDisplayedNameLength))}";

    public CommitTemplate ToModel() => new(Name, Text, IsRegex);
}
