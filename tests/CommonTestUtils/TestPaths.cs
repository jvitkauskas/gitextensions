namespace CommonTestUtils;

/// <summary>
///  Paths of test data written as Windows paths, as paths of the system the tests run on (docs/avalonia-port/CROSS-PLATFORM.md,
///  phase 2): unchanged on Windows; elsewhere without the drive and with <c>/</c> (<c>C:\src\repo\</c> is <c>/src/repo/</c>).
/// </summary>
public static class TestPaths
{
    public static string Native(string windowsPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return windowsPath;
        }

        string path = windowsPath.Length >= 2 && windowsPath[1] == ':' ? windowsPath[2..] : windowsPath;
        return path.Replace('\\', '/');
    }
}
