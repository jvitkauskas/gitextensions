using GitCommands;
using GitCommands.Git.Gpg;
using GitUI.Presentation.CommandsDialogs;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>The GPG tab of the Avalonia main window (<c>FillGpgInfoAsync</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseGpgHost
    {
        private GpgInfoProvider? _gpgInfoProvider;

        public bool ShowGpgInformation => AppSettings.ShowGpgInformation.Value;

        public Task<GpgInfo?> LoadGpgInfoAsync(GitRevision revision)
        {
            _gpgInfoProvider ??= new GpgInfoProvider(new GitGpgController(() => Module));
            return _gpgInfoProvider.LoadGpgInfoAsync(revision);
        }
    }
}
