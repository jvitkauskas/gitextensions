namespace GitExtensions.Extensibility;

/// <summary>
///  The bridge between <see cref="WindowOwner"/> (plugin API v2) and <see cref="IWin32Window"/> (plugin API v1 and the WinForms
///  code). It goes away with WinForms (docs/avalonia-port/PLAN.md, phase 8).
/// </summary>
public static class WindowOwnerWinFormsExtensions
{
    /// <summary>The owner of <paramref name="window"/>'s handle; <see cref="WindowOwner.None"/> for <see langword="null"/>.</summary>
    /// <remarks>Reading the handle of a WinForms control creates it if needed.</remarks>
    public static WindowOwner ToWindowOwner(this IWin32Window? window)
        => window is null ? WindowOwner.None : new WindowOwner(window.Handle);

    /// <summary>
    ///  The owner as a WinForms owner: the WinForms control of the handle if there is one (so that the WinForms code finds the
    ///  form it expects, e.g. <c>FormBrowse</c>), or else a wrapper of the handle (e.g. of an Avalonia window);
    ///  <see langword="null"/> for <see cref="WindowOwner.None"/>.
    /// </summary>
    public static IWin32Window? ToWin32Window(this WindowOwner owner)
    {
        if (owner.IsNone)
        {
            return null;
        }

        return Control.FromHandle(owner.Handle) ?? (IWin32Window)new NativeWindowHandle(owner.Handle);
    }

    /// <summary>A native window known by its handle only.</summary>
    private sealed class NativeWindowHandle(nint handle) : IWin32Window
    {
        public nint Handle { get; } = handle;
    }
}
