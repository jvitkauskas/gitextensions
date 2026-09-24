using System.ComponentModel;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GitCommands;
using GitCommands.ExternalLinks;
using GitCommands.Git;
using GitCommands.Remotes;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI;
using GitExtUtils.GitUI.Theming;
using GitUI.CommandsDialogs;
using GitUI.UserControls;
using GitUIPluginInterfaces;
using Microsoft;
using Microsoft.VisualStudio.Threading;
using ResourceManager;
using ResourceManager.CommitDataRenders;

namespace GitUI.CommitInfo;

// The orders of the tags and branches of the commit info (moved out of the WinForms CommitInfo control).

internal sealed class TagsComparer : IComparer<string>
{
    private readonly IDictionary<string, int> _orderDict;
    private readonly string _prefix;

    public TagsComparer(IDictionary<string, int> orderDict, string prefix = "refs/tags/")
    {
        _orderDict = orderDict;
        _prefix = prefix;
    }

    public int Compare(string? a, string? b)
    {
        return b is null ? -1 : a is null ? 1 : IndexOf(a) - IndexOf(b);

        int IndexOf(string s)
        {
            if (s.StartsWith("remotes/"))
            {
                s = "refs/" + s;
            }
            else
            {
                s = _prefix + s;
            }

            if (_orderDict.TryGetValue(s, out int index))
            {
                return index;
            }

            return -1;
        }
    }
}

internal sealed class BranchComparer : IComparer<string>
{
    private const string _remoteBranchPrefix = "remotes/";
    private readonly string _currentBranch;
    private readonly bool _isDetachedHead;
    private readonly Dictionary<string, int> _orderByBranch = [];

    public BranchComparer(string[] branches, string currentBranch)
    {
        _currentBranch = currentBranch;
        _isDetachedHead = DetachedHeadParser.IsDetachedHead(currentBranch);
        string[] branchRegexes = AppSettings.PrioritizedBranchNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] localBranchRegexes = [.. branchRegexes.Select(regex => $"^({regex})$")];
        string[] remoteBranchRegexes = [.. branchRegexes.Select(regex => $"^{_remoteBranchPrefix}[^/]+/({regex})$")];
        string[] remoteRegexes = [.. AppSettings.PrioritizedRemoteNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(regex => $"^{_remoteBranchPrefix}({regex})/")];

        foreach (string branch in branches)
        {
            _orderByBranch[branch] = GetBranchOrder(branch);
        }

        return;

        // Get the order for each branch.
        // Add max possible order value to next "level" to sort properly with the order for each regex.
        int GetBranchOrder(string branch)
        {
            int order = 0;
            if (_isDetachedHead ? DetachedHeadParser.IsDetachedHead(branch) : branch == _currentBranch)
            {
                return order;
            }

            // length of "current branch" group
            order += 1;

            if (IsLocalBranch())
            {
                if (!TryGetOrder(branch, localBranchRegexes, out int localBranchOrder))
                {
                    // Non prioritized local branches added after prioritized remote branches
                    // localBranchOrder==localBranchRegexes.Length, an extra priority level
                    order += prioritizedRemoteBranchesLength();
                }

                // Order by branch priority
                order += localBranchOrder;

                return order;
            }

            // Remote branches after local prioritized branches
            order += localBranchRegexes.Length;

            if (!TryGetOrder(branch, remoteBranchRegexes, out int remoteBranchOrder))
            {
                // after non priority local branches (that are inserted after remote prioritzed branches)
                const int localNonprioritizedBranchesLength = 1;
                order += localNonprioritizedBranchesLength;
            }

            // Group by branch priority then order by remote
            order += (remoteBranchOrder * remotesGroupLength()) + GetOrder(branch, remoteRegexes);

            return order;

            bool IsLocalBranch() => !branch.StartsWith(_remoteBranchPrefix);

            // The groups for a prioritized remote branch adds the unprioritized remotes to the regexes
            int remotesGroupLength() => remoteRegexes.Length + 1;

            // Length of the block of all prioritized remote branches (non prioritized branches separate)
            int prioritizedRemoteBranchesLength() => remoteBranchRegexes.Length * remotesGroupLength();

            // Get the index of the match for prioritized sorting,
            // set order to regexes.Length at no match
            bool TryGetOrder(string branch, string[] regexes, out int order)
            {
                int currentOrder = 0;
                foreach (string regex in regexes)
                {
                    if (Regex.IsMatch(branch, regex, RegexOptions.ExplicitCapture))
                    {
                        order = currentOrder;
                        return true;
                    }

                    currentOrder++;
                }

                order = currentOrder;
                return false;
            }

            int GetOrder(string branch, string[] regexes)
            {
                TryGetOrder(branch, regexes, out int order);
                return order;
            }
        }
    }

    public int Compare(string? a, string? b)
    {
        if (b is null)
        {
            return -1;
        }

        if (a is null)
        {
            return 1;
        }

        int priorityA = _orderByBranch[a];
        int priorityB = _orderByBranch[b];
        return priorityA == priorityB ? StringComparer.Ordinal.Compare(a, b) : priorityA - priorityB;
    }
}
