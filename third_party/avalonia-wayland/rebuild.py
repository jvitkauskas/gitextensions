#!/usr/bin/env python3
"""Rebuild the pinned Wayland package, or verify the checked-in artifacts."""

import argparse
import hashlib
import json
from pathlib import Path
import shlex
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile


HERE = Path(__file__).resolve().parent
MANIFEST = json.loads((HERE / "provenance.json").read_text(encoding="utf-8"))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def run(*args, cwd, timeout=None):
    print("+ " + shlex.join(str(arg) for arg in args), flush=True)
    subprocess.run([str(arg) for arg in args], cwd=cwd, check=True, timeout=timeout)


def verify():
    package = HERE.parent / "nuget" / MANIFEST["package_file"]
    assert digest(package.read_bytes()) == MANIFEST["package_sha256"], "Package checksum mismatch"
    assert digest((HERE / "source.patch").read_bytes()) == MANIFEST["patch_sha256"], "Patch checksum mismatch"
    with zipfile.ZipFile(package) as archive:
        dll = archive.read("lib/net10.0/Avalonia.Wayland.dll")
        assert digest(dll) == MANIFEST["dll_sha256"], "DLL checksum mismatch"
        ns = {"n": "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"}
        spec = ET.fromstring(archive.read("Avalonia.Wayland.nuspec"))
        assert spec.find("n:metadata/n:version", ns).text == MANIFEST["package_version"]
        assert spec.find("n:metadata/n:repository", ns).get("commit") == MANIFEST["source_commit"]
    print(f"Verified {package.name}, its DLL, source revision and source.patch")


def rebuild(work, output):
    work = work.resolve()
    output = output.resolve()
    if (output / MANIFEST["package_file"]).exists():
        raise SystemExit("Output package already exists. Use a new output directory; never overwrite a restored version.")
    if digest((HERE / "source.patch").read_bytes()) != MANIFEST["patch_sha256"]:
        raise SystemExit("Patch checksum mismatch")
    work.mkdir(parents=True, exist_ok=False)
    output.mkdir(parents=True, exist_ok=True)
    source = work / "source"
    run("git", "init", "--quiet", source, cwd=work)
    run("git", "remote", "add", "origin", MANIFEST["source_repository"], cwd=source)
    run("git", "fetch", "--depth", "1", "origin", MANIFEST["source_commit"], cwd=source)
    run("git", "checkout", "--detach", "FETCH_HEAD", cwd=source)
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=source, text=True).strip()
    if revision != MANIFEST["source_commit"]:
        raise SystemExit("Unexpected source revision")
    run("git", "apply", "--reverse", "--check", HERE / "source.patch", cwd=source)
    run("git", "submodule", "update", "--init", "--depth", "1", cwd=source)
    sdk = subprocess.check_output(["dotnet", "--version"], cwd=source, text=True).strip()
    if sdk != MANIFEST["sdk_version"]:
        raise SystemExit(f"Use .NET SDK {MANIFEST['sdk_version']} for this package; found {sdk}")

    run("dotnet", "build", "tests/Avalonia.Wayland.UnitTests/Avalonia.Wayland.UnitTests.csproj",
        "-c", "Release", "-p:AvsSkipBuildingLegacyTargetFrameworks=True",
        "-p:ContinuousIntegrationBuild=true", cwd=source)
    run("dotnet", "tests/Avalonia.Wayland.UnitTests/bin/Release/net10.0/Avalonia.Wayland.UnitTests.dll",
        cwd=source, timeout=180)

    stage = work / "package-content"
    stage.mkdir()
    for extension in ("dll", "pdb", "xml"):
        name = f"Avalonia.Wayland.{extension}"
        shutil.copy2(source / "src/Avalonia.Wayland/bin/Release/net10.0" / name, stage / name)
    shutil.copy2(source / "build/Assets/Icon.png", stage / "Icon.png")
    shutil.copy2(source / "licence.md", stage / "LICENSE.md")
    shutil.copy2(source / "NOTICE.md", stage / "NOTICE.md")
    shutil.copy2(HERE / "README.md", stage / "README.md")
    spec = ET.parse(HERE / "package.nuspec")
    ns = {"n": "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"}
    ET.register_namespace("", ns["n"])
    spec.find("n:metadata/n:version", ns).text = MANIFEST["package_version"]
    repository = spec.find("n:metadata/n:repository", ns)
    repository.set("url", MANIFEST["source_repository"])
    repository.set("commit", MANIFEST["source_commit"])
    nuspec = work / "Avalonia.Wayland.nuspec"
    spec.write(nuspec, encoding="utf-8", xml_declaration=True)
    run("dotnet", "pack", "src/Avalonia.Wayland/Avalonia.Wayland.csproj", "-c", "Release",
        "--no-build", "--no-restore", "-p:AvsSkipBuildingLegacyTargetFrameworks=True",
        "-p:IncludeSymbols=false", f"-p:NuspecFile={nuspec}", f"-p:NuspecBasePath={stage}",
        "-o", output, cwd=source)
    package = output / MANIFEST["package_file"]
    print(f"Package SHA-256: {digest(package.read_bytes())}")
    print(f"DLL SHA-256: {digest((stage / 'Avalonia.Wayland.dll').read_bytes())}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify-only", action="store_true")
    parser.add_argument("--output", type=Path, help="Output directory outside the vendored feed")
    parser.add_argument("--work-dir", type=Path, help="New build directory; otherwise a temporary directory is used")
    args = parser.parse_args()
    if args.verify_only:
        verify()
    elif not args.output:
        parser.error("--output is required when rebuilding")
    elif args.work_dir:
        rebuild(args.work_dir, args.output)
    else:
        with tempfile.TemporaryDirectory(prefix="avalonia-wayland-") as temporary:
            rebuild(Path(temporary) / "build", args.output)
