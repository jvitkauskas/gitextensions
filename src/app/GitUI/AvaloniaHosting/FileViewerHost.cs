using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Git;
using GitCommands.Patches;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.Editor;
using GitUI.Editor.Diff;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.FileStatusList;
using ICSharpCode.TextEditor.Util;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The changes of a file for the Avalonia file viewer: a port of <c>GitUIExtensions.ViewChangesAsync</c> and the parts of
///  <c>FileViewer</c> it uses (docs/avalonia-port/PLAN.md, phase 3); keep it in sync. Range diffs, git grep and difftastic are
///  not ported yet.
/// </summary>
internal sealed partial class FileViewerHost(IGitUICommands commands) : IFileViewerHost
{
    [GeneratedRegex(@"warning: .*has type .* expected .*", RegexOptions.ExplicitCapture)]
    private static partial Regex FileModeWarningRegex { get; }

    private readonly FileViewerStrings _strings = ViewStrings.Load<FileViewerStrings>();

    private IGitModule Module => commands.Module;

    public IThemeColors ThemeColors => HostThemeColors.Instance;

    /// <summary>The dialog of the viewer, the owner of its questions and errors.</summary>
    public DialogWindow? Window { get; init; }

    private IWin32Window? Owner => Window is null ? null : new AvaloniaDialogs.NativeWindowOwner(Window);

    public bool IsPatchAppearance => AppSettings.DiffDisplayAppearance.Value == GitCommands.Settings.DiffDisplayAppearance.Patch;

    public IReadOnlyList<HotkeyBinding> Hotkeys => field ??= [.. commands.GetRequiredService<IHotkeySettingsLoader>().LoadHotkeys(FileViewer.HotkeySettingsName)
        .Select(hotkey => new HotkeyBinding(hotkey.CommandCode, (int)hotkey.KeyData))];

    public void CopyToClipboard(string text, bool adjustLineEndings)
        => ClipboardUtil.TrySetText(adjustLineEndings ? text.AdjustLineEndings(Module.GetEffectiveSetting<AutoCRLFType>("core.autocrlf")) : text);

    /// <summary>
    ///  As <c>StageSelectedLines</c>, <c>ResetNoncommittedSelectedLines</c> and <c>ApplySelectedLines</c> of <c>FileViewer</c>:
    ///  the worktree and the index are staged / unstaged and reset in the index; the lines of a commit are applied (or
    ///  reverted) to the working directory.
    /// </summary>
    public bool ApplyLinePatch(LinePatchOperation operation, FileStatusEntry entry, StagedStatus stagedStatus, string text, int selectionStart, int selectionLength, byte[]? filePreamble)
    {
        Encoding encoding = Module.FilesEncoding;
        GitItemStatus item = entry.Item;
        byte[]? patch;
        GitArgumentBuilder args;
        bool patchUpdatesDiff = true;
        if (operation == LinePatchOperation.Reset && stagedStatus is StagedStatus.WorkTree or StagedStatus.Index)
        {
            // As ResetNoncommittedSelectedLines: reset only on an explicit confirmation.
            if (!AvaloniaUi.RunInHostContext(() => MessageBoxes.Confirm(Owner, TranslatedStrings.ResetSelectedLinesConfirmation, TranslatedStrings.ResetChangesCaption, MessageBoxIcon.Warning)))
            {
                return false;
            }

            bool currentItemStaged = entry.SecondRevision.ObjectId == ObjectId.IndexId;
            patch = item.IsNew
                ? PatchManager.GetSelectedLinesAsNewPatch(Module, item.Name, text, selectionStart, selectionLength, encoding, reset: true, filePreamble ?? [], GetUpdateTreeId(item, entry.SecondRevision.ObjectId, default).ToString())
                : currentItemStaged
                    ? PatchManager.GetSelectedLinesAsPatch(text, selectionStart, selectionLength, isIndex: true, encoding, reset: true, item.IsNew, item.IsRenamed)
                    : PatchManager.GetResetWorkTreeLinesAsPatch(text, selectionStart, selectionLength, encoding);
            args = new GitArgumentBuilder("apply")
            {
                "--whitespace=nowarn",
                { currentItemStaged, "--reverse --index" }
            };
        }
        else if (operation != LinePatchOperation.Reset && stagedStatus is StagedStatus.WorkTree or StagedStatus.Index)
        {
            // As StageSelectedLines(stage).
            bool stage = operation == LinePatchOperation.Stage;
            patch = item.IsNew
                ? PatchManager.GetSelectedLinesAsNewPatch(Module, item.Name, text, selectionStart, selectionLength, encoding, reset: false, filePreamble ?? [], GetUpdateTreeId(item, entry.SecondRevision.ObjectId, default).ToString())
                : PatchManager.GetSelectedLinesAsPatch(text, selectionStart, selectionLength, isIndex: !stage, encoding, reset: false, item.IsNew, item.IsRenamed);
            args = new GitArgumentBuilder("apply")
            {
                "--cached",
                "--index",
                "--whitespace=nowarn",
                { !stage, "--reverse" }
            };
        }
        else
        {
            // As ApplySelectedLines: the lines of a commit are cherry-picked (or reverted) into the working directory.
            bool reverse = operation == LinePatchOperation.Reset;
            patch = item.IsNew
                ? PatchManager.GetSelectedLinesAsNewPatch(Module, item.Name, text, selectionStart, selectionLength, encoding, reset: reverse, filePreamble ?? [],
                    (reverse ? GetUpdateTreeId(item, entry.SecondRevision.ObjectId, default) : default).ToString())
                : !reverse
                    ? PatchManager.GetSelectedLinesAsPatch(text, selectionStart, selectionLength, isIndex: false, encoding, reset: false, item.IsNew, item.IsRenamed)
                    : PatchManager.GetResetWorkTreeLinesAsPatch(text, selectionStart, selectionLength, encoding);
            args = new GitArgumentBuilder("apply")
            {
                "--3way",
                "--index",
                "--whitespace=nowarn"
            };
            patchUpdatesDiff = false;
        }

        if (patch?.Length is not > 0)
        {
            return false;
        }

        ProcessApplyOutput(args, patch, patchUpdatesDiff, encoding);
        return true;
    }

    /// <summary>As <c>ProcessApplyOutput</c>.</summary>
    private void ProcessApplyOutput(GitArgumentBuilder args, byte[] patch, bool patchUpdatesDiff, Encoding encoding)
    {
        ExecutionResult result = Module.GitExecutable.Execute(args, inputWriter => inputWriter.BaseStream.Write(patch), throwOnErrorExit: false);
        string output = result.AllOutput.Trim();
        if (OperatingSystem.IsWindows())
        {
            // remove file mode warnings
            output = output.RemoveLines(FileModeWarningRegex.IsMatch);
        }

        if (!result.ExitedSuccessfully
            && (patchUpdatesDiff || !AvaloniaUi.RunInHostContext(() => MergeConflictHandler.HandleMergeConflicts(commands, Owner, false, false))))
        {
            AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(Owner, $"{output}\n\n{encoding.GetString(patch)}", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
        else if (!result.ExitedSuccessfully || output.StartsWith("error: ") || output.StartsWith("warning: "))
        {
            Trace.WriteLineIf(!string.IsNullOrWhiteSpace(output), $"Patch output: {result.ExitCode}:{output} for: git {args}");
        }
    }

    public bool ReverseGitColoring => AppSettings.ReverseGitColoring.Value;

    private static bool UseGitColoring => AppSettings.UseGitColoring.Value;

    /// <summary>As <c>encodingToolStripComboBox_SelectedIndexChanged</c>: the chosen encoding, or the files encoding.</summary>
    private Encoding GetEncoding(string? encodingName)
        => encodingName is null
            ? Module.FilesEncoding
            : AppSettings.AvailableEncodings.Values.FirstOrDefault(e => e.EncodingName == encodingName) ?? Module.FilesEncoding;

    public FileViewerSettings Settings
    {
        get => new(AppSettings.ShowNonPrintingChars.Value, AppSettings.ShowEntireFile.Value, AppSettings.NumberOfContextLines, AppSettings.IgnoreWhitespaceKind.Value);
        set
        {
            AppSettings.ShowNonPrintingChars.Value = value.ShowNonPrintingChars;
            AppSettings.ShowEntireFile.Value = value.ShowEntireFile;
            AppSettings.NumberOfContextLines = value.NumberOfContextLines;
            AppSettings.IgnoreWhitespaceKind.Value = value.IgnoreWhitespace;
        }
    }

    public IReadOnlyList<string> AvailableEncodings => [.. AppSettings.AvailableEncodings.Values.Select(e => e.EncodingName)];

    public string FilesEncoding => Module.FilesEncoding.EncodingName;

    /// <summary>As <c>settingsButton_Click</c>.</summary>
    public void OpenSettings()
        => AvaloniaUi.RunInHostContext(() => commands.StartSettingsDialog(owner: null, CommandsDialogs.SettingsDialog.Pages.DiffViewerSettingsPage.GetPageReference()));

    public async Task<FileViewContent> GetChangesAsync(FileStatusEntry entry, string? encodingName, CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;
        FileViewContent content = GetChanges(entry, GetEncoding(encodingName), cancellationToken);
        content = content with { SupportsLinePatching = SupportsLinePatching(entry, content) };
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return content;
    }

    /// <summary>
    ///  As <c>SupportLinePatching</c> of <c>ResetView</c>: the diffs of existing files, and the added files of the working
    ///  directory or the index (or that do not exist any more); not in a bare repository.
    /// </summary>
    private bool SupportsLinePatching(FileStatusEntry entry, FileViewContent content)
    {
        GitItemStatus item = entry.Item;
        string? fullPath = new FullPathResolver(() => Module.WorkingDir).Resolve(item.Name);
        bool isNormalDiff = content.Kind == FileViewKind.Diff && entry.FirstRevision?.ObjectId != ObjectId.CombinedDiffId;
        bool isDiff = isNormalDiff
            && content.Text.Contains("@@")
            && AppSettings.DiffDisplayAppearance.Value != GitCommands.Settings.DiffDisplayAppearance.GitWordDiff
            && File.Exists(fullPath);
        bool isAdded = item.IsAdded && content.Kind == FileViewKind.Text
            && (item.Staged is StagedStatus.WorkTree or StagedStatus.Index || !File.Exists(fullPath));
        return (isDiff || isAdded) && !Module.IsBareRepository();
    }

    public async Task<FileViewContent> GetFileAsync(GitItemStatus file, ObjectId objectId, string? encodingName, CancellationToken cancellationToken)
    {
        await TaskScheduler.Default;
        FileViewContent content = GetGitItem(file, objectId, GetEncoding(encodingName), cancellationToken);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return content;
    }

    /// <summary>As <c>ViewChangesAsync</c>.</summary>
    private FileViewContent GetChanges(FileStatusEntry entry, Encoding encoding, CancellationToken cancellationToken)
    {
        GitItemStatus item = entry.Item;
        if (item.IsStatusOnly)
        {
            // Present error (e.g. parsing Git)
            return new FileViewContent(FileViewKind.Text, item.ErrorMessage ?? "", item.Name);
        }

        ObjectId firstId = entry.FirstRevision?.ObjectId ?? entry.SecondRevision.FirstParentId;
        ObjectId secondId = entry.SecondRevision.ObjectId;

        if (!item.IsSubmodule && (item.IsNew || firstId.IsZero || (!item.IsDeleted && FileHelper.IsImage(item.Name))))
        {
            // View blob guid from revision, or file for worktree
            return GetGitItem(item, secondId, encoding, cancellationToken);
        }

        if (item.IsRangeDiff || !string.IsNullOrWhiteSpace(item.GrepString))
        {
            return new FileViewContent(FileViewKind.Text, "(Range diffs and git grep results are not shown by the Avalonia viewer yet.)");
        }

        if (firstId == ObjectId.CombinedDiffId)
        {
            bool success = Module.GetCombinedDiffContent(secondId, item.Name, GetExtraDiffArguments(isCombinedDiff: true), encoding, out string diffOfConflict,
                useGitColoring: UseGitColoring,
                commandConfiguration: CombinedDiffHighlightService.GetGitCommandConfiguration(Module, UseGitColoring),
                cancellationToken);
            if (!success)
            {
                return new FileViewContent(FileViewKind.Text, diffOfConflict, item.Name);
            }

            return string.IsNullOrWhiteSpace(diffOfConflict)
                ? new FileViewContent(FileViewKind.Text, TranslatedStrings.UninterestingDiffOmitted, item.Name)
                : Diff(diffOfConflict);
        }

        if (item.IsSubmodule)
        {
            GitSubmoduleStatus? status = ThreadHelper.JoinableTaskFactory.Run(async () => item.GetSubmoduleStatusAsync() is Task<GitSubmoduleStatus?> statusTask

                // Patch already evaluated, normal case for e.g. FileStatusList
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                ? await statusTask
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
                : await SubmoduleHelpers.GetSubmoduleDiffChangesAsync(Module, item.Name, item.OldName, firstId, secondId, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            string text = status is null
                ? $"Failed to get status for submodule \"{item.Name}\""
                : SubmoduleResources.GetSubmoduleStatusText(Module, status);
            return new FileViewContent(FileViewKind.Text, text, item.Name);
        }

        // Diff of a text file (difftastic is not ported yet, the patch is shown instead).
        string patch = ThreadHelper.JoinableTaskFactory.Run(() => GetSelectedPatchAsync(item, firstId, secondId, encoding, cancellationToken)) ?? "";
        return Diff(patch);

        static FileViewContent Diff(string text) => new(FileViewKind.Diff, text, HasGitColors: AnsiEscapeParser.HasEscapes(text));
    }

    /// <summary>As <c>GetSelectedPatchAsync</c> in <c>ViewChangesAsync</c>.</summary>
    private async Task<string?> GetSelectedPatchAsync(GitItemStatus file, ObjectId firstId, ObjectId selectedId, Encoding encoding, CancellationToken cancellationToken)
    {
        bool isSkipWorktree = file.IsSkipWorktree;
        if (isSkipWorktree)
        {
            Module.SkipWorktreeFiles([file], skipWorktree: false, out _);
        }

        Patch? patch;
        string? errorMessage;
        try
        {
            // Files with tree guid should be presented with normal diff
            bool isTracked = file.IsTracked || (!file.TreeId.IsZero && !selectedId.IsZero);
            (patch, errorMessage) = await Module.GetSingleDiffAsync(firstId, selectedId, file.Name, file.OldName, GetExtraDiffArguments(), encoding, cacheResult: true, isTracked,
                UseGitColoring,
                PatchHighlightService.GetGitCommandConfiguration(Module, UseGitColoring),
                cancellationToken);
        }
        finally
        {
            if (isSkipWorktree)
            {
                try
                {
                    file.IsSkipWorktree = false;
                    Module.SkipWorktreeFiles([file], skipWorktree: true, out _);
                }
                finally
                {
                    file.IsSkipWorktree = true;
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return patch?.Text ?? errorMessage;
    }

    /// <summary>As <c>FileViewer.GetExtraDiffArguments</c> with its settings (the git word diff is not ported yet).</summary>
    private static ArgumentString GetExtraDiffArguments(bool isCombinedDiff = false)
    {
        IgnoreWhitespaceKind ignoreWhitespace = AppSettings.IgnoreWhitespaceKind.Value;
        return new ArgumentBuilder
        {
            { ignoreWhitespace == IgnoreWhitespaceKind.AllSpace, "--ignore-all-space" },
            { ignoreWhitespace == IgnoreWhitespaceKind.Change, "--ignore-space-change" },
            { ignoreWhitespace == IgnoreWhitespaceKind.Eol, "--ignore-space-at-eol" },
            { AppSettings.ShowEntireFile.Value, "--inter-hunk-context=9000 --unified=9000", $"--unified={AppSettings.NumberOfContextLines}" },
        };
    }

    /// <summary>As <c>FileViewer.ViewGitItemAsync</c>: the blob of the revision, or the file of the working directory.</summary>
    private FileViewContent GetGitItem(GitItemStatus file, ObjectId objectId, Encoding encoding, CancellationToken cancellationToken)
    {
        ObjectId blobId = GetUpdateTreeId(file, objectId, cancellationToken);
        if (!blobId.IsZero)
        {
            return GetItem(
                file.Name,
                file.IsSubmodule,
                getImage: () => ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    using MemoryStream? stream = await Module.GetFileStreamAsync(blobId.ToString(), cancellationToken);
                    return stream?.ToArray();
                }),
                getFileText: () =>
                {
                    // If the file blob seem to be a diff file, get also escape sequences, that possibly are stored in the diff
                    bool stripAnsiEscapeCodes = !file.Name.EndsWith(".diff", StringComparison.OrdinalIgnoreCase) && !file.Name.EndsWith(".patch", StringComparison.OrdinalIgnoreCase);
                    return Module.GetFileText(blobId, encoding, stripAnsiEscapeCodes) ?? "";
                },
                getSubmoduleText: () => SubmoduleResources.GetSubmoduleText(Module, file.Name.TrimEnd('/'), blobId.ToString())) with { FilePreamble = [] };
        }

        // As FileViewer.ViewFileAsync.
        string? fullPath = new FullPathResolver(() => Module.WorkingDir).Resolve(file.Name);
        if (fullPath is null)
        {
            return FileViewContent.Empty;
        }

        bool isSubmodule = file.IsSubmodule;
        if (!isSubmodule && file.TreeId.IsZero && (file.Name.EndsWith('/') || Directory.Exists(fullPath)))
        {
            if (!GitModule.IsValidGitWorkingDir(fullPath))
            {
                return new FileViewContent(FileViewKind.Text, "Directory: " + file.Name, file.Name);
            }

            isSubmodule = true;
        }

        if (!isSubmodule && !File.Exists(fullPath))
        {
            return new FileViewContent(FileViewKind.Text, $"File {fullPath} does not exist", file.Name);
        }

        byte[]? preamble = null;
        FileViewContent content = GetItem(
            file.Name,
            isSubmodule,
            getImage: () => File.ReadAllBytes(fullPath),
            getFileText: () =>
            {
                using FileStream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = FileReader.OpenStream(stream, encoding);
                string text = reader.ReadToEnd();
                preamble = reader.CurrentEncoding.GetPreamble();
                return text;
            },
            getSubmoduleText: () => SubmoduleResources.GetSubmoduleText(Module, file.Name.TrimEnd('/'), ""));
        return content with { FilePreamble = preamble };
    }

    /// <summary>As <c>FileViewer.ViewItemAsync</c> and the binary check of <c>ViewTextAsync</c> (without the hex dump).</summary>
    private FileViewContent GetItem(string fileName, bool isSubmodule, Func<byte[]?> getImage, Func<string> getFileText, Func<string> getSubmoduleText)
    {
        if (isSubmodule)
        {
            return new FileViewContent(FileViewKind.Text, getSubmoduleText(), fileName);
        }

        if (FileHelper.IsImage(fileName))
        {
            byte[]? image = null;
            try
            {
                image = getImage();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            }

            return image is null
                ? new FileViewContent(FileViewKind.Text, string.Format(_strings.CannotViewImage.Text, fileName), fileName)
                : new FileViewContent(FileViewKind.Image, "", fileName, Image: image);
        }

        string text = getFileText();
        if (FileHelper.IsBinaryFileName(Module, fileName) || FileHelper.IsBinaryFileAccordingToContent(text))
        {
            return new FileViewContent(FileViewKind.Text, string.Format(_strings.BinaryFileDetected.Text, fileName));
        }

        return new FileViewContent(FileViewKind.Text, text, fileName);
    }

    /// <summary>As <c>FileViewer.GetUpdateTreeId</c>.</summary>
    private ObjectId GetUpdateTreeId(GitItemStatus file, ObjectId commitId, CancellationToken cancellationToken)
    {
        if (!file.TreeId.IsZero && !commitId.IsArtificial)
        {
            // current value is immutable (and IsSubmodule should have been set)
            return file.TreeId;
        }

        if (commitId == ObjectId.WorkTreeId && (!file.TreeId.IsZero || file.IsSubmodule))
        {
            // treeId already calculated, no point in doing it again.
            return default;
        }

        cancellationToken.ThrowIfCancellationRequested();
        IObjectGitItem[] items = [.. Module.GetTree(commitId, full: true, file.Name, cancellationToken)];
        if (items.Length == 1)
        {
            IObjectGitItem gitItem = items[0];
            file.IsSubmodule = gitItem.ObjectType == GitObjectType.Commit;
            file.TreeId = gitItem.ObjectId;
            return commitId == ObjectId.WorkTreeId ? default : file.TreeId;
        }

        return default;
    }
}
