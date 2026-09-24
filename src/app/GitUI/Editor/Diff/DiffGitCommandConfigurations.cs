using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;

namespace GitUI.Editor.Diff;

/// <summary>
///  The git configuration of the commands whose output the file viewer shows (as the WinForms highlight services set it,
///  e.g. <c>DiffHighlightService.GetGitCommandConfiguration</c>).
/// </summary>
public static class DiffGitCommandConfigurations
{
    /// <summary>The configuration of a diff command (<paramref name="command"/>); <see langword="null"/> for the defaults without git's colors.</summary>
    public static IGitCommandConfiguration ForDiff(IGitModule module, bool useGitColoring, string command)
    {
        if (!useGitColoring)
        {
            // Use default
            return null!;
        }

        GitCommandConfiguration commandConfiguration = new();
        IReadOnlyList<GitConfigItem> items = GitCommandConfiguration.Default.Get(command);
        foreach (GitConfigItem cfg in items)
        {
            commandConfiguration.Add(cfg, command);
        }

        // https://git-scm.com/docs/git-diff#Documentation/git-diff.txt---color-moved-wsltmodesgt
        // Disable by default, document that this can be enabled.
        SetIfUnsetInGit(key: "diff.colormovedws", value: "no");

        // https://git-scm.com/docs/git-diff#Documentation/git-diff.txt-diffwordRegex
        // Set to "minimal" diff unless configured.
        SetIfUnsetInGit(key: "diff.wordregex", value: "\"[a-z0-9_]+|.\"");

        // dimmed-zebra highlights borders better than the default "zebra"
        SetIfUnsetInGit(key: "diff.colormoved", value: "dimmed-zebra");

        // Use reverse color to follow GE theme
        string reverse = AppSettings.ReverseGitColoring.Value ? "reverse" : "";

        SetIfUnsetInGit(key: "color.diff.old", value: $"red {reverse}");
        SetIfUnsetInGit(key: "color.diff.new", value: $"green {reverse}");

        if (AppSettings.ReverseGitColoring.Value)
        {
            // Fix: Force black foreground to avoid that foreground is calculated to white
            GitVersion supportsBrightColors = new("2.26.0.0");
            if (module.GitVersion >= supportsBrightColors)
            {
                SetIfUnsetInGit(key: "color.diff.oldmoved", value: "black brightmagenta");
                SetIfUnsetInGit(key: "color.diff.newmoved", value: "black brightblue");
                SetIfUnsetInGit(key: "color.diff.oldmovedalternative", value: "black brightcyan");
                SetIfUnsetInGit(key: "color.diff.newmovedalternative", value: "black brightyellow");
            }
            else
            {
                SetIfUnsetInGit(key: "color.diff.oldmoved", value: "reverse bold magenta");
                SetIfUnsetInGit(key: "color.diff.newmoved", value: "reverse bold blue");
                SetIfUnsetInGit(key: "color.diff.oldmovedalternative", value: "reverse bold cyan");
                SetIfUnsetInGit(key: "color.diff.newmovedalternative", value: "reverse bold yellow");
            }
        }

        // Set dimmed colors, default is gray dimmed/italic
        SetIfUnsetInGit(key: "color.diff.oldmoveddimmed", value: $"magenta dim {reverse}");
        SetIfUnsetInGit(key: "color.diff.newmoveddimmed", value: $"blue dim {reverse}");
        SetIfUnsetInGit(key: "color.diff.oldmovedalternativedimmed", value: $"cyan dim {reverse}");
        SetIfUnsetInGit(key: "color.diff.newmovedalternativedimmed", value: $"yellow dim {reverse}");

        // range-diff
        if (command == "range-diff")
        {
            // No override for contextBold, contextDimmed
            SetIfUnsetInGit(key: "color.diff.oldbold", value: $"brightred {reverse}");
            SetIfUnsetInGit(key: "color.diff.newbold", value: $"brightgreen {reverse}");
            SetIfUnsetInGit(key: "color.diff.olddimmed", value: $"red dim {reverse}");
            SetIfUnsetInGit(key: "color.diff.newdimmed", value: $"green dim {reverse}");
        }

        return commandConfiguration;

        void SetIfUnsetInGit(string key, string value)
        {
            // Note: Only check Windows, not WSL settings
            if (string.IsNullOrEmpty(module.GetEffectiveSetting(key)))
            {
                commandConfiguration.Add(new GitConfigItem(key, value), command);
            }
        }
    }

    /// <summary>The configuration of a combined diff (<c>CombinedDiffHighlightService</c>).</summary>
    public static IGitCommandConfiguration ForCombinedDiff(IGitModule module, bool useGitColoring)
        => ForDiff(module, useGitColoring, "diff-tree");

    /// <summary>The configuration of a patch (<c>PatchHighlightService</c>).</summary>
    public static IGitCommandConfiguration ForPatch(IGitModule module, bool useGitColoring)
        => ForDiff(module, useGitColoring, "diff");

    /// <summary>The configuration of a range diff (<c>RangeDiffHighlightService</c>), always with git's colors.</summary>
    public static IGitCommandConfiguration ForRangeDiff(IGitModule module)
        => ForDiff(module, useGitColoring: true, "range-diff");

    /// <summary>The configuration of a grep (<c>GrepHighlightService</c>), whose output is parsed.</summary>
    public static IGitCommandConfiguration ForGrep(IGitModule module)
    {
        GitCommandConfiguration commandConfiguration = new();
        IReadOnlyList<GitConfigItem> items = GitCommandConfiguration.Default.Get("grep");
        foreach (GitConfigItem cfg in items)
        {
            commandConfiguration.Add(cfg, "grep");
        }

        // No coloring, values are parsed
        commandConfiguration.Add(new GitConfigItem("color.grep.linenumber", ""), "grep");
        commandConfiguration.Add(new GitConfigItem("color.grep.separator", ""), "grep");

        SetIfUnsetInGit(key: "color.grep.function", value: "white dim reverse");
        if (AppSettings.ReverseGitColoring.Value)
        {
            SetIfUnsetInGit(key: "color.grep.matchselected", value: "red bold reverse");
        }

        return commandConfiguration;

        void SetIfUnsetInGit(string key, string value)
        {
            // Note: Only check Windows, not WSL settings
            if (string.IsNullOrEmpty(module.GetEffectiveSetting(key)))
            {
                commandConfiguration.Add(new GitConfigItem(key, value), "grep");
            }
        }
    }
}
