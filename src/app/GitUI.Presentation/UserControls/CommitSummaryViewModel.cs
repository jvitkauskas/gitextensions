using CommunityToolkit.Mvvm.ComponentModel;
using GitUI.Presentation.Translations;
using ResourceManager;

namespace GitUI.Presentation.UserControls;

/// <summary>Strings of the commit summary; ids match <c>CommitSummaryUserControl</c>.</summary>
public sealed class CommitSummaryStrings : ViewStrings
{
    public CommitSummaryStrings()
        : base("CommitSummaryUserControl")
    {
        Tags = Add("labelTagsCaption", "Text", "Tag(s):");
        Branches = Add("labelBranchesCaption", "Text", "Branch(es):");
        NoRevision = Add("_noRevision", "Text", "No revision");
        NotAvailable = Add("_notAvailable", "Text", "n/a");
    }

    public TranslatedText Tags { get; }

    public TranslatedText Branches { get; }

    public TranslatedText NoRevision { get; }

    public TranslatedText NotAvailable { get; }

    /// <summary>The (application-wide translated) author caption.</summary>
    public string Author => TranslatedStrings.Author + ":";

    /// <summary>The (application-wide translated) commit date caption.</summary>
    public string CommitDate => TranslatedStrings.CommitDate + ":";
}

/// <summary>What the commit summary shows of a commit.</summary>
/// <param name="Date">The commit date, formatted for display.</param>
public sealed record CommitSummary(string ShortId, string Author, string Date, string Subject, IReadOnlyList<string> Tags, IReadOnlyList<string> Branches);

/// <summary>A parent of a merge commit, as listed to choose the mainline of a cherry-pick or revert.</summary>
public sealed record ParentCommit(int Number, string Subject, string Author, string Date);

/// <summary>A commit to act on, with what the dialogs show of it.</summary>
/// <param name="Parents">The parents, if the commit is a merge; otherwise empty.</param>
public sealed record RevisionInfo(string Guid, CommitSummary Summary, IReadOnlyList<ParentCommit> Parents)
{
    public bool IsMerge => Parents.Count > 1;
}

/// <summary>View model of the commit summary box (port of <c>CommitSummaryUserControl</c>).</summary>
public sealed partial class CommitSummaryViewModel : ObservableObject
{
    private const int MaxBranchTagLength = 75;

    public CommitSummaryViewModel(CommitSummaryStrings strings, CommitSummary? summary = null)
    {
        Strings = strings;
        Summary = summary;
    }

    public CommitSummaryStrings Strings { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(Author), nameof(Date), nameof(Subject), nameof(TagsText), nameof(BranchesText), nameof(HasTags), nameof(HasBranches))]
    public partial CommitSummary? Summary { get; set; }

    /// <summary>The short commit id, the title of the box.</summary>
    public string Title => Summary?.ShortId ?? Strings.NoRevision.Text;

    public string Author => Summary?.Author ?? "---";

    public string Date => Summary?.Date ?? "---";

    public string Subject => Summary?.Subject ?? "---";

    public bool HasTags => Summary?.Tags.Count > 0;

    public bool HasBranches => Summary?.Branches.Count > 0;

    public string TagsText => FormatRefs(Summary?.Tags);

    public string BranchesText => FormatRefs(Summary?.Branches);

    private string FormatRefs(IReadOnlyList<string>? refs)
    {
        if (refs is null)
        {
            return "---";
        }

        if (refs.Count == 0)
        {
            return Strings.NotAvailable.Text;
        }

        return string.Join(", ", refs).ShortenTo(MaxBranchTagLength);
    }
}
