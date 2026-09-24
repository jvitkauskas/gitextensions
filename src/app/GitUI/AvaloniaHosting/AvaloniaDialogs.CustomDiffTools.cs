using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.FileStatusList;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>The custom difftools of the difftool menus (port of <c>CustomDiffMergeToolProvider</c>).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The time before the difftools are read (<c>FormBrowseToolDelay</c>): not while git log and git diff run.</summary>
    internal const int CustomDiffToolsDelay = 8000;

    /// <summary>
    ///  As <c>LoadCustomDiffMergeTools</c>: the difftools configured in git, from the shared cache, after
    ///  <paramref name="delay"/>; none unless there are several (the menus have no submenu then), or if they are disabled
    ///  (<c>AppSettings.ShowAvailableDiffTools</c>).
    /// </summary>
    internal static async Task<IReadOnlyList<string>> LoadCustomDiffToolsAsync(IGitModule module, int delay, CancellationToken cancellationToken)
    {
        if (!AppSettings.ShowAvailableDiffTools)
        {
            return [];
        }

        List<string> tools = [.. await CustomDiffMergeToolCache.DiffToolCache.GetToolsAsync(module, delay, cancellationToken)];
        return tools.Count > 1 ? tools : [];
    }

    /// <summary>
    ///  The submenu of a difftool item (as <c>LoadCustomDiffMergeTools</c> fills the <c>DropDown</c>): the tools, the first (the
    ///  default) bold, and "Disable this dropdown"; <see langword="null"/> without tools.
    /// </summary>
    internal static IReadOnlyList<MenuModelItem>? CreateCustomDiffToolItems(IReadOnlyList<string> tools, Action<string> open, Action disable)
        => tools.Count <= 1
            ? null
            : [.. tools.Select((tool, index) => new MenuModelItem(tool, () => open(tool), IsBold: index == 0)), MenuModelItem.Separator, new MenuModelItem(ResourceManager.TranslatedStrings.DisableMenuItem, disable)];

    /// <summary>
    ///  Reads the difftools for the difftool items of <paramref name="files"/> (as <c>FileStatusList.LoadCustomDifftools</c>);
    ///  "Disable this dropdown" removes them.
    /// </summary>
    internal static void LoadCustomDiffTools(FileStatusListViewModel files, IGitModule module)
    {
        files.DisableCustomDiffToolsAction = () =>
        {
            AppSettings.ShowAvailableDiffTools = false;
            files.CustomDiffTools = [];
        };
        ThreadHelper.FileAndForget(async () =>
        {
            IReadOnlyList<string> tools = await LoadCustomDiffToolsAsync(module, CustomDiffToolsDelay, CancellationToken.None);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            files.CustomDiffTools = tools;
        });
    }
}
