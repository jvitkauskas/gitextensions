using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.Translations;

namespace GitExtensions.Plugins.ReleaseNotesGenerator;

/// <summary>Shows the Avalonia port of <c>ReleaseNotesGeneratorForm</c> (docs/avalonia-port/PLAN.md, phase 7).</summary>
internal static class ReleaseNotesGeneratorDialog
{
    /// <summary>Returns <see langword="false"/> when the port is disabled, in which case the caller shows the WinForms form.</summary>
    public static bool TryShow(GitUIEventArgs args)
    {
        AvaloniaPluginDialogs.ShowDialog(
            () =>
            {
                ReleaseNotesGeneratorWindow window = new();
                window.DataContext = new ReleaseNotesGeneratorViewModel(
                    ViewStrings.Load<ReleaseNotesGeneratorStrings>(),
                    args.GitModule.GitExecutable,
                    new Clipboard(),
                    AvaloniaPluginDialogs.CreateMessageBoxService(window));
                return window;
            },
            args.Owner);
        return true;
    }

    private sealed class Clipboard : IReleaseNotesClipboard
    {
        public void CopyText(string text) => AvaloniaUi.RunInHostContext(() => ClipboardUtil.TrySetText(text));

        public void CopyHtml(string htmlFragment) => AvaloniaUi.RunInHostContext(() => HtmlFragment.CopyToClipboard(htmlFragment));
    }
}
