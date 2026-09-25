using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GitExtensions.Extensibility;

/// <summary>
///  The native dialog to choose a file (the Common Item Dialog), with the properties the WinForms <c>FileDialog</c> had; off
///  Windows the file dialog of the UI of the application (<see cref="DialogBoxHost"/>).
/// </summary>
public abstract class FileDialog : IDisposable
{
    private const uint ERROR_CANCELLED = 0x800704C7;

    /// <summary>The title of the dialog; the default one if none.</summary>
    public string? Title { get; set; }

    /// <summary>The file types, as in WinForms: "Text files (*.txt)|*.txt|All files (*.*)|*.*".</summary>
    public string? Filter { get; set; }

    /// <summary>The one-based index of the selected file type in <see cref="Filter"/>.</summary>
    public int FilterIndex { get; set; } = 1;

    public string? InitialDirectory { get; set; }

    /// <summary>The initial file name, and the chosen one once the dialog is accepted.</summary>
    public string FileName { get; set; } = "";

    /// <summary>
    ///  The extension added to a file name typed without one, set with or without the dot and kept without it, as the
    ///  <c>DefaultExt</c> of WinForms (callers set it from <see cref="Path.GetExtension(string)"/> and build filters with it).
    /// </summary>
    public string? DefaultExt { get; set => field = value?.TrimStart('.'); }

    /// <summary>Whether <see cref="DefaultExt"/> is added to a file name typed without extension.</summary>
    public bool AddExtension { get; set; } = true;

    /// <summary>Shows the dialog modally over <paramref name="owner"/> (the active window if none).</summary>
    /// <returns><see cref="DialogResult.OK"/> if a file was chosen, else <see cref="DialogResult.Cancel"/>.</returns>
    public DialogResult ShowDialog(IWin32Window? owner = null)
    {
        if (DialogBoxHost.Active is { } host)
        {
            FileDialogRequest request = new(
                Kind,
                Title,
                [.. ParseFilter(Filter).Select(f => new FileDialogFileType(f.Name, f.Spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))],
                FilterIndex,
                InitialDirectory,
                FileName,
                AddExtension && !string.IsNullOrEmpty(DefaultExt) ? DefaultExt.TrimStart('.') : null,
                AllowsMultiselect,
                PromptsOverwrite);
            if (host.ShowFileDialog(owner?.Handle ?? 0, request) is not { Paths.Count: > 0 } result)
            {
                return DialogResult.Cancel;
            }

            FilterIndex = result.FileTypeIndex;
            SetResult(result.Paths);
            return DialogResult.OK;
        }

        return OperatingSystem.IsWindowsVersionAtLeast(6, 1) ? ShowNativeDialog(owner) : throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows6.1")]
    private DialogResult ShowNativeDialog(IWin32Window? owner)
    {
        nint hwndOwner = owner?.Handle ?? 0;
        if (hwndOwner == 0)
        {
            hwndOwner = DialogNativeMethods.GetActiveWindow();
        }

        IFileDialog dialog = CreateDialog();
        try
        {
            dialog.GetOptions(out uint options);
            dialog.SetOptions(options | GetOptions() | FileDialogOptions.ForceFileSystem | FileDialogOptions.NoChangeDirectory);

            if (!string.IsNullOrEmpty(Title))
            {
                dialog.SetTitle(Title);
            }

            FilterSpec[] filters = ParseFilter(Filter);
            if (filters.Length > 0)
            {
                dialog.SetFileTypes((uint)filters.Length, filters);
                dialog.SetFileTypeIndex((uint)Math.Clamp(FilterIndex, 1, filters.Length));
            }

            if (AddExtension && !string.IsNullOrEmpty(DefaultExt))
            {
                dialog.SetDefaultExtension(DefaultExt.TrimStart('.'));
            }

            if (!string.IsNullOrEmpty(FileName))
            {
                dialog.SetFileName(Path.GetFileName(FileName));
            }

            string? initialDirectory = !string.IsNullOrEmpty(InitialDirectory) ? InitialDirectory : Path.GetDirectoryName(FileName);
            if (!string.IsNullOrEmpty(initialDirectory) && TryCreateShellItem(initialDirectory) is { } folder)
            {
                dialog.SetFolder(folder);
            }

            using (new DialogNativeMethods.ThemingScope())
            {
                int hresult = dialog.Show(hwndOwner);
                if ((uint)hresult == ERROR_CANCELLED)
                {
                    return DialogResult.Cancel;
                }

                Marshal.ThrowExceptionForHR(hresult);
            }

            dialog.GetFileTypeIndex(out uint fileTypeIndex);
            FilterIndex = (int)fileTypeIndex;
            ReadResult(dialog);
            return DialogResult.OK;
        }
        finally
        {
            Marshal.FinalReleaseComObject(dialog);
        }
    }

    public void Dispose()
    {
        // Nothing to release: the native dialog lives while shown only.
        GC.SuppressFinalize(this);
    }

    /// <summary>What the dialog chooses (for the dialogs of <see cref="DialogBoxHost"/>).</summary>
    private protected abstract FileDialogKind Kind { get; }

    private protected virtual bool AllowsMultiselect => false;

    private protected virtual bool PromptsOverwrite => false;

    /// <summary>Takes the paths chosen with a dialog of <see cref="DialogBoxHost"/>.</summary>
    private protected virtual void SetResult(IReadOnlyList<string> paths) => FileName = paths[0];

    [SupportedOSPlatform("windows6.1")]
    private protected abstract IFileDialog CreateDialog();

    [SupportedOSPlatform("windows6.1")]
    private protected abstract uint GetOptions();

    [SupportedOSPlatform("windows6.1")]
    private protected virtual void ReadResult(IFileDialog dialog)
    {
        dialog.GetResult(out IShellItem item);
        FileName = GetPath(item);
    }

    [SupportedOSPlatform("windows6.1")]
    private protected static string GetPath(IShellItem item)
    {
        item.GetDisplayName(ShellItemDisplayName.FileSystemPath, out nint path);
        try
        {
            return Marshal.PtrToStringUni(path) ?? "";
        }
        finally
        {
            Marshal.FreeCoTaskMem(path);
        }
    }

    [SupportedOSPlatform("windows6.1")]
    private protected static IShellItem? TryCreateShellItem(string path)
    {
        try
        {
            Guid shellItemId = typeof(IShellItem).GUID;
            return SHCreateItemFromParsingName(Path.GetFullPath(path), 0, ref shellItemId, out IShellItem item) == 0 ? item : null;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or COMException)
        {
            return null;
        }
    }

    /// <summary>The file types of a WinForms filter ("name|patterns|name|patterns").</summary>
    internal static FilterSpec[] ParseFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return [];
        }

        string[] parts = filter.Split('|');
        if (parts.Length % 2 != 0)
        {
            throw new ArgumentException("The filter must be pairs of a name and patterns, separated by '|'.", nameof(filter));
        }

        return [.. Enumerable.Range(0, parts.Length / 2).Select(i => new FilterSpec { Name = parts[2 * i], Spec = parts[(2 * i) + 1] })];
    }

    [SupportedOSPlatform("windows6.1")]
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(string path, nint bindContext, ref Guid interfaceId, out IShellItem item);

    private protected static class FileDialogOptions
    {
        public const uint OverwritePrompt = 0x2;
        public const uint NoChangeDirectory = 0x8;
        public const uint PickFolders = 0x20;
        public const uint ForceFileSystem = 0x40;
        public const uint AllowMultiselect = 0x200;
        public const uint PathMustExist = 0x800;
        public const uint FileMustExist = 0x1000;
    }

    private protected static class ShellItemDisplayName
    {
        public const uint FileSystemPath = 0x80058000;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct FilterSpec
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Name;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Spec;
    }

    // The COM interfaces of the Common Item Dialog (shobjidl_core.h), with the methods of their base interfaces in
    // vtable order.
    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SupportedOSPlatform("windows6.1")]
    private protected interface IFileDialog
    {
        [PreserveSig]
        int Show(nint parent);

        void SetFileTypes(uint count, [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] filterSpecs);

        void SetFileTypeIndex(uint index);

        void GetFileTypeIndex(out uint index);

        void Advise(nint events, out uint cookie);

        void Unadvise(uint cookie);

        void SetOptions(uint options);

        void GetOptions(out uint options);

        void SetDefaultFolder(IShellItem item);

        void SetFolder(IShellItem item);

        void GetFolder(out IShellItem item);

        void GetCurrentSelection(out IShellItem item);

        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

        void GetResult(out IShellItem item);

        void AddPlace(IShellItem item, int placement);

        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);

        void Close(int result);

        void SetClientGuid(ref Guid guid);

        void ClearClientData();

        void SetFilter(nint filter);
    }

    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SupportedOSPlatform("windows6.1")]
    private protected interface IFileOpenDialog : IFileDialog
    {
        [PreserveSig]
        new int Show(nint parent);

        new void SetFileTypes(uint count, [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] filterSpecs);

        new void SetFileTypeIndex(uint index);

        new void GetFileTypeIndex(out uint index);

        new void Advise(nint events, out uint cookie);

        new void Unadvise(uint cookie);

        new void SetOptions(uint options);

        new void GetOptions(out uint options);

        new void SetDefaultFolder(IShellItem item);

        new void SetFolder(IShellItem item);

        new void GetFolder(out IShellItem item);

        new void GetCurrentSelection(out IShellItem item);

        new void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

        new void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        new void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

        new void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

        new void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

        new void GetResult(out IShellItem item);

        new void AddPlace(IShellItem item, int placement);

        new void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);

        new void Close(int result);

        new void SetClientGuid(ref Guid guid);

        new void ClearClientData();

        new void SetFilter(nint filter);

        void GetResults(out IShellItemArray items);

        void GetSelectedItems(out IShellItemArray items);
    }

    [ComImport]
    [Guid("84bccd23-5fde-4cdb-aea4-af64b83d78ab")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SupportedOSPlatform("windows6.1")]
    private protected interface IFileSaveDialog : IFileDialog
    {
        [PreserveSig]
        new int Show(nint parent);

        new void SetFileTypes(uint count, [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] filterSpecs);

        new void SetFileTypeIndex(uint index);

        new void GetFileTypeIndex(out uint index);

        new void Advise(nint events, out uint cookie);

        new void Unadvise(uint cookie);

        new void SetOptions(uint options);

        new void GetOptions(out uint options);

        new void SetDefaultFolder(IShellItem item);

        new void SetFolder(IShellItem item);

        new void GetFolder(out IShellItem item);

        new void GetCurrentSelection(out IShellItem item);

        new void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);

        new void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);

        new void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);

        new void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);

        new void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);

        new void GetResult(out IShellItem item);

        new void AddPlace(IShellItem item, int placement);

        new void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);

        new void Close(int result);

        new void SetClientGuid(ref Guid guid);

        new void ClearClientData();

        new void SetFilter(nint filter);

        void SetSaveAsItem(IShellItem item);

        void SetProperties(nint store);

        void SetCollectedProperties(nint list, int appendDefault);

        void GetProperties(out nint store);

        void ApplyProperties(IShellItem item, nint store, nint window, nint sink);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SupportedOSPlatform("windows6.1")]
    private protected interface IShellItem
    {
        void BindToHandler(nint bindContext, ref Guid handler, ref Guid interfaceId, out nint result);

        void GetParent(out IShellItem parent);

        void GetDisplayName(uint type, out nint name);

        void GetAttributes(uint mask, out uint attributes);

        void Compare(IShellItem item, uint hint, out int order);
    }

    [ComImport]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SupportedOSPlatform("windows6.1")]
    private protected interface IShellItemArray
    {
        void BindToHandler(nint bindContext, ref Guid handler, ref Guid interfaceId, out nint result);

        void GetPropertyStore(int flags, ref Guid interfaceId, out nint result);

        void GetPropertyDescriptionList(nint keyType, ref Guid interfaceId, out nint result);

        void GetAttributes(int flags, uint mask, out uint attributes);

        void GetCount(out uint count);

        void GetItemAt(uint index, out IShellItem item);

        void EnumItems(out nint items);
    }

    [ComImport]
    [Guid("dc1c5a9c-e88a-4dde-a5a1-60f82a20aef7")]
    [SupportedOSPlatform("windows6.1")]
    private protected class FileOpenDialogCoClass
    {
    }

    [ComImport]
    [Guid("c0b4e2f3-ba21-4773-8dba-335ec946eb8b")]
    [SupportedOSPlatform("windows6.1")]
    private protected class FileSaveDialogCoClass
    {
    }
}

/// <summary>The native dialog to choose files to open (as the WinForms <c>OpenFileDialog</c>).</summary>
public sealed class OpenFileDialog : FileDialog
{
    public bool Multiselect { get; set; }

    public bool CheckFileExists { get; set; } = true;

    /// <summary>The chosen files, once the dialog is accepted.</summary>
    public string[] FileNames { get; private set; } = [];

    private protected override FileDialogKind Kind => FileDialogKind.Open;

    private protected override bool AllowsMultiselect => Multiselect;

    private protected override void SetResult(IReadOnlyList<string> paths)
    {
        FileNames = [.. paths];
        FileName = paths[0];
    }

    [SupportedOSPlatform("windows6.1")]
    private protected override IFileDialog CreateDialog() => (IFileOpenDialog)new FileOpenDialogCoClass();

    [SupportedOSPlatform("windows6.1")]
    private protected override uint GetOptions()
        => FileDialogOptions.PathMustExist
            | (CheckFileExists ? FileDialogOptions.FileMustExist : 0)
            | (Multiselect ? FileDialogOptions.AllowMultiselect : 0);

    [SupportedOSPlatform("windows6.1")]
    private protected override void ReadResult(IFileDialog dialog)
    {
        ((IFileOpenDialog)dialog).GetResults(out IShellItemArray items);
        items.GetCount(out uint count);
        FileNames = [.. Enumerable.Range(0, (int)count).Select(i =>
        {
            items.GetItemAt((uint)i, out IShellItem item);
            return GetPath(item);
        })];
        FileName = FileNames.FirstOrDefault() ?? "";
    }
}

/// <summary>The native dialog to choose a file to save (as the WinForms <c>SaveFileDialog</c>).</summary>
public sealed class SaveFileDialog : FileDialog
{
    public bool OverwritePrompt { get; set; } = true;

    private protected override FileDialogKind Kind => FileDialogKind.Save;

    private protected override bool PromptsOverwrite => OverwritePrompt;

    [SupportedOSPlatform("windows6.1")]
    private protected override IFileDialog CreateDialog() => (IFileSaveDialog)new FileSaveDialogCoClass();

    [SupportedOSPlatform("windows6.1")]
    private protected override uint GetOptions()
        => FileDialogOptions.PathMustExist | (OverwritePrompt ? FileDialogOptions.OverwritePrompt : 0);
}

/// <summary>The native dialog to choose a folder (as the WinForms <c>FolderBrowserDialog</c>, the Common Item Dialog).</summary>
public sealed class FolderBrowserDialog : IDisposable
{
    public string? InitialDirectory { get; set; }

    /// <summary>The initially selected folder, and the chosen one once the dialog is accepted.</summary>
    public string SelectedPath { get; set; } = "";

    /// <summary>The title of the dialog.</summary>
    public string? Description { get; set; }

    /// <summary>Kept for compatibility: the dialog always allows to create a folder.</summary>
    public bool ShowNewFolderButton { get; set; } = true;

    public DialogResult ShowDialog(IWin32Window? owner = null)
    {
        FolderPicker picker = new()
        {
            Title = Description,
            InitialDirectory = string.IsNullOrEmpty(SelectedPath) ? InitialDirectory : SelectedPath,
        };
        DialogResult result = picker.ShowDialog(owner);
        if (result == DialogResult.OK)
        {
            SelectedPath = picker.FileName;
        }

        return result;
    }

    public void Dispose()
    {
        // Nothing to release: the native dialog lives while shown only.
    }

    /// <summary>The open dialog of the folders.</summary>
    private sealed class FolderPicker : FileDialog
    {
        private protected override FileDialogKind Kind => FileDialogKind.Folder;

        [SupportedOSPlatform("windows6.1")]
        private protected override IFileDialog CreateDialog() => (IFileOpenDialog)new FileOpenDialogCoClass();

        [SupportedOSPlatform("windows6.1")]
        private protected override uint GetOptions() => FileDialogOptions.PickFolders | FileDialogOptions.PathMustExist;
    }
}

/// <summary>
///  The native dialog to choose a color (as the WinForms <c>ColorDialog</c>); off Windows the color dialog of the UI of the
///  application (<see cref="DialogBoxHost"/>).
/// </summary>
public sealed class ColorDialog : IDisposable
{
    private const int CC_RGBINIT = 0x1;
    private const int CC_FULLOPEN = 0x2;
    private const int CC_ANYCOLOR = 0x100;

    private static readonly int[] _customColors = new int[16];

    public Color Color { get; set; } = Color.Black;

    public bool FullOpen { get; set; }

    public DialogResult ShowDialog(IWin32Window? owner = null)
    {
        if (DialogBoxHost.Active is { } host)
        {
            if (host.ShowColorDialog(owner?.Handle ?? 0, Color) is not { } color)
            {
                return DialogResult.Cancel;
            }

            Color = color;
            return DialogResult.OK;
        }

        return OperatingSystem.IsWindowsVersionAtLeast(6, 1) ? ShowNativeDialog(owner) : throw new PlatformNotSupportedException();
    }

    [SupportedOSPlatform("windows6.1")]
    private DialogResult ShowNativeDialog(IWin32Window? owner)
    {
        nint hwndOwner = owner?.Handle ?? DialogNativeMethods.GetActiveWindow();
        nint customColors = Marshal.AllocCoTaskMem(sizeof(int) * _customColors.Length);
        try
        {
            Marshal.Copy(_customColors, 0, customColors, _customColors.Length);
            ChooseColorData data = new()
            {
                Size = Marshal.SizeOf<ChooseColorData>(),
                Owner = hwndOwner,
                Result = ColorTranslator.ToWin32(Color),
                CustomColors = customColors,
                Flags = CC_RGBINIT | CC_ANYCOLOR | (FullOpen ? CC_FULLOPEN : 0),
            };

            bool accepted;
            using (new DialogNativeMethods.ThemingScope())
            {
                accepted = ChooseColorW(ref data);
            }

            Marshal.Copy(customColors, _customColors, 0, _customColors.Length);
            if (!accepted)
            {
                return DialogResult.Cancel;
            }

            Color = ColorTranslator.FromWin32(data.Result);
            return DialogResult.OK;
        }
        finally
        {
            Marshal.FreeCoTaskMem(customColors);
        }
    }

    public void Dispose()
    {
        // Nothing to release: the native dialog lives while shown only.
    }

    [SupportedOSPlatform("windows6.1")]
    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChooseColorW(ref ChooseColorData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct ChooseColorData
    {
        public int Size;
        public nint Owner;
        public nint Instance;
        public int Result;
        public nint CustomColors;
        public int Flags;
        public nint CustomData;
        public nint Hook;
        public nint TemplateName;
    }
}

/// <summary>The native dialog to choose a font (as the WinForms <c>FontDialog</c>).</summary>
[SupportedOSPlatform("windows6.1")]
public sealed class FontDialog : IDisposable
{
    private const int CF_SCREENFONTS = 0x1;
    private const int CF_INITTOLOGFONTSTRUCT = 0x40;
    private const int CF_EFFECTS = 0x100;
    private const int CF_FIXEDPITCHONLY = 0x4000;
    private const int CF_NOVERTFONTS = 0x01000000;

    public Font Font { get; set; } = SystemFonts.DefaultFont;

    public Color Color { get; set; } = SystemColors.ControlText;

    public bool FixedPitchOnly { get; set; }

    public bool AllowVerticalFonts { get; set; } = true;

    public bool ShowEffects { get; set; } = true;

    public DialogResult ShowDialog(IWin32Window? owner = null)
    {
        nint hwndOwner = owner?.Handle ?? DialogNativeMethods.GetActiveWindow();
        LogFont logFont = new();
        Font.ToLogFont(logFont);
        nint logFontMemory = Marshal.AllocCoTaskMem(Marshal.SizeOf<LogFont>());
        try
        {
            Marshal.StructureToPtr(logFont, logFontMemory, fDeleteOld: false);
            ChooseFontData data = new()
            {
                Size = Marshal.SizeOf<ChooseFontData>(),
                Owner = hwndOwner,
                LogFont = logFontMemory,
                Flags = CF_SCREENFONTS | CF_INITTOLOGFONTSTRUCT
                    | (ShowEffects ? CF_EFFECTS : 0)
                    | (FixedPitchOnly ? CF_FIXEDPITCHONLY : 0)
                    | (AllowVerticalFonts ? 0 : CF_NOVERTFONTS),
                Colors = ColorTranslator.ToWin32(Color),
            };

            bool accepted;
            using (new DialogNativeMethods.ThemingScope())
            {
                accepted = ChooseFontW(ref data);
            }

            if (!accepted)
            {
                return DialogResult.Cancel;
            }

            Marshal.PtrToStructure(logFontMemory, logFont);
            using Font chosen = Font.FromLogFont(logFont);

            // As WinForms: the size in points (not in the world units of the log font).
            Font = new Font(chosen.FontFamily, chosen.SizeInPoints, chosen.Style, GraphicsUnit.Point, chosen.GdiCharSet, chosen.GdiVerticalFont);
            Color = ColorTranslator.FromWin32(data.Colors);
            return DialogResult.OK;
        }
        finally
        {
            Marshal.FreeCoTaskMem(logFontMemory);
        }
    }

    public void Dispose()
    {
        // Nothing to release: the native dialog lives while shown only.
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChooseFontW(ref ChooseFontData data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class LogFont
    {
        public int Height;
        public int Width;
        public int Escapement;
        public int Orientation;
        public int Weight;
        public byte Italic;
        public byte Underline;
        public byte StrikeOut;
        public byte CharacterSet;
        public byte OutPrecision;
        public byte ClipPrecision;
        public byte Quality;
        public byte PitchAndFamily;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FaceName = "";
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ChooseFontData
    {
        public int Size;
        public nint Owner;
        public nint DeviceContext;
        public nint LogFont;
        public int PointSize;
        public int Flags;
        public int Colors;
        public nint CustomData;
        public nint Hook;
        public nint TemplateName;
        public nint Instance;
        public nint Style;
        public short FontType;
        public short MissingAlignment;
        public int SizeMin;
        public int SizeMax;
    }
}

/// <summary>
///  The dialog to choose a font of the settings: the native font dialog on Windows (<see cref="FontDialog"/>), else the font
///  dialog of the UI of the application (<see cref="DialogBoxHost"/>).
/// </summary>
public static class FontPicker
{
    /// <summary>Shows the font dialog over <paramref name="owner"/>, with <paramref name="font"/> (Consolas 12 if none).</summary>
    /// <param name="fixedPitchOnly">Whether only the fonts of fixed pitch are offered (for code).</param>
    /// <returns>The chosen font; <see langword="null"/> if the dialog was cancelled.</returns>
    /// <exception cref="ArgumentException">The native dialog does not accept the font (e.g. a font that is not TrueType).</exception>
    public static FontDescriptor? Show(IWin32Window? owner, FontDescriptor? font, bool fixedPitchOnly)
    {
        if (DialogBoxHost.Active is { } host)
        {
            return host.ShowFontDialog(owner?.Handle ?? 0, font, fixedPitchOnly);
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            throw new PlatformNotSupportedException();
        }

        using FontDialog dialog = new()
        {
            AllowVerticalFonts = false,
            Color = SystemColors.ControlText,
            FixedPitchOnly = fixedPitchOnly,
        };
        dialog.Font = font?.ToFont() ?? new Font("Consolas", 12);
        return dialog.ShowDialog(owner) is DialogResult.OK or DialogResult.Yes ? dialog.Font.ToFontDescriptor() : null;
    }
}
