namespace GitUI.Presentation.Services;

/// <summary>
///  Message boxes for view models, replacing direct use of <c>MessageBoxes</c> / <c>TaskDialog</c>.
///  The host shows them owned by the view's window.
/// </summary>
public interface IMessageBoxService
{
    void ShowError(string text, string caption);

    void ShowInformation(string text, string caption);

    void ShowWarning(string text, string caption);

    /// <summary>Asks a yes/no question; returns <see langword="true"/> for yes.</summary>
    /// <param name="defaultNo">Whether "No" is the default button, for destructive actions.</param>
    bool Confirm(string text, string caption, bool defaultNo = false);

    /// <summary>
    ///  As <see cref="Confirm"/>, with the Question icon (<c>MessageBoxIcon.Question</c>) where <see cref="Confirm"/> shows
    ///  another one.
    /// </summary>
    bool ConfirmQuestion(string text, string caption) => Confirm(text, caption);

    /// <summary>Asks a yes/no/cancel question; returns <see langword="true"/> for yes, <see langword="false"/> for no and <see langword="null"/> for cancel.</summary>
    bool? ConfirmWithCancel(string text, string caption);
}
