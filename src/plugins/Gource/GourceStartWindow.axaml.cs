using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.Gource;

/// <summary>Avalonia port of <see cref="GourceStart"/>; behaviour lives in <see cref="GourceStartViewModel"/>.</summary>
public partial class GourceStartWindow : DialogWindow
{
    public GourceStartWindow()
    {
        InitializeComponent();
        Opened += (_, _) => startButton.Focus();
    }
}
