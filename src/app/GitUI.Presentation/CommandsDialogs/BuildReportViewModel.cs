using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>
///  The build report tab of a window (<c>BuildReportTabPageExtension</c>, in the main window and the file history): shown when
///  the revision has a build status with a report; the report in a web browser, or a link to open it in the default browser
///  when the build server does not show it in the tab (<c>ShowInBuildReportTab</c>).
/// </summary>
public sealed partial class BuildReportViewModel : ObservableObject, IDisposable
{
    private readonly IBrowseBuildReportHost? _host;
    private IBrowseWebView? _webView;
    private GitRevision? _revision;
    private string? _navigatedUrl;

    /// <param name="host">The application; none for no tab.</param>
    public BuildReportViewModel(IBrowseBuildReportHost? host)
    {
        _host = host;
    }

    public BuildReportStrings Strings { get; } = ViewStrings.Load<BuildReportStrings>();

    /// <summary>Whether the build report tab is shown.</summary>
    [ObservableProperty]
    public partial bool HasBuildReport { get; private set; }

    /// <summary>Whether the report is shown in the tab (else the "Open report" link).</summary>
    [ObservableProperty]
    public partial bool IsBuildReportInTab { get; private set; }

    /// <summary>The URL of the report of the revision.</summary>
    [ObservableProperty]
    public partial string? BuildReportUrl { get; private set; }

    /// <summary>The favicon of the report (PNG), shown in the header of the tab; none until a report was shown.</summary>
    [ObservableProperty]
    public partial byte[]? BuildReportIcon { get; private set; }

    /// <summary>The web browser of the report, created when a report is first shown in the tab.</summary>
    [ObservableProperty]
    public partial IEmbeddedNativeView? BuildReportView { get; private set; }

    /// <summary>Whether the tab is shown, which loads the report (<c>LoadReportContent</c> when the tab is selected).</summary>
    public bool IsTabShown
    {
        get;
        set
        {
            field = value;
            Fill();
        }
    }

    /// <summary>
    ///  As <c>FillBuildReport</c> and <c>SetSelectedRevision</c>: the tab follows the build status of the revision, which the
    ///  build server may report later.
    /// </summary>
    public void SetRevision(GitRevision? revision)
    {
        if (revision != _revision)
        {
            _revision?.PropertyChanged -= OnRevisionPropertyChanged;
            _revision = revision;
            _revision?.PropertyChanged += OnRevisionPropertyChanged;
        }

        Fill();
    }

    /// <summary>The "Open report" link: the report in the default browser.</summary>
    [RelayCommand]
    private void OpenBuildReport()
    {
        if (!string.IsNullOrWhiteSpace(BuildReportUrl))
        {
            _host?.OpenUrl(BuildReportUrl);
        }
    }

    public void Dispose()
    {
        _revision?.PropertyChanged -= OnRevisionPropertyChanged;
        _revision = null;
        _webView?.IconChanged -= OnIconChanged;
        _webView?.Dispose();
        _webView = null;
        BuildReportView = null;
    }

    private void OnRevisionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GitRevision.BuildStatus))
        {
            // Refresh the selected Git revision.
            Fill();
        }
    }

    // As BuildReportWebBrowserOnNavigated: the favicon of the page, kept when a page has none.
    private void OnIconChanged(object? sender, byte[]? icon)
    {
        if (icon is not null)
        {
            BuildReportIcon = icon;
        }
    }

    // As BuildReportTabPageExtension.FillBuildReport.
    private void Fill()
    {
        GitRevision? revision = _revision;
        bool buildResultPageEnabled = revision is not null && _host?.IsBuildReportEnabled is true;
        string? url = revision?.BuildStatus?.Url;
        if (!buildResultPageEnabled || string.IsNullOrEmpty(url))
        {
            if (HasBuildReport)
            {
                _webView?.Clear();
                _navigatedUrl = null;
            }

            HasBuildReport = false;
            BuildReportUrl = null;
            return;
        }

        // As SetTabPageContent: the web browser, or the link to the report.
        IsBuildReportInTab = revision!.BuildStatus!.ShowInBuildReportTab;
        BuildReportUrl = url;
        if (IsBuildReportInTab && _webView is null)
        {
            _webView = _host!.CreateWebView();
            BuildReportView = _webView?.View;
            _webView?.IconChanged += OnIconChanged;
        }

        HasBuildReport = true;

        // As LoadReportContent: the report is loaded when the tab is shown (its favicon then shown in the header of the tab).
        if (IsBuildReportInTab && IsTabShown && _navigatedUrl != url && _webView is { } webView)
        {
            try
            {
                _navigatedUrl = url;
                webView.Navigate(url);
            }
            catch (Exception)
            {
                // No propagation to the user if the report fails.
            }
        }
    }
}
