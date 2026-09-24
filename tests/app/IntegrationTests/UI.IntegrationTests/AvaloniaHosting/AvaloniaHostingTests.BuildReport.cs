using System.Runtime.InteropServices;
using CommonTestUtils;
using GitUI.AvaloniaHosting;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Phase 8: the WebView2 browser of the build report tab, on a real WebView2 runtime (skipped without it).</summary>
public sealed partial class AvaloniaHostingTests
{
    [Test]
    public void The_WebView2_browser_of_the_build_report_queues_the_navigation_until_ready_and_follows_the_size_of_its_window()
    {
        if (!BrowseWebViews.IsWebView2Available(BrowseWebViews.GetAvailableBrowserVersion))
        {
            Assert.Ignore("The WebView2 runtime is not installed.");
        }

        string pageDirectory = Directory.CreateTempSubdirectory("GitExtensions.UITests.BuildReport-").FullName;
        string page = Path.Combine(pageDirectory, "report.html");
        File.WriteAllText(page, "<html><head><title>Build 42</title></head><body>#42 succeeded</body></html>");
        List<string> openedInBrowser = [];
        WebView2BrowseWebView webView = new(BrowseWebViews.UserDataFolder, openedInBrowser.Add);
        nint window = 0;
        try
        {
            // Requested before the host window and the controller exist.
            webView.Navigate(new Uri(page).AbsoluteUri);

            window = webView.Attach(_owner.Handle);
            MoveWindow(window, 0, 0, 640, 480, repaint: true);
            PumpUntil(() => webView.Initialization.IsCompleted);
            webView.InitializationError.Should().BeNull();
            webView.IsReady.Should().BeTrue();

            PumpUntil(() => webView.DocumentTitle == "Build 42");
            webView.DocumentTitle.Should().Be("Build 42", "the queued navigation is applied once the controller is ready");
            webView.Bounds.Should().Be(new Rectangle(0, 0, 640, 480));

            // The host (Avalonia's NativeControlHost) resizes the window: the controller follows.
            MoveWindow(window, 0, 0, 320, 200, repaint: true);
            webView.Bounds.Should().Be(new Rectangle(0, 0, 320, 200));

            // Cleared: a blank page.
            webView.Clear();
            PumpUntil(() => webView.LastCompletedNavigationUrl == "about:blank");
            webView.LastCompletedNavigationUrl.Should().Be("about:blank");

            // Parked when the tab is hidden, and the same window is attached again.
            webView.Detach();
            IsWindowVisible(window).Should().BeFalse();
            webView.Attach(_owner.Handle).Should().Be(window);
            IsWindowVisible(window).Should().BeTrue();
        }
        finally
        {
            webView.Dispose();
            PumpUntil(() => webView.Initialization.IsCompleted);
            Directory.Delete(pageDirectory, recursive: true);
        }

        IsWindow(window).Should().BeFalse("the host window is destroyed with the view");
        openedInBrowser.Should().BeEmpty();
    }

    /// <summary>Pumps the messages (WebView2 completes its operations with window messages) until the condition, or 30 seconds.</summary>
    private static void PumpUntil(Func<bool> condition, int seconds = 30)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            MessagePump.DoEvents();
            Thread.Sleep(10);
        }
    }

    /// <summary>The user data folder of WebView2 in the tests: in the temporary folder, never the user's.</summary>
    private static string TestWebView2UserDataFolder
        => Path.Combine(Path.GetTempPath(), "GitExtensions.UITests", $"WebView2-{Environment.ProcessId}");

    /// <summary>Deletes the user data folder of WebView2 of the tests, once its browser processes have released it.</summary>
    private static void DeleteTestWebView2UserDataFolder()
    {
        string folder = TestWebView2UserDataFolder;
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (Directory.Exists(folder))
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (DateTime.UtcNow > deadline)
                {
                    // Still in use: it is in the temporary folder anyway.
                    return;
                }

                MessagePump.DoEvents();
                Thread.Sleep(100);
            }
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveWindow(nint handle, int x, int y, int width, int height, [MarshalAs(UnmanagedType.Bool)] bool repaint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint handle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint handle);
}
