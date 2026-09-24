using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;

namespace GitUI.AvaloniaHosting;

/// <summary>The default pull action and the visibility of the toolbar items of the Avalonia main window (as <c>FormBrowseMenus</c>).</summary>
internal static partial class AvaloniaDialogs
{
    private sealed partial class BrowseHost : IBrowseToolbarItemsHost
    {
        private const string ToolbarVisibilityPrefix = "formbrowse_toolbar_visibility_";

        public GitPullAction DefaultPullAction
        {
            get => AppSettings.DefaultPullAction;
            set => AppSettings.DefaultPullAction = value;
        }

        public bool GetToolbarItemVisibility(string key, bool defaultValue)
            => AppSettings.GetBool(ToolbarVisibilityPrefix + key, defaultValue);

        public void SetToolbarItemVisibility(string key, bool visible, bool defaultValue)
            => AppSettings.SetBool(ToolbarVisibilityPrefix + key, visible == defaultValue ? null : visible);
    }
}
