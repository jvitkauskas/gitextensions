# QA click-through of the macOS port

A manual check of Git Extensions (the Avalonia port, branch `avalonia`) on macOS, by clicking through the application
as a user would. Read [HANDOFF.md](HANDOFF.md) first (state of the port, rules), and the "Platforms" section of
[ledger.md](ledger.md) for what was already checked.

## Setup

- Build: `dotnet build GitExtensions.slnx -c Release` (after `git submodule update --init`). The application is
  `artifacts/Release/bin/GitExtensions/net10.0/GitExtensions`.
- Copy that folder to a scratch folder and run the copy as portable, so its settings stay in that folder.
- Start it with `HOME` set to a scratch folder and without `GIT_EDITOR` (`env -u GIT_EDITOR HOME=<scratch> ...`), so
  git's global config is a scratch `.gitconfig`. The Keychain, gh and VS Code need the real `HOME`: then use
  `GIT_CONFIG_GLOBAL=<scratch>/.gitconfig` instead.
- Work in scratch repositories only (make commits, branches, conflicts, stashes, remotes there). A local bare
  repository, or a user-level `sshd` on 127.0.0.1, can stand in for a server.
- Driving the UI: `screencapture -x -o -l <window id>` (window ids from `CGWindowListCopyWindowInfo`, e.g. through
  `osascript -l JavaScript`) and `cliclick`. Pitfalls:
  - Escape must be sent with `osascript -e 'tell application "System Events" to key code 53'`.
  - A lost click is retried with `dd:x,y w:100 du:x,y`.
  - Check `lsappinfo front` before typing, since someone may be using the Mac.
  - A macOS crash report dialog swallows input; close it with `killall UserNotificationCenter`.
  - A locked screen stops Avalonia from starting.

## What to test

Go through each area and watch for errors, crashes, hangs, clipped or unreadable text, wrong button order (macOS:
Cancel before OK), and Ctrl where Cmd is expected.

1. **Startup**: the first-run dialogs (language, the settings checklist), the dashboard, open and clone a repository,
   the recent repositories.
2. **Main window**:
   - The revision grid (graph, selection, Cmd+C copies hashes, the context menus) and the left panel (branches,
     remotes, tags, stashes, submodules).
   - The commit info, diff, file tree, blame and console tabs.
   - The macOS menu bar (the application menu with About, Settings Cmd+, and Quit Cmd+Q, the main menu and its
     shortcuts).
   - Light, dark and system themes.
3. **Everyday git**:
   - Commit (stage, unstage, amend, Cmd+Return), and push, pull and fetch (to a local remote, over SSH with the host key
     and passphrase prompts, over HTTPS).
   - Create, check out, rename, delete and merge branches; rebase, cherry-pick, revert, reset.
   - Stash, tags, remotes (with color and prefix), submodules, worktrees.
4. **Conflicts**: the conflicts dialog and "Open in <tool>" for each installed merge tool (FileMerge, KDiff3, Meld,
   P4Merge, Sublime Merge, Beyond Compare, VS Code); diff tools from the diff tab.
5. **Tools and dialogs**:
   - The settings pages (fonts, hotkeys, git config, editors, diff/merge tools), the file history, the search and
     filter boxes.
   - The file, folder, font and color pickers; scripts; the terminal tab (zsh).
   - Plugins (e.g. Find large files, Delete unused branches, Statistics); build server integration (a fake server is
     fine).
6. **Credentials**: the askpass prompts, and saving to the Keychain. Delete every Keychain item the test created
   (`security delete-generic-password -s <name>`).

## When something is wrong

Try to fix it, in the style of the surrounding code:

- Find the cause first: reproduce it, and read the log of the app and the console.
- Fix it for every system when the bug is general. Put a macOS-only change behind `OperatingSystem.IsMacOS()` when it
  would change Windows or Linux.
- Add or extend a headless test in `tests/app/UnitTests/GitUI.Avalonia.Tests` (or the matching project). The test
  fails without the fix and passes with it.
- Build, run every test suite, check the fix in the app again, and update the ledger (and HANDOFF.md if the state
  changed).
- Commit per the rules of HANDOFF.md (the co-author line, no `prototypes/`, and say what could not be checked on
  other systems).
- When a fix is too large or unclear, write the issue down (steps, screenshot, what was expected) instead of guessing.

Finish with a short report: what was tested, what was fixed (commits), what is still open. Close the application and
every helper process, and confirm that `~/.gitconfig` and the Keychain are as they were.
