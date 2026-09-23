using GitExtUtils.GitUI.Theming;

namespace GitUI.UserControls.RevisionGrid.Graph;

/// <summary>
///  The number of lane colors, which the layout needs to choose different colors for neighbouring lanes
///  (<see cref="LaneInfo"/>); the UI draws lane color <c>i</c> with its <c>i</c>-th graph branch color.
/// </summary>
/// <remarks>
///  The UI sets <see cref="ColorCountProvider"/> from its theme (GitUI: <c>RevisionGraphLaneColor</c>). Until then
///  the count of the default graph colors is used, as <c>RevisionGraphLaneColor</c> computes it for the default theme.
/// </remarks>
public static class RevisionGraphLanePalette
{
    private const int MinBranchColors = 4;

    private static readonly Lazy<int> _defaultColorCount = new(() =>
    {
        int count = Enum.GetNames<AppColor>()
            .Where(name => name.StartsWith(nameof(AppColor.GraphBranch1)[..^1]))
            .Select(name => AppColorDefaults.GetBy(Enum.Parse<AppColor>(name)))
            .Where(color => !color.IsEmpty)
            .Distinct()
            .Count();
        return count < MinBranchColors ? MinBranchColors : count;
    });

    /// <summary>Provides the number of distinct lane colors of the theme.</summary>
    public static Func<int>? ColorCountProvider { get; set; }

    public static int ColorCount => ColorCountProvider?.Invoke() ?? _defaultColorCount.Value;

    public static int GetColorForLane(int seed) => Math.Abs(seed) % ColorCount;
}
