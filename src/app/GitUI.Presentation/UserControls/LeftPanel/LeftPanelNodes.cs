using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitCommands.Git;
using GitCommands.Submodules;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces.RepositoryHosts;

namespace GitUI.Presentation.UserControls.LeftPanel;

/// <summary>The icons of the nodes: the names of the images of the WinForms tree (<c>Images</c>), which the view loads.</summary>
public static class LeftPanelIcons
{
    public const string BitBucket = "BitBucket";
    public const string BranchFolder = "BranchFolder";
    public const string BranchLocal = "BranchLocal";
    public const string BranchLocalMerged = "BranchLocalMerged";
    public const string BranchLocalRoot = "BranchLocalRoot";
    public const string BranchRemote = "BranchRemote";
    public const string BranchRemoteMerged = "BranchRemoteMerged";
    public const string BranchRemoteRoot = "BranchRemoteRoot";
    public const string EyeClosed = "EyeClosed";
    public const string FileStatusModified = "FileStatusModified";
    public const string FolderClosed = "FolderClosed";
    public const string FolderSubmodule = "FolderSubmodule";
    public const string GitHub = "GitHub";
    public const string Remote = "Remote";
    public const string Stash = "Stash";
    public const string SubmoduleDirty = "SubmoduleDirty";
    public const string SubmoduleRevisionDown = "SubmoduleRevisionDown";
    public const string SubmoduleRevisionDownDirty = "SubmoduleRevisionDownDirty";
    public const string SubmoduleRevisionSemiDown = "SubmoduleRevisionSemiDown";
    public const string SubmoduleRevisionSemiDownDirty = "SubmoduleRevisionSemiDownDirty";
    public const string SubmoduleRevisionSemiUp = "SubmoduleRevisionSemiUp";
    public const string SubmoduleRevisionSemiUpDirty = "SubmoduleRevisionSemiUpDirty";
    public const string SubmoduleRevisionUp = "SubmoduleRevisionUp";
    public const string SubmoduleRevisionUpDirty = "SubmoduleRevisionUpDirty";
    public const string TagHorizontal = "TagHorizontal";
    public const string VisualStudioTeamServices = "VisualStudioTeamServices";
    public const string WorkTree = "WorkTree";
}

/// <summary>
///  A node of the left panel (port of <c>NodeBase</c> and <c>Node</c>): its text, icon and style, its children, and the
///  operations of a click, a double click and the hotkeys.
/// </summary>
public abstract partial class LeftPanelNode : ObservableObject
{
    private protected LeftPanelNode(LeftPanelTree? tree)
    {
        Tree = tree ?? (LeftPanelTree)this;
    }

    /// <summary>The tree (the root node) of the node.</summary>
    public LeftPanelTree Tree { get; }

    public LeftPanelNode? Parent { get; internal set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChildren))]
    public partial IReadOnlyList<LeftPanelNode> Children { get; internal set; } = new List<LeftPanelNode>();

    public bool HasChildren => Children.Count > 0;

    [ObservableProperty]
    public partial string Text { get; protected set; } = "";

    [ObservableProperty]
    public partial string? ToolTip { get; protected set; }

    /// <summary>The icon (see <see cref="LeftPanelIcons"/>), none if <see langword="null"/>.</summary>
    [ObservableProperty]
    public partial string? IconKey { get; protected set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>Whether the node is in the multi-selection (underlined; <c>NodeBase.IsSelected</c>).</summary>
    [ObservableProperty]
    public partial bool IsMultiSelected { get; internal set; }

    /// <summary>Whether the node is a result of the search (highlighted).</summary>
    [ObservableProperty]
    public partial bool IsSearchMatch { get; internal set; }

    /// <summary>Whether the node is grayed out (a revision hidden from the grid, a deleted worktree).</summary>
    [ObservableProperty]
    public partial bool IsGrayed { get; protected set; }

    /// <summary>Whether the text is bold (the current branch, submodule or worktree).</summary>
    public virtual bool IsBold => false;

    public virtual bool IsItalic => false;

    /// <summary>Whether a double click runs an operation, rather than expanding or collapsing the node.</summary>
    public virtual bool HasDoubleClickAction => false;

    /// <summary>The name identifying the node among its siblings across reloads (<c>TreeNode.Name</c>).</summary>
    protected internal virtual string Key => Text;

    /// <summary>The path of the keys from the tree (<c>GetFullNamePath</c>).</summary>
    public string KeyPath => Parent is null ? Key : $"{Parent.KeyPath}/{Key}";

    private protected LeftPanelViewModel Panel => Tree.Owner;

    /// <summary>The descendants, depth first and in order (<c>DepthEnumerator</c>).</summary>
    public IEnumerable<LeftPanelNode> Descendants()
    {
        foreach (LeftPanelNode child in Children)
        {
            yield return child;
            foreach (LeftPanelNode descendant in child.Descendants())
            {
                yield return descendant;
            }
        }
    }

    public IEnumerable<LeftPanelNode> SelfAndDescendants() => Descendants().Prepend(this);

    public override string ToString() => Text;

    internal void AddChild(LeftPanelNode child)
    {
        child.Parent = this;
        if (Children is not List<LeftPanelNode> list)
        {
            list = [.. Children];
            Children = list;
        }

        list.Add(child);
    }

    /// <summary>Sets the text and the style, as <c>ApplyText</c> and <c>ApplyStyle</c> when the node is shown.</summary>
    internal void ApplyTextAndStyle()
    {
        Text = DisplayText();
        ApplyStyle();
    }

    protected virtual string DisplayText() => Text;

    /// <summary>As <c>ApplyStyle</c>: the icon, the tooltip and the colors.</summary>
    protected internal virtual void ApplyStyle()
    {
        ToolTip = null;
    }

    internal virtual void OnSelected()
    {
    }

    internal virtual void OnDoubleClick()
    {
    }

    internal virtual void OnRename()
    {
    }

    internal virtual void OnDelete()
    {
    }

    private protected bool Run(LeftPanelAction action) => Panel.Run(action, this);
}

/// <summary>A node of a reference or a folder of references (port of <c>BaseRevisionNode</c>).</summary>
[DebuggerDisplay("(Node) FullPath = {FullPath}")]
public abstract class LeftPanelRevisionNode : LeftPanelNode
{
    public const char PathSeparator = '/';

    private bool _visible;

    private protected LeftPanelRevisionNode(LeftPanelTree tree, string fullPath, bool visible)
        : base(tree)
    {
        fullPath = fullPath.Trim();
        ArgumentException.ThrowIfNullOrEmpty(fullPath);

        FullPath = fullPath;
        int nameIndex = fullPath.LastIndexOf(PathSeparator);
        Name = nameIndex == -1 ? fullPath : fullPath[(nameIndex + 1)..];
        ParentPath = nameIndex == -1 ? null : fullPath[..nameIndex];
        _visible = visible;
        Text = Name;
    }

    /// <summary>The short name, e.g. <c>issue1344</c>.</summary>
    public string Name { get; }

    /// <summary>The full path, e.g. <c>issues/issue1344</c>.</summary>
    public string FullPath { get; }

    internal string? ParentPath { get; }

    /// <summary>The revision of the reference, zero for a folder.</summary>
    public ObjectId ObjectId { get; init; }

    /// <summary>Whether the revision is in the grid (a hidden one is grayed out and not selected in the grid).</summary>
    public bool Visible
    {
        get => _visible;
        internal set
        {
            if (_visible != value)
            {
                _visible = value;
                ApplyStyle();
                OnPropertyChanged();
            }
        }
    }

    protected internal override string Key => Name;

    protected override string DisplayText() => Name;

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IsGrayed = !Visible;
        IconKey = Visible ? null : LeftPanelIcons.EyeClosed;
    }

    /// <summary>As <c>CreateRootNode</c>: adds the node to its folder, creating the missing folders; returns the new top node.</summary>
    internal LeftPanelRevisionNode? CreateRootNode(IDictionary<string, LeftPanelRevisionNode> pathToNode, Func<LeftPanelTree, string, LeftPanelRevisionNode> createPathNode)
    {
        if (string.IsNullOrEmpty(ParentPath))
        {
            return this;
        }

        LeftPanelRevisionNode? result;
        if (pathToNode.TryGetValue(ParentPath, out LeftPanelRevisionNode? parent))
        {
            result = null;
        }
        else
        {
            parent = createPathNode(Tree, ParentPath);
            pathToNode.Add(ParentPath, parent);
            result = parent.CreateRootNode(pathToNode, createPathNode);
        }

        parent.AddChild(this);
        return result;
    }

    /// <summary>As <c>SelectRevision</c>: selects the revision in the grid.</summary>
    private protected virtual void SelectRevision()
    {
        if (!ObjectId.IsZero)
        {
            Panel.GoToRevision(ObjectId);
        }
    }
}

/// <summary>A folder of references (port of <c>BasePathNode</c>).</summary>
public class LeftPanelPathNode : LeftPanelRevisionNode
{
    internal LeftPanelPathNode(LeftPanelTree tree, string fullPath)
        : base(tree, fullPath, visible: true)
    {
    }

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IconKey = FullPath == Panel.Strings.Inactive.Text ? LeftPanelIcons.EyeClosed : LeftPanelIcons.BranchFolder;
    }
}

/// <summary>A folder of local branches (port of <c>BranchPathNode</c>).</summary>
[DebuggerDisplay("(Branch path) FullPath = {FullPath}")]
public sealed class BranchPathNode : LeftPanelPathNode
{
    internal BranchPathNode(LeftPanelTree tree, string fullPath)
        : base(tree, fullPath)
    {
    }

    /// <summary>The local branches in the folder, recursively.</summary>
    public IReadOnlyList<string> Branches => [.. Descendants().OfType<LocalBranchNode>().Select(branch => branch.FullPath)];
}

/// <summary>A local or remote branch (port of <c>BaseBranchLeafNode</c>).</summary>
public abstract class LeftPanelBranchNode : LeftPanelRevisionNode
{
    private readonly string _iconUnmerged;
    private readonly string _iconMerged;
    private bool _isMerged;

    private protected LeftPanelBranchNode(LeftPanelTree tree, ObjectId objectId, string fullPath, bool visible, string iconUnmerged, string iconMerged)
        : base(tree, fullPath, visible)
    {
        ObjectId = objectId;
        _iconUnmerged = iconUnmerged;
        _iconMerged = iconMerged;
    }

    /// <summary>The ahead/behind counts (e.g. <c>1↑ 2↓</c>), shown after the name.</summary>
    public string? AheadBehind { get; private set; }

    /// <summary>The tracked branch of a local branch, or the tracking branch of a remote one.</summary>
    public string? RelatedBranch { get; private set; }

    /// <summary>Whether the branch is merged into the revision selected in the grid.</summary>
    public bool IsMerged
    {
        get => _isMerged;
        internal set
        {
            if (_isMerged != value)
            {
                _isMerged = value;
                ApplyStyle();
                OnPropertyChanged();
            }
        }
    }

    public override bool HasDoubleClickAction => true;

    internal void UpdateAheadBehind(string aheadBehindData, string relatedBranch)
    {
        AheadBehind = aheadBehindData;
        RelatedBranch = relatedBranch;
    }

    protected override string DisplayText() => string.IsNullOrEmpty(AheadBehind) ? Name : $"{Name} ({AheadBehind})";

    // As BaseBranchLeafNode.SelectRevision: with Alt, the related branch.
    private protected override void SelectRevision()
    {
        if (Panel.IsAlternateSelection && RelatedBranch is { } relatedBranch)
        {
            Panel.GoToRef(relatedBranch);
            return;
        }

        base.SelectRevision();
    }

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IconKey = Visible ? IsMerged ? _iconMerged : _iconUnmerged : LeftPanelIcons.EyeClosed;
        if (!Visible)
        {
            ToolTip = string.Format(Panel.Strings.InvisibleCommit.Text, FullPath);
        }
        else if (IsMerged)
        {
            ToolTip = string.Format(Panel.Strings.ContainedInCurrentCommit.Text, Name);
        }
    }

    internal override void OnSelected() => SelectRevision();
}

/// <summary>A local branch (port of <c>LocalBranchNode</c>).</summary>
[DebuggerDisplay("(Local) FullPath = {FullPath}, Hash = {ObjectId}, Visible: {Visible}")]
public sealed class LocalBranchNode : LeftPanelBranchNode
{
    internal LocalBranchNode(LeftPanelTree tree, ObjectId objectId, string fullPath, bool isCurrent, bool visible)
        : base(tree, objectId, fullPath, visible, LeftPanelIcons.BranchLocal, LeftPanelIcons.BranchLocalMerged)
    {
        IsCurrent = isCurrent;
    }

    /// <summary>Whether this is the checked out branch (bold).</summary>
    public bool IsCurrent { get; }

    public override bool IsBold => IsCurrent;

    internal override void OnDoubleClick() => Run(LeftPanelAction.CheckoutBranch);

    internal override void OnRename() => Run(LeftPanelAction.RenameBranch);

    internal override void OnDelete() => Run(LeftPanelAction.DeleteBranch);
}

/// <summary>A remote branch (port of <c>RemoteBranchNode</c>).</summary>
[DebuggerDisplay("(Remote) FullPath = {FullPath}, Hash = {ObjectId}, Visible: {Visible}")]
public sealed class RemoteBranchNode : LeftPanelBranchNode
{
    internal RemoteBranchNode(LeftPanelTree tree, ObjectId objectId, string fullPath, bool visible)
        : base(tree, objectId, fullPath, visible, LeftPanelIcons.BranchRemote, LeftPanelIcons.BranchRemoteMerged)
    {
        int separator = FullPath.IndexOf(PathSeparator);
        RemoteName = separator < 0 ? FullPath : FullPath[..separator];
        BranchName = separator < 0 ? "" : FullPath[(separator + 1)..];
    }

    /// <summary>The name of the remote, e.g. <c>origin</c>.</summary>
    public string RemoteName { get; }

    /// <summary>The name of the branch on the remote, e.g. <c>feature/x</c>.</summary>
    public string BranchName { get; }

    internal override void OnDoubleClick() => Run(LeftPanelAction.CheckoutRemoteBranch);

    internal override void OnDelete() => Run(LeftPanelAction.DeleteRemoteBranch);
}

/// <summary>A remote (port of <c>RemoteRepoNode</c>).</summary>
[DebuggerDisplay("Remote = {Remote.Name}, FullPath = {FullPath}")]
public sealed class RemoteRepoNode : LeftPanelRevisionNode
{
    internal RemoteRepoNode(LeftPanelTree tree, string fullPath, Remote remote, bool isEnabled)
        : base(tree, fullPath, visible: true)
    {
        Remote = remote;
        Enabled = isEnabled;
    }

    public Remote Remote { get; }

    /// <summary>Whether the remote is active (else it is listed under the inactive remotes).</summary>
    public bool Enabled { get; }

    public bool IsRemoteUrlUsingHttp => Remote.FetchUrl.IsUrlUsingHttp();

    public override bool HasDoubleClickAction => true;

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();

        ToolTip = Remote.PushUrls is { Count: > 1 } pushUrls && Remote.FetchUrl != pushUrls[0]
            ? $"Fetch: {Remote.FetchUrl}\nPush: {string.Join("\n", pushUrls)}"
            : Remote.FetchUrl;

        string fetchUrl = Remote.FetchUrl;
        IconKey = fetchUrl.Contains("github.com") ? LeftPanelIcons.GitHub
            : fetchUrl.Contains("bitbucket.") ? LeftPanelIcons.BitBucket
            : fetchUrl.Contains("visualstudio.com") || fetchUrl.Contains("dev.azure.com") ? LeftPanelIcons.VisualStudioTeamServices
            : LeftPanelIcons.Remote;
    }

    internal override void OnDoubleClick() => Run(LeftPanelAction.ManageRemotes);
}

/// <summary>The folder of the inactive remotes (port of <c>RemoteRepoFolderNode</c>).</summary>
[DebuggerDisplay("(Folder) FullPath = {FullPath}")]
public sealed class RemoteRepoFolderNode : LeftPanelRevisionNode
{
    internal RemoteRepoFolderNode(LeftPanelTree tree, string name)
        : base(tree, name, visible: true)
    {
    }

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IconKey = LeftPanelIcons.EyeClosed;
    }
}

/// <summary>A tag (port of <c>TagNode</c>).</summary>
[DebuggerDisplay("(Tag) FullPath = {FullPath}, Hash = {ObjectId}, Visible: {Visible}")]
public sealed class TagNode : LeftPanelRevisionNode
{
    internal TagNode(LeftPanelTree tree, ObjectId objectId, string fullPath, bool visible)
        : base(tree, fullPath, visible)
    {
        ObjectId = objectId;
    }

    public override bool HasDoubleClickAction => true;

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IconKey = Visible ? LeftPanelIcons.TagHorizontal : LeftPanelIcons.EyeClosed;
    }

    internal override void OnSelected() => SelectRevision();

    internal override void OnDoubleClick() => Run(LeftPanelAction.CreateBranchFromTag);

    internal override void OnDelete() => Run(LeftPanelAction.DeleteTag);
}

/// <summary>A stash (port of <c>StashNode</c>).</summary>
[DebuggerDisplay("(Stash) FullPath = {FullPath}, Hash = {ObjectId}, Visible: {Visible}")]
public sealed class StashNode : LeftPanelRevisionNode
{
    internal StashNode(LeftPanelTree tree, ObjectId objectId, string reflogSelector, string subject, bool visible)
        : base(tree, reflogSelector.RemovePrefix("refs/"), visible)
    {
        ObjectId = objectId;
        DisplayName = $"{reflogSelector.RemovePrefix(GitRefName.RefsStashPrefix)}: {subject}";
        ReflogSelector = reflogSelector;
    }

    public string DisplayName { get; }

    /// <summary>The stash, e.g. <c>refs/stash@{0}</c>.</summary>
    public string ReflogSelector { get; }

    public override bool HasDoubleClickAction => true;

    protected override string DisplayText() => DisplayName;

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IconKey = Visible ? LeftPanelIcons.Stash : LeftPanelIcons.EyeClosed;
    }

    internal override void OnSelected() => SelectRevision();

    internal override void OnDoubleClick() => Run(LeftPanelAction.OpenStash);
}

/// <summary>A submodule, or the top project (port of <c>SubmoduleNode</c>).</summary>
public sealed class SubmoduleNode : LeftPanelNode
{
    internal SubmoduleNode(LeftPanelTree tree, SubmoduleInfo submoduleInfo, bool isCurrent, IReadOnlyList<GitItemStatus>? gitStatus, string localPath, string superPath)
        : base(tree)
    {
        Info = submoduleInfo;
        IsCurrent = isCurrent;
        GitStatus = gitStatus;
        LocalPath = localPath;
        SuperPath = superPath;

        // e.g. "Externals/conemu-inside [no branch]"; the branch is missing if the submodule is not initialized and updated.
        string[] pathAndBranch = Info.Text.Split(' ', 2);
        SubmoduleName = pathAndBranch[0].SubstringAfterLast('/');
        BranchText = pathAndBranch.Length == 2 ? " " + pathAndBranch[1] : "";
    }

    public SubmoduleInfo Info { get; internal set; }

    /// <summary>Whether this is the current module (bold).</summary>
    public bool IsCurrent { get; }

    public IReadOnlyList<GitItemStatus>? GitStatus { get; }

    /// <summary>The path of the submodule in its super project.</summary>
    public string LocalPath { get; }

    /// <summary>The working directory of its super project.</summary>
    public string SuperPath { get; }

    public string SubmoduleName { get; }

    public string BranchText { get; }

    public override bool IsBold => IsCurrent;

    public override bool HasDoubleClickAction => true;

    protected internal override string Key => SubmoduleName;

    protected override string DisplayText() => SubmoduleName + BranchText + Info.Detailed?.AddedAndRemovedText;

    internal void SetToolTip(string toolTip) => ToolTip = toolTip;

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        ToolTip = DisplayText();

        // As GetSubmoduleItemImage.
        DetailedSubmoduleInfo? details = Info.Detailed;
        IconKey = (details?.Status, details?.IsDirty) switch
        {
            (SubmoduleStatus.FastForward, true) => LeftPanelIcons.SubmoduleRevisionUpDirty,
            (SubmoduleStatus.FastForward, false) => LeftPanelIcons.SubmoduleRevisionUp,
            (SubmoduleStatus.Rewind, true) => LeftPanelIcons.SubmoduleRevisionDownDirty,
            (SubmoduleStatus.Rewind, false) => LeftPanelIcons.SubmoduleRevisionDown,
            (SubmoduleStatus.NewerTime, true) => LeftPanelIcons.SubmoduleRevisionSemiUpDirty,
            (SubmoduleStatus.NewerTime, false) => LeftPanelIcons.SubmoduleRevisionSemiUp,
            (SubmoduleStatus.OlderTime, true) => LeftPanelIcons.SubmoduleRevisionSemiDownDirty,
            (SubmoduleStatus.OlderTime, false) => LeftPanelIcons.SubmoduleRevisionSemiDown,
            (_, true) => LeftPanelIcons.SubmoduleDirty,
            (_, false) => LeftPanelIcons.FileStatusModified,
            _ => LeftPanelIcons.FolderSubmodule,
        };
    }

    // For the current module, which is open already, a new instance is launched.
    internal override void OnDoubleClick() => Run(IsCurrent ? LeftPanelAction.OpenSubmoduleInNewInstance : LeftPanelAction.OpenSubmodule);
}

/// <summary>A folder of submodules (port of <c>SubmoduleFolderNode</c>).</summary>
public sealed class SubmoduleFolderNode : LeftPanelNode
{
    private string _name;

    internal SubmoduleFolderNode(LeftPanelTree tree, string name)
        : base(tree)
    {
        _name = name;
        Text = name;
    }

    public override bool IsItalic => true;

    protected override string DisplayText() => _name;

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IconKey = LeftPanelIcons.FolderClosed;
    }

    /// <summary>Merges chains of single-child folders, e.g. <c>extension/src/test</c>.</summary>
    internal void CompactSingleChildFolders()
    {
        while (Children is [SubmoduleFolderNode childFolder])
        {
            _name += "/" + childFolder._name;
            foreach (LeftPanelNode grandChild in childFolder.Children)
            {
                grandChild.Parent = this;
            }

            Children = childFolder.Children;
        }

        Text = _name;
    }
}

/// <summary>A worktree (port of <c>WorktreeNode</c>).</summary>
[DebuggerDisplay("(Worktree) Path = {Worktree.Path}, Branch = {Worktree.Branch}")]
public sealed class WorktreeNode : LeftPanelNode
{
    private readonly string _displayPath;

    internal WorktreeNode(LeftPanelTree tree, GitWorktree worktree, bool isCurrent, string displayPath)
        : base(tree)
    {
        Worktree = worktree;
        IsCurrent = isCurrent;
        _displayPath = displayPath;
    }

    public GitWorktree Worktree { get; }

    /// <summary>Whether this is the worktree of the repository (bold).</summary>
    public bool IsCurrent { get; }

    public override bool IsBold => IsCurrent;

    public override bool HasDoubleClickAction => true;

    protected internal override string Key => Worktree.Path;

    protected override string DisplayText() => Worktree.GetDisplayName(_displayPath);

    protected internal override void ApplyStyle()
    {
        base.ApplyStyle();
        IsGrayed = Worktree.IsDeleted;
        IconKey = LeftPanelIcons.WorkTree;

        string? shortSha = Worktree.Sha1?.Length >= 7 ? Worktree.Sha1[..7] : Worktree.Sha1;
        string status = IsCurrent ? " (current)" : Worktree.IsDeleted ? " (deleted)" : "";
        string branchLine = Worktree.HeadType is GitWorktreeHeadType.Bare ? "bare"
            : Worktree.HeadType is GitWorktreeHeadType.Detached ? $"detached at {shortSha}"
            : Worktree.Branch ?? "unknown";
        ToolTip = shortSha is not null
            ? $"{Worktree.Path}{status}\nBranch: {branchLine}\nHEAD: {shortSha}"
            : $"{Worktree.Path}{status}\nBranch: {branchLine}";
    }

    internal override void OnSelected()
    {
        if (Worktree.Sha1 is { } sha1 && ObjectId.TryParse(sha1, out ObjectId objectId))
        {
            Panel.GoToRevision(objectId);
        }
    }

    internal override void OnDoubleClick()
    {
        if (!IsCurrent && !Worktree.IsDeleted)
        {
            Run(LeftPanelAction.OpenWorktree);
        }
    }
}
