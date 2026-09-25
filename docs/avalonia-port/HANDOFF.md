# Handoff: working on the Avalonia port

Notes for whoever continues the port (a person or an agent session), on any machine. The plans are `PLAN.md` (the
port, done) and `CROSS-PLATFORM.md` (macOS and Linux, next); `ledger.md` records every ported form and what differs.

## State (2026-09-25)

- Branch `avalonia` (and `avalonia-3rdparty`, the same commit) of the fork `jvitkauskas/gitextensions`: the Avalonia port
  is complete on Windows, WinForms is removed, and the parity gaps of the ledger are closed or recorded as deliberate.
- Phase 0 of `CROSS-PLATFORM.md` (guard rails and CI) is done: the solution builds on Linux, and CI has a Linux job.
  Next: phase 1 (portable core libraries), approved by the owner. The dependency order of its projects is
  GitExtensions.Extensibility, GitExtUtils, GitUIPluginInterfaces, GitCommands, ResourceManager, GitUI.RevisionGraph,
  GitUI.Presentation; no test runs on Linux until GitCommands is portable, since CommonTestUtils references it.
- Work in batches: each ends with a build, the full test suites, a commit, and a push of the branch.

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
  case is checked as on a real Linux machine). The tests of the `net10.0-windows` projects are skipped there by NUnit.
- Translations: a new or changed string of a strings class (`GitUI.Presentation/**/*Strings.cs`) needs English.xlf
  regenerated: `cd src/app/GitExtensions && dotnet msbuild -p:Configuration=Release -t:_UpdateEnglishTranslations
  -p:RunTranslationApp=true`. `ViewStringsTests` fails until then; new strings classes are added to its list.
- A portable copy of the app for smoke tests: copy `artifacts/Release/bin/GitExtensions/net10.0-windows` to another
  folder, keeping that folder's own `GitExtensions.settings` (portable mode) between copies.
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
- From Git Bash, `wsl.exe` arguments are mangled twice: MSYS converts paths (set `MSYS_NO_PATHCONV=1`) and `wsl.exe`
  expands `$` through the default shell of the distribution. Put Linux commands in a script file and run it with
  `wsl.exe -e bash <script>`.
