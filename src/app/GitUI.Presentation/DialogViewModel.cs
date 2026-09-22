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

    protected void Close(bool accepted) => CloseRequested?.Invoke(this, accepted);
}
