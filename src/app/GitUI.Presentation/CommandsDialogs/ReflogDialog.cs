using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the reflog dialog; ids match <c>FormReflog</c>.</summary>
public sealed class ReflogStrings : ViewStrings
{
    public ReflogStrings()
        : base("FormReflog")
    {
        Title = Add("$this", "Text", "Reflog");
        DisplayReflogFor = Add("label1", "Text", "Display reflog for:");
        Reference = Add("label2", "Text", "Reference:");
        Head = Add("linkHead", "Text", "HEAD");
        DirtyWorkingDirectory = Add("lblDirtyWorkingDirectory", "Text", "Warning: you've got changes in your working directory that could be lost if you want to reset the current branch to another commit.\nStash them before if you don't want to lose them.");
        Sha = Add("Sha", "HeaderText", "SHA-1");
        Ref = Add("Ref", "HeaderText", "Ref");
        Action = Add("Action", "HeaderText", "Action");
        CopySha = Add("copySha1ToolStripMenuItem", "Text", "Copy SHA-1");
        CreateBranch = Add("createABranchOnThisCommitToolStripMenuItem", "Text", "Create a branch on this commit...");
        ResetCurrentBranch = Add("resetCurrentBranchOnThisCommitToolStripMenuItem", "Text", "Reset current branch to this commit...");
        ContinueResetWithChanges = Add("_continueResetCurrentBranchEvenWithChangesText", "Text", "You have changes in your working directory that could be lost.\n\nDo you want to continue?");
        ContinueResetWithChangesCaption = Add("_continueResetCurrentBranchCaptionText", "Text", "Changes not committed...");
    }

    public TranslatedText Title { get; }

    public TranslatedText DisplayReflogFor { get; }

    public TranslatedText Reference { get; }

    public TranslatedText Head { get; }

    public TranslatedText DirtyWorkingDirectory { get; }

    public TranslatedText Sha { get; }

    public TranslatedText Ref { get; }

    public TranslatedText Action { get; }

    public TranslatedText CopySha { get; }

    public TranslatedText CreateBranch { get; }

    public TranslatedText ResetCurrentBranch { get; }

    public TranslatedText ContinueResetWithChanges { get; }

    public TranslatedText ContinueResetWithChangesCaption { get; }
}

/// <summary>A line of <c>git reflog</c> (<c>RefLine</c>).</summary>
public sealed record ReflogEntry(string Sha, string Ref, string Action);

/// <summary>Operations of the reflog dialog that need the host (git and the other dialogs).</summary>
public interface IReflogHost
{
    /// <summary>Runs <c>git reflog --no-abbrev</c> for <paramref name="reference"/> in the background, reporting its output on the UI thread.</summary>
    void LoadReflog(string reference, Action<string> report);

    /// <summary>Shows the create branch dialog on the commit; returns whether a branch was created.</summary>
    bool CreateBranch(string sha);

    /// <summary>Shows the reset current branch dialog on the commit, with a soft or hard reset preselected.</summary>
    bool ResetCurrentBranch(string sha, bool soft);

    void CopyToClipboard(string text);
}

/// <summary>View model of the reflog dialog (port of <c>FormReflog</c>).</summary>
public sealed partial class ReflogViewModel : DialogViewModel
{
    private readonly string _currentBranch;
    private readonly IReflogHost _host;
    private readonly IMessageBoxService _messageBoxes;

    /// <param name="references">HEAD, then the local and the remote branches.</param>
    /// <param name="currentBranch">The checked out branch, or the detached HEAD marker.</param>
    /// <param name="isBranchCheckedOut">Whether a branch (not a detached HEAD) is checked out.</param>
    /// <param name="isDirty">Whether the working directory has changes a reset could lose.</param>
    public ReflogViewModel(
        ReflogStrings strings,
        IReadOnlyList<string> references,
        string currentBranch,
        bool isBranchCheckedOut,
        bool isDirty,
        IReflogHost host,
        IMessageBoxService messageBoxes)
    {
        Strings = strings;
        References = references;
        _currentBranch = currentBranch;
        IsBranchCheckedOut = isBranchCheckedOut;
        IsDirty = isDirty;
        _host = host;
        _messageBoxes = messageBoxes;

        // As FormReflog, which does not translate it.
        CurrentBranchText = $"current branch ({currentBranch})";
        SelectedReference = references.FirstOrDefault();
    }

    public ReflogStrings Strings { get; }

    public IReadOnlyList<string> References { get; }

    public string CurrentBranchText { get; }

    public bool IsBranchCheckedOut { get; }

    public bool IsDirty { get; }

    public ObservableCollection<ReflogEntry> Entries { get; } = [];

    [ObservableProperty]
    public partial string? SelectedReference { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyShaCommand), nameof(CreateBranchCommand), nameof(ResetCurrentBranchCommand))]
    public partial ReflogEntry? SelectedEntry { get; set; }

    partial void OnSelectedReferenceChanged(string? value)
    {
        if (value is null)
        {
            return;
        }

        _host.LoadReflog(value, output =>
        {
            // A later selection wins over the output of an earlier one.
            if (value != SelectedReference)
            {
                return;
            }

            Entries.Clear();
            foreach (ReflogEntry entry in Parse(output))
            {
                Entries.Add(entry);
            }
        });
    }

    /// <summary>Parses the output of <c>git reflog --no-abbrev</c> (as <c>FormReflog.ConvertReflogOutput</c>).</summary>
    public static IEnumerable<ReflogEntry> Parse(string output)
        => from line in output.Split('\n')
           where line.Length != 0
           select ReflogRegex.Match(line)
           into match
           where match.Success
           select new ReflogEntry(match.Groups["sha"].Value, match.Groups["ref"].Value, match.Groups["action"].Value);

    [GeneratedRegex(@"^(?<sha>[^ ]+) (?<ref>[^:]+): (?<action>.+)$", RegexOptions.ExplicitCapture)]
    private static partial Regex ReflogRegex { get; }

    [RelayCommand]
    private void ShowHead() => SelectedReference = References.FirstOrDefault();

    [RelayCommand]
    private void ShowCurrentBranch() => SelectedReference = _currentBranch;

    private bool HasSelectedEntry() => SelectedEntry is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedEntry))]
    private void CopySha() => _host.CopyToClipboard(SelectedEntry!.Sha);

    [RelayCommand(CanExecute = nameof(HasSelectedEntry))]
    private void CreateBranch() => _host.CreateBranch(SelectedEntry!.Sha);

    private bool CanResetCurrentBranch() => SelectedEntry is not null && IsBranchCheckedOut;

    [RelayCommand(CanExecute = nameof(CanResetCurrentBranch))]
    private void ResetCurrentBranch()
    {
        if (IsDirty && !_messageBoxes.Confirm(Strings.ContinueResetWithChanges.Text, Strings.ContinueResetWithChangesCaption.Text, defaultNo: true))
        {
            return;
        }

        // A dirty working directory preselects a soft reset, which keeps the changes.
        _host.ResetCurrentBranch(SelectedEntry!.Sha, soft: IsDirty);
    }
}
