using System.Text;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtUtils;
using GitExtUtils.GitUI;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.Blame;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.RepositoryHosts;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the blame dialog (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>Shows the Avalonia port of <c>FormBlame</c> (the <c>blame</c> verb), of HEAD as <c>FormBlame</c> without a revision.</summary>
    public static bool TryShowBlame(IWin32Window? owner, IGitUICommands commands, string fileName, int? initialLine)
    {
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
            positionName: "FormBlame");
        return true;
    }

    /// <summary>The git and dialog operations of <c>BlameControl</c>.</summary>
    internal sealed class BlameHost(IGitUICommands commands, DialogWindow window, Func<Encoding>? getEncoding = null) : IBlameHost
    {
        private readonly GitRevisionSummaryBuilder _summaryBuilder = new();

        // As ConfigureRepositoryHostPlugin: the repository host plugin of the repository, found once.
        private IRepositoryHostPlugin? _gitHoster;
        private bool _gitHosterFound;

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

        public void ShowCommitDiff(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => TryShowCommitDiff(Owner, commands, objectId));

        public void ShowRevisionFiltered(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);

        // As BlameControl.ProcessBlame: the avatar of the provider, or the default image without an email.
        public async Task<byte[]?> GetAvatarAsync(string email, string? name, int size, CancellationToken cancellationToken)
        {
            Image? image = null;
            if (!string.IsNullOrWhiteSpace(email))
            {
                image = await GitUI.Avatars.AvatarService.DefaultProvider.GetAvatarAsync(email, name, DpiUtil.Scale(size));
            }

            cancellationToken.ThrowIfCancellationRequested();
            image ??= Properties.Images.User80;
            using MemoryStream stream = new();
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }

        // As ConfigureContextMenu, for the repository host plugins of API v2 (IBlameContextMenuProvider): their items for the line.
        public IReadOnlyList<MenuModelItem> GetRepositoryHostMenuItems(string fileName, int lineIndex, ObjectId blameId)
            => AvaloniaUi.RunInHostContext<IReadOnlyList<MenuModelItem>>(() =>
            {
                if (!_gitHosterFound)
                {
                    _gitHosterFound = true;
                    _gitHoster = PluginRegistry.TryGetGitHosterForModule(Module);
                }

                if (_gitHoster is not IBlameContextMenuProvider provider || blameId.IsZero)
                {
                    return [];
                }

                return ToMenuModel(provider.GetBlameContextMenuItems(new GitBlameContext(fileName, lineIndex, lineIndex, blameId)));
            });
    }

    /// <summary>The menu items of a plugin (plugin API v2) as a menu model, run in the host context.</summary>
    internal static IReadOnlyList<MenuModelItem> ToMenuModel(IEnumerable<PluginMenuItem> items)
        => [.. items.Select(ToMenuModel)];

    private static MenuModelItem ToMenuModel(PluginMenuItem item)
        => item.IsSeparator
            ? MenuModelItem.Separator
            : new MenuModelItem(
                TranslatedText.ToAccessKeyText(item.Text),
                item.OnClick is null ? null : () => AvaloniaUi.RunInHostContext(item.Click),
                ToPng(item.Icon),
                item.Children.Count == 0 ? null : ToMenuModel(item.Children),
                IsEnabled: item.IsEnabled);
}
