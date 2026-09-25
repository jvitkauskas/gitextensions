using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.Blame;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>The blame of the diff and file tree tabs of the Avalonia main window (the <c>BlameControl</c> of <c>RevisionDiffControl</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseBlameHost
    {
        public BlameViewModel CreateBlame()
            => new(ViewStrings.Load<BlameStrings>(), new BlameHost(_commands, _window), new CommitInfoHost(_commands)) { ShowCommitInfo = false };

        public bool UseDiffViewerForBlame => AppSettings.UseDiffViewerForBlame.Value;

        // As RevisionGridControl.GetActualRevision: the parents of the revision, which a path filter rewrites.
        public GitRevision GetActualRevision(GitRevision revision)
        {
            if (revision.IsArtificial)
            {
                return revision;
            }

            revision = revision.Clone();
            revision.ParentIds = [.. Module.GetParents(revision.ObjectId)];
            return revision;
        }

        public GitRevision? GetRevision(ObjectId objectId) => Module.GetRevision(objectId, shortFormat: true, loadRefs: true);

        public ObjectId? GetCurrentCheckout() => Module.GetCurrentCheckout();
    }
}
