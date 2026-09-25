# Windows QA — 2026-09-25 (in progress)

This pass follows QA.md on Windows. Linux manual QA is deferred at the owner's request; Linux builds and automated
suites still validate shared changes. Scratch portable application, HOME, Git global config, repositories and a local
bare remote are under `%LOCALAPPDATA%/Temp/ge-qa-20260925`. Beyond Compare 3 is excluded by the owner.

## First fix batch

- Portable startup no longer registers its own folder as the installed Git Extensions directory. Reading or importing
  absent registry settings no longer creates an empty `HKCU/Software/GitExtensions` key.
- The portable checklist startup preference is stored in its settings file, including automatic disabling after a successful scan.
- Saving unchanged shell-extension settings no longer writes their default values to the registry when another
  settings page is saved.
- Discovery finds current KDiff3 `bin`, Unity Version Control's SemanticMerge, the current Araxis machine/per-user
  layouts, Visual Studio instances reported by `vswhere`, and Zed's current `bin` folder. Legacy VS search paths now
  return a directory, avoiding a doubled executable name.

Validation: Release solution builds on Windows and WSL; all 18 Windows and 17 Linux suite invocations pass with
`--no-build --blame-hang-timeout 3m`. Discovery, portable registration and unchanged shell-settings regression tests
were observed failing without their fixes. The actual Windows Settings page finds the four previously missing merge
tools, and a probe of the built definitions finds all 14 installed Windows profiles. The editor menu resolves Zed.
macOS was not available for this batch.

## Manual results so far

- First-run language and checklist; main revision graph and commit info, left-panel headings, light theme at the
  machine's display scaling; Ctrl+C copies the selected revision's exact hash without changing selection.
- Commit dialog initial focus, stage, unstage, stage all, and Ctrl+Enter commit; resulting repository is clean.
- Settings: editor presets, Notepad++ selection, merge/diff configuration saved to scratch Git global config;
  Araxis, KDiff3, SemanticMerge and VS 2022 auto-discovery rechecked in the UI.
- WinMerge: launched from "Open in winmerge" on a three-way conflict with a space in the filename, chose local text,
  saved, closed, and verified output and empty unmerged index. Git Extensions waited for the tool and offered to
  commit the resolved merge. The No response dismissed that prompt.
- Push from the main toolbar to the local bare remote: exit 0 and the remote main ref matches the committed hash.

- Pull from the main toolbar fetched a remote change and fast-forwarded the working branch (exit 0).
- Create branch with checkout, checkout back to main, merge feature (a non-fast-forward merge commit), create tag,
  and delete the temporary branch all produced the expected Git refs.
- Built-in editor opened from the Diff tab, saved an added line, and closed; the file content was verified.
- Blame displayed the expected author and line in the File tree tab; File history opened a separate filtered browse window.
- Stash all with a message cleaned the working tree, Apply restored the edit, and Drop with confirmation removed the test stash.
- WinMerge two-way diff from the Diff tab correctly displayed the addition of remote.txt against an empty left side.

## Still open

The remaining base-functionality, diff/merge and editor matrix is being exercised. Installation/discovery is not a
saved-merge or editor-wait sign-off. In particular, tool licensing/first-run barriers are not yet assessed.

## Tool and editor follow-up

Each merge below was started from the application's "Open in <tool>" button against a scratch three-way conflict in
`merge sample.txt`. Successful saves were checked on disk and against `git ls-files -u`, including the application's
wait and subsequent offer to commit. No merge commits were made in these fixtures.

| Profile | Observed result |
|---|---|
| winmerge | Chose local text, saved, closed; conflict cleared. Two-way addition diff also displayed correctly. |
| kdiff3 | Selected lines from B, saved local text, closed; conflict cleared. |
| p4merge | Selected local text in the result, saved, closed; conflict cleared. |
| bc | Beyond Compare 4 Text Merge saved local text; conflict cleared after closing. |
| diffmerge | DiffMerge Edit View copied local text into the result; save/close cleared the conflict. |
| meld | Slow first launch, then local-to-middle copy, save and close cleared the conflict. |
| smerge | Sublime Merge used the intended output path; saved a resolution removing the disputed line, retaining header/footer; conflict cleared. |
| tortoisediff | TortoiseGitMerge copied the local block, saved and closed; conflict cleared. |
| tortoisemerge | Same executable and merge arguments; opened correctly. Closing without saving retained all three unmerged index entries. |
| vscode | Skipped account sign-in, completed first-run UI, accepted local text, saved and closed; conflict cleared. |
| vsdiffmerge | Saved local text using Take Right / Accept Merge; conflict cleared when the launcher returned. |
| araxis | Opened registration requiring a serial number. Cancel left the conflict intact. No license was supplied. |
| semanticmerge | Unity VCS reported missing per-user `plastic4/client.conf`. Dismissed the error; no resolution claimed. |
| bc3 | Excluded by the owner. |

Visual Studio's CoreEditor-only installation was insufficient: `vsDiffMerge` returned exit 1 without a window,
including a standalone invocation using Microsoft's documented syntax. Initial setup and `/setup` did not resolve
it. Adding `Microsoft.VisualStudio.Workload.ManagedDesktop` completed successfully and registered `VisualStudio.DTE.17.0`;
the unchanged Git Extensions profile then worked. No Git Extensions command-line change was needed.

Editor commands produced by the built Git Extensions presets were placed in scratch repository `core.editor`
settings. Notepad++, Sublime Text, Zed, VS Code and Windows Notepad each opened `COMMIT_EDITMSG`, saved a new message,
kept Git waiting while the window remained open, and completed the expected commit after closing. The built-in file
editor was separately exercised from the Diff tab. Terminal `vi` remains to be checked in the application.

Further base checks: Clone to a scratch directory, the post-clone Open prompt, and checkout of a remote branch to a
local tracking branch. The first clone displayed Git's warning about the scratch bare remote's nonexistent default
HEAD; the fixture's HEAD was corrected to `main`. Revert without auto-commit staged the expected deletion; the Commit
dialog committed it, and Amend with its confirmation rewrote that unpublished commit. Fonts opened the native Windows
picker and accepted a changed code-font size; Hotkeys displayed Browse mappings; the SSH page exposed Windows clients.

The explicit dark theme reproduced white text on light backgrounds after restart. The cause was the Windows fallback
to system light colors for values omitted by dark.css, formerly supplied by WinForms dark mode. Avalonia no longer
enables that mode. Dark defaults now apply on every platform; explicit theme overrides and Windows light system colors
are preserved. Five new regression cases failed before the fix. Build, suite and visual rechecks are in progress.

The automatic checklist preference write was also reproduced and fixed. Its regression test reads the isolated
setting before testing a write, so the old implementation fails without touching the real registry.

The Computer Use helper did not expose owned Avalonia windows as targetable windows. Native UI Automation and
DPI-aware PrintWindow, as described in QA.md, are used for the rest of the pass. Owned dialogs may require coordinates
where their UI Automation descendants are unavailable. This is an automation limitation, not an established app hang.

## Isolation and evidence

Evidence, full test logs/TRXs, tool inventory and machine snapshots remain in the scratch root. The real `.gitconfig`
hash still matches its baseline. Final process, registry and credential cleanup is pending completion of the pass.

During one build-copy refresh the portable XML setting was applied to the wrong node. That instance was stopped at
the language screen. Its newly-created real settings file was preserved in scratch and its newly-created empty data
directories removed. Subsequent launches verify the actual `IsPortable` setting before starting.

WinMerge was launched before a baseline of third-party tool preferences was captured. Its original preference state
cannot be fully reconstructed; do not claim full restoration of those preferences. Vendor registry snapshots were
captured before the remaining tools. Scratch merge backups are retained with their test repositories.


Final first-batch recheck: starting the rebuilt portable Settings window and saving it both leave HKCU/Software/GitExtensions absent. The real .gitconfig hash matches its baseline. Windows results contain 25,686 passing test cases across 18 suite invocations; Linux's 17 suite invocations all exit 0.

