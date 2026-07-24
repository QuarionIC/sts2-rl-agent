---
name: sts2-build-and-env
description: >
  Recreate, verify, or repair the sts2-rl-agent development environment on
  Windows: the Python .venv (Store Python 3.13 + editable install + the
  undocumented CUDA cu128 torch wheel), the uv.lock trap, the three decompiled
  C# ground-truth trees and how to regenerate them (ilspycmd / GDRE Tools),
  the .NET 9 + Godot 4.5.1 Mono toolchain the bridge mod build needs, what is
  git-tracked vs gitignored, and Windows-specific traps (SubprocVecEnv spawn
  pickling, __main__ guards, Microsoft Store Python aliases, per-user dotnet
  PATH). Load this when torch/CUDA looks wrong, when setting up a fresh
  machine or clone, when a build tool is missing, when deciding which
  decompiled tree to trust, or when a failure only happens on Windows.
  Do NOT load this for: launching/resuming training or evals
  (sts2-run-and-operate), bridge-mod deploy/verify/protocol or Harmony patch
  maintenance (sts2-bridge-and-realgame), parity audit semantics and the
  PATCHED allowlists (sts2-parity-discipline), test-suite anatomy
  (sts2-testing-and-qa), or game-mechanics content questions
  (sts2-game-and-mods-reference).
---

# Build and environment: recreate, verify, repair

This skill is the single source of record for how this project's development
environment is actually constructed — including the one load-bearing fact
written down nowhere else in the repo: **the GPU torch build was installed
manually from the cu128 index and is not captured by `uv.lock`, `pyproject.toml`,
or any doc** (verified by grep across all repo `*.md` on 2026-07-24: zero
mentions of `cu128`, `download.pytorch.org`, or `index-url`).

Jargon used here, defined once:

- **cu128** — PyTorch wheel variant compiled against CUDA 12.8 (the `+cu128`
  suffix in `torch.__version__`). The default PyPI wheel is CPU-only.
- **editable install** — `pip install -e .`: the venv imports `sts2_env`
  directly from the checkout, so source edits apply without reinstalling.
- **PCK** — Godot's packed resource archive format. The game ships resources
  in `sts2.pck`; mods must also ship a `.pck`.
- **Harmony** — .NET runtime patching library (`0Harmony.dll`) the bridge mod
  uses to hook game methods.
- **Megadot** — MegaCrit's fork/build of Godot 4.5.1 that the game runs on
  (`bridge_mod/STS2BridgeMod.csproj:22`).
- **AFTP** — the ActsFromThePast mod (legacy STS1 acts). With Act4Heart it is
  ACTIVE in the campaign config; both are decompiled under `decompiled_mods/`.
- **parity** — the discipline of matching simulator behavior to the decompiled
  C# bit-for-bit; owned by sts2-parity-discipline.

## 0. Fast health check

Run the read-only doctor script first whenever anything environmental smells
wrong:

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe .claude/skills/sts2-build-and-env/scripts/check_env.py
```

Known-good output (2026-07-24) is all PASS plus exactly two WARNs
(`extracted_pck/` absent, `nuget.config` absent — both normal; see sections
4 and 5). Any FAIL line names its fix. The script is read-only and safe to
run any time; it imports torch, so it takes a few seconds.

## 1. The Python environment: what it is and why

### 1.1 Verified state (2026-07-24, HEAD fe25668)

| Component | Value | Evidence |
|---|---|---|
| venv path | `C:\Users\motqu\GitHub\sts2-rl-agent\.venv` | exists |
| Created by | plain `python -m venv` (NOT uv) | `.venv/pyvenv.cfg` `command =` line |
| Base interpreter | Microsoft Store Python 3.13 (`...\WindowsApps\PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0`) | `.venv/pyvenv.cfg` `home =` line |
| Python | 3.13.14 | `.venv\Scripts\python.exe --version` |
| torch | **2.11.0+cu128**, CUDA verified on NVIDIA GeForce RTX 4060 Laptop GPU | `python -c "import torch; print(torch.__version__, torch.cuda.is_available())"` |
| stable_baselines3 / sb3_contrib | 2.9.0 / 2.9.0 | `pip list` |
| gymnasium / numpy | 1.3.0 / 2.5.1 | `pip list` |
| pytest / cloudpickle / pip | 9.1.1 / 3.1.2 / 26.1.2 | `pip list` |
| sts2-rl-agent | 0.1.0, editable at repo root | `pip list` shows the path |

`pyproject.toml` (27 lines) declares only `gymnasium>=1.0.0` and `numpy>=1.26`
as core deps, with extras `[train]` (stable-baselines3>=2.3.0,
sb3-contrib>=2.3.0, torch>=2.0) and `[dev]` (pytest>=8.0, pytest-cov>=5.0);
`requires-python = ">=3.11"`; packages found via
`include = ["sts2_env*"]` (`pyproject.toml:22-23`); pytest `testpaths =
["tests"]` (`pyproject.toml:26`). There is no `[build-system]` table, no
entry points, and no `[tool.uv]` section.

### 1.2 The uv.lock trap (NEVER `uv sync`)

`uv.lock` is git-tracked but **stale and CPU-only**. Verified against the
lock file on 2026-07-24:

| Package | uv.lock pins | Live venv has |
|---|---|---|
| torch | 2.10.0, `source = { registry = "https://pypi.org/simple" }` (CPU wheel; no pytorch.org index anywhere in the lock) | 2.11.0+cu128 (GPU) |
| stable-baselines3 | 2.7.1 | 2.9.0 |
| sb3-contrib | 2.7.1 | 2.9.0 |
| gymnasium | 1.2.3 | 1.3.0 |
| numpy | 2.4.3 | 2.5.1 |

Running `uv sync` would therefore silently replace the working GPU torch with
a CPU build and downgrade everything else. **House rule (2026-07-24, applies
to this repo): never `uv sync` or reinstall over `.venv`.** If you suspect
the env is broken, run the doctor script (section 0) and repair surgically,
or recreate from scratch (section 1.3). Whether uv ever created this venv is
doubtful — `pyvenv.cfg` shows plain `python -m venv`; the lock was most
likely generated once and never used since.

The one symptom that proves this trap fired: `torch.__version__` has no
`+cu128` suffix, or `torch.cuda.is_available()` returns `False`. Training
then still "works" but runs on CPU at a fraction of the speed — SB3 does not
error, so nothing else will tell you.

### 1.3 Recreate the venv from scratch (checklist)

Use only if `.venv` is genuinely destroyed. Steps 1-2 are documented repo
procedure (`CONTRIBUTING.md:23`, `README.md:64-70`); **step 3 is
reconstructed** (2026-07-24) — the cu128 install command appears in no repo
doc; it is inferred from the observed `2.11.0+cu128` wheel, and step 4
verifies it regardless.

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent

# 1. Create the venv from a Python >= 3.11 (Store Python 3.13 is what the
#    known-good env used; any 3.11+ CPython should work per pyproject).
python -m venv .venv

# 2. Editable install with train+dev extras. This pulls the CPU torch wheel
#    first -- that is expected and fixed in step 3.
.venv\Scripts\python.exe -m pip install -e ".[dev,train]"

# 3. Replace CPU torch with the CUDA 12.8 build (RECONSTRUCTED command --
#    verify with step 4). Deps are already satisfied from step 2, so the
#    restricted index is fine.
.venv\Scripts\python.exe -m pip uninstall -y torch
.venv\Scripts\python.exe -m pip install torch==2.11.0 --index-url https://download.pytorch.org/whl/cu128

# 4. Verify GPU torch (must print: 2.11.0+cu128 True NVIDIA GeForce RTX 4060 Laptop GPU)
.venv\Scripts\python.exe -c "import torch; print(torch.__version__, torch.cuda.is_available(), torch.cuda.get_device_name(0))"

# 5. Verify collection (must report 5,276 tests as of HEAD fe25668, 2026-07-24)
.venv\Scripts\python.exe -m pytest --collect-only -q tests

# 6. Full suite green before using the env for real work (house rule; see
#    sts2-change-control for the full gate, sts2-testing-and-qa for anatomy)
.venv\Scripts\python.exe -m pytest tests/ -q
```

- [ ] Step 4 prints `+cu128` and `True` — if not, stop; you have a CPU env.
- [ ] Step 5 count matches the current HEAD's expected count (5,276 at
      fe25668; re-check with the provenance command if HEAD moved).
- [ ] Doctor script (section 0) is all-PASS on the Python rows.

Never install torch (or anything) into the global/Store Python — everything
goes through `.venv`. If newer package versions are deliberately adopted,
update the EXPECTED table in `scripts/check_env.py` in the same change (see
sts2-change-control for how that lands).

### 1.4 Always `.venv\Scripts\python.exe`, never bare `python`

The base interpreter is Microsoft Store Python. Two consequences:

1. Bare `python` outside the venv resolves through the WindowsApps
   app-execution alias, which on some machines opens the Microsoft Store
   instead of running anything. Every command in this skill library therefore
   uses the explicit `.venv\Scripts\python.exe` prefix — copy that habit.
2. The Store install lives under
   `%LOCALAPPDATA%\Microsoft\WindowsApps\PythonSoftwareFoundation.Python.3.13_...`
   (see `.venv/pyvenv.cfg`), a sandboxed per-user location. Do not try to
   pip-install into it; it is only the venv's base.

## 2. Windows-specific Python traps

### 2.1 SubprocVecEnv spawn pickling

On win32, Python multiprocessing uses **spawn** (not fork): child processes
re-import modules and receive pickled callables. SB3's `SubprocVecEnv`
therefore imposes two hard rules that only break on Windows:

1. **Env factory callables must be module-level** (hence picklable).
   `scripts/train_necrobinder.py` documents this explicitly — the section
   header comment "Env factories (module-level so SubprocVecEnv can pickle
   them on Windows)" (`scripts/train_necrobinder.py:79` at HEAD fe25668) and
   the `make_stage_env` factory below it, bound with `functools.partial`
   rather than a lambda or closure.
2. **Every training entry script needs the `if __name__ == "__main__":`
   guard** (`scripts/train_necrobinder.py:415` at fe25668) — without it,
   spawn re-executes the script top-level in every worker, forking-bombing
   the machine or crashing with cryptic pickling/recursion errors.

The selection logic is `SubprocVecEnv(factories) if n_envs > 1 else
DummyVecEnv(factories)` (`scripts/train_necrobinder.py:295-298` at fe25668).
Consequence for debugging: **any bug that only appears with `--n-envs 2+` and
not with 1 env is a spawn-pickling suspect first.** Line numbers here drift
with the in-flight training revamp — grep for `SubprocVecEnv` in the script
rather than trusting them.

New env-factory code must follow the same pattern: module-level function,
arguments via `functools.partial`, nothing captured from local scope.

### 2.2 Other Windows notes

- Paths in repo code are handled via `pathlib`; when writing commands prefer
  forward slashes or quoted backslash paths — both work in PowerShell.
- `*.log` is gitignored globally (`.gitignore:49`), which includes the
  campaign logs under `output/` — do not expect them in `git status`.
- The doctor script reads the Steam path from the registry key
  `HKCU\Software\Valve\Steam@SteamPath` exactly as the mod build does
  (section 5.3).

## 3. Decompiled ground truth: three trees, one precedence rule

The simulator is verified against decompiled C# committed to git. There are
**three** trees, and picking the wrong one produces wrong answers (example:
the Doom card was reworked in v0.109.0 — reading the old tree gives stale
values; see sts2-game-and-mods-reference for the mechanics themselves).

| Tree | Tracked files (2026-07-24) | What it is | Who uses it |
|---|---|---|---|
| `decompiled/` | 3,300 | Older game build (exact version undocumented anywhere; pre-0.109.0) | Still parsed by parity tooling: `sts2_env/cards/reference_static_metadata.py:16` (`REFERENCE_CARD_DIR = Path("decompiled/MegaCrit.Sts2.Core.Models.Cards")`) and `scripts/parity_reference_audit.py:74-109` |
| `decompiled_v0.109.0/` | 3,495 | Current beta v0.109.0 — **authoritative** | Campaign-era docs and code cite it (`docs/KNOWN_ISSUES.md` issues 6/7/16, `docs/TRAINING_REDESIGN.md:4`); many `sts2_env/` docstrings |
| `decompiled_mods/` | 2,590 | Decompiled mod DLLs: `Act4Heart/`, `ActsFromThePast/`, `BaseLib/`, `Downfall/` | AFTP + Act4Heart are ACTIVE in the campaign target ("v0.109.0 beta + Acts from the Past + Act 4 Heart mods", `docs/TRAINING_REDESIGN.md:4`) |

**Precedence rule:** for current game behavior, `decompiled_v0.109.0/` wins;
`decompiled/` is consulted only because legacy parity tooling still points at
it; mod behavior lives in `decompiled_mods/{ActsFromThePast,Act4Heart}`.
All three are plain committed `.cs` files — Grep them directly; no tooling
needed to read them. How parity claims must cite these files, and the PATCHED
allowlist for deliberate deviations, is owned by sts2-parity-discipline.

Count trick: `git ls-files decompiled_v0.109.0/ | measure` style counts
include a handful of non-`.cs` files; the doctor script counts `.cs` only
(3,299 / 3,494 / 2,586) — both are "right".

### 3.1 Regenerating after a game update

Tools (versions the project actually used, `docs/DECOMPILATION_GUIDE.md:20-28`):
ILSpy 10.0 (GUI browsing), **ilspycmd 9.1.0** (bulk CLI), **GDRE Tools 2.4.0**
(PCK extraction). Install ilspycmd once via the .NET SDK (section 5.1):

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" tool install -g ilspycmd
```

Decompile the game DLL — **always into a NEW versioned directory**, never
over an existing tree (the whole point of keeping `decompiled/` and
`decompiled_v0.109.0/` side by side is diffability across game versions):

```powershell
# -p = one file per class, organized by namespace (~3,300-3,500 .cs files)
ilspycmd -p -o decompiled_vNEW/ "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll"
```

Then diff against the previous tree to find changed mechanics, and follow the
game-update checklist in sts2-bridge-and-realgame — in particular, Harmony
binds injected prefix parameters **by name** and skips silently on mismatch;
a v0.109.0 parameter rename (`timeScale` → `scale` on
`MegaAnimationState.SetTimeScale`) silently disabled a patch once
(`docs/KNOWN_ISSUES.md:71-79`, `docs/MOD_BUILD_GUIDE.md:241`). Re-verifying
every patch target's signature against the fresh tree is mandatory.

Resources (only needed for localization text, card art, scenes — **no Python
code reads `extracted_pck/`**, verified by grep 2026-07-24):

```powershell
gdre_tools.exe --headless --recover="C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\sts2.pck"
```

This produces ~24,000 files in `extracted_pck/`, which is gitignored at
~2.5 GB (`.gitignore:45-46`) and **absent from the local checkout as of
2026-07-24** — regenerate it locally if and only if you need it. The most
valuable content is `localization/eng/*.json` (SmartFormat templates with
numeric values, cross-checkable against decompiled code;
`docs/DECOMPILATION_GUIDE.md:115-145`).

Committing a new decompiled tree is a repo change like any other — route it
through sts2-change-control (it changes what "ground truth" means for every
parity claim downstream).

## 4. What is tracked vs gitignored

From `.gitignore` (verified 2026-07-24) — the operational consequences matter
more than the list:

| Ignored | Line | Consequence |
|---|---|---|
| `.venv/` | 11 | The env is machine-local; this skill is its only spec |
| `output/`, `*.zip`, `*.pt`, `*.pth` | 25-28 | **Model checkpoints and training runs never appear in `git status`** — deleting a branch cannot lose them, but nothing backs them up either. Layout of `output/` is owned by sts2-run-and-operate |
| `tensorboard_logs/`, `tb_logs/` | 29-30 | Same for TB event files |
| `bridge_mod*/bin`, `obj`, `.godot/` | 33-43 | .NET build intermediates |
| `extracted_pck/` | 46 | 2.5 GB, regenerate-on-demand (section 3.1) |
| `*.log` | 49 | Includes `output/*_campaign.log` |
| `.pytest_cache/`, `.specstory/` | 50-51 | — |
| `nuget.config` | 54 | **Trap:** the BaseLib NuGet feed config is NOT in git — see section 5.4 |

NOT ignored (deliberately committed): all three `decompiled*` trees,
`uv.lock` (stale — section 1.2), and `dotnet-install.ps1` (71 KB official
Microsoft installer script kept at repo root for convenience).

Current `output/` contents (2026-07-24, informational only — a concurrent
training session updates this): `combat_ppo/`, `necrobinder_a10/`,
`necro_a10_ppo/`, `necrobinder_g1/`, `necrobinder_a10_campaign.log`,
`necrobinder_g1_campaign.log`. The `necrobinder_g1` entries are the revamp
G1 stage; see sts2-training-campaign for what that is.

## 5. .NET 9 + Godot 4.5.1 toolchain (bridge-mod build prerequisites)

This section owns the **toolchain**: what must be installed, where, and the
version traps. The build/deploy/verify workflow, godot.log markers, wire
protocol, and Harmony patch maintenance are owned by
**sts2-bridge-and-realgame** — go there before actually building or
deploying. Note that `dotnet build` in `bridge_mod/` **deploys into the live
game's `mods/` folder as a post-build step** (csproj:117-127), so a build is
never a no-op; treat it as a bridge-class change per sts2-change-control.

### 5.1 .NET 9 SDK

The game and mod target `net9.0` (`bridge_mod/STS2BridgeMod.csproj:3`).
Install per-user with the committed script (`docs/MOD_BUILD_GUIDE.md:15-19`):

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
./dotnet-install.ps1 -Channel 9.0
```

**PATH trap** (`docs/MOD_BUILD_GUIDE.md:79`): the SDK lands at
`%USERPROFILE%\.dotnet\dotnet.exe` and is NOT on PATH. Either prepend the
directory or call it explicitly:

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" --version   # verified 9.0.316 installed, 2026-07-24
```

### 5.2 Godot 4.5.1 Mono — exact version, exact path

- Download `Godot_v4.5.1-stable_mono_win64.zip` from the Godot 4.5.1-stable
  GitHub release and extract so that this exact file exists (hardcoded in
  `bridge_mod/STS2BridgeMod.csproj:29`):
  `C:/megadot/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe`
  (verified present 2026-07-24).
- **Version ceiling** (`STS2BridgeMod.csproj:22`): "Megadot is version 4.5.1,
  and the game won't load your .pck if the Godot version used is newer."
  Exactly 4.5.1 Mono — a newer Godot produces a `.pck` the game silently
  rejects (mod appears deployed but never loads).
- The csproj behavior around Godot, as committed (ground truth over docs):
  - `CheckDependencyPaths` (csproj:110-115) errors only on a missing game
    data dir; it deliberately skips the Godot check.
  - `GodotExport` (csproj:129-134) runs after **both** Build and Publish when
    `GodotPath` exists, with `ContinueOnError="WarnAndContinue"` — the export
    may report warnings/`exited with code -1` yet still write a valid `.pck`
    (`docs/MOD_BUILD_GUIDE.md:85`).
  - `GodotExportSkipped` (csproj:136-138) **errors the build** when
    `GodotPath` is missing. So despite `docs/MOD_BUILD_GUIDE.md:34,217`
    claiming plain `dotnet build` needs no Godot, the current csproj fails
    without it (the guide predates csproj:136-138; the DLL is still compiled
    and copied before the error fires). Trust the csproj.

### 5.3 Game install discovery

The csproj auto-locates Slay the Spire 2 (Windows, csproj:24-37):

1. Registry `HKCU\Software\Valve\Steam@SteamPath` + `\steamapps` — resolves
   to `c:/program files (x86)/steam` on this machine (verified 2026-07-24).
2. Fallback `C:/Program Files (x86)/Steam/steamapps` (csproj:31).
3. `Sts2DataDir = .../common/Slay the Spire 2/data_sts2_windows_x86_64`, from
   which it references `sts2.dll` and `0Harmony.dll` with `Private=false`
   (csproj:67-77). `sts2.dll` verified present 2026-07-24.

Non-default Steam library: `dotnet build -p:SteamLibraryPath="D:/SteamLibrary/steamapps"`
(`docs/MOD_BUILD_GUIDE.md:182-188`).

### 5.4 NuGet feed trap (fresh-machine builds)

`bridge_mod` pulls `Alchyr.Sts2.BaseLib` (Version `*`),
`Alchyr.Sts2.ModAnalyzers`, and `BepInEx.AssemblyPublicizer.MSBuild` 0.4.3
(csproj:79-83,103-108). BaseLib lives on a **custom feed**
(`https://nuget.pkg.github.com/Alchyr/index.json`) configured via
`nuget.config` — which is **gitignored** (`.gitignore:54`) and **absent from
the checkout as of 2026-07-24**. The last successful build (2026-07-23)
worked because the packages sit in the local NuGet cache. On a fresh machine,
restore will fail until you recreate `nuget.config` per
`docs/MOD_BUILD_GUIDE.md:190-207` (a GitHub PAT may be required for that
feed).

### 5.5 Minimal build sanity (toolchain-level)

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent\bridge_mod
& "$env:USERPROFILE\.dotnet\dotnet.exe" build
```

Expected: compile of the `Rl*.cs` handlers, "Copying .dll and manifests to
mods folder", BaseLib copy, then the Godot `.pck` export (possibly with
ignorable export warnings — csproj:133). Everything past this point —
verifying the mod actually loads ("Running Modded", `godot.log` markers,
`GDPC` magic-byte check on the `.pck`), the TCP protocol on port 9002, and
the live smoke-test that has never been done — is sts2-bridge-and-realgame
territory. As of 2026-07-24 the mod was last built and deployed 2026-07-23
and has never been live-smoke-tested; do not let a clean build imply a
working bridge.

## 6. Symptom → cause → fix

| Symptom | Cause | Fix |
|---|---|---|
| `torch.cuda.is_available()` is `False`, or version lacks `+cu128` | CPU wheel replaced the GPU build (`uv sync`, or a plain `pip install torch` upgrade) | Section 1.3 step 3; verify with step 4 |
| Training runs but is inexplicably slow | Same as above — SB3 silently uses CPU | Doctor script; section 1.3 step 3 |
| sb3/gymnasium/numpy versions suddenly older | `uv sync` ran (lock is stale) | Recreate venv (section 1.3); never `uv sync` (section 1.2) |
| Bare `python` opens Microsoft Store / does nothing | WindowsApps app-execution alias | Always `.venv\Scripts\python.exe` (section 1.4) |
| `ModuleNotFoundError: sts2_env` | Editable install missing in this venv | `.venv\Scripts\python.exe -m pip install -e ".[dev,train]"` |
| Multi-env training crashes with pickling/`spawn` errors, 1 env works | Non-module-level env factory or missing `__main__` guard | Section 2.1 |
| `dotnet` not recognized | Per-user SDK not on PATH | `& "$env:USERPROFILE\.dotnet\dotnet.exe"` or prepend to PATH (section 5.1) |
| Build error "Slay the Spire 2 data not found" | Steam library not at default/registry path | `-p:SteamLibraryPath=...` (section 5.3) |
| Build error "GodotPath is not configured or does not exist" | csproj:136-138 requires Godot for every build | Install Godot 4.5.1 Mono at the exact C:/megadot path (section 5.2) |
| Mod deployed but game never shows "Running Modded" | `.pck` exported with a newer Godot than 4.5.1 | Re-export with exactly 4.5.1 Mono (section 5.2); then sts2-bridge-and-realgame verify steps |
| NuGet restore fails on `Alchyr.Sts2.BaseLib` | `nuget.config` is gitignored and absent | Recreate per section 5.4 |
| Harmony patch count < 3/3 in game log after a game update | By-name parameter binding broke on a rename | sts2-bridge-and-realgame (game-update checklist); history in `docs/KNOWN_ISSUES.md:71-79` |
| Decompiled value disagrees with sim/tests | Read the wrong tree (e.g. pre-rework `decompiled/`) | Precedence rule, section 3 |
| `pytest` collects an unexpected count | HEAD moved, or env broken | Compare against the provenance command; if imports fail, doctor script |
| README says 408 tests / 131-dim obs / Discrete(61) | README is badly stale (predates rich env) | Trust code + this library; staleness map is owned by sts2-docs-and-writing |

## 7. Live-state caution (2026-07-24)

A concurrent session is actively advancing the training revamp. Observed
drift during the authoring of this skill alone: HEAD moved TWICE — 18a8059 →
fe25668 (committed the Phase 0 revamp and promoted
`docs/TRAINING_REVAMP_SPEC.json` from untracked to tracked) → 7af0a42 (web
play card previews) — and working-tree contents changed between every check.
**Run `git log --oneline -1` and
`git status --short` yourself before trusting any state claim in any doc,
including this one.** The environment facts in sections 1-5 (interpreter,
packages, toolchain paths) are machine-level and drift much more slowly than
repo state, but the provenance commands below re-verify each in seconds.

## 8. Provenance and maintenance

Every load-bearing fact above, with its one-line re-verification command.
All were run and confirmed on 2026-07-24 at HEAD fe25668. Run from
`C:\Users\motqu\GitHub\sts2-rl-agent` in PowerShell.

| Fact (as of 2026-07-24) | Re-verify with |
|---|---|
| Everything in sections 1, 3, 5 at once | `.venv\Scripts\python.exe .claude/skills/sts2-build-and-env/scripts/check_env.py` |
| Python 3.13.14 | `.venv\Scripts\python.exe --version` |
| venv created by `python -m venv` from Store Python | `Get-Content .venv\pyvenv.cfg` |
| torch 2.11.0+cu128, CUDA on RTX 4060 Laptop GPU | `.venv\Scripts\python.exe -c "import torch; print(torch.__version__, torch.cuda.is_available(), torch.cuda.get_device_name(0))"` |
| Package versions (sb3 2.9.0 etc.) | `.venv\Scripts\python.exe -m pip list` |
| uv.lock pins CPU torch 2.10.0, sb3 2.7.1, gymnasium 1.2.3, numpy 2.4.3 | `Select-String -Path uv.lock -Pattern 'name = "(torch\|stable-baselines3\|gymnasium\|numpy)"' -Context 0,2` |
| No repo doc mentions the cu128 index | `Get-ChildItem -Recurse -Filter *.md \| Select-String -Pattern "cu128\|download.pytorch"` (expect no gameplay-doc hits) |
| pyproject deps/extras/testpaths | `Get-Content pyproject.toml` |
| 5,276 tests collected in ~1 s | `.venv\Scripts\python.exe -m pytest --collect-only -q tests \| Select-Object -Last 1` |
| Tracked file counts 3300/3495/2590 for the three decompiled trees | `(git ls-files decompiled/ \| measure).Count` (and the other two dirs) |
| decompiled_mods subdirs = Act4Heart, ActsFromThePast, BaseLib, Downfall | `ls decompiled_mods` |
| Parity tooling still parses old `decompiled/` | `Get-Content sts2_env\cards\reference_static_metadata.py -TotalCount 20` and `Select-String -Path scripts\parity_reference_audit.py -Pattern "decompiled/"` |
| Campaign target = v0.109.0 + AFTP + Act4Heart | `Get-Content docs\TRAINING_REDESIGN.md -TotalCount 6` |
| Decompilation tool versions (ilspycmd 9.1.0, GDRE 2.4.0) | `Get-Content docs\DECOMPILATION_GUIDE.md -TotalCount 30` |
| `extracted_pck/` absent locally; nothing in Python reads it | `Test-Path extracted_pck` and `Get-ChildItem -Recurse -Filter *.py \| Select-String extracted_pck` |
| .gitignore entries (output/, *.log, nuget.config, extracted_pck/) | `Get-Content .gitignore` |
| dotnet 9.0.316 per-user | `& "$env:USERPROFILE\.dotnet\dotnet.exe" --version` |
| Godot 4.5.1 Mono at C:/megadot; version-ceiling comment | `Test-Path C:\megadot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe` and csproj:22,29 |
| csproj Godot targets (export on Build; error when missing) | `Get-Content bridge_mod\STS2BridgeMod.csproj \| Select-Object -Skip 109 -First 30` |
| Steam path via registry; sts2.dll present | `(Get-ItemProperty HKCU:\Software\Valve\Steam).SteamPath` then `Test-Path "<steam>\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll"` |
| nuget.config absent (gitignored) | `Test-Path bridge_mod\nuget.config; Test-Path nuget.config` |
| Spawn-pickling comment & `__main__` guard in trainer | `Select-String -Path scripts\train_necrobinder.py -Pattern "SubprocVecEnv\|__main__"` |
| HEAD / working-tree state | `git log --oneline -1; git status --short` |
| output/ contents (campaign artifacts) | `ls output` |

Maintenance rules for this skill:

- If package versions change deliberately, update section 1.1, the doctor
  script's `EXPECTED` dict, and the date stamps — in the same change.
- If a new decompiled tree lands for a game update, update section 3's table
  and precedence rule, and notify sts2-parity-discipline's owner content
  (the audit scripts' default paths in `scripts/parity_reference_audit.py`).
- If the bridge toolchain moves (new Godot ceiling, new .NET target), section
  5 and `bridge_mod/STS2BridgeMod.csproj` must agree — the csproj is ground
  truth, this skill is the explanation.
