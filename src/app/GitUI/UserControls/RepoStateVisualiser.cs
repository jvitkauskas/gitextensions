using GitExtensions.Extensibility.Git;

namespace GitUI.UserControls;

internal interface IRepoStateVisualiser
{
    /// <summary>The icon of the state of the working directory (an icon of <see cref="EmbeddedIcons"/>) and its color (the overlay of the taskbar).</summary>
    (string image, Color color) Invoke(IReadOnlyList<GitItemStatus>? allChangedFiles);
}

internal sealed class RepoStateVisualiser : IRepoStateVisualiser
{
    internal static readonly (string, Color) Clean = ("RepoStateClean", Color.Lime);
    internal static readonly (string, Color) Dirty = ("RepoStateDirty", Color.LightSalmon);
    internal static readonly (string, Color) DirtySubmodules = ("RepoStateDirtySubmodules", Color.Orange);
    internal static readonly (string, Color) Mixed = ("RepoStateMixed", Color.Yellow);
    internal static readonly (string, Color) Staged = ("RepoStateStaged", Color.LightSkyBlue);
    internal static readonly (string, Color) Unknown = ("RepoStateUnknown", Color.Gray);
    internal static readonly (string, Color) UntrackedOnly = ("RepoStateUntrackedOnly", Color.BlueViolet);

    public (string image, Color color) Invoke(IReadOnlyList<GitItemStatus>? allChangedFiles)
    {
        if (allChangedFiles is null)
        {
            return Unknown;
        }

        int indexCount = 0;
        int workTreeSubmodulesCount = 0;
        int notTrackedCount = 0;

        foreach (GitItemStatus status in allChangedFiles)
        {
            if (status.Staged == StagedStatus.Index)
            {
                indexCount++;
            }

            if (status.Staged == StagedStatus.WorkTree && status.IsSubmodule)
            {
                workTreeSubmodulesCount++;
            }

            if (!status.IsTracked)
            {
                notTrackedCount++;
            }
        }

        int workTreeCount = allChangedFiles.Count - indexCount;

        return (indexCount, workTreeCount) switch
        {
            (0, 0) => Clean,
            (0, _) when workTreeCount == notTrackedCount => UntrackedOnly,
            (0, _) when workTreeCount != workTreeSubmodulesCount => Dirty,
            (0, _) => DirtySubmodules,
            (_, 0) => Staged,
            (_, _) => Mixed
        };
    }
}
