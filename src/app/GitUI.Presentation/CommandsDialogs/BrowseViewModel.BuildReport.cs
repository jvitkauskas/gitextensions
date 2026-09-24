using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the build report tab; ids match <c>FormBrowse</c> and <c>TranslatedStrings</c>.</summary>
public sealed class BuildReportStrings : ViewStrings
{
    public BuildReportStrings()
        : base("FormBrowse")
    {
        BuildReportTab = Add("_buildReportTabCaption", "Text", "Build Report");
        OpenReport = Add("_openReport", "Text", "Open report", category: "TranslatedStrings");
    }

    public TranslatedText BuildReportTab { get; }

    public TranslatedText OpenReport { get; }
}

/// <summary>A web browser shown as a native view (the WinForms <c>WebBrowserControl</c> until the switch to WebView2).</summary>
public interface IBrowseWebView : IDisposable
{
    IEmbeddedNativeView View { get; }

    void Navigate(string url);

    /// <summary>Stops loading and empties the page (as <c>FillBuildReport</c> when the tab is removed).</summary>
    void Clear();
}

/// <summary>What the build report tab needs from the application.</summary>
public interface IBrowseBuildReportHost
{
    /// <summary>Whether the tab is shown for revisions with a build report (<c>BuildServerSettings.ShowBuildResultPage</c>).</summary>
    bool IsBuildReportEnabled { get; }

    /// <summary>The web browser of the tab, if it can be created.</summary>
    IBrowseWebView? CreateWebView();

    /// <summary>Opens a URL in the default browser.</summary>
    void OpenUrl(string url);
}

/// <summary>
///  The build report tab of the main window (<c>BuildReportTabPageExtension</c>, <c>FormBrowse.FillBuildReport</c>): shown when
///  the selected revision has a build status with a report; the report in a web browser, or a link to open it in the
///  default browser when the build server does not show it in the tab (<c>ShowInBuildReportTab</c>).
/// </summary>
public sealed partial class BrowseViewModel
{
    private IBrowseBuildReportHost? _buildReportHost;
    private IBrowseWebView? _buildReportWebView;
    private GitRevision? _buildReportRevision;
    private string? _navigatedBuildReportUrl;

    public BuildReportStrings BuildReportStrings { get; } = ViewStrings.Load<BuildReportStrings>();

    /// <summary>Whether the build report tab is shown.</summary>
    [ObservableProperty]
    public partial bool HasBuildReport { get; private set; }

    /// <summary>Whether the report is shown in the tab (else the "Open report" link).</summary>
    [ObservableProperty]
    public partial bool IsBuildReportInTab { get; private set; }

    /// <summary>The URL of the report of the selected revision.</summary>
    [ObservableProperty]
    public partial string? BuildReportUrl { get; private set; }

    /// <summary>The web browser of the report, created when a report is first shown in the tab.</summary>
    [ObservableProperty]
    public partial IEmbeddedNativeView? BuildReportView { get; private set; }

    /// <summary>The "Open report" link: the report in the default browser.</summary>
    [RelayCommand]
    private void OpenBuildReport()
    {
        if (!string.IsNullOrWhiteSpace(BuildReportUrl))
        {
            _buildReportHost?.OpenUrl(BuildReportUrl);
        }
    }

    private void InitializeBuildReport() => _buildReportHost = _host as IBrowseBuildReportHost;

    private void DisposeBuildReport()
    {
        _buildReportRevision?.PropertyChanged -= OnBuildReportRevisionPropertyChanged;
        _buildReportRevision = null;
        _buildReportWebView?.Dispose();
        _buildReportWebView = null;
        BuildReportView = null;
    }

    // As FormBrowse.FillBuildReport and BuildReportTabPageExtension.SetSelectedRevision: the tab follows the build status
    // of the selected revision, which the build server may report later.
    private void UpdateBuildReport(bool revisionChanged)
    {
        if (revisionChanged)
        {
            IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
            GitRevision? revision = selected.Count == 0 ? null : selected[0];
            if (revision != _buildReportRevision)
            {
                _buildReportRevision?.PropertyChanged -= OnBuildReportRevisionPropertyChanged;
                _buildReportRevision = revision;
                _buildReportRevision?.PropertyChanged += OnBuildReportRevisionPropertyChanged;
            }
        }

        FillBuildReport();
    }

    private void OnBuildReportRevisionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GitRevision.BuildStatus))
        {
            // Refresh the selected Git revision.
            FillBuildReport();
        }
    }

    // As BuildReportTabPageExtension.FillBuildReport.
    private void FillBuildReport()
    {
        GitRevision? revision = _buildReportRevision;
        bool buildResultPageEnabled = revision is not null && _buildReportHost?.IsBuildReportEnabled is true;
        string? url = revision?.BuildStatus?.Url;
        if (!buildResultPageEnabled || string.IsNullOrEmpty(url))
        {
            if (HasBuildReport)
            {
                _buildReportWebView?.Clear();
                _navigatedBuildReportUrl = null;
            }

            HasBuildReport = false;
            BuildReportUrl = null;
            return;
        }

        // As SetTabPageContent: the web browser, or the link to the report.
        IsBuildReportInTab = revision!.BuildStatus!.ShowInBuildReportTab;
        BuildReportUrl = url;
        if (IsBuildReportInTab && _buildReportWebView is null)
        {
            _buildReportWebView = _buildReportHost!.CreateWebView();
            BuildReportView = _buildReportWebView?.View;
        }

        HasBuildReport = true;

        // As LoadReportContent: the report is loaded when the tab is shown (the favicon of the tab is not shown).
        if (IsBuildReportInTab && SelectedTab == BrowseTab.BuildReport && _navigatedBuildReportUrl != url && _buildReportWebView is { } webView)
        {
            try
            {
                _navigatedBuildReportUrl = url;
                webView.Navigate(url);
            }
            catch (Exception)
            {
                // No propagation to the user if the report fails.
            }
        }
    }
}
