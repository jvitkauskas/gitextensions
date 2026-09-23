using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls;

/// <summary>Strings of the patch grid; ids match <c>PatchGrid</c>.</summary>
public sealed class PatchGridStrings : ViewStrings
{
    public PatchGridStrings()
        : base("PatchGrid")
    {
        Status = Add("Status", "HeaderText", "Status");
        Action = Add("Action", "HeaderText", "Action");
        FileName = Add("FileName", "HeaderText", "Name");
        Subject = Add("subjectDataGridViewTextBoxColumn", "HeaderText", "Subject");
        Author = Add("authorDataGridViewTextBoxColumn", "HeaderText", "Author");
        Date = Add("dateDataGridViewTextBoxColumn", "HeaderText", "Date");
        CommitHash = Add("CommitHash", "HeaderText", "Commit hash");
        UnableToShowPatchDetails = Add("_unableToShowPatchDetails", "Text", "Unable to show details of patch file.");
    }

    public TranslatedText Status { get; }

    public TranslatedText Action { get; }

    public TranslatedText FileName { get; }

    public TranslatedText Subject { get; }

    public TranslatedText Author { get; }

    public TranslatedText Date { get; }

    public TranslatedText CommitHash { get; }

    public TranslatedText UnableToShowPatchDetails { get; }
}

/// <summary>
///  A commit of a rebase or a patch being applied (port of <c>PatchFile</c>).
/// </summary>
public sealed class PatchItem
{
    /// <summary>The path of the patch file (patches only).</summary>
    public string? FullName { get; set; }

    /// <summary>The action of an interactive rebase, e.g. <c>pick</c>.</summary>
    public string? Action { get; set; }

    /// <summary>The name of the patch file, e.g. <c>0001</c>.</summary>
    public string? Name { get; set; }

    /// <summary>The commit (zero for patches).</summary>
    public ObjectId ObjectId { get; set; }

    public string? Author { get; set; }

    /// <summary>The subject of a patch, or the message of a commit (shown on one line, the whole text in the tooltip).</summary>
    public string? Subject { get; set; }

    public string? Date { get; set; }

    public bool IsNext { get; set; }

    public bool IsSkipped { get; set; }

    public bool IsApplied { get; set; }

    /// <summary>As <c>PatchFile.Status</c> (not translated, as in WinForms).</summary>
    public string Status
    {
        get
        {
            if (IsSkipped)
            {
                return "Skipped";
            }

            if (IsApplied)
            {
                return "Applied";
            }

            if (IsNext)
            {
                return "Applying...";
            }

            if (!string.IsNullOrEmpty(FullName) && !File.Exists(FullName))
            {
                return "Applied";
            }

            return "";
        }
    }

    /// <summary>The commit hash column; empty for patches (which have no commit).</summary>
    public string CommitHash => ObjectId.IsZero ? "" : ObjectId.ToString();
}

/// <summary>Operations of the patch grid that need the host (the git directory, the other dialogs).</summary>
public interface IPatchGridHost
{
    /// <summary>
    ///  As <c>PatchGrid.GetPatches</c> without the skipped commits: the commits of an interactive rebase
    ///  (<c>GetInteractiveRebasePatchFiles</c>, if there is a <c>git-rebase-todo</c>), otherwise the patches of the rebase
    ///  directory (<c>GetRebasePatchFiles</c>).
    /// </summary>
    IReadOnlyList<PatchItem> LoadPatches();

    /// <summary>Shows a commit (<c>StartFormCommitDiff</c>).</summary>
    void ShowCommit(ObjectId objectId);

    /// <summary>Shows a patch file (<c>StartViewPatchDialog</c>).</summary>
    void ShowPatch(string fullName);
}

/// <summary>
///  View model of the list of the commits of a rebase, or of the patches being applied (port of <c>PatchGrid</c>).
/// </summary>
public sealed partial class PatchGridViewModel : ObservableObject
{
    private readonly IPatchGridHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly string _errorCaption;

    /// <param name="isManagingRebase">
    ///  Whether the grid shows the commits of a rebase (with their action and hash), otherwise patches (with their name)
    ///  (<c>IsManagingRebase</c>).
    /// </param>
    /// <param name="skipped">
    ///  The commits or patches skipped so far (<c>SetSkipped</c>); the dialogs keep it across their instances, as the
    ///  WinForms forms did in a static list.
    /// </param>
    /// <param name="errorCaption">The caption of the error messages (<c>TranslatedStrings.Error</c>).</param>
    public PatchGridViewModel(PatchGridStrings strings, IPatchGridHost host, IMessageBoxService messageBoxes, bool isManagingRebase, IList<PatchItem> skipped, string errorCaption)
    {
        ArgumentNullException.ThrowIfNull(skipped);
        Strings = strings;
        _host = host;
        _messageBoxes = messageBoxes;
        IsManagingRebase = isManagingRebase;
        Skipped = skipped;
        _errorCaption = errorCaption;
    }

    public PatchGridStrings Strings { get; }

    public bool IsManagingRebase { get; }

    public IList<PatchItem> Skipped { get; }

    /// <summary>The rows, as <c>PatchFiles</c>, or <see langword="null"/> before <see cref="Initialize"/>.</summary>
    public IReadOnlyList<PatchItem>? PatchFiles { get; private set; }

    public ObservableCollection<PatchItem> Patches { get; } = [];

    [ObservableProperty]
    public partial PatchItem? SelectedPatch { get; set; }

    /// <summary>As <c>Initialize</c>: loads the rows.</summary>
    public void Initialize() => DisplayPatches(GetPatches());

    /// <summary>As <c>RefreshGrid</c>: reloads the rows, keeping which ones were skipped.</summary>
    public void RefreshGrid()
    {
        IReadOnlyList<PatchItem> patchFiles = PatchFiles ?? [];
        IReadOnlyList<PatchItem> updatedPatches = GetPatches();
        if (updatedPatches.Count != patchFiles.Count && !IsManagingRebase)
        {
            Trace.Write($"PatchGrid: RefreshGrid: PatchFiles count {patchFiles.Count} is different from updatedPatches count {updatedPatches.Count}. This should not happen.");
        }

        for (int i = 0; i < Math.Min(updatedPatches.Count, patchFiles.Count); i++)
        {
            updatedPatches[i].IsSkipped = patchFiles[i].IsSkipped;
        }

        DisplayPatches(updatedPatches);
    }

    /// <summary>As <c>SelectCurrentlyApplyingPatch</c>: selects the commit or patch being applied (the view shows it).</summary>
    public void SelectCurrentlyApplyingPatch()
    {
        if (PatchFiles?.FirstOrDefault(p => p.IsNext) is { } next)
        {
            SelectedPatch = next;
        }
    }

    /// <summary>As <c>Patches_DoubleClick</c>: shows the commit, or the patch file.</summary>
    [RelayCommand]
    private void OpenSelected()
    {
        if (SelectedPatch is not { } patchFile)
        {
            return;
        }

        if (patchFile.ObjectId is { IsZeroOrArtificial: false } patchObjectId)
        {
            // Normal commit selected
            _host.ShowCommit(patchObjectId);
            return;
        }

        if (string.IsNullOrEmpty(patchFile.FullName))
        {
            _messageBoxes.ShowError(Strings.UnableToShowPatchDetails.Text, _errorCaption);
            return;
        }

        _host.ShowPatch(patchFile.FullName);
    }

    /// <summary>As <c>GetPatches</c>: marks the skipped commits (by id) and patches (by name) before the current one.</summary>
    private IReadOnlyList<PatchItem> GetPatches()
    {
        IReadOnlyList<PatchItem> patches = _host.LoadPatches();
        if (Skipped.Count == 0)
        {
            return patches;
        }

        foreach (PatchItem patchFile in patches.TakeWhile(p => !p.IsNext).Where(p => Skipped.Any(s => p.ObjectId == s.ObjectId && p.Name == s.Name)))
        {
            patchFile.IsSkipped = true;
        }

        return patches;
    }

    /// <summary>As <c>DisplayPatches</c>.</summary>
    private void DisplayPatches(IReadOnlyList<PatchItem> patchFiles)
    {
        PatchFiles = patchFiles;
        SelectedPatch = null;
        Patches.Clear();
        foreach (PatchItem patchFile in patchFiles)
        {
            Patches.Add(patchFile);
        }

        SelectCurrentlyApplyingPatch();
    }
}
