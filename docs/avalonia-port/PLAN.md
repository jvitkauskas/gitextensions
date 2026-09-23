# Git Extensions → Avalonia: incremental porting plan (private fork, Windows first)

## Status

| Phase | State |
|---|---|
| 0: Foundations | **Done** except `ThreadHelper` overloads for Avalonia controls, which will be added when a ported view needs them. Done so far: new projects, packages, installer/publish, translation reuse (with access-key conversion), dialog base window, window position restore/save (shared `WindowPositions.xml`), hotkeys, `IMessageBoxService`, theme bridge (Fluent palette from the Git Extensions theme, `AppColor` brushes), embedding of native WinForms controls. |
| 1: Hybrid spike | **Done, go.** Approach (A) works: Avalonia windows are modal over WinForms owners. Ported: `FormAbout`, `FormRenameBranch`, `FormCommitTemplateSettings`. |
| 2: Lightweight dialogs | **In progress: 48 of ~50 done.** The progress dialog (`FormStatus`/`FormProcess`) covers the ~70 `FormProcess.ShowDialog`/`ReadDialog` and `FormStatus.ShowErrorDialog` call sites. Batch 1 adds command line help, add files, donate, contributors, reset changes, delete tag, init, go to line, and the script input and file prompts. Batch 2 adds the SSH key prompt, build server credentials, branch multi-selection, language selection, encodings, add submodule, clean working directory, submodule conflict, create worktree and open repository. Batch 3a adds delete branch, delete remote branch and merge branch, with ports of the `BranchComboBox` and `HelpImageDisplayUserControl` controls. Batch 3b adds cherry-pick, revert, reset current branch, reset another branch and archive, with a port of `CommitSummaryUserControl`. Batch 3c adds create branch and create tag, with a port of `CommitPickerSmallControl`. Batch 4 adds checkout revision, compare to branch, bisect, go to commit, dashboard category name and add to .gitignore. Batch 5 adds checkout branch. Batch 6 adds clone. Batch 7 adds the revision filter. Batch 8 adds the HOME directory, check for updates, worktree and submodule lists, and the modeless help text window of the scripts settings. Batch 9 adds the reflog and the recent repositories settings. Batch 10 adds the remotes dialog. `FormRemoteProcess` stays WinForms until Push/Pull/Clone are ported (phase 5). See the [ledger](ledger.md). |
| 3: Text/diff viewer | **In progress.** Step 1 done: `TextEditorView` on AvaloniaEdit (MIT, added to `Product.wxs`) in editing mode, with `FormEditor` (git's editor), `FormGitAttributes` and `FormMailMap` on it. `DialogViewModel.CanClose` lets view models keep a dialog open (e.g. unsaved changes). Step 2 done: a read-only diff mode (`TextEditorViewModel.LoadDiff`) with the two-column line-number margin and the added/removed/header backgrounds of a patch without git's colors (`DiffLinesAnalyzer`, a port of `DiffLineNumAnalyzer`), with `FormViewPatch` on it. `FormSparseWorkingCopy` is on the editing mode. Step 3 done: the identical parts of matching removed and added lines are dimmed (`InlineDiffAnalyzer`, a port of `LinesMatcher` and `AddInlineDifferenceMarkers`), with a parity test against the WinForms viewer. Next: git's colors (ANSI), then the diff dialogs. |
| 4: Revision grid | **In progress.** Step 1 done: the graph layout (`RevisionGraph`, rows, segments, lanes) is compiled by the UI-neutral `GitUI.RevisionGraph`, in place (see finding 30). Step 2 done: the Avalonia revision grid (`RevisionGridView`, graph drawn by `RevisionGraphRenderer`), and `FormChooseCommit` on it. Step 3 done: artificial commits, quick search and multi-selection; `FormFormatPatch`. Next: the grid context menu and column settings; `FormLog` and `FormFileHistory` wait for the diff viewer (phase 3) and the file list (phase 5). |
| 5+ | Not started. |

Using the port:
- Ported dialogs are on by default.
- `GE_AVALONIA=none` falls back to WinForms.
- `GE_AVALONIA=FormAbout,FormRenameBranch` enables only the listed dialogs.

Code layout:
- `src/app/GitUI.Presentation`: view models and strings.
- `src/app/GitUI.Avalonia`: views and hosting.
- `src/app/GitUI.RevisionGraph`: the revision graph layout, compiled from its GitUI sources.
- `src/app/GitUI/AvaloniaHosting/`: routing and composition (`AvaloniaDialogs*.cs`) and host services (`AvaloniaHostServices.cs`).

### Phase 1 findings (rules for all further porting)

1. **Hosting works.** `AvaloniaDialogHost.ShowDialog` shows an Avalonia window modally over a native owner:
   - it sets the owner window;
   - it disables the thread's other windows;
   - it runs a nested `DispatcherFrame`.

   Nesting works in both directions: an Avalonia dialog can open WinForms dialogs (`FormProcess`, `FormContributors`), and vice versa.
2. **Run WinForms code with the WinForms `SynchronizationContext`.** Inside Avalonia's loop, the current context is Avalonia's. WinForms code called from Avalonia (a view model callback that opens `FormProcess`, for example) would queue its continuations to Avalonia's dispatcher, which does not run them while that callback is still on the stack. So always go through `AvaloniaUi.RunInHostContext(...)` when calling into WinForms code.
3. **Avalonia binds to a single UI thread per process.** That's fine for the app. Tests that show Avalonia windows must all run on one thread: use `[SingleThreaded]`, as `AvaloniaHostingTests` does.
4. **Stay on NUnit 4.3.x for now.** NUnit 4.5 wraps the STA `SynchronizationContext` (`SafeSynchronizationContext`). That breaks `ConfigureJoinableTaskFactory`, so JoinableTaskFactory "main thread" switches land on a pool thread. For this reason the headless tests use `Avalonia.Headless`, not `Avalonia.Headless.NUnit` (which requires NUnit ≥ 4.5).
5. **Avalonia packages leak analyzers.** Package-to-package dependencies bring Avalonia's analyzers and XAML generator into projects that only reach Avalonia transitively (plugins, tests). The repo-root `Directory.Build.targets` removes them from projects that don't reference Avalonia directly.
6. **Avalonia 12 needs HarfBuzz enabled explicitly** (the `Avalonia.HarfBuzz` package and `.UseHarfBuzz()`), otherwise text rendering fails at runtime.
7. **Translations carry over.**
   - Each view's `*Strings` class declares the old form's XLIFF ids, and `ViewStringsTests` pins them to `English.xlf`.
   - A portable build running in German shows the Avalonia About dialog translated from the existing `German.xlf`.
   - `English.xlf` is unchanged, so CI's "Verify localisation" passes.
8. **DPI.** The process stays system-DPI-aware. Avalonia renders crisply at 200% on a single monitor. Mixed-DPI multi-monitor setups are untested.
9. **Packaging.**
   - Avalonia adds about 23 managed DLLs, plus Skia, HarfBuzz and ANGLE native DLLs for win-x64 and win-arm64 (around 18 MB per architecture). All are declared in `Product.wxs` under `Component.AvaloniaUI`.
   - `Project.Avalonia.targets` strips the other operating systems' natives and the native `.pdb` files (over 100 MB) before packing.
   - Plugins that reference `GitUI` also copy Avalonia natives into their build output. Publish does not ship those.
10. **Known behaviour differences.** Avalonia's `CheckBox` handles Enter itself (it toggles). WinForms instead triggers the dialog's OK button when Enter is pressed on a check box.

### Phase 2 findings

11. **WinForms controls that aren't ported yet can be embedded.** `EmbeddedNativeViewHost` (a `NativeControlHost`) parents a WinForms control's window into an Avalonia window. The progress dialog embeds the existing console control this way (ConEmu, Mintty or plain text, whichever is configured), so terminal emulation keeps working unchanged. On close, the control is parked under `HWND_MESSAGE` before being disposed, so Windows doesn't destroy it underneath WinForms.
12. **Pass a "post to UI thread" delegate to view models** that react to background events. The repo's threading analyzers forbid raw `SynchronizationContext.Post`. In the app the delegate is JoinableTaskFactory-based (`InvokeAndForget`), and in tests it runs the action synchronously.
13. **Translate WinForms access keys.** XLIFF texts carry WinForms mnemonics (`&Abort`). Bind `TranslatedText.AccessKeyText` (`_Abort`) for controls, and `PlainText` for titles.
14. **Window sizes are outer bounds.** WinForms persists outer bounds, while Avalonia's `Width`/`Height` are the client size. `DialogWindow` measures the frame thickness when it opens and converts in both directions.
15. **Inside `GitUI.*` namespaces, `Avalonia.X` resolves to `GitUI.Avalonia.X`.** Use top-level `using Avalonia...;` directives and plain type names.
16. **ConEmu fails on ARM64 Windows** with "Can't load library, ErrCode=193" (its helper DLL is x64-only). The WinForms dialog fails the same way, so this is an existing limitation, not caused by the port. A failed ConEmu run also skipped the refresh after create branch/tag. Fixed by reporting ConEmu as unsupported in non-x86/x64 processes, so commands fall back to Mintty or plain text, and the console tab uses Mintty or is hidden; an x64 build of the app (private x64 runtime + x64 apphost) uses ConEmu normally.
17. **Positions are shared with the WinForms forms, but sizes are only restored for dialogs that don't size to their content.** A WinForms-saved height would crop an Avalonia layout that sizes itself to its content.
18. **Event scripts need a host form.** `IScriptsRunner.RunEventScripts` requires `IGitModuleForm` + `IScriptOptionsForm` + `IWin32Window`; `AvaloniaDialogs.ScriptHost` provides that for an Avalonia dialog.
19. **File and folder pickers** go through `IFileDialogService` (Avalonia `StorageProvider`), not `OpenFileDialog` / `OsShellUtil.PickFolder`.
20. **Compute application-level values lazily in view models**, like the user-manual URL, which needs `AppSettings.DocumentationBaseUrl` (set at startup but not in tests). The WinForms controls did the same.
21. **WinForms forms whose strings never reached `English.xlf`** (`FormContributors`, `SimplePrompt`) show English. Their ports use constants, so the XLIFF test still holds.
22. **Reuse small WinForms helpers by widening them to `internal`** (e.g. `FormCreateWorktree.CreateWorktreeCommand`, `FormOpenDirectory.OpenGitRepository`) instead of copying their logic. It is a one-word upstream diff and keeps a single implementation. Copy only logic that is tangled with controls.
23. **Dialogs shown by `GitExtensions.exe` itself** (e.g. the language selection on first start) go through the public `AvaloniaStartupDialogs`, because `AvaloniaDialogs` is internal to GitUI.
24. **Avalonia details that differ from WinForms:** a button disabled by its command's `CanExecute` still has `IsEnabled = true` (check `IsEffectivelyEnabled`), and a `ListBox` doesn't take focus itself (focus an item container).
25. **Dialogs that can act without being shown** (e.g. checkout branch with the default action) run the same view model without a window, owned by the caller; the view model gets an `isVisible` flag where the WinForms code checked `Visible`.
26. **Dialogs sized to their content (`SizeToContent="Height"`) resize only horizontally**, as the WinForms forms that pin their height with `MinimumSize` / `MaximumSize`. Avalonia drops `SizeToContent` once the user drags a top or bottom border, so `DialogWindow` remaps those borders to non-resizing ones in `WM_NCHITTEST` (Windows only). Other resizes (Win+Shift+Up, keyboard sizing) are vetoed in `WM_WINDOWPOSCHANGING` unless they size the window to its content (`DesiredSize`), which is what Avalonia's own layout resizes do. Such dialogs restore their saved width but not their height, and are still centred over the owner.
30. **Shared code is compiled in place, not moved.** `GitUI.RevisionGraph` compiles `src/app/GitUI/UserControls/RevisionGrid/Graph/*.cs` and GitUI excludes those files (`Compile Remove` in `GitUI.csproj`), so upstream changes to them still merge into the same paths. New upstream files in that folder join the library automatically; one that needs GitUI must be excluded there and re-included in GitUI. The rendering, the theme colors (`RevisionGraphLaneColor`) and the translated lane tooltips (`LaneInfoProvider`, `BranchFinder`) stay in GitUI. The one upstream edit: `LaneInfo` takes its color index from `RevisionGraphLanePalette`, whose color count GitUI provides from its theme (a module initializer). The library declares its own `Validates` in the graph namespace, which avoids a clash with the `Microsoft.Validates` linked into GitUI.
27. **List dialogs use `Avalonia.Controls.DataGrid`** (MIT, versioned with Avalonia; added to `Product.wxs`). Column headers are not in the logical tree, so the views set them from the strings in code-behind rather than binding them. App-wide styles give the grid the compact row height and the dialog font. Per-row styling (e.g. struck-out deleted worktrees) sets a class in `LoadingRow`.
28. **Modeless windows** (`AvaloniaDialogHost.Show`) set the native owner but do not disable windows or push a frame; the WinForms message loop dispatches their messages. The bridge closes them with their owner form, since Windows would otherwise destroy the owned window behind Avalonia's back.
29. **Avoid `InlineUIContainer`** for links inside text (the equivalent of a `LinkLabel` `LinkArea`): Avalonia 12.1 measures the embedded control during rendering and throws "Visual was invalidated during the render pass". Lay the text and the link button out in a horizontal `StackPanel` instead.

## Context

Git Extensions is a WinForms app: about 99k lines of UI code in `src/app/GitUI`, about 88 forms, and heavy custom controls. The goal is an Avalonia UI that runs on Windows first. OS-specific pieces stay as they are until the end: Windows diff/merge tools and editors, ConEmu, the shell extension, jump lists, and Credential Manager. Porting everything in one go is not feasible. This plan replaces the UI in slices, and the fork stays **shippable and in sync with upstream** after every step.

Three findings shape the plan:

- **There is a natural seam.** `GitUICommands` (`src/app/GitUI/GitUICommands.cs`) has 75 `Start*` methods, and almost every one builds exactly one form. Command-line verbs (`RunCommandBasedOnArgument`, around line 1565) go through the same methods. An Avalonia window can therefore replace a WinForms form one command at a time.
- **Threading is mostly independent of the toolkit.** It is built on JoinableTaskFactory (`src/app/GitExtUtils/GitUI/ThreadHelper.cs`, `TaskManager.cs`). About 225 call sites use plain JTF and work under any `SynchronizationContext`. The roughly 170 `Control.SwitchToMainThreadAsync` / `InvokeAndForget` sites only change when their form is ported.
- **Staying Windows-only for now removes a big blocker.** Everything can keep targeting `net10.0-windows`, so new code can reference `GitCommands` and `AppSettings` directly. Retargeting the core libraries to plain `net10.0` moves to the final, cross-platform phase.

## Strategy: a hybrid process that replaces the UI piece by piece

1. **Run Avalonia inside the existing WinForms process** on the same UI thread (`AppBuilder...SetupWithoutStarting()` in `Program.cs`). WinForms stays the host until the main window is ported.
2. **Port one dialog at a time.** Each ported dialog is a view model with no UI dependency, plus an Avalonia view. Its `Start*` method calls it through a switch, and the WinForms version stays as a fallback.
3. **Port the heavy shared controls as separate workstreams.** These are the text/diff viewer, the revision grid, the file-status list and the commit editor. They are what unlock the big dialogs.
4. **Switch the host last.** Port `FormBrowse`, then let Avalonia own the app lifetime. Any WinForms pieces still left get hosted inside Avalonia until they are replaced.
5. **Only after the UI is fully ported:** remove WinForms, then start the OS-abstraction work for macOS and Linux.

### Keeping upstream merges manageable
- **New code goes into new projects** (below). Existing WinForms files get only one-line hooks, such as the routing check in each `Start*` method.
- **Don't delete ported WinForms forms until Phase 8.** Keeping them makes upstream merges apply cleanly and gives a fallback.
- **Keep a porting ledger** (`docs/avalonia-port/ledger.md`). For each ported form, record the upstream commit its behavior was ported from. After each upstream merge, run `git log <hash>..upstream/master -- <form files>` to find the changes that need re-applying to the Avalonia version.

## New projects and dependency direction

```
GitUI (WinForms, existing) ──► GitUI.Avalonia ──► GitUI.Presentation ──► GitCommands / GitExtensions.Extensibility / GitExtUtils
```

- **`src/app/GitUI.Presentation`:** view models, dialog and message-box service interfaces, and settings adapters. It references no UI toolkit (`UseWindowsForms=false`), keeps TFM `net10.0-windows` for now, and uses CommunityToolkit.Mvvm.
- **`src/app/GitUI.Avalonia`:** XAML views, Avalonia controls, the dialog shell, the theme bridge and interop helpers (`UseWindowsForms=false`, so implicit usings don't clash on `Button`, `Color` and similar).
- **Tests:**
  - `tests/app/UnitTests/GitUI.Presentation.Tests`: view-model tests using the existing NUnit, AwesomeAssertions and NSubstitute setup from `eng/Tests.targets`.
  - `tests/app/IntegrationTests/GitUI.Avalonia.IntegrationTests`: Avalonia headless tests that run the real XAML, with optional screenshot snapshots through Verify.
- **Reuse** the prototype in `prototypes/Avalonia` as a starting point, including its view model, headless test setup and compact Fluent styling.

## Phases

Each phase ends with an app that works and can be shipped. Phases 3, 4 and 5 can run in parallel with 2.

### Phase 0: Foundations (no user-visible change)
- **Build:**
  - Add the Avalonia packages to `Directory.Packages.props` and the new projects to `GitExtensions.slnx`.
  - Add the Avalonia and Skia DLLs, including the native files under `runtimes\win-*\native`, to `setup/installer/Product.wxs`, otherwise `Check-BundlesConsistent.ps1` fails the publish.
  - Make CI run the headless tests.
- **Translation:** find a way for Avalonia views to reuse the existing XLIFF IDs (category = old form name, id = `field.Text`), so the 30+ existing translations carry over.
  - Proposal: one C# strings class per view (a `Translate` subclass with `TranslationString` fields), exposed to XAML through a markup extension.
  - Extend `setup/TranslationApp` and `tests/app/UnitTests/GitUI.Tests/TranslationTest.cs` to cover these classes.
  - The existing mechanism is `src/app/GitExtensions.Extensibility/Translations/Xliff/TranslationUtil.cs`; it reflects over WinForms fields.
- **Theme bridge:** map the Git Extensions `Theme` / `AppColor` model (`src/app/GitExtUtils/GitUI/Theming/`, CSS files in `src/app/GitUI/Themes/`) to an Avalonia resource dictionary. Base it on Fluent compact density, with light/dark following `ThemeModule`.
- **Dialog shell:** an Avalonia counterpart of `GitExtensionsForm` / `GitExtensionsDialog`.
  - Window position save/restore: abstract `src/app/GitUI/WindowPositionManager.cs`, which currently works on `Form`.
  - Hotkeys (`IHotkeySettingsLoader`), the OK/Cancel footer and the help button.
  - Button order that follows platform conventions.
- **Services for view models:** `IDialogService`, `IMessageBoxService` and `IFolderPicker`. These replace direct `MessageBoxes`, `TaskDialog` and `FolderBrowserDialog` use from logic code.
- **Threading helpers:** add Avalonia-flavoured `SwitchToMainThreadAsync` / `InvokeAndForget` overloads next to the WinForms ones in `ThreadHelper` / `ControlThreadingExtensions`.
- **Image interop:** convert `System.Drawing.Image` / `Icon` to an Avalonia `Bitmap` (for plugin icons, commit-template icons and file-type icons).

### Phase 1: Hybrid spike and the first three dialogs (the go/no-go gate)
- **Start Avalonia in `src/app/GitExtensions/Program.cs`** after the WinForms sync context and `JoinableTaskContext` are set up (around line 106).
- **Prototype two ways to show dialogs** and pick one:
  - **(A, preferred)** A real Avalonia `Window` shown modally over a WinForms owner. Set the native owner through the HWND, disable the owner, and run a nested `Dispatcher` frame. Views are then already windows, so nothing needs redoing at the host switch.
  - **(B, fallback)** An Avalonia view hosted in a thin WinForms shell through `Avalonia.Win32.Interoperability` (`WinFormsAvaloniaControlHost`).
- **What the spike must prove:**
  - modality and focus return;
  - Tab, Enter and Esc;
  - IME and clipboard;
  - DPI (see risks);
  - no leaks after opening a dialog 100 times.
- **Routing:** a one-line check at the top of each ported `Start*` method, e.g. `if (AvaloniaUi.TryShow...(owner, ...)) return ...;`. It is controlled by a global setting plus a per-dialog override through an environment variable, for example `GE_AVALONIA=all|none|FormAbout,...`.
- **Port:** `FormAbout` (`about` verb), `FormRenameBranch` and `FormCommitTemplateSettings` (from the prototype).
- **Exit criteria:**
  - the dialogs work from the menus and from the CLI;
  - the WinForms fallback still works;
  - an MSI and portable build install and run;
  - you record a go/no-go decision on the hybrid approach.

### Phase 2: Lightweight dialogs (~50 forms)
Work through the dialogs that don't embed heavy controls, smallest first.
- **Tiny:** `FormCommandlineHelp`, `FormGoToLine`, `SimplePrompt`, `FormFilePrompt`, `FormPuttyError`, `FormBuildServerCredentials`, `FormSelectMultipleBranches`, `FormChooseTranslation`, `FormAvailableEncodings`, `FormDonate`, `FormContributors`, `FormAddFiles`, `FormResetChanges`.
- **Then:** `FormCreateBranch`, `FormDeleteBranch`, `FormDeleteTag`, `FormInit`, `FormClone`, `FormMergeBranch`, `FormCherryPick`, `FormRevertCommit`, `FormAddSubmodule`, `FormCreateWorktree`, `FormArchive`, `FormCleanupRepository`, `FormRevisionFilter`, and similar.
- **The progress dialogs `FormStatus` / `FormProcess` / `FormRemoteProcess`** are used everywhere. Port them early, starting with plain-text output: `PlainTextConsoleCommandRunner` already exists behind `IConsoleEmulator` in `src/app/GitUI/ConsoleEmulation/`.
- **Porting recipe for each dialog:**
  1. Move the logic out of code-behind into a view model in `GitUI.Presentation`.
  2. Write the XAML view.
  3. Map the strings to the existing translation IDs.
  4. Add the routing hook.
  5. Write view-model tests and a headless view test.
  6. Add a ledger entry.
  7. Smoke-test through the CLI verb or menu with the switch on.
- **Existing logic to reuse:** `FormRemotesController`, `FormSparseWorkingCopyViewModel`, `GitIgnoreModel`, and `CheckSettingsLogic` / `CommonLogic`.

### Phase 3: Text/diff viewer on AvaloniaEdit (workstream, large)
- **Separate the logic from ICSharpCode first.** Introduce a small "highlight/marker" DTO and have `DiffHighlightService`, `DifftasticHighlightService`, `GrepHighlightService`, `DiffLineNumAnalyzer` and `AnsiEscapeUtilities` (all in `src/app/GitUI/Editor/`) produce DTOs instead of writing to `IDocument.MarkerStrategy`. The existing ICSharpCode viewer then consumes the DTOs through an adapter, so the WinForms side keeps working.
- **Reuse these as-is (no changes):** `GitCommands/Patches/PatchManager.cs`, `PatchProcessor.cs`, `Editor/Diff/LinesMatcher.cs`, `GitBlameParser`.
- **Build an Avalonia `FileViewer` on AvaloniaEdit:**
  - custom highlighting strategies turned into colorizing transformers;
  - the two-column diff line-number margin and the blame author margin;
  - markers for intra-line diff and search hits;
  - find/replace;
  - synced scrolling;
  - image mode;
  - an editable mode for the `fileeditor` verb, which Git Extensions uses as git's `core.editor`.
- **Unlocks:** `FormEditor`, `FormEdit`, `FormViewPatch`, `FormMailMap`, `FormGitAttributes`, `FormDiff`, `FormBlame` / `BlameControl`, and `FindAndReplaceForm`.

### Phase 4: Revision grid (workstream, large)
- **Move the graph layout into a UI-neutral library.** `Graph/RevisionGraph*.cs`, `Lane*.cs`, `BranchFinder.cs` and `LaneNodeLocator.cs` in `src/app/GitUI/UserControls/RevisionGrid/Graph/` use no GDI.
- **Pull the geometry decisions out of `Graph/Rendering/GraphRenderer.cs`** (lane sharing, diagonals, merge-lane collapsing) so they aren't tangled with the drawing code.
- **Build an Avalonia control:**
  - a virtualized row list fed by the existing `RevisionReader` (`src/app/GitCommands/RevisionReader.cs`) batches;
  - the graph drawn with `DrawingContext`, plus a row cache;
  - columns (avatar, author, date, id, message with ref "capsules", build status), tooltips, hover highlight, quick search and context menus.
- **Keep the existing Verify revision-graph tests (39)**. They test the layout, which doesn't change.
- **Unlocks:** `FormLog` (`viewdiff`), `FormChooseCommit`, `FormFormatPatch`, `FormBisect`, and the commit-picker controls (`FormCheckoutRevision`, `FormCreateTag`).

### Phase 5: FileStatusList, commit editor, CommitInfo, then the big dialogs
- **`FileStatusList`:** today a WinForms `TreeView` with owner drawing and hand-rolled multi-select. Port it as an Avalonia `TreeView` or TreeDataGrid with multiple selection. Keep Windows file icons behind an icon-provider interface. Port its 1.3k-line context menu through `FileStatusListContextMenuController`.
- **Commit message editor:** `EditNetSpell` is RichTextBox plus Win32 messages. Rebuild it on AvaloniaEdit with squiggle underlines. `externals/NetSpell.SpellChecker` is nearly pure logic and only needs its WinForms attributes dropped.
- **`CommitInfo`:** today the XHTML is rendered into a RichTextBox through P/Invoke. Replace it with Avalonia `SelectableTextBlock` inlines and links.
- **Then the big dialogs, composed from these pieces:** `FormCommit` (2960 lines; the prototype's settings dialog already hangs off it), `FormStash`, `FormPush`, `FormPull`, `FormResolveConflicts`, `FormFileHistory`, `FormRebase`, and `FormCheckoutBranch`. Existing logic to reuse: `RevisionDiffController`, `FormFileHistoryController`.

### Phase 6: Settings dialog and plugin settings
- **Port the `FormSettings` host** (tree plus page panel; `src/app/GitUI/CommandsDialogs/FormSettings.cs`) and its 26 pages in `CommandsDialogs/SettingsDialog/Pages/`, most of which are 25–250 lines.
- **Add an Avalonia implementation of the plugin setting bindings** (`ISettingControlBinding` for `ChoiceSetting`, `NumberSetting`, `StringSetting` and so on) so that `PluginSettingsPage` / `AutoLayoutSettingsPage` render plugin settings with no plugin changes.
- **Reuse `ColorsSettingsPageController`**; it is already MVP.

### Phase 7: Plugin API v2 and switching the host
- **Plugin API v2** (in `src/app/GitExtensions.Extensibility`):
  - replace `IWin32Window`, `Control`, `Image`, `ContextMenuStrip` and `Point` with neutral types: an opaque owner handle, an image source abstraction, and a menu model;
  - keep the v1 interfaces through a shim so existing third-party plugins still load;
  - port the plugins' own forms (7 forms, for example `DeleteUnusedBranchesForm` and `FormGitStatistics`).
- **Port `FormBrowse` (3740 lines):**
  - left panel `RepoObjectsTree`, the dashboard (`UserRepositoriesList`), and the revision grid plus diff/file tree;
  - the console tab keeps ConEmu, hosted through Avalonia `NativeControlHost` (it's an HWND);
  - jump lists and taskbar progress keep WindowsAPICodePack, using the Avalonia window HWND.
- **Switch the host:**
  - `Program.cs` starts `StartWithClassicDesktopLifetime`;
  - `JoinableTaskContext` is created on Avalonia's sync context;
  - `TaskManager` stops using `Application.OnThreadException`.

### Phase 8: Remove WinForms
- Remove the WinForms forms, `ICSharpCode.TextEditor`, the WinForms parts of `ResourceManager` / `GitExtUtils/GitUI`, and the global `UseWindowsForms=true` in `Directory.Build.props`.
- Move `UI.IntegrationTests` over to headless Avalonia tests.
- Replace the IE `WebBrowserControl` (build reports, pull requests) with WebView2 through `NativeControlHost`, or with an external browser.

### Later, out of scope for now: cross-platform
- Retarget the core libraries to `net10.0`, removing `System.Drawing` `Font` / `Color` / `Image` from `AppSettings`, `GitModule` and the plugin API.
- Put these behind OS services:
  - diff/merge tool and editor discovery (`src/app/GitCommands/DiffMergeTools/`, `EditorHelper.cs`);
  - console (ConEmu or Mintty, replaced by a cross-platform terminal);
  - Credential Manager;
  - SSH askpass (`src/native/GitExtSshAskPass`);
  - the registry;
  - shell integration;
  - VS integration.
- Add macOS and Linux packaging next to the WiX installer.

## Deliberately Windows-only until the cross-platform phase
These work unchanged in the Avalonia app on Windows:
- Diff/merge tool discovery through ProgramFiles and the registry. Tools are launched through `git difftool` / `git mergetool` anyway.
- Editor detection.
- ConEmu and Mintty (through `NativeControlHost`).
- The Explorer shell extension.
- SSH askpass.
- Jump lists and taskbar (WindowsAPICodePack).
- Credential Manager (AdysTech).
- Visual Studio integration (EnvDTE).
- `Icon.ExtractAssociatedIcon` file icons.
- The Ctrl+C P/Invoke in `ProcessExtensions.cs`.

## Main risks and how the plan handles them
- **DPI mode.** The process is system-DPI-aware (`Program.cs` around lines 44–49). Avalonia prefers per-monitor. Mixing them in one process means system-aware Avalonia windows until the host switch. Test this in the Phase 1 spike; switching WinForms to PerMonitorV2 early is a separate, risky change.
- **Hybrid message loop, focus and modality.** Settled by the Phase 1 spike. Fallback (B) keeps the plan viable.
- **AvaloniaEdit feature gaps** (margins, markers, performance on very large diffs). Mitigated by separating the logic from ICSharpCode first, so the two editors can be compared side by side.
- **Upstream drift during a long port.** Mitigated by the ledger, one-line hooks, and not deleting forms until Phase 8.
- **Installer size and bundle checks.** Avalonia plus Skia adds native DLLs. Handled in Phase 0 through `Product.wxs`.

## Critical files (existing)
- `src/app/GitExtensions/Program.cs`: Avalonia setup, and the host switch later.
- `src/app/GitUI/GitUICommands.cs`: the one-line routing hook for each ported `Start*` method.
- `src/app/GitExtUtils/GitUI/ThreadHelper.cs`, `TaskManager.cs`, `ControlThreadingExtensions.cs`: Avalonia overloads.
- `src/app/GitExtensions.Extensibility/Translations/Xliff/TranslationUtil.cs`, `setup/TranslationApp`: translation for Avalonia views.
- `src/app/GitUI/Theming/ThemeModule.cs`, `src/app/GitExtUtils/GitUI/Theming/`: the theme bridge.
- `src/app/GitUI/WindowPositionManager.cs`: abstract it for the Avalonia shell.
- `Directory.Packages.props`, `GitExtensions.slnx`, `setup/installer/Product.wxs`, `.github/workflows/_app-build-core.yml`: build, packaging and CI.
- `src/app/GitExtensions.Extensibility/**`: plugin API v2 (Phase 7).

## Verification (every phase)
- **CI:** `dotnet build`, `dotnet test` including the new view-model and headless test projects, and `dotnet publish`, where `Check-BundlesConsistent` must pass and the MSI must build.
- **Both UI routes stay green.** The existing `UI.IntegrationTests` run with the switch off, and the new headless tests cover the Avalonia views. When a heavy control is ported, the existing Verify snapshots must still match: revision graph, `FileStatusList` sorter/filter, commit-info HTML.
- **Manual smoke tests.** CLI verbs make ported dialogs easy to open one at a time, e.g. `GitExtensions.exe about|tag|branch|commit|settings`, with `GE_AVALONIA=all` and with `GE_AVALONIA=none`. Run a short checklist on each dialog: keyboard (Tab/Enter/Esc), dark theme, 150% and 200% DPI, a non-English translation, and position restore.
- **After each upstream merge:** go through the ledger, re-port any behavior changes, and re-run the tests.
