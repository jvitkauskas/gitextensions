using System.Collections.Concurrent;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.Blame;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the file history (docs/avalonia-port/PLAN.md, phase 5).</summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormFileHistory</c> (the <c>filehistory</c> and <c>blamehistory</c> verbs), modeless
    ///  if other windows are open, as <c>ShowModelessForm</c>.
    /// </summary>
    /// <param name="fileName">The file, quoted or not.</param>
    public static bool TryShowFileHistory(IGitUICommands commands, string fileName, GitRevision? revision, bool filterByRevision, bool showBlame)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        FileHistoryWindow window = new() { PositionName = "FormFileHistory", PositionStore = WindowPositionStore.Instance };
        FileHistoryHost host = new(commands, window, fileName.Trim('"').ToPosixPath());
        FilterInfo filter = new() { ByPathFilter = true, PathFilter = host.FileName.Quote() };
        if (filterByRevision && revision is not null)
        {
            // As ToolStripFilters.SetRevisionFilter: the hash is a filter of the commit messages (the default of the filter box).
            filter.Apply(new RevisionFilter(revision.Guid, byCommit: true, byCommitter: false, byAuthor: false, byDiffContent: false));
        }

        RevisionGridHost gridHost = new(
            commands,
            currentCheckout => filter.GetRevisionFilter(new Lazy<ObjectId>(() => currentCheckout)),
            showArtificial: true,
            getPathFilter: host.BuildPathFilter);
        RevisionGridViewModel grid = new(gridHost, new RevisionGridDisplayOptions(AppSettings.RelativeDate, AppSettings.ShowAuthorDate, TranslatedStrings.SearchingFor, AppSettings.RevisionGridQuickSearchTimeout))
        {
            MultiSelect = true,
        };

        // As the BuildServerWatcher of its RevisionGridControl: the build statuses of the revisions, for the build report tab.
        ApplyColumns(grid);
        GridBuildServerWatcher buildServerWatcher = new(commands, grid, () => new NativeWindowOwner(window));
        window.Closed += (_, _) =>
        {
            buildServerWatcher.Dispose();
            grid.Dispose();
        };
        BrowseGridFilter gridFilter = new(commands, () => new NativeWindowOwner(window), filter) { Grid = grid };
        RevisionGridMenuBuilder gridMenu = new((GitUICommands)commands, () => new NativeWindowOwner(window), grid, () => grid.Load(grid.SelectedRow?.ObjectId))
        {
            Filter = gridFilter,
        };
        FileViewerHost fileViewerHost = new(commands);
        CommitDiffViewModel commitDiff = new(
            ViewStrings.Load<CommitDiffStrings>(),
            new CommitDiffHost(commands),
            fileViewerHost,
            new CommitInfoHost(commands),
            ViewStrings.Load<FileStatusListStrings>(),
            GetFileStatusTreeOptions(),
            revision?.ObjectId ?? default);
        UseFileStatusListMenu(commitDiff.Files, commands, window);
        BlameViewModel blame = new(ViewStrings.Load<BlameStrings>(), new BlameHost(commands, window), new CommitInfoHost(commands));
        FileHistoryViewModel viewModel = new(
            ViewStrings.Load<FileHistoryStrings>(),
            host,
            grid,
            commitDiff,
            fileViewerHost,
            blame,
            host.FileName,
            revision?.ObjectId,
            showBlame)
        {
            // As ToolStripFilters.Bind and the menus of FormBrowseMenus in the toolbar: the filters, Navigate and View of the grid.
            Filters = new FilterToolBarViewModel(ViewStrings.Load<FilterToolBarStrings>(), gridFilter),
            BuildReport = new BuildReportViewModel(new BuildReportHost(commands)),
            NavigateMenuProvider = gridMenu.CreateNavigateItems,
            ViewMenuProvider = gridMenu.CreateViewItems,
        };
        window.DataContext = viewModel;

        // As FormFileHistory.LoadCustomDifftools: the difftools of the submenus of the grid.
        ThreadHelper.FileAndForget(async () =>
        {
            IReadOnlyList<string> tools = await LoadCustomDiffToolsAsync(commands.Module, CustomDiffToolsDelay, CancellationToken.None);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            viewModel.CustomDiffTools = tools;
        });
        if (AvaloniaDialogHost.HasOpenWindows)
        {
            AvaloniaDialogHost.Show(window, ownerHandle: 0);
        }
        else
        {
            AvaloniaDialogHost.ShowDialog(window, ownerHandle: 0);
        }

        return true;
    }

    /// <summary>The file history settings (<c>AppSettings</c>).</summary>
    private sealed class FileHistorySettings : IFileHistorySettings
    {
        public bool FollowRenames { get => AppSettings.FollowRenamesInFileHistory; set => AppSettings.FollowRenamesInFileHistory = value; }

        public bool FollowRenamesExactOnly { get => AppSettings.FollowRenamesInFileHistoryExactOnly; set => AppSettings.FollowRenamesInFileHistoryExactOnly = value; }

        public bool FullHistory { get => AppSettings.FullHistoryInFileHistory; set => AppSettings.FullHistoryInFileHistory = value; }

        public bool SimplifyMerges { get => AppSettings.SimplifyMergesInFileHistory; set => AppSettings.SimplifyMergesInFileHistory = value; }

        public bool LoadHistoryOnShow { get => AppSettings.LoadFileHistoryOnShow; set => AppSettings.LoadFileHistoryOnShow = value; }

        public bool LoadBlameOnShow { get => AppSettings.LoadBlameOnShow; set => AppSettings.LoadBlameOnShow = value; }

        public bool IgnoreWhitespaceOnBlame { get => AppSettings.IgnoreWhitespaceOnBlame; set => AppSettings.IgnoreWhitespaceOnBlame = value; }

        public bool DetectCopyInFileOnBlame { get => AppSettings.DetectCopyInFileOnBlame; set => AppSettings.DetectCopyInFileOnBlame = value; }

        public bool DetectCopyInAllOnBlame { get => AppSettings.DetectCopyInAllOnBlame; set => AppSettings.DetectCopyInAllOnBlame = value; }

        public bool BlameDisplayAuthorFirst { get => AppSettings.BlameDisplayAuthorFirst; set => AppSettings.BlameDisplayAuthorFirst = value; }

        public bool BlameShowAuthorAvatar { get => AppSettings.BlameShowAuthorAvatar; set => AppSettings.BlameShowAuthorAvatar = value; }

        public bool BlameShowAuthor { get => AppSettings.BlameShowAuthor; set => AppSettings.BlameShowAuthor = value; }

        public bool BlameShowAuthorDate { get => AppSettings.BlameShowAuthorDate; set => AppSettings.BlameShowAuthorDate = value; }

        public bool BlameShowAuthorTime { get => AppSettings.BlameShowAuthorTime; set => AppSettings.BlameShowAuthorTime = value; }

        public bool BlameShowLineNumbers { get => AppSettings.BlameShowLineNumbers; set => AppSettings.BlameShowLineNumbers = value; }

        public bool BlameShowOriginalFilePath { get => AppSettings.BlameShowOriginalFilePath; set => AppSettings.BlameShowOriginalFilePath = value; }
    }

    /// <summary>The git operations and dialogs of <c>FormFileHistory</c>, with the path filter of its grid.</summary>
    internal sealed class FileHistoryHost(IGitUICommands commands, DialogWindow window, string fileName) : IFileHistoryHost
    {
        // As RevisionGridControl._objectIdPrefix: marks the hashes in the output of git log --name-only.
        private const string ObjectIdPrefix = "????";

        private readonly FullPathResolver _fullPathResolver = new(() => commands.Module.WorkingDir);

        // As RevisionGridControl.FilePathByObjectId: the name of the file in the revisions, filled in the background.
        private readonly ConcurrentDictionary<ObjectId, string> _filePathByObjectId = new();

        private IGitModule Module => commands.Module;

        private NativeWindowOwner Owner => new(window);

        public string FileName { get; } = fileName;

        public IFileHistorySettings Settings { get; } = new FileHistorySettings();

        public string WorkingDirectory => PathUtil.GetDisplayPath(Module.WorkingDir);

        public bool IsSubmodule => GitModule.IsValidGitWorkingDir(_fullPathResolver.Resolve(FileName));

        public string NoChangesText => TranslatedStrings.NoChanges;

        /// <summary>As <c>GetFileNameForRevision</c> and <c>RevisionGridControl.GetRevisionFileName</c>.</summary>
        public string? GetFileName(GitRevision revision)
        {
            ObjectId objectId = revision.IsArtificial ? Module.GetCurrentCheckout() : revision.ObjectId;
            if (objectId.IsZero)
            {
                return null;
            }

            if (_filePathByObjectId.TryGetValue(objectId, out string? name))
            {
                return name;
            }

            GitArgumentBuilder args = new("log")
            {
                // --name-only will list each filename on a separate line, ending with an empty line
                $"--format=\"{ObjectIdPrefix}%H\"",
                "--name-only",
                "--follow",
                "--diff-merges=separate",
                FindRenamesAndCopiesOpts(),
                objectId.ToString(),
                "--max-count=1",
                "--",
                FileName.QuoteIfNotQuotedAndNE(),
            };

            return ParseFileNames(args, cancellationToken: default).FirstOrDefault();
        }

        public bool IsFileAvailable(string fileName, GitRevision revision)
            => revision.IsArtificial ? File.Exists(_fullPathResolver.Resolve(fileName)) : !Module.GetFileBlobHash(fileName, revision.ObjectId).IsZero;

        public bool IsInWorkingDirectory(string fileName) => File.Exists(_fullPathResolver.Resolve(fileName));

        /// <summary>As <c>RevisionGridControl.GetActualRevision</c>: the path filter rewrites the parents.</summary>
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

        /// <summary>As <c>Blame_CommandClick</c>.</summary>
        public ObjectId? ResolveCommit(string commitOrRef, bool isRef)
        {
            if (!isRef)
            {
                return Module.TryResolvePartialCommitId(commitOrRef, out ObjectId commitId) ? commitId : null;
            }

            return new CommitDataManager(() => Module).GetCommitData(commitOrRef)?.ObjectId;
        }

        public void OpenWithDifftool(IReadOnlyList<GitRevision> revisions, string fileName, string? revisionFileName, bool toLocal, string? customTool = null)
            => AvaloniaUi.RunInHostContext(() => commands.OpenWithDifftool(Owner, revisions, fileName, revisionFileName, toLocal ? RevisionDiffKind.DiffBLocal : RevisionDiffKind.DiffAB, isTracked: true, customTool: customTool));

        /// <summary>As <c>saveAsToolStripMenuItem_Click</c>.</summary>
        public void SaveAs(GitRevision revision, string fileName) => AvaloniaUi.RunInHostContext(() =>
        {
            string? fullName = _fullPathResolver.Resolve(fileName);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return;
            }

            fullName = fullName.ToNativePath();
            using SaveFileDialog fileDialog = new()
            {
                InitialDirectory = Path.GetDirectoryName(fullName),
                FileName = Path.GetFileName(fullName),
                DefaultExt = Path.GetExtension(fullName),
                AddExtension = true
            };
            fileDialog.Filter =
                "Current format (*." +
                fileDialog.DefaultExt + ")|*." +
                fileDialog.DefaultExt +
                "|All files (*.*)|*.*";
            if (fileDialog.ShowDialog(Owner) == DialogResult.OK)
            {
                Module.SaveBlobAs(fileDialog.FileName, $"{revision.Guid}:\"{fileName}\"");
            }
        });

        public void CherryPick(GitRevision revision) => AvaloniaUi.RunInHostContext(() => commands.StartCherryPickDialog(Owner, revision));

        public void Revert(GitRevision revision) => AvaloniaUi.RunInHostContext(() => commands.StartRevertCommitDialog(Owner, revision));

        /// <summary>As <c>RevisionGridControl.ViewSelectedRevisions</c>.</summary>
        public void ViewRevisions(IReadOnlyList<GitRevision> revisions) => AvaloniaUi.RunInHostContext(() =>
        {
            if (revisions.Count > 0 && !revisions[0].IsArtificial)
            {
                TryShowCommitDiff(Owner, commands, revisions[0].ObjectId, modeless: true);
            }
        });

        public void ShowGitCommandLog() => AvaloniaUi.RunInHostContext(() => TryShowGitCommandLog(Owner));

        public void ShowRevisionFiltered(ObjectId objectId) => AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(Owner, objectId));

        public IReadOnlyList<RevisionCopyItem> GetCopyItems(IReadOnlyList<GitRevision> revisions) => GetRevisionCopyItems(revisions);

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);

        /// <summary>
        ///  As <c>RevisionGridControl.BuildPathFilter</c> (in the background): with renames followed, the names of the file in
        ///  its history, which <see cref="GetFileName"/> then knows.
        /// </summary>
        public string BuildPathFilter(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _filePathByObjectId.Clear();

            // Manual arguments must be quoted if needed (internal paths are quoted)
            // except for simple arguments without any quotes or spaces
            string path = FileName.Quote().Trim();
            bool multpleArgs = false;
            if (!path.Any(c => c == '"') && !path.Any(c => c == '\''))
            {
                if (!path.Any(c => c == ' '))
                {
                    path = path.Quote();
                }
                else
                {
                    multpleArgs = true;
                }
            }
            else if (path.Count(c => c == '"') + path.Count(c => c == '\'') > 2)
            {
                // Basic detection of multiple quoted strings (let the Git command fail for more advanced usage)
                multpleArgs = true;
            }

            if (!AppSettings.FollowRenamesInFileHistory

                // The command line can be very long for folders, just ignore.
                || path.EndsWith('/')
                || path.EndsWith("/\"")

                // --follow only accepts exactly one argument, error for all other
                || multpleArgs)
            {
                return path;
            }

            // git log --follow is not working as expected (see  https://stackoverflow.com/questions/46487476/git-log-follow-graph-skips-commits)
            //
            // But we can take a more complicated path to get reasonable results:
            //  1. use git log --follow to get all previous filenames of the file we are interested in
            //  2. use git log "list of files names" to get the history graph
            GitArgumentBuilder args = new("log")
            {
                // --name-only will list each filename on a separate line, ending with an empty line
                $"--format=\"{ObjectIdPrefix}%H\"",
                "--name-only",
                "--follow",
                FindRenamesAndCopiesOpts(),
                "--",
                path.QuoteIfNotQuotedAndNE()
            };

            HashSet<string?> setOfFileNames = [.. ParseFileNames(args, cancellationToken)];

            // Add path in case of no matches so result is never empty
            // This also occurs if Git detects more than one path argument
            string pathFilter = setOfFileNames.Count == 0
                ? path
                : string.Join("", setOfFileNames.Select(s => @$" ""{s}"""));

            // Windows commands have a max length of 32267 characters,
            // git-log command is normally around 200 characters.
            if (pathFilter.Length > 31000)
            {
                ThreadHelper.FileAndForget(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    AvaloniaUi.RunInHostContext(() => MessageBoxes.ShowError(
                        Owner,
                        $"Ignoring too long pathfilter ({pathFilter.Length}). (Are you trying to filter a folder?)",
                        "Cannot follow file renames"));
                });
                return path;
            }

            return pathFilter;
        }

        /// <summary>As <c>RevisionGridControl.ParseFileNames</c>.</summary>
        private IEnumerable<string> ParseFileNames(GitArgumentBuilder args, CancellationToken cancellationToken)
        {
            ExecutionResult result = Module.GitExecutable.Execute(args, outputEncoding: GitModule.LosslessEncoding, throwOnErrorExit: false, cancellationToken: cancellationToken);
            if (!result.ExitedSuccessfully)
            {
                yield break;
            }

            ObjectId currentObjectId = default;
            foreach (string? line in result.StandardOutput.LazySplit('\n').Select(GitModule.ReEncodeFileNameFromLossless))
            {
                if (string.IsNullOrEmpty(line))
                {
                    // empty line after sha
                    continue;
                }

                if (line.StartsWith(ObjectIdPrefix))
                {
                    currentObjectId = line.Length >= ObjectId.Sha1CharCount + ObjectIdPrefix.Length
                        && ObjectId.TryParse(line, offset: ObjectIdPrefix.Length, out ObjectId parsedId)
                            ? parsedId
                            : default;
                    continue;
                }

                if (currentObjectId.IsZero)
                {
                    // Parsing has failed, ignore
                    continue;
                }

                // Add only the first file to the dictionary
                cancellationToken.ThrowIfCancellationRequested();
                _filePathByObjectId.TryAdd(currentObjectId, line);

                yield return line;
            }
        }

        // As RevisionGridControl.FindRenamesAndCopiesOpts.
        private static ArgumentString FindRenamesAndCopiesOpts()
            => AppSettings.FollowRenamesInFileHistoryExactOnly
                ? " --find-renames=\"100%\" --find-copies=\"100%\""
                : " --find-renames --find-copies";
    }

    /// <summary>The copy menu of revisions, as <c>CopyContextMenuItem.OnDropDownOpening</c>.</summary>
    internal static IReadOnlyList<RevisionCopyItem> GetRevisionCopyItems(IReadOnlyList<GitRevision> revisions)
    {
        if (revisions.Count == 0)
        {
            return [];
        }

        List<RevisionCopyItem> items = [];
        List<string> branchNames = [];
        List<string> tagNames = [];
        foreach (GitRevision revision in revisions)
        {
            GitRefListsForRevision refLists = new(revision);
            branchNames.AddRange(refLists.GetAllBranchNames());
            tagNames.AddRange(refLists.GetAllTagNames());
        }

        uint itemNumber = 0;
        AddRefs(TranslatedStrings.Branches, branchNames);
        AddRefs(TranslatedStrings.Tags, tagNames);

        int count = revisions.Count;
        AddItem(ResourceManager.TranslatedStrings.GetCommitHash(count), r => r.Guid, 'C');
        AddItem(ResourceManager.TranslatedStrings.GetMessage(count), r => r.Body ?? r.Subject, 'M');
        AddItem(ResourceManager.TranslatedStrings.GetAuthor(count), r => $"{r.Author} <{r.AuthorEmail}>", 'A');
        if (count == 1 && revisions[0].AuthorDate == revisions[0].CommitDate)
        {
            AddItem(ResourceManager.TranslatedStrings.Date, r => r.AuthorDate.ToString(), 'D');
        }
        else
        {
            AddItem(ResourceManager.TranslatedStrings.GetAuthorDate(count), r => r.AuthorDate.ToString(), 'T');
            AddItem(ResourceManager.TranslatedStrings.GetCommitDate(count), r => r.CommitDate.ToString(), 'D');
        }

        return items;

        void AddRefs(string caption, List<string> names)
        {
            if (names.Count == 0)
            {
                return;
            }

            items.Add(new RevisionCopyItem(RevisionCopyItemKind.Caption, caption));
            foreach (string name in names)
            {
                Add(name, name, hotkey: null);
            }

            items.Add(new RevisionCopyItem(RevisionCopyItemKind.Separator));
        }

        void AddItem(string displayText, Func<GitRevision, string> extractRevisionText, char hotkey)
        {
            string[] textToCopy = [.. revisions.Select(extractRevisionText).Distinct()];
            displayText += ":   " + textToCopy.Select(t => t.SubstringUntil('\n')).Join(", ").ShortenTo(40);
            Add(displayText, textToCopy.Join("\n"), hotkey);
        }

        void Add(string displayText, string textToCopy, char? hotkey)
        {
            // Mnemonics are escaped in the names; the hotkey or a number marks the access key.
            displayText = displayText.Replace("&", "&&");
            if (hotkey.HasValue)
            {
                int position = displayText.IndexOf(hotkey.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
                if (position >= 0)
                {
                    displayText = displayText.Insert(position, "&");
                }
            }
            else
            {
                displayText = ++itemNumber > 10 ? displayText : "&" + (itemNumber % 10) + ":   " + displayText;
            }

            items.Add(new RevisionCopyItem(RevisionCopyItemKind.Item, displayText.TrimEnd(Delimiters.LineFeedAndCarriageReturn), textToCopy));
        }
    }
}
