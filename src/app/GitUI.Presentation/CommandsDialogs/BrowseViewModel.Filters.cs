using GitUI.Presentation.UserControls.RevisionGrid;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>The filter toolbar of the main window (<c>ToolStripFilters</c>).</summary>
public sealed partial class BrowseViewModel
{
    /// <summary>The branch and text filters of the grid, if the application filters it.</summary>
    public FilterToolBarViewModel? Filters
    {
        get;
        init
        {
            field = value;

            // As the items of ToolStripMain: the visibility of the items is read when first shown.
            value?.ItemVisibility.LoadVisibility = key => _toolbarItemsHost?.GetToolbarItemVisibility(GetFilterSettingKey(key), defaultValue: true) ?? true;
        }
    }
}
