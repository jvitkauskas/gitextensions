using GitUI.CommandsDialogs.SettingsDialog;

namespace GitUITests.CommandsDialogs.SettingsDialog;

/// <summary>The editors offered off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).</summary>
public sealed class EditorHelperTests
{
    [Test]
    public void On_Linux_the_editors_on_the_PATH_are_offered_with_the_options_that_make_them_wait()
    {
        HashSet<string> onPath = ["code", "gedit", "kate"];

        EditorHelper.GetUnixEditors(isMacOS: false, onPath.Contains)
            .Should().Equal("vi", "code --new-window --wait", "gedit --standalone", "kate --block");
    }

    [Test]
    public void On_macOS_TextEdit_is_offered_too()
    {
        EditorHelper.GetUnixEditors(isMacOS: true, name => name == "subl")
            .Should().Equal("vi", "subl --new-window --wait", "open -W -n -e");
    }
}
