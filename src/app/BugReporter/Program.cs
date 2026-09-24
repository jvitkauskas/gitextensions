using System.Text;
using Avalonia.Threading;
using BugReporter.Serialization;
using GitCommands;
using GitUI;
using GitUI.Avalonia.Hosting;
using Microsoft.VisualStudio.Threading;

namespace BugReporter;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        AvaloniaUi.EnsureInitialized(GetOptions);

        // The shared JoinableTaskContext, on the synchronization context of Avalonia's UI thread.
        SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

        // If an error happens before we had a chance to init the environment information
        // the call to GetInformation() from BugReporter.ShowNBug() will fail.
        // There's no perf hit calling Initialise() multiple times.
        UserEnvironmentInformation.Initialise(ThisAssembly.Git.Sha, ThisAssembly.Git.IsDirty);

        SerializableException? exception = null;

        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            string xml;
            try
            {
                xml = Base64Decode(arg);
            }
            catch (Exception ex)
            {
                exception = new(new Exception($"Failed to decode the error payload\r\n{arg}", ex));
                continue;
            }

            try
            {
                exception = SerializableException.FromXmlString(xml);
            }
            catch (Exception ex)
            {
                exception = new(new Exception($"Failed to decode/parse error payload\r\n{xml}", ex));
            }
        }

        // No specific info extracted from these exceptions
        string exceptionInfo = "";
        exception ??= new(new Exception("Missing error payload"));

        BugReportDialog.Show(owner: null, GetOptions, exception, exceptionInfo, UserEnvironmentInformation.GetInformation(), canIgnore: true, showIgnore: false, focusDetails: false);
    }

    /// <summary>The fonts of the application (its theme is not loaded by the bug reporter).</summary>
    private static AvaloniaUiOptions GetOptions()
    {
        Font font = AppSettings.Font;
        return new AvaloniaUiOptions(
            IsDarkTheme: false,
            FontFamily: font.FontFamily.Name,
            FontSize: font.SizeInPoints * 96 / 72,
            MonospaceFontFamily: AppSettings.MonospaceFont.FontFamily.Name);
    }

    private static string Base64Decode(string base64EncodedData)
    {
        byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}
