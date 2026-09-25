# Handoff: working on the Avalonia port

Notes for whoever continues the port (a person or an agent session), on any machine. The plans are `PLAN.md` (the
port, done) and `CROSS-PLATFORM.md` (macOS and Linux, next); `ledger.md` records every ported form and what differs.

## State (2026-09-25)

- Branch `avalonia` of the fork `jvitkauskas/gitextensions`: the Avalonia port is complete on Windows, WinForms is
  removed, and the parity gaps of the ledger are closed or recorded as deliberate. (`avalonia-3rdparty` stayed at the
  commit before the cross-platform work.)
- Phase 0 of `CROSS-PLATFORM.md` (guard rails and CI) is done: the solution builds on Linux, and CI has a Linux job.
- Phase 1 (portable core libraries) is done: GitExtensions.Extensibility, GitExtUtils, GitUIPluginInterfaces,
  GitCommands, ResourceManager, GitUI.RevisionGraph and GitUI.Presentation target `net10.0`, and their tests run on Linux.
  The "Platforms" section of the ledger says what runs where.
- Phase 2 (the application starts on Linux and macOS) is under way, approved by the owner. The spike of the modal
  message boxes passed on X11 (WSLg) and Windows. Done: `Avalonia.Desktop`, owners, message boxes, task dialogs, common
  dialogs, clipboard, system theme; all application projects target `net10.0`. Under WSLg the application starts,
  browses, diffs and commits. Every project, plugins and tests included, targets `net10.0`, and the tests run on Linux
  (all but `UI.IntegrationTests`). macOS: see "macOS" below.
- Phase 3 (git, tools and processes on each system) is done for Linux and macOS: the file manager, git discovery,
  editors, diff and merge tools, shells, scripts and plugins.
- Phase 4 (credentials, SSH, the Windows capabilities) is done for Linux and macOS: the askpass prompt of Git
  Extensions, the Secret Service and the Keychain for credentials, the Windows features hidden.
- Phase 5 (macOS and Linux conventions) is under way on macOS: Cmd, the application menu, the dialog buttons, fonts.
- Work in batches: each ends with a build, the full test suites (on Windows, and the portable ones on Linux), a commit,
  a fast-forward of `avalonia` and a push of it.

## macOS (2026-09-25)

The macOS pass of phases 2-5 ran on a Mac (Apple silicon, macOS 26, .NET SDK 10.0.401, Xcode). Windows and Linux are
checked separately by the owner: keep their behavior unchanged, put macOS-only changes behind `OperatingSystem.IsMacOS()`
where shared code would otherwise change, and say in each commit what could not be verified.

Done and checked there (the ledger's "Platforms" section has the details): the build and every test suite (the submodules
must be checked out); the native backend; browse, diff, blame, commit, push, pull, stash; message boxes, task dialogs, the
file and font pickers (native open panel as a sheet), the clipboard; the dark theme (it was unreadable off Windows); the
terminal tab (zsh); git from Homebrew; editors (TextEdit with `open -W -n -e` waits); diff and merge tools (bundles of
`/Applications`, FileMerge / `opendiff`, Beyond Compare's `bcomp`); `open` and `open -R`; the askpass prompt with
macOS's OpenSSH (`ssh-add` of a key with a passphrase); the Keychain (opt-in test); Cmd for the shortcuts; the
application menu (About, Settings with Cmd+,, Quit that closes the windows) and the main menu in the menu bar, with the
shortcuts, also over the other windows; the order of the dialog buttons; the
default UI font (13 pixels, it was 17); the scaling of Retina displays (`DpiUtil`).

Left for macOS, in batches:

1. **Not checked**: Araxis (not installed), DiffMerge (its Homebrew cask is disabled), merges in each tool, a push over
   SSH to a server, the credentials from the plugin settings, the color picker.
2. **Phase 6**: the `.app` bundle, signing and notarization. The application cannot start while the display sleeps
   or the screen is locked (Avalonia.Native: "not able to start the RenderTimer", -6661).

Done in the last batch of phase 5: the order of the buttons of the settings (`StackPanel.dialogButtons`) and of the
askpass prompt (a dialog footer), the theme of the askpass prompt (`ThemeModule.Load` in the askpass mode), the sizes
with decimals of the font picker and the Fonts page (9.75), the defaults of the hotkeys on keys that macOS takes
(Commit Cmd+Return, the tabs Cmd+Shift+] / [, the multi-selection of the left panel Cmd+Shift+Space; saved hotkeys on
such keys are moved at load), the menu of the main window in the menu bar of the other windows (without shortcuts,
disabled while modal), the default extension of the save dialogs (`*..txt` in the filter of the file history). Checked
with the macOS fonts: the stash, commit, push, create branch and settings dialogs fit.

Seen on macOS and fixed on every system: Cmd/Ctrl+C in the revision grid copies the hashes of the selection again (the
DataGrid copied its empty template cells), the commit dialog gives the focus to the unstaged files, the message or Amend
once its files are loaded (as `LoadUnstagedOutput`), and `dotnet GitExtensions.dll` takes the folder of the application
rather than the one of `dotnet` (`ApplicationInfo.GetExecutablePath`), and the translation target runs off Windows.

## Rules

- Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` when an agent makes them. Do not commit the
  untracked `prototypes/` folder.
- Tests must not change the machine: no writes to the real Git Extensions settings, the registry
  (`HKCU\Software\GitExtensions`, e.g. `gitcommand`), `WindowPositions.xml` or real repositories. A test that changes an
  `AppSettings` value saves and restores it, or uses a fake.
- Run tests with `--blame-hang-timeout 3m` (as CI): a test that shows a modal dialog otherwise hangs forever, and the
  slowest healthy test takes about 25 s. The driven dialogs of `UI.IntegrationTests` close themselves after 2 minutes
  or when a step of their driver fails, so such a test fails instead of hanging. The test assemblies run one at a time:
  each holds an exclusive lock on `%TEMP%/GitExtensionsTestAssemblySerializer.lock` (`TestAppSettingsAttribute`), which
  the system releases when a test host is killed.
- Smoke tests of the real app run a copy with portable settings (below), on scratch repositories, never on the
  user's own; close every instance afterwards (`Stop-Process -Id <pid> -Force` on Windows) and check it is gone.
  Starting the app on a folder that is not a repository may open the last used repository instead.

## Build and test

- Build: `dotnet build GitExtensions.slnx -c Release` (outputs under `artifacts/Release/bin`). `TreatWarningsAsErrors` is
  on, with StyleCop (e.g. SA1515: a comment needs a blank line before it; SA1119: no needless parentheses).
- Tests: every `tests/**/*.csproj` except `CommonTestUtils`, with `--no-build --blame-hang-timeout 3m`; 18 suites.
- Linux: the solution builds with the .NET 10 SDK (e.g. in WSL: `dotnet-install.sh --channel 10.0`, plus `libicu`
  and `fontconfig`). Build in a copy on the Linux file system rather than under `/mnt/c` (much faster, and file name
  case is checked as on a real Linux machine). NUnit skips the Windows-only tests there (`UI.IntegrationTests`, and the
  tests marked `[Platform(Include = "Win")]`). Test data written as Windows paths goes through `TestPaths.Native`.
- macOS: `brew install --cask dotnet-sdk` (Microsoft's; the `dotnet` formula that `powershell` pulls in must not shadow
  it), Xcode for FileMerge, `git submodule update --init` before the first build. The opt-in Keychain test:
  `GE_TEST_KEYCHAIN=1`. Smoke tests: the portable copy started with `HOME` set to a scratch folder (git's global config
  then is a scratch `.gitconfig`: the Repair buttons and the diff tool settings write to it) and without `GIT_EDITOR`
  (agent shells set it to `true`, which overrides `core.editor`). Windows are captured with `screencapture -l <id>`
  (the ids from `CGWindowListCopyWindowInfo`, e.g. through `osascript -l JavaScript`) and driven with `cliclick`
  (Accessibility and Screen Recording for the terminal app). `cliclick kp:esc` does not reach Avalonia windows: send
  Escape with `osascript -e 'tell application "System Events" to key code 53'`. Check the front app (`lsappinfo front`)
  before typing, as someone else may be using the Mac. A crash report dialog of macOS ("quit unexpectedly") takes the
  keyboard and ignores synthetic input: `killall UserNotificationCenter` closes it.
- Translations: a new or changed string of a strings class (`GitUI.Presentation/**/*Strings.cs`) needs English.xlf
  regenerated: `cd src/app/GitExtensions && dotnet msbuild -p:Configuration=Release -t:_UpdateEnglishTranslations
  -p:RunTranslationApp=true`. `ViewStringsTests` fails until then; new strings classes are added to its list. The target
  also runs off Windows (`dotnet TranslationApp.dll`, `grep`); it wrote an incomplete English.xlf there before, since the
  plugins were looked for next to `dotnet` (`ManagedExtensibility` now uses `AppContext.BaseDirectory`).
- CI has not run on this branch: `app-build.yml` builds `master`, `release*` and `experimental/**` pushes and pull
  requests only (add the branch, or use `workflow_dispatch`, to get the Linux job and the Windows publish with the MSI).
- A portable copy of the app for smoke tests: copy `artifacts/Release/bin/GitExtensions/net10.0` to another folder,
  keeping that folder's own `GitExtensions.settings` (portable mode) between copies. Portable mode is `IsPortable` =
  `True` in its `GitExtensions.dll.config`.
- Linux smoke tests (WSLg): the same portable copy of the Linux build, started with `DISPLAY=:0` on a scratch repository
  in the Linux file system, with a `GitExtensions.settings` that sets `translation`, `CheckForUpdates`=false and
  `CheckSettings`=false (else the language dialog and the checklist come first) and `gitcommand` (`/usr/bin/git`).
  Its windows cannot be captured from Windows (they are remote windows): `import -window <id>` (ImageMagick) with the
  ids of `xwininfo -root -tree` (class `GitExtensions`); `xdotool mousemove <x> <y> click 1` (screen coordinates,
  `--window` does not reach them) and `xdotool type` drive them. Kill the process afterwards.
- Publish needs an existing `artifacts/Release/publish/GitExtensions.PluginManager` folder to skip the download. Its
  PowerShell steps run Windows PowerShell with `-ExecutionPolicy Bypass` on Windows, `pwsh` elsewhere.

## Avalonia and editor quirks found during the port

- `TextBox.TextChanged` is raised after the change is applied, even for programmatic changes: code that reacts to its own
  edits must recognise them (see `ComboBoxSuggestAppend`).
- Avalonia 12 has no `ActualThemeVariantProperty` for class handlers: subscribe to `ActualThemeVariantChanged`.
- A `MenuFlyout` keeps the items it was first shown with; menus built when they open use `FreshMenuFlyout`.
- A `MenuItem` with `ToggleType` toggles itself when clicked; with a one-way `IsChecked` binding, raise the property
  changed notification after the command so the check mark follows the model.
- The `DataGrid` lists its selected items in row order; the revision grid tracks the selection order itself.
- AvaloniaEdit: `ScrollToVerticalOffset` does not scroll (set the offset of its `ScrollViewer`); the selection is dropped
  when the caret leaves it; there is no IME pre-edit.
- Windows wheel events lack the Alt modifier; track Alt from the key events of the top level.
- In headless tests, `await` continuations may leave the UI thread; poll with `Dispatcher.UIThread.RunJobs()` instead.
- Test fixtures of `GitRef` need a module (`TestGitModule.Instance`), since `IsTrackingRemote` reads `MergeWith` from it.
- Shell heredocs mangle `\\`, `\n` and similar escapes in code being edited; write edit scripts to a file instead.
- Linux GUI in WSL: WSLg shows X11 windows on the Windows desktop (`DISPLAY=:0`); Avalonia's X11 backend needs
  `libice6`, `libsm6` and the X11 libraries (`apt-get install libice6 libsm6 libx11-xcb1 libxrandr2 libxi6 libxcursor1`).
- `TranslationApp.exe` is started by name from its folder by `_UpdateEnglishTranslations`; a shell with
  `NoDefaultCurrentDirectoryInExePath` set (e.g. an agent's) cannot find it: unset it for that command.
- From Git Bash, `wsl.exe` arguments are mangled twice: MSYS converts paths (set `MSYS_NO_PATHCONV=1`) and `wsl.exe`
  expands `$` through the default shell of the distribution. Put Linux commands in a script file and run it with
  `wsl.exe -e bash <script>`.
