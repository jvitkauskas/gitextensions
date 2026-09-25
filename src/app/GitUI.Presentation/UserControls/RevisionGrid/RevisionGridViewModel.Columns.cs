using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>The commands of the "RevisionGrid" hotkeys; the codes are the ones of <c>HotkeyCommands.RevisionGrid</c>.</summary>
public enum RevisionGridCommand
{
    ToggleRevisionGraph = 0,
    RevisionFilter = 1,
    ToggleAuthorDateCommitDate = 2,
    ToggleOrderRevisionsByDate = 3,
    ToggleShowRelativeDate = 4,
    ToggleDrawNonRelativesGray = 5,
    ToggleShowGitNotes = 6,
    ToggleShowGitNotesColumn = 47,
    ToggleHideMergeCommits = 8,
    ShowAllBranches = 9,
    ShowCurrentBranchOnly = 10,
    ShowFilteredBranches = 11,
    ShowRemoteBranches = 12,
    ShowFirstParent = 13,
    GoToParent = 14,
    GoToFirstParent = 45,
    GoToLastParent = 46,
    GoToChild = 15,
    ToggleHighlightSelectedBranch = 16,
    NextQuickSearch = 17,
    PrevQuickSearch = 18,
    SelectCurrentRevision = 19,
    GoToCommit = 20,
    NavigateBackward = 21,
    NavigateForward = 22,
    SelectAsBaseToCompare = 23,
    CompareToBase = 24,
    CreateFixupCommit = 25,
    ToggleShowTags = 26,
    CompareToWorkingDirectory = 27,
    CompareToCurrentBranch = 28,
    CompareToBranch = 29,
    CompareSelectedCommits = 30,
    GoToMergeBase = 31,
    OpenCommitsWithDifftool = 32,
    ToggleBetweenArtificialAndHeadCommits = 33,
    ShowReflogReferences = 34,
    ShowStashes = 35,
    ResetRevisionFilter = 36,
    ResetRevisionPathFilter = 37,
    SelectNextForkPointAsDiffBase = 38,
    NavigateBackward_AlternativeHotkey = 39,
    NavigateForward_AlternativeHotkey = 40,
    DeleteRef = 41,
    RenameRef = 42,
    CreateSquashCommit = 43,
    CreateAmendCommit = 44,
}

/// <summary>A right click on the label of a reference (<c>_rightClickedHitInfo</c>), with the modifier keys pressed.</summary>
public sealed record RevisionGridRefMenuRequest(IGitRef GitRef, bool Shift, bool Control);

/// <summary>
///  The columns of the revision grid beyond the first version (avatar, notes, build status), the highlighting (author,
///  hover, non-relatives, selected branch) and the hotkeys of <c>RevisionGridControl</c>.
/// </summary>
public sealed partial class RevisionGridViewModel
{
    /// <summary>The size of the avatars of the avatar column, as its WinForms cell (the row height without the padding).</summary>
    public const int AvatarSize = 18;

    private readonly Dictionary<(string Email, string? Name), Task<byte[]?>> _avatars = [];
    private string? _authorEmailToHighlight;
    private bool _authorEmailInitialized;
    private int _hoverRequest;

    /// <summary>As <c>ShowAuthorAvatarColumn</c>.</summary>
    [ObservableProperty]
    public partial bool ShowAvatarColumn { get; set; }

    /// <summary>As <c>ShowGitNotesColumn</c>.</summary>
    [ObservableProperty]
    public partial bool ShowNotesColumn { get; set; }

    /// <summary>
    ///  Whether the build status column is shown (<c>BuildStatusColumnProvider.ApplySettings</c>: the integration is enabled
    ///  or a build server was detected, and its icon or text is shown).
    /// </summary>
    [ObservableProperty]
    public partial bool ShowBuildStatusColumn { get; set; }

    /// <summary>As <c>ShowBuildStatusIconColumn</c>: the symbol of the build status is shown.</summary>
    [ObservableProperty]
    public partial bool ShowBuildStatusIcon { get; set; } = true;

    /// <summary>As <c>ShowBuildStatusTextColumn</c>: the description of the build status is shown.</summary>
    [ObservableProperty]
    public partial bool ShowBuildStatusText { get; set; }

    /// <summary>As <c>RevisionGraphDrawNonRelativesGray</c>: the lanes of the revisions not related to HEAD are gray.</summary>
    [ObservableProperty]
    public partial bool DrawNonRelativesGray { get; set; }

    /// <summary>As <c>RevisionGraphDrawNonRelativesTextGray</c>: the texts of the revisions not related to HEAD are gray.</summary>
    [ObservableProperty]
    public partial bool DrawNonRelativesTextGray { get; set; }

    /// <summary>As <c>HighlightAuthoredRevisions</c>: the rows of the highlighted author have the highlight background.</summary>
    [ObservableProperty]
    public partial bool HighlightAuthoredRevisions { get; set; }

    /// <summary>
    ///  Whether the branch of a revision is highlighted (<c>RevisionGraphDrawStyle.HighlightSelected</c>, until the next
    ///  refresh): the other revisions are drawn as non-relatives.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBranchHighlighted { get; private set; }

    /// <summary>The revisions highlighted while a reference label is hovered (<c>HoverHighlightCalculator</c>); none if null.</summary>
    [ObservableProperty]
    public partial IReadOnlySet<ObjectId>? HoverHighlightedIds { get; private set; }

    /// <summary>Raised when the relatives changed (a highlighted branch), so that the view draws the rows again.</summary>
    public event EventHandler? RelativesChanged;

    /// <summary>The configured "RevisionGrid" hotkeys.</summary>
    public IReadOnlyList<HotkeyBinding> Hotkeys { get; set; } = [];

    /// <summary>Runs the commands of the hotkeys that the grid does not handle itself (the menus of the window).</summary>
    public Func<RevisionGridCommand, bool>? CommandHandler { get; set; }

    /// <summary>The reference label right-clicked last, for the focused context menu (read and cleared when it is built).</summary>
    public RevisionGridRefMenuRequest? RefMenuRequest { get; set; }

    /// <summary>Whether the revision of the row is drawn as a relative of HEAD (or of the highlighted branch).</summary>
    public bool IsRelative(int rowIndex) => Graph.IsRowRelative(rowIndex);

    /// <summary>Whether the texts of the row are gray (<c>RevisionDataGridView</c> with <c>RevisionGraphDrawNonRelativesTextGray</c>).</summary>
    public bool IsTextGray(int rowIndex) => DrawNonRelativesTextGray && rowIndex < CachedGraphRowCount && !Graph.IsRowRelative(rowIndex);

    /// <summary>Whether the row has the background of the highlighted author (<c>RevisionDataGridView.GetBackground</c>).</summary>
    public bool IsAuthoredHighlight(RevisionGridRow row) => HighlightAuthoredRevisions && !row.Revision.IsArtificial && row.IsAuthorHighlighted;

    /// <summary>As <c>HighlightSelectedBranch</c>: the selected revision and its ancestors are the relatives, until the next refresh.</summary>
    public void HighlightSelectedBranch()
    {
        if (SelectedRow?.Revision is not { } revision)
        {
            return;
        }

        Graph.HighlightBranch(revision.ObjectId);
        IsBranchHighlighted = true;
        RelativesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///  As <c>UpdateLaneHighlight</c>: highlights the ancestry of the hovered reference label (none to clear), in the
    ///  visible rows.
    /// </summary>
    public async Task SetHoverReferenceAsync(IGitRef? gitRef, int rowIndex, int firstVisibleRow, int visibleRowCount)
    {
        int request = ++_hoverRequest;
        IReadOnlySet<ObjectId>? ids;
        try
        {
            ids = await _host.GetHoverHighlightAsync(Graph, gitRef, rowIndex, firstVisibleRow, visibleRowCount);
        }
        catch (OperationCanceledException)
        {
            // A later hover, or the highlight did not change.
            return;
        }

        if (request == _hoverRequest)
        {
            HoverHighlightedIds = ids;
        }
    }

    /// <summary>The tooltip of the author and avatar columns; none for the artificial commits.</summary>
    public string? GetAuthorToolTip(RevisionGridRow row) => row.Revision.IsArtificial ? null : _host.GetAuthorToolTip(row.Revision);

    /// <summary>As <c>AvatarColumnProvider.OnCellPainting</c>: loads the avatar of the author of a row shown in the avatar column.</summary>
    public void RequestAvatar(RevisionGridRow row)
    {
        if (!ShowAvatarColumn || row.Avatar is not null || row.Revision.IsArtificial || row.Revision.AuthorEmail is not { } email)
        {
            return;
        }

        (string, string?) key = (email, row.Revision.Author);
        if (!_avatars.TryGetValue(key, out Task<byte[]?>? avatar))
        {
            avatar = _host.GetAvatarAsync(email, row.Revision.Author, AvatarSize);
            _avatars[key] = avatar;
        }

        _ = SetAvatarAsync(row, avatar);
    }

    /// <summary>Forgets the loaded avatars, e.g. after the cache of the avatars was cleared.</summary>
    public void ClearAvatars()
    {
        _avatars.Clear();
        foreach (RevisionGridRow row in Rows)
        {
            row.Avatar = null;
        }
    }

    private static async Task SetAvatarAsync(RevisionGridRow row, Task<byte[]?> avatar)
    {
        try
        {
#pragma warning disable VSTHRD003 // The task of an author's avatar is shared by the rows of the author.
            row.Avatar = await avatar;
#pragma warning restore VSTHRD003
        }
        catch (Exception)
        {
            // As the WinForms column: without an avatar, nothing is drawn.
        }
    }

    /// <summary>As <c>OpenBuildReport</c>: the report of the build status of the row, in the browser.</summary>
    public void OpenBuildReport(RevisionGridRow row)
    {
        if (row.BuildStatus?.Url is { } url && !string.IsNullOrWhiteSpace(url))
        {
            _host.OpenUrl(url);
        }
    }

    /// <summary>The hotkey of a command, if one is configured (the <c>ShortcutKeyDisplayString</c> of the menus).</summary>
    public HotkeyBinding? GetHotkey(RevisionGridCommand command) => Hotkeys.FirstOrDefault(h => h.CommandCode == (int)command);

    /// <summary>As <c>RevisionGridControl.ExecuteCommand</c>: the navigation here, the other commands through <see cref="CommandHandler"/>.</summary>
    public bool ExecuteHotkey(RevisionGridCommand command)
    {
        switch (command)
        {
            case RevisionGridCommand.GoToParent: GoToParent(); break;
            case RevisionGridCommand.GoToFirstParent: GoToFirstParent(); break;
            case RevisionGridCommand.GoToLastParent: GoToLastParent(); break;
            case RevisionGridCommand.GoToChild: GoToChild(); break;
            case RevisionGridCommand.NextQuickSearch: QuickSearchNext(down: true); break;
            case RevisionGridCommand.PrevQuickSearch: QuickSearchNext(down: false); break;
            case RevisionGridCommand.NavigateBackward:
            case RevisionGridCommand.NavigateBackward_AlternativeHotkey: NavigateBackward(); break;
            case RevisionGridCommand.NavigateForward:
            case RevisionGridCommand.NavigateForward_AlternativeHotkey: NavigateForward(); break;
            case RevisionGridCommand.ToggleHighlightSelectedBranch: HighlightSelectedBranch(); break;
            case RevisionGridCommand.SelectNextForkPointAsDiffBase: SelectNextForkPointAsDiffBase(); break;
            default: return CommandHandler?.Invoke(command) ?? false;
        }

        return true;
    }

    // As HighlightRevisionsByAuthor (AuthorRevisionHighlighting.ProcessRevisionSelectionChange): the author of the selected
    // revision, or the user without a selection; unchanged when several revisions are selected.
    private void UpdateAuthorHighlight()
    {
        IReadOnlyList<GitRevision> selected = GetSelectedRevisionsLatestSelectedFirst();
        if (selected.Count > 1)
        {
            return;
        }

        string? email = selected.Count == 1 ? selected[0].AuthorEmail : _host.UserEmail;
        if (_authorEmailInitialized && string.Equals(email, _authorEmailToHighlight, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _authorEmailInitialized = true;
        _authorEmailToHighlight = email;
        foreach (RevisionGridRow row in Rows)
        {
            row.IsAuthorHighlighted = IsAuthorHighlightedFor(row.Revision);
        }
    }

    // As AuthorRevisionHighlighting.IsHighlighted (the author of the artificial commits is not drawn in bold).
    private bool IsAuthorHighlightedFor(GitRevision revision)
        => !revision.IsArtificial && !string.IsNullOrWhiteSpace(revision.AuthorEmail) && string.Equals(revision.AuthorEmail, _authorEmailToHighlight, StringComparison.OrdinalIgnoreCase);
}
