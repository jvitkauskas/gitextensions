using System.ComponentModel;
using Avalonia.Threading;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.RepoHosting;

namespace GitUI.Avalonia.CommandsDialogs.RepoHosting;

/// <summary>Avalonia port of <c>ViewPullRequestsForm</c>; behaviour lives in <see cref="ViewPullRequestsViewModel"/>.</summary>
public partial class ViewPullRequestsWindow : DialogWindow
{
    public ViewPullRequestsWindow()
    {
        InitializeComponent();

        // As ViewPullRequestsForm_Load.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as ViewPullRequestsViewModel)?.LoadAsync());
    }

    /// <summary>The spell checking of the comment (<c>EditNetSpell</c>), if the view model has it.</summary>
    public SpellCheckController? SpellCheck { get; private set; }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ViewPullRequestsViewModel viewModel)
        {
            // Column headers are not in the logical tree, so they are not bound.
            pullRequests.Columns[1].Header = viewModel.Strings.HeadingColumn.Text;
            pullRequests.Columns[2].Header = viewModel.Strings.ByColumn.Text;
            pullRequests.Columns[3].Header = viewModel.Strings.CreatedColumn.Text;
            pullRequests.Columns[4].Header = viewModel.Strings.BranchColumn.Text;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (viewModel.SpellCheck is { } spellCheck && SpellCheck is null)
            {
                comment.Editor.WordWrap = true;
                SpellCheck = new SpellCheckController(comment.Editor, spellCheck);
            }
        }
    }

    /// <summary>As <c>_discussionWB_DocumentCompleted</c>: the discussion is scrolled to its end.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewPullRequestsViewModel.Discussion))
        {
            Dispatcher.UIThread.Post(() => discussionScroller.ScrollToEnd(), DispatcherPriority.Background);
        }
    }
}
