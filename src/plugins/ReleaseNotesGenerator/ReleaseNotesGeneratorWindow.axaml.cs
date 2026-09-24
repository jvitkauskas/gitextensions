using GitUI.Avalonia.Hosting;

namespace GitExtensions.Plugins.ReleaseNotesGenerator;

/// <summary>Avalonia port of <see cref="ReleaseNotesGeneratorForm"/>; behaviour lives in <see cref="ReleaseNotesGeneratorViewModel"/>.</summary>
public partial class ReleaseNotesGeneratorWindow : DialogWindow
{
    public ReleaseNotesGeneratorWindow()
    {
        InitializeComponent();
        Opened += (_, _) => revisionFrom.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ReleaseNotesGeneratorViewModel viewModel)
        {
            viewModel.FocusRequested += (_, input) => (input == ReleaseNotesInput.From ? revisionFrom : revisionTo).Focus();
        }
    }
}
