using GitUI.Editor;
using GitUI.Hotkey;
using GitUI.Presentation.Editor;

namespace GitUITests.Editor;

/// <summary>
///  Compares the hotkey commands of the Avalonia file viewer (<see cref="FileViewerHotkeyCommand"/>) with
///  <c>HotkeyCommands.FileViewer</c>, so that an upstream change fails here until it is ported (docs/avalonia-port/ledger.md).
/// </summary>
[TestFixture]
public sealed class AvaloniaFileViewerPortParityTests
{
    [Test]
    public void The_hotkey_commands_match_the_commands_of_the_winforms_viewer()
    {
        Enum.GetValues<FileViewerHotkeyCommand>().ToDictionary(c => c.ToString(), c => (int)c)
            .Should().Equal(Enum.GetValues<HotkeyCommands.FileViewer>().ToDictionary(c => c.ToString(), c => (int)c));
    }
}
