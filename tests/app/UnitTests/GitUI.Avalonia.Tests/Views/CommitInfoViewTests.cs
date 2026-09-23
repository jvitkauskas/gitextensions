using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls;
using GitUI.Presentation.UserControls;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless tests of the XHTML text and the commit info.</summary>
[TestFixture]
public sealed class CommitInfoViewTests : HeadlessTest
{
    private static readonly GitRevision Revision = new(ObjectId.Parse("c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3")) { Subject = "Fix the bug", Author = "Alice", AuthorEmail = "alice@example.org" };

    [Test]
    public Task Xhtml_text_shows_the_markup_and_raises_link_clicks() => OnUiThreadAsync(() =>
    {
        XhtmlTextBlock text = new() { Xhtml = "Author: <a href='mailto:alice@example.org'>Alice &lt;alice@example.org&gt;</a><br/><b>bold</b> <u>tag</u>: caf&#233; &amp; more" };

        text.Inlines!.OfType<Run>().Select(r => r.Text).Should().Equal("Author: ", "Alice <alice@example.org>", "\n", "bold", " ", "tag", ": café & more");
        text.Inlines!.OfType<Run>().Single(r => r.Text == "bold").FontWeight.Should().Be(FontWeight.Bold);
        text.Links.Should().Equal((8, 33, "mailto:alice@example.org"));

        string? clicked = null;
        text.LinkClicked += (_, e) => clicked = e.Uri;
        text.ClickLinkAt(4).Should().BeFalse("not on a link");
        text.ClickLinkAt(10).Should().BeTrue();
        clicked.Should().Be("mailto:alice@example.org");
    });

    [Test]
    public Task Malformed_xhtml_is_shown_as_plain_text() => OnUiThreadAsync(() =>
    {
        XhtmlTextBlock text = new() { Xhtml = "a <b>b & c" };

        text.Inlines!.OfType<Run>().Select(r => r.Text).Should().Equal("a b & c");
        text.Links.Should().BeEmpty();
    });

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        FakeHost host = new();
        CommitInfoViewModel viewModel = new(host) { ShowBranchesAsLinks = true };
        viewModel.CommandClicked += (_, _) => { };
        CommitInfoView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 520, Height = 360 };
        window.Show();
        viewModel.SetRevision(Revision);
        Dispatcher.UIThread.RunJobs();

        view.GetLogicalDescendants().OfType<XhtmlTextBlock>().Should().Contain(t => t.Links.Any(l => l.Uri == "gitext://gotobranch/master"));
        SaveScreenshot(window.CaptureRenderedFrame(), $"commit-info-{theme}");
        window.Close();
    });

    [Test]
    public Task Links_of_the_view_reach_the_view_model() => OnUiThreadAsync(() =>
    {
        FakeHost host = new();
        CommitInfoViewModel viewModel = new(host);
        CommitInfoView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 400, Height = 300 };
        window.Show();
        viewModel.SetRevision(Revision);
        Dispatcher.UIThread.RunJobs();

        XhtmlTextBlock message = view.FindControl<XhtmlTextBlock>("message")!;
        message.ClickLinkAt(message.Links[0].Start);

        host.Executed.Should().Equal("https://example.org/issues/42");
        window.Close();
    });

    [Test]
    public Task The_menu_copies_the_link_and_toggles_the_settings() => OnUiThreadAsync(() =>
    {
        FakeHost host = new() { ShowAvatar = true };
        CommitInfoViewModel viewModel = new(host);
        CommitInfoView view = new() { DataContext = viewModel };
        Window window = new() { Content = view, Width = 400, Height = 300 };
        window.Show();
        viewModel.SetRevision(Revision);
        Dispatcher.UIThread.RunJobs();

        view.FindControl<Image>("avatar")!.Source.Should().NotBeNull();

        view.OpenMenuFor("https://example.org/issues/42");
        MenuItem copyLink = view.Menu.Items.OfType<MenuItem>().First();
        copyLink.IsVisible.Should().BeTrue();
        copyLink.Header.Should().Be("Copy _link (https://example.org/issues/42)");
        copyLink.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.Copied.Should().Equal("https://example.org/issues/42");

        MenuItem tags = view.Menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Show tags containing this commit"));
        tags.IsChecked.Should().BeTrue();
        tags.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        host.Options.ShowContainedInTags.Should().BeFalse();

        view.OpenMenuFor(null);
        copyLink.IsVisible.Should().BeFalse();
        window.Close();
    });
    internal sealed class FakeHost : ICommitInfoHost
    {
        public List<string> Executed { get; } = [];

        public List<string> Copied { get; } = [];

        public List<ObjectId> EditedNotes { get; } = [];

        public CommitInfoDisplayOptions Options { get; set; } = new();

        public CommitInfoStrings Strings { get; } = new();

        public bool ShowAvatar { get; set; }

        public int AvatarSize => 80;

        // A 1x1 PNG.
        public static byte[] AvatarImage { get; } = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        public Task<byte[]?> GetAvatarAsync(string? email, string? name, CancellationToken cancellationToken) => Task.FromResult<byte[]?>(AvatarImage);

        public void EditNotes(ObjectId objectId) => EditedNotes.Add(objectId);

        public string GetCopyText(string header, string message) => $"{header}\n\n{message}";

        public void CopyToClipboard(string text) => Copied.Add(text);

        public CommitInfoContent Render(GitRevision revision, IReadOnlyList<ObjectId>? children, bool showRevisionsAsLinks)
            => new(
                [
                    new("Author:", "<a href='mailto:alice@example.org'>Alice &lt;alice@example.org&gt;</a>"),
                    new("Date:", "2 days ago (9/21/2026 10:00:00 AM)"),
                    new("Commit hash:", revision.ObjectId.ToString()),
                    new("Parent:", showRevisionsAsLinks ? "<a href='gitext://gotocommit/a1a1a1a1'>a1a1a1a1</a>" : "a1a1a1a1"),
                ],
                "Fix the bug");

        public Task<string> LoadMessageAsync(GitRevision revision, IReadOnlyList<ObjectId>? children, bool showRevisionsAsLinks, CancellationToken cancellationToken)
            => Task.FromResult("Fix the bug\n\nSee <a href='https://example.org/issues/42'>#42</a> for the details.");

        public Task<string> LoadRevisionInfoAsync(GitRevision revision, bool showBranchesAsLinks, IReadOnlySet<string> showAll, CancellationToken cancellationToken)
            => Task.FromResult($"Contained in branches: <a href='gitext://gotobranch/master'>master</a>{(showAll.Contains("branches") ? ", feature" : "")}\n\nDerives from tag: v1.0 + 3 commits");

        public void ExecuteLink(string uri, Action<string, string?>? internalCommand, Action<string?> showAll)
        {
            Executed.Add(uri);
            if (uri == "gitext://showall/branches")
            {
                showAll("branches");
            }
            else if (uri.StartsWith("gitext://") && internalCommand is not null)
            {
                internalCommand(uri["gitext://".Length..].Split('/')[0], uri.Split('/')[^1]);
            }
        }
    }
}
