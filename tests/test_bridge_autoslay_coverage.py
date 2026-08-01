"""Coverage guards for the AutoSlay bridge wiring."""

import re
from pathlib import Path

RL_AUTO_SLAYER = Path(__file__).resolve().parents[1] / "bridge_mod" / "RlAutoSlayer.cs"
CARD_SELECT_CMD = (
    Path(__file__).resolve().parents[1]
    / "decompiled"
    / "MegaCrit.Sts2.Core.Commands"
    / "CardSelectCmd.cs"
)


def _rl_auto_slayer_source() -> str:
    return RL_AUTO_SLAYER.read_text(encoding="utf-8")


def _card_select_cmd_source() -> str:
    return CARD_SELECT_CMD.read_text(encoding="utf-8")


def _method_bodies(source: str, method_name: str) -> list[str]:
    pattern = re.compile(
        rf"\n\tpublic static (?:async )?Task[^\n]*\b{re.escape(method_name)}\b"
    )
    starts = [match.start() for match in pattern.finditer(source)]
    bodies: list[str] = []
    for start in starts:
        next_method = source.find("\n\tpublic static", start + 1)
        if next_method == -1:
            next_method = source.find("\n\tprivate static", start + 1)
        bodies.append(source[start:next_method])
    return bodies


def _method_body_with_call(source: str, method_name: str, call: str) -> str:
    for body in _method_bodies(source, method_name):
        if call in body:
            return body
    raise AssertionError(f"{method_name} does not call {call}")


def test_autoslay_run_rooms_route_through_rl_handlers() -> None:
    source = _rl_auto_slayer_source()

    assert "[RoomType.Monster] = combatHandler" in source
    assert "[RoomType.Elite] = combatHandler" in source
    assert "[RoomType.Boss] = combatHandler" in source
    assert "[RoomType.Event] = new RlEventRoomHandler()" in source
    assert "[RoomType.Shop] = new RlShopRoomHandler()" in source
    assert "[RoomType.Treasure] = new RlTreasureRoomHandler()" in source
    assert "[RoomType.RestSite] = new RlRestSiteRoomHandler()" in source
    assert "_mapHandler = new RlMapHandler()" in source


def test_autoslay_standalone_choice_screens_route_through_rl_handlers() -> None:
    source = _rl_auto_slayer_source()

    assert "[typeof(NRewardsScreen)] = new RlRewardsScreenHandler()" in source
    assert "[typeof(NCardRewardSelectionScreen)] = new RlCardRewardScreenHandler()" in source
    assert "[typeof(NChooseABundleSelectionScreen)] = new RlCardBundleScreenHandler()" in source
    assert "[typeof(NChooseARelicSelection)] = new RlChooseARelicScreenHandler()" in source
    assert "[typeof(NGameOverScreen)] = new RlGameOverScreenHandler()" in source
    assert "[typeof(NCrystalSphereScreen)] = new RlCrystalSphereScreenHandler()" in source


def test_card_select_screens_are_guarded_by_rl_selector() -> None:
    source = _rl_auto_slayer_source()

    assert "CardSelectCmd.UseSelector(new RlCardSelector())" in source
    assert "[typeof(NDeckUpgradeSelectScreen)] = new DeckUpgradeScreenHandler()" in source
    assert "[typeof(NDeckTransformSelectScreen)] = new DeckTransformScreenHandler()" in source
    assert "[typeof(NDeckEnchantSelectScreen)] = new DeckEnchantScreenHandler()" in source
    assert "[typeof(NDeckCardSelectScreen)] = new DeckCardSelectScreenHandler()" in source
    assert "[typeof(NSimpleCardSelectScreen)] = new SimpleCardSelectScreenHandler()" in source
    assert "[typeof(NChooseACardSelectionScreen)] = new ChooseACardScreenHandler()" in source


def test_autoslay_run_flow_uses_named_protocol_and_timing_constants() -> None:
    source = _rl_auto_slayer_source()
    compact_source = "".join(source.split())

    # The terminated signal must name the protocol constant AND carry a
    # reason.
    #
    # It used to be asserted as the bare one-argument call. That was correct
    # until 2026-08-01, when measurement showed "terminated" is 62% of all
    # live runs (46 of 74 across 8 sessions) -- it is the majority outcome,
    # not an edge case, and it truncates every live floor and win-rate number
    # on a non-random subset of runs. The exception text that explains it went
    # to GD.Print, whose stdout Steam discards, so it was unrecoverable. The
    # reason now travels on the payload.
    assert "RunCompleteState(NonCombatBridgeProtocol.TerminatedResult," in compact_source, (
        "the terminated signal no longer carries a reason argument; without it "
        "the majority run outcome is unexplained and unrecoverable"
    )
    assert "lastFailure" in compact_source, (
        "the exception text is not captured for the terminated payload"
    )
    assert "RunCompleteState(NonCombatBridgeProtocol.VictoryResult)" in compact_source
    assert "NonCombatBridgeProtocol.GameOverState" in source
    assert "NonCombatBridgeProtocol.GameOverMessage" in source
    assert '"{\\"type\\":\\"run_complete\\"' not in source
    assert '"{\\"type\\":\\"game_over\\"' not in source
    # Runs play to a win or a death now, not to a floor number. The old
    # FinalRunFloor cap silently truncated every successful run -- the agent
    # could never finish an Act 3 boss or reach Act 4 -- so the loop bound is
    # a runaway guard and the RunEnded checks are what terminate it.
    assert "while (runState.TotalFloor < AbsoluteMaxFloor)" in source
    # Match the USE, not the word: the comment above the loop deliberately
    # names FinalRunFloor to explain what it replaced and why.
    assert "< FinalRunFloor" not in source
    assert "private const int FinalRunFloor" not in source
    assert "if (RunEnded)" in source
    assert "TimeSpan.FromMinutes(RunTimeoutMinutes)" in source
    assert "TimeSpan.FromSeconds(RunStateTimeoutSeconds)" in source
    assert "TimeSpan.FromSeconds(RoomAssignmentTimeoutSeconds)" in source
    assert "TimeSpan.FromSeconds(RewardsScreenTimeoutSeconds)" in source


def test_decompiled_card_select_cmd_intercepts_default_selection_screens() -> None:
    source = _card_select_cmd_source()
    guarded_screens = {
        "FromChooseACardScreen": "NChooseACardSelectionScreen.ShowScreen",
        "FromSimpleGridForRewards": "NSimpleCardSelectScreen.Create",
        "FromSimpleGrid": "NSimpleCardSelectScreen.Create",
        "FromDeckForUpgrade": "NDeckUpgradeSelectScreen.ShowScreen",
        "FromDeckForTransformation": "NDeckTransformSelectScreen.ShowScreen",
        "FromDeckForEnchantment": "NDeckEnchantSelectScreen.ShowScreen",
        "FromDeckGeneric": "NDeckCardSelectScreen.Create",
    }

    for method_name, screen_call in guarded_screens.items():
        method = _method_body_with_call(source, method_name, screen_call)
        assert "Selector != null" in method or "Selector == null" in method
        assert method.index("Selector") < method.index(screen_call)


def test_decompiled_card_bundle_path_does_not_use_card_selector() -> None:
    source = _card_select_cmd_source()
    method = _method_body_with_call(
        source,
        "FromChooseABundleScreen",
        "NChooseABundleSelectionScreen.ShowScreen",
    )

    assert "Selector" not in method
    assert "NChooseABundleSelectionScreen.ShowScreen" in method
    assert "[typeof(NChooseABundleSelectionScreen)] = new RlCardBundleScreenHandler()" in (
        _rl_auto_slayer_source()
    )
