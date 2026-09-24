using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Editor;

namespace GitUI.Avalonia.Editor;

/// <summary>Avalonia port of <c>FormFindInCommitFilesGitGrep</c>, the modeless git grep prompt of the file status list.</summary>
public partial class FindInCommitFilesGitGrepWindow : DialogWindow
{
    private FindInCommitFilesGitGrepViewModel? _viewModel;

    public FindInCommitFilesGitGrepWindow()
    {
        InitializeComponent();
        Opened += (_, _) => expressionComboBox.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _viewModel?.FocusExpressionRequested -= OnFocusExpressionRequested;
        _viewModel = DataContext as FindInCommitFilesGitGrepViewModel;
        _viewModel?.FocusExpressionRequested += OnFocusExpressionRequested;
    }

    private void OnFocusExpressionRequested(object? sender, EventArgs e) => expressionComboBox.Focus();
}
