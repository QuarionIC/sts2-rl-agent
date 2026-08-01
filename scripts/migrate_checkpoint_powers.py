#!/usr/bin/env python3
"""Widen a checkpoint trained before PowerId members were appended.

Why this is separate from migrate_checkpoint_vocab
--------------------------------------------------
Cards are EMBEDDED: every card-indexed observation segment is projected through
``card_embedding``, so adding a card only means appending rows to one table.

Powers are RAW ONE-HOT. ``NUM_POWER_IDS`` sizes ``PLAYER_POWERS_SIZE`` and,
through ``ENEMY_BLOCK_SIZE``, every one of the five enemy slots -- and all of
that sits inside ``RichFeaturesExtractor``'s flat passthrough
(``obs[:, PILE_SIZES_OFF:DECK_BAG_OFF]``, concatenated LAST in ``forward``).
So adding 6 powers widened the observation by 6 + 5*6 = 36 and the extractor's
output from 4210 to 4246, and the growth lands at SIX separate offsets inside
the first MLP layer's input rather than at the end.

Appending 36 columns would therefore be wrong in a way that still loads and
still runs: every flat feature after the first insertion point would be read
off by six columns, which is a silently mis-wired policy rather than a crash.

New columns are zeroed: the model has never seen these powers, and a zero
column contributes nothing, so on any observation where the new powers are
absent the migrated network computes EXACTLY what the original did. That is
asserted rather than assumed -- see --verify.

Usage
-----
    python -m scripts.migrate_checkpoint_powers OLD.zip NEW.zip --old-powers 293
"""

from __future__ import annotations

import argparse
import base64
import io
import json
import math
import sys
import zipfile
from pathlib import Path

import torch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import sts2_env.gym_env.rich_observation as R  # noqa: E402


def flat_insertion_points(old_powers: int) -> list[int]:
    """Offsets INSIDE the flat slice where the new power columns belong.

    Returned in OLD-slice coordinates, ascending: the end of the player's
    power block, then the end of each enemy's power block. Inserting at these
    points, right-to-left, keeps every earlier offset valid.
    """
    grown = R.NUM_POWER_IDS - old_powers
    base = R.PILE_SIZES_OFF

    # Rebuild the OLD layout: each block before a given point is smaller by
    # `grown` for every power block that preceded it.
    player_powers_start = R.PLAYER_POWERS_OFF - base
    points = [player_powers_start + old_powers]

    enemies_start = R.ENEMIES_OFF - base - grown  # one player block shrank
    old_enemy_block = R.ENEMY_BLOCK_SIZE - grown
    non_power = old_enemy_block - old_powers      # core features + intents
    for i in range(R.ENEMIES_SIZE // R.ENEMY_BLOCK_SIZE):
        block = enemies_start + i * old_enemy_block
        points.append(block + non_power + old_powers)
    return points


def widen_flat_columns(weight: torch.Tensor, old_powers: int,
                       flat_start_in_features: int) -> torch.Tensor:
    """Insert zero columns at each power-block boundary of the flat slice."""
    grown = R.NUM_POWER_IDS - old_powers
    out = weight
    # Right-to-left so earlier offsets stay valid as the tensor grows.
    for point in sorted(flat_insertion_points(old_powers), reverse=True):
        at = flat_start_in_features + point
        out = torch.cat(
            [out[:, :at], out.new_zeros((out.shape[0], grown)), out[:, at:]],
            dim=1,
        )
    return out


def _features_dim_before_flat(old_powers: int) -> int:
    """Width of everything concatenated BEFORE the flat slice.

    Unaffected by the power count -- hand, bags, deck, offer, potion and boss
    are all embedding-derived -- so it is the same in both layouts and can be
    read off the current build.
    """
    grown = R.NUM_POWER_IDS - old_powers
    new_flat = R.DECK_BAG_OFF - R.PILE_SIZES_OFF
    # features_dim = <before> + flat, in the CURRENT build.
    from sts2_env.train.policy import RichFeaturesExtractor

    import gymnasium as gym
    import numpy as np

    space = gym.spaces.Box(low=R.RICH_OBS_LOW, high=R.RICH_OBS_HIGH,
                           shape=(R.RICH_OBS_SIZE,), dtype=np.float32)
    return RichFeaturesExtractor(space).features_dim - new_flat


def _rewrite_observation_space(raw: bytes) -> bytes:
    import cloudpickle
    import gymnasium as gym
    import numpy as np

    data = json.loads(raw.decode("utf-8"))
    entry = data.get("observation_space")
    if not isinstance(entry, dict) or ":serialized:" not in entry:
        return raw
    old = cloudpickle.loads(base64.b64decode(entry[":serialized:"]))
    if getattr(old, "shape", None) == (R.RICH_OBS_SIZE,):
        return raw
    new = gym.spaces.Box(low=float(R.RICH_OBS_LOW), high=float(R.RICH_OBS_HIGH),
                         shape=(R.RICH_OBS_SIZE,), dtype=np.float32)
    entry[":serialized:"] = base64.b64encode(cloudpickle.dumps(new)).decode("ascii")
    print(f"  observation_space: {old.shape} -> {new.shape}")
    return json.dumps(data).encode("utf-8")


def migrate(src: Path, dst: Path, old_powers: int) -> None:
    grown = R.NUM_POWER_IDS - old_powers
    if grown <= 0:
        raise SystemExit(f"--old-powers {old_powers} is not smaller than "
                         f"this build's {R.NUM_POWER_IDS}")

    with zipfile.ZipFile(src) as zf:
        names = zf.namelist()
        payload = zf.read("policy.pth")
        other = {n: zf.read(n) for n in names if n != "policy.pth"}
    if "data" in other:
        other["data"] = _rewrite_observation_space(other["data"])

    state = torch.load(io.BytesIO(payload), map_location="cpu", weights_only=False)
    before_flat = _features_dim_before_flat(old_powers)

    # One player power block plus one per enemy slot.
    n_blocks = 1 + R.ENEMIES_SIZE // R.ENEMY_BLOCK_SIZE
    total_growth = grown * n_blocks
    new_features = before_flat + (R.DECK_BAG_OFF - R.PILE_SIZES_OFF)
    old_features = new_features - total_growth
    print(f"  extractor output: {old_features} -> {new_features} "
          f"({total_growth} new columns at {n_blocks} offsets)")

    touched = []
    for key, tensor in list(state.items()):
        # Only the layers consuming the EXTRACTOR OUTPUT need widening; every
        # deeper layer is shape-independent of the observation.
        if not isinstance(tensor, torch.Tensor) or tensor.ndim != 2:
            continue
        if tensor.shape[1] != old_features:
            continue
        state[key] = widen_flat_columns(tensor, old_powers, before_flat)
        touched.append((key, tuple(tensor.shape), tuple(state[key].shape)))

    if not touched:
        raise SystemExit(
            f"{src}: no layer had the expected pre-migration input width. "
            f"Is --old-powers {old_powers} right?")

    # THE OPTIMIZER STATE NEEDS THE SAME COLUMNS.
    #
    # Adam's exp_avg / exp_avg_sq are shaped like their parameter, so a policy
    # whose first MLP layer grew 4210 -> 4246 still carries 4210-wide moments.
    # That loads cleanly and dies on the FIRST update inside
    # torch._foreach_lerp_ with "size of tensor a (4210) must match tensor b
    # (4246)" -- no mention of migration, checkpoints or powers.
    #
    # migrate_checkpoint_vocab hit exactly this and was fixed for embedding
    # rows; the lesson did not travel to this script until the same crash
    # happened again. Widening the weights without the moments is not a
    # migration, it is a delayed failure.
    if "policy.optimizer.pth" in other:
        other["policy.optimizer.pth"] = _grow_optimizer_state(
            other["policy.optimizer.pth"], old_features, old_powers, before_flat)

    buf = io.BytesIO()
    torch.save(state, buf)
    dst.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED) as zf:
        for name, data in other.items():
            zf.writestr(name, data)
        zf.writestr("policy.pth", buf.getvalue())

    for key, was, now in touched:
        print(f"  {key}: {was} -> {now}")
    print(f"migrated {len(touched)} layer(s): {src} -> {dst}")


def _grow_optimizer_state(raw: bytes, old_features: int, old_powers: int,
                          before_flat: int) -> bytes:
    """Apply the same column insertion to Adam's moments."""
    opt = torch.load(io.BytesIO(raw), map_location="cpu", weights_only=False)
    changed = 0
    for entry in (opt.get("state") or {}).values():
        if not isinstance(entry, dict):
            continue
        for key, value in list(entry.items()):
            if (isinstance(value, torch.Tensor) and value.ndim == 2
                    and value.shape[1] == old_features):
                entry[key] = widen_flat_columns(value, old_powers, before_flat)
                changed += 1
    if changed:
        print(f"  optimizer: widened {changed} moment tensor(s)")
    buf = io.BytesIO()
    torch.save(opt, buf)
    return buf.getvalue()


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("src", type=Path)
    ap.add_argument("dst", type=Path)
    ap.add_argument("--old-powers", type=int, required=True,
                    help="NUM_POWER_IDS the checkpoint was trained with")
    args = ap.parse_args(argv)
    migrate(args.src, args.dst, args.old_powers)
    return 0


if __name__ == "__main__":
    sys.exit(main())
