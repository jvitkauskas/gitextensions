using System.Text;
using GitCommands;
using GitCommands.Patches;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the view patch dialog, the first user of the diff mode of the Avalonia text editor (docs/avalonia-port/PLAN.md, phase 3).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowViewPatch(IWin32Window? owner, IGitUICommands commands, string? patchFile)
    {
        ShowDialog(
            () =>
            {
                ViewPatchWindow window = new();
                window.DataContext = new ViewPatchViewModel(ViewStrings.Load<ViewPatchStrings>(), new ViewPatchHost(commands, window), patchFile);
                return window;
            },
            owner,
            positionName: "FormViewPatch");
        return true;
    }

    private sealed class ViewPatchHost(IGitUICommands commands, DialogWindow window) : IViewPatchHost
    {
        public string? BrowsePatchFile(string filter, string title) => AvaloniaUi.RunInHostContext(() =>
        {
            using OpenFileDialog dialog = new()
            {
                Filter = filter,
                InitialDirectory = @".",
                Title = title,
            };
            return dialog.ShowDialog(new NativeWindowOwner(window)) == DialogResult.OK ? dialog.FileName : null;
        });

        public IReadOnlyList<Patch> LoadPatches(string path)
        {
            // As FormViewPatch.LoadPatchFile.
            string text = File.ReadAllText(path, GitModule.LosslessEncoding);
            return [.. PatchProcessor.CreatePatchesFromString(text, new Lazy<Encoding>(() => commands.Module.FilesEncoding))];
        }
    }
}
