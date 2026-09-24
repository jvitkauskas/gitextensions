using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Translations;
using GitUI.Properties;

namespace GitUI.AvaloniaHosting;

/// <summary>Routing of the change log dialog (docs/avalonia-port/PLAN.md, phase 3).</summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowChangeLog(IWin32Window? owner)
    {
        ShowDialog(
            () => new ChangeLogWindow { DataContext = new ChangeLogViewModel(ViewStrings.Load<ChangeLogStrings>(), Resources.ChangeLog) },
            owner,
            positionName: "FormChangeLog");
        return true;
    }
}
