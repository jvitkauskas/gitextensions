using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using GitUI.Presentation;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Base class of Avalonia dialogs; the counterpart of <c>GitExtensionsForm</c> / <c>GitExtensionsDialog</c>.
/// </summary>
/// <remarks>
///  Closes itself when its <see cref="DialogViewModel"/> requests it, and on Escape (like <c>GitExtensionsForm</c>).
/// </remarks>
public class DialogWindow : Window
{
    private DialogViewModel? _viewModel;

    private static readonly Lazy<WindowIcon> _applicationIcon = new(
        () => new WindowIcon(AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/git-extensions-logo-256px.png"))));

    public DialogWindow()
    {
        CanMinimize = false;
        Icon = _applicationIcon.Value;
    }

    /// <summary>
    ///  <see langword="true"/> if the dialog was accepted (the equivalent of <c>DialogResult.OK</c>).
    /// </summary>
    public bool DialogResult { get; private set; }

    /// <summary>
    ///  The native window handle, available once the window has been created; used to parent WinForms dialogs.
    /// </summary>
    public nint NativeHandle => TryGetPlatformHandle()?.Handle ?? 0;

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as DialogViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            CloseDialog(accepted: false);
        }
    }

    private void OnCloseRequested(object? sender, bool accepted) => CloseDialog(accepted);

    private void CloseDialog(bool accepted)
    {
        DialogResult = accepted;
        Close();
    }
}
