using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using static GitUI.AvaloniaTests.ViewModels.ProcessViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the verify database dialog (port of <c>FormVerify</c>).</summary>
[TestFixture]
public sealed class VerifyViewModelTests
{
    internal static readonly ObjectId CommitId = ObjectId.Parse("1111111111111111111111111111111111111111");
    internal static readonly ObjectId OlderCommitId = ObjectId.Parse("2222222222222222222222222222222222222222");
    internal static readonly ObjectId BlobId = ObjectId.Parse("3333333333333333333333333333333333333333");
    internal static readonly ObjectId TagId = ObjectId.Parse("4444444444444444444444444444444444444444");

    [Test]
    public void Shows_the_commits_and_tags_the_most_recent_first()
    {
        FakeHost host = new();
        VerifyViewModel viewModel = Create(host);

        viewModel.Initialize();

        host.Log.Should().Equal("fsck:  --no-reflogs");
        viewModel.LostObjects.Select(o => o.ObjectId).Should().Equal(TagId, CommitId, OlderCommitId);
        viewModel.CanCreateRef.Should().BeFalse("nothing is selected");
    }

    [Test]
    public void Options_run_fsck_again()
    {
        FakeHost host = new();
        VerifyViewModel viewModel = Create(host);
        viewModel.Initialize();

        viewModel.Unreachable = true;
        viewModel.FullCheck = true;
        viewModel.NoReflogs = false;

        host.Log.Should().Equal("fsck:  --no-reflogs", "fsck:  --unreachable --no-reflogs", "fsck:  --unreachable --full --no-reflogs", "fsck:  --unreachable --full");
    }

    [Test]
    public void Other_objects_are_shown_with_their_guessed_type_and_one_kind_is_always_shown()
    {
        FakeHost host = new();
        VerifyViewModel viewModel = Create(host);
        viewModel.Initialize();

        viewModel.ShowOtherObjects = true;
        viewModel.LostObjects.Should().HaveCount(4);
        viewModel.LostObjects.Single(o => o.Kind == LostObjectKind.Blob).RawType.Should().Be("dangling blob (seemingly: json)");

        viewModel.ShowCommitsAndTags = false;
        viewModel.LostObjects.Select(o => o.Kind).Should().Equal(LostObjectKind.Blob);

        viewModel.ShowOtherObjects = false;
        viewModel.ShowCommitsAndTags.Should().BeTrue("one kind of objects is always shown");
    }

    [Test]
    public void Previews_a_commit_as_a_patch_and_a_blob_with_its_guessed_file_type()
    {
        FakeHost host = new();
        VerifyViewModel viewModel = Create(host);
        viewModel.Initialize();
        viewModel.ShowOtherObjects = true;

        viewModel.SelectedObject = viewModel.LostObjects.Single(o => o.ObjectId == CommitId);
        viewModel.Preview.DiffLines.Should().NotBeNull("a commit is shown as a patch");
        viewModel.CanCreateRef.Should().BeTrue();
        viewModel.CanSaveAs.Should().BeFalse();

        viewModel.SelectedObject = viewModel.LostObjects.Single(o => o.ObjectId == BlobId);
        viewModel.Preview.DiffLines.Should().BeNull();
        viewModel.Preview.FileName.Should().Be($"LOST_FOUND_{BlobId}.json");
        viewModel.CanSaveAs.Should().BeTrue();

        viewModel.ViewCommand.Execute(null);
        viewModel.SaveAsCommand.Execute(null);
        host.Log.Should().EndWith([$"view: LOST_FOUND_{BlobId}.json", $"save as: LOST_FOUND_{BlobId}.json|json Files (*.json)|*.json| All Files (*.*)|*.*"]);
    }

    [Test]
    public void Recovering_selected_objects_replaces_the_lost_and_found_tags()
    {
        FakeHost host = new() { TagNames = ["LOST_FOUND_old", "v1.0"] };
        FakeMessageBoxes messageBoxes = new();
        VerifyViewModel viewModel = Create(host, messageBoxes);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.Initialize();

        viewModel.RestoreSelectedObjectsCommand.Execute(null);
        messageBoxes.Warnings.Should().Equal("Select objects to restore.");
        host.Log.Should().EndWith("delete tag: LOST_FOUND_old", "the old tags are deleted first, as FormVerify");

        viewModel.LostObjects.Single(o => o.ObjectId == TagId).IsSelected = true;
        viewModel.LostObjects.Single(o => o.ObjectId == CommitId).IsSelected = true;
        host.Log.Clear();
        viewModel.RestoreSelectedObjectsCommand.Execute(null);

        host.Log.Should().Equal($"tag: LOST_FOUND_release {TagId}", $"tag: LOST_FOUND_2 {CommitId}", "fsck:  --no-reflogs");
        messageBoxes.Informations.Should().ContainSingle().Which.Should().StartWith("2 Tags created.");
        closed.Should().BeNull("not all objects are recovered");

        viewModel.SelectAll(true);
        viewModel.RestoreSelectedObjectsCommand.Execute(null);
        closed.Should().BeTrue("all objects are recovered");
    }

    [Test]
    public void Removing_dangling_objects_asks_first()
    {
        FakeHost host = new();
        FakeMessageBoxes messageBoxes = new() { ConfirmResult = false };
        VerifyViewModel viewModel = Create(host, messageBoxes);

        viewModel.RemoveCommand.Execute(null);
        host.Log.Should().BeEmpty();

        messageBoxes.ConfirmResult = true;
        viewModel.RemoveCommand.Execute(null);
        host.Log.Should().Equal("prune", "fsck:  --no-reflogs");
    }

    [Test]
    public void Closes_if_fsck_is_aborted_and_copies_hashes()
    {
        FakeHost host = new() { Aborted = true };
        VerifyViewModel viewModel = Create(host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;

        viewModel.Initialize();
        closed.Should().BeFalse();

        host.Aborted = false;
        viewModel.Initialize();
        viewModel.SelectedObject = viewModel.LostObjects.Single(o => o.ObjectId == CommitId);
        viewModel.CopyHashCommand.Execute(null);
        viewModel.CopyParentHashCommand.Execute(null);
        host.Clipboard.Should().Equal(CommitId.ToString(), OlderCommitId.ToString());
    }

    [TestCase("{\"a\": 1}", "json")]
    [TestCase("using System;", "cs")]
    [TestCase("<?xml version=\"1.0\"?>", "xml")]
    [TestCase("plain", "txt")]
    public void Guesses_the_file_type_from_the_content(string content, string type)
        => VerifyViewModel.GuessFileTypeWithContent(content).Should().Be(type);

    private static VerifyViewModel Create(FakeHost host, FakeMessageBoxes? messageBoxes = null)
        => new(new VerifyStrings(), host, messageBoxes ?? new FakeMessageBoxes());

    internal sealed class FakeHost : IVerifyHost
    {
        public bool Aborted { get; set; }

        public List<string> TagNames { get; init; } = [];

        public List<string> Log { get; } = [];

        public List<string> Clipboard { get; } = [];

        public IReadOnlyList<LostObjectItem>? ReadLostObjects(string options)
        {
            Log.Add($"fsck: {options}");
            if (Aborted)
            {
                return null;
            }

            DateTime now = new(2026, 9, 1, 12, 0, 0);
            return
            [
                new(LostObjectKind.Commit, OlderCommitId, "dangling commit") { Subject = "Older", Author = "Alice", Date = now.AddDays(-2) },
                new(LostObjectKind.Blob, BlobId, "dangling blob"),
                new(LostObjectKind.Commit, CommitId, "dangling commit") { Subject = "Lost work", Author = "Bob", Date = now.AddDays(-1), Parent = OlderCommitId },
                new(LostObjectKind.Tag, TagId, "dangling tag") { Subject = "Release", Author = "Carol", Date = now, TagName = "release" },
            ];
        }

        public void SaveLostObjects(string options) => Log.Add($"save: {options}");

        public void Prune() => Log.Add("prune");

        public string GetContent(LostObjectItem item) => item.Kind switch
        {
            LostObjectKind.Blob => "{\"key\": \"value\"}",
            _ => "commit 1111\nAuthor: Bob\n\ndiff --git a/a.txt b/a.txt\n@@ -1 +1 @@\n-a\n+b\n",
        };

        public bool ShowCreateTag(ObjectId objectId) => true;

        public bool ShowCreateBranch(ObjectId objectId) => true;

        public void CreateTag(string tagName, ObjectId objectId) => Log.Add($"tag: {tagName} {objectId}");

        public IEnumerable<string> GetTagNames() => TagNames;

        public void DeleteTag(string tagName)
        {
            TagNames.Remove(tagName);
            Log.Add($"delete tag: {tagName}");
        }

        public void CopyToClipboard(string text) => Clipboard.Add(text);

        public void SaveBlobAs(ObjectId objectId, string fileName, string filter, string extension) => Log.Add($"save as: {fileName}|{filter}");

        public void View(string text, string fileName) => Log.Add($"view: {fileName}");

        public void RunInBackground(Action work, Action then)
        {
            work();
            then();
        }
    }
}
