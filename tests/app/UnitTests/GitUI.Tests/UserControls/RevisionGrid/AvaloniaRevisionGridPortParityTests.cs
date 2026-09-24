using GitUI;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUITests.UserControls.RevisionGrid;

/// <summary>The Avalonia revision grid keeps the hotkey commands of <see cref="RevisionGridControl"/> (docs/avalonia-port/PLAN.md, phase 4).</summary>
[TestFixture]
public sealed class AvaloniaRevisionGridPortParityTests
{
    [Test]
    public void The_hotkey_commands_keep_the_names_and_codes_of_the_WinForms_grid()
    {
        // The codes are stored in the hotkey settings ("RevisionGrid").
        RevisionGridControl.Command[] winForms = Enum.GetValues<RevisionGridControl.Command>();
        foreach (RevisionGridControl.Command command in winForms)
        {
            Enum.Parse<RevisionGridCommand>(command.ToString()).Should().Be((RevisionGridCommand)(int)command, command.ToString());
        }

        Enum.GetValues<RevisionGridCommand>().Should().HaveCount(winForms.Length);
    }
}
