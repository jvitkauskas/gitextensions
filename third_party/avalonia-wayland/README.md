# Temporary Avalonia Wayland package

Git Extensions vendors **Avalonia.Wayland 12.1.3-gitextensions-wayland.1** to fix modal dialogs reopening after a
shortcut is released. This is a patched build for this application, not an official Avalonia release.

- Upstream report: [Avalonia#22317](https://github.com/AvaloniaUI/Avalonia/issues/22317).
- Upstream fix PR: [Avalonia#22318](https://github.com/AvaloniaUI/Avalonia/pull/22318), with tests followed by the fix.
- Base: Avalonia 12.1.3, commit `8eeda4f6f546165b3f72e63c9f42247abb306905`.
- Patched source: [fa5cfdcb672dc1c615848bac71f9ae5b47c60328](https://github.com/jvitkauskas/Avalonia/commit/fa5cfdcb672dc1c615848bac71f9ae5b47c60328).
  This commits the same fix and tests on the 12.1.3 base; the PR targets the newer upstream main branch.
- Target: **net10.0 only**, matching this application. The other Avalonia dependencies remain official 12.1.3 packages.

The patch stops key repeat when a modal owner becomes disabled, processes release/focus-loss cleanup while disabled,
and rejects repeat into disabled/disposed sinks. `source.patch` contains the full source change and eight Linux
regression cases. Four fail before the fix; all 49 Wayland tests pass afterwards. The patched backend passed actual
native Hyprland dialog cycles, including held Ctrl+O and nested Escape dismissal; see the Linux QA report for scope.

Following broader application QA, Linux sessions with `WAYLAND_DISPLAY` now use native Wayland by default.
Set `GITEXTENSIONS_USE_WAYLAND=0` to force X11; initialization failures also fall back to X11. The backend remains
experimental, and this patch does not establish complete parity across compositors.

## Restore and integrity

The `.nupkg` is checked in under `third_party/nuget/`. Root `NuGet.Config` maps the exact `Avalonia.Wayland` package
ID exclusively to that relative local source; all other package IDs use nuget.org. `Directory.Packages.props`
uses an exact version range, so restore cannot silently substitute another version. No separate feed credentials
or local paths are needed on CI or another contributor's machine. NuGet may reuse an already restored version
from its cache, so package versions must never be reused for changed contents.

For a separate clean-cache Release restore/build, use matching configurations for both commands. This repository
keeps intermediate restore assets separately by configuration:

```sh
dotnet restore GitExtensions.slnx -p:Configuration=Release
dotnet build GitExtensions.slnx -c Release --no-restore
```

`provenance.json` records the source revision, build SDK, patch checksum and package/DLL SHA-256 values.
To verify the checked-in artifacts using Python 3:

```sh
python3 third_party/avalonia-wayland/rebuild.py --verify-only
```

`LICENSE.md` and `NOTICE.md` are copied from the pinned upstream source and are also embedded in the package.
The package contains only the Wayland assembly and its documentation/symbols, plus metadata and notices.

## Rebuild

Requirements: Git, Python 3 and **.NET SDK 10.0.201** on PATH. Linux is required to execute the eight keyboard-repeat
regressions; those cases skip off Linux. Normal application builds do not need Python or this source checkout.

From the Git Extensions repository root, choose a new work and output directory:

```sh
python3 third_party/avalonia-wayland/rebuild.py \
  --work-dir /tmp/avalonia-wayland-rebuild \
  --output /tmp/avalonia-wayland-packages
```

The script fetches the exact committed source and its pinned submodules, verifies that the patch is present,
builds with `ContinuousIntegrationBuild=true`, runs the Wayland tests with a three-minute timeout, and packs a
NuGet package whose repository metadata identifies the patched revision. It refuses to overwrite an output package.
Set `NUGET_PACKAGES` and `DOTNET_CLI_HOME` to scratch paths if isolation from existing caches is desired.

The explicit `package.nuspec` uses the official Wayland package's dependency layout. Upstream's normal release
pipeline merges Avalonia.Dialogs into the Avalonia package; direct project packing otherwise incorrectly declares
a dependency on an unpublished Avalonia.Dialogs package. Rebuild inputs are pinned, but NuGet archive timestamps
may differ between rebuilds; do not assume byte-for-byte identical `.nupkg` files.

When changing the patch, use a new committed source revision and package version, update the manifest/patch,
rebuild, and update the exact central pin and checksums. Test restore with empty NuGet caches as well as the build,
full application suites and the native modal regression. Do not overwrite an existing version in anyone's cache.

## Remove after an upstream release

Once an official release contains the fix:

1. Upgrade the Avalonia package set to compatible official versions.
2. Remove the `vendored` source and the exact `Avalonia.Wayland` source mapping from `NuGet.Config` so that it restores
   from nuget.org again. The generic nuget.org mapping can remain.
3. Remove this directory and the temporary `.nupkg` from `third_party/nuget/`.
4. Repeat the native modal regression and clean-cache build/test checks when upgrading the backend.
