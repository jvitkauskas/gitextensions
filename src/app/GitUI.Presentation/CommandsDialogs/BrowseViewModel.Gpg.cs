using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands.Git.Gpg;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the GPG tab; ids match <c>RevisionGpgInfoControl</c>.</summary>
public sealed class GpgInfoStrings : ViewStrings
{
    public GpgInfoStrings()
        : base("RevisionGpgInfoControl")
    {
        CommitNotSigned = Add("_commitNotSigned", "Text", "Commit is not signed");
        TagNotSigned = Add("_tagNotSigned", "Text", "Tag is not signed");
        LoadingData = Add("_loadingDataText", "Text", "Loading data...", category: "TranslatedStrings");
        GpgTab = Add("GpgInfoTabPage", "Text", "GPG", category: "FormBrowse");
    }

    public TranslatedText CommitNotSigned { get; }

    public TranslatedText TagNotSigned { get; }

    public TranslatedText LoadingData { get; }

    public TranslatedText GpgTab { get; }
}

/// <summary>What the GPG tab needs from the application.</summary>
public interface IBrowseGpgHost
{
    /// <summary>Whether the tab is shown (<c>AppSettings.ShowGpgInformation</c>).</summary>
    bool ShowGpgInformation { get; }

    /// <summary>The signatures of the commit and its tags (<c>IGpgInfoProvider.LoadGpgInfoAsync</c>), none if neither is signed.</summary>
    Task<GpgInfo?> LoadGpgInfoAsync(GitRevision revision);
}

/// <summary>The GPG tab of the main window (<c>RevisionGpgInfoControl</c>, <c>FormBrowse.FillGpgInfoAsync</c>).</summary>
public sealed partial class BrowseViewModel
{
    private IBrowseGpgHost? _gpgHost;
    private GitRevision? _gpgRevision;
    private bool _gpgUpToDate;
    private int _gpgRequest;

    public GpgInfoStrings GpgStrings { get; } = ViewStrings.Load<GpgInfoStrings>();

    /// <summary>Whether the GPG tab is shown.</summary>
    public bool HasGpgInfo { get; private set; }

    /// <summary>The verification of the signature of the commit, or "Commit is not signed".</summary>
    [ObservableProperty]
    public partial string CommitGpgText { get; private set; } = "";

    /// <summary>The image of the signature of the commit (an asset name), if it is signed.</summary>
    [ObservableProperty]
    public partial string? CommitGpgIcon { get; private set; }

    /// <summary>The verification of the tags of the commit; null without a tag (the tag row is hidden).</summary>
    [ObservableProperty]
    public partial string? TagGpgText { get; private set; }

    [ObservableProperty]
    public partial string? TagGpgIcon { get; private set; }

    private void InitializeGpg()
    {
        _gpgHost = _host as IBrowseGpgHost;
        HasGpgInfo = _gpgHost?.ShowGpgInformation is true;
        ShowGpgPending();
    }

    // As FillGpgInfoAsync: verified when the tab is shown, once for each selected revision.
    private void UpdateGpgInfo(bool revisionChanged)
    {
        if (revisionChanged)
        {
            IReadOnlyList<GitRevision> selected = Grid.GetSelectedRevisionsLatestSelectedFirst();
            _gpgRevision = selected.Count == 0 ? null : selected[0];
            _gpgUpToDate = false;
        }

        if (!HasGpgInfo || _gpgUpToDate || SelectedTab != BrowseTab.Gpg || _gpgRevision is not { } revision)
        {
            return;
        }

        _gpgUpToDate = true;
        _ = LoadGpgInfoAsync(revision);
    }

    private async Task LoadGpgInfoAsync(GitRevision revision)
    {
        // Until the verification is done, neither the result of the previous revision nor "not signed" is shown.
        int request = ++_gpgRequest;
        ShowGpgPending();
        GpgInfo? info = await _gpgHost!.LoadGpgInfoAsync(revision);
        if (request == _gpgRequest)
        {
            ShowGpgInfo(info);
        }
    }

    private void ShowGpgPending()
    {
        CommitGpgIcon = null;
        CommitGpgText = GpgStrings.LoadingData.Text;
        TagGpgIcon = null;
        TagGpgText = null;
    }

    // As RevisionGpgInfoControl.DisplayGpgInfo.
    private void ShowGpgInfo(GpgInfo? info)
    {
        if (info is null)
        {
            CommitGpgIcon = null;
            CommitGpgText = GpgStrings.CommitNotSigned.Text;
            TagGpgIcon = null;
            TagGpgText = null;
            return;
        }

        CommitGpgIcon = info.CommitStatus switch
        {
            CommitStatus.GoodSignature => "CommitSignatureOk",
            CommitStatus.MissingPublicKey => "CommitSignatureWarning",
            CommitStatus.SignatureError => "CommitSignatureError",
            _ => null,
        };
        CommitGpgText = info.CommitStatus != CommitStatus.NoSignature ? NormalizeNewLines(info.CommitVerificationMessage) : GpgStrings.CommitNotSigned.Text;

        TagGpgIcon = info.TagStatus switch
        {
            TagStatus.OneGood => "TagOk",
            TagStatus.OneBad => "TagError",
            TagStatus.Many => "TagMany",
            TagStatus.NoPubKey => "TagWarning",
            _ => null,
        };

        // Without a tag the tag row is hidden; a tag that is not signed shows "not signed".
        TagGpgText = info.TagStatus switch
        {
            TagStatus.NoTag => null,
            TagStatus.TagNotSigned => GpgStrings.TagNotSigned.Text,
            _ => NormalizeNewLines(info.TagVerificationMessage ?? ""),
        };

        static string NormalizeNewLines(string text) => text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }
}
