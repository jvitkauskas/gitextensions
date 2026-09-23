namespace GitUI.Presentation.Services;

/// <summary>
///  Message boxes for view models, replacing direct use of <c>MessageBoxes</c> / <c>TaskDialog</c>.
///  The host shows them owned by the view's window.
/// </summary>
public interface IMessageBoxService
{
    void ShowError(string text, string caption);

    void ShowInformation(string text, string caption);

    /// <summary>Asks a yes/no question; returns <see langword="true"/> for yes.</summary>
    /// <param name="defaultNo">Whether "No" is the default button, for destructive actions.</param>
    bool Confirm(string text, string caption, bool defaultNo = false);
}
