using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using GitUI.Avalonia.Controls.Blame;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormFileHistory</c>.</summary>
public partial class FileHistoryWindow : DialogWindow
{
    private FileHistoryViewModel? _viewModel;
    private bool _updatingTab;

    public FileHistoryWindow()
    {
        InitializeComponent();

        // As OnRuntimeLoad: the history is loaded once the window is shown.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _viewModel?.Initialize());
        tabs.SelectionChanged += (_, _) =>
        {
            if (!_updatingTab && _viewModel is not null && GetTab(tabs.SelectedItem) is { } tab)
            {
                _viewModel.SelectedTab = tab;
            }
        };
        revisionGrid.RevisionActivated += (_, _) => _viewModel?.ViewSelectedRevisions();
        gridMenu.Opening += (_, _) => UpdateMenu();
        openWithDifftoolItem.Click += (_, _) => _viewModel?.OpenWithDifftoolCommand.Execute(false);
        diffToolRemoteLocalItem.Click += (_, _) => _viewModel?.OpenWithDifftoolCommand.Execute(true);
        saveAsItem.Click += (_, _) => _viewModel?.SaveAsCommand.Execute(null);
        revertCommitItem.Click += (_, _) => _viewModel?.RevertCommand.Execute(null);
        cherryPickItem.Click += (_, _) => _viewModel?.CherryPickCommand.Execute(null);
        followRenamesItem.Click += (_, _) => _viewModel?.FollowRenames = !_viewModel.FollowRenames;
        followRenamesExactOnlyItem.Click += (_, _) => _viewModel?.FollowRenamesExactOnly = !_viewModel.FollowRenamesExactOnly;
    }

    public RevisionGridView RevisionGrid => revisionGrid;

    public BlameView Blame => blame;

    public TabControl Tabs => tabs;

    public ContextMenu GridMenu => gridMenu;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = DataContext as FileHistoryViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        if (_viewModel is null)
        {
            return;
        }

        FileHistoryStrings strings = _viewModel.Strings;
        copyToClipboardItem.Header = strings.CopyToClipboard.AccessKeyText;
        openWithDifftoolItem.Header = strings.OpenWithDifftool.AccessKeyText;
        diffToolRemoteLocalItem.Header = strings.DiffToolRemoteLocal.AccessKeyText;
        saveAsItem.Header = strings.SaveAs.AccessKeyText;
        saveAsItem.IsVisible = _viewModel.CanSaveAsVisible;
        manipulateCommitItem.Header = strings.ManipulateCommit.AccessKeyText;
        revertCommitItem.Header = strings.RevertCommit.AccessKeyText;
        cherryPickItem.Header = strings.CherryPickCommit.AccessKeyText;
        followRenamesItem.Header = strings.FollowRenames.AccessKeyText;
        followRenamesExactOnlyItem.Header = strings.FollowRenamesExactOnly.AccessKeyText;
        SelectTab(_viewModel.SelectedTab);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileHistoryViewModel.SelectedTab))
        {
            SelectTab(_viewModel!.SelectedTab);
        }
    }

    private void SelectTab(FileHistoryTab tab)
    {
        _updatingTab = true;
        try
        {
            tabs.SelectedItem = tab switch
            {
                FileHistoryTab.Commit => commitTab,
                FileHistoryTab.Diff => diffTab,
                FileHistoryTab.View => viewTab,
                _ => blameTab,
            };
        }
        finally
        {
            _updatingTab = false;
        }
    }

    private FileHistoryTab? GetTab(object? item)
        => item == commitTab ? FileHistoryTab.Commit
            : item == diffTab ? FileHistoryTab.Diff
            : item == viewTab ? FileHistoryTab.View
            : item == blameTab ? FileHistoryTab.Blame
            : null;

    /// <summary>As <c>FileHistoryContextMenuOpening</c>, with the copy menu of the selected revisions.</summary>
    private void UpdateMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        FileHistoryMenuState state = _viewModel.GetMenuState();
        diffToolRemoteLocalItem.IsEnabled = state.CanDiffToLocal;
        openWithDifftoolItem.IsEnabled = state.CanOpenWithDifftool;
        manipulateCommitItem.IsEnabled = state.CanManipulateCommit;
        saveAsItem.IsEnabled = state.CanSaveAs;
        copyToClipboardItem.IsEnabled = state.CanCopy;
        followRenamesItem.IsChecked = _viewModel.FollowRenames;
        followRenamesExactOnlyItem.IsEnabled = _viewModel.FollowRenames;
        followRenamesExactOnlyItem.IsChecked = _viewModel.FollowRenamesExactOnly;

        List<Control> copyItems = [];
        foreach (RevisionCopyItem item in _viewModel.GetCopyItems())
        {
            copyItems.Add(item.Kind switch
            {
                RevisionCopyItemKind.Separator => new Separator(),
                RevisionCopyItemKind.Caption => new MenuItem { Header = item.Header, IsEnabled = false, FontWeight = global::Avalonia.Media.FontWeight.SemiBold },
                _ => CreateCopyItem(item),
            });
        }

        copyToClipboardItem.ItemsSource = copyItems;

        MenuItem CreateCopyItem(RevisionCopyItem item)
        {
            MenuItem menuItem = new() { Header = TranslatedText.ToAccessKeyText(item.Header) };
            menuItem.Click += (_, _) => _viewModel?.Copy(item);
            return menuItem;
        }
    }
}
