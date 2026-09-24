using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog.Pages;

public partial class RevisionLinksSettingsPageView : UserControl
{
    public RevisionLinksSettingsPageView()
    {
        InitializeComponent();

        // As the DataGridView deleting the selected row with the Delete key.
        linksGrid.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Delete && DataContext is RevisionLinksSettingsPageViewModel viewModel && linksGrid.SelectedItem is RevisionLinkFormatRow row && !row.IsNewRow)
            {
                viewModel.RemoveLinkFormatCommand.Execute(row);
                e.Handled = true;
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not RevisionLinksSettingsPageViewModel viewModel)
        {
            return;
        }

        linksGrid.Columns[0].Header = viewModel.Strings.Caption.Text;
        linksGrid.Columns[1].Header = viewModel.Strings.Uri.Text;

        // As LoadTemplatesInMenu: the templates of the providers in the drop-down of Add.
        MenuFlyout menu = (MenuFlyout)addCategory.Flyout!;
        menu.Items.Clear();
        foreach (RevisionLinkTemplate template in viewModel.Templates)
        {
            menu.Items.Add(new MenuItem
            {
                Header = template.Text,
                Icon = new Image { Width = 16, Height = 16, Source = SettingsIconConverter.Instance.Convert(template.IconName, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) as global::Avalonia.Media.IImage },
                Command = template.Command,
            });
        }
    }

    private void OnHelpClick(object? sender, RoutedEventArgs e)
        => DialogWindow.OpenManualSection?.Invoke(RevisionLinksSettingsPageViewModel.ManualSectionSubfolder, RevisionLinksSettingsPageViewModel.ManualSectionAnchorName);
}
