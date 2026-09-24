namespace GitExtensions.Extensibility;

/// <summary>The bridge between <see cref="WindowOwner"/> (plugin API v2) and <see cref="IWin32Window"/> (the host code).</summary>
public static class WindowOwnerExtensions
{
    /// <summary>The owner of <paramref name="window"/>'s handle; <see cref="WindowOwner.None"/> for <see langword="null"/>.</summary>
    public static WindowOwner ToWindowOwner(this IWin32Window? window)
        => window is null ? WindowOwner.None : new WindowOwner(window.Handle);

    /// <summary>The owner as an <see cref="IWin32Window"/>; <see langword="null"/> for <see cref="WindowOwner.None"/>.</summary>
    public static IWin32Window? ToWin32Window(this WindowOwner owner)
        => owner.IsNone ? null : new NativeWindowHandle(owner.Handle);

    /// <summary>A native window known by its handle only.</summary>
    private sealed class NativeWindowHandle(nint handle) : IWin32Window
    {
        public nint Handle { get; } = handle;
    }
}
