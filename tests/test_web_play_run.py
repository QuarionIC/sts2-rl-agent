"""Tests for the local full-run web UI state surface."""

import sts2_env.events  # noqa: F401

from sts2_env.core.enums import RoomType
from sts2_env.potions.base import create_potion
from sts2_env.run.events import get_event
from sts2_env.run.run_manager import RunManager
from sts2_env.web.play_run import RunSession, serialize_run


FULL_RUN_FLOW_SEED = 123
ROOM_SCREEN_TEST_SEED = 321
PENDING_CHOICE_TEST_SEED = 75
COMBAT_TEST_SEED = 456
LEGENDS_WERE_TRUE_EVENT_ID = "TheLegendsWereTrue"
LEGENDS_WERE_TRUE_TITLE = "The Legends Were True"
LEGENDS_NAB_MAP_LABEL = "Nab the Map"
LEGENDS_FIND_EXIT_LABEL = "Slowly Find an Exit"
BOSS_RELIC_WITHOUT_FOLLOWUP_CHOICE = "SOZU"
REST_TEST_STARTING_HP = 40
REST_TEST_EXPECTED_HP = "64/80"
LEGENDS_EXIT_EXPECTED_HP = "72/80"
MAX_COMBAT_ACTIONS_TO_REACH_REWARD = 200
MAX_REWARD_SCREENS_TO_SKIP = 10


def _first_damage_or_end_turn_action_index(state: dict) -> int:
    play_action = next(
        (
            action
            for action in state["actions"]
            if action["kind"] == "play_card"
            and ("Strike" in action["label"] or "Bash" in action["label"])
        ),
        None,
    )
    if play_action is None:
        play_action = next(
            (action for action in state["actions"] if action["kind"] == "play_card"),
            None,
        )
    if play_action is not None:
        return play_action["index"]
    return next(action["index"] for action in state["actions"] if action["kind"] == "end_turn")


def _skip_current_reward_screen(state: dict, session: RunSession) -> dict:
    item = next(
        (
            item
            for item in state["screen"]["items"]
            if item["name"].startswith("Skip")
        ),
        state["screen"]["items"][0],
    )
    return session.take_action(item["action_index"])


def _skip_reward_screens_until_map(state: dict, session: RunSession) -> dict:
    reward_steps = 0
    while state["phase"] == RunManager.PHASE_CARD_REWARD and reward_steps < MAX_REWARD_SCREENS_TO_SKIP:
        state = _skip_current_reward_screen(state, session)
        reward_steps += 1
    return state


def _take_first_map_node(state: dict, session: RunSession) -> dict:
    move_action = next(action for action in state["actions"] if action["kind"] == "move")
    return session.take_action(move_action["index"])


def _enter_legends_were_true_event(session: RunSession) -> dict:
    state = session.start(character="Ironclad", seed=ROOM_SCREEN_TEST_SEED)
    assert session.mgr is not None
    event = get_event(LEGENDS_WERE_TRUE_EVENT_ID)
    assert event is not None
    event.reset_rng_for_run(session.mgr.run_state)
    event.ensure_vars_calculated(session.mgr.run_state)
    session.mgr._phase = RunManager.PHASE_EVENT
    session.mgr._event_model = event
    session.mgr._event_started = True
    session.mgr._event_options = event.generate_initial_options(session.mgr.run_state)
    state = session.state()
    assert state["screen"]["title"] == LEGENDS_WERE_TRUE_TITLE
    return state


def _reach_first_combat_reward(session: RunSession) -> dict:
    state = session.start(character="Ironclad", seed=FULL_RUN_FLOW_SEED)
    assert state["screen"]["title"] == "Neow"
    state = session.take_action(0)
    assert state["screen"]["title"] == "Relic Reward"
    state = session.take_action(0)
    assert state["screen"]["title"] == "Map"
    state = _take_first_map_node(state, session)
    assert state["screen"]["title"] == "Combat"

    steps = 0
    while state["phase"] == RunManager.PHASE_COMBAT and steps < MAX_COMBAT_ACTIONS_TO_REACH_REWARD:
        state = session.take_action(_first_damage_or_end_turn_action_index(state))
        steps += 1

    assert state["phase"] == RunManager.PHASE_CARD_REWARD
    assert state["screen"]["title"] == "Card Reward"
    return state


def test_web_session_waits_for_new_run_before_neow() -> None:
    session = RunSession()

    state = session.state()

    assert state["phase"] == "START"
    assert state["screen"]["type"] == "start"
    assert state["screen"]["title"] == "Start Run"
    assert state["actions"] == []
    assert state["last_description"] == "Ready to start."


def test_web_session_starts_at_neow_and_advances_to_map() -> None:
    session = RunSession()

    state = session.start(character="Ironclad", seed=FULL_RUN_FLOW_SEED)
    assert state["phase"] == RunManager.PHASE_EVENT
    assert state["screen"]["type"] == "event"
    assert state["screen"]["title"] == "Neow"
    assert state["actions"][0]["label"] == "Booming Conch - Gain a positive relic"

    reward = session.take_action(0)
    assert reward["phase"] == RunManager.PHASE_CARD_REWARD
    assert reward["screen"]["type"] == "reward"
    assert reward["screen"]["items"] == [
        {"name": "Booming Conch", "action_index": 0},
    ]

    map_state = session.take_action(0)
    assert map_state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert map_state["screen"]["type"] == "map"
    assert map_state["screen"]["columns"]
    assert map_state["screen"]["paths"]
    reachable_nodes = [
        node
        for row in map_state["screen"]["rows"]
        for node in row
        if node["reachable"]
    ]
    assert reachable_nodes
    assert all(node["action_index"] is not None for node in reachable_nodes)


def test_web_treasure_item_links_to_collect_action() -> None:
    mgr = RunManager(seed=ROOM_SCREEN_TEST_SEED, character_id="Ironclad")
    mgr._enter_treasure()
    actions = mgr.get_available_actions()

    state = serialize_run(
        mgr,
        actions,
        seed=ROOM_SCREEN_TEST_SEED,
        character="Ironclad",
        ascension=0,
        last_description="",
    )

    assert state["screen"]["type"] == "treasure"
    assert state["screen"]["items"][0]["action_index"] == 0


def test_web_session_can_collect_treasure_and_return_to_map() -> None:
    session = RunSession()
    session.start(character="Ironclad", seed=ROOM_SCREEN_TEST_SEED)
    assert session.mgr is not None
    session.mgr._enter_treasure()

    state = session.state()
    collect_action = state["screen"]["items"][0]["action_index"]
    state = session.take_action(collect_action)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"


def test_web_state_serializes_combat_for_browser_display() -> None:
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Ironclad")
    mgr._enter_combat(RoomType.MONSTER)
    actions = mgr.get_available_actions()

    state = serialize_run(
        mgr,
        actions,
        seed=COMBAT_TEST_SEED,
        character="Ironclad",
        ascension=0,
        last_description="",
    )

    assert state["screen"]["type"] == "combat"
    assert state["screen"]["enemies"]
    assert "intent" in state["screen"]["enemies"][0]
    assert state["screen"]["end_turn_action_index"] == 0
    assert any(card["actions"] for card in state["screen"]["hand"])
    assert any(action["kind"] == "play_card" for action in state["actions"])


def test_web_state_serializes_combat_potion_actions() -> None:
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Ironclad")
    assert mgr.run_state.player.add_potion(create_potion("FirePotion"))
    mgr._enter_combat(RoomType.MONSTER)
    actions = mgr.get_available_actions()

    state = serialize_run(
        mgr,
        actions,
        seed=COMBAT_TEST_SEED,
        character="Ironclad",
        ascension=0,
        last_description="",
    )

    assert state["screen"]["potions"]
    assert state["screen"]["potions"][0]["actions"]
    assert any(action["kind"] == "use_potion" for action in state["actions"])


def test_web_state_serializes_pending_card_choice() -> None:
    mgr = RunManager(seed=PENDING_CHOICE_TEST_SEED, character_id="Ironclad")
    mgr._enter_rest_site()
    result = mgr._do_rest_site({"option_id": "SMITH"})
    actions = mgr.get_available_actions()

    state = serialize_run(
        mgr,
        actions,
        seed=PENDING_CHOICE_TEST_SEED,
        character="Ironclad",
        ascension=0,
        last_description=result["description"],
    )

    assert state["screen"]["type"] == "choice"
    assert state["screen"]["items"]
    assert all(item["action_index"] is not None for item in state["screen"]["items"])


def test_web_session_can_rest_and_return_to_map() -> None:
    session = RunSession()
    session.start(character="Ironclad", seed=ROOM_SCREEN_TEST_SEED)
    assert session.mgr is not None
    session.mgr.run_state.player.current_hp = REST_TEST_STARTING_HP
    session.mgr._enter_rest_site()

    state = session.state()
    rest_action = next(item["action_index"] for item in state["screen"]["items"] if item["name"] == "Rest")
    state = session.take_action(rest_action)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"
    assert state["hp"] == REST_TEST_EXPECTED_HP


def test_web_session_can_leave_shop_from_shop_screen() -> None:
    session = RunSession()
    session.start(character="Ironclad", seed=ROOM_SCREEN_TEST_SEED)
    assert session.mgr is not None
    session.mgr._enter_shop()

    state = session.state()
    leave_action = next(
        item["action_index"]
        for section in state["screen"]["sections"]
        for item in section["items"]
        if item["name"] == "Leave shop"
    )
    state = session.take_action(leave_action)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"


def test_web_session_can_take_boss_relic_and_enter_next_act() -> None:
    session = RunSession()
    session.start(character="Ironclad", seed=ROOM_SCREEN_TEST_SEED)
    assert session.mgr is not None
    session.mgr._phase = RunManager.PHASE_BOSS_RELIC
    session.mgr._boss_relics = [BOSS_RELIC_WITHOUT_FOLLOWUP_CHOICE]

    state = session.state()
    relic_action = state["screen"]["items"][0]["action_index"]
    state = session.take_action(relic_action)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"
    assert state["act"] == 2
    assert "Sozu" in state["relics"]


def test_web_session_can_finish_direct_event_and_return_to_map() -> None:
    session = RunSession()

    state = _enter_legends_were_true_event(session)
    nab_action = next(
        item["action_index"]
        for item in state["screen"]["items"]
        if item["name"] == LEGENDS_NAB_MAP_LABEL
    )
    starting_deck_size = state["deck_size"]
    state = session.take_action(nab_action)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"
    assert state["deck_size"] == starting_deck_size + 1


def test_web_session_can_finish_event_reward_and_return_to_map() -> None:
    session = RunSession()

    state = _enter_legends_were_true_event(session)
    exit_action = next(
        item["action_index"]
        for item in state["screen"]["items"]
        if item["name"] == LEGENDS_FIND_EXIT_LABEL
    )
    state = session.take_action(exit_action)

    assert state["phase"] == RunManager.PHASE_CARD_REWARD
    assert state["screen"]["title"] == "Potion Reward"

    state = _skip_current_reward_screen(state, session)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"
    assert state["hp"] == LEGENDS_EXIT_EXPECTED_HP


def test_web_session_can_reach_first_combat_reward() -> None:
    session = RunSession()

    state = _reach_first_combat_reward(session)
    state = _skip_reward_screens_until_map(state, session)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"


def test_web_session_can_take_card_reward_and_return_to_map() -> None:
    session = RunSession()

    state = _reach_first_combat_reward(session)
    card_reward = next(
        item
        for item in state["screen"]["items"]
        if not item["name"].startswith("Skip")
    )
    starting_deck_size = state["deck_size"]
    state = session.take_action(card_reward["action_index"])

    while state["phase"] == RunManager.PHASE_CARD_REWARD:
        state = _skip_current_reward_screen(state, session)

    assert state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert state["screen"]["title"] == "Map"
    assert state["deck_size"] == starting_deck_size + 1


def test_web_session_can_continue_to_second_map_node_after_rewards() -> None:
    session = RunSession()

    state = _reach_first_combat_reward(session)
    state = _skip_reward_screens_until_map(state, session)
    assert state["screen"]["current"] == (0, 1)

    state = _take_first_map_node(state, session)

    assert state["phase"] == RunManager.PHASE_COMBAT
    assert state["screen"]["title"] == "Combat"
    assert state["floor"] == 2


def test_web_combat_screen_exposes_powers_and_pile_contents() -> None:
    """The combat screen must surface player/enemy powers and the contents of
    the draw, discard, and exhaust piles (not just their counts) so the human
    player can see buffs/debuffs and inspect their piles."""
    from sts2_env.core.enums import PowerId

    mgr = RunManager(seed=7, character_id="Necrobinder", ascension_level=10)
    for _ in range(200):
        if mgr.phase == RunManager.PHASE_COMBAT:
            break
        actions = mgr.get_available_actions()
        if not actions:
            break
        mgr.take_action(actions[0])
    assert mgr.phase == RunManager.PHASE_COMBAT
    combat = mgr.get_combat_state()
    combat.apply_power_to(combat.primary_player, PowerId.VULNERABLE, 2)
    if combat.enemies:
        combat.apply_power_to(combat.enemies[0], PowerId.STRENGTH, 3)
    if combat.hand:
        combat.exhaust_pile.append(combat.hand[0])

    screen = serialize_run(
        mgr,
        mgr.get_available_actions(),
        seed=7,
        character="Necrobinder",
        ascension=10,
        last_description="",
    )["screen"]

    player_powers = screen["player"]["powers"]
    assert any(p["text"] == "VULNERABLE(2)" for p in player_powers)
    # Each power entry carries a hover description explaining the effect.
    vulnerable = next(p for p in player_powers if p["text"] == "VULNERABLE(2)")
    assert vulnerable["desc"] and "50%" in vulnerable["desc"]
    enemy_powers = [p for e in screen["enemies"] for p in e["powers"]]
    assert any(p["text"] == "STRENGTH(3)" for p in enemy_powers)
    strength = next(p for p in enemy_powers if p["text"] == "STRENGTH(3)")
    assert strength["desc"] and "3" in strength["desc"]
    piles = screen["piles"]
    # Contents, not just counts.
    assert len(piles["draw_cards"]) == piles["draw"]
    assert len(piles["exhaust_cards"]) == piles["exhaust"] >= 1
    assert "discard_cards" in piles
    # Draw pile is presented sorted (order hidden in-game).
    assert piles["draw_cards"] == sorted(piles["draw_cards"])


def test_web_combat_hand_cards_and_potions_carry_descriptions() -> None:
    """Hand cards (and combat potions) must expose a non-empty ``desc`` string
    that the frontend renders as a hover tooltip."""
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Necrobinder")
    assert mgr.run_state.player.add_potion(create_potion("FirePotion"))
    mgr._enter_combat(RoomType.MONSTER)

    screen = serialize_run(
        mgr,
        mgr.get_available_actions(),
        seed=COMBAT_TEST_SEED,
        character="Necrobinder",
        ascension=0,
        last_description="",
    )["screen"]

    assert screen["hand"]
    for card in screen["hand"]:
        assert card["desc"] and card["desc"].strip()
    for potion in screen["potions"]:
        assert potion["desc"] and potion["desc"].strip()


def test_web_combat_hand_carries_live_preview_fields() -> None:
    """Every hand entry must expose the live-value ``preview`` and structured
    ``label`` used by the frontend to render effective numbers."""
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Ironclad")
    mgr._enter_combat(RoomType.MONSTER)

    screen = serialize_run(
        mgr,
        mgr.get_available_actions(),
        seed=COMBAT_TEST_SEED,
        character="Ironclad",
        ascension=0,
        last_description="",
    )["screen"]

    assert screen["hand"]
    for card in screen["hand"]:
        assert "preview" in card
        assert "eff_damage_by_target" in card["preview"]
        assert "flags" in card["preview"]
        label = card["label"]
        assert label["title"]
        assert label["cost"]


def test_web_combat_hand_shows_effective_damage_when_strengthened() -> None:
    """With Strength on the player, a Strike's label, tooltip, and per-target
    buttons must show the value the sim would actually deal (6 + 3 = 9)."""
    from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
    from sts2_env.core.combat import CombatState
    from sts2_env.core.enums import PowerId
    from sts2_env.core.rng import Rng
    from sts2_env.monsters.act1_weak import create_shrinker_beetle
    from sts2_env.web.play_run import _combat_screen

    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=42,
    )
    creature, ai = create_shrinker_beetle(Rng(42))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    combat.apply_power_to(combat.primary_player, PowerId.STRENGTH, 3)
    strike_index = next(
        index for index, card in enumerate(combat.hand) if card.base_damage == 6
    )
    actions = [{
        "action": "play_card",
        "hand_index": strike_index,
        "target_index": 0,
        "target_name": "SHRINKER_BEETLE",
    }]

    entry = _combat_screen(combat, actions)["hand"][strike_index]

    assert "9dmg" in entry["name"]
    assert entry["label"]["damage"] == "9"
    assert entry["label"]["damage_mod"] == "up"
    assert "Deal 9 (base 6) damage" in entry["desc"]
    assert entry["preview"]["eff_damage_by_target"] == [
        {"enemy_index": 0, "value": 9, "hits": 1},
    ]
    assert entry["actions"][0]["target"] == "Shrinker Beetle (9)"


def test_web_combat_hand_shows_effective_block_when_frail() -> None:
    """With Frail on the player, a Defend previews the reduced Block through
    the sim's own block pipeline (floor(5 * 0.75) = 3)."""
    from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
    from sts2_env.core.combat import CombatState
    from sts2_env.core.enums import PowerId
    from sts2_env.core.rng import Rng
    from sts2_env.monsters.act1_weak import create_shrinker_beetle
    from sts2_env.web.play_run import _combat_screen

    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=42,
    )
    creature, ai = create_shrinker_beetle(Rng(42))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    combat.apply_power_to(combat.primary_player, PowerId.FRAIL, 2)
    defend_index = next(
        index for index, card in enumerate(combat.hand) if card.base_block == 5
    )

    entry = _combat_screen(combat, [])["hand"][defend_index]

    assert "3blk" in entry["name"]
    assert entry["label"]["block"] == "3"
    assert entry["label"]["block_mod"] == "down"
    assert "Gain 3 (base 5) Block" in entry["desc"]
    assert entry["preview"]["eff_block"] == 3


def test_web_combat_hand_unmodified_cards_keep_classic_labels() -> None:
    """Without any modifiers, labels and tooltips must match the pre-preview
    output exactly (no visual regression, no modified flags)."""
    from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
    from sts2_env.content import card_description
    from sts2_env.core.combat import CombatState
    from sts2_env.core.rng import Rng
    from sts2_env.monsters.act1_weak import create_shrinker_beetle
    from sts2_env.web.play_run import _combat_screen, describe_card

    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=42,
    )
    creature, ai = create_shrinker_beetle(Rng(42))
    combat.add_enemy(creature, ai)
    combat.start_combat()

    screen = _combat_screen(combat, [])

    for index, card in enumerate(combat.hand):
        entry = screen["hand"][index]
        assert entry["name"] == describe_card(card)
        assert entry["desc"] == card_description(card)
        assert entry["preview"]["flags"] == {}


def test_web_combat_hand_shows_per_target_damage_in_tooltip() -> None:
    """When one enemy is Vulnerable and another is not, the tooltip lists the
    per-target values and each target button carries its own number."""
    from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
    from sts2_env.core.combat import CombatState
    from sts2_env.core.enums import PowerId
    from sts2_env.core.rng import Rng
    from sts2_env.monsters.act1_weak import create_shrinker_beetle
    from sts2_env.web.play_run import _combat_screen

    rng = Rng(42)
    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=42,
    )
    for _ in range(2):
        creature, ai = create_shrinker_beetle(rng)
        combat.add_enemy(creature, ai)
    combat.start_combat()
    combat.apply_power_to(combat.enemies[1], PowerId.VULNERABLE, 2)
    strike_index = next(
        index for index, card in enumerate(combat.hand) if card.base_damage == 6
    )
    actions = [
        {
            "action": "play_card",
            "hand_index": strike_index,
            "target_index": target_index,
            "target_name": "SHRINKER_BEETLE",
        }
        for target_index in range(2)
    ]

    entry = _combat_screen(combat, actions)["hand"][strike_index]

    values = {
        item["enemy_index"]: item["value"]
        for item in entry["preview"]["eff_damage_by_target"]
    }
    assert values[0] == 6
    assert values[1] == 9  # floor(6 * 1.5) via the sim pipeline
    assert "6-9dmg" in entry["name"]
    assert entry["label"]["damage"] == "6-9"
    assert "By target:" in entry["desc"]
    assert entry["actions"][0]["target"] == "Shrinker Beetle (6)"
    assert entry["actions"][1]["target"] == "Shrinker Beetle (9)"


def test_web_inventory_deck_and_potions_carry_descriptions() -> None:
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Necrobinder")
    assert mgr.run_state.player.add_potion(create_potion("BlockPotion"))

    state = serialize_run(
        mgr,
        mgr.get_available_actions(),
        seed=COMBAT_TEST_SEED,
        character="Necrobinder",
        ascension=0,
        last_description="",
    )

    deck = state["inventory"]["deck"]
    assert deck
    for card in deck:
        assert card["name"] and card["desc"] and card["desc"].strip()
    for potion in state["inventory"]["potions"]:
        assert potion["desc"] and potion["desc"].strip()
