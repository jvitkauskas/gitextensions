using GitExtensions.Extensibility.Settings;

namespace GitUIPluginInterfaces.BuildServerIntegration;

/// <summary>
///  The settings of a build server integration, declared as settings (plugin API v2 of
///  <c>IBuildServerSettingsUserControl</c>, a WinForms control): the host shows them on the build server integration
///  page of both settings dialogs, as the settings of the plugins (a caption and a control for each), and loads and saves them
///  in the settings of the build server (<c>BuildServerSettings.GetSettingsSource</c>), which the adapter reads.
/// </summary>
/// <remarks>
///  <para>
///   Export it with <c>[Export(typeof(IBuildServerSettingsProvider))]</c>,
///   <c>[BuildServerSettingsProviderMetadata(buildServerType)]</c> and <c>[PartCreationPolicy(CreationPolicy.NonShared)]</c>.
///   The host uses it rather than an <c>IBuildServerSettingsUserControl</c> exported for the same build server type.
///  </para>
///  <para>
///   The settings are the kinds rendered by both dialogs: <see cref="StringSetting"/>, <see cref="PasswordSetting"/>,
///   <see cref="BoolSetting"/>, <see cref="ChoiceSetting"/>, <see cref="NumberSetting{T}"/>, <see cref="PseudoSetting"/> with a
///   text, and <see cref="ActionSetting"/> (a link that can change the values being edited).
///  </para>
/// </remarks>
public interface IBuildServerSettingsProvider
{
    /// <summary>
    ///  The settings to show for the repository of <paramref name="context"/>, created for each page (the host binds controls
    ///  to them). The values to suggest for the unset settings are added to <paramref name="context"/>
    ///  (<see cref="BuildServerSettingsContext.SuggestValue"/>).
    /// </summary>
    IEnumerable<ISetting> GetSettings(BuildServerSettingsContext context);

    /// <summary>
    ///  Checks the values before they are saved, as the v1 controls refused to save invalid values: returns the error to show,
    ///  in which case none of the values is saved, or <see langword="null"/> when they can be saved.
    /// </summary>
    /// <param name="values">The values being saved (with the level of the page).</param>
    string? Validate(SettingsSource values) => null;
}
