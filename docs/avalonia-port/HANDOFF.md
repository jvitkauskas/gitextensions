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
  (all but `UI.IntegrationTests`). Left: macOS (not checked: no Mac here).
- Phase 3 (git, tools and processes on each system) is done for Linux: the file manager, git discovery, editors, diff
  and merge tools, shells, scripts and plugins. The owner checks macOS afterwards; the macOS branches exist (`open`,
  TextEdit, Homebrew folders, Araxis) but were not run.
- Phase 4 (credentials, SSH, the Windows capabilities) is done for Linux: the askpass prompt of Git Extensions, the
  Secret Service for credentials, the Windows features hidden. macOS needs its Keychain (`ICredentialStore`).
- Work in batches: each ends with a build, the full test suites (on Windows, and the portable ones on Linux), a commit,
  a fast-forward of `avalonia` and a push of it.

## Next: macOS

Nothing has run on macOS yet; the macOS branches of phases 2-4 are written but untested (the ledger's "Platforms"
section says which). Windows and Linux are checked separately by the owner: keep their behavior unchanged, put
macOS-only changes behind `OperatingSystem.IsMacOS()` where shared code would otherwise change, and say in each commit
what could not be verified. In batches:

1. **Build and test.** The .NET SDK of `global.json`, the build and every test suite (below). `UI.IntegrationTests`
   is Windows-only (NUnit skips it). The headless tests measure text with the system fonts.
2. **Smoke test** a portable copy (below) on scratch repositories:
   - startup with the native backend (Avalonia.Native), browse, diff, blame, commit, push and pull, stash;
   - message boxes, task dialogs and dialogs (synchronous modality with Avalonia owners), the file, folder, color and
     font pickers, the clipboard;
   - dark mode (`SystemTheme`: `defaults read -g AppleInterfaceStyle`);
   - the terminal tab (`$SHELL`, zsh);
   - git discovery (`/opt/homebrew/bin`, `/opt/local/bin`; `/usr/bin/git` is the Xcode shim);
   - editors (TextEdit with `open -W -n -e`, `code --wait`, `subl --wait`);
   - diff and merge tools (Araxis is offered on macOS; add FileMerge / `opendiff`; search
     `/Applications/*.app/Contents/MacOS` in `PathUtil.FindInFolders`, as the plan says);
   - `open` and `open -R` (`OsShellUtil`: folders, Show in folder);
   - the askpass prompt (`SSH_ASKPASS` is the script of `AskPassScript`, which runs `GitExtensions askpass`) with a
     passphrase-protected key.
3. **Keychain.** An `ICredentialStore` for the macOS Keychain in
   `src/app/GitExtensions.Extensibility/Settings/CredentialStore.cs` (macOS has `NoCredentialStore` today: the
   credentials are kept for the session only). Tests that write to the Keychain are opt-in, as the Secret Service one is.
4. **Phase 5 of `CROSS-PLATFORM.md`:** Cmd instead of Ctrl (hotkeys stay stored as `Control`), the application menu
   (`NativeMenu`: About, Settings with Cmd+,, Quit), the order of dialog buttons, file dialog filters, fixed widths that
   clip with the macOS fonts.
5. Later: phase 6, the `.app` bundle, signing and notarization.

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
- Translations: a new or changed string of a strings class (`GitUI.Presentation/**/*Strings.cs`) needs English.xlf
  regenerated: `cd src/app/GitExtensions && dotnet msbuild -p:Configuration=Release -t:_UpdateEnglishTranslations
  -p:RunTranslationApp=true`. `ViewStringsTests` fails until then; new strings classes are added to its list.
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
