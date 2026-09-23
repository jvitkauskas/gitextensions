using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the add submodule dialog; ids match <c>FormAddSubmodule</c>.</summary>
public sealed class AddSubmoduleStrings : ViewStrings
{
    public AddSubmoduleStrings()
        : base("FormAddSubmodule")
    {
        Title = Add("$this", "Text", "Add submodule");
        PathToSubmodule = Add("label1", "Text", "Path to submodule");
        LocalPath = Add("label2", "Text", "Local path");
        Branch = Add("label3", "Text", "Branch");
        Force = Add("chkForce", "Text", "Force");
        Browse = Add("Browse", "Text", "Browse");
        AddSubmodule = Add("Add", "Text", "Add");
        RemoteAndLocalPathRequired = Add("_remoteAndLocalPathRequired", "Text", "A remote path and local path are required");
    }

    public TranslatedText Title { get; }

    public TranslatedText PathToSubmodule { get; }

    public TranslatedText LocalPath { get; }

    public TranslatedText Branch { get; }

    public TranslatedText Force { get; }

    public TranslatedText Browse { get; }

    public TranslatedText AddSubmodule { get; }

    public TranslatedText RemoteAndLocalPathRequired { get; }
}

/// <summary>Strings of the clean working directory dialog; ids match <c>FormCleanupRepository</c>.</summary>
public sealed class CleanupRepositoryStrings : ViewStrings
{
    public CleanupRepositoryStrings()
        : base("FormCleanupRepository")
    {
        Title = Add("$this", "Text", "Clean working directory");
        RemoveUntracked = Add("groupBox1", "Text", "Remove untracked files from working directory");
        RemoveAll = Add("RemoveAll", "Text", "Remove all untracked files");
        RemoveNonIgnored = Add("RemoveNonIgnored", "Text", "Remove only non-ignored untracked files");
        RemoveIgnored = Add("RemoveIgnored", "Text", "Remove only ignored untracked files");
        RemoveDirectories = Add("RemoveDirectories", "Text", "Remove untracked directories");
        CleanSubmodules = Add("CleanSubmodules", "Text", "Clean submodules recursively");
        IncludePathFilter = Add("checkBoxIncludePathFilter", "Text", "Affect the following directory path(s) only:");
        IncludePathHint = Add("labelPathHintInclude", "Text", "(one path per line)");
        AddIncludePath = Add("AddInclusivePath", "Text", "Add a path...");
        ExcludePathFilter = Add("checkBoxExcludePathFilter", "Text", "Exclude the following file path(s):");
        ExcludePathHint = Add("labelPathHintExclude", "Text", "(one path per line)");
        AddExcludePath = Add("AddExclusivePath", "Text", "Add a path...");
        Log = Add("label1", "Text", "Log:");
        Preview = Add("Preview", "Text", "Preview");
        Cleanup = Add("Cleanup", "Text", "Cleanup");
        ReallyCleanupQuestion = Add("_reallyCleanupQuestion", "Text", "Are you sure you want to cleanup the repository?");
        ReallyCleanupQuestionCaption = Add("_reallyCleanupQuestionCaption", "Text", "Cleanup");
    }

    public TranslatedText Title { get; }

    public TranslatedText RemoveUntracked { get; }

    public TranslatedText RemoveAll { get; }

    public TranslatedText RemoveNonIgnored { get; }

    public TranslatedText RemoveIgnored { get; }

    public TranslatedText RemoveDirectories { get; }

    public TranslatedText CleanSubmodules { get; }

    public TranslatedText IncludePathFilter { get; }

    public TranslatedText IncludePathHint { get; }

    public TranslatedText AddIncludePath { get; }

    public TranslatedText ExcludePathFilter { get; }

    public TranslatedText ExcludePathHint { get; }

    public TranslatedText AddExcludePath { get; }

    public TranslatedText Log { get; }

    public TranslatedText Preview { get; }

    public TranslatedText Cleanup { get; }

    public TranslatedText ReallyCleanupQuestion { get; }

    public TranslatedText ReallyCleanupQuestionCaption { get; }

    /// <summary>The (not translated) caption of the close button, <c>_NO_TRANSLATE_Close</c>.</summary>
    public string Close => "Close";
}

/// <summary>Strings of the submodule conflict dialog; ids match <c>FormMergeSubmodule</c>.</summary>
public sealed class MergeSubmoduleStrings : ViewStrings
{
    public MergeSubmoduleStrings()
        : base("FormMergeSubmodule")
    {
        Title = Add("$this", "Text", "Submodule conflict");
        ConflictOnSubmodule = Add("label2", "Text", "There is a conflict on the submodule:");
        Base = Add("label1", "Text", "Base:");
        Local = Add("label3", "Text", "Local:");
        Remote = Add("label4", "Text", "Remote:");
        Current = Add("label5", "Text", "Current:");
        StageCurrent = Add("btStageCurrent", "Text", "Stage Current");
        CheckoutBranch = Add("btCheckoutBranch", "Text", "Checkout Branch");
        OpenSubmodule = Add("btOpenSubmodule", "Text", "Open submodule");
        Deleted = Add("_deleted", "Text", "deleted");
        StageFilename = Add("_stageFilename", "Text", "Stage {0}");
    }

    public TranslatedText Title { get; }

    public TranslatedText ConflictOnSubmodule { get; }

    public TranslatedText Base { get; }

    public TranslatedText Local { get; }

    public TranslatedText Remote { get; }

    public TranslatedText Current { get; }

    public TranslatedText StageCurrent { get; }

    public TranslatedText CheckoutBranch { get; }

    public TranslatedText OpenSubmodule { get; }

    public TranslatedText Deleted { get; }

    public TranslatedText StageFilename { get; }
}

/// <summary>Strings of the create worktree dialog; ids match <c>FormCreateWorktree</c> and its <c>FolderBrowserButton</c>.</summary>
public sealed class CreateWorktreeStrings : ViewStrings
{
    public CreateWorktreeStrings()
        : base("FormCreateWorktree")
    {
        Title = Add("$this", "Text", "Create a new worktree");
        WhatToCheckout = Add("gbxWhatToCheckout", "Text", "What to checkout:");
        CheckoutExistingBranch = Add("rbCheckoutExistingBranch", "Text", "Checkout an &existing branch:");
        CreateNewBranch = Add("rbCreateNewBranch", "Text", "Create a &new branch:\r\n(from current commit)");
        NewWorktreeDirectory = Add("lblNewWorktreeFolder", "Text", "New worktree &directory:");
        Create = Add("btnCreateWorktree", "Text", "&Create the new worktree");
        Browse = Add("buttonBrowse", "Text", "&Browse...", category: "FolderBrowserButton");
    }

    public TranslatedText Title { get; }

    public TranslatedText WhatToCheckout { get; }

    public TranslatedText CheckoutExistingBranch { get; }

    public TranslatedText CreateNewBranch { get; }

    public TranslatedText NewWorktreeDirectory { get; }

    public TranslatedText Create { get; }

    public TranslatedText Browse { get; }
}

/// <summary>Strings of the open local repository dialog; ids match <c>FormOpenDirectory</c>.</summary>
public sealed class OpenDirectoryStrings : ViewStrings
{
    public OpenDirectoryStrings()
        : base("FormOpenDirectory")
    {
        Title = Add("$this", "Text", "Open local repository");
        Directory = Add("label1", "Text", "&Directory:");
        Browse = Add("folderBrowserButton", "Text", "&Browse...");
        GoUpTooltip = Add("folderGoUpButton", "toolTip1", "Go to parent directory...");
        Open = Add("Load", "Text", "Open");
        OpenFailed = Add("_warningOpenFailed", "Text", "The selected directory is not a valid git repository.");
    }

    public TranslatedText Title { get; }

    public TranslatedText Directory { get; }

    public TranslatedText Browse { get; }

    public TranslatedText GoUpTooltip { get; }

    public TranslatedText Open { get; }

    public TranslatedText OpenFailed { get; }
}
