using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

/// <summary>The auto-completion of the editable combo boxes (<c>AutoCompleteMode.SuggestAppend</c>).</summary>
[TestFixture]
public sealed class ComboBoxSuggestAppendTests : HeadlessTest
{
    [Test]
    public Task Typing_appends_the_rest_of_the_first_matching_item_selected() => OnUiThreadAsync(() =>
    {
        ComboBox comboBox = new() { IsEditable = true, ItemsSource = new[] { "[ All ]", "origin", "upstream" } };
        ComboBoxSuggestAppend.SetIsEnabled(comboBox, true);
        Window window = new() { Content = comboBox, Width = 300, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        TextBox textBox = comboBox.GetVisualDescendants().OfType<TextBox>().First();
        textBox.Focus();

        window.KeyTextInput("U");
        Dispatcher.UIThread.RunJobs();
        comboBox.Text.Should().Be("Upstream", "the typed text is kept, the rest of the item appended");
        textBox.SelectedText.Should().Be("pstream");

        // Typing on replaces the selection and completes again; without a match nothing is appended.
        window.KeyTextInput("p");
        Dispatcher.UIThread.RunJobs();
        comboBox.Text.Should().Be("Upstream");
        textBox.SelectedText.Should().Be("stream");
        window.KeyTextInput("x");
        Dispatcher.UIThread.RunJobs();
        comboBox.Text.Should().Be("Upx");

        // A deletion does not complete.
        window.KeyPressQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        comboBox.Text.Should().Be("Up");
        window.Close();
    });
}
