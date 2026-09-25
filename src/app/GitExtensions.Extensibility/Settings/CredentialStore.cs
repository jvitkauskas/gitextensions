using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitExtensions.Extensibility.Settings;

/// <summary>
///  The secure store of credentials of the system (docs/avalonia-port/CROSS-PLATFORM.md, phase 4): the Credential Manager
///  of Windows, the Secret Service of Linux (libsecret: GNOME Keyring, KWallet, KeePassXC). Elsewhere, or without a
///  secret service, <see cref="IsAvailable"/> is <see langword="false"/> and nothing is stored.
/// </summary>
public interface ICredentialStore
{
    /// <summary>Whether credentials can be stored (the store exists and answers).</summary>
    bool IsAvailable { get; }

    /// <summary>The credentials stored for <paramref name="target"/>, or <see langword="null"/>.</summary>
    NetworkCredential? Get(string target);

    /// <summary>Stores the credentials for <paramref name="target"/>; whether they were stored.</summary>
    bool Save(string target, NetworkCredential credential);

    /// <summary>Removes the credentials of <paramref name="target"/>; whether there were some.</summary>
    bool Remove(string target);
}

/// <summary>The credential store of the system.</summary>
public static class CredentialStores
{
    public static ICredentialStore Current { get; set; }
        = OperatingSystem.IsWindows() ? new WindowsCredentialStore()
            : OperatingSystem.IsLinux() ? new SecretServiceCredentialStore()
            : new NoCredentialStore();
}

/// <summary>No store: nothing is kept (macOS until its Keychain is supported).</summary>
internal sealed class NoCredentialStore : ICredentialStore
{
    public bool IsAvailable => false;

    public NetworkCredential? Get(string target) => null;

    public bool Save(string target, NetworkCredential credential) => false;

    public bool Remove(string target) => false;
}

/// <summary>The Credential Manager of Windows (AdysTech.CredentialManager), with the targets prefixed by <c>GitExtensions_</c>.</summary>
internal sealed class WindowsCredentialStore : ICredentialStore
{
    private const string TargetPrefix = "GitExtensions_";

    public bool IsAvailable => true;

    public NetworkCredential? Get(string target) => AdysTech.CredentialManager.CredentialManager.GetCredentials(GetTarget(target));

    public bool Save(string target, NetworkCredential credential)
        => AdysTech.CredentialManager.CredentialManager.SaveCredentials(GetTarget(target), credential) is not null;

    public bool Remove(string target)
        => AdysTech.CredentialManager.CredentialManager.GetCredentials(GetTarget(target)) is not null
            && AdysTech.CredentialManager.CredentialManager.RemoveCredentials(GetTarget(target));

    private static string GetTarget(string rawTarget)
        => string.IsNullOrWhiteSpace(rawTarget) ? throw new ArgumentNullException(nameof(rawTarget)) : $"{TargetPrefix}{rawTarget}";
}

/// <summary>
///  The Secret Service of freedesktop.org through libsecret: one item per target (attribute <c>target</c> of the schema
///  <c>org.gitextensions.Credentials</c>), whose secret is the user name and the password on two lines.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed partial class SecretServiceCredentialStore : ICredentialStore
{
    private const string LibSecret = "libsecret-1.so.0";
    private const string LibGLib = "libglib-2.0.so.0";
    private const string SchemaName = "org.gitextensions.Credentials";
    private const string TargetAttribute = "target";

    private static readonly Lazy<IntPtr> _schema = new(CreateSchema);
    private readonly Lazy<bool> _isAvailable;

    public SecretServiceCredentialStore()
    {
        _isAvailable = new(CheckAvailable);
    }

    public bool IsAvailable => _isAvailable.Value;

    private static bool CheckAvailable()
    {
        // Whether a secret service answers on the session bus, without unlocking anything (a lookup would ask to
        // unlock the keyring). Guarded by a timeout: D-Bus may start a service that does not answer.
        Task<bool> check = Task.Run(() =>
        {
            try
            {
                IntPtr service = secret_service_get_sync(0 /* SECRET_SERVICE_NONE */, IntPtr.Zero, out IntPtr error);
                if (service == IntPtr.Zero || !CheckError(error))
                {
                    return false;
                }

                g_object_unref(service);
                return true;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                Trace.WriteLine($"No libsecret: the credentials are not stored. {ex.Message}");
                return false;
            }
        });

#pragma warning disable VSTHRD002 // A native call with a timeout.
        return check.Wait(TimeSpan.FromSeconds(10)) && check.Result;
#pragma warning restore VSTHRD002
    }

    public NetworkCredential? Get(string target)
    {
        if (!IsAvailable || !TryLookup(target, out string? secret) || secret is null)
        {
            return null;
        }

        int newLine = secret.IndexOf('\n');
        return newLine < 0 ? new NetworkCredential(secret, "") : new NetworkCredential(secret[..newLine], secret[(newLine + 1)..]);
    }

    public bool Save(string target, NetworkCredential credential)
    {
        if (!IsAvailable)
        {
            return false;
        }

        return WithAttributes(target, attributes =>
        {
            int stored = secret_password_storev_sync(_schema.Value, attributes, collection: null, $"Git Extensions: {target}", $"{credential.UserName}\n{credential.Password}", IntPtr.Zero, out IntPtr error);
            return stored != 0 && CheckError(error);
        });
    }

    public bool Remove(string target)
    {
        if (!IsAvailable)
        {
            return false;
        }

        return WithAttributes(target, attributes =>
        {
            int removed = secret_password_clearv_sync(_schema.Value, attributes, IntPtr.Zero, out IntPtr error);
            return removed != 0 && CheckError(error);
        });
    }

    private static bool TryLookup(string target, [MaybeNullWhen(false)] out string? secret)
    {
        string? found = null;
        bool answered = WithAttributes(target, attributes =>
        {
            IntPtr password = secret_password_lookupv_sync(_schema.Value, attributes, IntPtr.Zero, out IntPtr error);
            if (!CheckError(error))
            {
                return false;
            }

            if (password != IntPtr.Zero)
            {
                found = Marshal.PtrToStringUTF8(password);
                secret_password_free(password);
            }

            return true;
        });
        secret = found;
        return answered;
    }

    /// <summary>Runs <paramref name="action"/> with a <c>GHashTable</c> of the attributes of <paramref name="target"/>.</summary>
    private static bool WithAttributes(string target, Func<IntPtr, bool> action)
    {
        IntPtr glib = NativeLibrary.Load(LibGLib);
        IntPtr table = g_hash_table_new(NativeLibrary.GetExport(glib, "g_str_hash"), NativeLibrary.GetExport(glib, "g_str_equal"));
        IntPtr key = Marshal.StringToCoTaskMemUTF8(TargetAttribute);
        IntPtr value = Marshal.StringToCoTaskMemUTF8(target);
        try
        {
            g_hash_table_insert(table, key, value);
            return action(table);
        }
        finally
        {
            g_hash_table_unref(table);
            Marshal.FreeCoTaskMem(key);
            Marshal.FreeCoTaskMem(value);
        }
    }

    /// <summary>Whether there was no error (a <c>GError</c> is traced and freed).</summary>
    private static bool CheckError(IntPtr error)
    {
        if (error == IntPtr.Zero)
        {
            return true;
        }

        // GError: GQuark domain, gint code, gchar* message.
        Trace.WriteLine($"Secret Service: {Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(error, 8))}");
        g_error_free(error);
        return false;
    }

    /// <summary>The <c>SecretSchema</c> of the credentials (kept for the life of the process).</summary>
    private static IntPtr CreateSchema()
    {
        SecretSchema schema = new()
        {
            Name = Marshal.StringToCoTaskMemUTF8(SchemaName),
            Flags = 0, // SECRET_SCHEMA_NONE
            Attributes = new SecretSchemaAttribute[32],
        };
        schema.Attributes[0] = new SecretSchemaAttribute { Name = Marshal.StringToCoTaskMemUTF8(TargetAttribute), Type = 0 }; // SECRET_SCHEMA_ATTRIBUTE_STRING
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<SecretSchema>());
        Marshal.StructureToPtr(schema, pointer, fDeleteOld: false);
        return pointer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecretSchemaAttribute
    {
        public IntPtr Name;
        public int Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecretSchema
    {
        public IntPtr Name;
        public int Flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public SecretSchemaAttribute[] Attributes;
        public int Reserved;
        public IntPtr Reserved1;
        public IntPtr Reserved2;
        public IntPtr Reserved3;
        public IntPtr Reserved4;
        public IntPtr Reserved5;
        public IntPtr Reserved6;
        public IntPtr Reserved7;
    }

    [LibraryImport(LibSecret, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int secret_password_storev_sync(IntPtr schema, IntPtr attributes, string? collection, string label, string password, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial IntPtr secret_service_get_sync(int flags, IntPtr cancellable, out IntPtr error);

    [LibraryImport("libgobject-2.0.so.0")]
    private static partial void g_object_unref(IntPtr instance);

    [LibraryImport(LibSecret)]
    private static partial IntPtr secret_password_lookupv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial int secret_password_clearv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial void secret_password_free(IntPtr password);

    [LibraryImport(LibGLib)]
    private static partial IntPtr g_hash_table_new(IntPtr hashFunc, IntPtr keyEqualFunc);

    [LibraryImport(LibGLib)]
    private static partial int g_hash_table_insert(IntPtr hashTable, IntPtr key, IntPtr value);

    [LibraryImport(LibGLib)]
    private static partial void g_hash_table_unref(IntPtr hashTable);

    [LibraryImport(LibGLib)]
    private static partial void g_error_free(IntPtr error);
}
