using System.ComponentModel.Design;
using System.Reflection;
using System.Text;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using NSubstitute;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  The Avalonia port of the file status list (<see cref="FileStatusTreeBuilder"/>, <see cref="FileStatusListViewModel"/>) must
///  build the same tree as the WinForms <see cref="FileStatusList"/>: texts, icons and expansion (docs/avalonia-port/PLAN.md,
///  phase 5). A failure after an upstream merge points at a change to re-port.
/// </summary>
[Apartment(ApartmentState.STA)]
public sealed class FileStatusListPortParityTests
{
    private Form _form = null!;
    private FileStatusList _fileStatusList = null!;
    private DiffListSortType _sorting;
    private bool _mergeSingleItemWithFolder;

    [SetUp]
    public void SetUp()
    {
        _sorting = DiffListSortService.Instance.DiffListSorting;
        _mergeSingleItemWithFolder = AppSettings.FileStatusMergeSingleItemWithFolder.Value;

        ServiceContainer serviceContainer = GlobalServiceContainer.CreateDefaultMockServiceContainer();
        IGitModule module = Substitute.For<IGitModule>();
        GitUICommands commands = new(serviceContainer, module);
        IGitUICommandsSource uiCommandsSource = Substitute.For<IGitUICommandsSource>();
        uiCommandsSource.UICommands.Returns(x => commands);

        _form = new Form();
        _fileStatusList = new FileStatusList { Parent = _form, UICommandsSource = uiCommandsSource };
        _form.Show();

        // Subscribes to the sorting of DiffListSortService.
        _fileStatusList.Bind(refreshArtificial: () => { });
    }

    [TearDown]
    public void TearDown()
    {
        DiffListSortService.Instance.DiffListSorting = _sorting;
        AppSettings.FileStatusMergeSingleItemWithFolder.Value = _mergeSingleItemWithFolder;
        _fileStatusList.Dispose();
        _form.Dispose();
    }

    [Test]
    public void Single_diff_matches_the_WinForms_list(
        [Values(DiffListSortType.FilePath, DiffListSortType.FilePathFlat, DiffListSortType.FileStatus, DiffListSortType.FileStatusFlat)] DiffListSortType sorting,
        [Values] bool mergeSingleItemWithFolder)
    {
        FileStatusGroup group = new(new GitRevision(ObjectId.Random()), new GitRevision(ObjectId.Random()), "", CreateStatuses());

        CompareTrees([group], sorting, mergeSingleItemWithFolder, filter: "");
    }

    [Test]
    public void Several_diffs_match_the_WinForms_list([Values(DiffListSortType.FilePath, DiffListSortType.FileStatusFlat)] DiffListSortType sorting)
    {
        GitRevision selected = new(ObjectId.Random());
        FileStatusGroup[] groups =
        [
            new(new GitRevision(ObjectId.Random()), selected, "First parent", CreateStatuses()),
            new(new GitRevision(ObjectId.Random()), selected, "Second parent", []),
            new(new GitRevision(ObjectId.Random()), selected, "Only B", [new("src/only-b.cs") { IsChanged = true, DiffStatus = DiffBranchStatus.OnlyBChange }], IconName: FileStatusIcons.DiffB),
        ];

        CompareTrees(groups, sorting, mergeSingleItemWithFolder: false, filter: "");
    }

    [TestCase(@"\.cs$")]
    [TestCase("file1")]
    [TestCase("no match")]
    public void Filtered_diff_matches_the_WinForms_list(string filter)
    {
        FileStatusGroup group = new(new GitRevision(ObjectId.Random()), new GitRevision(ObjectId.Random()), "", CreateStatuses());

        CompareTrees([group], DiffListSortType.FilePath, mergeSingleItemWithFolder: false, filter);
    }

    private void CompareTrees(IReadOnlyList<FileStatusGroup> groups, DiffListSortType sorting, bool mergeSingleItemWithFolder, string filter)
    {
        DiffListSortService.Instance.DiffListSorting = sorting;
        AppSettings.FileStatusMergeSingleItemWithFolder.Value = mergeSingleItemWithFolder;

        // WinForms: the files of the diffs, as FileStatusList.SetDiffsAsync shows the result of FileStatusDiffCalculator.
        List<FileStatusWithDescription> descriptions = [.. groups.Select(g => new FileStatusWithDescription(g.FirstRevision, g.SecondRevision, g.Summary, g.Statuses, iconName: g.IconName))];
        typeof(FileStatusList).GetMethod("UpdateFileStatusListView", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_fileStatusList, [descriptions, false, Type.Missing, default(CancellationToken)]);
        if (filter.Length > 0)
        {
            _fileStatusList.SetFilter(filter);
        }

        FileStatusListViewModel viewModel = new(
            new FileStatusListStrings(),
            new FileStatusTreeOptions(sorting, MergeSingleItemsWithFolder: mergeSingleItemWithFolder, ShowGroupNodesInFlatList: AppSettings.FileStatusShowGroupNodesInFlatList.Value),
            fileNameOnlyFilter: AppSettings.TruncatePathMethod == TruncatePathMethod.FileNameOnly)
        {
            // As the WinForms default, which does not expand the parents of a selected first file.
            SelectFirstItemOnSetItems = false,
        };
        viewModel.SetGroups(groups);
        viewModel.Filter = filter;

        string expected = Serialize(_fileStatusList.GetTestAccessor().FileStatusListView.Nodes.Cast<TreeNode>());
        string actual = Serialize(viewModel.Nodes);
        actual.Should().Be(expected);
        expected.Should().Be(filter == "no match" ? "" : expected, "a filter without a match leaves the list empty");
        if (filter != "no match")
        {
            expected.Should().NotBeEmpty();
        }
    }

    private static string Serialize(IEnumerable<TreeNode> nodes)
    {
        Dictionary<int, string> keys = GetImageKeys();
        StringBuilder text = new();
        Append(nodes, 0);
        return text.ToString();

        void Append(IEnumerable<TreeNode> level, int depth)
        {
            foreach (TreeNode node in level)
            {
                AppendNode(text, depth, node.Text, node.ImageIndex < 0 ? FileStatusIcons.FolderClosed /* the default image of the tree */ : keys.GetValueOrDefault(node.ImageIndex, "?"), node.Nodes.Count > 0 && node.IsExpanded);
                Append(node.Nodes.Cast<TreeNode>(), depth + 1);
            }
        }
    }

    private static string Serialize(IEnumerable<FileStatusNode> nodes)
    {
        StringBuilder text = new();
        Append(nodes, 0);
        return text.ToString();

        void Append(IEnumerable<FileStatusNode> level, int depth)
        {
            foreach (FileStatusNode node in level)
            {
                AppendNode(text, depth, node.Text, node.IconKey, node.Children.Count > 0 && node.IsExpanded);
                Append(node.Children, depth + 1);
            }
        }
    }

    private static void AppendNode(StringBuilder text, int depth, string nodeText, string icon, bool expanded)
        => text.Append(' ', depth * 2).Append(expanded ? "- " : "+ ").Append(nodeText).Append(" [").Append(icon).AppendLine("]");

    /// <summary>The image keys of the WinForms list by image index (FileStatusList._imageListData.StateImageIndexMap).</summary>
    private static Dictionary<int, string> GetImageKeys()
    {
        object imageListData = typeof(FileStatusList).GetField("_imageListData", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Dictionary<string, int> map = (Dictionary<string, int>)imageListData.GetType().GetProperty("StateImageIndexMap")!.GetValue(imageListData)!;
        return map.ToDictionary(pair => pair.Value, pair => pair.Key);
    }

    private static List<GitItemStatus> CreateStatuses()
    {
        // As FileStatusListSorterTests, with the various statuses.
        List<GitItemStatus> statuses =
        [
            new("root_file") { IsChanged = true },
            new(".hidden_root_file") { IsNew = true },
            new("ext/submodule/") { IsSubmodule = true, IsDirty = true },
            new("ext/subfolder/filees") { IsDeleted = true },
            new("ext/file") { IsChanged = true, DiffStatus = DiffBranchStatus.OnlyAChange },
            new("ext2/file.cs") { IsChanged = true },
            new("1/file1") { IsChanged = true },
            new("1/subfolder/file1s.cs") { IsNew = true, IsTracked = false },
            new("1/2/file12") { IsUnmerged = true },
            new("1/3/file13") { IsCopied = true },
            new("1/3/4/file134.cs") { IsChanged = true, DiffStatus = DiffBranchStatus.SameChange },
            new("5/file1") { IsChanged = true },
            new("5/6/file56b") { IsChanged = true },
            new("5/6/file56a") { IsRenamed = true, RenameCopyPercentage = "100", OldName = "5/6/old56a" },
            new("5/7/file57b") { IsRenamed = true, RenameCopyPercentage = "80", OldName = "old/file57b" },
            new("5/7/file57a") { IsChanged = true, DiffStatus = DiffBranchStatus.UnequalChange },
        ];
        foreach (GitItemStatus status in statuses.Where(s => !s.IsNew || s.Name.StartsWith('.')))
        {
            status.IsTracked = true;
        }

        return statuses;
    }
}
