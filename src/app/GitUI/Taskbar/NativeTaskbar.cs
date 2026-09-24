using System.Runtime.InteropServices;

namespace GitUI;

/// <summary>The progress state of a taskbar button (the values of <c>TBPFLAG</c>).</summary>
public enum TaskbarProgressBarState
{
    NoProgress = 0,
    Indeterminate = 0x1,
    Normal = 0x2,
    Error = 0x4,
    Paused = 0x8,
}

/// <summary>A button of the thumbnail toolbar of a window (below its preview on the taskbar).</summary>
public sealed class ThumbnailToolBarButton : IDisposable
{
    private static uint _nextId = 0x4747;
    private nint _window;
    private bool _enabled = true;

    public ThumbnailToolBarButton(Icon icon, string tooltip)
    {
        Icon = icon;
        Tooltip = tooltip;
        Id = _nextId++;
    }

    /// <summary>Raised when the button is clicked.</summary>
    public event EventHandler? Click;

    public Icon Icon
    {
        get;
        set
        {
            field = value;
            Update();
        }
    }

    public string Tooltip { get; }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            Update();
        }
    }

    internal uint Id { get; }

    public void Dispose()
    {
        // The icons are owned by the caller; the buttons go away with the window.
        _window = 0;
    }

    internal void Attach(nint window) => _window = window;

    internal void OnClick() => Click?.Invoke(this, EventArgs.Empty);

    internal NativeTaskbar.ThumbButton ToNative() => new()
    {
        Mask = NativeTaskbar.THB_ICON | NativeTaskbar.THB_TOOLTIP | NativeTaskbar.THB_FLAGS,
        Id = Id,
        Icon = Icon.Handle,
        Tip = Tooltip,
        Flags = _enabled ? NativeTaskbar.THBF_ENABLED : NativeTaskbar.THBF_DISABLED,
    };

    private void Update()
    {
        if (_window != 0)
        {
            NativeTaskbar.UpdateThumbnailButtons(_window, [this]);
        }
    }
}

/// <summary>
///  The taskbar of Windows (the <c>ITaskbarList3</c> and the jump list of the shell), in place of the WindowsAPICodePack,
///  whose taskbar needs WinForms.
/// </summary>
internal static class NativeTaskbar
{
    internal const uint THB_ICON = 0x2;
    internal const uint THB_TOOLTIP = 0x4;
    internal const uint THB_FLAGS = 0x8;
    internal const uint THBF_ENABLED = 0x0;
    internal const uint THBF_DISABLED = 0x1;

    private const uint WM_COMMAND = 0x0111;
    private const int THBN_CLICKED = 0x1800;
    private const int KDC_RECENT = 2;
    private const uint SHARD_PATHW = 0x3;

    private static readonly Lazy<ITaskbarList3?> _taskbar = new(CreateTaskbar);
    private static readonly List<ThumbnailToolBarButton> _buttons = [];

    /// <summary>Whether the taskbar has progress, overlays, thumbnail toolbars and jump lists (Windows 7 and later).</summary>
    public static bool IsPlatformSupported => OperatingSystem.IsWindowsVersionAtLeast(6, 1);

    /// <summary>The application id of the process, which groups its windows and its jump list on the taskbar.</summary>
    public static void SetApplicationId(string applicationId) => SetCurrentProcessExplicitAppUserModelID(applicationId);

    public static void SetProgressState(nint window, TaskbarProgressBarState state)
    {
        if (window != 0)
        {
            _taskbar.Value?.SetProgressState(window, (int)state);
        }
    }

    public static void SetProgressValue(nint window, int completed, int total)
    {
        if (window != 0)
        {
            _taskbar.Value?.SetProgressValue(window, (ulong)Math.Max(0, completed), (ulong)Math.Max(0, total));
        }
    }

    /// <summary>Sets the overlay icon of the taskbar button of <paramref name="window"/>; none if <see langword="null"/>.</summary>
    public static void SetOverlayIcon(nint window, Icon? icon, string description)
    {
        if (window != 0)
        {
            _taskbar.Value?.SetOverlayIcon(window, icon?.Handle ?? 0, description);
        }
    }

    /// <summary>Adds the thumbnail toolbar of <paramref name="window"/> (once per window, as Windows allows).</summary>
    public static void AddThumbnailButtons(nint window, params ThumbnailToolBarButton[] buttons)
    {
        if (_taskbar.Value is not { } taskbar || window == 0)
        {
            return;
        }

        ThumbButton[] nativeButtons = [.. buttons.Select(button => button.ToNative())];
        Marshal.ThrowExceptionForHR(taskbar.ThumbBarAddButtons(window, (uint)nativeButtons.Length, nativeButtons));
        foreach (ThumbnailToolBarButton button in buttons)
        {
            button.Attach(window);
            _buttons.Add(button);
        }
    }

    public static void UpdateThumbnailButtons(nint window, ThumbnailToolBarButton[] buttons)
    {
        if (_taskbar.Value is not { } taskbar)
        {
            return;
        }

        ThumbButton[] nativeButtons = [.. buttons.Select(button => button.ToNative())];
        taskbar.ThumbBarUpdateButtons(window, (uint)nativeButtons.Length, nativeButtons);
    }

    /// <summary>
    ///  Raises the click of a thumbnail toolbar button for its <c>WM_COMMAND</c> message (from the window procedure of the
    ///  window of the toolbar); returns whether the message was a click.
    /// </summary>
    public static bool ProcessThumbnailButtonMessage(uint message, nint wordParameter)
    {
        if (message != WM_COMMAND || ((int)((long)wordParameter >> 16) & 0xFFFF) != THBN_CLICKED)
        {
            return false;
        }

        uint id = (uint)((long)wordParameter & 0xFFFF);
        ThumbnailToolBarButton? button = _buttons.FirstOrDefault(button => button.Id == id);
        button?.OnClick();
        return button is not null;
    }

    /// <summary>Adds <paramref name="path"/> to the recent documents of the application (its jump list).</summary>
    public static void AddToRecent(string path) => SHAddToRecentDocs(SHARD_PATHW, path);

    /// <summary>Shows the recent documents (the category of Windows) in the jump list, without tasks.</summary>
    public static void RefreshJumpList()
    {
        ICustomDestinationList list = (ICustomDestinationList)new DestinationListCoClass();
        try
        {
            Guid objectArrayId = typeof(IObjectArray).GUID;
            list.BeginList(out _, ref objectArrayId, out object removed);
            Marshal.ReleaseComObject(removed);
            list.AppendKnownCategory(KDC_RECENT);
            list.CommitList();
        }
        finally
        {
            Marshal.ReleaseComObject(list);
        }
    }

    private static ITaskbarList3? CreateTaskbar()
    {
        if (!IsPlatformSupported)
        {
            return null;
        }

        try
        {
            ITaskbarList3 taskbar = (ITaskbarList3)new TaskbarListCoClass();
            taskbar.HrInit();
            return taskbar;
        }
        catch (COMException)
        {
            return null;
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SetCurrentProcessExplicitAppUserModelID(string applicationId);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHAddToRecentDocs(uint flags, string path);

    /// <summary>The <c>THUMBBUTTON</c> structure.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ThumbButton
    {
        public uint Mask;
        public uint Id;
        public uint Bitmap;
        public nint Icon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string Tip;

        public uint Flags;
    }

    // ITaskbarList3 (shobjidl_core.h), with the methods of ITaskbarList and ITaskbarList2 in vtable order.
    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        void HrInit();

        void AddTab(nint window);

        void DeleteTab(nint window);

        void ActivateTab(nint window);

        void SetActiveAlt(nint window);

        void MarkFullscreenWindow(nint window, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);

        void SetProgressValue(nint window, ulong completed, ulong total);

        void SetProgressState(nint window, int flags);

        void RegisterTab(nint tab, nint window);

        void UnregisterTab(nint tab);

        void SetTabOrder(nint tab, nint insertBefore);

        void SetTabActive(nint tab, nint window, uint reserved);

        [PreserveSig]
        int ThumbBarAddButtons(nint window, uint count, [MarshalAs(UnmanagedType.LPArray)] ThumbButton[] buttons);

        [PreserveSig]
        int ThumbBarUpdateButtons(nint window, uint count, [MarshalAs(UnmanagedType.LPArray)] ThumbButton[] buttons);

        void ThumbBarSetImageList(nint window, nint imageList);

        void SetOverlayIcon(nint window, nint icon, [MarshalAs(UnmanagedType.LPWStr)] string description);

        void SetThumbnailTooltip(nint window, [MarshalAs(UnmanagedType.LPWStr)] string tip);

        void SetThumbnailClip(nint window, nint clip);
    }

    [ComImport]
    [Guid("6332debf-87b5-4670-90c0-5e57b408a49e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICustomDestinationList
    {
        void SetAppID([MarshalAs(UnmanagedType.LPWStr)] string applicationId);

        void BeginList(out uint minSlots, ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out object removedItems);

        void AppendCategory([MarshalAs(UnmanagedType.LPWStr)] string category, [MarshalAs(UnmanagedType.Interface)] object items);

        void AppendKnownCategory(int category);

        void AddUserTasks([MarshalAs(UnmanagedType.Interface)] object tasks);

        void CommitList();

        void GetRemovedDestinations(ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out object removedItems);

        void DeleteList([MarshalAs(UnmanagedType.LPWStr)] string? applicationId);

        void AbortList();
    }

    [ComImport]
    [Guid("92ca9dcd-5622-4bba-a805-5e9f541bd8c9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IObjectArray
    {
        void GetCount(out uint count);

        void GetAt(uint index, ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out object item);
    }

    [ComImport]
    [Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
    private class TaskbarListCoClass
    {
    }

    [ComImport]
    [Guid("77f10cf0-3db5-4966-b520-b7c54fd35ed6")]
    private class DestinationListCoClass
    {
    }
}
