using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.ProxySwitcher;

/// <summary>Avalonia port of <see cref="ProxySwitcherForm"/>; behaviour lives in <see cref="ProxySwitcherViewModel"/>.</summary>
public partial class ProxySwitcherWindow : DialogWindow
{
    public ProxySwitcherWindow()
    {
        InitializeComponent();
        Opened += (_, _) => setProxyButton.Focus();
    }
}
