using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the format patch dialog; ids match <c>FormFormatPatch</c>.</summary>
public sealed class FormatPatchStrings : ViewStrings
{
    public FormatPatchStrings()
        : base("FormFormatPatch")
    {
        Title = Add("$this", "Text", "Format patch");
        Patch = Add("lblPatches", "Text", "Patch:");
        Browse = Add("Browse", "Text", "Browse");
        CreatePatches = Add("FormatPatch", "Text", "Create patch(es)");
        CurrentBranch = Add("_currentBranchText", "Text", "Current branch:");
        NoOutputPathEntered = Add("_noOutputPathEnteredText", "Text", "You need to enter an output path.");
        RevisionsNeeded = Add("_revisionsNeededText", "Text", "You need to select at least one revision");
        RevisionsNeededCaption = Add("_revisionsNeededCaption", "Text", "Patch error");
        PatchResultCaption = Add("_patchResultCaption", "Text", "Patch result");
        FailCreatePatch = Add("_failCreatePatch", "Text", "Unable to create patch file(s)");
    }

    public TranslatedText Title { get; }

    public TranslatedText Patch { get; }

    public TranslatedText Browse { get; }

    public TranslatedText CreatePatches { get; }

    public TranslatedText CurrentBranch { get; }

    public TranslatedText NoOutputPathEntered { get; }

    public TranslatedText RevisionsNeeded { get; }

    public TranslatedText RevisionsNeededCaption { get; }

    public TranslatedText PatchResultCaption { get; }

    public TranslatedText FailCreatePatch { get; }
}

/// <summary>Operations of the format patch dialog that need the host (git, settings).</summary>
public interface IFormatPatchHost
{
    /// <summary>Runs <c>git format-patch</c> from..to into the directory; returns its output (as <c>GitModule.FormatPatch</c>).</summary>
    string FormatPatch(string from, string to, string outputPath, int start);

    /// <summary>Remembers an existing output directory (<c>AppSettings.LastFormatPatchDir</c>).</summary>
    void RememberOutputPath(string outputPath);
}

/// <summary>View model of the format patch dialog (port of <c>FormFormatPatch</c>).</summary>
public sealed partial class FormatPatchViewModel : DialogViewModel, IDisposable
{
    private readonly IFormatPatchHost _host;
    private readonly IMessageBoxService _messageBoxes;
    private readonly IFileDialogService _fileDialogs;
    private readonly string _errorCaption;
    private readonly bool _initialized;

    /// <param name="grid">The revision grid, with multi-selection and without artificial commits.</param>
    /// <param name="outputPath">The last output directory.</param>
    /// <param name="currentBranch">The checked out branch.</param>
    public FormatPatchViewModel(
        FormatPatchStrings strings,
        RevisionGridViewModel grid,
        string outputPath,
        string currentBranch,
        string errorCaption,
        IFormatPatchHost host,
        IMessageBoxService messageBoxes,
        IFileDialogService fileDialogs)
    {
        Strings = strings;
        Grid = grid;
        _errorCaption = errorCaption;
        _host = host;
        _messageBoxes = messageBoxes;
        _fileDialogs = fileDialogs;
        OutputPath = outputPath;
        CurrentBranchText = $"{strings.CurrentBranch.Text} {currentBranch}";
        _initialized = true;
    }

    public FormatPatchStrings Strings { get; }

    public RevisionGridViewModel Grid { get; }

    public string CurrentBranchText { get; }

    [ObservableProperty]
    public partial string OutputPath { get; set; }

    /// <summary>As <c>FormFormatPatch.OutputPath_TextChanged</c>, which is subscribed after the initial path is set.</summary>
    partial void OnOutputPathChanged(string value)
    {
        if (_initialized)
        {
            _host.RememberOutputPath(value);
        }
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (await _fileDialogs.PickFolderAsync() is { } folder)
        {
            OutputPath = folder;
        }
    }

    [RelayCommand]
    private void FormatPatch()
    {
        // As FormFormatPatch.FormatPatch_Click.
        if (string.IsNullOrEmpty(OutputPath))
        {
            _messageBoxes.ShowError(Strings.NoOutputPathEntered.Text, _errorCaption);
            return;
        }

        IReadOnlyList<GitRevision> revisions = Grid.GetSelectedRevisions(descending: true);
        if (revisions.Count == 0)
        {
            _messageBoxes.ShowError(Strings.RevisionsNeeded.Text, Strings.RevisionsNeededCaption.Text);
            return;
        }

        string result;
        if (revisions.Count <= 2)
        {
            // A single revision, or the range from the older to the newer one.
            result = _host.FormatPatch(FirstParent(revisions[0]), revisions[^1].Guid, OutputPath, start: 0);
        }
        else
        {
            result = "";
            int n = 0;
            foreach (GitRevision revision in revisions)
            {
                n++;
                result += _host.FormatPatch(FirstParent(revision), revision.Guid, OutputPath, start: n);
            }
        }

        if (string.IsNullOrEmpty(result))
        {
            _messageBoxes.ShowError(Strings.FailCreatePatch.Text, Strings.RevisionsNeededCaption.Text);
            return;
        }

        _messageBoxes.ShowInformation(result, Strings.PatchResultCaption.Text);
        Close(accepted: true);

        static string FirstParent(GitRevision revision)
            => revision.ParentIds is { Count: > 0 } parents ? parents[0].ToString() : "";
    }

    public void Dispose() => Grid.Dispose();
}
