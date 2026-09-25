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

/// <summary>A web browser shown as a native view (WebView2, or the WinForms <c>WebBrowserControl</c> without its runtime).</summary>
public interface IBrowseWebView : IDisposable
{
    IEmbeddedNativeView View { get; }

    void Navigate(string url);

    /// <summary>Stops loading and empties the page (as <c>FillBuildReport</c> when the tab is removed).</summary>
    void Clear();

    /// <summary>Raised with the icon of the page shown (PNG), or none, as the favicon of <c>BuildReportWebBrowserOnNavigated</c>.</summary>
    event EventHandler<byte[]?>? IconChanged
    {
        add { }
        remove { }
    }
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
///  The build report tab of the main window (<c>BuildReportTabPageExtension</c>, <c>FormBrowse.FillBuildReport</c>), for the
///  latest selected revision (<see cref="BuildReportViewModel"/>, whose properties the window binds as its own).
/// </summary>
public sealed partial class BrowseViewModel
{
    private BuildReportViewModel _buildReport = null!;

    /// <summary>The build report tab.</summary>
    public BuildReportViewModel BuildReport => _buildReport;

    public BuildReportStrings BuildReportStrings => _buildReport.Strings;

    /// <inheritdoc cref="BuildReportViewModel.HasBuildReport"/>
    public bool HasBuildReport => _buildReport.HasBuildReport;

    /// <inheritdoc cref="BuildReportViewModel.IsBuildReportInTab"/>
    public bool IsBuildReportInTab => _buildReport.IsBuildReportInTab;

    /// <inheritdoc cref="BuildReportViewModel.BuildReportUrl"/>
    public string? BuildReportUrl => _buildReport.BuildReportUrl;

    /// <inheritdoc cref="BuildReportViewModel.BuildReportIcon"/>
    public byte[]? BuildReportIcon => _buildReport.BuildReportIcon;

    /// <inheritdoc cref="BuildReportViewModel.BuildReportView"/>
    public IEmbeddedNativeView? BuildReportView => _buildReport.BuildReportView;

    /// <summary>The "Open report" link: the report in the default browser.</summary>
    public IRelayCommand OpenBuildReportCommand => _buildReport.OpenBuildReportCommand;

    private void InitializeBuildReport()
    {
        _buildReport = new BuildReportViewModel(_host as IBrowseBuildReportHost);

        // Its properties are the window's (the same names).
        _buildReport.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
    }

    private void DisposeBuildReport() => _buildReport.Dispose();

    // As FormBrowse.FillBuildReport and BuildReportTabPageExtension.SetSelectedRevision: the latest selected revision; the
    // report loaded when the tab is shown.
    private void UpdateBuildReport(bool revisionChanged)
    {
        if (_buildReport is null)
        {
            // In the constructor, before InitializeBuildReport.
            return;
        }

        if (revisionChanged)
        {
            IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
            _buildReport.SetRevision(selected.Count == 0 ? null : selected[0]);
        }

        _buildReport.IsTabShown = SelectedTab == BrowseTab.BuildReport;
    }
}
