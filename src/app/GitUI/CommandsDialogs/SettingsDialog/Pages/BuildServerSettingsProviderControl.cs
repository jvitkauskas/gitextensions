using GitExtensions.Extensibility.Settings;
using GitExtUtils.GitUI;
using GitUI.SettingControlBindings;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages;

/// <summary>
///  The settings of a build server integration of plugin API v2 (<see cref="IBuildServerSettingsProvider"/>) in the WinForms
///  build server integration page: a caption and a control for each setting (as <c>TableSettingsLayout</c>), in the place of
///  the settings control of plugin API v1, whose interface it implements for the page.
/// </summary>
internal sealed partial class BuildServerSettingsProviderControl : UserControl, IBuildServerSettingsUserControl
{
    private readonly IBuildServerSettingsProvider _provider;
    private readonly TableLayoutPanel _panel;
    private readonly List<ISettingControlBinding> _bindings = [];
    private BuildServerSettingsContext? _context;
    private SettingLevel _level;

    public BuildServerSettingsProviderControl(IBuildServerSettingsProvider provider)
    {
        _provider = provider;
        _panel = AutoLayoutSettingsPage.CreateDefaultTableLayoutPanel();
        AutoScroll = true;
        Controls.Add(_panel);
    }

    /// <summary>The settings of the build server type (plugin API v2), if its plugin declares them.</summary>
    public static IBuildServerSettingsProvider? FindProvider(string? buildServerType)
        => buildServerType is null
            ? null
            : ManagedExtensibility.GetExports<IBuildServerSettingsProvider, IBuildServerTypeMetadata>()
                .SingleOrDefault(export => export.Metadata.BuildServerType == buildServerType)?.Value;

    internal IReadOnlyList<ISettingControlBinding> Bindings => _bindings;

    /// <summary>Shows the error of <see cref="IBuildServerSettingsProvider.Validate"/>; a message box by default (replaced by tests).</summary>
    internal Action<string>? ShowError { get; set; }

    public void Initialize(string defaultProjectName, IEnumerable<string?> remotes)
    {
        _context = new BuildServerSettingsContext(defaultProjectName, remotes);
        _panel.SuspendLayout();
        foreach (ISetting setting in _provider.GetSettings(_context))
        {
            ISettingControlBinding binding = SettingControlBindingsProvider.CreateControlBinding(setting);
            _bindings.Add(binding);
            AddRow(binding);
        }

        _panel.ResumeLayout();
        EditedSettingValues.Connect(_bindings, () => _level);
    }

    public void LoadSettings(SettingsSource buildServerConfig)
    {
        _level = buildServerConfig.SettingLevel;
        SettingsSource settings = _context?.WithSuggestedValues(buildServerConfig) ?? buildServerConfig;
        foreach (ISettingControlBinding binding in _bindings)
        {
            binding.LoadSetting(settings);
        }
    }

    public void SaveSettings(SettingsSource buildServerConfig)
    {
        _level = buildServerConfig.SettingLevel;

        // As the v1 controls, which saved nothing when a value was invalid.
        if (_provider.Validate(new EditedSettingValues(_bindings, () => _level).GetValues()) is { } error)
        {
            if (ShowError is { } showError)
            {
                showError(error);
            }
            else
            {
                MessageBoxes.ShowError(FindForm(), error);
            }

            return;
        }

        foreach (ISettingControlBinding binding in _bindings)
        {
            binding.SaveSetting(buildServerConfig);
        }
    }

    // As TableSettingsLayout.AddSettingControlImpl.
    private void AddRow(ISettingControlBinding binding)
    {
        int row = _panel.RowCount++;
        _panel.RowStyles.Add(new RowStyle());
        string caption = binding.Caption();
        if (!string.IsNullOrEmpty(caption))
        {
            _panel.Controls.Add(
                new Label
                {
                    Text = caption,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left | AnchorStyles.Top,
                    Margin = new Padding(0, DpiUtil.Scale(2), 0, 0)
                },
                0,
                row);
        }

        Control control = binding.GetControl();
        control.Dock = DockStyle.Fill;
        _panel.Controls.Add(control, 1, row);
    }
}
