using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitCommands.ExternalLinks;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.RevisionLinks;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the revision links settings; ids match <c>RevisionLinksSettingsPage</c>.</summary>
public sealed class RevisionLinksSettingsPageStrings : ViewStrings
{
    public RevisionLinksSettingsPageStrings()
        : base("RevisionLinksSettingsPage")
    {
        Title = Add("$this", "Text", "Revision links");
        AddTemplate = Add("_addTemplate", "Text", "Add {0} templates");
        Categories = Add("CategoriesLabel", "Text", "Categories");
        AddCategory = Add("Add", "Text", "Add");
        RemoveCategory = Add("Remove", "Text", "Remove");
        Name = Add("label1", "Text", "Name");
        Enabled = Add("EnabledChx", "Text", "Enabled");
        RemoteData = Add("remoteGrp", "Text", "Remote data");
        UseRemotes = Add("lblUseRemotes", "Text", "Use remotes");
        OnlyFirstRemote = Add("chkOnlyFirstRemote", "Text", "Only use the first match");
        RemoteSearchIn = Add("lblRemoteSearchIn", "Text", "Search in");
        Url = Add("chxURL", "Text", "URL");
        PushUrl = Add("chxPushURL", "Text", "Push URL");
        RemoteSearchPattern = Add("lblSearchRemotePattern", "Text", "Search pattern");
        RevisionData = Add("revisionDataGrp", "Text", "Revision data");
        SearchIn = Add("label5", "Text", "Search in");
        Message = Add("MessageChx", "Text", "Message");
        LocalBranch = Add("LocalBranchChx", "Text", "Local branch name");
        RemoteBranch = Add("RemoteBranchChx", "Text", "Remote branch name");
        SearchPattern = Add("label2", "Text", "Search pattern");
        NestedPattern = Add("nestedPatternLab", "Text", "Nested pattern");
        Links = Add("label6", "Text", "Links");
        Caption = Add("CaptionCol", "HeaderText", "Caption");
        Uri = Add("URICol", "HeaderText", "URI");
        Help = Add("linkLabelHelp", "Text", "Help", category: "GotoUserManualControl");
    }

    public TranslatedText Title { get; }

    public TranslatedText AddTemplate { get; }

    public TranslatedText Categories { get; }

    public TranslatedText AddCategory { get; }

    public TranslatedText RemoveCategory { get; }

    public TranslatedText Name { get; }

    public TranslatedText Enabled { get; }

    public TranslatedText RemoteData { get; }

    public TranslatedText UseRemotes { get; }

    public TranslatedText OnlyFirstRemote { get; }

    public TranslatedText RemoteSearchIn { get; }

    public TranslatedText Url { get; }

    public TranslatedText PushUrl { get; }

    public TranslatedText RemoteSearchPattern { get; }

    public TranslatedText RevisionData { get; }

    public TranslatedText SearchIn { get; }

    public TranslatedText Message { get; }

    public TranslatedText LocalBranch { get; }

    public TranslatedText RemoteBranch { get; }

    public TranslatedText SearchPattern { get; }

    public TranslatedText NestedPattern { get; }

    public TranslatedText Links { get; }

    public TranslatedText Caption { get; }

    public TranslatedText Uri { get; }

    /// <summary>The link to the manual (<c>gotoUserManualControl1</c>).</summary>
    public TranslatedText Help { get; }
}

/// <summary>What <see cref="RevisionLinksSettingsPageViewModel"/> needs from the application.</summary>
public interface IRevisionLinksSettingsPageHost
{
    /// <summary>The remotes of the repository (<c>GetRemotesAsync</c>), for the templates of their provider.</summary>
    IReadOnlyList<Remote> GetRemotes();
}

/// <summary>A category of revision links (an <see cref="ExternalLinkDefinition"/>) in the list of the page.</summary>
public sealed partial class RevisionLinkCategory(ExternalLinkDefinition definition) : ObservableObject
{
    public ExternalLinkDefinition Definition { get; } = definition;

    public string? Name
    {
        get => Definition.Name;
        set
        {
            if (Definition.Name != value)
            {
                Definition.Name = value;
                OnPropertyChanged();
            }
        }
    }
}

/// <summary>
///  A link of a category (an <see cref="ExternalLinkFormat"/>) in the grid; the last row is empty, and adds a link when edited
///  (as the new row of <c>LinksGrid</c>).
/// </summary>
public sealed partial class RevisionLinkFormatRow : ObservableObject
{
    private Action<RevisionLinkFormatRow>? _onEdited;

    internal RevisionLinkFormatRow(ExternalLinkFormat format, Action<RevisionLinkFormatRow>? onEdited = null)
    {
        Format = format;
        _onEdited = onEdited;
    }

    public ExternalLinkFormat Format { get; }

    /// <summary>The empty row adding a link.</summary>
    public bool IsNewRow => _onEdited is not null;

    public string? Caption
    {
        get => Format.Caption;
        set
        {
            if (Format.Caption != value)
            {
                Format.Caption = value;
                OnPropertyChanged();
                _onEdited?.Invoke(this);
            }
        }
    }

    public string? Uri
    {
        get => Format.Format;
        set
        {
            if (Format.Format != value)
            {
                Format.Format = value;
                OnPropertyChanged();
                _onEdited?.Invoke(this);
            }
        }
    }

    /// <summary>The new row was edited: it is a link now.</summary>
    internal void MarkAdded()
    {
        _onEdited = null;
        OnPropertyChanged(nameof(IsNewRow));
    }
}

/// <summary>A command adding the templates of a provider (a drop-down item of <c>Add</c>).</summary>
public sealed class RevisionLinkTemplate(string text, string iconName, IRelayCommand command)
{
    public string Text { get; } = text;

    public string IconName { get; } = iconName;

    public IRelayCommand Command { get; } = command;
}

/// <summary>
///  Port of <c>RevisionLinksSettingsPage</c> (a <c>DistributedSettingsPage</c>): the categories of revision links, with their
///  search patterns and links.
/// </summary>
public sealed partial class RevisionLinksSettingsPageViewModel : SettingsPageViewModel
{
    /// <summary>The section of the user manual (<c>gotoUserManualControl1</c>).</summary>
    public const string ManualSectionSubfolder = "settings";

    /// <inheritdoc cref="ManualSectionSubfolder"/>
    public const string ManualSectionAnchorName = "revision-links";

    private readonly IRevisionLinksSettingsPageHost _host;
    private ExternalLinksManager? _externalLinksManager;
    private bool _showingCategory;

    public RevisionLinksSettingsPageViewModel(RevisionLinksSettingsPageStrings strings, IRevisionLinksSettingsPageHost host)
    {
        Strings = strings;
        _host = host;

        // As LoadTemplatesInMenu.
        Templates = [.. new CloudProviderExternalLinkDefinitionExtractorFactory().GetAllExtractor().Select(extractor => new RevisionLinkTemplate(
            string.Format(strings.AddTemplate.Text, extractor.ServiceName),
            extractor.IconName,
            new RelayCommand(() => ExtractExternalLinkDefinitions(extractor))))];
    }

    public RevisionLinksSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "RevisionLinksSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The templates of the drop-down of <c>Add</c>.</summary>
    public IReadOnlyList<RevisionLinkTemplate> Templates { get; }

    /// <summary>As <c>_NO_TRANSLATE_Categories</c>.</summary>
    public ObservableCollection<RevisionLinkCategory> Categories { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCategorySelected))]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    public partial RevisionLinkCategory? SelectedCategory { get; set; }

    /// <summary>As <c>splitContainer1.Panel2.Enabled</c>.</summary>
    public bool IsCategorySelected => SelectedCategory is not null;

    /// <summary>As <c>LinksGrid</c>: the links of the category, then the new row.</summary>
    public ObservableCollection<RevisionLinkFormatRow> LinkFormats { get; } = [];

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool SearchInMessage { get; set; }

    [ObservableProperty]
    public partial bool SearchInLocalBranch { get; set; }

    [ObservableProperty]
    public partial bool SearchInRemoteBranch { get; set; }

    [ObservableProperty]
    public partial string SearchPattern { get; set; } = "";

    [ObservableProperty]
    public partial string NestedPattern { get; set; } = "";

    [ObservableProperty]
    public partial string RemotePattern { get; set; } = "";

    [ObservableProperty]
    public partial string UseRemotes { get; set; } = "";

    [ObservableProperty]
    public partial bool OnlyFirstRemote { get; set; }

    [ObservableProperty]
    public partial bool SearchInUrl { get; set; }

    [ObservableProperty]
    public partial bool SearchInPushUrl { get; set; }

    private ExternalLinkDefinition? SelectedLinkDefinition => SelectedCategory?.Definition;

    protected override void SettingsToPage(SettingsSource? settings)
    {
        _externalLinksManager = new ExternalLinksManager(settings as DistributedSettings ?? throw new ArgumentException("The distributed settings are expected", nameof(settings)));

        ReloadCategories();
        SelectedCategory = Categories.FirstOrDefault();

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        _externalLinksManager?.Save();

        base.PageToSettings(settings);
    }

    private void ReloadCategories(ExternalLinkDefinition? selected = null)
    {
        Categories.Clear();
        if (_externalLinksManager is not null)
        {
            foreach (ExternalLinkDefinition definition in _externalLinksManager.GetEffectiveSettings())
            {
                Categories.Add(new RevisionLinkCategory(definition));
            }
        }

        if (selected is not null)
        {
            SelectedCategory = Categories.FirstOrDefault(category => category.Definition == selected);
        }
    }

    // As CategoryChanged: the category selected is shown.
    partial void OnSelectedCategoryChanged(RevisionLinkCategory? value)
    {
        _showingCategory = true;
        try
        {
            ExternalLinkDefinition? definition = value?.Definition;
            Name = definition?.Name ?? "";
            IsEnabled = definition?.Enabled ?? false;
            SearchInMessage = definition?.SearchInParts.Contains(ExternalLinkDefinition.RevisionPart.Message) ?? false;
            SearchInLocalBranch = definition?.SearchInParts.Contains(ExternalLinkDefinition.RevisionPart.LocalBranches) ?? false;
            SearchInRemoteBranch = definition?.SearchInParts.Contains(ExternalLinkDefinition.RevisionPart.RemoteBranches) ?? false;
            SearchPattern = definition?.SearchPattern ?? "";
            NestedPattern = definition?.NestedSearchPattern ?? "";
            RemotePattern = definition?.RemoteSearchPattern ?? "";
            SearchInUrl = definition?.RemoteSearchInParts.Contains(ExternalLinkDefinition.RemotePart.URL) ?? false;
            SearchInPushUrl = definition?.RemoteSearchInParts.Contains(ExternalLinkDefinition.RemotePart.PushURL) ?? false;
            UseRemotes = definition?.UseRemotesPattern ?? "";
            OnlyFirstRemote = definition?.UseOnlyFirstRemote ?? false;

            LinkFormats.Clear();
            if (definition is not null)
            {
                foreach (ExternalLinkFormat format in definition.LinkFormats)
                {
                    LinkFormats.Add(new RevisionLinkFormatRow(format));
                }

                AddNewRow();
            }
        }
        finally
        {
            _showingCategory = false;
        }
    }

    private void AddNewRow() => LinkFormats.Add(new RevisionLinkFormatRow(new ExternalLinkFormat(), OnNewRowEdited));

    // As the new row of LinksGrid: the link edited is added, with a new empty row.
    private void OnNewRowEdited(RevisionLinkFormatRow row)
    {
        if (SelectedLinkDefinition is not { } definition || LinkFormats.LastOrDefault() != row)
        {
            return;
        }

        definition.LinkFormats.Add(row.Format);
        row.MarkAdded();
        AddNewRow();
    }

    /// <summary>As deleting a row of <c>LinksGrid</c>.</summary>
    [RelayCommand]
    private void RemoveLinkFormat(RevisionLinkFormatRow? row)
    {
        if (row is null || row.IsNewRow || SelectedLinkDefinition is not { } definition)
        {
            return;
        }

        definition.LinkFormats.Remove(row.Format);
        LinkFormats.Remove(row);
    }

    /// <summary>As <c>Add_Click</c>: a new category.</summary>
    [RelayCommand]
    private void Add()
    {
        ExternalLinkDefinition definition = new()
        {
            Name = "<new>",
            Enabled = true,
            UseRemotesPattern = "upstream|origin",
            UseOnlyFirstRemote = true,
            SearchInParts = { ExternalLinkDefinition.RevisionPart.Message },
            RemoteSearchInParts = { ExternalLinkDefinition.RemotePart.URL }
        };
        _externalLinksManager?.Add(definition);

        ReloadCategories(definition);
    }

    /// <summary>As <c>Remove_Click</c>: the selected category is removed, the next one selected.</summary>
    [RelayCommand(CanExecute = nameof(IsCategorySelected))]
    private void Remove()
    {
        if (SelectedCategory is not { } selected)
        {
            return;
        }

        int index = Categories.IndexOf(selected);
        _externalLinksManager?.Remove(selected.Definition);

        ReloadCategories();

        if (index >= 0 && Categories.Count > 0)
        {
            SelectedCategory = Categories[Math.Min(index, Categories.Count - 1)];
        }
        else
        {
            SelectedCategory = null;
        }
    }

    /// <summary>As <c>ExtractExternalLinkDefinitions</c>: the templates of the provider, for its preferred remote.</summary>
    private void ExtractExternalLinkDefinitions(ICloudProviderExternalLinkDefinitionExtractor externalLinkDefinitionExtractor)
    {
        if (_externalLinksManager is null)
        {
            return;
        }

        IReadOnlyList<Remote> remotes = _host.GetRemotes();
        Remote selectedRemote = FindRemoteByPreference([.. remotes.Where(r => externalLinkDefinitionExtractor.IsValidRemoteUrl(r.FetchUrl))]);

        IList<ExternalLinkDefinition> externalLinkDefinitions = externalLinkDefinitionExtractor.GetDefinitions(selectedRemote.Name is null ? "" : selectedRemote.FetchUrl);
        _externalLinksManager.AddRange(externalLinkDefinitions);

        ReloadCategories(externalLinkDefinitions[0]);
    }

    private static Remote FindRemoteByPreference(IList<Remote> remotes)
    {
        if (remotes.Count == 0)
        {
            return default;
        }

        string[] remoteNames = ["upstream", "fork", "origin"];
        foreach (string remoteName in remoteNames)
        {
            Remote remoteFound = remotes.FirstOrDefault(r => r.Name == remoteName);
            if (remoteFound.Name is not null)
            {
                return remoteFound;
            }
        }

        return remotes[0];
    }

    // As _NO_TRANSLATE_Name_Leave (applied as typed, the list shows it).
    partial void OnNameChanged(string value)
    {
        if (!_showingCategory && SelectedCategory is { } category)
        {
            category.Name = value;
        }
    }

    partial void OnIsEnabledChanged(bool value) => Update(definition => definition.Enabled = value);

    partial void OnSearchInMessageChanged(bool value) => Update(definition => Toggle(definition.SearchInParts, ExternalLinkDefinition.RevisionPart.Message, value));

    partial void OnSearchInLocalBranchChanged(bool value) => Update(definition => Toggle(definition.SearchInParts, ExternalLinkDefinition.RevisionPart.LocalBranches, value));

    partial void OnSearchInRemoteBranchChanged(bool value) => Update(definition => Toggle(definition.SearchInParts, ExternalLinkDefinition.RevisionPart.RemoteBranches, value));

    // As the Leave handlers of the patterns: they are trimmed.
    partial void OnSearchPatternChanged(string value) => Update(definition => definition.SearchPattern = value.Trim());

    partial void OnNestedPatternChanged(string value) => Update(definition => definition.NestedSearchPattern = value.Trim());

    partial void OnRemotePatternChanged(string value) => Update(definition => definition.RemoteSearchPattern = value.Trim());

    partial void OnUseRemotesChanged(string value) => Update(definition => definition.UseRemotesPattern = value.Trim());

    partial void OnOnlyFirstRemoteChanged(bool value) => Update(definition => definition.UseOnlyFirstRemote = value);

    partial void OnSearchInUrlChanged(bool value) => Update(definition => Toggle(definition.RemoteSearchInParts, ExternalLinkDefinition.RemotePart.URL, value));

    partial void OnSearchInPushUrlChanged(bool value) => Update(definition => Toggle(definition.RemoteSearchInParts, ExternalLinkDefinition.RemotePart.PushURL, value));

    private void Update(Action<ExternalLinkDefinition> update)
    {
        if (!_showingCategory && SelectedLinkDefinition is { } definition)
        {
            update(definition);
        }
    }

    private static void Toggle<T>(HashSet<T> parts, T part, bool included)
    {
        if (included)
        {
            parts.Add(part);
        }
        else
        {
            parts.Remove(part);
        }
    }
}
