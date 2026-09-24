using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Patches;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI;
using GitExtUtils.GitUI.Theming;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Properties;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using Microsoft;
using ResourceManager;

namespace GitUI.Editor;

/// <summary>The git arguments of the options of the file viewer (moved out of the WinForms <c>FileViewer</c>).</summary>
internal static partial class FileViewerArguments
{
    [GeneratedRegex(@"\n\s*(@@|##)\s+(?<file>[^#:\n]+)", RegexOptions.ExplicitCapture)]
    internal static partial Regex FileNameRegex { get; }

    /// <summary>Whether the difftastic difftool is configured in the repository, also for the Avalonia viewer (<c>FileViewerHost</c>).</summary>
    internal static bool IsDifftasticConfigured(IGitModule module)
    {
        try
        {
            const string difftasticCmd = "difftool.difftastic.cmd";
            return !string.IsNullOrEmpty(module.GetEffectiveSetting(difftasticCmd));
        }
        catch (Exception exception)
        {
            Trace.WriteLine(exception);
            return false;
        }
    }

    /// <summary>The grep arguments of the options of a viewer, also for the Avalonia viewer (<c>FileViewerHost</c>).</summary>
    internal static ArgumentString GetExtraGrepArguments(bool showEntireFile, int numberOfContextLines, bool treatAllFilesAsText)
    {
        int contextLines = showEntireFile ? 100_000 : numberOfContextLines;
        return new ArgumentBuilder
        {
            "-h",
            $"--context={contextLines}",
            { treatAllFilesAsText, "--text" },
        };
    }

    /// <summary>
    ///  The difftool arguments and the environment of difftastic for the options of a viewer, also for the Avalonia viewer
    ///  (<c>FileViewerHost</c>).
    /// </summary>
    /// <param name="viewerWidth">The width of the viewer in pixels.</param>
    /// <param name="width">The width of the output (<c>DFT_WIDTH</c>).</param>
    internal static (ArgumentString Args, string ExtraCacheKey) GetDifftasticArguments(IgnoreWhitespaceKind ignoreWhitespace, bool showSyntaxHighlightingInDiff,
        bool showEntireFile, int numberOfContextLines, bool treatAllFilesAsText, int viewerWidth, out int width)
    {
        EnvironmentAbstraction env = new();
        StringBuilder extraCacheKey = new();

        // Difftastic coloring is always used (AppSettings.UseGitColoring.Value is not used).
        // Allow user to override with difftool command line options.
        SetEnvironmentVariable("DFT_COLOR", "always");

        // DFT_BACKGROUND="dark" applies bold-bold colors, "light" corresponds better with Git colors
        SetEnvironmentVariable("DFT_BACKGROUND", "light");
        SetEnvironmentVariable("DFT_SYNTAX_HIGHLIGHT", showSyntaxHighlightingInDiff ? "on" : "off");
        int contextLines = showEntireFile ? 9000 : numberOfContextLines;
        SetEnvironmentVariable("DFT_CONTEXT", contextLines.ToString());

        // Reasonable similar to IgnoreWhitespaceKind.Eol
        SetEnvironmentVariable("DFT_STRIP_CR", ignoreWhitespace == IgnoreWhitespaceKind.None ? "off" : "on");

        // Guess a reasonable even column number from viewer width, so scrollbar is (barely) activated.
        // At least 2*(2+linenoLength) of the width is used for difftastic lineno.
        // DFT_WIDTH is also used when parsing in GE, must be in environment.
        width = Math.Max(88, Math.Min(200, DpiUtil.Scale(viewerWidth) / 7)) / 2 * 2;
        SetEnvironmentVariable("DFT_WIDTH", width.ToString());

        // Also export to WSL environment.
        env.SetEnvironmentVariable("WSLENV", "DFT_COLOR:DFT_BACKGROUND:DFT_SYNTAX_HIGHLIGHT:DFT_CONTEXT:DFT_STRIP_CR:DFT_WIDTH");

        return (new ArgumentBuilder
        {
            "--tool=difftastic",
            { treatAllFilesAsText, "--text" },
        },
        extraCacheKey.ToString());

        void SetEnvironmentVariable(string variable, string value)
        {
            env.SetEnvironmentVariable(variable, value);
            extraCacheKey.AppendFormat($";{variable}={value}");
        }
    }

    /// <summary>The diff arguments of the options of a viewer, also for the Avalonia viewer (<c>FileViewerHost</c>).</summary>
    internal static ArgumentString GetExtraDiffArguments(IgnoreWhitespaceKind ignoreWhitespace, bool showEntireFile, int numberOfContextLines, bool treatAllFilesAsText,
        DiffDisplayAppearance diffDisplayAppearance, bool isRangeDiff, bool isCombinedDiff)
    {
        return new ArgumentBuilder
        {
            { ignoreWhitespace == IgnoreWhitespaceKind.AllSpace, "--ignore-all-space" },
            { ignoreWhitespace == IgnoreWhitespaceKind.Change, "--ignore-space-change" },
            { ignoreWhitespace == IgnoreWhitespaceKind.Eol, "--ignore-space-at-eol" },
            { showEntireFile, "--inter-hunk-context=9000 --unified=9000", $"--unified={numberOfContextLines}" },

            // Handle zero context as showing no file changes, to get the summary only
            { isRangeDiff && numberOfContextLines == 0, "--no-patch " },
            { treatAllFilesAsText, "--text" },
            { !isCombinedDiff && diffDisplayAppearance == DiffDisplayAppearance.GitWordDiff, "--word-diff=color" },
        };
    }
}
