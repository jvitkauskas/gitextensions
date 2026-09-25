using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Presentation.HelperDialogs;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The prompt of ssh and git off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 4).</summary>
[TestFixture]
public sealed class AskPassViewTests : HeadlessTest
{
    [TestCase("Enter passphrase for key '/home/user/.ssh/id_ed25519': ", true)]
    [TestCase("user@example.org's password: ", true)]
    [TestCase("Password for 'https://user@github.com': ", true)]
    [TestCase("Enter PIN for authenticator: ", true)]
    [TestCase("Username for 'https://github.com': ", false)]
    [TestCase("Are you sure you want to continue connecting (yes/no/[fingerprint])? ", false)]
    [TestCase("Something else: ", true)]
    public void The_answers_of_secrets_are_hidden(string prompt, bool isSecret)
    {
        AskPassViewModel.IsSecretPrompt(prompt).Should().Be(isSecret);
    }

    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        Capture(new AskPassWindow { DataContext = new AskPassViewModel("Enter passphrase for key '/home/user/.ssh/id_ed25519': ", new DialogBoxStrings()) }, $"askpass-{theme}");
    });

    [Test]
    public Task A_passphrase_is_typed_hidden_and_accepted() => OnUiThreadAsync(() =>
    {
        AskPassViewModel viewModel = new("Enter passphrase for key 'id_ed25519': ", new DialogBoxStrings());
        AskPassWindow window = Show(new AskPassWindow { DataContext = viewModel });
        TextBox input = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "inputTextBox");

        input.PasswordChar.Should().Be('●');
        input.Text = "secret";
        viewModel.OkCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        viewModel.Answer.Should().Be("secret");
    });

    [Test]
    public Task A_question_is_answered_in_clear_and_cancelling_gives_no_answer() => OnUiThreadAsync(() =>
    {
        AskPassViewModel viewModel = new("Are you sure you want to continue connecting (yes/no/[fingerprint])? ", new DialogBoxStrings());
        AskPassWindow window = Show(new AskPassWindow { DataContext = viewModel });
        TextBox input = window.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "inputTextBox");

        input.PasswordChar.Should().Be(default(char));
        window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "promptText").Text
            .Should().Be("Are you sure you want to continue connecting (yes/no/[fingerprint])?");
        viewModel.CancelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        viewModel.Answer.Should().BeNull();
    });

    private static T Show<T>(T window)
        where T : GitUI.Avalonia.Hosting.DialogWindow
    {
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Capture(GitUI.Avalonia.Hosting.DialogWindow window, string name)
    {
        Show(window);
        SaveScreenshot(window.CaptureRenderedFrame(), name);
        window.Close();
    }
}
