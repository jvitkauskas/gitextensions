using System.Runtime.CompilerServices;
using GitUI.UserControls.RevisionGrid.Graph;

namespace GitUI.UserControls.RevisionGrid;

/// <summary>
///  Gives the revision graph layout (GitUI.RevisionGraph) the number of lane colors of the theme, before GitUI builds any graph.
/// </summary>
internal static class RevisionGraphLanePaletteInitializer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "The palette must be set before GitUI builds any graph, from any entry point.")]
    [ModuleInitializer]
    internal static void Initialize()
        => RevisionGraphLanePalette.ColorCountProvider = static () => RevisionGraphLaneColor.PresetGraphBrushes.Count;
}
