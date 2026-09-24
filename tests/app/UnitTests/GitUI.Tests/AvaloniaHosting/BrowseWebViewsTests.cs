using GitUI.AvaloniaHosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using Microsoft.Web.WebView2.Core;

namespace GitUITests.AvaloniaHosting;

[TestFixture]
public sealed class BrowseWebViewsTests
{
    [Test]
    public void The_build_report_is_shown_with_WebView2_when_its_runtime_is_installed()
    {
        FakeWebView webView2 = new();

        IBrowseWebView? created = BrowseWebViews.Create(() => "140.0.3485.54", () => webView2);

        created.Should().BeSameAs(webView2);
    }

    [Test]
    public void The_build_report_has_no_browser_without_the_WebView2_runtime()
    {
        FakeWebView webView2 = new();

        IBrowseWebView? created = BrowseWebViews.Create(() => throw new WebView2RuntimeNotFoundException(), () => webView2);

        created.Should().BeNull();
    }

    [Test]
    public void The_build_report_has_no_browser_without_the_WebView2_loader()
    {
        BrowseWebViews.IsWebView2Available(() => throw new DllNotFoundException("WebView2Loader.dll")).Should().BeFalse();
        BrowseWebViews.IsWebView2Available(() => throw new BadImageFormatException()).Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("")]
    public void The_build_report_has_no_browser_without_a_runtime_version(string? version)
    {
        BrowseWebViews.IsWebView2Available(() => version).Should().BeFalse();
    }

    [Test]
    public void The_user_data_folder_of_WebView2_is_in_the_local_application_data()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        BrowseWebViews.DefaultUserDataFolder.Should().Be(Path.Join(localApplicationData, "GitExtensions", "WebView2"));
    }

    [Test]
    public void The_navigations_requested_before_the_browser_is_ready_are_applied_once_it_is()
    {
        WebViewNavigationQueue queue = new();
        FakeNavigation browser = new();

        queue.Navigate("https://ci.example.com/build/41");
        queue.Navigate("https://ci.example.com/build/42");

        queue.IsReady.Should().BeFalse();
        queue.PendingUrl.Should().Be("https://ci.example.com/build/42", "the last navigation replaces the previous one");

        queue.SetReady(browser);

        queue.IsReady.Should().BeTrue();
        queue.PendingUrl.Should().BeNull();
        browser.Calls.Should().Equal("navigate https://ci.example.com/build/42");

        // Once ready, the navigations are applied directly.
        queue.Navigate("https://ci.example.com/build/43");
        queue.Clear();

        browser.Calls.Should().Equal("navigate https://ci.example.com/build/42", "navigate https://ci.example.com/build/43", "clear");
    }

    [Test]
    public void Clearing_before_the_browser_is_ready_drops_the_pending_navigation()
    {
        WebViewNavigationQueue queue = new();
        FakeNavigation browser = new();

        queue.Navigate("https://ci.example.com/build/42");
        queue.Clear();
        queue.SetReady(browser);

        queue.PendingUrl.Should().BeNull();
        browser.Calls.Should().BeEmpty("the new browser is blank");
    }

    [Test]
    public void No_navigation_is_applied_once_the_browser_is_gone()
    {
        WebViewNavigationQueue queue = new();
        FakeNavigation browser = new();
        queue.SetReady(browser);

        queue.Reset();
        queue.Navigate("https://ci.example.com/build/42");

        queue.IsReady.Should().BeFalse();
        browser.Calls.Should().BeEmpty();
    }

    private sealed class FakeNavigation : IWebViewNavigation
    {
        public List<string> Calls { get; } = [];

        public void Navigate(string url) => Calls.Add($"navigate {url}");

        public void Clear() => Calls.Add("clear");
    }

    private sealed class FakeWebView : IBrowseWebView, IEmbeddedNativeView
    {
        public IEmbeddedNativeView View => this;

        public void Navigate(string url)
        {
        }

        public void Clear()
        {
        }

        public nint Attach(nint parentWindow) => 0;

        public void Detach()
        {
        }

        public void Dispose()
        {
        }
    }
}
