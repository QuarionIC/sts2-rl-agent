"""Bridge preflight: read-only checks before a real-game (live-bridge) session.

Checks, in order:
  1. Deployed mod files in the Steam mods folder (DLL/JSON/PCK + BaseLib),
     including the GDPC magic bytes of the .pck.
  2. A usable .NET 9 SDK (per-user %USERPROFILE%\\.dotnet first, then PATH).
  3. The game's Godot log (mod-load markers), if the game has ever run.
  4. Optional --model-path: reads the SB3 zip's action/observation spaces
     WITHOUT importing torch and reports whether the bridge agent_runner
     (sts2_env/bridge/agent_runner.py detect_model_mode) can load it.
  5. Optional --ping: TCP-connects to 127.0.0.1:9002 (only works while the
     modded game is running).

Usage (from the repo root):
  .venv\\Scripts\\python.exe .claude\\skills\\sts2-bridge-and-realgame\\scripts\\bridge_preflight.py
  .venv\\Scripts\\python.exe .claude\\skills\\sts2-bridge-and-realgame\\scripts\\bridge_preflight.py --model-path output\\combat_ppo\\final_model.zip
  .venv\\Scripts\\python.exe .claude\\skills\\sts2-bridge-and-realgame\\scripts\\bridge_preflight.py --ping

Exit code 0 = all executed checks passed; 1 = at least one FAIL.
Purely read-only: never writes, builds, or launches anything.
"""

from __future__ import annotations

import argparse
import base64
import json
import os
import pickle
import socket
import subprocess
import sys
import zipfile
from datetime import datetime
from pathlib import Path

DEFAULT_MODS_DIR = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods")
GODOT_LOG = (
    Path(os.environ.get("APPDATA", ""))
    / "Godot" / "app_userdata" / "Slay the Spire 2" / "logs" / "godot.log"
)
LOG_MARKERS = (
    "[STS2Bridge] Harmony: 3/3 patches applied.",
    "[STS2Bridge] TCP server started on port 9002.",
    "[STS2Bridge] === STS2 RL Bridge Mod Ready ===",
)

# Bridge-compatible model layouts (see agent_runner.detect_model_mode).
KNOWN_LAYOUTS = {
    (115, 131): "combat-only (heuristics drive non-combat phases)",
    (157, 151): "full-run legacy (RunStateAdapter drives every phase)",
}

_failures: list[str] = []


def report(ok: bool, label: str, detail: str = "") -> None:
    tag = "PASS" if ok else "FAIL"
    if not ok:
        _failures.append(label)
    line = f"[{tag}] {label}"
    if detail:
        line += f" -- {detail}"
    print(line)


def check_deployment(mods_dir: Path) -> None:
    mod_dir = mods_dir / "STS2BridgeMod"
    required = ["STS2BridgeMod.dll", "STS2BridgeMod.json", "mod_manifest.json"]
    for name in required:
        f = mod_dir / name
        if f.is_file():
            ts = datetime.fromtimestamp(f.stat().st_mtime).strftime("%Y-%m-%d %H:%M")
            report(True, f"deployed {name}", f"{f.stat().st_size} bytes, modified {ts}")
        else:
            report(False, f"deployed {name}", f"missing at {f}")

    pck = mod_dir / "STS2BridgeMod.pck"
    if pck.is_file():
        magic = pck.read_bytes()[:4]
        report(magic == b"GDPC", "STS2BridgeMod.pck GDPC magic",
               f"first bytes {magic!r}")
    else:
        report(False, "STS2BridgeMod.pck", f"missing at {pck} (game will not load the mod)")

    baselib = mods_dir / "BaseLib" / "BaseLib.dll"
    report(baselib.is_file(), "BaseLib dependency deployed", str(baselib))


def check_dotnet() -> None:
    candidates = [
        Path(os.environ.get("USERPROFILE", "")) / ".dotnet" / "dotnet.exe",
        Path(r"C:\Program Files\dotnet\dotnet.exe"),
    ]
    for exe in candidates:
        if not exe.is_file():
            continue
        try:
            out = subprocess.run(
                [str(exe), "--list-sdks"], capture_output=True, text=True, timeout=30
            )
            if out.returncode == 0 and out.stdout.strip():
                report(True, "dotnet SDK", f"{exe}: {out.stdout.strip().splitlines()[0]}")
                return
        except OSError:
            pass
    report(False, "dotnet SDK",
           "no dotnet.exe with an installed SDK found (per-user ~/.dotnet or PATH); "
           "needed only for rebuilding the mod, not for running the agent")


def check_godot_log() -> None:
    if not GODOT_LOG.is_file():
        # Not a failure: the log only exists after the game has been launched.
        print(f"[INFO] godot.log not found at {GODOT_LOG} "
              "(game not launched yet on this machine, or custom user dir)")
        return
    text = GODOT_LOG.read_text(encoding="utf-8", errors="replace")
    for marker in LOG_MARKERS:
        report(marker in text, f"log marker {marker!r}")


def check_model(model_path: Path) -> None:
    if not model_path.is_file():
        report(False, "model file", f"missing: {model_path}")
        return
    try:
        with zipfile.ZipFile(model_path) as z:
            data = json.loads(z.read("data"))
        action_space = pickle.loads(base64.b64decode(data["action_space"][":serialized:"]))
        obs_space = pickle.loads(base64.b64decode(data["observation_space"][":serialized:"]))
        action_n = int(action_space.n)
        obs_n = int(obs_space.shape[0])
    except Exception as exc:  # noqa: BLE001 - report and continue
        report(False, "model readable", f"{model_path}: {exc}")
        return

    layout = KNOWN_LAYOUTS.get((action_n, obs_n))
    if layout is not None:
        report(True, "model bridge-compatible",
               f"action={action_n}, obs={obs_n}: {layout}")
    else:
        hint = ""
        if obs_n == 4184:
            hint = (" -- this is a rich-obs model; the bridge has NO adapter for it "
                    "(agent_runner.detect_model_mode raises ValueError). "
                    "A RichRunStateAdapter must be written first.")
        report(False, "model bridge-compatible",
               f"action={action_n}, obs={obs_n} matches no known bridge layout{hint}")


def check_ping(host: str, port: int) -> None:
    try:
        with socket.create_connection((host, port), timeout=3) as sock:
            sock.sendall(json.dumps({"action": "ping"}).encode() + b"\n")
        report(True, "TCP bridge reachable", f"{host}:{port}")
    except OSError as exc:
        report(False, "TCP bridge reachable",
               f"{host}:{port}: {exc} (is the modded game running at the main menu?)")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--mods-dir", type=Path, default=DEFAULT_MODS_DIR,
                        help="Steam mods folder (default: standard Steam library)")
    parser.add_argument("--model-path", type=Path, default=None,
                        help="Optional SB3 .zip to check for bridge compatibility")
    parser.add_argument("--ping", action="store_true",
                        help="Also try a TCP ping to the live game bridge")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=9002)
    args = parser.parse_args()

    check_deployment(args.mods_dir)
    check_dotnet()
    check_godot_log()
    if args.model_path is not None:
        check_model(args.model_path)
    if args.ping:
        check_ping(args.host, args.port)

    if _failures:
        print(f"\n{len(_failures)} check(s) FAILED: {', '.join(_failures)}")
        return 1
    print("\nAll executed checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
