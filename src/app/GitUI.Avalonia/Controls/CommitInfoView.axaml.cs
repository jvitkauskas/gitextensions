using Avalonia.Controls;
using GitUI.Presentation.UserControls;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  The Avalonia commit info (port of the WinForms <c>CommitInfo</c>; docs/avalonia-port/PLAN.md, phase 5): the header, the
///  message and the related refs of a commit, with their links.
/// </summary>
public partial class CommitInfoView : UserControl
{
    public CommitInfoView()
    {
        InitializeComponent();

        // The links of the header, the message and the refs bubble up here.
        AddHandler(XhtmlTextBlock.LinkClickedEvent, (_, e) => (DataContext as CommitInfoViewModel)?.OnLinkClicked(e.Uri));
    }
}
