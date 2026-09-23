using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormDashboardCategoryTitle</c>.</summary>
public partial class DashboardCategoryTitleWindow : DialogWindow
{
    public DashboardCategoryTitleWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            categoryNameTextBox.Focus();
            categoryNameTextBox.SelectAll();
        };
    }
}
