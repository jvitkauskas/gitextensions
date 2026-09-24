using System.Buffers;
using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.Git;
using GitCommands.Submodules;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.LeftPanel;

/// <summary>
///  A tree of the left panel, the root node of its nodes (port of <c>Tree</c>): loads its nodes in the background and shows
///  them keeping the expanded nodes, the selection and the multi-selection.
/// </summary>
public abstract class LeftPanelTree : LeftPanelNode
{
    private CancellationTokenSource? _reloading;

    private protected LeftPanelTree(LeftPanelViewModel owner, LeftPanelTreeKind kind, string text, string icon)
        : base(tree: null)
    {
        Owner = owner;
        Kind = kind;
        Text = text;
        IconKey = icon;
    }

    public LeftPanelTreeKind Kind { get; }

    internal LeftPanelViewModel Owner { get; }

    /// <summary>Whether the tree is shown (<c>IsAttached</c>).</summary>
    public bool IsAttached { get; internal set; }

    /// <summary>
    ///  Whether the next load is the first one (since the module changed): the expanded state of the nodes is then not kept,
    ///  and each tree expands as it does by default.
    /// </summary>
    internal bool IsFirstLoad { get; set; } = true;

    protected internal override string Key => Kind.ToString();

    private protected ILeftPanelHost Host => Owner.Host;

    protected internal override void ApplyStyle()
    {
        // The text and the icon of a tree are fixed.
    }

    /// <summary>
    ///  As <c>ReloadNodesDetached</c>: loads the nodes in the background (not in the dashboard) and shows them.
    /// </summary>
    internal virtual async Task ReloadAsync()
    {
        if (!IsAttached)
        {
            return;
        }

        CancellationToken cancellationToken = RestartLoading();
        IReadOnlyList<LeftPanelNode> nodes;
        try
        {
            nodes = await Host.RunInBackgroundAsync(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Host.IsValidWorkingDir ? LoadNodes(cancellationToken) : [];
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // E.g. git failed; the tree keeps its nodes, as a failed load of the WinForms tree.
            System.Diagnostics.Trace.WriteLine($"Failed to load the {Kind} of the left panel: {ex}");
            return;
        }

        if (!cancellationToken.IsCancellationRequested && IsAttached)
        {
            Owner.Fill(this, nodes);
        }
    }

    internal void CancelLoading()
    {
#pragma warning disable VSTHRD103 // CancelAsync may resume off the UI thread.
        _reloading?.Cancel();
#pragma warning restore VSTHRD103
    }

    private protected CancellationToken RestartLoading()
    {
        CancelLoading();
        _reloading = new CancellationTokenSource();
        return _reloading.Token;
    }

    /// <summary>Creates the nodes; called in the background.</summary>
    private protected abstract IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken);

    /// <summary>
    ///  As <c>PostFillTreeViewNode</c>: after the nodes are shown, e.g. the default expanded state of the first load.
    /// </summary>
    internal virtual void PostFill(bool firstTime)
    {
    }
}

/// <summary>A tree of references (port of <c>BaseRefTree</c>).</summary>
public abstract class LeftPanelRefTree : LeftPanelTree
{
    private protected LeftPanelRefTree(LeftPanelViewModel owner, LeftPanelTreeKind kind, string text, string icon)
        : base(owner, kind, text, icon)
    {
    }

    private protected IReadOnlyList<IGitRef> GetRefs(Func<IGitRef, bool> predicate) => [.. Owner.GetAllRefs().Where(predicate)];

    private protected IEnumerable<IGitRef> PrioritizedBranches(IReadOnlyList<IGitRef> branches)
        => OrderByPriority(branches, branch => branch.LocalName, Owner.Settings.PrioritizedBranchNames);

    private protected IEnumerable<RemoteRepoNode> PrioritizedRemotes(IReadOnlyList<RemoteRepoNode> remotes)
        => OrderByPriority([.. remotes.OrderBy(node => node.FullPath)], node => node.FullPath, Owner.Settings.PrioritizedRemoteNames);

    /// <summary>As <c>BaseRefTree.OrderByPriority</c>: the references matching the regexes of the setting first, in the order of the regexes.</summary>
    internal static IEnumerable<T> OrderByPriority<T>(IReadOnlyList<T> references, Func<T, string> keySelector, string setting)
    {
        string[] regexes = [.. setting.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(regex => $"^({regex})$")];
        if (regexes.Length == 0)
        {
            return references;
        }

        // Many regexes would push others out of the static regex cache (see BaseRefTree).
        const int additionalRegexCacheEntries = 10;
        if ((regexes.Length * 2) + additionalRegexCacheEntries > Regex.CacheSize)
        {
            Regex.CacheSize = (regexes.Length * 2) + additionalRegexCacheEntries;
        }

        Dictionary<string, int> orderByNodeKey = [];
        foreach (T node in references)
        {
            string key = keySelector(node);
            int currentOrder = 0;
            foreach (string regex in regexes)
            {
                if (Regex.IsMatch(key, regex, RegexOptions.ExplicitCapture))
                {
                    orderByNodeKey[key] = currentOrder;
                    break;
                }

                currentOrder++;
            }
        }

        return references.OrderBy(node => orderByNodeKey.GetValueOrDefault(keySelector(node), int.MaxValue));
    }
}

/// <summary>The local branches (port of <c>LocalBranchTree</c>): folders for the path of the names, the current one bold.</summary>
public sealed class LocalBranchTree : LeftPanelRefTree
{
    internal LocalBranchTree(LeftPanelViewModel owner)
        : base(owner, LeftPanelTreeKind.Branches, owner.Strings.Branches.Text, LeftPanelIcons.BranchLocalRoot)
    {
    }

    private protected override IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken)
    {
        IReadOnlyList<IGitRef> branches = GetRefs(r => r.IsHead);
        IReadOnlyDictionary<string, AheadBehindData>? aheadBehindData = Host.GetAheadBehindData();
        string currentBranch = Host.GetCurrentBranch();
        Dictionary<string, LeftPanelRevisionNode> pathToNode = [];
        List<LeftPanelNode> nodes = [];
        foreach (IGitRef branch in PrioritizedBranches(branches))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (branch.ObjectId.IsZero)
            {
                continue;
            }

            LocalBranchNode node = new(this, branch.ObjectId, branch.Name, branch.Name == currentBranch, visible: true);
            if (aheadBehindData?.TryGetValue(node.FullPath, out AheadBehindData aheadBehind) is true)
            {
                node.UpdateAheadBehind(aheadBehind.ToDisplay(), aheadBehind.RemoteRef);
            }

            if (node.CreateRootNode(pathToNode, (tree, parentPath) => new BranchPathNode(tree, parentPath)) is { } parent)
            {
                nodes.Add(parent);
            }
        }

        return nodes;
    }

    internal override void PostFill(bool firstTime)
    {
        if (firstTime)
        {
            IsExpanded = true;
        }

        // Without a selection, the current branch is selected.
        if (IsAttached && Owner.SelectedNode is null)
        {
            Owner.SetSelectedNodeSilently(Descendants().OfType<LocalBranchNode>().FirstOrDefault(b => b.IsCurrent));
        }
    }
}

/// <summary>The remotes and their branches (port of <c>RemoteBranchTree</c>), the inactive remotes in a folder.</summary>
public sealed class RemoteBranchTree : LeftPanelRefTree
{
    internal RemoteBranchTree(LeftPanelViewModel owner)
        : base(owner, LeftPanelTreeKind.Remotes, owner.Strings.Remotes.Text, LeftPanelIcons.BranchRemoteRoot)
    {
    }

    private protected override IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken)
    {
        IReadOnlyList<IGitRef> branches = GetRefs(r => r.IsRemote);

        // More than one local branch can track a remote branch: one of them is picked.
        Dictionary<string, AheadBehindData>? aheadBehindData = Host.GetAheadBehindData()?
            .DistinctBy(r => r.Value.RemoteRef)
            .ToDictionary(r => r.Value.RemoteRef, r => r.Value);
        Dictionary<string, Remote> remoteByName = Host.GetRemotes().ToDictionary(r => r.Name);
        Dictionary<string, LeftPanelRevisionNode> pathToNodes = [];
        List<RemoteRepoNode> enabledRemoteRepoNodes = [];

        // The active remotes with branches
        foreach (IGitRef branch in PrioritizedBranches(branches))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (branch.ObjectId.IsZero)
            {
                continue;
            }

            string remoteName = branch.Name.SubstringUntil('/');
            if (!remoteByName.TryGetValue(remoteName, out Remote remote))
            {
                continue;
            }

            RemoteBranchNode node = new(this, branch.ObjectId, branch.Name, visible: true);
            if (aheadBehindData?.TryGetValue(branch.CompleteName, out AheadBehindData aheadBehind) is true)
            {
                node.UpdateAheadBehind(aheadBehind.ToDisplay(reverse: true), $"{GitRefName.RefsHeadsPrefix}{aheadBehind.Branch}");
            }

            if (node.CreateRootNode(
                pathToNodes,
                (tree, parentPath) => parentPath == remote.Name ? new RemoteRepoNode(tree, parentPath, remote, isEnabled: true) : new LeftPanelPathNode(tree, parentPath)) is RemoteRepoNode parent)
            {
                enabledRemoteRepoNodes.Add(parent);
            }
        }

        // The active remotes without branches
        HashSet<string> remotesWithBranches = [.. branches.Select(branch => branch.Name.SubstringUntil('/'))];
        foreach (Remote remote in remoteByName.Values.Where(remote => !remotesWithBranches.Contains(remote.Name)))
        {
            enabledRemoteRepoNodes.Add(new RemoteRepoNode(this, remote.Name, remote, isEnabled: true));
        }

        List<LeftPanelNode> nodes = [.. PrioritizedRemotes(enabledRemoteRepoNodes)];

        // The inactive remotes
        IReadOnlyList<Remote> disabledRemotes = Host.GetDisabledRemotes();
        if (disabledRemotes.Count > 0)
        {
            RemoteRepoFolderNode disabledFolderNode = new(this, Owner.Strings.Inactive.Text);
            foreach (RemoteRepoNode node in PrioritizedRemotes([.. disabledRemotes.Select(remote => new RemoteRepoNode(this, remote.Name, remote, isEnabled: false))]))
            {
                disabledFolderNode.AddChild(node);
            }

            nodes.Add(disabledFolderNode);
        }

        return nodes;
    }

    internal override void PostFill(bool firstTime)
    {
        if (firstTime)
        {
            IsExpanded = true;
        }
    }
}

/// <summary>The tags (port of <c>TagTree</c>), collapsed at first.</summary>
public sealed class TagTree : LeftPanelRefTree
{
    internal TagTree(LeftPanelViewModel owner)
        : base(owner, LeftPanelTreeKind.Tags, owner.Strings.Tags.Text, LeftPanelIcons.TagHorizontal)
    {
    }

    private protected override IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken)
    {
        Dictionary<string, LeftPanelRevisionNode> pathToNodes = [];
        List<LeftPanelNode> nodes = [];
        foreach (IGitRef tag in GetRefs(r => r.IsTag))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TagNode node = new(this, tag.ObjectId, tag.Name, visible: true);
            if (node.CreateRootNode(pathToNodes, (tree, parentPath) => new LeftPanelPathNode(tree, parentPath)) is { } parent)
            {
                nodes.Add(parent);
            }
        }

        return nodes;
    }

    internal override void PostFill(bool firstTime)
    {
        if (firstTime)
        {
            IsExpanded = false;
        }
    }
}

/// <summary>The stashes (port of <c>StashTree</c>), collapsed at first; hidden until they are found in the grid.</summary>
public sealed class StashTree : LeftPanelTree
{
    internal StashTree(LeftPanelViewModel owner)
        : base(owner, LeftPanelTreeKind.Stashes, owner.Strings.Stashes.Text, LeftPanelIcons.Stash)
    {
    }

    private protected override IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken)
    {
        Dictionary<string, LeftPanelRevisionNode> pathToNodes = [];
        List<LeftPanelNode> nodes = [];
        foreach (GitRevision stash in Host.GetStashes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stash.ReflogSelector is null)
            {
                continue;
            }

            // The visibility is set once the grid is loaded.
            StashNode node = new(this, stash.ObjectId, stash.ReflogSelector, stash.Subject, visible: false);
            if (node.CreateRootNode(pathToNodes, (tree, parentPath) => new LeftPanelPathNode(tree, parentPath)) is { } parent)
            {
                nodes.Add(parent);
            }
        }

        return nodes;
    }

    internal override void PostFill(bool firstTime)
    {
        if (firstTime)
        {
            IsExpanded = false;
        }
    }
}

/// <summary>The worktrees (port of <c>WorktreeTree</c>): their paths relative to the parent of the main worktree.</summary>
public sealed class WorktreeTree : LeftPanelTree
{
    private static readonly SearchValues<char> DirectorySeparatorChars = SearchValues.Create([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
    private static readonly SearchValues<char> WordBoundaryChars = SearchValues.Create(['_', '-', '.', ' ']);

    internal WorktreeTree(LeftPanelViewModel owner)
        : base(owner, LeftPanelTreeKind.Worktrees, owner.Strings.Worktrees.Text, LeftPanelIcons.WorkTree)
    {
    }

    /// <summary>The path of the main worktree (listed first by git), else the working directory.</summary>
    public string MainWorktreePath => Children.OfType<WorktreeNode>().FirstOrDefault()?.Worktree.Path ?? Host.WorkingDir;

    private protected override IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken)
    {
        string currentWorkingDir = Host.WorkingDir.TrimEnd(Path.DirectorySeparatorChar);
        IReadOnlyList<GitWorktree> worktrees = Host.GetWorktrees();

        // Relative to the parent of the main worktree, so that linked worktrees in a sibling directory show their folder.
        string mainWorktreePath = worktrees.Count > 0 ? worktrees[0].Path.TrimEnd(Path.DirectorySeparatorChar) : currentWorkingDir;
        string parentDir = Path.GetDirectoryName(mainWorktreePath) ?? mainWorktreePath;

        List<(GitWorktree Worktree, bool IsCurrent, string RelativePath)> worktreeInfos = [];
        foreach (GitWorktree worktree in worktrees)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isCurrent = string.Equals(worktree.Path.TrimEnd(Path.DirectorySeparatorChar), currentWorkingDir, StringComparison.OrdinalIgnoreCase);
            string relativePath;
            try
            {
                relativePath = Path.GetRelativePath(parentDir, worktree.Path);
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
            {
                relativePath = worktree.Path;
            }

            worktreeInfos.Add((worktree, isCurrent, relativePath));
        }

        // The common prefix of the linked worktrees (the main one excluded) is removed.
        string commonPrefix = GetCommonPrefix(worktreeInfos.Skip(1).Select(w => w.RelativePath));
        List<LeftPanelNode> nodes = [];
        for (int i = 0; i < worktreeInfos.Count; i++)
        {
            (GitWorktree worktree, bool isCurrent, string relativePath) = worktreeInfos[i];
            string displayPath = i > 0 && commonPrefix.Length > 0 && relativePath.StartsWith(commonPrefix, StringComparison.OrdinalIgnoreCase)
                ? relativePath[commonPrefix.Length..]
                : relativePath;
            nodes.Add(new WorktreeNode(this, worktree, isCurrent, displayPath));
        }

        return nodes;
    }

    /// <summary>
    ///  As <c>WorktreeTree.GetCommonPrefix</c>: the longest common prefix of the paths, at a directory separator, or (if
    ///  none of the paths has one) at <c>_</c>, <c>-</c>, <c>.</c> or a space; empty if there are less than two paths.
    /// </summary>
    internal static string GetCommonPrefix(IEnumerable<string> paths)
    {
        ReadOnlySpan<char> prefix = default;
        bool hasDirectorySeparator = false;
        int count = 0;
        foreach (string path in paths)
        {
            ReadOnlySpan<char> span = path.AsSpan();
            count++;
            if (count == 1)
            {
                prefix = span;
                hasDirectorySeparator = span.ContainsAny(DirectorySeparatorChars);
                continue;
            }

            hasDirectorySeparator = hasDirectorySeparator || span.ContainsAny(DirectorySeparatorChars);
            int limit = Math.Min(prefix.Length, span.Length);
            int matchLength = 0;
            for (int i = 0; i < limit; i++)
            {
                if (char.ToUpperInvariant(prefix[i]) != char.ToUpperInvariant(span[i]))
                {
                    break;
                }

                matchLength = i + 1;
            }

            prefix = prefix[..matchLength];
        }

        if (count < 2 || prefix.Length == 0)
        {
            return "";
        }

        int dirSepIndex = prefix.LastIndexOfAny(DirectorySeparatorChars);
        if (dirSepIndex >= 0)
        {
            return prefix[..(dirSepIndex + 1)].ToString();
        }

        if (hasDirectorySeparator)
        {
            return "";
        }

        int boundaryIndex = prefix.LastIndexOfAny(WordBoundaryChars);
        return boundaryIndex < 0 ? "" : prefix[..(boundaryIndex + 1)].ToString();
    }

    internal override void PostFill(bool firstTime)
    {
        if (firstTime && Children.Count > 1)
        {
            IsExpanded = true;
        }
    }
}

/// <summary>
///  The top project and its submodules (port of <c>SubmoduleTree</c>), from the submodule status (<see cref="ILeftPanelHost.SubmodulesUpdated"/>):
///  folders for the paths, the chains of single folders merged.
/// </summary>
public sealed class SubmoduleTree : LeftPanelTree
{
    private LeftPanelSubmodules? _submodules;

    internal SubmoduleTree(LeftPanelViewModel owner)
        : base(owner, LeftPanelTreeKind.Submodules, owner.Strings.Submodules.Text, LeftPanelIcons.FolderSubmodule)
    {
    }

    /// <summary>The submodules are requested; the tree is filled when they come.</summary>
    internal override Task ReloadAsync()
    {
        if (IsAttached)
        {
            Host.UpdateSubmodules();
        }

        return Task.CompletedTask;
    }

    /// <summary>As <c>OnStatusUpdated</c>: the nodes are created again if the structure changed, else their status is updated.</summary>
    internal async Task OnSubmodulesUpdatedAsync(LeftPanelSubmodules submodules)
    {
        _submodules = submodules;
        if (!IsAttached)
        {
            return;
        }

        if (submodules.StructureUpdated || !UpdateStatus(submodules))
        {
            await base.ReloadAsync();
        }

        await LoadToolTipsAsync();
    }

    private bool UpdateStatus(LeftPanelSubmodules submodules)
    {
        List<SubmoduleNode> nodes = [.. Descendants().OfType<SubmoduleNode>()];
        if (nodes.Count == 0)
        {
            return false;
        }

        Dictionary<string, SubmoduleInfo> infos = submodules.AllSubmodules.ToDictionary(info => info.Path, info => info);
        infos[submodules.TopProject.Path] = submodules.TopProject;
        foreach (SubmoduleNode node in nodes)
        {
            if (!infos.Remove(node.Info.Path, out SubmoduleInfo? info))
            {
                return false;
            }

            node.Info = info;
            node.ApplyTextAndStyle();
        }

        // Status records left over: the structure does not match.
        return infos.Count == 0;
    }

    private async Task LoadToolTipsAsync()
    {
        CancellationToken cancellationToken = RestartLoading();
        foreach (SubmoduleNode node in Descendants().OfType<SubmoduleNode>().ToList())
        {
            try
            {
                string toolTip = await Host.RunInBackgroundAsync(() => Host.GetSubmoduleToolTip(node.Info, node.GitStatus), cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                node.SetToolTip(toolTip);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private protected override IReadOnlyList<LeftPanelNode> LoadNodes(CancellationToken cancellationToken)
        => _submodules is { } submodules ? CreateNodes(submodules) : [];

    /// <summary>As <c>FillSubmoduleTree</c>: the top project with the submodules under their folders.</summary>
    private List<LeftPanelNode> CreateNodes(LeftPanelSubmodules submodules)
    {
        // The super project of a submodule is the nearest module containing its path (the paths are sorted descending).
        List<string> modulePaths = [.. submodules.AllSubmodules.Select(info => info.Path).Concat(submodules.ModulePaths).OrderByDescending(path => path)];
        List<SubmoduleNode> submoduleNodes = [];
        foreach (SubmoduleInfo submoduleInfo in submodules.AllSubmodules)
        {
            string? superPath = modulePaths.Find(path => submoduleInfo.Path != path && submoduleInfo.Path.Contains(path));
            if (superPath is null || !Host.DirectoryExists(superPath))
            {
                Owner.ReportSubmoduleDirectoryMissing(superPath ?? submoduleInfo.Path, submoduleInfo.Text);
                continue;
            }

            string localPath = Path.GetDirectoryName(submoduleInfo.Path[superPath.Length..]).ToPosixPath() ?? "";
            bool isCurrent = submoduleInfo.Bold;
            submoduleNodes.Add(new SubmoduleNode(this, submoduleInfo, isCurrent, isCurrent ? submodules.CurrentSubmoduleStatus : null, localPath, superPath));
        }

        // The tree of folders (SubmoduleFolderNode) for the paths relative to the top project, with the submodules in them.
        Dictionary<string, LeftPanelNode> pathToNodes = [];
        foreach (SubmoduleNode node in submoduleNodes)
        {
            pathToNodes[GetRelativePath(node)] = node;
        }

        foreach (SubmoduleNode node in submoduleNodes)
        {
            string[] parts = GetRelativePath(node).Split('/');
            for (int i = 0; i < parts.Length - 1; ++i)
            {
                string path = string.Join("/", parts.Take(i + 1));
                if (!pathToNodes.ContainsKey(path))
                {
                    pathToNodes[path] = new SubmoduleFolderNode(this, parts[i]);
                }
            }
        }

        SubmoduleInfo topProject = submodules.TopProject;
        SubmoduleNode topModuleNode = new(this, topProject, topProject.Bold, topProject.Bold ? submodules.CurrentSubmoduleStatus : null, "", topProject.Path);
        HashSet<LeftPanelNode> nodesInTree = [];
        foreach (SubmoduleNode node in submoduleNodes)
        {
            LeftPanelNode parentNode = topModuleNode;
            string[] parts = GetRelativePath(node).Split('/');
            for (int i = 0; i < parts.Length; ++i)
            {
                LeftPanelNode nodeToAdd = pathToNodes[string.Join("/", parts.Take(i + 1))];
                if (nodesInTree.Add(nodeToAdd))
                {
                    parentNode.AddChild(nodeToAdd);
                }

                parentNode = nodeToAdd;
            }
        }

        CompactSingleChildFolderChains(topModuleNode);
        return [topModuleNode];

        string GetRelativePath(SubmoduleNode node)
            => node.SuperPath.SubstringAfter(submodules.TopModuleWorkingDir).ToPosixPath() + node.LocalPath;

        static void CompactSingleChildFolderChains(LeftPanelNode node)
        {
            foreach (LeftPanelNode child in node.Children)
            {
                if (child is SubmoduleFolderNode folderNode)
                {
                    folderNode.CompactSingleChildFolders();
                }

                CompactSingleChildFolderChains(child);
            }
        }
    }

    internal override void PostFill(bool firstTime)
    {
        if (firstTime)
        {
            foreach (LeftPanelNode node in SelfAndDescendants())
            {
                node.IsExpanded = true;
            }
        }
    }
}
