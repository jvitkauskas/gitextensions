using GitUI.Presentation.UserControls;
using GitUI.Presentation.UserControls.Blame;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>View model of the blame dialog (port of <c>FormBlame</c>): the blame of a file in a revision.</summary>
public sealed class BlameDialogViewModel : DialogViewModel
{
    private readonly GitRevision _revision;
    private readonly int? _initialLine;

    public BlameDialogViewModel(BlameStrings strings, IBlameHost host, ICommitInfoHost commitInfoHost, string fileName, GitRevision revision, int? initialLine = null)
    {
        _revision = revision;
        _initialLine = initialLine;
        FileName = fileName;
        Blame = new BlameViewModel(strings, host, commitInfoHost);
    }

    public string FileName { get; }

    /// <summary>As <c>FormBlameLoad</c> (not translated there either).</summary>
    public string Title => $"Blame ({FileName})";

    public BlameViewModel Blame { get; }

    public Task InitializeAsync() => Blame.LoadAsync(_revision, children: null, FileName, _initialLine);
}
