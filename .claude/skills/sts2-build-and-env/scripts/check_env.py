"""Environment doctor for sts2-rl-agent (read-only).

Checks the Python venv, GPU torch build, editable install, decompiled
ground-truth trees, and the .NET/Godot/Steam toolchain for the bridge mod.
Prints PASS/WARN/FAIL lines and exits 1 if any FAIL.

Run with the project venv python:
    C:\\Users\\motqu\\GitHub\\sts2-rl-agent\\.venv\\Scripts\\python.exe \\
        .claude/skills/sts2-build-and-env/scripts/check_env.py

Expected-good values are pinned to the state verified on 2026-07-24; version
drift prints WARN (not FAIL) so an intentionally upgraded env does not
false-alarm.
"""

from __future__ import annotations

import os
import sys
from importlib import metadata
from pathlib import Path

# repo/.claude/skills/sts2-build-and-env/scripts/check_env.py -> repo root
REPO = Path(__file__).resolve().parents[4]

# Versions verified installed and working on 2026-07-24.
EXPECTED = {
    "torch": "2.11.0+cu128",
    "stable_baselines3": "2.9.0",
    "sb3_contrib": "2.9.0",
    "gymnasium": "1.3.0",
    "numpy": "2.5.1",
}

failures = 0
warnings = 0


def _pass(msg: str) -> None:
    print(f"PASS  {msg}")


def _warn(msg: str) -> None:
    global warnings
    warnings += 1
    print(f"WARN  {msg}")


def _fail(msg: str) -> None:
    global failures
    failures += 1
    print(f"FAIL  {msg}")


def check_python() -> None:
    ver = sys.version.split()[0]
    if sys.version_info >= (3, 11):
        _pass(f"Python {ver} at {sys.executable}")
    else:
        _fail(f"Python {ver} < 3.11 (pyproject requires-python >=3.11)")
    exe = sys.executable.replace("\\", "/").lower()
    if "/.venv/" not in exe and "/venv/" not in exe:
        _warn("not running from the project .venv -- use "
              ".venv\\Scripts\\python.exe")


def check_packages() -> None:
    for dist, expected in EXPECTED.items():
        try:
            got = metadata.version(dist)
        except metadata.PackageNotFoundError:
            _fail(f"{dist} not installed (pip install -e \".[dev,train]\")")
            continue
        if got == expected:
            _pass(f"{dist}=={got}")
        else:
            _warn(f"{dist}=={got} (verified-good 2026-07-24 was {expected})")


def check_torch_cuda() -> None:
    try:
        import torch
    except ImportError:
        _fail("torch not importable")
        return
    if "+cu" not in torch.__version__:
        _fail(f"torch {torch.__version__} is a CPU-only build -- the cu128 "
              "wheel was replaced (did something run 'uv sync'?)")
    if torch.cuda.is_available():
        _pass(f"CUDA available: {torch.cuda.get_device_name(0)}")
    else:
        _fail("torch.cuda.is_available() is False (training would silently "
              "run on CPU)")


def check_editable_install() -> None:
    try:
        import sts2_env
    except ImportError:
        _fail("sts2_env not importable (run pip install -e \".[dev,train]\")")
        return
    loc = Path(sts2_env.__file__).resolve()
    if REPO in loc.parents:
        _pass(f"sts2_env imports from the repo checkout ({loc.parent})")
    else:
        _fail(f"sts2_env imports from OUTSIDE the repo: {loc} "
              "(stale non-editable install shadowing the checkout)")


def check_decompiled_trees() -> None:
    for tree in ("decompiled", "decompiled_v0.109.0", "decompiled_mods"):
        p = REPO / tree
        if p.is_dir():
            n = sum(1 for _ in p.rglob("*.cs"))
            _pass(f"{tree}/ present ({n} .cs files)")
        else:
            _fail(f"{tree}/ missing -- it is git-tracked; restore via git")
    for mod in ("ActsFromThePast", "Act4Heart"):
        if not (REPO / "decompiled_mods" / mod).is_dir():
            _fail(f"decompiled_mods/{mod}/ missing (active campaign mod)")


def check_dotnet() -> None:
    per_user = Path(os.environ.get("USERPROFILE", "")) / ".dotnet" / "dotnet.exe"
    if per_user.exists():
        _pass(f".NET SDK (per-user) at {per_user}")
    else:
        _warn(f"no dotnet.exe at {per_user} -- bridge-mod builds need the "
              ".NET 9 SDK (./dotnet-install.ps1 -Channel 9.0)")


def check_godot() -> None:
    godot = Path("C:/megadot/Godot_v4.5.1-stable_mono_win64/"
                 "Godot_v4.5.1-stable_mono_win64_console.exe")
    if godot.exists():
        _pass(f"Godot 4.5.1 Mono at {godot}")
    else:
        _warn(f"Godot 4.5.1 Mono not found at {godot} -- bridge_mod builds "
              "will FAIL at the GodotExportSkipped target (csproj:136-138)")


def check_steam_game() -> None:
    steam = None
    if sys.platform == "win32":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER,
                                r"Software\Valve\Steam") as k:
                steam = Path(winreg.QueryValueEx(k, "SteamPath")[0])
        except OSError:
            pass
    if steam is None:
        steam = Path("C:/Program Files (x86)/Steam")
    dll = (steam / "steamapps" / "common" / "Slay the Spire 2"
           / "data_sts2_windows_x86_64" / "sts2.dll")
    if dll.exists():
        _pass(f"game DLL at {dll}")
    else:
        _warn(f"sts2.dll not found under {steam} -- bridge-mod builds and "
              "re-decompilation need the Steam install")


def check_gitignored_hazards() -> None:
    if (REPO / "extracted_pck").is_dir():
        _pass("extracted_pck/ present (gitignored, ~2.5 GB)")
    else:
        _warn("extracted_pck/ absent (normal; regenerate with GDRE Tools "
              "only if you need localization/resource files)")
    if not (REPO / "bridge_mod" / "nuget.config").exists() and \
            not (REPO / "nuget.config").exists():
        _warn("nuget.config absent (it is gitignored) -- BaseLib NuGet "
              "restore may fail on a fresh machine; see MOD_BUILD_GUIDE.md")


def main() -> int:
    print(f"repo root: {REPO}")
    check_python()
    check_packages()
    check_torch_cuda()
    check_editable_install()
    check_decompiled_trees()
    check_dotnet()
    check_godot()
    check_steam_game()
    check_gitignored_hazards()
    print(f"\n{failures} FAIL, {warnings} WARN")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
