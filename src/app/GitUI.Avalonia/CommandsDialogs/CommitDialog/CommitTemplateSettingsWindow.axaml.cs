using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.CommitDialog;

namespace GitUI.Avalonia.CommandsDialogs.CommitDialog;

/// <summary>Avalonia port of <c>FormCommitTemplateSettings</c>; behaviour lives in <see cref="CommitTemplateSettingsViewModel"/>.</summary>
public partial class CommitTemplateSettingsWindow : DialogWindow
{
    public CommitTemplateSettingsWindow()
    {
        InitializeComponent();
    }
}
