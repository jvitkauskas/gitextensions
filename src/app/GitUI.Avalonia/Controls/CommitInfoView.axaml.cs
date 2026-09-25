using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using GitCommands;
using GitCommands.Utils;
using GitUI.Presentation.UserControls;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  The Avalonia commit info (port of the WinForms <c>CommitInfo</c>; docs/avalonia-port/PLAN.md, phase 5): the avatar, the
///  header, the message and the related refs of a commit, with their links and the context menu.
/// </summary>
public partial class CommitInfoView : UserControl
{
    private CommitInfoViewModel? _viewModel;
    private string? _linkToCopy;

    public CommitInfoView()
    {
        InitializeComponent();

        // The links of the header, the message and the refs bubble up here.
        AddHandler(XhtmlTextBlock.LinkClickedEvent, (_, e) => _viewModel?.OnLinkClicked(e.Uri));

        // As commitInfoContextMenuStrip_Opening: the link under the mouse can be copied.
        ContextRequested += (_, e) =>
        {
            _linkToCopy = null;
            if (e.Source is Visual source && (source as XhtmlTextBlock ?? source.FindAncestorOfType<XhtmlTextBlock>()) is { } text
                && e.TryGetPosition(text, out Point position))
            {
                _linkToCopy = text.GetLinkAt(position);
            }
        };
        menu.Opening += (_, _) => UpdateMenu();
        copyLinkItem.Click += (_, _) =>
        {
            if (_linkToCopy is not null)
            {
                _viewModel?.CopyLink(_linkToCopy);
            }
        };
        copyCommitInfoItem.Click += (_, _) => _viewModel?.CopyCommitInfo();
        addNotesItem.Click += (_, _) => _viewModel?.AddNotes();
        showContainedInBranchesLocalItem.Click += (_, _) => Toggle(o => o with { ShowContainedInBranchesLocal = !o.ShowContainedInBranchesLocal });
        showContainedInBranchesRemoteItem.Click += (_, _) => Toggle(o => o with { ShowContainedInBranchesRemote = !o.ShowContainedInBranchesRemote });
        showContainedInBranchesRemoteIfNoLocalItem.Click += (_, _) => Toggle(o => o with { ShowContainedInBranchesRemoteIfNoLocal = !o.ShowContainedInBranchesRemoteIfNoLocal });
        showContainedInTagsItem.Click += (_, _) => Toggle(o => o with { ShowContainedInTags = !o.ShowContainedInTags });
        showAnnotatedTagsMessagesItem.Click += (_, _) => Toggle(o => o with { ShowAnnotatedTagsMessages = !o.ShowAnnotatedTagsMessages });
        showTagThisCommitDerivesFromItem.Click += (_, _) => Toggle(o => o with { ShowTagThisCommitDerivesFrom = !o.ShowTagThisCommitDerivesFrom });

        // As AvatarControl: the menu of the avatar, one item for each provider and generated style.
        foreach (AvatarProvider provider in EnumHelper.GetValues<AvatarProvider>())
        {
            MenuItem item = new() { Header = provider.GetDescription(), ToggleType = MenuItemToggleType.Radio, Tag = provider };
            item.Click += (_, _) => _ = _viewModel?.SetAvatarProviderAsync(provider);
            avatarProviderItem.Items.Add(item);
        }

        foreach (AvatarFallbackType fallbackType in EnumHelper.GetValues<AvatarFallbackType>())
        {
            MenuItem item = new() { Header = fallbackType.GetDescription(), ToggleType = MenuItemToggleType.Radio, Tag = fallbackType };
            item.Click += (_, _) => _ = _viewModel?.SetAvatarFallbackTypeAsync(fallbackType);
            fallbackAvatarStyleItem.Items.Add(item);
        }

        avatarMenu.Opening += (_, e) =>
        {
            if (_viewModel?.HasAvatarMenu is not true)
            {
                e.Cancel = true;
                return;
            }

            UpdateAvatarMenu();
        };
        clearImageCacheItem.Click += (_, _) => _ = _viewModel?.ClearAvatarCacheAsync();
        registerGravatarItem.Click += (_, _) => _viewModel?.RegisterAtGravatar();
    }

    public ContextMenu AvatarMenu => avatarMenu;

    /// <summary>The check marks of the current provider and style (as the <c>DropDownOpening</c> of their items).</summary>
    public void UpdateAvatarMenu()
    {
        foreach (MenuItem item in avatarProviderItem.Items.OfType<MenuItem>())
        {
            item.IsChecked = Equals(item.Tag, _viewModel?.AvatarProvider);
        }

        foreach (MenuItem item in fallbackAvatarStyleItem.Items.OfType<MenuItem>())
        {
            item.IsChecked = Equals(item.Tag, _viewModel?.AvatarFallbackType);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewModel?.WatchAvatarCache(true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _viewModel?.WatchAvatarCache(false);
        base.OnDetachedFromVisualTree(e);
    }

    public ContextMenu Menu => menu;

    /// <summary>Prepares the menu as if opened over <paramref name="link"/> (e.g. for tests).</summary>
    public void OpenMenuFor(string? link)
    {
        _linkToCopy = link;
        UpdateMenu();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel?.WatchAvatarCache(false);
        _viewModel = DataContext as CommitInfoViewModel;
        _viewModel?.PropertyChanged += OnViewModelPropertyChanged;
        if (_viewModel is null)
        {
            return;
        }

        if (this.IsAttachedToVisualTree())
        {
            _viewModel.WatchAvatarCache(true);
        }

        AvatarStrings avatarStrings = _viewModel.AvatarStrings;
        clearImageCacheItem.Header = avatarStrings.ClearImageCache.AccessKeyText;
        avatarProviderItem.Header = avatarStrings.AvatarProvider.AccessKeyText;
        fallbackAvatarStyleItem.Header = avatarStrings.FallbackAvatarStyle.AccessKeyText;
        registerGravatarItem.Header = avatarStrings.RegisterGravatar.AccessKeyText;

        CommitInfoStrings strings = _viewModel.Strings;
        copyCommitInfoItem.Header = strings.CopyCommitInfo.AccessKeyText;
        showContainedInBranchesLocalItem.Header = strings.ShowContainedInBranchesLocal.AccessKeyText;
        showContainedInBranchesRemoteItem.Header = strings.ShowContainedInBranchesRemote.AccessKeyText;
        showContainedInBranchesRemoteIfNoLocalItem.Header = strings.ShowContainedInBranchesRemoteIfNoLocal.AccessKeyText;
        showContainedInTagsItem.Header = strings.ShowContainedInTags.AccessKeyText;
        showAnnotatedTagsMessagesItem.Header = strings.ShowAnnotatedTagsMessages.AccessKeyText;
        showTagThisCommitDerivesFromItem.Header = strings.ShowTagThisCommitDerivesFrom.AccessKeyText;
        addNotesItem.Header = strings.AddNotes.AccessKeyText;
        UpdateAvatar();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommitInfoViewModel.Avatar))
        {
            UpdateAvatar();
        }
    }

    private void UpdateAvatar()
    {
        Bitmap? previous = avatar.Source as Bitmap;
        byte[]? image = _viewModel?.Avatar;
        try
        {
            avatar.Source = image is null ? null : new Bitmap(new MemoryStream(image));
        }
        catch (Exception)
        {
            // Not an image Avalonia can decode: no avatar.
            avatar.Source = null;
        }

        previous?.Dispose();
    }

    private void UpdateMenu()
    {
        if (_viewModel is null)
        {
            return;
        }

        CommitInfoDisplayOptions options = _viewModel.Options;
        copyLinkItem.IsVisible = _linkToCopy is not null;
        copyLinkItem.Header = string.Format(_viewModel.Strings.CopyLink.AccessKeyText, _linkToCopy?.Replace("_", "__"));
        showContainedInBranchesLocalItem.IsChecked = options.ShowContainedInBranchesLocal;
        showContainedInBranchesRemoteItem.IsChecked = options.ShowContainedInBranchesRemote;
        showContainedInBranchesRemoteIfNoLocalItem.IsChecked = options.ShowContainedInBranchesRemoteIfNoLocal;
        showContainedInTagsItem.IsChecked = options.ShowContainedInTags;
        showAnnotatedTagsMessagesItem.IsChecked = options.ShowAnnotatedTagsMessages;
        showTagThisCommitDerivesFromItem.IsChecked = options.ShowTagThisCommitDerivesFrom;
    }

    private void Toggle(Func<CommitInfoDisplayOptions, CommitInfoDisplayOptions> change)
    {
        if (_viewModel is not null)
        {
            _viewModel.SetOptions(change(_viewModel.Options));
        }
    }
}
