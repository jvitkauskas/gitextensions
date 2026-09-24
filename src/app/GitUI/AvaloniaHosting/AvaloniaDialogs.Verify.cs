using GitCommands;
using GitCommands.Git;
using GitCommands.Git.Tag;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.HelperDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the verify database dialog and its text viewer (docs/avalonia-port/PLAN.md, phase 3).</summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowVerify(IWin32Window? owner, IGitUICommands commands)
    {
        ShowDialog(
            () =>
            {
                VerifyWindow window = new();
                window.DataContext = new VerifyViewModel(ViewStrings.Load<VerifyStrings>(), new VerifyHost(commands, window), new MessageBoxService(window));
                return window;
            },
            owner,
            positionName: nameof(FormVerify));
        return true;
    }

    /// <summary>Shows the Avalonia port of <c>FormEdit</c>; returns <see langword="false"/> if it is disabled.</summary>
    public static bool TryShowTextViewer(IWin32Window? owner, string text, string fileName, bool isReadOnly)
    {
        ShowDialog(
            () => new TextViewerWindow { DataContext = new TextViewerViewModel(ViewStrings.Load<TextViewerStrings>(), text, fileName, isReadOnly) },
            owner,
            positionName: nameof(FormEdit));
        return true;
    }

    private sealed class VerifyHost(IGitUICommands commands, DialogWindow window) : IVerifyHost
    {
        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        /// <summary>As <c>FormVerify.UpdateLostObjects</c>.</summary>
        public IReadOnlyList<LostObjectItem>? ReadLostObjects(string options) => AvaloniaUi.RunInHostContext(() =>
        {
            string cmdOutput = FormProcess.ReadDialog(Owner, commands, arguments: $"fsck-objects{options}", Module.WorkingDir, input: null, useDialogSettings: true);
            if (FormProcess.IsOperationAborted(cmdOutput))
            {
                return null;
            }

            List<FormVerify.LostObject> lostObjects = [.. cmdOutput
                .Split(Delimiters.LineFeedAndCarriageReturn)
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => FormVerify.LostObject.TryParse(Module, s))
                .WhereNotNull()];

            FormVerify.LostObject[] commits = [.. lostObjects.Where(o => o.ObjectType == FormVerify.LostObjectType.Commit)];
            List<string> metadata = new(commits.Length);
            int batchSize = 30_000 / (ObjectId.Sha1CharCount + 1); // Based on process max command line length and hash length (with a margin)
            for (int currentBatch = 0; currentBatch * batchSize < commits.Length; ++currentBatch)
            {
                FormVerify.LostObject[] nextBatch = [.. commits.Skip(currentBatch * batchSize).Take(batchSize)];
                metadata.AddRange(FormVerify.LostObject.GetCommitsMetadata(Module, nextBatch.Select(c => c.ObjectId.ToString())));
            }

            for (int i = 0; i < commits.Length; i++)
            {
                commits[i].FillCommitData(Module, metadata[i]);
            }

            return (IReadOnlyList<LostObjectItem>?)[.. lostObjects.Select(ToItem)];
        });

        public void SaveLostObjects(string options) => AvaloniaUi.RunInHostContext(()
            => FormProcess.ShowDialog(Owner, commands, arguments: $"fsck-objects --lost-found{options}", Module.WorkingDir, input: null, useDialogSettings: true));

        public void Prune() => AvaloniaUi.RunInHostContext(()
            => FormProcess.ShowDialog(Owner, commands, arguments: "prune", Module.WorkingDir, input: null, useDialogSettings: true));

        public string GetContent(LostObjectItem item)
            => Module.ShowObject(item.ObjectId, returnRaw: item.Kind == LostObjectKind.Blob) ?? "";

        public bool ShowCreateTag(ObjectId objectId) => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowCreateTag(Owner, commands, objectId, out bool created))
            {
                return created;
            }

            using FormCreateTag form = new(commands, objectId);
            return form.ShowDialog(Owner) == DialogResult.OK;
        });

        public bool ShowCreateBranch(ObjectId objectId) => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowCreateBranch(Owner, commands, objectId, new(BranchName: null), out bool created))
            {
                return created;
            }

            using FormCreateBranch form = new(commands, objectId);
            return form.ShowDialog(Owner) == DialogResult.OK;
        });

        public void CreateTag(string tagName, ObjectId objectId)
            => AvaloniaUi.RunInHostContext(() => new GitTagController(commands).CreateTag(new GitCreateTagArgs(tagName, objectId), Owner));

        public IEnumerable<string> GetTagNames() => Module.GetRefs(RefsFilter.Tags).Select(tag => tag.Name);

        public void DeleteTag(string tagName) => Module.DeleteTag(tagName);

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);

        /// <summary>As <c>FormVerify.saveAsToolStripMenuItem_Click</c>.</summary>
        public void SaveBlobAs(ObjectId objectId, string fileName, string filter, string extension) => AvaloniaUi.RunInHostContext(() =>
        {
            using SaveFileDialog fileDialog = new()
            {
                InitialDirectory = Module.WorkingDir,
                FileName = fileName,
                Filter = filter,
                DefaultExt = extension,
                AddExtension = true
            };
            if (fileDialog.ShowDialog(Owner) == DialogResult.OK)
            {
                Module.SaveBlobAs(fileDialog.FileName, objectId.ToString());
            }
        });

        /// <summary>As <c>FormVerify.ViewCurrentItem</c>.</summary>
        public void View(string text, string fileName) => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowTextViewer(Owner, text, fileName, isReadOnly: true))
            {
                return;
            }

            using FormEdit form = new(commands, text, fileName);
            form.IsReadOnly = true;
            form.ShowDialog(Owner);
        });

        public void RunInBackground(Action work, Action then)
            => ThreadHelper.FileAndForget(async () =>
            {
                await TaskScheduler.Default;
                work();
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                then();
            });

        private static LostObjectItem ToItem(FormVerify.LostObject lostObject)
            => new(
                lostObject.ObjectType switch
                {
                    FormVerify.LostObjectType.Commit => LostObjectKind.Commit,
                    FormVerify.LostObjectType.Blob => LostObjectKind.Blob,
                    FormVerify.LostObjectType.Tree => LostObjectKind.Tree,
                    FormVerify.LostObjectType.Tag => LostObjectKind.Tag,
                    _ => LostObjectKind.Other,
                },
                lostObject.ObjectId,
                lostObject.RawType)
            {
                Parent = lostObject.Parent is { IsZero: false } parent ? parent : null,
                Author = lostObject.Author,
                Subject = lostObject.Subject,
                Date = lostObject.Date,
                TagName = lostObject.TagName,
            };
    }
}
