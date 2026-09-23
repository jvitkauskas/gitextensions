using System.ComponentModel.Design;
using System.Reflection;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.AvaloniaHosting;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using NSubstitute;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>
///  The menu of the Avalonia file status list (<see cref="FileStatusListMenuHost"/>) must show and enable the same items as the
///  WinForms <see cref="FileStatusList"/> for the selection (docs/avalonia-port/PLAN.md, phase 5). A failure after an upstream
///  merge points at a change to re-port.
/// </summary>
[Apartment(ApartmentState.STA)]
public sealed class FileStatusListMenuParityTests
{
    private static readonly GitRevision Parent = new(ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1"));
    private static readonly GitRevision Commit = new(ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2")) { ParentIds = [Parent.ObjectId] };
    private static readonly GitRevision WorkTree = new(ObjectId.WorkTreeId) { ParentIds = [ObjectId.IndexId] };
    private static readonly GitRevision Index = new(ObjectId.IndexId);

    private Form _form = null!;
    private FileStatusList _fileStatusList = null!;
    private GitUICommands _commands = null!;

    [SetUp]
    public void SetUp()
    {
        ServiceContainer serviceContainer = GlobalServiceContainer.CreateDefaultMockServiceContainer();
        IGitModule module = Substitute.For<IGitModule>();
        _commands = new GitUICommands(serviceContainer, module);
        IGitUICommandsSource uiCommandsSource = Substitute.For<IGitUICommandsSource>();
        uiCommandsSource.UICommands.Returns(x => _commands);

        _form = new Form();
        _fileStatusList = new FileStatusList { Parent = _form, UICommandsSource = uiCommandsSource };
        _form.Show();
        _fileStatusList.Bind(refreshArtificial: () => { });
    }

    [TearDown]
    public void TearDown()
    {
        _fileStatusList.Dispose();
        _form.Dispose();
    }

    private static IEnumerable<TestCaseData> Selections
    {
        get
        {
            yield return new TestCaseData(Parent, Commit, new[] { "changed.txt" }).SetArgDisplayNames("changed file of a commit");
            yield return new TestCaseData(Parent, Commit, new[] { "new.txt" }).SetArgDisplayNames("new file of a commit");
            yield return new TestCaseData(Parent, Commit, new[] { "deleted.txt" }).SetArgDisplayNames("deleted file of a commit");
            yield return new TestCaseData(Parent, Commit, new[] { "changed.txt", "new.txt" }).SetArgDisplayNames("two files of a commit");
            yield return new TestCaseData(Index, WorkTree, new[] { "changed.txt" }).SetArgDisplayNames("working directory change");
            yield return new TestCaseData(Parent, Index, new[] { "new.txt" }).SetArgDisplayNames("staged new file");
            yield return new TestCaseData(null, Commit, new[] { "changed.txt" }).SetArgDisplayNames("file of a root commit");
        }
    }

    [TestCaseSource(nameof(Selections))]
    public void Menu_items_match_the_WinForms_list(GitRevision? first, GitRevision second, string[] selectedNames)
    {
        List<GitItemStatus> statuses =
        [
            new("changed.txt") { IsChanged = true, IsTracked = true },
            new("new.txt") { IsNew = true, IsTracked = true },
            new("deleted.txt") { IsDeleted = true, IsTracked = true },
        ];
        foreach (GitItemStatus status in statuses)
        {
            status.Staged = second.ObjectId == ObjectId.WorkTreeId ? StagedStatus.WorkTree : second.ObjectId == ObjectId.IndexId ? StagedStatus.Index : StagedStatus.None;
        }

        _fileStatusList.SetDiffs(first, second, statuses);
        _fileStatusList.SelectedItems = [.. _fileStatusList.AllItems.Where(item => selectedNames.Contains(item.Item.Name))];
        _fileStatusList.UpdateStatusOfMenuItems();
        typeof(FileStatusList).GetMethod("OpenWithDifftool_DropDownOpening", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_fileStatusList, [null, EventArgs.Empty]);

        FileStatusMenuState state = new FileStatusListMenuHost(_commands, window: null!).GetMenuState(
            [.. _fileStatusList.SelectedItems.Select(item => new FileStatusEntry(item.FirstRevision, item.SecondRevision, item.Item))],
            selectedFolder: null);

        Dictionary<string, object?> expected = new()
        {
            ["OpenWithDifftool"] = Item("tsmiOpenWithDifftool").Enabled,
            ["DiffFirstToSelected"] = Item("tsmiDiffFirstToSelected").Enabled,
            ["DiffFirstToLocal"] = Item("tsmiDiffFirstToLocal").Enabled,
            ["DiffSelectedToLocal"] = Item("tsmiDiffSelectedToLocal").Enabled,
            ["ShowDiffToLocal"] = Item("tsmiDiffFirstToLocal").Available,
            ["ShowOpenWorkingDirectoryFile"] = Item("tsmiOpenWorkingDirectoryFile").Available,
            ["ShowEditWorkingDirectoryFile"] = Item("tsmiEditWorkingDirectoryFile").Available,
            ["ShowOpenRevisionFile"] = Item("tsmiOpenRevisionFile").Available,
            ["CanOpenRevisionFile"] = Item("tsmiOpenRevisionFile").Enabled,
            ["ShowSaveAs"] = Item("tsmiSaveAs").Available,
            ["CanCopyPaths"] = Item("tsmiCopyPaths").Enabled,
            ["ShowShowInFolder"] = Item("tsmiShowInFolder").Available,
            ["CanShowInFolder"] = Item("tsmiShowInFolder").Enabled,
            ["CanShowFileHistory"] = Item("tsmiFileHistory").Enabled,
            ["CanBlame"] = Item("tsmiBlame").Enabled,
            ["CanResetFileTo"] = Item("tsmiResetFileTo").Enabled,
            ["ResetToSelectedText"] = Item("tsmiResetFileToSelected") is { Available: true, Enabled: true } toSelected ? toSelected.Text : null,
            ["ResetToParentText"] = Item("tsmiResetFileToParent") is { Available: true, Enabled: true } toParent ? toParent.Text : null,
        };
        Dictionary<string, object?> actual = new()
        {
            ["OpenWithDifftool"] = state.CanOpenWithDifftool,
            ["DiffFirstToSelected"] = state.CanDiffFirstToSelected,
            ["DiffFirstToLocal"] = state.CanDiffFirstToLocal,
            ["DiffSelectedToLocal"] = state.CanDiffSelectedToLocal,
            ["ShowDiffToLocal"] = state.ShowDiffToLocal,
            ["ShowOpenWorkingDirectoryFile"] = state.ShowOpenWorkingDirectoryFile,
            ["ShowEditWorkingDirectoryFile"] = state.ShowEditWorkingDirectoryFile,
            ["ShowOpenRevisionFile"] = state.ShowOpenRevisionFile,
            ["CanOpenRevisionFile"] = state.CanOpenRevisionFile,
            ["ShowSaveAs"] = state.ShowSaveAs,
            ["CanCopyPaths"] = state.CanCopyPaths,
            ["ShowShowInFolder"] = state.ShowShowInFolder,
            ["CanShowInFolder"] = state.CanShowInFolder,
            ["CanShowFileHistory"] = state.CanShowFileHistory,
            ["CanBlame"] = state.CanBlame,
            ["CanResetFileTo"] = state.CanResetFileTo,
            ["ResetToSelectedText"] = state.ResetToSelectedText,
            ["ResetToParentText"] = state.ResetToParentText,
        };

        actual.Should().BeEquivalentTo(expected);
    }

    private ToolStripItem Item(string name)
        => (ToolStripItem)typeof(FileStatusList).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_fileStatusList)!;
}
