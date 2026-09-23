using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the dashboard category name dialog; ids match <c>FormDashboardCategoryTitle</c>.</summary>
public sealed class DashboardCategoryTitleStrings : ViewStrings
{
    public DashboardCategoryTitleStrings()
        : base("FormDashboardCategoryTitle")
    {
        Title = Add("$this", "Text", "Enter Caption");
        RenameCategory = Add("_renameCategoryText", "Text", "Rename category");
        CategoryName = Add("lblCategoryName", "Text", "Category name");
        Ok = Add("btnOk", "Text", "OK");
        Cancel = Add("btnCancel", "Text", "Cancel");
        CategoryNameRequired = Add("_categoryNameRequiredText", "Text", "Category name is required");
        CategoryNameExists = Add("_categoryNameExistsText", "Text", "Category name already exists");
    }

    public TranslatedText Title { get; }

    public TranslatedText RenameCategory { get; }

    public TranslatedText CategoryName { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }

    public TranslatedText CategoryNameRequired { get; }

    public TranslatedText CategoryNameExists { get; }
}

/// <summary>View model of the dashboard category name dialog (port of <c>FormDashboardCategoryTitle</c>).</summary>
public sealed partial class DashboardCategoryTitleViewModel : DialogViewModel
{
    private readonly IReadOnlyList<string> _existingCategories;
    private readonly string? _originalName;
    private readonly IMessageBoxService _messageBoxes;

    /// <param name="originalName">The name of the category to rename, or <see langword="null"/> for a new category.</param>
    public DashboardCategoryTitleViewModel(DashboardCategoryTitleStrings strings, IReadOnlyList<string> existingCategories, string? originalName, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _existingCategories = existingCategories;
        _originalName = originalName;
        _messageBoxes = messageBoxes;
        Title = originalName is null ? strings.Title.PlainText : strings.RenameCategory.PlainText;
        CategoryName = originalName ?? "";
    }

    public DashboardCategoryTitleStrings Strings { get; }

    public string Title { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    public partial string CategoryName { get; set; }

    /// <summary>The entered category name, once accepted.</summary>
    public string? Category { get; private set; }

    private bool IsChanged() => CategoryName != _originalName;

    [RelayCommand(CanExecute = nameof(IsChanged))]
    private void Ok()
    {
        if (string.IsNullOrEmpty(CategoryName))
        {
            _messageBoxes.ShowError(Strings.CategoryNameRequired.Text, Strings.CategoryName.PlainText);
            return;
        }

        if (_existingCategories.Contains(CategoryName, StringComparer.Ordinal))
        {
            _messageBoxes.ShowError(Strings.CategoryNameExists.Text, Strings.CategoryName.PlainText);
            return;
        }

        Category = CategoryName;
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

/// <summary>Strings of the add to .gitignore dialog; ids match <c>FormAddToGitIgnore</c>.</summary>
public sealed class AddToGitIgnoreStrings : ViewStrings
{
    public AddToGitIgnoreStrings()
        : base("FormAddToGitIgnore")
    {
        Title = Add("$this", "Text", "Add file(s) to .gitignore");
        AddToLocalExcludeTitle = Add("_addToLocalExcludeTitle", "Text", "Add file(s) to .git/info/exclude");
        FilePattern = Add("groupFilePattern", "Text", "Enter a file pattern to ignore:");
        Preview = Add("groupBox1", "Text", "Preview");
        NoMatch = Add("label2", "Text", "No existing files match that pattern.");
        MatchingFiles = Add("_matchingFilesString", "Text", "{0} file(s) matched");
        Updating = Add("_updateStatusString", "Text", "Updating ...");
        Ignore = Add("AddToIgnore", "Text", "Ignore");
        Cancel = Add("btnCancel", "Text", "Cancel");
    }

    public TranslatedText Title { get; }

    public TranslatedText AddToLocalExcludeTitle { get; }

    public TranslatedText FilePattern { get; }

    public TranslatedText Preview { get; }

    public TranslatedText NoMatch { get; }

    public TranslatedText MatchingFiles { get; }

    public TranslatedText Updating { get; }

    public TranslatedText Ignore { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>Operations of the add to .gitignore dialog that need the host (git, file system).</summary>
public interface IAddToGitIgnoreHost
{
    /// <summary>
    ///  Lists (in the background, after a short delay that is restarted by each request) the files the patterns ignore,
    ///  and reports them on the UI thread.
    /// </summary>
    void RequestIgnoredFiles(IReadOnlyList<string> patterns, Action<IReadOnlyList<string>> report);

    /// <summary>Appends the patterns to the ignore file, showing errors.</summary>
    void AddPatterns(IReadOnlyList<string> patterns);
}

/// <summary>View model of the add to .gitignore dialog (port of <c>FormAddToGitIgnore</c>).</summary>
public sealed partial class AddToGitIgnoreViewModel : DialogViewModel
{
    private readonly IAddToGitIgnoreHost _host;

    /// <param name="localExclude">Whether the patterns go to <c>.git/info/exclude</c> instead of <c>.gitignore</c>.</param>
    public AddToGitIgnoreViewModel(AddToGitIgnoreStrings strings, bool localExclude, IEnumerable<string> patterns, IAddToGitIgnoreHost host)
    {
        Strings = strings;
        _host = host;
        Title = localExclude ? strings.AddToLocalExcludeTitle.PlainText : strings.Title.PlainText;
        Patterns = string.Join(Environment.NewLine, patterns);
    }

    public AddToGitIgnoreStrings Strings { get; }

    public string Title { get; }

    /// <summary>The patterns to ignore, one per line.</summary>
    [ObservableProperty]
    public partial string Patterns { get; set; }

    /// <summary>The files the patterns ignore (or the progress message while they are listed).</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> PreviewFiles { get; private set; } = [];

    [ObservableProperty]
    public partial string MatchStatus { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsUpdating { get; private set; }

    /// <summary>Whether no existing file matches the patterns.</summary>
    [ObservableProperty]
    public partial bool HasNoMatch { get; private set; }

    /// <summary>The non-empty lines of <see cref="Patterns"/>.</summary>
    public IReadOnlyList<string> GetPatterns() => [.. Patterns.Split(["\r\n", "\n"], StringSplitOptions.None).Where(line => !string.IsNullOrEmpty(line))];

    partial void OnPatternsChanged(string value)
    {
        IsUpdating = true;
        MatchStatus = Strings.Updating.Text;
        PreviewFiles = [Strings.Updating.Text];
        HasNoMatch = false;
        _host.RequestIgnoredFiles(GetPatterns(), files =>
        {
            // Only the result of the latest patterns is shown.
            if (value != Patterns)
            {
                return;
            }

            IsUpdating = false;
            PreviewFiles = files;
            MatchStatus = string.Format(Strings.MatchingFiles.Text, files.Count);
            HasNoMatch = files.Count == 0;
        });
    }

    [RelayCommand]
    private void Ignore()
    {
        IReadOnlyList<string> patterns = GetPatterns();
        if (patterns.Count > 0)
        {
            _host.AddPatterns(patterns);
        }

        Close(accepted: patterns.Count > 0);
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}
