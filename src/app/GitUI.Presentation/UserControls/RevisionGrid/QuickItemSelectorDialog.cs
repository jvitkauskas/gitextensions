using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>
///  Strings of the quick picker; ids match <c>FormQuickGitRefSelector</c> (the <c>Name</c> of <c>FormQuickItemSelector</c>,
///  which <c>FormQuickStringSelector</c> shares).
/// </summary>
public sealed class QuickItemSelectorStrings : ViewStrings
{
    public QuickItemSelectorStrings()
        : base("FormQuickGitRefSelector")
    {
        Rename = Add("_actionRename", "Text", "Rename");
        Delete = Add("_actionDelete", "Text", "Delete");
        Select = Add("_actionSelect", "Text", "Select");
        Local = Add("_local", "Text", "local");
        Remote = Add("_remote", "Text", "remote");
        Tag = Add("_tag", "Text", "tag");
    }

    public TranslatedText Rename { get; }

    public TranslatedText Delete { get; }

    public TranslatedText Select { get; }

    public TranslatedText Local { get; }

    public TranslatedText Remote { get; }

    public TranslatedText Tag { get; }
}

/// <summary>The action of the quick ref picker, which is the text of its button (<c>FormQuickGitRefSelector.QuickAction</c>).</summary>
public enum QuickRefAction
{
    Rename = 0,
    Delete,
    Select,
}

/// <summary>An item of the quick picker (<c>FormQuickItemSelector.ItemData</c>); a header if it has no <see cref="Item"/>.</summary>
public sealed record QuickSelectorItem(string Label, object? Item)
{
    public bool IsHeader => Item is null;

    public override string ToString() => Label;
}

/// <summary>
///  View model of the quick picker of the revision grid (port of <c>FormQuickItemSelector</c>, <c>FormQuickGitRefSelector</c> and
///  <c>FormQuickStringSelector</c>): a list of items and a button with the action.
/// </summary>
public sealed partial class QuickItemSelectorViewModel : DialogViewModel
{
    /// <summary>How many items are shown before the list scrolls (<c>MaxVisibleItemsWithoutScroll</c>).</summary>
    public const int MaxVisibleItemsWithoutScroll = 8;

    private const string Separator = "――――――――――――――――――";

    public QuickItemSelectorViewModel(IReadOnlyList<QuickSelectorItem> items, string actionText, int selectedIndex = 0)
    {
        Items = items;
        ActionText = actionText;
        if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            SelectedItem = items[selectedIndex];
        }
    }

    public IReadOnlyList<QuickSelectorItem> Items { get; }

    /// <summary>The text of the button (<c>btnAction</c>).</summary>
    public string ActionText { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    public partial QuickSelectorItem? SelectedItem { get; set; }

    /// <summary>The chosen item once accepted (<c>SelectedItem</c>); <see langword="null"/> if cancelled.</summary>
    public object? SelectedValue { get; private set; }

    /// <summary>
    ///  As <c>FormQuickGitRefSelector.Init</c>: the local branches, the remote branches and the tags, sorted by name, each
    ///  group after its header; the first ref is selected.
    /// </summary>
    public static QuickItemSelectorViewModel ForRefs(QuickItemSelectorStrings strings, QuickRefAction action, IReadOnlyList<IGitRef> refs)
    {
        List<QuickSelectorItem> items = Filter(strings.Local, r => r.IsHead);
        items.AddRange(Filter(strings.Remote, r => r.IsRemote));
        items.AddRange(Filter(strings.Tag, r => r.IsTag));

        // If there are any items, then skip the header and select the first actual item.
        int selectedIndex = items.Count > 0 ? 1 : 0;
        string actionText = action switch
        {
            QuickRefAction.Delete => strings.Delete.Text,
            QuickRefAction.Rename => strings.Rename.Text,
            _ => strings.Select.Text,
        };
        return new QuickItemSelectorViewModel(items, actionText, selectedIndex);

        List<QuickSelectorItem> Filter(TranslatedText kind, Func<IGitRef, bool> selector)
        {
            List<QuickSelectorItem> list = [.. refs.Where(selector).OrderBy(r => r.Name).Select(r => new QuickSelectorItem(r.Name, r))];
            if (list.Count > 0)
            {
                list.Insert(0, new QuickSelectorItem($"{kind.Text} {Separator}"[..Separator.Length], null));
            }

            return list;
        }
    }

    /// <summary>As <c>FormQuickStringSelector.Init</c>: the strings sorted.</summary>
    public static QuickItemSelectorViewModel ForStrings(QuickItemSelectorStrings strings, IReadOnlyList<string> values)
        => new([.. values.OrderBy(s => s).Select(s => new QuickSelectorItem(s, s))], strings.Select.Text);

    private bool CanAccept() => SelectedItem is { IsHeader: false };

    [RelayCommand(CanExecute = nameof(CanAccept))]
    private void Accept()
    {
        SelectedValue = SelectedItem?.Item;
        Close(accepted: true);
    }
}
