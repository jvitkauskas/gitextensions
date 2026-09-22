# Avalonia port: ledger

One row per ported WinForms form. **Ported from** is the upstream commit whose behaviour the Avalonia version
reproduces. After merging upstream, list what changed in the WinForms sources since then and re-apply it:

```bash
git log <ported-from>..upstream/master -- <WinForms sources>
```

Then update **Ported from** to the merged upstream commit.

The WinForms forms are kept (not deleted) until phase 8, so upstream merges apply cleanly and
`GE_AVALONIA=none` falls back to them.

| WinForms form | WinForms sources | Ported from | Avalonia view / view model | Routed in | Notes |
|---|---|---|---|---|---|
| `FormAbout` | `src/app/GitUI/CommandsDialogs/FormAbout*.cs`, `EnvironmentInfo*.cs` | `66050831e` | `GitUI.Avalonia/CommandsDialogs/AboutWindow.axaml`, `GitUI.Presentation/CommandsDialogs/AboutViewModel.cs` | `GitUICommands` (`about` verb), `HelpToolStripMenuItem` | Contributor list logic duplicated in `AvaloniaDialogs.TryShowAbout` (a local function in `FormAbout`). |
| `FormRenameBranch` | `src/app/GitUI/CommandsDialogs/FormRenameBranch*.cs` | `66050831e` | `RenameBranchWindow.axaml`, `RenameBranchViewModel.cs` | `GitUICommands.StartRenameDialog` | Git runs in the WinForms `FormProcess`, owned by the Avalonia dialog. |
| `FormCommitTemplateSettings` | `src/app/GitUI/CommandsDialogs/CommitDialog/FormCommitTemplateSettings*.cs` | `66050831e` | `CommitDialog/CommitTemplateSettingsWindow.axaml`, `CommitTemplateSettingsViewModel.cs` | `FormCommit` (commit templates menu → settings) | Settings stored through `AppSettings`/`CommitTemplateItem`, exactly as before. |
| `FormProcess` / `FormStatus` | `src/app/GitUI/HelperDialogs/FormStatus*.cs`, `FormProcess.cs`, `UserControls/PasswordInput*.cs`, `FormStatusOutputLog.cs` | `66050831e` | `HelperDialogs/ProcessWindow.axaml`, `GitUI.Presentation/HelperDialogs/ProcessViewModel.cs` | `FormProcess.ShowDialog`, `FormProcess.ReadDialog`, `FormStatus.ShowErrorDialog` (~70 call sites) | The console control (`IConsoleCommandRunner`) is reused and embedded. The WSL argument rewriting is duplicated in `AvaloniaDialogs.ResolveProcess`; keep it in sync with `FormProcess`'s constructor. Calls with process input and direct `new FormProcess`/`FormRemoteProcess` uses (Push, Pull, Clone) stay on WinForms. |
| `FormRemoteProcess` | `src/app/GitUI/HelperDialogs/FormRemoteProcess*.cs` | not ported | | | PuTTY/plink handling; port with Push/Pull/Clone (phase 5). |

## Translations

Each view's strings class (`GitUI.Presentation/**/*Strings.cs`) declares the XLIFF ids of the form it replaces.
`GitUI.Avalonia.Tests/ViewModels/ViewStringsTests.cs` fails if an id or its English text no longer matches
`English.xlf`. So when an upstream merge changes a form's strings, that test points at what to update.
