using System.Diagnostics;
using GitExtUtils.GitUI.Theming;
using GitUI.Theming;

namespace GitUI.UserControls.RevisionGrid.Graph;

public static class RevisionGraphLaneColor
{
    public static int GetColorForLane(int seed)
    {
        return Math.Abs(seed) % PresetGraphColors.Count;
    }

    public static Color NonRelativeColor { get; } = AppColor.GraphNonRelativeBranch.GetThemeColor();

    internal static readonly List<Color> PresetGraphColors = [];

    static RevisionGraphLaneColor()
    {
        Color[] branchColors = [.. Enum.GetNames<AppColor>()
            .Where(name => name.StartsWith(nameof(AppColor.GraphBranch1)[..^1]))
            .Select(name => Enum.Parse<AppColor>(name).GetThemeColor())
            .Where(color => !color.IsEmpty)
            .Distinct()];

        const int minBranchColors = 4;
        if (branchColors.Length < minBranchColors)
        {
            Trace.WriteLine(@"At least {minBranchColors} different graph colors must be configured - using crying fallback");
            branchColors = [Color.Cyan, Color.Magenta, Color.Yellow, Color.Lime];
        }

        PresetGraphColors.AddRange(branchColors);
    }
}
