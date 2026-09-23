using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 3: the verify database dialog, recovering a lost commit of a real repository.</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void Verify_recovers_a_lost_commit()
    {
        string lostCommit = _referenceRepository.CreateCommit("Lost work", "lost content");
        _referenceRepository.Module.GitExecutable.GetOutput("reset --hard HEAD~1");

        List<string> shown = [];
        System.Windows.Forms.Timer acknowledgeTags = new() { Interval = 200 };
        acknowledgeTags.Tick += (_, _) => CloseTopLevelWindow("Tags created", except: 0);
        acknowledgeTags.Start();
        try
        {
            DriveDialogs(
                window => WhenLoaded(window, viewModel =>
                {
                    shown.AddRange(viewModel.LostObjects.Select(o => $"{o.RawType} {o.Subject}"));
                    LostObjectItem lost = viewModel.LostObjects.Single(o => o.ObjectId.ToString() == lostCommit);
                    viewModel.SelectedObject = lost;
                    lost.IsSelected = true;
                    DispatcherTimer.RunOnce(
                        () =>
                        {
                            Capture(window, "verify");
                            viewModel.RestoreSelectedObjectsCommand.Execute(null);
                        },
                        TimeSpan.FromMilliseconds(300));
                }),
                AcknowledgeWhenDone, // fsck
                AcknowledgeWhenDone); // git tag

            _commands.StartVerifyDatabaseDialog(_owner).Should().BeTrue();
        }
        finally
        {
            acknowledgeTags.Dispose();
        }

        shown.Should().Equal("dangling commit Lost work");
        _referenceRepository.Module.GetRefs(RefsFilter.Tags).Should().ContainSingle(tag => tag.Name == "LOST_FOUND_1")
            .Which.ObjectId.ToString().Should().Be(lostCommit);
    }

    /// <summary>Runs the action once fsck (in the progress dialog, driven next) has listed the lost objects.</summary>
    private static void WhenLoaded(DialogWindow window, Action<VerifyViewModel> action)
    {
        VerifyViewModel viewModel = (VerifyViewModel)window.DataContext!;
        DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
        int ticks = 0;
        timer.Tick += (_, _) =>
        {
            if (viewModel.LostObjects.Count > 0 || ++ticks > 100)
            {
                timer.Stop();
                action(viewModel);
            }
        };
        timer.Start();
    }
}
