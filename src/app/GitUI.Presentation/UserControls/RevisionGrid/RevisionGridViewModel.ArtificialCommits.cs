using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using GitExtensions.Extensibility.Git;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>A count of the summary of an artificial commit (as <c>DrawArtificialCount</c>): its image, and the number (none for the clean or unknown state).</summary>
public sealed record ArtificialChangeCount(string Icon, string Count);

/// <summary>The changes of an artificial commit by kind (as <c>ArtificialCommitChangeCount</c>).</summary>
public sealed class ArtificialCommitChanges
{
    /// <param name="items">The changes, or <see langword="null"/> when they are not known (e.g. while updating).</param>
    public ArtificialCommitChanges(IReadOnlyList<GitItemStatus>? items)
    {
        DataValid = items is not null;
        items ??= [];
        Changed = [.. items.Where(item => !item.IsNew && !item.IsDeleted && !item.IsSubmodule)];
        New = [.. items.Where(item => item.IsNew && !item.IsSubmodule)];
        Deleted = [.. items.Where(item => item.IsDeleted && !item.IsSubmodule)];
        SubmodulesChanged = [.. items.Where(item => item.IsSubmodule && item.IsChanged)];
        SubmodulesDirty = [.. items.Where(item => item.IsSubmodule && item.IsDirty)];
    }

    public bool DataValid { get; }

    public IReadOnlyList<GitItemStatus> Changed { get; }

    public IReadOnlyList<GitItemStatus> New { get; }

    public IReadOnlyList<GitItemStatus> Deleted { get; }

    public IReadOnlyList<GitItemStatus> SubmodulesChanged { get; }

    public IReadOnlyList<GitItemStatus> SubmodulesDirty { get; }

    public bool HasChanges => DataValid && (Changed.Count + New.Count + Deleted.Count + SubmodulesChanged.Count + SubmodulesDirty.Count) > 0;

    /// <summary>As <c>DrawArtificialRevision</c>: the image and number of each kind of change, else the clean or unknown image.</summary>
    public IReadOnlyList<ArtificialChangeCount> Counts
    {
        get
        {
            if (!DataValid)
            {
                return [new("RepoStateUnknown", "")];
            }

            if (!HasChanges)
            {
                return [new("RepoStateClean", "")];
            }

            List<ArtificialChangeCount> counts = [];
            Add(Changed, "FileStatusModified");
            Add(New, "FileStatusAdded");
            Add(Deleted, "FileStatusRemoved");
            Add(SubmodulesChanged, "SubmoduleRevisionDown");
            Add(SubmodulesDirty, "SubmoduleDirty");
            return counts;

            void Add(IReadOnlyList<GitItemStatus> items, string icon)
            {
                if (items.Count > 0)
                {
                    counts.Add(new(icon, items.Count.ToString()));
                }
            }
        }
    }

    /// <summary>As <c>GetSummary</c>: the tooltip, with the first files of each kind.</summary>
    public string GetSummary()
    {
        StringBuilder builder = new();

        // As the WinForms grid, not translated.
        Append(Changed, "changed file");
        Append(Deleted, "deleted file");
        Append(New, "new file");
        Append(SubmodulesChanged, "changed submodule");
        Append(SubmodulesDirty, "dirty submodule");

        return builder.ToString();

        void Append(IReadOnlyList<GitItemStatus> items, string singular)
        {
            if (items.Count == 0)
            {
                return;
            }

            if (builder.Length != 0)
            {
                builder.AppendLine();
            }

            builder.Append($"{items.Count} {singular}{(items.Count == 1 ? "" : "s")}").AppendLine();

            const int maxItems = 5;
            for (int i = 0; i < maxItems && i < items.Count; i++)
            {
                builder.Append("- ").AppendLine(items[i].Name);
            }

            if (items.Count > maxItems)
            {
                int unlistedCount = items.Count - maxItems;
                builder.Append("- (").Append(unlistedCount).Append(" more file");
                if (unlistedCount != 1)
                {
                    builder.Append('s');
                }

                builder.Append(')').AppendLine();
            }
        }
    }
}

/// <summary>The changes of an artificial row, after its subject (the count of <c>ShowGitStatusForArtificialCommits</c>).</summary>
public sealed partial class RevisionGridRow
{
    /// <summary>The changes of the working directory or the index; <see langword="null"/> for a commit, or when they are not shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeCounts), nameof(ChangesToolTip))]
    public partial ArtificialCommitChanges? Changes { get; internal set; }

    public IReadOnlyList<ArtificialChangeCount> ChangeCounts => Changes?.Counts ?? [];

    /// <summary>As <c>MessageColumnProvider.TryGetToolTip</c> of an artificial commit.</summary>
    public string? ChangesToolTip => Changes is { HasChanges: true } changes ? changes.GetSummary() : null;
}

/// <summary>The changes of the artificial commits (<c>UpdateArtificialCommitCount</c>).</summary>
public sealed partial class RevisionGridViewModel
{
    private IReadOnlyList<GitItemStatus>? _workingDirectoryStatus;

    /// <summary>Whether the artificial commits show the counts of their changes (<c>AppSettings.ShowGitStatusForArtificialCommits</c>).</summary>
    public bool ShowArtificialCommitChanges { get; init; }

    /// <summary>
    ///  As <c>UpdateArtificialCommitCount</c>: the status of the working directory, split between the working directory and the
    ///  index rows; <see langword="null"/> when it is not known.
    /// </summary>
    public void UpdateArtificialCommitCount(IReadOnlyList<GitItemStatus>? status)
    {
        _workingDirectoryStatus = status;
        foreach (ObjectId id in (ObjectId[])[ObjectId.WorkTreeId, ObjectId.IndexId])
        {
            if (Graph.TryGetRowIndex(id, out int index) && index < Rows.Count && Rows[index].ObjectId == id)
            {
                Rows[index].Changes = GetArtificialCommitChanges(id);
            }
        }
    }

    private ArtificialCommitChanges? GetArtificialCommitChanges(ObjectId id)
    {
        if (!ShowArtificialCommitChanges || (id != ObjectId.WorkTreeId && id != ObjectId.IndexId))
        {
            return null;
        }

        StagedStatus staged = id == ObjectId.WorkTreeId ? StagedStatus.WorkTree : StagedStatus.Index;
        return new ArtificialCommitChanges(_workingDirectoryStatus?.Where(item => item.Staged == staged).ToList());
    }
}
