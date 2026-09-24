using System.ComponentModel.Composition;
using CommonTestUtils;
using CommonTestUtils.MEF;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitExtensions.UITests;
using GitUI;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;
using Microsoft.VisualStudio.Composition;

namespace UITests.CommandsDialogs.SettingsDialog.Pages;

/// <summary>The WinForms build server page with a build server plugin that declares its settings (plugin API v2).</summary>
[Apartment(ApartmentState.STA)]
public class BuildServerIntegrationSettingsPageTests_WithPluginApiV2
{
    private ReferenceRepository _referenceRepository = null!;
    private MockHost _form = null!;
    private BuildServerIntegrationSettingsPage _settingsPage = null!;

    [SetUp]
    public void SetUp()
    {
        _referenceRepository = new ReferenceRepository();

        // The settings control of plugin API v1 is exported too: the settings declared (v2) are used rather than it.
        TestComposition composition = TestComposition.Empty
            .AddParts(typeof(MockGenericBuildServerAdapter))
            .AddParts(typeof(MockGenericBuildServerSettingsUserControl))
            .AddParts(typeof(MockGenericBuildServerSettingsProvider));
        ExportProvider mefExportProvider = composition.ExportProviderFactory.CreateExportProvider();
        ManagedExtensibility.SetTestExportProvider(mefExportProvider);
    }

    [TearDown]
    public void TearDown()
    {
        _settingsPage.Dispose();
        _form.Dispose();
        _referenceRepository.Dispose();
    }

    [Test]
    public void BuildServerType_shows_the_settings_declared_by_the_plugin()
    {
        RunFormTest(
            async form =>
            {
                await AsyncTestHelper.JoinPendingOperationsAsync(AsyncTestHelper.UnexpectedTimeout);

                _settingsPage.GetTestAccessor().BuildServerType.SelectedIndex = 1;

                Control control = _settingsPage.GetTestAccessor().buildServerSettingsPanel.Controls.Cast<Control>().Single();
                control.Should().BeOfType<BuildServerSettingsProviderControl>();
                BuildServerSettingsProviderControl settings = (BuildServerSettingsProviderControl)control;
                settings.Bindings.Select(b => b.Caption()).Should().Equal(["Project name", "Account name", ""]);
                string repositoryName = Path.GetFileName(_referenceRepository.Module.WorkingDir.TrimEnd(Path.DirectorySeparatorChar));
                settings.Bindings[0].GetControl().Text.Should().Be(repositoryName, "the project name is suggested");
                settings.Bindings[2].GetControl().Should().BeOfType<LinkLabel>();
            });
    }

    private void RunFormTest(Func<MockHost, Task> testDriverAsync)
    {
        UITest.RunForm(
            () =>
            {
                _form = new MockHost(_referenceRepository.Module)
                {
                    Size = new(800, 400)
                };

                _settingsPage = SettingsPageBase.Create<BuildServerIntegrationSettingsPage>(_form, GitUICommands.EmptyServiceProvider);
                _settingsPage.Dock = DockStyle.Fill;

                _form.Controls.Add(_settingsPage);

                _form.ShowDialog(owner: null);
            },
            testDriverAsync);
    }

    private class MockHost : Form, ISettingsPageHost
    {
        public MockHost(GitModule module)
        {
            CheckSettingsLogic = new(new(module));
        }

        public CheckSettingsLogic CheckSettingsLogic { get; }

        public void GotoPage(SettingsPageReference settingsPageReference)
        {
            throw new NotImplementedException();
        }

        public void LoadAll()
        {
            throw new NotImplementedException();
        }

        public void SaveAll()
        {
            throw new NotImplementedException();
        }
    }
}

[PartNotDiscoverable]
[Export(typeof(IBuildServerSettingsProvider))]
[BuildServerSettingsProviderMetadata("GenericBuildServerMock")]
[PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
internal sealed class MockGenericBuildServerSettingsProvider : IBuildServerSettingsProvider
{
    public IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context)
    {
        StringSetting projectName = new("ProjectName", "Project name", defaultValue: "");
        context.SuggestValue(projectName, context.DefaultProjectName);
        return
        [
            projectName,
            new StringSetting("AccountName", "Account name", defaultValue: ""),
            new ActionSetting("Open the web site", () => { }),
        ];
    }
}
