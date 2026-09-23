using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Presentation.UserControls;

namespace GitUI.Avalonia.Controls;

/// <summary>Avalonia port of <c>CommitPickerSmallControl</c>.</summary>
public partial class CommitPickerView : UserControl
{
    public CommitPickerView()
    {
        InitializeComponent();
    }

    private void CommitHashTextBox_LostFocus(object? sender, RoutedEventArgs e)
        => (DataContext as CommitPickerViewModel)?.CommitText();
}
