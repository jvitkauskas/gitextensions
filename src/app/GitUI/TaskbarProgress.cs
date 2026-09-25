using System.Diagnostics;
using System.Runtime.Versioning;
using GitCommands.Utils;

namespace GitUI;

public static class TaskbarProgress
{
    /// <summary>Whether the taskbar has progress, overlays, thumbnail toolbars and jump lists (Windows 7 and later; not on other systems).</summary>
    [SupportedOSPlatformGuard("windows6.1")]
    public static bool IsPlatformSupported => OperatingSystem.IsWindowsVersionAtLeast(6, 1);

    public static void Clear() => SetState(TaskbarProgressBarState.NoProgress);

    public static void SetProgress(TaskbarProgressBarState state, int progressValue, int maximumValue)
    {
        if (TryGetMainWindow(out nint window))
        {
            try
            {
                NativeTaskbar.SetProgressState(window, state);
                NativeTaskbar.SetProgressValue(window, progressValue, maximumValue);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
            {
            }
        }
    }

    public static void SetState(TaskbarProgressBarState state)
    {
        if (TryGetMainWindow(out nint window))
        {
            try
            {
                NativeTaskbar.SetProgressState(window, state);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
            {
            }
        }
    }

    // As the TaskbarManager of the WindowsAPICodePack: the taskbar button of the main window of the process.
    [SupportedOSPlatformGuard("windows6.1")]
    private static bool TryGetMainWindow(out nint window)
    {
        window = 0;
        if (!EnvUtils.RunningOnWindowsWithMainWindow() || !IsPlatformSupported)
        {
            return false;
        }

        try
        {
            using Process process = Process.GetCurrentProcess();
            window = process.MainWindowHandle;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
