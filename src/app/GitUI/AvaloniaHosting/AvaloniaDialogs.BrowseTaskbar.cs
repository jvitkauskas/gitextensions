using System.Drawing.Drawing2D;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Properties;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The taskbar of the Avalonia main window, as <c>FormBrowse</c>: the jump list with the thumbnail toolbar (commit, pull, push,
///  close all windows), the recent repositories of the jump list, and the overlay icon of the status of the working directory.
/// </summary>
internal static partial class AvaloniaDialogs
{
    private static readonly uint _closeAllMessage = global::System.NativeMethods.RegisterWindowMessageW("Global.GitExtensions.CloseAllInstances");
    private static readonly Dictionary<Brush, Icon> _overlayIconByBrush = [];

    private sealed partial class BrowseSession
    {
        private IWindowsJumpListManager? _jumpList;
        private bool _isValidRepository;

        // As OnActivated, OnDeactivate and WndProc of FormBrowse, once for the window.
        private void AttachTaskbar(IGitUICommands commands, bool isValid)
        {
            _isValidRepository = isValid;
            if (_jumpList is null)
            {
                _jumpList = commands.GetRequiredService<IWindowsJumpListManager>();
                window.Activated += (_, _) => OnActivated();
                window.Deactivated += (_, _) => _jumpList.EnableThumbnailToolbar(_isValidRepository);
                global::Avalonia.Controls.Win32Properties.AddWndProcHookCallback(window, CloseAllHook);
            }

            if (isValid)
            {
                // As RefreshWorkingDirComboText: the repository is a recent one of the jump list.
                _jumpList.AddToRecent(commands.Module.WorkingDir);
            }

            _jumpList.EnableThumbnailToolbar(isValid && window.IsActive);
        }

        private void OnActivated()
        {
            if (_jumpList is null)
            {
                return;
            }

            // Once the window is really shown, as OnActivated.
            if (_jumpList.NeedsJumpListCreation && _viewModel is { } viewModel)
            {
                BrowseStrings strings = viewModel.Strings;
                _jumpList.CreateJumpList(
                    window.NativeHandle,
                    new WindowsThumbnailToolbarButtons(
                        new WindowsThumbnailToolbarButton(strings.CommitButton.Text, Images.RepoStateClean, (_, _) => RunFromTaskbar(BrowseCommand.Commit)),
                        new WindowsThumbnailToolbarButton(strings.PullButton.Text, Images.Pull, (_, _) => RunFromTaskbar(BrowseCommand.Pull)),
                        new WindowsThumbnailToolbarButton(strings.PushButton.Text, Images.Push, (_, _) => RunFromTaskbar(BrowseCommand.Push)),
                        new WindowsThumbnailToolbarButton(viewModel.ToolbarItemsStrings.CloseAllWindows.Text, Images.DeleteFile, (_, _) => global::System.NativeMethods.PostMessageW(global::System.NativeMethods.HWND_BROADCAST, _closeAllMessage))));
            }

            _jumpList.EnableThumbnailToolbar(_isValidRepository);
        }

        // As the click of a thumbnail button (e.g. CommitToolStripMenuItemClick): the window comes to the front first.
        private void RunFromTaskbar(BrowseCommand command)
        {
            window.Activate();
            _viewModel?.RunCommand.Execute(command);
        }

        // As WndProc: another instance asks all the windows to close ("Close all windows" of the thumbnail toolbar).
        private nint CloseAllHook(nint handle, uint message, nint wordParameter, nint longParameter, ref bool handled)
        {
            if (message == _closeAllMessage)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(window.Close);
            }
            else if (_jumpList?.ProcessThumbnailButtonMessage(message, wordParameter) == true)
            {
                // As the WindowsAPICodePack: the click of a button of the thumbnail toolbar.
                handled = true;
            }

            return 0;
        }
    }

    private sealed partial class BrowseHost
    {
        // As UpdateStatusInTaskbar: a dot of the color of the state over the icon of the taskbar, and the image of the commit button.
        private void UpdateStatusInTaskbar(Image image, Brush? brush)
        {
            if (!GitCommands.Utils.EnvUtils.RunningOnWindowsWithMainWindow() || !NativeTaskbar.IsPlatformSupported || _window.NativeHandle == 0)
            {
                return;
            }

            try
            {
                if (brush is null)
                {
                    NativeTaskbar.SetOverlayIcon(_window.NativeHandle, null, "");
                    return;
                }

                if (!_overlayIconByBrush.TryGetValue(brush, out Icon? overlay))
                {
                    const int imgDim = 32;
                    const int dotDim = 15;
                    const int pad = 2;
                    using Bitmap bmp = new(imgDim, imgDim);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(Color.Transparent);
                        g.FillEllipse(brush, new Rectangle(imgDim - dotDim - pad, imgDim - dotDim - pad, dotDim, dotDim));
                    }

                    overlay = bmp.ToIcon();
                    _overlayIconByBrush.Add(brush, overlay);
                }

                NativeTaskbar.SetOverlayIcon(_window.NativeHandle, overlay, "");
                _commands.GetRequiredService<IWindowsJumpListManager>().UpdateCommitIcon(image);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
            {
                // As WindowsJumpListManager.SafeInvoke: the taskbar may not be ready (e.g. explorer restarting).
            }
        }
    }
}
