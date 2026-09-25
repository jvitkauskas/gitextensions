using GitCommands;
using GitUI.Hotkey;
using GitUI.ScriptsEngine;
using NSubstitute;
using ResourceManager;
using ResourceManager.Hotkey;

namespace GitUITests.Hotkey;
public class HotkeySettingsManagerTests
{
    private HotkeySettingsManager _settingsManager = null!;

    [SetUp]
    public void SetUp()
    {
        IScriptsManager scriptsManager = Substitute.For<IScriptsManager>();
        scriptsManager.GetScripts().Returns([]);

        _settingsManager = new(scriptsManager);
    }

    // Keys that macOS takes (Control is Cmd there) get the default of macOS of their command, if it is free in the window.
    [Test]
    public void A_saved_key_that_macOS_takes_gets_the_default_of_macOS()
    {
        HotkeySettings[] saved =
        [
            new HotkeySettings("Browse",
                new HotkeyCommand(7, "Commit") { KeyData = Keys.Control | Keys.Space },
                new HotkeyCommand(31, "FocusNextTab") { KeyData = Keys.Control | Keys.Tab },
                new HotkeyCommand(8, "Other") { KeyData = Keys.Control | Keys.Shift | Keys.OemCloseBrackets },
                new HotkeyCommand(9, "Kept") { KeyData = Keys.Control | Keys.K }),
        ];
        HotkeySettings[] macOSDefaults =
        [
            new HotkeySettings("Browse",
                new HotkeyCommand(7, "Commit") { KeyData = Keys.Control | Keys.Return },
                new HotkeyCommand(31, "FocusNextTab") { KeyData = Keys.Control | Keys.Shift | Keys.OemCloseBrackets },
                new HotkeyCommand(9, "Kept") { KeyData = Keys.Control | Keys.J }),
        ];

        HotkeySettingsManager.ReplaceKeysTakenByMacOS(saved, macOSDefaults);

        saved[0].Commands![0].KeyData.Should().Be(Keys.Control | Keys.Return);
        saved[0].Commands![1].KeyData.Should().Be(Keys.Control | Keys.Tab, "the default of macOS is used by another command");
        saved[0].Commands![3].KeyData.Should().Be(Keys.Control | Keys.K, "a key that macOS does not take is kept");
    }

    [Test]
    public void The_defaults_of_macOS_use_no_key_that_macOS_takes()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Assert.Ignore("The defaults of macOS");
        }

        _settingsManager.CreateDefaultSettings().SelectMany(setting => setting.Commands!).Select(command => command.KeyData)
            .Should().NotContain(key => HotkeySettingsManager.TakenByMacOS.Contains(key));
    }

    [Test]
    public void MergeEqualSettings()
    {
        // arrange

        HotkeySettings[] defaultHotkeySettingsArray = CreateHotkeySettings(2);
        HotkeySettings[] loadedHotkeySettingsArray = CreateHotkeySettings(2);

        HotkeySettingsManager.MergeIntoDefaultSettings(defaultHotkeySettingsArray, loadedHotkeySettingsArray);
        HotkeySettings[] expected = CreateHotkeySettings(2);

        defaultHotkeySettingsArray.SequenceEqual(expected).Should().BeTrue();
    }

    public void SequenceEqualOnDifferentSettings()
    {
        HotkeySettings[] defaultHotkeySettingsArray = CreateHotkeySettings(2);
        HotkeySettings[] loadedHotkeySettingsArray = CreateHotkeySettings(2);
        loadedHotkeySettingsArray[0].Commands![0].KeyData = Keys.C;

        defaultHotkeySettingsArray.SequenceEqual(loadedHotkeySettingsArray).Should().BeFalse();
    }

    [Test]
    public void SequenceEqualOnEqualSettings()
    {
        HotkeySettings[] defaultHotkeySettingsArray = CreateHotkeySettings(2);
        HotkeySettings[] loadedHotkeySettingsArray = CreateHotkeySettings(2);

        defaultHotkeySettingsArray.SequenceEqual(loadedHotkeySettingsArray).Should().BeTrue();
    }

    [Test]
    public void MergeLoadedSettings()
    {
        // arrange

        HotkeySettings[] defaultHotkeySettingsArray = CreateHotkeySettings(2);
        HotkeySettings[] loadedHotkeySettingsArray = CreateHotkeySettings(2);
        loadedHotkeySettingsArray[0].Commands![0].KeyData = Keys.C;

        HotkeySettingsManager.MergeIntoDefaultSettings(defaultHotkeySettingsArray, loadedHotkeySettingsArray);

        defaultHotkeySettingsArray.SequenceEqual(loadedHotkeySettingsArray).Should().BeTrue();
    }

    [Test]
    public void MergeLoadedDiffSizeSettings()
    {
        // arrange

        HotkeySettings[] defaultHotkeySettingsArray = CreateHotkeySettings(3);
        HotkeySettings[] loadedHotkeySettingsArray = CreateHotkeySettings(2);
        loadedHotkeySettingsArray[1].Commands![1].KeyData = Keys.C;

        HotkeySettingsManager.MergeIntoDefaultSettings(defaultHotkeySettingsArray, loadedHotkeySettingsArray);
        HotkeySettings[] expected = CreateHotkeySettings(3);
        expected[1].Commands![1].KeyData = loadedHotkeySettingsArray[1].Commands![1].KeyData;

        defaultHotkeySettingsArray.SequenceEqual(expected).Should().BeTrue();
    }

    [Test]
    public async Task Can_save_settings()
    {
        string? originalHotkeys = AppSettings.SerializedHotkeys;

        try
        {
            _settingsManager.SaveSettings(CreateHotkeySettings(2));

            // Verify as a string, as the xml verifier ignores line breaks.
            await Verifier.Verify(AppSettings.SerializedHotkeys);
        }
        finally
        {
            AppSettings.SerializedHotkeys = originalHotkeys!;
        }
    }

    private static HotkeySettings[] CreateHotkeySettings(int count)
    {
        return [.. Enumerable.Range(1, count).Select(i =>
            new HotkeySettings("settings" + i,
                new HotkeyCommand(1, "C1") { KeyData = Keys.A },
                new HotkeyCommand(2, "C2") { KeyData = Keys.B }))];
    }
}
