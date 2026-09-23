using System.Runtime.InteropServices;
using Avalonia.Threading;
using GitCommands;

namespace GitExtensions.UITests.AvaloniaHosting;

/// <summary>Sizing of dialogs whose height follows their content, as the WinForms forms with a fixed height.</summary>
public sealed partial class AvaloniaHostingTests
{
    private const uint WM_NCHITTEST = 0x0084;
    private const int HTRIGHT = 11;
    private const int HTBORDER = 18;

    [Test]
    public void Content_sized_dialog_resizes_only_horizontally_and_remembers_its_width()
    {
        bool alwaysShow = AppSettings.AlwaysShowCheckoutBranchDlg;
        AppSettings.AlwaysShowCheckoutBranchDlg = true;
        try
        {
            nint bottomHit = 0;
            nint bottomRightHit = 0;
            nint rightHit = 0;
            double resizedWidth = 0;
            DriveNextDialog(window =>
            {
                GetWindowRect(window.NativeHandle, out RECT rect);
                bottomHit = HitTest(window.NativeHandle, (rect.Left + rect.Right) / 2, rect.Bottom - 2);
                bottomRightHit = HitTest(window.NativeHandle, rect.Right - 2, rect.Bottom - 2);
                rightHit = HitTest(window.NativeHandle, rect.Right - 2, (rect.Top + rect.Bottom) / 2);

                // The width is persisted across test runs: alternate between two widths.
                window.Width = window.ClientSize.Width > 700 ? 600 : 760;
                resizedWidth = window.Width;

                // Close once the new width is laid out, so that it is saved.
                DispatcherTimer.RunOnce(window.Close, TimeSpan.FromMilliseconds(300));
            });

            _commands.StartCheckoutBranch(_owner, "");

            bottomHit.Should().Be(HTBORDER);
            bottomRightHit.Should().Be(HTRIGHT);
            rightHit.Should().Be(HTRIGHT);

            double restoredWidth = 0;
            RECT dialogRect = default;
            DriveNextDialog(window =>
            {
                restoredWidth = window.ClientSize.Width;
                GetWindowRect(window.NativeHandle, out dialogRect);
                window.Close();
            });

            _commands.StartCheckoutBranch(_owner, "");

            restoredWidth.Should().BeApproximately(resizedWidth, 1);

            // Still centered over the owner (as FormCheckoutBranch, StartPosition = CenterParent).
            GetWindowRect(_owner.Handle, out RECT ownerRect);
            ((dialogRect.Left + dialogRect.Right) / 2).Should().BeCloseTo((ownerRect.Left + ownerRect.Right) / 2, 4);
            ((dialogRect.Top + dialogRect.Bottom) / 2).Should().BeCloseTo((ownerRect.Top + ownerRect.Bottom) / 2, 4);
        }
        finally
        {
            AppSettings.AlwaysShowCheckoutBranchDlg = alwaysShow;
        }
    }

    [Test]
    public void Content_sized_dialog_keeps_its_height_on_other_resizes_but_follows_its_content()
    {
        bool alwaysShow = AppSettings.AlwaysShowCheckoutBranchDlg;
        AppSettings.AlwaysShowCheckoutBranchDlg = true;
        try
        {
            RECT before = default;
            RECT afterStretch = default;
            RECT afterGrowth = default;
            Avalonia.Controls.SizeToContent sizeToContent = Avalonia.Controls.SizeToContent.Manual;
            DriveNextDialog(window =>
            {
                GetWindowRect(window.NativeHandle, out before);

                // As Win+Shift+Up (stretch vertically): a resize from outside Avalonia's layout.
                SetWindowPos(window.NativeHandle, 0, before.Left, before.Top - 100, before.Right - before.Left, before.Bottom - before.Top + 300, SWP_NOZORDER | SWP_NOACTIVATE);
                GetWindowRect(window.NativeHandle, out afterStretch);
                sizeToContent = window.SizeToContent;

                // The layout still sizes the dialog to its content.
                ((Avalonia.Controls.Control)window.Content!).Margin = new Avalonia.Thickness(0, 0, 0, 100);
                DispatcherTimer.RunOnce(
                    () =>
                    {
                        GetWindowRect(window.NativeHandle, out afterGrowth);
                        window.Close();
                    },
                    TimeSpan.FromMilliseconds(300));
            });

            _commands.StartCheckoutBranch(_owner, "");

            afterStretch.Should().Be(before);
            sizeToContent.Should().Be(Avalonia.Controls.SizeToContent.Height);
            (afterGrowth.Bottom - afterGrowth.Top).Should().BeGreaterThan(before.Bottom - before.Top + 90);
        }
        finally
        {
            AppSettings.AlwaysShowCheckoutBranchDlg = alwaysShow;
        }
    }

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint handle, nint insertAfter, int x, int y, int width, int height, uint flags);

    private static nint HitTest(nint handle, int x, int y)
        => SendMessage(handle, WM_NCHITTEST, 0, (nint)(((y & 0xFFFF) << 16) | (x & 0xFFFF)));

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint handle, uint msg, nint wordParameter, nint longParameter);
}
