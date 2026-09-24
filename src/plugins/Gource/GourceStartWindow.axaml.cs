using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.Gource;

/// <summary>Avalonia port of <c>GourceStart</c>; behaviour lives in <c>GourceStartViewModel</c>.</summary>
public partial class GourceStartWindow : DialogWindow
{
    public GourceStartWindow()
    {
        InitializeComponent();
        Opened += (_, _) => startButton.Focus();
    }
}
