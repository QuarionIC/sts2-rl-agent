"""Small local web UI for playing a full STS2 run."""

from __future__ import annotations

import argparse
import json
import random
import threading
import webbrowser
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any
from urllib.parse import parse_qs, urlparse

import sts2_env.events  # noqa: F401
import sts2_env.potions.effects  # noqa: F401

from sts2_env.content import card_description, card_preview, potion_description, power_description
from sts2_env.cli.play_run import (
    CHARACTERS,
    DEFAULT_CHARACTER_INDEX,
    PHASE_LABELS,
    ROOM_LABELS,
    describe_action,
    describe_card,
    describe_enemy_intents,
    display_name,
    display_text,
)
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import PotionTargetType, TargetType
from sts2_env.core.rng import INT_MAX
from sts2_env.run.reward_objects import CardBundlesReward, CardReward, PotionReward, RelicReward
from sts2_env.run.run_manager import RunManager
from sts2_env.run.shop import is_shop_entry_available


STATIC_DIR = Path(__file__).resolve().parent / "static"
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 8765
DEFAULT_ASCENSION = 0
STATE_SESSION_ID = "default"
START_PHASE = "START"
START_PHASE_LABEL = "Start"
START_SCREEN_TITLE = "Start Run"
START_NOTICE = "Ready to start."


class RunSession:
    def __init__(self) -> None:
        self.lock = threading.Lock()
        self.mgr: RunManager | None = None
        self.seed: int | None = None
        self.character = CHARACTERS[DEFAULT_CHARACTER_INDEX]
        self.ascension = 0
        self.last_description = ""

    def start(self, *, character: str, seed: int | None, ascension: int = 0, skip_neow: bool = False) -> dict:
        with self.lock:
            self.seed = seed if seed is not None else random.randint(0, INT_MAX)
            self.character = character if character in CHARACTERS else CHARACTERS[DEFAULT_CHARACTER_INDEX]
            self.ascension = ascension
            self.last_description = ""
            self.mgr = RunManager(
                seed=self.seed,
                character_id=self.character,
                ascension_level=self.ascension,
                start_with_neow=not skip_neow,
            )
            return self.state()

    def take_action(self, action_index: int) -> dict:
        with self.lock:
            if self.mgr is None:
                self.last_description = "Start a new run first."
                return self.state()
            mgr = self.mgr
            actions = mgr.get_available_actions()
            if action_index < 0 or action_index >= len(actions):
                self.last_description = "Invalid action."
                return self.state()
            result = mgr.take_action(actions[action_index])
            self.last_description = result.get("description", "")
            return self.state()

    def state(self) -> dict:
        if self.mgr is None:
            return serialize_start_state(
                seed=self.seed,
                character=self.character,
                ascension=self.ascension,
                last_description=self.last_description,
            )
        mgr = self.mgr
        actions = mgr.get_available_actions()
        return serialize_run(
            mgr,
            actions,
            seed=self.seed,
            character=self.character,
            ascension=self.ascension,
            last_description=self.last_description,
        )


def serialize_start_state(
    *,
    seed: int | None,
    character: str,
    ascension: int,
    last_description: str,
) -> dict:
    return {
        "session_id": STATE_SESSION_ID,
        "seed": seed,
        "character": character,
        "ascension": ascension,
        "phase": START_PHASE,
        "phase_label": START_PHASE_LABEL,
        "act": "-",
        "floor": "-",
        "hp": "-",
        "gold": "-",
        "deck_size": "-",
        "relics": [],
        "is_over": False,
        "player_won": False,
        "last_description": last_description or START_NOTICE,
        "actions": [],
        "screen": {"type": "start", "title": START_SCREEN_TITLE, "items": []},
        "inventory": {"deck": [], "relics": [], "potions": []},
    }


def serialize_run(
    mgr: RunManager,
    actions: list[dict[str, Any]],
    *,
    seed: int | None,
    character: str,
    ascension: int,
    last_description: str,
) -> dict:
    summary = mgr.summary()
    combat = mgr.get_combat_state()
    return {
        "session_id": STATE_SESSION_ID,
        "seed": seed,
        "character": character,
        "ascension": ascension,
        "phase": summary["phase"],
        "phase_label": PHASE_LABELS.get(summary["phase"], summary["phase"]),
        "act": summary["act"] + 1,
        "floor": summary["floor"],
        "hp": summary["hp"],
        "gold": summary["gold"],
        "deck_size": summary["deck_size"],
        "relics": [display_name(relic) for relic in summary["relics"]],
        "is_over": summary["is_over"],
        "player_won": summary["player_won"],
        "last_description": last_description,
        "actions": [
            {"index": index, "label": describe_action(action), "kind": action.get("action", "")}
            for index, action in enumerate(actions)
        ],
        "screen": _screen(mgr, actions, combat),
        # Read-only snapshot of the act map so the UI can show it at any time
        # (e.g. during combat) without advancing the run. During the MAP phase
        # "screen" already holds the interactive map; this mirrors it and, in
        # other phases, has no "move" actions so it renders purely as an
        # overview.
        "map_overview": _map_screen(mgr, actions),
        "inventory": _inventory(mgr),
    }


def _screen(mgr: RunManager, actions: list[dict[str, Any]], combat: CombatState | None) -> dict:
    if any(action.get("action") in {"choose", "confirm_choice"} for action in actions):
        return _choice_screen(actions)
    if combat is not None:
        return _combat_screen(combat, actions)
    phase = mgr.phase
    if phase == RunManager.PHASE_MAP_CHOICE:
        return _map_screen(mgr, actions)
    if phase == RunManager.PHASE_CARD_REWARD:
        return _reward_screen(mgr, actions)
    if phase == RunManager.PHASE_BOSS_RELIC:
        return {
            "type": "boss_relic",
            "title": "Boss Relics",
            "items": [
                {
                    "name": display_name(relic_id),
                    "action_index": _find_action_index(actions, action="pick_relic", index=index),
                }
                for index, relic_id in enumerate(getattr(mgr, "_boss_relics", []))
            ],
        }
    if phase == RunManager.PHASE_SHOP:
        return _shop_screen(mgr, actions)
    if phase == RunManager.PHASE_REST_SITE:
        return _rest_screen(mgr, actions)
    if phase == RunManager.PHASE_EVENT:
        return _event_screen(mgr, actions)
    if phase == RunManager.PHASE_TREASURE:
        return _treasure_screen(mgr, actions)
    return {"type": "message", "title": PHASE_LABELS.get(phase, phase), "items": []}


def _choice_screen(actions: list[dict[str, Any]]) -> dict:
    prompt = next(
        (action.get("prompt") for action in actions if action.get("prompt")),
        "Choose",
    )
    items = []
    for index, action in enumerate(actions):
        action_type = action.get("action")
        if action_type == "confirm_choice":
            selected_count = action.get("selected_count", 0)
            items.append({
                "name": "Confirm" if selected_count else "Skip",
                "description": f"{selected_count} selected",
                "selected": False,
                "action_index": index,
            })
        elif action_type == "choose":
            items.append({
                "name": display_name(action.get("card_id", action.get("index"))),
                "description": action.get("source_pile", ""),
                "selected": bool(action.get("selected")),
                "action_index": index,
            })
    return {
        "type": "choice",
        "title": display_text(str(prompt)),
        "items": items,
    }


def _power_entries(powers: Any) -> list[dict[str, str]]:
    """Serialize a creature's powers as ``{text, desc}`` pairs.

    ``text`` keeps the existing display string (e.g. ``"VULNERABLE(2)"``) so the
    UI looks unchanged; ``desc`` is a hover tooltip explaining the effect.
    """
    return [
        {
            "text": str(power),
            "desc": power_description(
                getattr(power, "power_id", None),
                getattr(power, "amount", None),
            ),
        }
        for power in powers.values()
    ]


def _combat_screen(combat: CombatState, actions: list[dict[str, Any]]) -> dict:
    player = combat.primary_player
    end_turn_action_index = _find_action_index(actions, action="end_turn")
    enemies = []
    for index, enemy in enumerate(combat.enemies):
        ai = combat.enemy_ais.get(enemy.combat_id)
        enemies.append({
            "index": index,
            "name": display_name(enemy.monster_id),
            "hp": f"{enemy.current_hp}/{enemy.max_hp}",
            "block": enemy.block,
            "intent": describe_enemy_intents(ai),
            "alive": enemy.is_alive,
            "powers": _power_entries(enemy.powers),
        })
    allies = []
    for ally in combat.allies:
        if ally is None:
            continue
        allies.append({
            "name": "Osty" if getattr(ally, "is_osty", False) else display_name(getattr(ally, "monster_id", "Ally")),
            "hp": f"{ally.current_hp}/{ally.max_hp}",
            "block": ally.block,
            "alive": ally.is_alive,
            "powers": _power_entries(ally.powers),
        })
    return {
        "type": "combat",
        "title": "Combat",
        "round": combat.round_number,
        "energy": combat.energy,
        "end_turn_action_index": end_turn_action_index,
        "player": {
            "hp": f"{player.current_hp}/{player.max_hp}",
            "block": player.block,
            "powers": _power_entries(player.powers),
        },
        "enemies": enemies,
        "allies": allies,
        "hand": [_hand_card_entry(combat, player, actions, index, card)
                 for index, card in enumerate(combat.hand)],
        "potions": [
            {
                "index": index,
                "name": display_name(potion.potion_id),
                "desc": potion_description(potion),
                "targeted": potion.target_type == PotionTargetType.ANY_ENEMY,
                "actions": _combat_potion_actions(actions, index),
            }
            for index, potion in enumerate(combat.potions)
            if potion is not None
        ],
        "piles": {
            "draw": len(combat.draw_pile),
            "discard": len(combat.discard_pile),
            "exhaust": len(combat.exhaust_pile),
            # Contents so the player can inspect what remains. The draw pile
            # order is hidden in-game, so its cards are sorted by name (like
            # the real game's draw-pile view); discard/exhaust keep pile order.
            "draw_cards": sorted(describe_card(card) for card in combat.draw_pile),
            "discard_cards": [describe_card(card) for card in combat.discard_pile],
            "exhaust_cards": [describe_card(card) for card in combat.exhaust_pile],
        },
    }


def _hand_card_entry(
    combat: CombatState,
    player: Any,
    actions: list[dict[str, Any]],
    index: int,
    card: Any,
) -> dict[str, Any]:
    """Serialize one hand card with live effective values (damage/block/cost)."""
    preview = card_preview(card, combat, player)
    enemy_names = {
        enemy_index: display_name(enemy.monster_id)
        for enemy_index, enemy in enumerate(combat.enemies)
    }
    return {
        "index": index,
        "name": _hand_card_name(card, preview),
        "label": _hand_card_label(card, preview),
        "preview": preview,
        "desc": _hand_card_desc(card, preview, enemy_names),
        "playable": combat.can_play_card(card),
        "targeted": card.target_type_for(player) in {TargetType.ANY_ENEMY, TargetType.ANY_ALLY},
        "actions": _combat_card_actions(actions, index, preview),
    }


def _damage_display(preview: dict[str, Any]) -> str | None:
    """Effective damage as a compact string: "9", or "9-13" when it differs
    per enemy; falls back to the base value when no preview exists."""
    values = sorted({entry["value"] for entry in preview["eff_damage_by_target"]})
    if values:
        return str(values[0]) if len(values) == 1 else f"{values[0]}-{values[-1]}"
    if preview["base_damage"] is not None:
        return str(preview["base_damage"])
    return None


def _mod_direction(up: bool, down: bool) -> str | None:
    if up and down:
        return "mixed"
    if up:
        return "up"
    if down:
        return "down"
    return None


def _hand_card_label(card: Any, preview: dict[str, Any]) -> dict[str, Any]:
    """Structured label pieces so the frontend can color modified values."""
    flags = preview["flags"]
    if card.has_energy_cost_x:
        cost = "X"
    elif preview["eff_cost"] is not None:
        cost = str(preview["eff_cost"])
    else:
        cost = str(card.cost)
    block = preview["eff_block"] if preview["eff_block"] is not None else card.base_block
    return {
        "title": display_name(card.card_id.name),
        "upgraded": card.upgraded,
        "cost": cost,
        "cost_mod": _mod_direction(flags.get("cost_up", False), flags.get("cost_down", False)),
        "damage": _damage_display(preview),
        "damage_mod": _mod_direction(flags.get("damage_up", False), flags.get("damage_down", False)),
        "block": str(block) if block is not None else None,
        "block_mod": _mod_direction(flags.get("block_up", False), flags.get("block_down", False)),
    }


def _hand_card_name(card: Any, preview: dict[str, Any]) -> str:
    """Flat hand-card label in the classic ``Name(1E 9dmg 3blk+)`` shape,
    but showing the values the card would produce *right now*."""
    label = _hand_card_label(card, preview)
    parts = [f"{label['title']}({label['cost']}E"]
    if label["damage"] is not None:
        parts.append(f" {label['damage']}dmg")
    if label["block"] is not None:
        parts.append(f" {label['block']}blk")
    if label["upgraded"]:
        parts.append("+")
    return "".join(parts) + ")"


def _hand_card_desc(card: Any, preview: dict[str, Any], enemy_names: dict[int, str]) -> str:
    """Tooltip text with live numbers woven in, e.g. "Deal 9 (base 6) damage."

    Unmodified cards keep the clean base description."""
    desc = card_description(card)
    flags = preview["flags"]

    def swap(text: str, old: str, new: str, fallback_line: str) -> str:
        if old in text:
            return text.replace(old, new, 1)
        return f"{text}\n{fallback_line}"

    if flags.get("cost_up") or flags.get("cost_down"):
        base_cost, eff_cost = preview["base_cost"], preview["eff_cost"]
        desc = swap(
            desc,
            f"{base_cost} energy",
            f"{eff_cost} energy (base {base_cost})",
            f"Costs {eff_cost} energy right now (base {base_cost}).",
        )
    if flags.get("damage_up") or flags.get("damage_down"):
        base_damage = preview["base_damage"]
        eff_text = _damage_display(preview)
        desc = swap(
            desc,
            f"Deal {base_damage} damage",
            f"Deal {eff_text} (base {base_damage}) damage",
            f"Deals {eff_text} damage right now (base {base_damage}).",
        )
        per_target = {entry["value"] for entry in preview["eff_damage_by_target"]}
        if len(per_target) > 1:
            names = [
                enemy_names.get(entry["enemy_index"], "Enemy " + str(entry["enemy_index"]))
                for entry in preview["eff_damage_by_target"]
            ]
            desc += "\nBy target: " + ", ".join(
                f"{name} {entry['value']}"
                for name, entry in zip(names, preview["eff_damage_by_target"])
            )
    if flags.get("block_up") or flags.get("block_down"):
        base_block, eff_block = preview["base_block"], preview["eff_block"]
        desc = swap(
            desc,
            f"Gain {base_block} Block",
            f"Gain {eff_block} (base {base_block}) Block",
            f"Gains {eff_block} Block right now (base {base_block}).",
        )
    return desc


def _combat_card_actions(
    actions: list[dict[str, Any]],
    hand_index: int,
    preview: dict[str, Any] | None = None,
) -> list[dict[str, Any]]:
    damage_by_enemy = {}
    if preview is not None and preview["targeted"]:
        damage_by_enemy = {
            entry["enemy_index"]: entry["value"]
            for entry in preview["eff_damage_by_target"]
        }
    card_actions = []
    for index, action in enumerate(actions):
        if action.get("action") != "play_card" or action.get("hand_index") != hand_index:
            continue
        target_name = action.get("target_name")
        target = display_name(target_name) if target_name else "Play"
        target_index = action.get("target_index")
        if target_index in damage_by_enemy:
            target += f" ({damage_by_enemy[target_index]})"
        card_actions.append({
            "action_index": index,
            "target": target,
        })
    return card_actions


def _combat_potion_actions(actions: list[dict[str, Any]], slot_index: int) -> list[dict[str, Any]]:
    potion_actions = []
    for index, action in enumerate(actions):
        if action.get("action") != "use_potion" or action.get("slot_index") != slot_index:
            continue
        target_name = action.get("target_name")
        potion_actions.append({
            "action_index": index,
            "target": display_name(target_name) if target_name else "Use",
        })
    return potion_actions


#: In-game act names (from decompiled act classes: Overgrowth/Hive/Glory/Underdocks).
import re as _re

# Fallback act names by slot index, used only if the selected act has no
# act_id set. The real name comes from the selected ActConfig.act_id, since a
# slot can now be filled by an alternate act (e.g. Act 1 = Overgrowth /
# Underdocks / Exordium).
ACT_NAMES = {0: "Overgrowth", 1: "Hive", 2: "Glory", 3: "The Ending"}


def _act_display_name(mgr: RunManager) -> str:
    act = mgr.run_state.current_act
    act_id = getattr(act, "act_id", "") or ""
    if act_id:
        # "TheCity" -> "The City", "TheBeyond" -> "The Beyond".
        return _re.sub(r"(?<=[a-z])(?=[A-Z])", " ", act_id)
    return ACT_NAMES.get(act.act_index, f"Act {act.act_index + 1}")


def _boss_name(mgr: RunManager) -> str | None:
    """Human-readable name of this act's boss, rolled when the map was generated."""
    setup_fn = mgr.act_boss_setup
    if setup_fn is None:
        return None
    name = setup_fn.__name__
    name = name.removeprefix("setup_").removesuffix("_boss")
    return display_name(name)


def _map_screen(mgr: RunManager, actions: list[dict[str, Any]]) -> dict:
    run_state = mgr.run_state
    act_map = run_state.map
    if act_map is None:
        return {"type": "map", "title": "Map", "columns": [], "rows": [], "paths": []}
    visited = set(run_state.visited_map_coords)
    act_index = run_state.current_act_index
    boss_label = _boss_name(mgr)
    reachable = {
        tuple(action["coord"]): index
        for index, action in enumerate(actions)
        if action.get("action") == "move"
    }
    rows = []
    all_points = act_map.all_points()
    max_row = max((point.row for point in all_points), default=0)
    columns = sorted({point.col for point in all_points})
    for row_index in range(max_row, 0, -1):
        row = []
        for point in act_map.get_row(row_index):
            coord = (point.col, point.row)
            is_boss = act_map.boss_point is not None and point is act_map.boss_point
            row.append({
                "coord": coord,
                "label": ROOM_LABELS.get(point.point_type.name, display_name(point.point_type.name)),
                "reachable": coord in reachable,
                "action_index": reachable.get(coord),
                "visited": point.coord in visited,
                "next": [(child.col, child.row) for child in point.children],
                "boss": boss_label if is_boss else None,
            })
        if row:
            rows.append(row)
    paths = []
    for coord in run_state.get_available_next_coords():
        point = act_map.get_point(coord)
        if point is None:
            continue
        paths.append({
            "coord": (point.col, point.row),
            "label": ROOM_LABELS.get(point.point_type.name, display_name(point.point_type.name)),
            "next": [
                {
                    "coord": (child.col, child.row),
                    "label": ROOM_LABELS.get(child.point_type.name, display_name(child.point_type.name)),
                }
                for child in sorted(point.children, key=lambda child: child.col)
            ],
        })
    current = None
    if run_state.visited_map_coords:
        coord = run_state.visited_map_coords[-1]
        current = (coord.col, coord.row)
    return {
        "type": "map",
        "title": "Map",
        "current": current,
        "columns": columns,
        "rows": rows,
        "paths": paths,
        "act_name": _act_display_name(mgr),
        "act_index": act_index,
        "bosses": [boss_label] if boss_label else [],
    }


def _reward_screen(mgr: RunManager, actions: list[dict[str, Any]]) -> dict:
    reward = getattr(mgr, "_current_reward", None)
    alternative_items = _reward_alternative_items(actions)
    if isinstance(reward, CardReward):
        return {
            "type": "reward",
            "title": "Card Reward",
            "items": alternative_items + [
                {
                    "name": describe_card(card),
                    "action_index": _find_action_index(actions, action="pick_card", index=index),
                }
                for index, card in enumerate(getattr(mgr, "_offered_cards", []))
            ],
        }
    if isinstance(reward, CardBundlesReward):
        return {
            "type": "reward",
            "title": "Card Bundle",
            "items": alternative_items + [
                {
                    "name": ", ".join(describe_card(card) for card in bundle),
                    "action_index": _find_action_index(actions, action="pick_card_bundle", index=index),
                }
                for index, bundle in enumerate(getattr(mgr, "_offered_card_bundles", []))
            ],
        }
    if isinstance(reward, PotionReward):
        potion = getattr(mgr, "_offered_potion", None)
        return {
            "type": "reward",
            "title": "Potion Reward",
            "items": alternative_items + [
                {
                    "name": display_name(potion.potion_id if potion else reward.potion_id),
                    "action_index": _find_action_index(actions, action="pick_potion"),
                },
            ],
        }
    if isinstance(reward, RelicReward):
        return {
            "type": "reward",
            "title": "Relic Reward",
            "items": alternative_items + [
                {
                    "name": display_name(reward.relic_id),
                    "action_index": _find_action_index(actions, action="pick_relic_reward"),
                },
            ],
        }
    return {"type": "reward", "title": "Reward", "items": []}


def _reward_alternative_items(actions: list[dict[str, Any]]) -> list[dict[str, Any]]:
    alternatives = []
    labels = {
        "skip": "Skip",
        "skip_potion": "Skip potion",
        "skip_relic": "Skip relic",
        "reroll_card_reward": "Reroll",
        "sacrifice_card_reward": "Sacrifice",
    }
    for index, action in enumerate(actions):
        action_type = action.get("action")
        if action_type in labels:
            alternatives.append({"name": labels[action_type], "action_index": index})
    return alternatives


def _shop_screen(mgr: RunManager, actions: list[dict[str, Any]]) -> dict:
    inv = getattr(mgr, "_shop_inventory", None)
    if inv is None:
        return {"type": "shop", "title": "Shop", "sections": []}
    return {
        "type": "shop",
        "title": "Shop",
        "sections": [
            {
                "title": "Exit",
                "items": [
                    {
                        "name": "Leave shop",
                        "action_index": _find_action_index(actions, action="leave_shop"),
                    },
                ],
            },
            {
                "title": "Cards",
                "items": [
                    _shop_item(
                        describe_card(entry.card) if entry.card is not None else display_name(entry.card_id),
                        entry.price,
                        entry,
                        action_index=_find_action_index(actions, action="buy_card", index=index),
                    )
                    for index, entry in enumerate([*inv.cards, *inv.colorless_cards])
                ],
            },
            {
                "title": "Relics",
                "items": [
                    _shop_item(
                        display_name(entry.relic_id),
                        entry.price,
                        entry,
                        action_index=_find_action_index(actions, action="buy_relic", index=index),
                    )
                    for index, entry in enumerate(inv.relics)
                ],
            },
            {
                "title": "Potions",
                "items": [
                    _shop_item(
                        display_name(entry.potion_id),
                        entry.price,
                        entry,
                        action_index=_find_action_index(actions, action="buy_potion", index=index),
                    )
                    for index, entry in enumerate(inv.potions)
                ],
            },
            {
                "title": "Services",
                "items": [
                    {
                        "name": "Remove card",
                        "price": inv.removal_cost,
                        "sold": inv.removal_used,
                        "action_index": _find_action_index(actions, action="remove_card"),
                    },
                ],
            },
        ],
    }


def _shop_item(name: str, price: int, entry: object, *, action_index: int | None) -> dict:
    return {
        "name": name,
        "price": price,
        "sold": not is_shop_entry_available(entry),
        "action_index": action_index,
    }


def _rest_screen(mgr: RunManager, actions: list[dict[str, Any]]) -> dict:
    return {
        "type": "rest",
        "title": "Rest Site",
        "items": [
            {
                "name": option.label,
                "description": option.description,
                "enabled": option.enabled,
                "action_index": _find_action_index(actions, action="rest_option", option_id=option.option_id),
            }
            for option in getattr(mgr, "_rest_options", [])
        ],
    }


def _event_screen(mgr: RunManager, actions: list[dict[str, Any]]) -> dict:
    event = getattr(mgr, "_event_model", None)
    return {
        "type": "event",
        "title": display_name(event.event_id if event is not None else "Event"),
        "items": [
            {
                "name": option.label,
                "description": option.description,
                "enabled": option.enabled,
                "action_index": _find_action_index(actions, action="event_choice", option_id=option.option_id),
            }
            for option in getattr(mgr, "_event_options", [])
        ],
    }


def _treasure_screen(mgr: RunManager, actions: list[dict[str, Any]]) -> dict:
    reward = getattr(mgr, "_current_reward", None)
    item = display_name(reward.relic_id) if isinstance(reward, RelicReward) else "Chest"
    return {
        "type": "treasure",
        "title": "Treasure",
        "items": [{"name": item, "action_index": _find_action_index(actions, action="collect")}],
    }


def _find_action_index(actions: list[dict[str, Any]], **criteria: Any) -> int | None:
    for index, action in enumerate(actions):
        if all(action.get(key) == value for key, value in criteria.items()):
            return index
    return None


def _inventory(mgr: RunManager) -> dict:
    player = mgr.run_state.player
    return {
        "deck": [{"name": describe_card(card), "desc": card_description(card)} for card in player.deck],
        "relics": [display_name(relic) for relic in player.relics],
        "potions": [
            {
                "index": index,
                "name": display_name(potion.potion_id),
                "desc": potion_description(potion),
            }
            for index, potion in enumerate(player.potions)
            if potion is not None
        ],
    }


class PlayRunHandler(BaseHTTPRequestHandler):
    session = RunSession()

    def do_GET(self) -> None:
        path = urlparse(self.path).path
        if path == "/":
            self._send_file(STATIC_DIR / "play_run.html", "text/html; charset=utf-8")
            return
        if path == "/api/state":
            self._send_json(self.session.state())
            return
        self.send_error(HTTPStatus.NOT_FOUND)

    def do_POST(self) -> None:
        parsed = urlparse(self.path)
        if parsed.path == "/api/new":
            query = parse_qs(parsed.query)
            seed = _optional_int(query.get("seed", [""])[0])
            raw_ascension = query.get("ascension", [None])[0]
            ascension = _optional_int(raw_ascension) if raw_ascension is not None else None
            if ascension is None:
                ascension = DEFAULT_ASCENSION
            character = query.get("character", [CHARACTERS[DEFAULT_CHARACTER_INDEX]])[0]
            skip_neow = query.get("skip_neow", ["0"])[0] in {"1", "true", "yes"}
            self._send_json(self.session.start(
                character=character,
                seed=seed,
                ascension=ascension,
                skip_neow=skip_neow,
            ))
            return
        if parsed.path == "/api/action":
            query = parse_qs(parsed.query)
            action_index = _optional_int(query.get("index", ["-1"])[0])
            self._send_json(self.session.take_action(action_index if action_index is not None else -1))
            return
        self.send_error(HTTPStatus.NOT_FOUND)

    def log_message(self, format: str, *args: object) -> None:
        return

    def _send_json(self, payload: dict) -> None:
        body = json.dumps(payload).encode("utf-8")
        self.send_response(HTTPStatus.OK)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _send_file(self, path: Path, content_type: str) -> None:
        body = path.read_bytes()
        self.send_response(HTTPStatus.OK)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


def _optional_int(value: str) -> int | None:
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def make_server(host: str = DEFAULT_HOST, port: int = DEFAULT_PORT) -> ThreadingHTTPServer:
    return ThreadingHTTPServer((host, port), PlayRunHandler)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Serve the STS2 full-run web UI.")
    parser.add_argument("--host", default=DEFAULT_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--open", action="store_true", help="Open the UI in the default browser.")
    parser.add_argument(
        "--ascension",
        type=int,
        default=DEFAULT_ASCENSION,
        help="Default ascension level for new runs (overridable per run via /api/new?ascension=N).",
    )
    return parser.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    global DEFAULT_ASCENSION
    args = parse_args(argv)
    DEFAULT_ASCENSION = args.ascension
    PlayRunHandler.session.ascension = args.ascension
    server = make_server(args.host, args.port)
    url = f"http://{args.host}:{args.port}/"
    print(f"Serving STS2 full-run web UI at {url} (ascension {args.ascension})")
    if args.open:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
