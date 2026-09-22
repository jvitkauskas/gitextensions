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

## Translations

Each view's strings class (`GitUI.Presentation/**/*Strings.cs`) declares the XLIFF ids of the form it replaces.
`GitUI.Avalonia.Tests/ViewModels/ViewStringsTests.cs` fails if an id or its English text no longer matches
`English.xlf`. So when an upstream merge changes a form's strings, that test points at what to update.
