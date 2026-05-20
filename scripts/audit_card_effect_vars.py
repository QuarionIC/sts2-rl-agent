#!/usr/bin/env python3
"""Report card effects that read effect_vars keys absent from their factories."""

from __future__ import annotations

import ast
import importlib
import inspect
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from sts2_env.cards.factory import create_card
from sts2_env.cards.registry import _CARD_EFFECTS


CARD_MODULES = (
    "sts2_env.cards.ironclad_basic",
    "sts2_env.cards.ironclad",
    "sts2_env.cards.silent",
    "sts2_env.cards.defect",
    "sts2_env.cards.necrobinder",
    "sts2_env.cards.regent",
    "sts2_env.cards.colorless",
    "sts2_env.cards.status",
)


def _effect_var_keys_read(func) -> set[str]:
    tree = ast.parse(inspect.getsource(func))
    keys: set[str] = set()
    for node in ast.walk(tree):
        if (
            isinstance(node, ast.Call)
            and isinstance(node.func, ast.Attribute)
            and node.func.attr == "get"
            and isinstance(node.func.value, ast.Attribute)
            and node.func.value.attr == "effect_vars"
            and node.args
            and isinstance(node.args[0], ast.Constant)
            and isinstance(node.args[0].value, str)
        ):
            keys.add(node.args[0].value)
        elif (
            isinstance(node, ast.Subscript)
            and isinstance(node.value, ast.Attribute)
            and node.value.attr == "effect_vars"
            and isinstance(node.slice, ast.Constant)
            and isinstance(node.slice.value, str)
        ):
            keys.add(node.slice.value)
    return keys


def main() -> int:
    for module_name in CARD_MODULES:
        importlib.import_module(module_name)

    mismatches: list[tuple[str, list[str], list[str], list[str]]] = []
    for card_id, func in sorted(_CARD_EFFECTS.items(), key=lambda item: item[0].name):
        read_keys = _effect_var_keys_read(func)
        if not read_keys:
            continue
        try:
            base_card = create_card(card_id, upgraded=False)
            upgraded_card = create_card(card_id, upgraded=True)
        except Exception:
            continue
        available_keys = set(base_card.effect_vars) | set(upgraded_card.effect_vars)
        missing_keys = sorted(read_keys - available_keys)
        if missing_keys:
            mismatches.append(
                (
                    card_id.name,
                    sorted(read_keys),
                    sorted(available_keys),
                    missing_keys,
                )
            )

    for card_name, read_keys, available_keys, missing_keys in mismatches:
        print(
            f"{card_name}: missing={missing_keys} "
            f"read={read_keys} available={available_keys}"
        )

    if mismatches:
        print(f"{len(mismatches)} card effect var mismatch(es) found")
        return 1
    print("card effect var audit passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
