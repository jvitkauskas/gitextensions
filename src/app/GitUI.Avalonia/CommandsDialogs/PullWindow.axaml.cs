using System.ComponentModel;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormPull</c>.</summary>
public partial class PullWindow : DialogWindow
{
    private static readonly Lazy<Bitmap> _helpMerge = new(() => LoadHelpImage("HelpPullMerge"));
    private static readonly Lazy<Bitmap> _helpMergeFastForward = new(() => LoadHelpImage("HelpPullMergeFastForward"));
    private static readonly Lazy<Bitmap> _helpRebase = new(() => LoadHelpImage("HelpPullRebase"));
    private static readonly Lazy<Bitmap> _helpFetch = new(() => LoadHelpImage("HelpPullFetch"));

    private PullViewModel? _viewModel;

    public PullWindow()
    {
        InitializeComponent();

        // As FormPullLoad.
        Opened += (_, _) => remotesComboBox.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as PullViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        UpdateHelpImages();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PullViewModel.Action))
        {
            UpdateHelpImages();
        }
    }

    /// <summary>The images of <c>MergeCheckedChanged</c>, <c>RebaseCheckedChanged</c> and <c>FetchCheckedChanged</c>.</summary>
    private void UpdateHelpImages()
    {
        PullMergeAction? action = _viewModel?.Action;
        helpImage.Image1 = action switch
        {
            PullMergeAction.Merge => _helpMerge.Value,
            PullMergeAction.Rebase => _helpRebase.Value,
            PullMergeAction.Fetch => _helpFetch.Value,
            _ => null,
        };
        helpImage.Image2 = action == PullMergeAction.Merge ? _helpMergeFastForward.Value : null;
    }

    private static Bitmap LoadHelpImage(string name)
        => new(AssetLoader.Open(new Uri($"avares://GitUI.Avalonia/Assets/Help/{name}.png")));

    private void LocalBranchTextBox_LostFocus(object? sender, RoutedEventArgs e) => _viewModel?.OnLocalBranchLeave();

    private void RemoteBranchComboBox_DropDownOpened(object? sender, EventArgs e) => _viewModel?.LoadRemoteBranchesCommand.Execute(null);
}
