using System.Collections.Concurrent;
using System.Net;

namespace GitExtensions.Extensibility.Settings;

public interface ICredentialsManager
{
    void Save();
}

internal class CredentialsManager : ICredentialsManager
{
    private static ConcurrentDictionary<string, NetworkCredential?> Credentials { get; } = new ConcurrentDictionary<string, NetworkCredential?>();
    private readonly Func<string?>? _getWorkingDir;

    public CredentialsManager()
    {
    }

    protected internal CredentialsManager(Func<string?> getWorkingDir)
    {
        _getWorkingDir = getWorkingDir;
    }

    public void Save()
    {
        List<KeyValuePair<string, NetworkCredential?>> credentials = [.. Credentials];
        if (credentials.Count < 1)
        {
            return;
        }

        // Without a store of the system, the credentials are kept for the session only (never in clear on disk).
        ICredentialStore store = CredentialStores.Current;
        if (!store.IsAvailable)
        {
            return;
        }

        Credentials.Clear();

        foreach (KeyValuePair<string, NetworkCredential?> networkCredentials in credentials)
        {
            // Cleared credentials (no user name) are removed from the store.
            if (networkCredentials.Value is null)
            {
                store.Remove(networkCredentials.Key);
                continue;
            }

            UpdateCredentials(store, networkCredentials.Key, networkCredentials.Value.UserName, networkCredentials.Value.Password);
        }
    }

    protected internal NetworkCredential GetCredentialOrDefault(SettingLevel settingLevel, string name, NetworkCredential defaultValue)
    {
        string? targetName = GetWindowsCredentialsTarget(name, settingLevel);
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return defaultValue;
        }

        if (Credentials.TryGetValue(targetName, out NetworkCredential? result))
        {
            return result ?? defaultValue;
        }

        ICredentialStore store = CredentialStores.Current;
        if (store.IsAvailable && store.Get(targetName) is NetworkCredential stored)
        {
            return stored;
        }

        return defaultValue;
    }

    protected internal void SetCredentials(SettingLevel settingLevel, string name, NetworkCredential? value)
    {
        string? targetName = GetWindowsCredentialsTarget(name, settingLevel);
        ArgumentNullException.ThrowIfNull(targetName);
        Credentials.AddOrUpdate(targetName, value, (s, credential) => value);
    }

    private string? GetWindowsCredentialsTarget(string name, SettingLevel settingLevel)
    {
        if (settingLevel == SettingLevel.Global)
        {
            return $"{name}";
        }

        ArgumentNullException.ThrowIfNull(_getWorkingDir);
        string? suffix = _getWorkingDir();
        return string.IsNullOrWhiteSpace(suffix) ? null : $"{name}_{suffix}";
    }

    /// <summary>Stores the credentials (without a user name: removes them).</summary>
    private static bool UpdateCredentials(ICredentialStore store, string target, string userName, string password)
        => string.IsNullOrWhiteSpace(userName)
            ? store.Remove(target)
            : store.Save(target, new NetworkCredential(userName.Trim(), password));
}
