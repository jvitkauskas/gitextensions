using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GitExtUtils;

public static class ClipboardUtil
{
    public static bool TrySetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            // Setting clipboard data can fail, as applications can lock the clipboard.
            // Such failures surface as ExternalException.
            // Here we use an approach that retries to set the data periodically as required,
            // and throws if it's unable to do so after a given number of attempts.
            //
            // See https://github.com/gitextensions/gitextensions/issues/4542

            Clipboard.SetDataObject(
                text,
                copy: true, // keep the data on the clipboard, even after Git Extensions exits
                retryTimes: 5,
                retryDelay: 100);

            return true;
        }
        catch (ExternalException)
        {
            // The clipboard is being used by another process
            return false;
        }
    }

    /// <summary>Reads the text of the clipboard, if it has text (e.g. for the plugins, which need no WinForms type then).</summary>
    public static bool TryGetText([NotNullWhen(returnValue: true)] out string? text)
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
                return true;
            }
        }
        catch (ExternalException)
        {
            // The clipboard is being used by another process
        }

        text = null;
        return false;
    }
}
