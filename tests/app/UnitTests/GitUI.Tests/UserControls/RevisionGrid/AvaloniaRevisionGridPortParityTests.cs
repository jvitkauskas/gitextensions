using GitUI;
using GitUI.Hotkey;
using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUITests.UserControls.RevisionGrid;

/// <summary>The Avalonia revision grid keeps the hotkey commands of <c>RevisionGridControl</c> (docs/avalonia-port/PLAN.md, phase 4).</summary>
[TestFixture]
public sealed class AvaloniaRevisionGridPortParityTests
{
    [Test]
    public void The_hotkey_commands_keep_the_names_and_codes_of_the_WinForms_grid()
    {
        // The codes are stored in the hotkey settings ("RevisionGrid").
        HotkeyCommands.RevisionGrid[] winForms = Enum.GetValues<HotkeyCommands.RevisionGrid>();
        foreach (HotkeyCommands.RevisionGrid command in winForms)
        {
            Enum.Parse<RevisionGridCommand>(command.ToString()).Should().Be((RevisionGridCommand)(int)command, command.ToString());
        }

        Enum.GetValues<RevisionGridCommand>().Should().HaveCount(winForms.Length);
    }
}
