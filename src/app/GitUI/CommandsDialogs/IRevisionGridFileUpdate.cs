using GitExtensions.Extensibility.Git;

namespace GitUI.CommandsDialogs;

public interface IRevisionGridFileUpdate
{
    /// <summary>
    ///  Tries to select the revision having <paramref name="commitId"/> in the <c>RevisionGridControl</c>
    ///  and stores the <paramref name="filename"/> for selection in the <c>FileStatusList</c> after asynchronous loading of the revision.
    /// </summary>
    /// <returns><c>true</c> if the revision was found.</returns>
    bool SelectFileInRevision(ObjectId commitId, RelativePath filename);
}
