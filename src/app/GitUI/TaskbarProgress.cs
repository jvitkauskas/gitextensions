using System.Diagnostics;
using GitCommands.Utils;

namespace GitUI;

public static class TaskbarProgress
{
    // As the TaskbarManager of the WindowsAPICodePack: the taskbar button of the main window of the process.
    private static void Try(Action<nint> action)
    {
        if (EnvUtils.RunningOnWindowsWithMainWindow() && NativeTaskbar.IsPlatformSupported)
        {
            try
            {
                using Process process = Process.GetCurrentProcess();
                action(process.MainWindowHandle);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
            {
            }
        }
    }

    public static void Clear()
    {
        Try(window => NativeTaskbar.SetProgressState(window, TaskbarProgressBarState.NoProgress));
    }

    public static void SetProgress(TaskbarProgressBarState state, int progressValue, int maximumValue)
    {
        Try(window =>
        {
            NativeTaskbar.SetProgressState(window, state);
            NativeTaskbar.SetProgressValue(window, progressValue, maximumValue);
        });
    }

    public static void SetState(TaskbarProgressBarState state)
    {
        Try(window => NativeTaskbar.SetProgressState(window, state));
    }
}
