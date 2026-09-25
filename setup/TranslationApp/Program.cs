using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Translations;
using GitExtensions.Extensibility.Translations.Xliff;
using GitUI;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace TranslationApp;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // Store the shared JoinableTaskContext
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

        ManagedExtensibility.Initialise();

        // Required for translation
        PluginRegistry.InitializeAll();

        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            AppSettings.Font = SystemFonts.MessageBoxFont!.ToFontDescriptor();
        }

        IDictionary<string, List<TranslationItemWithCategory>> neutralItems = TranslationHelpers.LoadNeutralItems();
        string filename = Path.Combine(Translator.GetTranslationDir(), "English.xlf");
        TranslationHelpers.SaveTranslation(null, neutralItems, filename);

        string[] translationsNames = Translator.GetAllTranslations();
        foreach (string name in translationsNames)
        {
            IDictionary<string, TranslationFile> translation = Translator.GetTranslation(name);
            IDictionary<string, List<TranslationItemWithCategory>> translateItems = TranslationHelpers.LoadTranslation(translation, neutralItems);
            filename = Path.Combine(Translator.GetTranslationDir(), name + ".xlf");
            TranslationHelpers.SaveTranslation(translation.First().Value.TargetLanguage!, translateItems, filename);
        }
    }
}
