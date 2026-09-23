using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.Avalonia.CommandsDialogs.BrowseDialog;

/// <summary>Avalonia port of <c>FormRecentReposSettings</c>.</summary>
public partial class RecentReposSettingsWindow : DialogWindow
{
    public RecentReposSettingsWindow()
    {
        InitializeComponent();

        foreach (ListBox listBox in new[] { topListBox, recentListBox })
        {
            listBox.ItemTemplate = new FuncDataTemplate<RecentRepoItem>((_, _) => CreateItemView());
        }

        // As FormRecentReposSettings: double click moves the anchor to the other list.
        recentListBox.DoubleTapped += (_, _) => Execute(recentListBox, viewModel => viewModel.AnchorToTopCommand);
        topListBox.DoubleTapped += (_, _) => Execute(topListBox, viewModel => viewModel.AnchorToRecentCommand);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is RecentReposSettingsViewModel viewModel)
        {
            topListBox.ContextMenu = CreateContextMenu(topListBox, viewModel);
            recentListBox.ContextMenu = CreateContextMenu(recentListBox, viewModel);
        }
    }

    /// <summary>The caption, bold if anchored in its list, red if the directory is missing, as wide as the combobox.</summary>
    private Control CreateItemView()
    {
        TextBlock text = new()
        {
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Left,
            [!TextBlock.TextProperty] = new Binding(nameof(RecentRepoItem.Caption)),
            [!ToolTip.TipProperty] = new Binding(nameof(RecentRepoItem.Path)),
            [!WidthProperty] = new Binding($"{nameof(DataContext)}.{nameof(RecentReposSettingsViewModel.CaptionWidth)}") { Source = this },
        };
        text.DataContextChanged += (_, _) =>
        {
            if (text.DataContext is RecentRepoItem item)
            {
                text.Classes.Set("anchored", item.IsAnchoredInList);
                text.Classes.Set("missing", !item.Exists);
            }
        };
        return text;
    }

    private static ContextMenu CreateContextMenu(ListBox listBox, RecentReposSettingsViewModel viewModel)
    {
        (string Header, ICommand Command)[] actions =
        [
            (viewModel.Strings.AnchorToTop.AccessKeyText, viewModel.AnchorToTopCommand),
            (viewModel.Strings.AnchorToRecent.AccessKeyText, viewModel.AnchorToRecentCommand),
            (viewModel.Strings.RemoveAnchor.AccessKeyText, viewModel.RemoveAnchorCommand),
            (viewModel.Strings.RemoveFromRecent.AccessKeyText, viewModel.RemoveFromRecentCommand),
        ];
        MenuItem[] items = [.. actions.Select(action => new MenuItem { Header = action.Header, Command = action.Command })];
        ContextMenu menu = new() { ItemsSource = items };

        // As contextMenuStrip1_Opening: the actions apply to the selected repositories, and there is no menu without any.
        menu.Opening += (_, e) =>
        {
            if (listBox.SelectedItems is not { Count: > 0 } selected)
            {
                e.Cancel = true;
                return;
            }

            object[] selection = [.. selected.Cast<object>()];
            foreach (MenuItem item in items)
            {
                item.CommandParameter = selection;
            }
        };
        return menu;
    }

    private void Execute(ListBox listBox, Func<RecentReposSettingsViewModel, IRelayCommand<object?>> command)
    {
        object[] selection = [.. listBox.SelectedItems?.Cast<object>() ?? []];
        if (DataContext is RecentReposSettingsViewModel viewModel && command(viewModel) is { } relayCommand && relayCommand.CanExecute(selection))
        {
            relayCommand.Execute(selection);
        }
    }
}
