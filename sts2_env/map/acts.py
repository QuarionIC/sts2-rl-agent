"""Per-act configuration: room counts, encounter pools, event pools, boss pools.

Based on decompiled MegaCrit.Sts2.Core.Models.Acts source.
"""

from __future__ import annotations

from dataclasses import dataclass, field


@dataclass
class ActConfig:
    """Configuration for a single act."""

    act_index: int
    num_rooms: int  # Number of room rows (used as mapLength input)
    num_weak_encounters: int = 3  # C# NumberOfWeakEncounters (3 for Acts 0/3, 2 for Acts 1/2)
    boss_ids: list[str] = field(default_factory=list)
    elite_ids: list[str] = field(default_factory=list)
    weak_encounter_ids: list[str] = field(default_factory=list)
    strong_encounter_ids: list[str] = field(default_factory=list)
    event_ids: list[str] = field(default_factory=list)
    events_visited: int = 0
    # "Acts from the Past" mod: True for legacy-act recreations (Exordium/
    # TheCity/TheBeyond). Used by run/events.py's SharedEvents pool filter.
    # Always False for the vanilla acts defined in this module.
    is_legacy: bool = False
    # "Acts from the Past" mod: stable identifier for the act model
    # ("Exordium"/"TheCity"/"TheBeyond" for the legacy recreations; empty for
    # vanilla acts). Lets content check which acts a run has visited (e.g.
    # TheCity's ForgottenAltar event is gated on an earlier ExordiumAct --
    # decompiled ForgottenAltar.HasVisitedExordium iterates
    # runState.Acts[0..CurrentActIndex) testing `is ExordiumAct`).
    act_id: str = ""
    # Identifies which encounters/* module supplies this act's encounter
    # setup pools (weak/normal/elite/boss). RunManager._get_encounter_pools
    # switches on this so that an alternate act selected for a slot pulls its
    # OWN encounters rather than the slot's vanilla default. Values match the
    # encounters module basename: "act1"/"act2"/"act3" (vanilla Overgrowth/
    # Hive/Glory), "act4" (vanilla Underdocks alternate), "act4_heart" (the
    # Act4Heart mod ending), "exordium"/"thecity"/"thebeyond" (legacy mod acts).
    pool_key: str = ""

    def to_mutable(self) -> "ActConfig":
        return ActConfig(
            act_index=self.act_index,
            num_rooms=self.num_rooms,
            num_weak_encounters=self.num_weak_encounters,
            boss_ids=list(self.boss_ids),
            elite_ids=list(self.elite_ids),
            weak_encounter_ids=list(self.weak_encounter_ids),
            strong_encounter_ids=list(self.strong_encounter_ids),
            event_ids=list(self.event_ids),
            events_visited=self.events_visited,
            is_legacy=self.is_legacy,
            act_id=self.act_id,
            pool_key=self.pool_key,
        )


# ── Act definitions ───────────────────────────────────────────────────

ACT_0 = ActConfig(
    act_index=0,
    num_rooms=15,
    pool_key="act1",
    act_id="Overgrowth",
    boss_ids=["TheLich"],
    elite_ids=["SentryAndSentry", "GremlinNob", "BookOfStabbing"],
    weak_encounter_ids=[
        "TwoLouses", "ThreeJawWorms", "SmallSlimes",
        "Cultist", "GremlinGang",
    ],
    strong_encounter_ids=[
        "BlueSlaver", "RedSlaver", "FungiBeast",
        "LooterGroup", "ExordiumWildlife", "LotOfSlimes",
    ],
    event_ids=[
        "AbyssalBaths", "Amalgamator", "BattlewornDummy", "BrainLeech",
        "Bugslayer", "ByrdonisNest", "ColorfulPhilosophers",
        "ColossalFlower", "Darv", "DenseVegetation",
        "DoorsOfLightAndDark", "DrowningBeacon",
        "GraveOfTheForgotten", "HungryForMushrooms",
        "InfestedAutomaton", "LostWisp", "Nonupeipe",
        "Orobas", "Pael", "PunchOff", "Reflections",
        "RoomFullOfCheese", "RoundTeaParty", "SapphireSeed",
        "SelfHelpBook", "SpiritGrafter", "SunkenStatue",
        "SunkenTreasury", "TabletOfTruth", "Tanx",
        "TeaMaster", "Tezcatara", "TheLegendsWereTrue",
        "ThisOrThat", "TinkerTime", "TrashHeap", "Trial",
        "UnrestSite", "Vakuu", "Wellspring",
        "WoodCarvings", "ZenWeaver",
    ],
)

ACT_1 = ActConfig(
    act_index=1,
    num_rooms=14,  # C# Hive.BaseNumberOfRooms = 14
    num_weak_encounters=2,  # C# Hive.NumberOfWeakEncounters = 2
    pool_key="act2",
    act_id="Hive",
    boss_ids=["TheCollector", "Automaton", "Champ"],
    elite_ids=["TaskMaster", "SphericGuardian", "Snecko"],
    weak_encounter_ids=[
        "SnakePlant", "Centurion", "ThreeByrds",
    ],
    strong_encounter_ids=[
        "SlaverGroup", "BookOfStabbing", "MushroomGroup",
    ],
    event_ids=[
        "AbyssalBaths", "Amalgamator", "AromaOfChaos",
        "BattlewornDummy", "Bugslayer", "ByrdonisNest",
        "ColorfulPhilosophers", "ColossalFlower", "CrystalSphere",
        "Darv", "DenseVegetation", "DollRoom",
        "DoorsOfLightAndDark", "DrowningBeacon", "EndlessConveyor",
        "FakeMerchant", "FieldOfManSizedHoles",
        "GraveOfTheForgotten", "HungryForMushrooms",
        "InfestedAutomaton", "JungleMazeAdventure",
        "LostWisp", "LuminousChoir", "MorphicGrove",
        "Nonupeipe", "Orobas", "Pael", "PotionCourier",
        "PunchOff", "RanwidTheElder", "Reflections",
        "RelicTrader", "RoundTeaParty", "SapphireSeed",
        "SelfHelpBook", "SlipperyBridge", "SpiralingWhirlpool",
        "SpiritGrafter", "StoneOfAllTime", "SunkenStatue",
        "SunkenTreasury", "Symbiote", "TabletOfTruth",
        "Tanx", "Tezcatara", "TheFutureOfPotions",
        "TheLanternKey", "ThisOrThat", "TinkerTime",
        "TrashHeap", "Trial", "UnrestSite", "Vakuu",
        "WaterloggedScriptorium", "WelcomeToWongos",
        "Wellspring", "WhisperingHollow", "WoodCarvings",
        "ZenWeaver",
    ],
)

ACT_2 = ActConfig(
    act_index=2,
    num_rooms=13,  # C# Glory.BaseNumberOfRooms = 13
    num_weak_encounters=2,  # C# Glory.NumberOfWeakEncounters = 2
    pool_key="act3",
    act_id="Glory",
    boss_ids=["AwakenedOne", "TimeEater", "DonuAndDeca"],
    elite_ids=["GiantHead", "Nemesis", "Reptomancer"],
    weak_encounter_ids=[
        "Darkling", "OrbWalker",
    ],
    strong_encounter_ids=[
        "WrithingMass", "Transient", "Maw",
    ],
    event_ids=[
        "AbyssalBaths", "Amalgamator", "AromaOfChaos",
        "BattlewornDummy", "Bugslayer", "ByrdonisNest",
        "ColorfulPhilosophers", "ColossalFlower", "CrystalSphere",
        "Darv", "DenseVegetation", "DoorsOfLightAndDark",
        "DrowningBeacon", "EndlessConveyor", "FakeMerchant",
        "FieldOfManSizedHoles", "GraveOfTheForgotten",
        "HungryForMushrooms", "InfestedAutomaton",
        "JungleMazeAdventure", "LostWisp", "LuminousChoir",
        "MorphicGrove", "Nonupeipe", "Orobas", "Pael",
        "PotionCourier", "PunchOff", "RanwidTheElder",
        "Reflections", "RelicTrader", "RoundTeaParty",
        "SapphireSeed", "SelfHelpBook", "SlipperyBridge",
        "SpiralingWhirlpool", "SpiritGrafter",
        "SunkenStatue", "SunkenTreasury", "Symbiote",
        "TabletOfTruth", "Tanx", "Tezcatara",
        "TheFutureOfPotions", "TheLanternKey", "ThisOrThat",
        "TinkerTime", "TrashHeap", "Trial", "UnrestSite",
        "Vakuu", "WaterloggedScriptorium", "Wellspring",
        "WhisperingHollow", "WoodCarvings", "ZenWeaver",
    ],
)

ACT_3 = ActConfig(
    # Act4Heart mod ("TheEnding" / identifier "glory"). Always active for
    # this simulator (see run/modifiers.py:Act4HeartModifier) -- appended
    # as a real 4th act rather than gated behind ascension or unlocks.
    # Hand-authored map (see map/generator.py:generate_act4_heart_map):
    # Start -> Rest Site -> Elite -> Boss. No shop/treasure/event/unknown
    # nodes and no procedurally-rolled Monster nodes, so num_rooms and
    # num_weak_encounters below are never actually exercised -- they exist
    # only so ActConfig's normal invariants hold if generic code inspects
    # them (e.g. before the map-generation override kicks in).
    act_index=3,
    num_rooms=2,
    num_weak_encounters=0,
    pool_key="act4_heart",
    act_id="TheEnding",
    boss_ids=["CorruptHeart"],
    elite_ids=["SpireShieldAndSpireSpear"],
    weak_encounter_ids=["EmptyFightAct4Weak"],
    strong_encounter_ids=[],
    event_ids=[],
)

ALL_ACTS = [ACT_0, ACT_1, ACT_2, ACT_3]


def get_act_config(act_index: int) -> ActConfig:
    if 0 <= act_index < len(ALL_ACTS):
        return ALL_ACTS[act_index]
    raise ValueError(f"Invalid act index: {act_index}")


# ── Act-slot candidates (extension point) ───────────────────────────────
#
# The campaign has 3 "randomizable" slots (0, 1, 2 -- vanilla Overgrowth /
# Hive / Glory). Each slot has a list of candidate ActConfigs; when a slot
# has more than one candidate, RunState picks one per-run using a dedicated
# RNG stream (RunState.rng.act_selection -- see run/run_state.py), the same
# way map generation already keys its RNG by act index
# (RunState.get_map_rng).
#
# ACT_3 ("Underdocks" / the Act4Heart mod's hand-authored ending act) is
# deliberately NOT part of this mechanism: it's always appended as a fixed
# 4th act (see run/modifiers.py:Act4HeartModifier) and never randomized.
#
# NOTE: as of this writing, this simulator has no pre-existing "alternate
# act" mechanic to extend (verified: ALL_ACTS / RunState.acts were a single
# fixed list with no per-slot variants). This registry is built fresh, with
# each slot defaulting to exactly its one vanilla ActConfig, so a *later*
# task can register the "Acts from the Past" legacy acts
# (ExordiumAct/TheCityAct/TheBeyondAct) as additional candidates for slots
# 0/1/2 without touching map-generation or run-state code. Those legacy acts
# are NOT registered here -- they have no monster/event content yet.
NUM_ACT_SLOTS = 3

_ACT_SLOT_CANDIDATES: dict[int, list[ActConfig]] = {
    0: [ACT_0],
    1: [ACT_1],
    2: [ACT_2],
}


def register_act_candidate(slot: int, act: ActConfig) -> None:
    """Register an additional candidate ActConfig for a campaign slot.

    Intended for future mod content: a later task can call this (e.g. once
    per legacy act) instead of modifying `select_act_for_slot` callers.
    """
    if slot not in _ACT_SLOT_CANDIDATES:
        raise ValueError(f"Invalid act slot: {slot} (valid slots: 0..{NUM_ACT_SLOTS - 1})")
    if act not in _ACT_SLOT_CANDIDATES[slot]:
        _ACT_SLOT_CANDIDATES[slot] = [*_ACT_SLOT_CANDIDATES[slot], act]


def act_candidates_for_slot(slot: int) -> list[ActConfig]:
    """Return the (copy of the) list of candidate ActConfigs for a slot."""
    _ensure_alternate_acts_registered()
    return list(_ACT_SLOT_CANDIDATES.get(slot, []))


def select_act_for_slot(slot: int, rng) -> ActConfig:
    """Pick which ActConfig fills a given campaign slot.

    With a single candidate, this is deterministic and never touches `rng`.
    With multiple candidates, one is chosen uniformly at random via
    `rng.choice`.
    """
    _ensure_alternate_acts_registered()
    candidates = _ACT_SLOT_CANDIDATES.get(slot)
    if not candidates:
        raise ValueError(f"No act candidates registered for slot {slot}")
    if len(candidates) == 1:
        return candidates[0]
    return rng.choice(candidates)


# ── Alternate acts for the user's install ───────────────────────────────
#
# The real game (v0.109.0 + the two active gameplay mods) offers these
# per-slot alternates, all treated as fully-discovered so each slot picks
# uniformly (see decompiled ActModel.GetRandomList: once every non-default
# act is in Progress.DiscoveredActs it joins the default in a uniform roll):
#
#   Slot 0 (Act 1): Overgrowth (ACT_0) | Underdocks | Exordium
#   Slot 1 (Act 2): Hive (ACT_1)       | TheCity
#   Slot 2 (Act 3): Glory (ACT_2)      | TheBeyond
#
# Underdocks is a VANILLA alternate (decompiled MegaCrit...Acts/Underdocks.cs,
# Index 0, IsDefault false, gated on UnderdocksEpoch which the user's account
# has revealed -- confirmed by the user). Its full encounter roster already
# lives in encounters/act4.py + monsters/act4.py (legacy naming predating the
# Act4Heart mod's real 4th act), so it just needs an ActConfig pointing at
# pool_key "act4". Exordium/TheCity/TheBeyond are the "Acts from the Past"
# mod's STS1-recreation acts (always unlocked; IsUnlocked => true).

UNDERDOCKS = ActConfig(
    act_index=0,
    num_rooms=15,  # C# Underdocks.BaseNumberOfRooms = 15
    num_weak_encounters=3,  # C# Underdocks.NumberOfWeakEncounters = 3
    pool_key="act4",
    act_id="Underdocks",
    boss_ids=["WaterfallGiant", "SoulFysh", "LagavulinMatriarch"],
    # Vanilla Underdocks.AllEvents (decompiled) -- all already implemented.
    event_ids=[
        "AbyssalBaths", "DrowningBeacon", "EndlessConveyor", "PunchOff",
        "SpiralingWhirlpool", "SunkenStatue", "SunkenTreasury",
        "DoorsOfLightAndDark", "TrashHeap", "WaterloggedScriptorium",
    ],
)


def _build_legacy_acts() -> tuple[ActConfig, ActConfig, ActConfig]:
    """Construct the three "Acts from the Past" legacy ActConfigs.

    Imported lazily inside this function so map/acts.py has no module-level
    dependency on the events package (which would risk an import cycle, since
    event modules pull in cards/relics that ultimately import map helpers).
    Each legacy act's candidate event pool is its own act-exclusive events
    plus the shared shrine/event pool; the per-run interleaved ordering
    (ShrinePatches.EventPoolPatch) is applied later at act-entry time by
    RunState.enter_act via events.build_legacy_event_pool.
    """
    from sts2_env.events.exordium import EXORDIUM_EVENT_IDS
    from sts2_env.events.thecity import THECITY_EVENT_IDS
    from sts2_env.events.thebeyond import THEBEYOND_EVENT_IDS
    from sts2_env.events.aftp_shared import AFTP_SHARED_EVENT_IDS

    exordium = ActConfig(
        act_index=0,
        num_rooms=15,  # CustomActModel.BaseNumberOfRooms[Index 0] = 15
        num_weak_encounters=3,
        pool_key="exordium",
        act_id="Exordium",
        is_legacy=True,
        boss_ids=["SlimeBoss", "Guardian", "Hexaghost"],
        event_ids=[*EXORDIUM_EVENT_IDS, *AFTP_SHARED_EVENT_IDS],
    )
    thecity = ActConfig(
        act_index=1,
        num_rooms=14,  # CustomActModel.BaseNumberOfRooms[Index 1] = 14
        num_weak_encounters=2,
        pool_key="thecity",
        act_id="TheCity",
        is_legacy=True,
        boss_ids=["Champ", "Collector", "BronzeAutomaton"],
        event_ids=[*THECITY_EVENT_IDS, *AFTP_SHARED_EVENT_IDS],
    )
    thebeyond = ActConfig(
        act_index=2,
        num_rooms=13,  # CustomActModel.BaseNumberOfRooms[Index 2] = 13
        num_weak_encounters=2,
        pool_key="thebeyond",
        act_id="TheBeyond",
        is_legacy=True,
        boss_ids=["AwakenedOne", "DonuAndDeca", "TimeEater"],
        event_ids=[*THEBEYOND_EVENT_IDS, *AFTP_SHARED_EVENT_IDS],
    )
    return exordium, thecity, thebeyond


_alternate_acts_registered = False


def _ensure_alternate_acts_registered() -> None:
    """Lazily build + register the per-slot alternate acts on first registry
    access (via act_candidates_for_slot / select_act_for_slot).

    This is deliberately NOT done at module import time: constructing the
    three legacy mod acts imports the events package, and doing so while
    map.acts is still initializing forms an import cycle
    (map.acts -> events.* -> run -> map/__init__ -> map.acts) that fires
    whenever sts2_env.events is imported before map.acts has finished (e.g.
    the web play_run entry point does `import sts2_env.events` first).
    Deferring to first access lets every module finish importing first.
    """
    global _alternate_acts_registered
    if _alternate_acts_registered:
        return
    _alternate_acts_registered = True  # set before building: re-entrancy guard
    # Each slot rolls uniformly among its candidates (default + alternates).
    register_act_candidate(0, UNDERDOCKS)
    exordium, thecity, thebeyond = _build_legacy_acts()
    register_act_candidate(0, exordium)
    register_act_candidate(1, thecity)
    register_act_candidate(2, thebeyond)
