using System.Diagnostics;
using GitExtensions.Extensibility;

namespace GitCommands;

/// <summary>
///  Provides helper methods for interacting with the OS shell (opening files, URLs, and the file manager): File Explorer
///  on Windows, the Finder on macOS (<c>open</c>), the file manager of the desktop on Linux (<c>xdg-open</c> and the
///  FreeDesktop <c>FileManager1</c> D-Bus interface). docs/avalonia-port/CROSS-PLATFORM.md, phase 3.
/// </summary>
public static class OsShellUtil
{
    internal enum ShellPlatform
    {
        Windows,
        MacOS,
        FreeDesktop,
    }

    private static ShellPlatform Platform
        => TestAccessor.Platform
            ?? (OperatingSystem.IsWindows() ? ShellPlatform.Windows : OperatingSystem.IsMacOS() ? ShellPlatform.MacOS : ShellPlatform.FreeDesktop);

    private static IExecutable CreateExecutable(string command)
    {
        if (TestAccessor.MockExecutable is { } mock)
        {
            TestAccessor.Commands.Add(command);
            return mock;
        }

        return new Executable(command);
    }

    /// <summary>Whether the system lets the user choose the application that opens a file (<see cref="OpenAs"/>).</summary>
    public static bool CanOpenAs => Platform == ShellPlatform.Windows;

    /// <summary>
    ///  Open a file with its associated default application.
    /// </summary>
    /// <param name="filePath">Pathname of the file to open.</param>
    public static void Open(string filePath)
    {
        try
        {
            // Off Windows .NET opens the file with xdg-open (Linux) or open (macOS).
            _ = CreateExecutable(filePath).Start(useShellExecute: true, throwOnErrorExit: false);
        }
        catch (Exception ex)
        {
            if (!CanOpenAs)
            {
                Trace.WriteLine(ex);
                return;
            }

            OpenAs(filePath);
        }
    }

    /// <summary>
    ///  Let the user chose an application to open a file; elsewhere than on Windows, opens it with its default application.
    /// </summary>
    /// <param name="filePath">Pathname of the file to open.</param>
    public static void OpenAs(string filePath)
    {
        if (!CanOpenAs)
        {
            Open(filePath);
            return;
        }

        // filePath must not be quoted
        _ = CreateExecutable("rundll32.exe").Start("shell32.dll,OpenAs_RunDLL " + filePath, redirectOutput: true, outputEncoding: System.Text.Encoding.UTF8);
    }

    /// <summary>
    ///  Selects the specified file in the file manager (on Linux, opens its folder if the file manager cannot select it).
    /// </summary>
    /// <param name="filePath">The full path of the file to select.</param>
    public static void SelectPathInFileExplorer(string filePath)
    {
        switch (Platform)
        {
            case ShellPlatform.Windows:
                OpenWithFileExplorer($"/select, {filePath.Quote()}", quote: false);
                break;
            case ShellPlatform.MacOS:
                _ = CreateExecutable("open").Start($"-R {filePath.Quote()}", throwOnErrorExit: false);
                break;
            default:
                // The file manager may take a while to start: not on the UI thread.
                Task task = Task.Run(() => SelectWithFreeDesktopFileManagerAsync(filePath));
                if (TestAccessor.Synchronous)
                {
#pragma warning disable VSTHRD002 // Tests only.
                    task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                }

                break;
        }
    }

    /// <summary>
    ///  Opens a folder in the file manager.
    /// </summary>
    /// <param name="arguments">The folder; on Windows, the arguments of explorer.exe.</param>
    /// <param name="quote">Whether to quote the <paramref name="arguments"/> (Windows only: elsewhere they are a path).</param>
    public static void OpenWithFileExplorer(string arguments, bool quote = true)
    {
        switch (Platform)
        {
            case ShellPlatform.Windows:
                _ = CreateExecutable("explorer.exe").Start(quote ? arguments.Quote() : arguments);
                break;
            case ShellPlatform.MacOS:
                _ = CreateExecutable("open").Start(arguments.Quote(), throwOnErrorExit: false);
                break;
            default:
                _ = CreateExecutable("xdg-open").Start(arguments.Quote(), throwOnErrorExit: false);
                break;
        }
    }

    /// <summary>
    ///  Opens the specified URL in the user's default web browser.
    /// </summary>
    /// <param name="url">The URL to open, or <see langword="null"/> to do nothing.</param>
    public static void OpenUrlInDefaultBrowser(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            _ = CreateExecutable(url).Start(useShellExecute: true, throwOnErrorExit: false);
        }
    }

    /// <summary>The <c>ShowItems</c> call of the FreeDesktop file manager interface, else the folder in the default file manager.</summary>
    private static async Task SelectWithFreeDesktopFileManagerAsync(string filePath)
    {
        try
        {
            using IProcess process = CreateExecutable("dbus-send").Start(GetShowItemsArguments(filePath), redirectOutput: true, throwOnErrorExit: false);
            if (await process.WaitForExitAsync() == 0)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            // No dbus-send, or no file manager that implements the interface.
            Trace.WriteLine(ex);
        }

        if (Path.GetDirectoryName(filePath) is { Length: > 0 } folder)
        {
            OpenWithFileExplorer(folder);
        }
    }

    /// <summary>The arguments of <c>dbus-send</c> that ask the file manager to show <paramref name="filePath"/> selected.</summary>
    internal static string GetShowItemsArguments(string filePath)
    {
        // A comma separates the items of an array of dbus-send.
        string uri = new Uri(Path.GetFullPath(filePath)).AbsoluteUri.Replace(",", "%2C");
        return $"--session --print-reply --dest=org.freedesktop.FileManager1 --type=method_call /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems {$"array:string:{uri}".Quote()} string:\"\"";
    }

    internal struct TestAccessor
    {
        public static IExecutable? MockExecutable { get; set; }

        /// <summary>The commands started with <see cref="MockExecutable"/>.</summary>
        public static List<string> Commands { get; } = [];

        /// <summary>The system whose shell is used, instead of the actual one.</summary>
        public static ShellPlatform? Platform { get; set; }

        /// <summary>Whether the file manager of Linux is asked on the calling thread.</summary>
        public static bool Synchronous { get; set; }
    }
}
