namespace GitUI.AvaloniaHosting;

/// <summary>Avalonia ports of the dialogs that the application shows at startup, before the main window.</summary>
public static class AvaloniaStartupDialogs
{
    /// <summary>
    ///  Shows the Avalonia port of <c>FormChooseTranslation</c>, which sets <c>AppSettings.Translation</c>;
    ///  returns <see langword="false"/> if the port is disabled.
    /// </summary>
    public static bool TryShowChooseTranslation() => AvaloniaDialogs.TryShowChooseTranslation(owner: null);
}
