using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitExtensions.Extensibility.BuildServerIntegration;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Presentation.Services;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
///  Headless view tests of the columns and highlighting of the revision grid beyond its first version: stash labels, notes,
///  avatars, build statuses, the highlighted author, gray non-relatives, the hover highlight, the menu of a reference label
///  and the hotkeys (phase 4).
/// </summary>
[TestFixture]
public sealed class RevisionGridColumnsViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (Window window, RevisionGridView _, RevisionGridViewModel viewModel, List<GitRevision> history, FakeHost _) = Show();

        viewModel.Rows.Single(r => r.Subject.StartsWith("WIP")).Refs.Select(r => r.Name).Should().Equal("stash@{0}");
        viewModel.Rows.Where(r => r.Avatar is not null).Should().HaveCount(viewModel.Rows.Count(r => !r.Revision.IsArtificial), "the avatars of the rows shown");

        // A build status reported later.
        viewModel.SelectedRow = viewModel.Rows[1];
        history[3].BuildStatus = new BuildInfo { Status = BuildStatus.InProgress, Description = "#8 running" };
        Dispatcher.UIThread.RunJobs();
        SaveScreenshot(window.CaptureRenderedFrame(), $"revision-grid-columns-{theme}");
        window.Close();
    });

    [Test]
    public Task The_texts_of_the_non_relatives_are_gray_and_the_authored_rows_highlighted() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView view, RevisionGridViewModel viewModel, List<GitRevision> _, FakeHost _) = Show();
        DataGridRow Row(string subject) => view.GetVisualDescendants().OfType<DataGridRow>().Single(r => r.IsVisible && ((RevisionGridRow)r.DataContext!).Subject == subject);

        Row("Experiment").Classes.Should().Contain("nonRelative", "not an ancestor of HEAD");
        Row("Update the documentation").Classes.Should().NotContain("nonRelative");

        viewModel.SelectedRow = viewModel.Rows.Single(r => r.Subject == "Update the documentation");
        Dispatcher.UIThread.RunJobs();
        Row("Release").Classes.Should().Contain("authored", "by Alice, as the selected revision");
        Row("Start feature").Classes.Should().NotContain("authored");
        Row("WIP on main: Update the documentation").Classes.Should().Contain("authored", "a stash by Alice");
        Row("Update the documentation").Classes.Should().NotContain("authored", "the selection background shows");

        // The branch of the experiment highlighted: the others are the non-relatives.
        viewModel.SelectedRow = viewModel.Rows.Single(r => r.Subject == "Experiment");
        viewModel.HighlightSelectedBranch();
        Dispatcher.UIThread.RunJobs();
        view.DrawStyle.Should().Be(RevisionGraphDrawStyle.HighlightSelected);
        Row("Experiment").Classes.Should().NotContain("nonRelative");
        Row("Update the documentation").Classes.Should().Contain("nonRelative");
        window.Close();
    });

    [Test]
    public Task A_right_click_on_a_reference_label_focuses_the_menu_on_it() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView view, RevisionGridViewModel viewModel, List<GitRevision> _, FakeHost _) = Show();
        RevisionGridRefMenuRequest? request = null;
        viewModel.ContextMenuProvider = () =>
        {
            request = viewModel.RefMenuRequest;
            return [new MenuModelItem("_Item", () => { })];
        };

        Border label = view.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("ref") && b.DataContext is RevisionRefItem { Name: "experiment" });
        Point center = label.TranslatePoint(new Point(label.Bounds.Width / 2, label.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Right, RawInputModifiers.Shift);
        window.MouseUp(center, MouseButton.Right, RawInputModifiers.Shift);
        Dispatcher.UIThread.RunJobs();

        viewModel.SelectedRow!.Subject.Should().Be("Experiment", "a right click selects the row");
        viewModel.RefMenuRequest!.GitRef.Name.Should().Be("experiment");
        viewModel.RefMenuRequest.Shift.Should().BeTrue();
        viewModel.RefMenuRequest.Control.Should().BeFalse();

        // Not on a label: the menu of the revision.
        Point subject = label.TranslatePoint(new Point(label.Bounds.Width + 40, label.Bounds.Height / 2), window)!.Value;
        window.MouseDown(subject, MouseButton.Right, RawInputModifiers.None);
        window.MouseUp(subject, MouseButton.Right, RawInputModifiers.None);
        viewModel.RefMenuRequest.Should().BeNull();
        request.Should().BeNull("the menu was not opened");
        window.Close();
    });

    [Test]
    public Task Hovering_a_reference_label_highlights_its_ancestry_in_the_graph() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView view, RevisionGridViewModel viewModel, List<GitRevision> history, FakeHost host) = Show();
        host.HoverResult = new HashSet<ObjectId> { history[1].ObjectId };

        Border label = view.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("ref") && b.DataContext is RevisionRefItem { Name: "experiment" });
        window.MouseMove(label.TranslatePoint(new Point(3, 3), window)!.Value);
        Dispatcher.UIThread.RunJobs();

        host.HoverRequests.Should().ContainSingle().Which.Ref!.Name.Should().Be("experiment");
        host.HoverRequests[0].Row.Should().Be(viewModel.Rows.Single(r => r.Subject == "Experiment").Index);
        host.HoverRequests[0].Count.Should().BeGreaterThan(0, "the visible rows");
        viewModel.HoverHighlightedIds.Should().BeEquivalentTo([history[1].ObjectId]);

        // Leaving the grid clears the highlight.
        host.HoverResult = null;
        window.MouseMove(new Point(window.Width - 2, window.Height - 2));
        Dispatcher.UIThread.RunJobs();
        host.HoverRequests[^1].Ref.Should().BeNull();
        window.Close();
    });

    [Test]
    public Task A_click_on_a_build_status_opens_its_report_and_the_hotkeys_run_their_commands() => OnUiThreadAsync(() =>
    {
        (Window window, RevisionGridView view, RevisionGridViewModel viewModel, List<GitRevision> _, FakeHost host) = Show();
        StackPanel status = view.GetVisualDescendants().OfType<StackPanel>().First(p => p.Name == "buildStatus" && p.DataContext is RevisionGridRow { BuildStatus.Url: "https://ci.example.com/7" });
        Point center = status.TranslatePoint(new Point(4, status.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(center, MouseButton.Left, RawInputModifiers.None);
        host.OpenedUrls.Should().Equal("https://ci.example.com/7");

        viewModel.Hotkeys = [new HotkeyBinding((int)RevisionGridCommand.GoToParent, HotkeyBinding.Control | 'P'), new HotkeyBinding((int)RevisionGridCommand.ShowStashes, HotkeyBinding.Control | 'S')];
        List<RevisionGridCommand> handled = [];
        viewModel.CommandHandler = command =>
        {
            handled.Add(command);
            return true;
        };
        viewModel.SelectedRow = viewModel.Rows.Single(r => r.Subject == "Update the documentation");
        view.ProcessHotkey(HotkeyBinding.Control | 'P').Should().BeTrue();
        viewModel.SelectedRow!.Subject.Should().Be("Merge branch 'feature'");
        view.ProcessHotkey(HotkeyBinding.Control | 'S').Should().BeTrue();
        handled.Should().Equal(RevisionGridCommand.ShowStashes);
        view.ProcessHotkey(HotkeyBinding.Control | 'Q').Should().BeFalse("not a hotkey of the grid");
        window.Close();
    });

    private static (Window Window, RevisionGridView View, RevisionGridViewModel ViewModel, List<GitRevision> History, FakeHost Host) Show()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        foreach (GitRevision revision in history)
        {
            revision.AuthorEmail = $"{revision.Author!.ToLowerInvariant()}@example.com";
        }

        // A stash of the head commit (its reflog selector), notes and build statuses.
        GitRevision stash = new(ObjectId.Random())
        {
            Subject = "WIP on main: Update the documentation",
            Author = "Alice",
            AuthorEmail = "alice@example.com",
            ReflogSelector = "refs/stash@{0}",
            ParentIds = [history[0].ObjectId],
            AuthorUnixTime = history[0].AuthorUnixTime,
            CommitUnixTime = history[0].CommitUnixTime,
        };
        history.Insert(0, stash);
        history[1].Notes = "Reviewed-by: Bob\nTested on Windows";
        history[3].Notes = "Needs a changelog entry";
        history[1].BuildStatus = new BuildInfo { Status = BuildStatus.Success, Description = "#9 succeeded", Url = "https://ci.example.com/9" };
        history[4].BuildStatus = new BuildInfo { Status = BuildStatus.Failure, Description = "#7 failed", Url = "https://ci.example.com/7" };
        history[5].BuildStatus = new BuildInfo { Status = BuildStatus.Unstable, Description = "#6 unstable" };

        FakeHost host = new(history);
        RevisionGridViewModel viewModel = new(host, new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false))
        {
            MultiSelect = true,
            ShowNotesColumn = true,
            ShowAvatarColumn = true,
            ShowBuildStatusColumn = true,
            ShowBuildStatusText = true,
            DrawNonRelativesGray = true,
            DrawNonRelativesTextGray = true,
            HighlightAuthoredRevisions = true,
        };
        RevisionGridView view = new() { DataContext = viewModel };
        Window window = new() { Width = 1000, Height = 280, Content = view };
        window.Show();
        viewModel.Load();
        Dispatcher.UIThread.RunJobs();
        return (window, view, viewModel, history, host);
    }

    internal sealed class FakeHost(IReadOnlyList<GitRevision> revisions) : IRevisionGridHost
    {
        private static readonly Dictionary<string, Color> _colors = new() { ["Alice"] = Colors.SteelBlue, ["Bob"] = Colors.DarkOrange, ["Carol"] = Colors.SeaGreen };

        public string CurrentBranch => "main";

        public string UserEmail => "carol@example.com";

        public List<string> OpenedUrls { get; } = [];

        public List<(IGitRef? Ref, int Row, int First, int Count)> HoverRequests { get; } = [];

        public IReadOnlySet<ObjectId>? HoverResult { get; set; }

        public bool MatchesQuickSearch(GitRevision revision, string criteria) => false;

        public void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken)
        {
            graph.HeadId = revisions[1].ObjectId;
            foreach (GitRevision revision in revisions)
            {
                graph.Add(revision);
            }

            reportBatch();
            completed(null);
        }

        public void RunInBackground(Action work, Action then)
        {
            work();
            then();
        }

        public void OpenUrl(string url) => OpenedUrls.Add(url);

        public Task<byte[]?> GetAvatarAsync(string email, string? name, int size) => Task.FromResult<byte[]?>(CreateAvatar(_colors.GetValueOrDefault(name ?? "", Colors.Gray), size));

        public Task<IReadOnlySet<ObjectId>?> GetHoverHighlightAsync(RevisionGraph graph, IGitRef? gitRef, int rowIndex, int firstVisibleRow, int visibleRowCount)
        {
            HoverRequests.Add((gitRef, rowIndex, firstVisibleRow, visibleRowCount));
            return Task.FromResult(HoverResult);
        }

        // A plain square of the author's color, as PNG.
        private static byte[] CreateAvatar(Color color, int size)
        {
            using WriteableBitmap bitmap = new(new PixelSize(size, size), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (ILockedFramebuffer buffer = bitmap.Lock())
            {
                byte[] pixels = new byte[buffer.RowBytes * size];
                for (int i = 0; i + 3 < pixels.Length; i += 4)
                {
                    pixels[i] = color.B;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.R;
                    pixels[i + 3] = 255;
                }

                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, buffer.Address, pixels.Length);
            }

            using MemoryStream stream = new();
            bitmap.Save(stream, new PngBitmapEncoderOptions());
            return stream.ToArray();
        }
    }
}
