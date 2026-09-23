using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Editor;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the view patch dialog; ids match <c>FormViewPatch</c>.</summary>
public sealed class ViewPatchStrings : ViewStrings
{
    public ViewPatchStrings()
        : base("FormViewPatch")
    {
        Title = Add("$this", "Text", "View patch file");
        Patch = Add("labelPatch", "Text", "Patch");
        Browse = Add("BrowsePatch", "Text", "Browse");
        ChangeColumn = Add("typeDataGridViewTextBoxColumn", "HeaderText", "Change");
        TypeColumn = Add("File", "HeaderText", "Type");
        FileNameColumn = Add("FileNameA", "HeaderText", "Filename");
        PatchFileFilter = Add("_patchFileFilterString", "Text", "Patch file (*.Patch)");
        PatchFileFilterTitle = Add("_patchFileFilterTitle", "Text", "Select patch file");
    }

    public TranslatedText Title { get; }

    public TranslatedText Patch { get; }

    public TranslatedText Browse { get; }

    public TranslatedText ChangeColumn { get; }

    public TranslatedText TypeColumn { get; }

    public TranslatedText FileNameColumn { get; }

    public TranslatedText PatchFileFilter { get; }

    public TranslatedText PatchFileFilterTitle { get; }
}

/// <summary>Operations of the view patch dialog that need the host (the file system).</summary>
public interface IViewPatchHost
{
    /// <summary>Picks a patch file; returns <see langword="null"/> if cancelled.</summary>
    string? BrowsePatchFile(string filter, string title);

    /// <summary>Reads the patches of the file (as <c>FormViewPatch.LoadPatchFile</c>); throws if it cannot be read.</summary>
    IReadOnlyList<Patch> LoadPatches(string path);
}

/// <summary>View model of the view patch dialog (port of <c>FormViewPatch</c>).</summary>
public sealed partial class ViewPatchViewModel : DialogViewModel
{
    private readonly IViewPatchHost _host;

    public ViewPatchViewModel(ViewPatchStrings strings, IViewPatchHost host, string? patchFile = null)
    {
        Strings = strings;
        _host = host;
        if (!string.IsNullOrEmpty(patchFile))
        {
            PatchFile = patchFile;
            LoadPatchFile();
        }
    }

    public ViewPatchStrings Strings { get; }

    [ObservableProperty]
    public partial string PatchFile { get; set; } = "";

    public ObservableCollection<Patch> Patches { get; } = [];

    [ObservableProperty]
    public partial Patch? SelectedPatch { get; set; }

    /// <summary>The diff of the selected patch.</summary>
    public TextEditorViewModel Diff { get; } = new();

    partial void OnSelectedPatchChanged(Patch? value)
    {
        // As FormViewPatch.GridChangedFiles_SelectionChanged, which keeps the diff shown when nothing is selected.
        if (value is not null)
        {
            Diff.LoadDiff(value.Text ?? "");
        }
    }

    [RelayCommand]
    private void Browse()
    {
        if (_host.BrowsePatchFile($"{Strings.PatchFileFilter.Text}|*.patch", Strings.PatchFileFilterTitle.Text) is { } path)
        {
            PatchFile = path;
        }

        LoadPatchFile();
    }

    /// <summary>As <c>FormViewPatch.LoadPatchFile</c>, which ignores a file that cannot be read.</summary>
    public void LoadPatchFile()
    {
        try
        {
            IReadOnlyList<Patch> patches = _host.LoadPatches(PatchFile);
            Patches.Clear();
            foreach (Patch patch in patches)
            {
                Patches.Add(patch);
            }

            SelectedPatch = Patches.FirstOrDefault();
        }
        catch
        {
            // As FormViewPatch.
        }
    }
}
