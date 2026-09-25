# Git Extensions on macOS and Linux: implementation plan

This is the "cross-platform" phase that `PLAN.md` left for after the Avalonia port. The port is complete on Windows:
WinForms is gone (phase 8) and the parity gaps of the ledger are closed. What still ties the application to Windows is
listed below with counts from a survey of the code (2026-09-25). The plan keeps the rules of the port: **Windows stays
shippable after every step**, changes go behind small seams so that upstream merges stay manageable, and every step is
verified by tests before the next one.

## Where the application stands

WinForms is gone, but hand-written Win32 code replaced it and kept the WinForms names. The Windows dependencies, by depth:

| Area | Today | Size |
|---|---|---|
| Target framework | `SolutionTargetFramework = net10.0-windows` (`eng/RepoLayout.props`) for about 35 projects; ConEmuInside, NetSpell and Git.hub hard-code it | one switch, but CA1416 then reports every Windows API |
| Avalonia platform | `Avalonia.Win32` only, `.UseWin32()` (`AvaloniaUi.cs`); `Project.Avalonia.targets` deletes the native assets of other systems | small |
| Window owners | `IWin32Window` (a native handle, 394 uses; 83 in `IGitUICommands`), `WindowOwner(nint)` of the plugin API v2, `AvaloniaDialogHost` modality through `EnableWindow` / `GWLP_HWNDPARENT`, `DialogWindow` window procedure hooks | core, touches the plugin API |
| Message boxes, dialogs | `NativeMessageBox` (`MessageBoxW`), `TaskDialog` (`TaskDialogIndirect`), `CommonDialogs` (COM `IFileDialog`, `ChooseColor`, `ChooseFont`): `MessageBoxes` 147 uses, `DialogResult` 117, `TaskDialog` 29, common dialogs 13 | core, synchronous calls |
| System.Drawing (GDI+) | `System.Drawing.Common` throws off Windows. `AppSettings` fonts (`Consolas`, `SystemFonts`) read at startup; `IGitPlugin.Icon`, `PluginMenuItem`, `AddCommitTemplate(Image)`; `Images.Designer.cs`; the avatar pipeline (14 files, drawn with `Graphics`); `ResourceManager` header renderers; `TextMeasurement`; 13 plugin resource files | core, touches the plugin API |
| Registry | `AppSettings` keeps the git, SSH and PuTTY paths, `InstallDir`, `CheckSettings`… in `HKCU\Software\GitExtensions`; `ImportFromRegistry` runs from the static constructor; git and PuTTY discovery read uninstall keys; `SystemTheme` reads the theme | the first start off Windows fails |
| Win32 helpers | `ClipboardUtil`, `Screens`, `DpiUtil` (unguarded static constructor), `AvatarDownloader` (`InternetGetConnectedState`), `ProcessExtensions` (Ctrl+C, guarded) | leaf |
| Paths and processes | about 100 `".exe"` and 135 backslash literals; `cmd.exe` launches (gitk, git gui, Plink, the terminal fallback), `explorer.exe` / `rundll32`; `;` as the PATH separator; `ApplicationInfo.UserAppDataPath` built with `\`; shells (`BashShell` looks for `git-bash.exe`, `CmdShell`, PowerShell); editor list (`notepad++.exe`, `code.exe`…); default scripts (`bash.exe`, powershell) | medium, spread out |
| Windows features | taskbar and jump list (`NativeTaskbar`), WebView2 build report, EnvDTE, Explorer shell extension (C++ COM), ConEmu, Mintty, Windows Credential Manager (AdysTech), SSH askpass (C++ Win32 `GitExtSshAskPass`) | leaf, each already optional or guarded |
| Build and packaging | WiX MSI (in the solution), `powershell.exe` publish steps, `Check-BundlesConsistent.ps1` against `Product.wxs`, `findstr`, TranslationApp, C++ native build, Windows-only CI runners | medium |
| Tests | `TestAppSettingsAttribute` uses a named `Semaphore` (unsupported on Unix) for 8 assemblies; Win32 message pump and STA contexts; `UI.IntegrationTests` (user32, `TestOwnerWindow`); 164 `C:\` paths in the Avalonia tests; 10 `[Platform("Win")]` tests | medium |

Already portable: the Avalonia views and the `GitUI.Presentation` view models (icons as bytes or names,
`IMessageBoxService`, `IFileDialogService` with `AvaloniaFileDialogService`), the built-in terminal (XTerm.NET and
Porta.Pty), and the non-Windows branches that GitCommands kept from the Mono days (diff tools, `GitModule`,
`EnvironmentConfiguration`, `PathUtil`, the settings checklist). Those branches have not run for years and need testing.

## Decisions

1. **One target framework, `net10.0`, with runtime checks.** The Windows code stays in the same assemblies behind
   `OperatingSystem.IsWindows()` and `[SupportedOSPlatform("windows")]`; the platform analyzer (CA1416) makes every
   unguarded call a build error. P/Invoke, CsWin32 and the WebView2 core library all build for `net10.0`. One build
   serves every system, and `dotnet publish -r <rid>` makes the packages. Multi-targeting (`net10.0;net10.0-windows`)
   was considered and rejected: it doubles the builds and the test matrix, and every Windows file would need `#if`s.
2. **Platform services, chosen once at startup.** A small `IPlatformServices` family in a portable project
   (`GitExtensions.Platform`), with `Windows`, `Linux` and `MacOS` implementations registered at startup. Each seam is
   first added with the Windows implementation only (no behaviour change on Windows), then implemented for the others.
3. **Keep the synchronous message boxes.** Avalonia 12 has nested dispatcher frames (`Dispatcher.PushFrame`), so an
   Avalonia message box and task dialog can be shown modally and still return a result synchronously. The 147
   `MessageBoxes` call sites and the 29 task dialogs keep their shape; only the implementation changes. On Windows the
   native dialogs can stay behind the same facade.
4. **Plugin API v3 for images and owners, v2 stays on Windows.** `Image` (GDI+) and `nint` owners cannot be part of a
   portable API. v3 adds `PluginImage` (PNG bytes or a resource name) and an opaque `WindowOwner` backed by the Avalonia
   window. The v2 members stay and keep working on Windows through a shim; off Windows, a plugin that only has a GDI+
   icon shows without one. The in-repo plugins move to v3; third-party plugins (PluginManager) need a release for
   macOS and Linux.
5. **Windows-only features become optional capabilities** instead of being ported: jump lists, the Explorer shell
   extension, ConEmu, Mintty, PuTTY, EnvDTE. Off Windows their menu items and settings pages are hidden. Native
   equivalents (a macOS dock menu, Nautilus or Finder actions) are later, separate work.

## Phases

Each phase ends with a Windows build that passes every test, and from phase 2 on with a Linux build in CI. Sizes are
relative (S, M, L, XL).

### Phase 0: Guard rails (S)
**Done (2026-09-25).** As planned, except: the publish steps keep Windows PowerShell on Windows (with
`-ExecutionPolicy Bypass`; `pwsh` is not always installed there) and use `pwsh` elsewhere; the solution build already
skipped the WiX project, so only `src/native/build.proj` needed a guard; the ledger has a "Platforms" section rather than
a column (its rows are forms). Found on the way: `CommonAssemblyInfo.cs` declared every assembly Windows-only, which
silenced CA1416 and made NUnit skip every test assembly off Windows; it now does so only for `net10.0-windows`.
- Build `setup/installer/Setup.wixproj` and `src/native` only on Windows (conditions in `GitExtensions.slnx` or a
  separate installer solution), so that `dotnet build` of the solution can work elsewhere.
- Add a Linux CI job (ubuntu) that builds the portable libraries and runs their tests; it grows with each phase. Add a
  macOS job once phase 2 builds.
- Replace the `powershell.exe` steps of `Project.Publish.targets` with `pwsh`, and write paths with `/` in the eng
  scripts, so the same scripts run on every system.
- Record the phase in the ledger (a "Platform" column: which areas run on which system).

### Phase 1: Portable core libraries (L)
**Done (2026-09-25).** All seven projects target `net10.0` (`$(PortableTargetFramework)`), with CommonTestUtils and the
tests of the libraries, which run on Linux in CI (about 3700). Differences from the plan below: the dependency order is
Extensibility, GitExtUtils, GitUIPluginInterfaces, GitCommands, ResourceManager, RevisionGraph, Presentation; only
`gitcommand` and `gitssh` moved to the settings file, since the installer, the shell extension and the Visual Studio
extension read the other registry values (they stay in the registry on Windows, and are in the settings file elsewhere);
`ImportFromRegistry` stays in the static constructor, guarded by `OperatingSystem.IsWindows()`; the plugin API v3 images
are `IGitPlugin.IconImage`, `PluginMenuItem.IconImage` and `IGitUICommands.RegisterCommitTemplate` (an `AddCommitTemplate`
overload would make the calls with `null` ambiguous). `DpiUtil`, `Screens`, `ClipboardUtil` and `TextMeasurement` stay
until phase 2, guarded or marked Windows-only. Found on the way: the settings were never saved off Windows (the name
of the mutex of `SaveSettings` had the path in it). The ledger's "Platforms" section has the details.
Retarget bottom-up, one project per step, each only once CA1416 is clean for it: `GitExtUtils` → `GitCommands` →
`GitExtensions.Extensibility` → `ResourceManager` → `GitUI.Presentation` → `GitUI.RevisionGraph`.
- **Settings off the registry.** Move the registry-backed settings (`gitcommand`, `gitssh`, `plink`, `puttygen`,
  `pageant`, `InstallDir`, `CheckSettings`, `CascadeShellMenuItems`, `AlwaysShowAllCommands`, `ShowCurrentBranchInVS`)
  into the settings file, with a one-time import from the registry on Windows only. `ImportFromRegistry` leaves the
  static constructor. The installer keeps writing `InstallDir` for the shell extension.
- **Settings folder.** `ApplicationInfo.UserAppDataPath` per system: `%APPDATA%` (unchanged), `$XDG_CONFIG_HOME` or
  `~/.config/GitExtensions` on Linux, `~/Library/Application Support/GitExtensions` on macOS. Portable mode unchanged.
- **Fonts without GDI+.** A `FontDescriptor` (family, size, style) in `AppSettings` instead of `System.Drawing.Font`,
  stored in the same text format (`FontParser`), so existing settings load. Defaults per system: Segoe UI and Consolas
  on Windows, the system UI font and Menlo on macOS, the system font and DejaVu Sans Mono on Linux (Avalonia resolves
  the family; the fallback list of `GitExtensionsAvaloniaApp.axaml` stays).
- **Images without GDI+.** Commit template icons and the other `Image` members of GitCommands and the plugin API become
  `PluginImage` (plugin API v3, see decision 4). `ColorHelper.AdaptLightness(Bitmap)` and `LightnessCorrection` are
  replaced by `ImageLightness` of the Avalonia layer (already ported). `DpiUtil`, `Screens` and `TextMeasurement` move
  to the Avalonia equivalents (render scaling, `Screens`, `FormattedText`) or are deleted where nothing needs them.
- **ResourceManager renderers** lose their `Graphics` parameters (the Avalonia commit info only needs the text).
- **Remove dead Win32 code** found by the survey: `GitUI/Interops/*` and `GitExtUtils/GitUI/Interops/*` leftovers,
  `WebBrowserEmulationMode`, the unused AdysTech reference in GitExtUtils, and the WinForms `Form` generator of the
  analyzers.

### Phase 2: The application starts on Linux and macOS (XL)
**Under way (2026-09-25).** The spike passed on X11 (WSLg) and Windows. Done as planned, except: the owners stay native
handles (mapped to the open Avalonia windows off Windows) rather than a new `IWindowOwner`; the common dialogs keep their
API and ask `IDialogBoxHost` off Windows rather than moving every call site to `IFileDialogService`; `SystemTheme` asks
the tools of the system (`gsettings`, `defaults`), since it is read before Avalonia starts. All application projects
target `net10.0`; images that ran through GDI+ on every system are PNG data now (avatars with SkiaSharp, the icons of
scripts, shells and states from embedded resources). Under WSLg the application starts, browses, diffs and commits.
The plugins and the tests target `net10.0` too, and CI's Linux job runs the headless test suite (the part of phase 7
about test paths is done: `TestPaths.Native`). Left: macOS and the Linux issues of the ledger.
- **Avalonia desktop backends.** `Avalonia.Desktop` (Win32, X11, native macOS) instead of `Avalonia.Win32`, and
  `UsePlatformDetect()` instead of `UseWin32()`. `Project.Avalonia.targets` keeps the native assets of the published
  runtime identifier instead of always keeping the Windows ones.
- **Window owners.** An owner abstraction over the Avalonia `Window` (`IWindowOwner`) replaces `IWin32Window` in
  GitUI and in `IGitUICommands` (v3 overloads, v2 kept on Windows). `AvaloniaDialogHost` shows dialogs with
  `Window.ShowDialog(owner)` and a dispatcher frame, instead of `EnableWindow` and `GWLP_HWNDPARENT`; the window
  procedure hooks of `DialogWindow` stay behind `OperatingSystem.IsWindows()` (they only fine-tune sizing).
- **Message boxes and task dialogs.** An Avalonia message box and task dialog (buttons, icon, default button, command
  links, "don't show again", expander), shown with a dispatcher frame so `MessageBoxes`, `TaskDialog` and
  `PluginMessageBoxes` stay synchronous (decision 3). The native Windows dialogs can stay the Windows implementation;
  the Avalonia ones are then tested on Windows too.
- **Common dialogs.** Every `OpenFileDialog`, `SaveFileDialog` and `FolderBrowserDialog` use goes through
  `IFileDialogService` (`StorageProvider`), which already exists; an Avalonia color picker (`ColorView`) and a font
  picker replace `ChooseColor` and `ChooseFont`.
- **Clipboard** through the Avalonia clipboard (text and HTML) instead of `ClipboardUtil`'s user32 calls.
- **Theme.** `SystemTheme` reads the Avalonia platform settings (dark mode on every system) instead of the registry.
- **Retarget `GitUI.Avalonia`, `GitUI`, `BugReporter` and `GitExtensions`** to `net10.0`, the Windows features guarded
  (they become the capabilities of phase 4). Milestone: the application starts on Linux and macOS, opens a repository,
  browses, diffs and commits.

### Phase 3: Git, tools and processes on each system (L)
- **Git discovery.** `CheckSettingsLogic`: `git` on the `PATH`, then `/usr/bin`, `/usr/local/bin`, `/opt/homebrew/bin`
  (and Xcode's `git` on macOS). The "Linux tools" (`sh.exe`) setting and its checks are Windows-only.
- **Diff and merge tools.** `PathUtil.FindInFolders` searches `/usr/bin`, `/usr/local/bin`, `/opt/homebrew/bin`,
  `/Applications/*.app/Contents/MacOS` and `~/Applications` off Windows; the tool list shows the tools of the system
  (Meld, KDiff3, Kompare, P4Merge, Beyond Compare, VS Code, FileMerge / `opendiff` on macOS); WinMerge and TortoiseGit
  are Windows-only. Tools still run through `git difftool` / `git mergetool`.
- **Editors.** `EditorHelper` offers per system: VS Code (`code --wait`), Sublime Text (`subl -w`), Zed, gedit, Kate,
  nano / vim in a terminal, TextEdit (`open -W -n -e`) on macOS. Git Extensions' own `fileeditor` stays the default.
- **Shell and file manager.** `OsShellUtil`: `xdg-open` and the FreeDesktop `FileManager1.ShowItems` D-Bus call (or
  opening the folder) on Linux, `open` and `open -R` on macOS; "Open with…" is Windows-only.
- **Consoles and shells.** A shell provider based on `$SHELL` (bash, zsh, fish) off Windows; `CmdShell`, the PowerShell
  shells and Git Bash are Windows-only; the `/c/…` path conversion of `BashShell` is Windows-only. The built-in
  terminal is the only console off Windows. `Path.PathSeparator` instead of `;`, and no `cmd.exe` fallbacks (gitk and
  git gui are started directly).
- **Default scripts** per system (`sh` instead of `bash.exe`, no PowerShell script off Windows).
- **Hard-coded paths and suffixes.** Go through the `".exe"` and backslash literals area by area; most are Windows
  discovery code that just needs its guard. Plugins: AutoCompileSubmodules (`msbuild` on the `PATH` off Windows), Gource
  (the system's `gource`).

### Phase 4: Credentials, SSH and the Windows capabilities (M)
- **SSH askpass.** A managed askpass mode of the application (`GitExtensions askpass <prompt>`, a small Avalonia
  prompt) is set as `SSH_ASKPASS` off Windows, instead of relying on an `ssh-askpass` package; the C++
  `GitExtSshAskPass` stays on Windows until the managed one has proved itself there too. PuTTY, Plink and pageant are
  Windows-only.
- **Credentials of the plugins** (`ICredentialsManager`): the macOS Keychain (Security framework) and the Secret Service
  (libsecret) on Linux, next to the Windows Credential Manager; without a secret service, the plugin settings say so
  instead of storing the password in clear.
- **Windows capabilities** behind `IPlatformFeatures`, hidden elsewhere: taskbar progress and jump list, the Explorer
  shell extension page, Visual Studio integration, ConEmu and Mintty, `Icon.ExtractAssociatedIcon` (file icons fall back
  to the icons by extension of the file list), the WebView2 build report (off Windows the report opens in the default
  browser, as for build servers without a report tab).

### Phase 5: macOS and Linux conventions (M)
- **Cmd instead of Ctrl on macOS.** The hotkeys stay stored as `Keys` with `Control`; on macOS the key mapping
  (`KeyMapping`) handles and shows `Control` as Cmd (`KeyModifiers.Meta`), and the few Cmd shortcuts macOS reserves
  (Cmd+Q, Cmd+H, Cmd+M) are not assignable.
- **Menu bar.** The main menu as the macOS application menu (`NativeMenu`), with About, Settings (Cmd+,) and Quit where
  macOS expects them.
- **Dialog buttons** in the order of each system (the OK/Cancel order of the footer), and the file dialogs' filters per
  system.
- **Fonts and metrics.** Check the fixed widths the layout notes of the ledger rely on with the fonts of each system.
- **Single instance and "close all instances".** The broadcast message becomes a named pipe or a lock file.

### Phase 6: Packaging and CI (M)
- **Publish per runtime identifier**: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`
  (self-contained or framework-dependent, to decide per system; Linux distributions ship .NET, macOS users usually do
  not).
- **Linux**: a portable `tar.gz` first, then an AppImage; Flatpak (with a portal for the file dialogs) and deb/rpm
  later. A `.desktop` file and icons.
- **macOS**: an `.app` bundle (`Info.plist`, `.icns`), signed with a Developer ID and notarized, in a `.dmg`; one per
  architecture or a universal bundle. This needs an Apple Developer account.
- **Windows**: the MSI and the portable zip as today; `Check-BundlesConsistent.ps1` compares against a manifest per
  system instead of `Product.wxs` only.
- **Translations**: `TranslationApp` runs on the Windows job only (it has no need to run elsewhere), `findstr` replaced.
- **CI**: build and test on ubuntu, macOS and Windows; publish all packages from the release workflow.

### Phase 7: Tests on every system (M, alongside phases 1 to 5)
- `TestAppSettingsAttribute`: a lock file (or a named `Mutex`, which .NET supports on Unix) instead of the named
  `Semaphore`.
- The Win32 message pump, STA and `MessageWindowSynchronizationContext` only for the Windows-only tests;
  `UI.IntegrationTests` stays Windows-only (user32, `TestOwnerWindow`), and new headless tests cover the hosting code
  that becomes portable in phase 2.
- Test data paths through `Path.Combine` or platform-neutral fixtures instead of `C:\…` literals (164 in the Avalonia
  tests); the 10 `[Platform("Win")]` tests are reviewed.
- The headless Avalonia tests use a bundled font (for example `Avalonia.Fonts.Inter`) so that text measures, and the
  tests that depend on them, match on every system. The Verify snapshots are text, so fonts do not affect them.

## Order and milestones

1. **Phase 0 and phase 1**: portable libraries, still Windows-only as an application. Low risk, mostly mechanical, and
   it helps Windows too (no registry settings, no GDI+ fonts).
2. **Phase 2**: the milestone "it starts on Linux and macOS". The owner model and the message boxes are the riskiest
   part; do a spike first (an Avalonia message box shown with a dispatcher frame from a synchronous call deep in
   GitCommands, owned by a dialog that is itself modal) on all three systems.
3. **Phases 3 and 4**: the milestone "usable every day on Linux". Use it on Linux before starting macOS specifics.
4. **Phase 5**: macOS conventions.
5. **Phase 6**: packages for users. Phase 7 runs alongside from phase 1 on.

## Risks

- **Modality and focus differ between systems** (X11 window managers, macOS sheets). Mitigated by the phase 2 spike and
  by keeping the Windows implementation of the owner and dialog services until the Avalonia ones are proven.
- **The non-Windows branches of GitCommands have not run for years.** Each gets a test when its phase touches it.
- **Plugin compatibility.** v2 plugins keep working on Windows; off Windows, plugins with GDI+ icons or native owners
  degrade (no icon, dialogs without an owner). The PluginManager plugin itself must be checked for Windows dependencies
  before it ships elsewhere.
- **Upstream merges.** The seams stay small (a service call where the Win32 call was), and the ledger records which
  platform each area supports, as it records the ported forms.
- **macOS signing and notarization** need an Apple Developer account and a signing identity in CI.
- **Fonts and layout.** Fixed widths chosen on Windows (see "Layout" in the ledger) may clip with other fonts; phase 5
  checks them.

## Verification

- Every phase: the Windows build, all the test suites on Windows, the Windows publish and bundle check.
- From phase 1: the portable libraries' tests on ubuntu in CI; from phase 2: the whole headless test suite on ubuntu and
  macOS.
- Manual checks per system, as the smoke tests of the port: open a repository, browse, diff, blame, commit, push and
  pull over SSH and HTTPS (askpass and credentials), stash, rebase with a conflict and a merge tool, the settings
  dialog, the built-in terminal, the dark theme, 100 % and 200 % scaling.
