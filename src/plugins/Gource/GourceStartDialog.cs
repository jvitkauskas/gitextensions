using System.Diagnostics;
using System.Drawing.Imaging;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.Avatars;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.Gource;

/// <summary>Shows the Avalonia port of <see cref="GourceStart"/> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class GourceStartDialog
{
    /// <summary>
    ///  Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form; otherwise
    ///  <paramref name="gourcePath"/> and <paramref name="gourceArguments"/> are the values to save in the settings.
    /// </summary>
    public static bool TryShow(GitUIEventArgs args, string pathToGource, string arguments, out string gourcePath, out string gourceArguments)
    {
        gourcePath = pathToGource;
        gourceArguments = arguments;
        if (!AvaloniaPluginDialogs.IsEnabledFor(nameof(GourceStart)))
        {
            return false;
        }

        GourceStartViewModel? viewModel = null;
        AvaloniaPluginDialogs.ShowDialog(
            () =>
            {
                GourceStartWindow window = new();
                viewModel = new GourceStartViewModel(
                    ViewStrings.Load<GourceStartStrings>(),
                    pathToGource,
                    args.GitModule.WorkingDir,
                    arguments,
                    new Host(args.GitModule),
                    AvaloniaPluginDialogs.CreateMessageBoxService(window),
                    AvaloniaPluginDialogs.CreateFileDialogService(window),
                    AvaloniaPluginDialogs.ErrorCaption);
                window.DataContext = viewModel;
                return window;
            },
            args.OwnerForm);

        if (viewModel is not null)
        {
            gourcePath = viewModel.PathToGource;
            gourceArguments = viewModel.GourceArguments;
        }

        return true;
    }

    private sealed class Host(IGitModule module) : IGourceStartHost
    {
        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));

        /// <summary>As <c>GourceStart.RunRealCmdDetached</c>.</summary>
        public void StartDetached(string command, string arguments, string workingDirectory)
            => Process.Start(new ProcessStartInfo
            {
                FileName = "\"" + command + "\"",
                Arguments = arguments,
                WorkingDirectory = workingDirectory
            });

        /// <summary>As <c>GourceStart.LoadAvatarsAsync</c> (a copy: keep in sync).</summary>
        public async Task<string> LoadAvatarsAsync()
        {
            string gourceAvatarsDir = Path.Join(Path.GetTempPath(), "GitAvatars");

            Directory.CreateDirectory(gourceAvatarsDir);

            foreach (string file in Directory.GetFiles(gourceAvatarsDir))
            {
                File.Delete(file);
            }

            GitArgumentBuilder args = new("log") { "--pretty=format:\"%aE|%aN\"" };
            string[] lines = module.GitExecutable.GetOutput(args).Split('\n');

            IEnumerable<(string email, string name)> authors = lines.Select(
                line =>
                {
                    string[] bits = line.Split('|');
                    return (email: bits[0], name: bits[1]);
                })
                .Where(t => !string.IsNullOrWhiteSpace(t.email) && !string.IsNullOrWhiteSpace(t.name))
                .GroupBy(t => t.name)
                .Select(g => (g.First().email, name: g.Key));

            await Task.WhenAll(authors.Select(DownloadImageAsync));

            return gourceAvatarsDir;

            async Task DownloadImageAsync((string email, string name) author)
            {
                try
                {
                    Image? image = await AvatarService.DefaultProvider.GetAvatarAsync(author.email, author.name, imageSize: 90);
                    string filename = author.name + ".png";

                    if (image is null || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        return;
                    }

                    string filePath = Path.Join(gourceAvatarsDir, filename);
                    image.Save(filePath, ImageFormat.Png);
                }
                catch
                {
                    // Do nothing, as GourceStart.
                }
            }
        }
    }
}
