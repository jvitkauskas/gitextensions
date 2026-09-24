using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using NSubstitute;

namespace GitExtensions.ExtensibilityTests;

/// <summary>The types of plugin API v2 and their bridging to plugin API v1 (docs/avalonia-port/PLAN.md, phase 7).</summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class PluginApiV2Tests
{
    [Test]
    public void WindowOwner_None_has_no_handle()
    {
        WindowOwner.None.Handle.Should().Be(0);
        WindowOwner.None.IsNone.Should().BeTrue();
        new WindowOwner(42).IsNone.Should().BeFalse();
        new WindowOwner(42).Should().Be(new WindowOwner(42));
    }

    [Test]
    public void ToWindowOwner_is_the_handle_of_the_window()
    {
        ((IWin32Window?)null).ToWindowOwner().Should().Be(WindowOwner.None);
        IWin32Window window = Substitute.For<IWin32Window>();
        window.Handle.Returns(42);

        window.ToWindowOwner().Should().Be(new WindowOwner(42));
    }

    [Test]
    public void ToWin32Window_is_the_WinForms_control_of_the_handle_or_a_wrapper_of_the_handle()
    {
        WindowOwner.None.ToWin32Window().Should().BeNull();

        using Form form = new();
        new WindowOwner(form.Handle).ToWin32Window().Should().BeSameAs(form, "the WinForms code gets the form it expects");

        // E.g. an Avalonia window: not a WinForms control.
        IWin32Window? wrapper = new WindowOwner(12345).ToWin32Window();
        wrapper.Should().NotBeNull();
        wrapper.Should().NotBeAssignableTo<Control>();
        wrapper!.Handle.Should().Be(12345);
    }

    [Test]
    public void GitUIEventArgs_created_the_v1_way_has_the_owner_of_its_OwnerForm()
    {
        IWin32Window ownerForm = Substitute.For<IWin32Window>();
        ownerForm.Handle.Returns(42);

        GitUIEventArgs args = new(ownerForm, Substitute.For<IGitUICommands>());

        args.OwnerForm.Should().BeSameAs(ownerForm);
        args.Owner.Should().Be(new WindowOwner(42));
        new GitUIEventArgs(ownerForm: null, Substitute.For<IGitUICommands>()).Owner.Should().Be(WindowOwner.None);
    }

    [Test]
    public void GitUIEventArgs_created_the_v2_way_gives_the_owner_to_the_v1_plugins_as_OwnerForm()
    {
        IGitUICommands commands = Substitute.For<IGitUICommands>();

        GitUIEventArgs args = new(new WindowOwner(12345), commands);

        args.Owner.Should().Be(new WindowOwner(12345));
        args.OwnerForm.Should().NotBeNull();
        args.OwnerForm!.Handle.Should().Be(12345);
        args.GitUICommands.Should().BeSameAs(commands);

        using Form form = new();
        new GitUIEventArgs(new WindowOwner(form.Handle), commands).OwnerForm.Should().BeSameAs(form);
        new GitUIEventArgs(WindowOwner.None, commands).OwnerForm.Should().BeNull();

        GitUIPostActionEventArgs postArgs = new(new WindowOwner(12345), commands, actionDone: true);
        postArgs.ActionDone.Should().BeTrue();
        postArgs.OwnerForm!.Handle.Should().Be(12345);
    }

    [Test]
    public void GitUICommands_extensions_call_the_v1_members_with_the_owner_as_a_WinForms_owner()
    {
        IGitUICommands commands = Substitute.For<IGitUICommands>();
        commands.StartCommandLineProcessDialog(Arg.Any<IWin32Window?>(), Arg.Any<string?>(), Arg.Any<ArgumentString>()).Returns(true);
        ArgumentString arguments = "/m";

        commands.StartCommandLineProcessDialog(new WindowOwner(42), "msbuild", arguments).Should().BeTrue();
        commands.StartSettingsDialog(new WindowOwner(42));
        commands.StartPluginSettingsDialog(WindowOwner.None);

        commands.Received(1).StartCommandLineProcessDialog(Arg.Is<IWin32Window?>(owner => owner != null && owner.Handle == 42), "msbuild", arguments);
        commands.Received(1).StartSettingsDialog(Arg.Is<IWin32Window?>(owner => owner != null && owner.Handle == 42), null);
        commands.Received(1).StartPluginSettingsDialog(null);
    }

    [Test]
    public void PluginMessageBoxes_show_through_their_service()
    {
        FakeMessageBoxService service = new() { Result = PluginMessageBoxResult.Yes };
        IPluginMessageBoxService original = PluginMessageBoxes.Service;
        PluginMessageBoxes.Service = service;
        try
        {
            PluginMessageBoxes.ShowError(new WindowOwner(42), "text");
            service.Shown.Should().Equal((new WindowOwner(42), "text", "Error", PluginMessageBoxButtons.Ok, PluginMessageBoxIcon.Error, PluginMessageBoxDefaultButton.Button1));

            PluginMessageBoxes.Confirm(WindowOwner.None, "sure?", "caption", PluginMessageBoxIcon.Warning, PluginMessageBoxDefaultButton.Button2).Should().BeTrue();
            service.Shown[^1].Should().Be((WindowOwner.None, "sure?", "caption", PluginMessageBoxButtons.YesNo, PluginMessageBoxIcon.Warning, PluginMessageBoxDefaultButton.Button2));

            service.Result = PluginMessageBoxResult.No;
            PluginMessageBoxes.Confirm(WindowOwner.None, "sure?", "caption").Should().BeFalse();

            service.Result = PluginMessageBoxResult.Cancel;
            PluginMessageBoxes.Show(WindowOwner.None, "text", "caption", PluginMessageBoxButtons.YesNoCancel).Should().Be(PluginMessageBoxResult.Cancel);

            PluginMessageBoxes.ShowWarning(WindowOwner.None, "warning", "caption");
            service.Shown[^1].Icon.Should().Be(PluginMessageBoxIcon.Warning);
            PluginMessageBoxes.ShowInformation(WindowOwner.None, "information", "caption");
            service.Shown[^1].Icon.Should().Be(PluginMessageBoxIcon.Information);
        }
        finally
        {
            PluginMessageBoxes.Service = original;
        }
    }

    [Test]
    public async Task PluginMessageBoxes_ask_to_choose_between_buttons_through_their_service()
    {
        FakeMessageBoxService service = new() { Choice = 1 };
        IPluginMessageBoxService original = PluginMessageBoxes.Service;
        PluginMessageBoxes.Service = service;
        try
        {
            (await PluginMessageBoxes.ShowChoiceAsync(WindowOwner.None, "caption", "heading", null, PluginMessageBoxIcon.Error, ["Open settings", "Ignore"])).Should().Be(1);
            service.Choices.Should().Equal(["Open settings", "Ignore"]);

            Func<Task<int>> noButton = () => PluginMessageBoxes.ShowChoiceAsync(WindowOwner.None, "caption", "heading", null, PluginMessageBoxIcon.Error, []);
            await noButton.Should().ThrowAsync<ArgumentException>();
        }
        finally
        {
            PluginMessageBoxes.Service = original;
        }
    }

    [TestCase(PluginMessageBoxButtons.Ok, MessageBoxButtons.OK)]
    [TestCase(PluginMessageBoxButtons.OkCancel, MessageBoxButtons.OKCancel)]
    [TestCase(PluginMessageBoxButtons.YesNo, MessageBoxButtons.YesNo)]
    [TestCase(PluginMessageBoxButtons.YesNoCancel, MessageBoxButtons.YesNoCancel)]
    [TestCase(PluginMessageBoxButtons.RetryCancel, MessageBoxButtons.RetryCancel)]
    [TestCase(PluginMessageBoxButtons.AbortRetryIgnore, MessageBoxButtons.AbortRetryIgnore)]
    public void WinForms_message_boxes_have_the_buttons(PluginMessageBoxButtons buttons, MessageBoxButtons expected)
        => WinFormsPluginMessageBoxService.ToWinForms(buttons).Should().Be(expected);

    [TestCase(PluginMessageBoxIcon.None, MessageBoxIcon.None)]
    [TestCase(PluginMessageBoxIcon.Information, MessageBoxIcon.Information)]
    [TestCase(PluginMessageBoxIcon.Warning, MessageBoxIcon.Warning)]
    [TestCase(PluginMessageBoxIcon.Error, MessageBoxIcon.Error)]
    [TestCase(PluginMessageBoxIcon.Question, MessageBoxIcon.Question)]
    public void WinForms_message_boxes_have_the_icon(PluginMessageBoxIcon icon, MessageBoxIcon expected)
        => WinFormsPluginMessageBoxService.ToWinForms(icon).Should().Be(expected);

    [TestCase(DialogResult.OK, PluginMessageBoxResult.Ok)]
    [TestCase(DialogResult.Cancel, PluginMessageBoxResult.Cancel)]
    [TestCase(DialogResult.Yes, PluginMessageBoxResult.Yes)]
    [TestCase(DialogResult.No, PluginMessageBoxResult.No)]
    [TestCase(DialogResult.Retry, PluginMessageBoxResult.Retry)]
    [TestCase(DialogResult.Abort, PluginMessageBoxResult.Abort)]
    [TestCase(DialogResult.Ignore, PluginMessageBoxResult.Ignore)]
    [TestCase(DialogResult.None, PluginMessageBoxResult.None)]
    public void WinForms_message_boxes_return_the_result(DialogResult result, PluginMessageBoxResult expected)
        => WinFormsPluginMessageBoxService.ToResult(result).Should().Be(expected);

    [Test]
    public void WinForms_message_boxes_have_the_default_button()
    {
        WinFormsPluginMessageBoxService.ToWinForms(PluginMessageBoxDefaultButton.Button1).Should().Be(MessageBoxDefaultButton.Button1);
        WinFormsPluginMessageBoxService.ToWinForms(PluginMessageBoxDefaultButton.Button2).Should().Be(MessageBoxDefaultButton.Button2);
        WinFormsPluginMessageBoxService.ToWinForms(PluginMessageBoxDefaultButton.Button3).Should().Be(MessageBoxDefaultButton.Button3);
        WinFormsPluginMessageBoxService.ToTaskDialogIcon(PluginMessageBoxIcon.Error).Should().Be(TaskDialogIcon.Error);
        WinFormsPluginMessageBoxService.ToTaskDialogIcon(PluginMessageBoxIcon.None).Should().BeNull();
    }

    [Test]
    public void PluginMenuItem_runs_its_action_when_enabled()
    {
        int clicks = 0;
        PluginMenuItem item = new("&View", () => clicks++);
        PluginMenuItem disabled = new("&View", () => clicks++) { IsEnabled = false };

        item.Click();
        disabled.Click();

        clicks.Should().Be(1);
        item.Children.Should().BeEmpty();
        item.IsSeparator.Should().BeFalse();
        PluginMenuItem.Separator.IsSeparator.Should().BeTrue();
        new PluginMenuItem("parent", children: [item]).Children.Should().Equal([item]);
    }

    [Test]
    public void ActionSetting_runs_its_action_with_the_owner_and_the_values()
    {
        SettingActionContext? context = null;
        ActionSetting setting = new("link", c => context = c, "caption");
        MemorySettingsSource values = new(SettingLevel.Local);

        setting.Execute(new SettingActionContext(new WindowOwner(42), values));

        setting.Text.Should().Be("link");
        setting.Caption.Should().Be("caption");
        ((ISetting)setting).CreateControlBinding().Should().BeNull("the host renders it");
        context!.Owner.Should().Be(new WindowOwner(42));
        context.Values.Should().BeSameAs(values);

        int runs = 0;
        new ActionSetting("link", () => runs++).Execute(new SettingActionContext(WindowOwner.None, values));
        runs.Should().Be(1);
    }

    [Test]
    public void MemorySettingsSource_keeps_the_values_and_the_level()
    {
        MemorySettingsSource settings = new(SettingLevel.Distributed);
        StringSetting setting = new("name", "default");

        setting[settings] = "value";
        settings.SetValue("unset", null);

        settings.SettingLevel.Should().Be(SettingLevel.Distributed);
        setting.ValueOrDefault(settings).Should().Be("value");
        settings.GetValue("unset").Should().BeNull();
        settings.Names.Should().Equal(["name"]);
        new MemorySettingsSource().SettingLevel.Should().Be(SettingLevel.Unknown);
    }

    private sealed class FakeMessageBoxService : IPluginMessageBoxService
    {
        public List<(WindowOwner Owner, string Text, string Caption, PluginMessageBoxButtons Buttons, PluginMessageBoxIcon Icon, PluginMessageBoxDefaultButton DefaultButton)> Shown { get; } = [];

        public PluginMessageBoxResult Result { get; set; }

        public int Choice { get; set; }

        public IReadOnlyList<string>? Choices { get; private set; }

        public PluginMessageBoxResult Show(WindowOwner owner, string text, string caption, PluginMessageBoxButtons buttons, PluginMessageBoxIcon icon, PluginMessageBoxDefaultButton defaultButton)
        {
            Shown.Add((owner, text, caption, buttons, icon, defaultButton));
            return Result;
        }

        public Task<int> ShowChoiceAsync(WindowOwner owner, string caption, string heading, string? text, PluginMessageBoxIcon icon, IReadOnlyList<string> buttons)
        {
            Choices = buttons;
            return Task.FromResult(Choice);
        }
    }
}
