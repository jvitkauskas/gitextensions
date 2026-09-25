using GitExtensions.Extensibility.Plugins;
using GitExtensions.Plugins.CreateLocalBranches;
using GitExtensions.Plugins.DeleteUnusedBranches;
using GitExtensions.Plugins.FindLargeFiles;
using GitExtensions.Plugins.GitImpact;
using GitExtensions.Plugins.GitStatistics;
using GitExtensions.Plugins.Gource;
using GitExtensions.Plugins.ProxySwitcher;
using GitExtensions.Plugins.ReleaseNotesGenerator;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  The icons of the in-repo plugins as images of plugin API v3 (docs/avalonia-port/CROSS-PLATFORM.md, decision 4): their
///  Resources\Icon*.png embedded as "PluginIcon.png" (src/plugins/Directory.Build.props).
/// </summary>
public sealed class PluginIconTests
{
    private static readonly byte[] _pngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    [TestCase(typeof(CreateLocalBranchesPlugin))]
    [TestCase(typeof(DeleteUnusedBranchesPlugin))]
    [TestCase(typeof(FindLargeFilesPlugin))]
    [TestCase(typeof(GitImpactPlugin))]
    [TestCase(typeof(GitStatisticsPlugin))]
    [TestCase(typeof(GourcePlugin))]
    [TestCase(typeof(ProxySwitcherPlugin))]
    [TestCase(typeof(ReleaseNotesGeneratorPlugin))]
    public void The_plugin_has_its_icon_embedded_as_a_png(Type pluginType)
    {
        PluginImage icon = PluginImage.FromResource(pluginType.Assembly, "PluginIcon.png");

        icon.ToArray().Take(_pngSignature.Length).Should().Equal(_pngSignature);
    }
}
