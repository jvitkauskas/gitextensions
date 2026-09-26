# [Wayland] Modal dialog opened by a shortcut keeps reopening after key release

Submitted as [AvaloniaUI/Avalonia#22317](https://github.com/AvaloniaUI/Avalonia/issues/22317).
The report below describes stock 12.1.3; the later local source-patch validation is recorded in
[the Linux QA report](QA-Linux-2026-09-26.md#local-avalonia-source-patch-2026-09-26).

## Description

With Avalonia.Wayland 12.1.3 on Hyprland, opening a modal dialog using Ctrl+O can leave the owner's keyboard repeat active after the keys are released. Closing or cancelling the dialog causes it to reopen without another Ctrl+O. Additional instances can also appear beneath a nested folder picker.

The Wayland protocol trace contains the O key release and keyboard-leave event. This appears to be a client-side repeat-state problem when the modal owner becomes disabled.

## Environment

- Avalonia / Avalonia.Wayland: **12.1.3**.
- OS: Arch Linux x86_64; kernel 7.2.3-arch1-3.
- Compositor: Hyprland **0.56.2**.
- .NET SDK: 10.0.111; application targets `net10.0`.
- Backend: native Wayland, confirmed by Hyprland's `xwayland: false`; `DISPLAY` unset.
- Display: 3840×2160 at 200%; no Avalonia scale override. Other scales were checked for rendering, but this input failure was recorded at 200%.
- Application: Git Extensions' Avalonia port. The affected integration is available in [commit b6d307f586e122166a2dcd443421ae52477f6618](https://github.com/jvitkauskas/gitextensions/commit/b6d307f586e122166a2dcd443421ae52477f6618).

## Reproduction in the application

1. Build the linked revision and prepare a portable copy with first-run setup completed, using a disposable Git repository.
2. Launch the copy with the native backend and protocol logging:

   ```sh
   env -u DISPLAY -u AVALONIA_GLOBAL_SCALE_FACTOR -u AVALONIA_SCREEN_SCALE_FACTORS \
     GITEXTENSIONS_USE_WAYLAND=1 WAYLAND_DEBUG=client \
     /path/to/portable/GitExtensions browse /path/to/disposable/repo \
     2>wayland.log
   ```

   Keep the session's valid `WAYLAND_DISPLAY` and `XDG_RUNTIME_DIR`. `GITEXTENSIONS_USE_WAYLAND` is an application-specific opt-in that selects `UseWaylandWithFallback()`; verify that the process actually uses Wayland.

3. Focus the main window and press/release Ctrl+O to open **Open local repository**.
4. Cancel or close the dialog. It can immediately reopen without a further Ctrl+O. Opening its Browse folder picker can also expose additional repository dialogs underneath.

The recorded reproduction used Hyprland's key-state injection, with separate down and up calls:

```sh
hyprctl dispatch 'hl.dsp.send_key_state({ mods = "CTRL", key = "O", state = "down" })'
hyprctl dispatch 'hl.dsp.send_key_state({ mods = "CTRL", key = "O", state = "up" })'
```

This was not a manually typed hardware-keyboard reproduction. It did not use `wtype` for the triggering shortcut; earlier `wtype` text-injection experiments had custom-keymap issues and are not the basis for this report.

The application calls `window.ShowDialog(owner)` from its shortcut handling path, then runs `Dispatcher.UIThread.PushFrame(frame)` until the dialog's `Closed` event ends the frame, supporting synchronous callers. See [the modal host](https://github.com/jvitkauskas/gitextensions/blob/b6d307f586e122166a2dcd443421ae52477f6618/src/app/GitUI.Avalonia/Hosting/AvaloniaDialogHost.cs#L199-L226). A standalone minimal sample and an ordinary async-only `ShowDialog` comparison have not yet been tested.

## Expected behavior

One shortcut press opens one dialog. Releasing the key, losing keyboard focus, or disabling the owner stops its repeat. A disabled owner should not continue receiving repeated shortcut input. Closing the dialog should leave it closed until the next explicit invocation.

## Observed protocol evidence

Selected lines from the same `WAYLAND_DEBUG=client` recording; omitted intervals are represented by `...`:

```text
[12:00:47.358150] wl_keyboard#66.key(5827, 8604412, 24, 1)
[12:00:47.361543] wl_keyboard#66.key(5831, 8604412, 24, 0)
[12:00:47.367387]  -> xdg_toplevel#76.set_title("Open local repository")
[12:00:47.367390]  -> xdg_toplevel#76.set_parent(xdg_toplevel#74)
[12:00:47.370504] wl_keyboard#66.leave(5836, wl_surface#70)
...
[12:03:28.027164] xdg_toplevel#96.close()
...
[12:03:28.067297]  -> xdg_toplevel#96.set_title("Open local repository")
```

Key 24 is O; states 1 and 0 are press and release. There is no further O key-down in the recording after the initial pair. The later compositor close is followed by another repository dialog approximately 40 ms later. Protocol object IDs can be reused after destruction; the last two lines do not imply that the original dialog remained open.

## Suspected cause in the pinned backend

Source inspection suggests two paths that together explain the behavior:

1. [`WindowBaseImpl.Sink.DispatchInput`](https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Wayland/WindowImplBase.cs#L212-L224) returns when `Parent.IsEnabled` is false **before** calling `HandleKeyboardDispatch`. Queued key-up and keyboard-leave events therefore cannot reach repeat cleanup while the modal owner is disabled.
2. [`HandleKeyboardDispatch` and `OnKeyRepeatTick`](https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Wayland/WindowImplBase.Keyboard.cs#L57-L135) stop repeat on key-up/leave, but the timer invokes `Parent.Input` directly without checking whether the parent is enabled. A timer left active can keep dispatching the stored Ctrl+O shortcut, bypassing the disabled-window check.

This diagnosis is based on the source and protocol/visible behavior, not a debugger capture of the timer or a validated backend patch.

## Suggested fix and regression coverage

Please consider stopping repeat when a window becomes disabled, preserving key-release/focus-loss cleanup while disabled, and ensuring repeat ticks cannot dispatch into a disabled or disposed window. Simply moving all keyboard handling before the enabled check could start repeats for new key-down events on a disabled window, so cleanup and new input likely need separate treatment.

Useful regression cases would include:

- Open a modal from KeyDown, then release the triggering key after the owner is disabled; cancel and verify no reopening.
- Lose keyboard focus to the modal before key-up is processed; verify repeat stops.
- Hold the shortcut beyond the repeat delay while the owner is disabled; verify no additional dialog invocation.
- Close/dispose a window with repeat active; verify no subsequent input dispatch.
- Verify normal held-key repeat still works in an enabled text control.
- Exercise both nested dispatcher frames and an async-only modal caller.

## Impact and scope

This makes keyboard-opened modal workflows unreliable, so Git Extensions currently leaves native Wayland opt-in and retains X11/XWayland as the default. No workaround for this repeat bug is applied to Avalonia internals. Other compositors, a newer Avalonia version, and a standalone reproducer have not been checked.
