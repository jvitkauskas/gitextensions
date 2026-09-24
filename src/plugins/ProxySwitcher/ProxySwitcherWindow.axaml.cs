using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.ProxySwitcher;

/// <summary>Avalonia port of <c>ProxySwitcherForm</c>; behaviour lives in <c>ProxySwitcherViewModel</c>.</summary>
public partial class ProxySwitcherWindow : DialogWindow
{
    public ProxySwitcherWindow()
    {
        InitializeComponent();
        Opened += (_, _) => setProxyButton.Focus();
    }
}
