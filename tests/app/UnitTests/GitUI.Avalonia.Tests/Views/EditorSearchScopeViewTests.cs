using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Search;
using GitUI.Avalonia.Editor;
using GitUI.AvaloniaTests.ViewModels;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;
using static GitUI.AvaloniaTests.ViewModels.FileStatusListViewModelTests;

namespace GitUI.AvaloniaTests.Views;

/// <summary>What the search of the editors searches (<c>FindAndReplaceForm</c>): a selection of several lines only, or the next files.</summary>
[TestFixture]
public sealed class EditorSearchScopeViewTests : HeadlessTest
{
    [Test]
    public Task A_selection_of_several_lines_is_searched_only() => OnUiThreadAsync(() =>
    {
        TextEditorViewModel viewModel = new() { IsReadOnly = true };
        viewModel.Load("foo 0\nfoo 1\nfoo 2\nfoo 3\n");
        TextEditorView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 560, Height = 220 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaEdit.TextEditor editor = view.Editor;

        // As SetScanRegion: the lines 2 and 3.
        editor.Select(6, 12);
        view.OpenSearch(replace: false);
        Dispatcher.UIThread.RunJobs();
        view.Search.SearchPattern = "foo";
        view.ScanRegion.Should().Be((6, 18));
        editor.Select(6, 0);

        view.FindNext(backward: false);
        editor.SelectionStart.Should().Be(6);

        // The buttons of the panel (and Enter in its search box) search the same.
        SearchCommands.FindNext.Execute(null, view.Search);
        editor.SelectionStart.Should().Be(12);
        SearchCommands.FindNext.Execute(null, view.Search);
        editor.SelectionStart.Should().Be(6, "the search loops around in the region");
        TextBox searchBox = view.Search.GetVisualDescendants().OfType<TextBox>().First();
        searchBox.PlaceholderText.Should().Be("Find (selection only)...", "as the title of FindAndReplaceForm");
        searchBox.Focus();
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        editor.SelectionStart.Should().Be(12);

        // The caret moved out of the region once the search is closed: the whole text.
        view.Search.Close();
        editor.Select(0, 0);
        view.FindNext(backward: false);
        view.ScanRegion.Should().BeNull();
        editor.SelectionStart.Should().Be(0);
        view.OpenSearch(replace: false);
        Dispatcher.UIThread.RunJobs();
        searchBox.PlaceholderText.Should().Be("Find...");
        window.Close();
    });

    [Test]
    public Task The_search_goes_on_in_the_next_files_while_the_panel_is_open() => OnUiThreadAsync(() =>
    {
        // The diff of each file is "diff of <name>" (FakeViewerHost).
        FileViewerViewModel viewer = new(new DiffViewModelTests.FakeViewerHost());
        FileStatusListViewModel files = Create();
        files.SelectionChanged += (_, _) => _ = viewer.ShowChangesAsync(files.SelectedEntry);
        viewer.SearchOnThrough(() => files);
        FileViewerView view = new() { DataContext = viewer };
        Window window = new() { Content = view, Width = 560, Height = 220 };
        window.Show();
        files.SetDiff(First, Second, CreateStatuses());
        files.Select(entry => entry.Item.Name == "docs/readme.md");
        Dispatcher.UIThread.RunJobs();
        TextEditorView text = view.TextView;

        text.OpenSearch(replace: false);
        Dispatcher.UIThread.RunJobs();
        text.Search.SearchPattern = "Program";
        text.Editor.Select(0, 0);
        text.FindNext(backward: false);
        Dispatcher.UIThread.RunJobs();

        files.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");
        text.Editor.SelectedText.Should().Be("Program");

        // No other file has it: the search comes back to it.
        text.FindNext(backward: false);
        Dispatcher.UIThread.RunJobs();
        files.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");
        text.Editor.SelectedText.Should().Be("Program");

        // Closed, F3 loops around in the file shown.
        text.Search.Close();
        text.FindNext(backward: false);
        Dispatcher.UIThread.RunJobs();
        files.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");
        window.Close();
    });
}
