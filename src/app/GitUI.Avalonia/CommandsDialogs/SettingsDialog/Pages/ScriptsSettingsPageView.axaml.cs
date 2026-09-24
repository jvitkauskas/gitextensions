using Avalonia;
using Avalonia.Controls;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class ScriptsSettingsPageView : UserControl
{
    private ScriptsSettingsPageViewModel? _viewModel;
    private SimpleHelpDisplayWindow? _argumentsHelp;

    public ScriptsSettingsPageView()
    {
        InitializeComponent();
    }

    public DataGrid ScriptsGrid => scriptsGrid;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_viewModel is not null)
        {
            _viewModel.ArgumentsHelpRequested -= OnArgumentsHelpRequested;
        }

        _viewModel = DataContext as ScriptsSettingsPageViewModel;
        if (_viewModel is not null)
        {
            _viewModel.ArgumentsHelpRequested += OnArgumentsHelpRequested;
            SetFileDialogs();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SetFileDialogs();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // As OnParentChanged: the help closes with the page.
        _argumentsHelp?.Close();
    }

    private void SetFileDialogs()
    {
        if (_viewModel is not null && TopLevel.GetTopLevel(this) is TopLevel topLevel)
        {
            _viewModel.FileDialogs = new AvaloniaFileDialogService(topLevel);
        }
    }

    // As btnArgumentsHelp_Click: a window beside the dialog, brought to front if shown already.
    private void OnArgumentsHelpRequested(object? sender, (string Title, string Content) help)
    {
        if (_argumentsHelp is { IsVisible: true })
        {
            _argumentsHelp.Activate();
            return;
        }

        _argumentsHelp = new SimpleHelpDisplayWindow { DataContext = new SimpleHelpDisplayViewModel(help.Title, help.Content) };
        _argumentsHelp.Closed += (_, _) => _argumentsHelp = null;
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            _argumentsHelp.Show(owner);
        }
        else
        {
            _argumentsHelp.Show();
        }
    }
}
