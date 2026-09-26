# Ubuntu GNOME and Fedora KDE VM QA — 2026-09-26

The current experimental port passed the checks below on two additional Wayland compositors. No new application
blocker was confirmed in this pass. This expands the earlier [Arch/Hyprland coverage](QA-Linux-2026-09-26.md);
it does not constitute completion of every item in [QA.md](QA.md).

## Systems and isolation

Both guests built branch `avalonia` at `495ede8b529799c0014ff35975540da8fdc36d89`, with .NET SDK **10.0.201**,
Avalonia **12.1.3**, and the committed **12.1.3-gitextensions-wayland.1** Wayland package. The package verification
helper passed in both fresh clones. Each clone included all submodules and had its own restored dependencies.

| | Ubuntu VM | Fedora VM |
|---|---|---|
| Distribution | Ubuntu 24.04.5 LTS | Fedora Linux 44 |
| Desktop | GNOME Shell 46.0, Mutter, `ubuntu` Wayland session | Plasma/KWin 6.7.5, Wayland session |
| Kernel | 6.8.0-139-generic | 6.19.10-300.fc44.x86_64 |
| Distro Git | 2.43.0 | 2.55.0 |
| Meld | 3.22.2 | 3.24.0 |
| Editor exercised | gedit 46.2 (`--wait`) | Kate 26.08.1 (`--block`) |
| Additional installed merge tool | KDiff3 1.10.7 | KDiff3 1.12.4 |

These are verified official cloud images with full desktop packages added, rather than installations from desktop
ISOs: `ubuntu-desktop-minimal` and Fedora's KDE desktop environment group. Fedora uses Plasma Login Manager;
Ubuntu uses GDM. Both automatically log in to the disposable `qa` desktop account.
The post-install reboot exposed an obsolete Ubuntu cloud-image `systemd-networkd-wait-online` service: the desktop
uses NetworkManager, so the unused wait delayed boot by two minutes. It was disabled; the next boot reached the graphical target in about 6 seconds, with no failed units.

The Linux host runs QEMU/KVM through the user's `qemu:///session` libvirt connection. Each guest has **6 vCPUs,
12 GiB RAM, a 60 GiB expanding qcow2 disk, UEFI/q35 and accelerated virtio graphics (virgl)**. Networking uses
passt with guest SSH forwarded to **127.0.0.1 only**, ports 22221 and 22222. A dedicated new SSH key authenticates
to these guests; host credentials and repositories were not shared into them. The guest accounts have locked
passwords and passwordless sudo for repeatable testing.

Tests used `/home/qa/qa/buildhome`. Manual QA used a complete portable application copy in `/home/qa/qa/app`,
scratch repositories and remotes under `/home/qa/qa`, and `/home/qa/qa/gui-home` for HOME, Git global configuration
and all writable XDG roots. A private D-Bus session was created **after** these environment variables were set.
Most manual tests ran with `DISPLAY` unset and no Wayland opt-in or scaling overrides. Wayland protocol traces,
actual desktop captures and X11 window queries verified backend selection. Input was injected into the VMs through
QEMU, without moving the host's pointer. Native guest screenshot tools captured the composited desktop.

## Automated build and tests

The full Release solution built separately in both guests, with seven warnings and no errors. The build invocation
was `dotnet build GitExtensions.slnx -c Release -p:NETCoreSdkRuntimeIdentifier=linux-x64`.
All 18 test-project invocations ran sequentially in each guest with `--no-build --no-restore --blame-hang-timeout 3m`,
TRX output and an outer 12-minute limit per project. `CommonTestUtils` was excluded.

| Guest | Passed | Skipped | Failed | Test-project invocations |
|---|---:|---:|---:|---:|
| Ubuntu | 25,333 | 167 | 0 | 18 |
| Fedora | 25,333 | 167 | 0 | 18 |

There were no runner errors or timeouts. Linux-inapplicable tests remain skipped, including the Windows UI
integration suite; these results must not be interpreted as automated Linux desktop click-through coverage.

## Actual desktop checks

The following checks were performed on **both** native Wayland desktops unless otherwise stated. Repository
state was checked with Git in addition to inspecting the windows.

| Area | Observed result |
|---|---|
| Startup and browsing | First-run language/checklist dialogs, repository graph and commit details rendered. Meld selected in settings. Ubuntu's older distro Git produced the expected version recommendation; tested workflows still worked. |
| Held modal shortcut | Six cycles of holding Ctrl+O for about 0.8 seconds, releasing, then cancelling with Escape passed per guest. The modal did not spontaneously reopen. This exercises the vendored repeat fix on Mutter and KWin. |
| Stage/unstage/commit | Selected files staged; an individual file unstaged; Stage All used later. Unicode and multiline commit-message paste/copy round-tripped. Commits made through the commit dialog, including Ctrl+Enter. |
| Push and pull | Pushed through the UI to a scratch local bare remote, verified matching object IDs, then pulled a new commit pushed by a separate scratch clone. |
| Conflicts and Meld | Created a real three-way conflict, opened Solve merge conflicts and launched Meld through its Merge action. Edited and saved the merged output in Meld, verified no remaining unmerged index entries, and completed the merge commit through Git Extensions. |
| Display scaling | Live **100% → 200% → 150% → 100%** on the virtual monitor, using 3840×2160 at the higher scales. Settings and a nested font picker remained displayed across the 200%→150% change. Text and controls resized without observed clipping or off-screen dialogs. Protocol scale values were 120/240/180 as expected. No environment scaling overrides. |
| Folder picker | Ctrl+O → Browse opened a real GTK folder picker on both guests; selecting the scratch repository returned to browsing. This was GTK on Fedora too, not a claimed KDE portal picker test. |
| Terminal | Embedded Bash right-click paste and execution produced a scratch file with the exact Unicode text `Wayland terminal Žąsis ✓`. |
| External editors | From the embedded terminal, `git -c core.editor="gedit --wait" commit --allow-empty` on Ubuntu and the equivalent `kate --block` on Fedora opened the editor, waited for save/close and created the intended Unicode commit. This checks Git/editor integration through the terminal, not a separate Git Extensions editor-menu path. |
| Native clipboard | Multiline/Unicode text paste and copy, plus revision hash copy, worked. |
| Wayland → X11 | Copied a revision hash into a visible X11 Tk window. On Fedora, actual Ctrl+V and copying it back verified the exact hash; a Tcl `clipboard get` probe using its default target returned no value, so it was not used as evidence of a failed user paste. |
| X11 → Wayland | An X11 clipboard owner supplied a Unicode shell command; native Git Extensions terminal paste executed it and produced the exact expected scratch file on both guests. |
| Reuse after reboot | Desktop autologin returned to Wayland. The persistent portable launcher created a fresh private D-Bus session and reopened the scratch repository. |

GNOME fractional scaling required its `scale-monitor-framebuffer` experimental feature. It was enabled for the
check, then restored to the original empty list. Both virtual displays returned to 1920×1080 at 100%. No host
display setting was changed in this VM pass.

## Startup/backend matrix

Each case launched the real main application on a scratch repository. The process stayed alive, displayed the
repository and was then closed. Protocol traces identify native windows; PID-scoped visible X11 window queries
identify fallback windows. Screenshots were saved for every case.

| Environment | Ubuntu | Fedora |
|---|---|---|
| Valid WAYLAND_DISPLAY and DISPLAY, no opt-in | Native Wayland | Native Wayland |
| Valid WAYLAND_DISPLAY, DISPLAY unset | Native Wayland | Native Wayland |
| `GITEXTENSIONS_USE_WAYLAND=0` | X11 | X11 |
| Invalid WAYLAND_DISPLAY, valid DISPLAY | X11 fallback | X11 fallback |
| WAYLAND_DISPLAY unset, valid DISPLAY | X11 | X11 |

The X11 cases ran through **XWayland inside these Wayland sessions**. A separate full Xorg desktop session was
not tested. The first Fedora trace parser incorrectly assumed the older libwayland `@` object separator; accepting
Fedora's `#` format and rerunning the cases confirmed all five results. This was a harness issue, not an app failure.

## Limits and remaining work

- No application code change was needed in this pass. Existing findings in the earlier Linux report remain open;
  this pass does not close them merely because these particular workflows passed.
- Virgl virtual graphics do not validate physical GPU drivers, NVIDIA behavior, mixed-DPI physical monitors,
  suspend/resume, real display hotplug or hardware performance.
- Single-monitor scaling was checked. Drag/drop, IME composition, accessibility and every settings/plugin path
  were not comprehensively exercised in these guests.
- Local remotes were used. SSH/HTTPS authentication and real credential-store integration were not retested here.
- KDiff3, gitk and Git GUI were installed for reuse but were not manually exercised in this VM pass. The broader
  seven-tool diff/merge and editor matrix belongs to the earlier Arch report, not these guests.
- This was an x86-64 test of two specific desktop configurations, not certification of every Linux distribution,
  every supported version or ARM64. Windows and macOS were not rerun.

## Retained environment and evidence

The reusable machines, original images/checksums, scripts and evidence are outside the repository at
`/home/julius/VirtualMachines/gitextensions-qa`. Its `README.md` documents starting, viewing and accessing them.
Each guest directory contains manual PNG captures, `backend-results.json`, `test-totals.json` and
`tests-and-logs.tar.gz` (build/test logs, all TRX files, protocol traces and package inventory). The guest source
clones, SDK, built app, scratch repositories and desktop launcher remain available.

Host packages installed for this work include `qemu-desktop`, `libvirt`, `virt-manager`, `virt-viewer`, `cloud-utils`,
`cdrtools` and `passt`, with their dependencies; exact requested package versions are recorded in
`logs/host-packages.txt`. All test applications were closed, then both VMs were shut down. Cold post-QA disk and
UEFI-state checkpoints were retained for reuse. They do not autostart with the host. The host Git configuration
checksum remained unchanged. No host sudo password was saved in the VM files or repository.
