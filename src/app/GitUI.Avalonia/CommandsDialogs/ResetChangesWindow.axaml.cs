using Avalonia.Controls;
using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormResetChanges</c>.</summary>
public partial class ResetChangesWindow : DialogWindow
{
    public ResetChangesWindow()
    {
        InitializeComponent();

        // As in FormResetChanges: a long custom message grows the dialog up to 3/4 of the screen, then scrolls.
        Opened += (_, _) =>
        {
            if (Screens.ScreenFromWindow(this) is { } screen)
            {
                messageScrollViewer.MaxWidth = screen.WorkingArea.Width * 3 / 4 / DesktopScaling;
                messageScrollViewer.MaxHeight = screen.WorkingArea.Height * 3 / 4 / DesktopScaling;
            }

            cancelButton.Focus();
        };
    }
}
