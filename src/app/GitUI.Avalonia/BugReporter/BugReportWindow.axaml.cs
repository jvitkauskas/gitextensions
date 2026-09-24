using GitUI.Avalonia.Hosting;

namespace GitUI.Avalonia.BugReporter;

/// <summary>
///  Avalonia port of <c>BugReportForm</c>: the exception, the environment and the description of the user, reported to
///  GitHub or copied; the value of the selected property of the exception is shown below its list (the WinForms form
///  opened <c>ExceptionDetailView</c> for it).
/// </summary>
public partial class BugReportWindow : DialogWindow
{
    public BugReportWindow()
    {
        InitializeComponent();
        Opened += (_, _) => descriptionText.Focus();
    }
}
