using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitExtensions.Extensibility;

/// <summary>The Win32 functions of the native message box and task dialog.</summary>
[SupportedOSPlatform("windows")]
internal static class DialogNativeMethods
{
    [DllImport("user32.dll")]
    public static extern nint GetActiveWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBoxW(nint hWnd, string text, string caption, int type);

    [DllImport("comctl32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int TaskDialogIndirect(ref TaskDialogConfig config, out int button, out int radioButton, [MarshalAs(UnmanagedType.Bool)] out bool verificationFlagChecked);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern nint SendMessageW(nint window, int message, nint wordParameter, nint longParameter);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateActCtxW(ref ActivationContext context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ActivateActCtx(nint activationContext, out nint cookie);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeactivateActCtx(int flags, nint cookie);

    public delegate int TaskDialogCallback(nint hwnd, int notification, nint wordParameter, nint longParameter, nint referenceData);

    /// <summary>The <c>TASKDIALOGCONFIG</c> structure (packed, as in <c>commctrl.h</c>).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TaskDialogConfig
    {
        public uint Size;
        public nint Parent;
        public nint Instance;
        public int Flags;
        public int CommonButtons;
        public nint WindowTitle;
        public nint MainIcon;
        public nint MainInstruction;
        public nint Content;
        public uint ButtonCount;
        public nint Buttons;
        public int DefaultButton;
        public uint RadioButtonCount;
        public nint RadioButtons;
        public int DefaultRadioButton;
        public nint VerificationText;
        public nint ExpandedInformation;
        public nint ExpandedControlText;
        public nint CollapsedControlText;
        public nint FooterIcon;
        public nint Footer;
        public nint Callback;
        public nint CallbackData;
        public uint Width;
    }

    /// <summary>The <c>TASKDIALOG_BUTTON</c> structure (packed, as in <c>commctrl.h</c>).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TaskDialogButtonData
    {
        public int Id;
        public nint Text;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ActivationContext
    {
        public int Size;
        public int Flags;
        public string Source;
        public ushort ProcessorArchitecture;
        public ushort LanguageId;
        public string? AssemblyDirectory;
        public string? ResourceName;
        public string? ApplicationName;
        public nint Module;
    }

    /// <summary>
    ///  Activates version 6 of the common controls (which has the task dialog, and themes the message box) for the
    ///  dialogs shown in its scope, as the WinForms <c>ThemingScope</c> does: the application manifest activates them for
    ///  Git Extensions, but not e.g. for the test hosts.
    /// </summary>
    public readonly struct ThemingScope : IDisposable
    {
        private const string Manifest = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
              <dependency>
                <dependentAssembly>
                  <assemblyIdentity type="win32" name="Microsoft.Windows.Common-Controls" version="6.0.0.0" processorArchitecture="*" publicKeyToken="6595b64144ccf1df" language="*" />
                </dependentAssembly>
              </dependency>
            </assembly>
            """;

        private static readonly Lazy<nint> _activationContext = new(CreateActivationContext);

        private readonly nint _cookie;

        public ThemingScope()
        {
            nint context = _activationContext.Value;
            if (context != -1 && !ActivateActCtx(context, out _cookie))
            {
                _cookie = 0;
            }
        }

        public void Dispose()
        {
            if (_cookie != 0)
            {
                DeactivateActCtx(0, _cookie);
            }
        }

        private static nint CreateActivationContext()
        {
            try
            {
                string path = Path.Join(Path.GetTempPath(), "GitExtensions", "CommonControls6.manifest");
                if (!File.Exists(path) || File.ReadAllText(path) != Manifest)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, Manifest);
                }

                ActivationContext context = new() { Size = Marshal.SizeOf<ActivationContext>(), Source = path };
                return CreateActCtxW(ref context);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return -1;
            }
        }
    }
}
