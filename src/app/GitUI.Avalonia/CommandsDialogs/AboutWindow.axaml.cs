using Avalonia.Threading;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.Avalonia.CommandsDialogs;

/// <summary>Avalonia port of <c>FormAbout</c>; behaviour lives in <see cref="AboutViewModel"/>.</summary>
public partial class AboutWindow : DialogWindow
{
    private readonly DispatcherTimer _thanksTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public AboutWindow()
    {
        InitializeComponent();

        _thanksTimer.Tick += (_, _) => (DataContext as AboutViewModel)?.ThankNextContributor();
        Opened += (_, _) => _thanksTimer.Start();
        Closed += (_, _) => _thanksTimer.Stop();
    }
}
