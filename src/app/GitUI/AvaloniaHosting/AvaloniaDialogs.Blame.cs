using System.Text;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.Blame;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the blame dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>FormBlame</c> (the <c>blame</c> verb), of HEAD as <c>FormBlame</c> without a revision.</summary>
    public static bool TryShowBlame(IWin32Window? owner, IGitUICommands commands, string fileName, int? initialLine)
    {
        if (!AvaloniaUi.IsEnabledFor(nameof(FormBlame)))
        {
            return false;
        }

        if (string.IsNullOrEmpty(fileName) || commands.Module.GetRevision() is not { } revision)
        {
            return false;
        }

        ShowDialog(
            () =>
            {
                BlameWindow window = new();
                window.DataContext = new BlameDialogViewModel(
                    ViewStrings.Load<BlameStrings>(),
                    new BlameHost(commands, window),
                    new CommitInfoHost(commands),
                    fileName,
                    revision,
                    initialLine);
                return window;
            },
            owner,
            positionName: nameof(FormBlame));
        return true;
    }

    /// <summary>The git and dialog operations of <c>BlameControl</c>.</summary>
    internal sealed class BlameHost(IGitUICommands commands, DialogWindow window, Func<Encoding>? getEncoding = null) : IBlameHost
    {
        private readonly GitRevisionSummaryBuilder _summaryBuilder = new();

        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        public BlameDisplayOptions Options => new(
            ShowAuthor: AppSettings.BlameShowAuthor,
            ShowAuthorDate: AppSettings.BlameShowAuthorDate,
            ShowAuthorTime: AppSettings.BlameShowAuthorTime,
            ShowOriginalFilePath: AppSettings.BlameShowOriginalFilePath,
            DisplayAuthorFirst: AppSettings.BlameDisplayAuthorFirst,
            ShowLineNumbers: AppSettings.BlameShowLineNumbers,
            ShowAuthorAvatar: AppSettings.BlameShowAuthorAvatar);

        public async Task<GitBlame> GetBlameAsync(string fileName, ObjectId objectId, CancellationToken cancellationToken)
        {
            // As FormBlame: the files encoding (the viewer's encoding in the file history).
            Encoding encoding = getEncoding?.Invoke() ?? Module.FilesEncoding;
            await TaskScheduler.Default;
            GitBlame blame = Module.Blame(fileName, objectId.ToString(), encoding, lines: null, cancellationToken);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return blame;
        }

        public GitRevision? GetRevision(ObjectId objectId) => Module.GetRevision(objectId);

        public string Describe(GitBlameCommit commit) => commit.ToString(_summaryBuilder.BuildSummary);

        public int GetOriginalLineInPreviousCommit(GitRevision revision, string fileName, int line)
            => new GitBlameParser(() => Module).GetOriginalLineInPreviousCommit(revision, fileName, line);

        public void ShowCommitDiff(ObjectId objectId) => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowCommitDiff(Owner, commands, objectId))
            {
                return;
            }

            using FormCommitDiff form = new(commands, objectId);
            form.ShowDialog(Owner);
        });

        public void ShowRevisionFiltered(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);
    }
}
