using System.Net;
using GitExtensions.Extensibility.Settings;

namespace GitExtensions.ExtensibilityTests;

/// <summary>The credentials of the plugins in the store of the system (docs/avalonia-port/CROSS-PLATFORM.md, phase 4).</summary>
[NonParallelizable]
public sealed class CredentialStoreTests
{
    private ICredentialStore _systemStore = null!;

    [SetUp]
    public void SetUp() => _systemStore = CredentialStores.Current;

    [TearDown]
    public void TearDown() => CredentialStores.Current = _systemStore;

    [Test]
    public void The_credentials_are_saved_in_the_store_and_read_from_it()
    {
        FakeStore store = new(isAvailable: true);
        CredentialStores.Current = store;
        CredentialsSetting setting = new($"Credentials{Guid.NewGuid():N}", "Credentials", () => "/repo");
        GlobalSource settings = new();

        setting.SaveValue(settings, "user", "secret");
        setting.Save();

        store.Items.Should().ContainSingle().Which.Value.Should().BeEquivalentTo(new NetworkCredential("user", "secret"));
        setting.GetValueOrDefault(settings).Password.Should().Be("secret");
        setting.IsPersistent.Should().BeTrue();

        // Without a user name the credentials are removed.
        setting.SaveValue(settings, "", "");
        setting.Save();
        store.Items.Should().BeEmpty();
    }

    [Test]
    public void Without_a_store_the_credentials_are_kept_for_the_session_only()
    {
        FakeStore store = new(isAvailable: false);
        CredentialStores.Current = store;
        CredentialsSetting setting = new($"Credentials{Guid.NewGuid():N}", "Credentials", () => "/repo");
        GlobalSource settings = new();

        setting.SaveValue(settings, "user", "secret");
        setting.Save();

        store.Items.Should().BeEmpty("nothing is written in clear");
        setting.GetValueOrDefault(settings).Password.Should().Be("secret");
        setting.IsPersistent.Should().BeFalse();
    }

    /// <summary>A round trip with the Secret Service of the desktop: opt in with GE_TEST_SECRET_SERVICE=1 (it writes an item and removes it).</summary>
    [Test]
    [Platform(Include = "Linux")]
    public void The_Secret_Service_keeps_the_credentials()
    {
        Assume.That(Environment.GetEnvironmentVariable("GE_TEST_SECRET_SERVICE"), Is.EqualTo("1"), "opt in: it writes to the keyring of the user");
        Assume.That(_systemStore.IsAvailable, "a secret service runs");

        string target = $"GitExtensionsTests_{Guid.NewGuid():N}";
        try
        {
            _systemStore.Save(target, new NetworkCredential("user", "pass\nword")).Should().BeTrue();
            NetworkCredential? read = _systemStore.Get(target);
            read!.UserName.Should().Be("user");
            read.Password.Should().Be("pass\nword");
        }
        finally
        {
            _systemStore.Remove(target).Should().BeTrue();
        }

        _systemStore.Get(target).Should().BeNull();
    }

    /// <summary>A round trip with the login Keychain of macOS: opt in with GE_TEST_KEYCHAIN=1 (it writes an item and removes it).</summary>
    [Test]
    [Platform(Include = "MacOsX")]
    public void The_Keychain_keeps_the_credentials()
    {
        Assume.That(Environment.GetEnvironmentVariable("GE_TEST_KEYCHAIN"), Is.EqualTo("1"), "opt in: it writes to the Keychain of the user");
        _systemStore.IsAvailable.Should().BeTrue();

        string target = $"GitExtensionsTests_{Guid.NewGuid():N}";
        try
        {
            _systemStore.Save(target, new NetworkCredential("user", "pass\nwörd")).Should().BeTrue();
            NetworkCredential? read = _systemStore.Get(target);
            read!.UserName.Should().Be("user");
            read.Password.Should().Be("pass\nwörd");

            // Saved again, the item is updated (the user name too).
            _systemStore.Save(target, new NetworkCredential("other", "secret")).Should().BeTrue();
            read = _systemStore.Get(target);
            read!.UserName.Should().Be("other");
            read.Password.Should().Be("secret");
        }
        finally
        {
            _systemStore.Remove(target).Should().BeTrue();
        }

        _systemStore.Get(target).Should().BeNull();
        _systemStore.Remove(target).Should().BeFalse();
    }

    private sealed class GlobalSource : SettingsSource
    {
        public override SettingLevel SettingLevel { get; init; } = SettingLevel.Global;

        public override string? GetValue(string name) => null;

        public override void SetValue(string name, string? value)
        {
        }
    }

    private sealed class FakeStore(bool isAvailable) : ICredentialStore
    {
        public Dictionary<string, NetworkCredential> Items { get; } = [];

        public bool IsAvailable => isAvailable;

        public NetworkCredential? Get(string target) => Items.GetValueOrDefault(target);

        public bool Save(string target, NetworkCredential credential)
        {
            Items[target] = credential;
            return true;
        }

        public bool Remove(string target) => Items.Remove(target);
    }
}
