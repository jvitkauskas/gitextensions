using CommunityToolkit.Mvvm.ComponentModel;

namespace GitUI.Presentation;

/// <summary>
///  Base class for the view model of a dialog: the view closes itself when <see cref="CloseRequested"/> is raised.
/// </summary>
public abstract class DialogViewModel : ObservableObject
{
    /// <summary>
    ///  Raised when the dialog should close; the argument is the dialog result (<see langword="true"/> = accepted).
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    /// <summary>
    ///  Executes a configured hotkey command (see <c>HotkeyCommand.CommandCode</c>);
    ///  returns <see langword="true"/> if the command was handled.
    /// </summary>
    public virtual bool ExecuteHotkeyCommand(int commandCode) => false;

    /// <summary>
    ///  Called when the dialog is about to close (also by the title bar or Escape); returns <see langword="false"/> to keep it
    ///  open, e.g. when the user cancels saving changes (the equivalent of <c>FormClosing</c> with <c>e.Cancel</c>).
    /// </summary>
    public virtual bool CanClose() => true;

    protected void Close(bool accepted) => CloseRequested?.Invoke(this, accepted);
}
