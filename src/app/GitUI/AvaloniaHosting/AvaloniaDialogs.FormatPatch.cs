using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the format patch dialog, on the Avalonia revision grid (docs/avalonia-port/PLAN.md, phase 4).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowFormatPatch(IWin32Window? owner, IGitUICommands commands)
    {
        FormatPatchViewModel? viewModel = null;
        try
        {
            ShowDialog(
                () =>
                {
                    FormatPatchWindow window = new();

                    // As FormFormatPatch: several revisions can be selected, without the artificial commits.
                    RevisionGridViewModel grid = new(
                        new RevisionGridHost(commands, GetRevisionFilterFactory(showCurrentBranchOnly: false, lastRevisionToDisplayHash: null), showArtificial: false),
                        new RevisionGridDisplayOptions(AppSettings.RelativeDate, AppSettings.ShowAuthorDate, GitUI.TranslatedStrings.SearchingFor, AppSettings.RevisionGridQuickSearchTimeout))
                    {
                        MultiSelect = true,
                    };
                    viewModel = new FormatPatchViewModel(
                        ViewStrings.Load<FormatPatchStrings>(),
                        grid,
                        AppSettings.LastFormatPatchDir,
                        commands.Module.GetSelectedBranch(),
                        TranslatedStrings.Error,
                        new FormatPatchHost(commands),
                        new MessageBoxService(window),
                        new AvaloniaFileDialogService(window));
                    window.DataContext = viewModel;
                    grid.Load();
                    return window;
                },
                owner,
                positionName: "FormFormatPatch");
        }
        finally
        {
            viewModel?.Dispose();
        }

        return true;
    }

    private sealed class FormatPatchHost(IGitUICommands commands) : IFormatPatchHost
    {
        public string FormatPatch(string from, string to, string outputPath, int start)
            => commands.Module.FormatPatch(from, to, outputPath, start == 0 ? null : start);

        public void RememberOutputPath(string outputPath)
        {
            // As FormFormatPatch.OutputPath_TextChanged.
            if (Directory.Exists(outputPath))
            {
                AppSettings.LastFormatPatchDir = outputPath;
            }
        }
    }
}
