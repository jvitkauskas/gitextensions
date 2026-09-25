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

