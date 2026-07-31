"""Widen a checkpoint trained on a smaller card vocabulary.

Why this is needed
------------------
Four segments of the rich observation are indexed by card -- three combat pile
bags and the run-level deck bag -- each exactly ``NUM_CARD_IDS`` wide. Adding
a card to the CardId enum therefore widens the observation and every model
trained before the addition refuses to load:

    AssertionError: RichFeaturesExtractor expects obs size 4900, got 4884

What actually has to change is small. ``RichFeaturesExtractor`` never consumes
a bag as a raw wide vector: it projects each one through the shared card
embedding (``bag @ E[1:]``, policy.py:156), so the only parameter whose shape
depends on the vocabulary is ``card_embedding.weight``, held as
``(NUM_CARD_IDS + 1, card_embed_dim)`` with row 0 reserved as padding_idx.

Migrating is therefore: append one embedding row per new card. Every existing
row keeps pointing at the same card, which is only true because new CardId
members are appended rather than inserted -- ``enums.py`` marks that
append-only and ``test_card_vocabulary_stability.py`` pins it. Run this
against a checkpoint from a build with a REORDERED vocabulary and the weights
would silently describe the wrong cards, so that guard is load-bearing here.

New rows start at zero: the model has never seen these cards, and zero is what
padding_idx already means to every downstream consumer.

Usage
-----
    python -m scripts.migrate_checkpoint_vocab OLD.zip NEW.zip
"""

from __future__ import annotations

import argparse
import io
import sys
import zipfile
from pathlib import Path

import torch

from sts2_env.gym_env.rich_observation import NUM_CARD_IDS


#: Every extractor copy SB3 keeps (shared, policy-side, value-side).
EMBEDDING_SUFFIX = "card_embedding.weight"


def _rewrite_observation_space(raw: bytes) -> bytes:
    """Point the checkpoint's saved observation_space at the current width.

    SB3 rebuilds the policy from the space stored in the archive's ``data``
    entry, not from the env it is being loaded into, so widening the
    embeddings alone still trips RichFeaturesExtractor's size assert. The
    space is stored as base64 cloudpickle under a ``:serialized:`` key.
    """
    import base64
    import json

    import cloudpickle
    import gymnasium as gym
    import numpy as np

    from sts2_env.gym_env.rich_observation import (
        RICH_OBS_HIGH, RICH_OBS_LOW, RICH_OBS_SIZE,
    )

    data = json.loads(raw.decode("utf-8"))
    entry = data.get("observation_space")
    if not isinstance(entry, dict) or ":serialized:" not in entry:
        return raw

    old = cloudpickle.loads(base64.b64decode(entry[":serialized:"]))
    old_shape = getattr(old, "shape", None)
    if old_shape == (RICH_OBS_SIZE,):
        return raw

    # Rebuild with THIS BUILD's bounds, not invented ones. Hardcoding low=0.0
    # here produced a space that matched on shape and differed on bounds, and
    # SB3's check_for_correct_spaces rejected the warm start with
    # "Box(0.0, 591.0, (4900,)) != Box(-1.0, 591.0, (4900,))" -- the ID block
    # is a raw index range but other segments encode signed deltas, so the
    # floor is -1.0.
    new = gym.spaces.Box(
        low=float(RICH_OBS_LOW), high=float(RICH_OBS_HIGH),
        shape=(RICH_OBS_SIZE,), dtype=np.float32,
    )
    entry[":serialized:"] = base64.b64encode(cloudpickle.dumps(new)).decode("ascii")
    print(f"  observation_space: {old_shape} -> {new.shape}")
    return json.dumps(data).encode("utf-8")


def migrate(src: Path, dst: Path) -> int:
    with zipfile.ZipFile(src) as zf:
        names = zf.namelist()
        if "policy.pth" not in names:
            raise SystemExit(f"{src}: no policy.pth inside the checkpoint")
        payload = zf.read("policy.pth")
        other = {n: zf.read(n) for n in names if n != "policy.pth"}
    if "data" in other:
        other["data"] = _rewrite_observation_space(other["data"])

    state = torch.load(io.BytesIO(payload), map_location="cpu", weights_only=False)

    target_rows = NUM_CARD_IDS + 1  # +1 for padding_idx=0
    touched = []
    grown_shapes: set[tuple[int, int]] = set()
    for key, tensor in list(state.items()):
        if not key.endswith(EMBEDDING_SUFFIX):
            continue
        if not isinstance(tensor, torch.Tensor) or tensor.ndim != 2:
            continue
        rows = tensor.shape[0]
        if rows == target_rows:
            continue
        if rows > target_rows:
            raise SystemExit(
                f"{key}: checkpoint has {rows} embedding rows but this build "
                f"has room for {target_rows}. The vocabulary SHRANK, which "
                f"means card indices moved; this script cannot fix that."
            )
        grown = torch.cat(
            [tensor, tensor.new_zeros((target_rows - rows, tensor.shape[1]))],
            dim=0,
        )
        state[key] = grown
        touched.append((key, rows, target_rows))
        grown_shapes.add((rows, int(tensor.shape[1])))

    if not touched:
        print(f"{src}: already at {target_rows} embedding rows; nothing to do")
        return 0

    # THE OPTIMIZER STATE HAS TO GROW TOO.
    #
    # Adam keeps exp_avg / exp_avg_sq shaped like each parameter, so a
    # checkpoint whose embedding grew from 587 to 591 rows still carries
    # 587-row moments. The model then loads fine and dies on the first
    # update with "The size of tensor a (587) must match the size of tensor
    # b (591)" -- inside torch._foreach_lerp_, a long way from the cause.
    #
    # Matching on the exact old shape is safe here: (old_rows, embed_dim) is
    # unique to the embedding tables among this policy's parameters.
    if "policy.optimizer.pth" in other:
        other["policy.optimizer.pth"] = _grow_optimizer_state(
            other["policy.optimizer.pth"], grown_shapes)

    buf = io.BytesIO()
    torch.save(state, buf)
    dst.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED) as zf:
        for name, data in other.items():
            zf.writestr(name, data)
        zf.writestr("policy.pth", buf.getvalue())

    for key, before, after in touched:
        print(f"  {key}: {before} -> {after} rows")
    print(f"migrated {len(touched)} embedding table(s): {src} -> {dst}")
    return len(touched)


def _grow_optimizer_state(raw: bytes, grown_shapes: set) -> bytes:
    """Row-pad Adam moments that match a grown parameter's OLD shape."""
    if not grown_shapes:
        return raw
    opt = torch.load(io.BytesIO(raw), map_location="cpu", weights_only=False)
    changed = 0
    for entry in (opt.get("state") or {}).values():
        if not isinstance(entry, dict):
            continue
        for key, value in list(entry.items()):
            if not isinstance(value, torch.Tensor) or value.ndim != 2:
                continue
            shape = (int(value.shape[0]), int(value.shape[1]))
            if shape not in grown_shapes:
                continue
            pad = NUM_CARD_IDS + 1 - shape[0]
            entry[key] = torch.cat(
                [value, value.new_zeros((pad, shape[1]))], dim=0)
            changed += 1
    if changed:
        print(f"  optimizer: grew {changed} moment tensor(s)")
    buf = io.BytesIO()
    torch.save(opt, buf)
    return buf.getvalue()


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("src", type=Path)
    parser.add_argument("dst", type=Path)
    args = parser.parse_args(argv)
    migrate(args.src, args.dst)
    return 0


if __name__ == "__main__":
    sys.exit(main())
