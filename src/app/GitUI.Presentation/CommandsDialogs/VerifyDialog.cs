using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the verify database dialog; ids match <c>FormVerify</c>.</summary>
public sealed class VerifyStrings : ViewStrings
{
    public VerifyStrings()
        : base("FormVerify")
    {
        Title = Add("$this", "Text", "Verify database");
        DeleteAllLostAndFoundTags = Add("DeleteAllLostAndFoundTags", "Text", "Delete all LOST_AND_FOUND tags");
        FullCheck = Add("FullCheck", "Text", "Check not just objects in GIT_OBJECT_DIRECTORY ($GIT_DIR/objects), \nbut also the ones found in alternate object pools.\n");
        NoReflogs = Add("NoReflogs", "Text", "Do not consider commits that are referenced only by an entry in a \nreflog to be reachable.");
        Remove = Add("Remove", "Text", "Remove all dangling objects");
        SaveObjects = Add("SaveObjects", "Text", "Save objects to .git/lost-found");
        ShowCommitsAndTags = Add("ShowCommitsAndTags", "Text", "Show commits and annotated tags");
        ShowCommitsAndTagsToolTip = Add("ShowCommitsAndTags", "toolTip", "To recover unreachable commits or annotated tags");
        ShowOtherObjects = Add("ShowOtherObjects", "Text", "Show blobs and trees");
        ShowOtherObjectsToolTip = Add("ShowOtherObjects", "toolTip", "To recover contents of files once staged but mistakenly deleted");
        Unreachable = Add("Unreachable", "Text", "Print out objects that exist but that aren't readable from any of the reference \nnodes.\n");
        LostObjectUnreadable = Add("_lostObjectUnreadable", "Text", "Object could not be read. It may be corrupted or no longer available.");
        RemoveDanglingObjectsCaption = Add("_removeDanglingObjectsCaption", "Text", "Remove");
        RemoveDanglingObjectsQuestion = Add("_removeDanglingObjectsQuestion", "Text", "Are you sure you want to delete all dangling objects?");
        Seemingly = Add("_seemingly", "Text", "seemingly");
        SelectLostObjectsToRestoreCaption = Add("_selectLostObjectsToRestoreCaption", "Text", "Restore lost objects");
        SelectLostObjectsToRestoreMessage = Add("_selectLostObjectsToRestoreMessage", "Text", "Select objects to restore.");
        TagsCreated = Add("_xTagsCreated", "Text", "{0} Tags created.\n\nDo not forget to delete these tags when finished.");
        Close = Add("btnCloseDialog", "Text", "Cancel");
        RestoreSelectedObjects = Add("btnRestoreSelectedObjects", "Text", "Recover selected objects");
        AuthorColumn = Add("columnAuthor", "HeaderText", "Author");
        DateColumn = Add("columnDate", "HeaderText", "Date");
        HashColumn = Add("columnHash", "HeaderText", "Hash");
        ParentColumn = Add("columnParent", "HeaderText", "Parent(s) hashs");
        SubjectColumn = Add("columnSubject", "HeaderText", "Subject");
        TypeColumn = Add("columnType", "HeaderText", "Type");
        CopyHash = Add("copyHashToolStripMenuItem", "Text", "Copy object hash");
        CopyParentHash = Add("copyParentHashToolStripMenuItem", "Text", "Copy parent hash");
        QuickViewHint = Add("label1", "Text", "Double-click on a row for quick view");
        Help = Add("label2", "Text", "By default only unreferenced objects that are older than \n2 weeks are removed when cleaning up the database. All\nother object are only deleted when you run \"Remove all\ndangling objects\"\n\nCheck commits you want to recover and press Recover button\nContext menu for additional operations");
        View = Add("mnuLostObjectView", "Text", "View");
        CreateBranch = Add("mnuLostObjectsCreateBranch", "Text", "Create branch");
        CreateTag = Add("mnuLostObjectsCreateTag", "Text", "Create tag");
        SaveAs = Add("saveAsToolStripMenuItem", "Text", "Save as...");
    }

    public TranslatedText Title { get; }

    public TranslatedText DeleteAllLostAndFoundTags { get; }

    public TranslatedText FullCheck { get; }

    public TranslatedText NoReflogs { get; }

    public TranslatedText Remove { get; }

    public TranslatedText SaveObjects { get; }

    public TranslatedText ShowCommitsAndTags { get; }

    public TranslatedText ShowCommitsAndTagsToolTip { get; }

    public TranslatedText ShowOtherObjects { get; }

    public TranslatedText ShowOtherObjectsToolTip { get; }

    public TranslatedText Unreachable { get; }

    public TranslatedText LostObjectUnreadable { get; }

    public TranslatedText RemoveDanglingObjectsCaption { get; }

    public TranslatedText RemoveDanglingObjectsQuestion { get; }

    public TranslatedText Seemingly { get; }

    public TranslatedText SelectLostObjectsToRestoreCaption { get; }

    public TranslatedText SelectLostObjectsToRestoreMessage { get; }

    public TranslatedText TagsCreated { get; }

    public TranslatedText Close { get; }

    public TranslatedText RestoreSelectedObjects { get; }

    public TranslatedText AuthorColumn { get; }

    public TranslatedText DateColumn { get; }

    public TranslatedText HashColumn { get; }

    public TranslatedText ParentColumn { get; }

    public TranslatedText SubjectColumn { get; }

    public TranslatedText TypeColumn { get; }

    public TranslatedText CopyHash { get; }

    public TranslatedText CopyParentHash { get; }

    public TranslatedText QuickViewHint { get; }

    public TranslatedText Help { get; }

    public TranslatedText View { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText CreateTag { get; }

    public TranslatedText SaveAs { get; }
}

/// <summary>Strings of the text viewer; ids match <c>FormEdit</c>.</summary>
public sealed class TextViewerStrings : ViewStrings
{
    public TextViewerStrings()
        : base("FormEdit")
    {
        Title = Add("$this", "Text", "View");
    }

    public TranslatedText Title { get; }
}

/// <summary>View model of the text viewer (port of <c>FormEdit</c>): a text in the text editor.</summary>
public sealed class TextViewerViewModel : DialogViewModel
{
    public TextViewerViewModel(TextViewerStrings strings, string text, string fileName, bool isReadOnly)
    {
        Strings = strings;
        Editor.IsReadOnly = isReadOnly;
        Editor.Load(text, fileName);
    }

    public TextViewerStrings Strings { get; }

    public TextEditorViewModel Editor { get; } = new();
}

/// <summary>The kind of a lost object (<c>LostObjectType</c>).</summary>
public enum LostObjectKind
{
    Other,
    Commit,
    Blob,
    Tree,
    Tag,
}

/// <summary>A lost object found by <c>git fsck</c> (<c>FormVerify.LostObject</c>).</summary>
public sealed partial class LostObjectItem(LostObjectKind kind, ObjectId objectId, string rawType) : ObservableObject
{
    public LostObjectKind Kind { get; } = kind;

    public ObjectId ObjectId { get; } = objectId;

    public ObjectId? Parent { get; init; }

    /// <summary>The type as reported by git; the guessed file type is appended for blobs.</summary>
    [ObservableProperty]
    public partial string RawType { get; set; } = rawType;

    public string? Author { get; init; }

    public string? Subject { get; init; }

    public DateTime? Date { get; init; }

    /// <summary>The name of an annotated tag.</summary>
    public string? TagName { get; init; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public bool IsCommitOrTag => Kind is LostObjectKind.Commit or LostObjectKind.Tag;
}

/// <summary>Operations of the verify database dialog that need the host (git, other dialogs, the clipboard, files).</summary>
public interface IVerifyHost
{
    /// <summary>Runs <c>git fsck-objects</c> with the options in the process dialog; <see langword="null"/> if the user aborted it.</summary>
    IReadOnlyList<LostObjectItem>? ReadLostObjects(string options);

    /// <summary>Runs <c>git fsck-objects --lost-found</c> with the options in the process dialog.</summary>
    void SaveLostObjects(string options);

    /// <summary>Runs <c>git prune</c> in the process dialog.</summary>
    void Prune();

    /// <summary>The content of the object (<c>git show</c>, raw for a blob); throws if it cannot be read.</summary>
    string GetContent(LostObjectItem item);

    /// <summary>Shows the create tag dialog for the object; returns whether a tag was created.</summary>
    bool ShowCreateTag(ObjectId objectId);

    /// <summary>Shows the create branch dialog for the object; returns whether a branch was created.</summary>
    bool ShowCreateBranch(ObjectId objectId);

    /// <summary>Creates a lightweight tag.</summary>
    void CreateTag(string tagName, ObjectId objectId);

    /// <summary>The names of the repository's tags.</summary>
    IEnumerable<string> GetTagNames();

    void DeleteTag(string tagName);

    void CopyToClipboard(string text);

    /// <summary>Asks where to save the blob (as <c>FormVerify.saveAsToolStripMenuItem_Click</c>) and saves it.</summary>
    void SaveBlobAs(ObjectId objectId, string fileName, string filter, string extension);

    /// <summary>Shows the text in the text viewer (<c>FormEdit</c>), read-only.</summary>
    void View(string text, string fileName);

    /// <summary>Runs <paramref name="work"/> in the background, then <paramref name="then"/> on the UI thread.</summary>
    void RunInBackground(Action work, Action then);
}

/// <summary>View model of the verify database dialog (port of <c>FormVerify</c>): recovers lost objects.</summary>
public sealed partial class VerifyViewModel : DialogViewModel
{
    private const string RestoredObjectsTagPrefix = "LOST_FOUND_";

    // https://en.wikipedia.org/wiki/List_of_file_signatures (as FormVerify._languagesStartOfFile)
    private static readonly (string Start, string Type)[] LanguagesStartOfFile =
    [
        (@"{\rtf", "rtf"),
        ("{", "json"),
        ("#include", "cpp"),
        ("import {", "js"),
        ("import * as", "js"),
        ("import \"", "js"),
        ("export ", "js"),
        ("import ", "java"),
        ("from", "py"),
        ("package", "go"),
        ("namespace ", "fs"),
        ("#!", "sh"),
        ("[", "ini"),
        ("using ", "cs"),
        ("# ", "md"),
        ("##", "md"),
        ("<!doctype html", "html"),
        ("<html", "html"),
        ("<?xml", "xml"),
        ("use ", "rs"),
        ("%PDF", "pdf"),
        ("PK", "zip"),
        ("MZ", "exe"),
        (@"\document", "tex"),
        ("\u0089PNG", "png"),
        ("ÿØÿQ", "jp2"),
        ("ÿØÿ", "jpg"),
        ("ÿ\x0A", "jxl"),
        ("RIFF", "webp"),
        ("<svg", "svg"),
        ("BM", "bmp"),
        ("7z", "7z"),
        ("GIF", "gif"),
        ("ÐÏ\x11à¡±\x1Aá", "doc"),
        ("qoif", "qoi"),
        ("Rar!", "rar"),
        ("%!PS", "ps"),
        ("OggS", "ogg"),
        ("8BPS", "psf"),
        ("ID3", "mp3"),
        ("CD001", "iso"),
        ("fLaC", "flac"),
        ("FLIF", "flif"),
        ("␚Eß£", "mkv"),
        ("<", "xml"),
    ];

    // As FormVerify._fileTypesEquivalences.
    private static readonly Dictionary<string, string[]> FileTypesEquivalences = new()
    {
        { "js", ["ts", "jsx", "tsx"] },
        { "html", ["php", "cshtml"] },
        { "cpp", ["c"] },
        { "xml", ["config", "settings", "csproj", "xlf", "props"] },
        { "zip", ["docx", "xlsx", "pptx", "odt", "ods", "odp", "epub", "jar", "msix"] },
        { "exe", ["dll"] },
        { "doc", ["xls", "ppt", "msi"] },
        { "md", ["sh", "yml"] },
        { "txt", ["csv", "css", "md", "yml"] },
    };

    private readonly IVerifyHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly List<LostObjectItem> _lostObjects = [];
    private bool _typeDetected;

    /// <summary>The file name the previewed object is shown and saved with.</summary>
    private string? _defaultFileName;

    public VerifyViewModel(VerifyStrings strings, IVerifyHost host, IMessageBoxService messageBoxes)
    {
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
    }

    public VerifyStrings Strings { get; }

    /// <summary>The lost objects shown, the most recent first.</summary>
    public ObservableCollection<LostObjectItem> LostObjects { get; } = [];

    [ObservableProperty]
    public partial LostObjectItem? SelectedObject { get; set; }

    /// <summary>The content of the selected object.</summary>
    public TextEditorViewModel Preview { get; } = new() { IsReadOnly = true };

    [ObservableProperty]
    public partial bool Unreachable { get; set; }

    [ObservableProperty]
    public partial bool FullCheck { get; set; }

    [ObservableProperty]
    public partial bool NoReflogs { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowCommitsAndTags { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowOtherObjects { get; set; }

    public bool CanCreateRef => SelectedObject?.Kind == LostObjectKind.Commit;

    public bool CanSaveAs => SelectedObject?.Kind == LostObjectKind.Blob;

    /// <summary>As <c>FormVerifyShown</c>: reads the lost objects.</summary>
    public void Initialize() => UpdateLostObjects();

    partial void OnUnreachableChanged(bool value) => UpdateLostObjects();

    partial void OnFullCheckChanged(bool value) => UpdateLostObjects();

    partial void OnNoReflogsChanged(bool value) => UpdateLostObjects();

    partial void OnShowCommitsAndTagsChanged(bool value)
    {
        // As ShowCommitsCheckedChanged: one of the kinds is always shown.
        if (!ShowCommitsAndTags && !ShowOtherObjects)
        {
            ShowOtherObjects = true;
        }
        else
        {
            UpdateFilteredLostObjects();
        }
    }

    partial void OnShowOtherObjectsChanged(bool value)
    {
        if (!ShowCommitsAndTags && !ShowOtherObjects)
        {
            ShowCommitsAndTags = true;
        }
        else
        {
            UpdateFilteredLostObjects();
        }
    }

    partial void OnSelectedObjectChanged(LostObjectItem? value)
    {
        OnPropertyChanged(nameof(CanCreateRef));
        OnPropertyChanged(nameof(CanSaveAs));
        ShowPreview(value);
    }

    /// <summary>Checks or unchecks all shown objects (the header of the check box column).</summary>
    public void SelectAll(bool isSelected)
    {
        foreach (LostObjectItem item in LostObjects)
        {
            item.IsSelected = isSelected;
        }
    }

    [RelayCommand]
    private void SaveObjects()
    {
        _host.SaveLostObjects(GetOptions());
        UpdateLostObjects();
    }

    [RelayCommand]
    private void Remove()
    {
        if (!_messageBoxes.Confirm(Strings.RemoveDanglingObjectsQuestion.Text, Strings.RemoveDanglingObjectsCaption.Text))
        {
            return;
        }

        _host.Prune();
        UpdateLostObjects();
    }

    [RelayCommand]
    private void DeleteAllLostAndFoundTags()
    {
        DeleteLostFoundTags();
        UpdateLostObjects();
    }

    /// <summary>As <c>btnRestoreSelectedObjects_Click</c>: tags the checked objects.</summary>
    [RelayCommand]
    private void RestoreSelectedObjects()
    {
        DeleteLostFoundTags();
        int restoredObjectsCount = CreateLostFoundTags();
        if (restoredObjectsCount == 0)
        {
            return;
        }

        _messageBoxes.ShowInformation(string.Format(Strings.TagsCreated.Text, restoredObjectsCount), "Tags created");

        // If the user restored all objects, they want to see the restored commits in the main window.
        if (restoredObjectsCount == LostObjects.Count)
        {
            Close(accepted: true);
            return;
        }

        UpdateLostObjects();
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);

    /// <summary>As <c>ViewCurrentItem</c>: shows the content of the object in the text viewer.</summary>
    [RelayCommand]
    private void View()
    {
        if (SelectedObject is not { } item)
        {
            return;
        }

        string content = GetContent(item);
        if (!string.IsNullOrEmpty(content))
        {
            _host.View(content, _defaultFileName!);
        }
    }

    [RelayCommand]
    private void CreateTag()
    {
        if (SelectedObject is { Kind: LostObjectKind.Commit } item && _host.ShowCreateTag(item.ObjectId))
        {
            UpdateLostObjects();
        }
    }

    [RelayCommand]
    private void CreateBranch()
    {
        if (SelectedObject is { Kind: LostObjectKind.Commit } item && _host.ShowCreateBranch(item.ObjectId))
        {
            UpdateLostObjects();
        }
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (SelectedObject is { } item)
        {
            _host.CopyToClipboard(item.ObjectId.ToString());
        }
    }

    [RelayCommand]
    private void CopyParentHash()
    {
        if (SelectedObject is { Parent: { IsZero: false } parent })
        {
            _host.CopyToClipboard(parent.ToString());
        }
    }

    /// <summary>As <c>saveAsToolStripMenuItem_Click</c>: saves a blob, with the guessed file type.</summary>
    [RelayCommand]
    private void SaveAs()
    {
        if (SelectedObject is not { Kind: LostObjectKind.Blob } item)
        {
            return;
        }

        string fileName = _defaultFileName ?? item.ObjectId + "_LOST_FOUND.txt";
        string extension = Path.GetExtension(fileName).TrimStart('.');
        string filter = $"{extension} Files (*.{extension})|*.{extension}";
        if (FileTypesEquivalences.TryGetValue(extension, out string[]? types))
        {
            filter += "|" + string.Join("|", types.Select(t => $"{t} Files (*.{t})|*.{t}"));
        }

        _host.SaveBlobAs(item.ObjectId, fileName, $"{filter}| All Files (*.*)|*.*", extension);
    }

    /// <summary>As <c>Warnings_SelectionChanged</c>: a commit or tag as a patch, a blob with its guessed file type.</summary>
    private void ShowPreview(LostObjectItem? item)
    {
        _defaultFileName = null;
        if (item is null)
        {
            return;
        }

        string content = GetContent(item);
        if (item.IsCommitOrTag)
        {
            _defaultFileName = "commit.patch";
            Preview.LoadDiff(content);
        }
        else if (item.Kind == LostObjectKind.Blob)
        {
            _defaultFileName = GuessFileNameWithContent(content, item.ObjectId.ToString());
            Preview.Load(content, _defaultFileName);
        }
        else
        {
            _defaultFileName = "file.txt";
            Preview.Load(content, _defaultFileName);
        }
    }

    /// <summary>As <c>UpdateLostObjects</c>: runs fsck; closes the dialog if the user aborted it.</summary>
    private void UpdateLostObjects()
    {
        IReadOnlyList<LostObjectItem>? lostObjects = _host.ReadLostObjects(GetOptions());
        if (lostObjects is null)
        {
            Close(accepted: false);
            return;
        }

        _lostObjects.Clear();
        _lostObjects.AddRange(lostObjects.OrderByDescending(l => l.Date));
        _typeDetected = false;
        UpdateFilteredLostObjects();
    }

    private void UpdateFilteredLostObjects()
    {
        if (ShowOtherObjects && !_typeDetected)
        {
            // As FormVerify: the guessed file type of the blobs is appended to their type, in the background.
            _typeDetected = true;
            LostObjectItem[] blobs = [.. _lostObjects.Where(o => o.Kind == LostObjectKind.Blob)];
            string[] types = new string[blobs.Length];
            _host.RunInBackground(
                () =>
                {
                    for (int i = 0; i < blobs.Length; i++)
                    {
                        types[i] = GuessFileTypeWithContent(GetContent(blobs[i]));
                    }
                },
                () =>
                {
                    for (int i = 0; i < blobs.Length; i++)
                    {
                        blobs[i].RawType += $" ({Strings.Seemingly.Text}: {types[i]})";
                    }
                });
        }

        LostObjectItem? selected = SelectedObject;
        LostObjects.Clear();
        foreach (LostObjectItem item in _lostObjects.Where(IsMatchToFilter))
        {
            LostObjects.Add(item);
        }

        SelectedObject = selected is not null && LostObjects.Contains(selected) ? selected : null;
    }

    private bool IsMatchToFilter(LostObjectItem lostObject)
        => (ShowCommitsAndTags && lostObject.IsCommitOrTag) || (ShowOtherObjects && !lostObject.IsCommitOrTag);

    private string GetContent(LostObjectItem item)
    {
        try
        {
            return _host.GetContent(item);
        }
        catch (Exception ex)
        {
            // The object could not be read (e.g. corrupted or pruned from the repository).
            return $"{Strings.LostObjectUnreadable.Text}\n\n{ex.Message}";
        }
    }

    private string GetOptions()
    {
        string options = string.Empty;
        if (Unreachable)
        {
            options += " --unreachable";
        }

        if (FullCheck)
        {
            options += " --full";
        }

        if (NoReflogs)
        {
            options += " --no-reflogs";
        }

        return options;
    }

    private int CreateLostFoundTags()
    {
        List<LostObjectItem> selectedLostObjects = [.. LostObjects.Where(o => o.IsSelected)];
        if (selectedLostObjects.Count == 0)
        {
            _messageBoxes.ShowWarning(Strings.SelectLostObjectsToRestoreMessage.Text, Strings.SelectLostObjectsToRestoreCaption.Text);
            return 0;
        }

        int currentTag = 0;
        foreach (LostObjectItem lostObject in selectedLostObjects)
        {
            currentTag++;
            string tagName = lostObject.Kind == LostObjectKind.Tag ? lostObject.TagName! : currentTag.ToString();
            _host.CreateTag($"{RestoredObjectsTagPrefix}{tagName}", lostObject.ObjectId);
        }

        return currentTag;
    }

    private void DeleteLostFoundTags()
    {
        foreach (string tagName in _host.GetTagNames().Where(name => name.StartsWith(RestoredObjectsTagPrefix)).ToList())
        {
            _host.DeleteTag(tagName);
        }
    }

    internal static string GuessFileTypeWithContent(string content)
    {
        foreach ((string start, string type) in LanguagesStartOfFile)
        {
            if (content.StartsWith(start, StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }

        return "txt";
    }

    private static string GuessFileNameWithContent(string content, string hash)
        => $"LOST_FOUND_{hash}.{GuessFileTypeWithContent(content)}";
}
