namespace GitUI;

/// <summary>The kinds of <c>git reset</c> of the reset branch dialog (<c>FormResetCurrentBranch.ResetType</c>).</summary>
public enum ResetCurrentBranchType
{
    Soft,
    Mixed,
    Keep,
    Merge,
    Hard
}
