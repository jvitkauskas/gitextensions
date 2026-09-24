using Avalonia.Threading;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.RepoHosting;

namespace GitUI.Avalonia.CommandsDialogs.RepoHosting;

/// <summary>Avalonia port of <c>CreatePullRequestForm</c>; behaviour lives in <see cref="CreatePullRequestViewModel"/>.</summary>
public partial class CreatePullRequestWindow : DialogWindow
{
    public CreatePullRequestWindow()
    {
        InitializeComponent();

        // As CreatePullRequestForm_Load.
        Opened += (_, _) => Dispatcher.UIThread.Post(() => _ = (DataContext as CreatePullRequestViewModel)?.LoadAsync());
    }

    /// <summary>The spell checking of the body (<c>EditNetSpell</c>), if the view model has it.</summary>
    public SpellCheckController? SpellCheck { get; private set; }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is CreatePullRequestViewModel { SpellCheck: { } spellCheck } && SpellCheck is null)
        {
            body.Editor.WordWrap = true;
            SpellCheck = new SpellCheckController(body.Editor, spellCheck);
        }
    }
}
