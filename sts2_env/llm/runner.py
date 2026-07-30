"""Local GGUF inference backend + the LLM run policy.

Model-agnostic by design: the model path, context size and GPU-layer count
are all configuration. Validating the harness with a small model on this
laptop and later running Qwen3.6-27B on bigger hardware is a config change,
not a code change.

Hardware note (measured 2026-07-28 on this machine): RTX 4060 Laptop with
8GB VRAM and 15.7GB system RAM. Qwen3.6-27B Q4_K_M is 16.8GB, which exceeds
total system RAM, so the full-quality quant cannot run here at all. A DGX
Spark's 128GB unified memory holds it at ~13% capacity, or BF16 (~54GB)
with no quantization loss.
"""

from __future__ import annotations

import os
import time
from dataclasses import dataclass, field

import numpy as np


def _enable_cuda_dlls() -> None:
    """Put torch's bundled CUDA runtime on the DLL search path.

    The prebuilt llama-cpp-python wheel links against CUDA but does not ship
    the runtime; without this, importing it fails with an opaque "Could not
    find module llama.dll" that names the wrong library.
    """
    if os.name != "nt":
        return
    try:
        import torch

        lib = os.path.join(os.path.dirname(torch.__file__), "lib")
        if os.path.isdir(lib):
            os.add_dll_directory(lib)
    except Exception:
        pass


@dataclass
class LLMConfig:
    model_path: str
    n_ctx: int = 8192
    #: -1 offloads everything it can; lower this when the model exceeds VRAM.
    n_gpu_layers: int = -1
    max_tokens: int = 160
    temperature: float = 0.3
    #: Qwen3.6 is a hybrid reasoning model. Thinking costs hundreds of tokens
    #: per decision, and at the few-tok/s this hardware sustains that is
    #: minutes per move, so it is off by default. Turn it on only where the
    #: throughput budget allows.
    enable_thinking: bool = False
    #: llama.cpp does not recognise the qwen35 architecture and silently
    #: applies NO chat template, so the model reads the prompt as a document
    #: to continue ("The user is playing...") instead of a turn to answer.
    #: Qwen uses ChatML, so state it explicitly.
    chat_format: str | None = "chatml"
    #: Prefilled assistant text. Qwen3.6 is a hybrid reasoner that ignores a
    #: /no_think suffix. Seeding with a bare "CHOICE:" only sometimes
    #: collapsed the reasoning block -- measured 23% parse rate over a full
    #: episode, because 77% of the time the model still opened a real <think>
    #: and the token cap truncated it mid-reasoning. Prefilling an ALREADY
    #: CLOSED think block removes the option entirely: the block cannot be
    #: reopened, so generation starts at the answer. Measured 4/4 parse at
    #: ~6.5s per decision.
    answer_prefill: str = "<think>\n\n</think>\n\nCHOICE:"
    #: GBNF grammar constraining the reply to exactly "CHOICE: <int>".
    #:
    #: The prefill alone does not hold. Measured on the Spark with Q8_0: the
    #: model emits the closed think block it was handed and then opens a NEW
    #: one -- "<think>\nThe user wants me to play one card..." -- reasoning past
    #: max_tokens, so the parser sees an unterminated block and correctly
    #: refuses to guess. Out of combat it closes the block immediately and
    #: answers (100% parse); in combat, where the decision is genuinely harder,
    #: it reasoned every time (0/2 parsed).
    #:
    #: A grammar removes the possibility rather than discouraging it, and it is
    #: also the single biggest speed lever: the reply becomes ~5 tokens instead
    #: of the full token budget. At the ~7.6 tok/s this 28GB Q8_0 sustains
    #: (memory-bandwidth-bound on GB10) that is the difference between ~21s and
    #: ~1s per decision -- a 40-hour evaluation versus a 4-hour one.
    #:
    #: The cost is real and must be stated with any result: the model can no
    #: longer reason in the open before answering. Set to None (and raise
    #: max_tokens) to measure the thinking configuration instead.
    grammar: str | None = 'root ::= "CHOICE: " [0-9]+'
    seed: int = 0
    verbose: bool = False


class LocalLLM:
    """Thin wrapper over llama.cpp with chat formatting and timing stats."""

    def __init__(self, cfg: LLMConfig):
        _enable_cuda_dlls()
        from llama_cpp import Llama

        self.cfg = cfg
        t0 = time.time()
        kw = {}
        if cfg.chat_format:
            kw["chat_format"] = cfg.chat_format
        self.llm = Llama(
            model_path=cfg.model_path,
            n_ctx=cfg.n_ctx,
            n_gpu_layers=cfg.n_gpu_layers,
            seed=cfg.seed,
            verbose=cfg.verbose,
            **kw,
        )
        self.load_s = time.time() - t0
        self._grammar = None
        self._answer_grammar = None
        if cfg.grammar:
            from llama_cpp import LlamaGrammar

            self._grammar = LlamaGrammar.from_string(cfg.grammar, verbose=False)
        self.calls = 0
        self.total_s = 0.0
        self.total_out_tokens = 0

    def ask(self, system: str, user: str) -> str:
        """One decision. In thinking mode this is TWO calls (budget forcing).

        Letting the model reason freely does not terminate in a usable answer.
        Measured on the Spark, thinking mode with a 1024-token budget: the model
        spends the whole budget restating the state and analysing, and 2 of the
        first 4 combat decisions never emitted a CHOICE at all -- so the parser
        correctly refused them and they fell back to a random legal action. At
        ~130s per decision that is unaffordable to fix by simply raising the
        budget (2048 tokens would be ~260s per decision, ~9h per episode).

        So: think within the budget, then CLOSE the block and force the answer
        under the grammar. The second call generates ~5 tokens, so it costs
        almost nothing, and the reply is guaranteed parseable while the
        reasoning is preserved. This is the standard budget-forcing trick and
        it is what makes the thinking arm measurable at all.
        """
        if self.cfg.enable_thinking:
            return self._ask_thinking(system, user)
        t0 = time.time()
        messages = [{"role": "system", "content": system},
                    {"role": "user", "content": user}]
        prefill = "" if self.cfg.enable_thinking else self.cfg.answer_prefill
        if prefill:
            messages.append({"role": "assistant", "content": prefill})
        kw2 = {}
        if self._grammar is not None:
            kw2["grammar"] = self._grammar
        out = self.llm.create_chat_completion(
            messages=messages,
            max_tokens=self.cfg.max_tokens,
            temperature=self.cfg.temperature,
            **kw2,
        )
        self.calls += 1
        self.total_s += time.time() - t0
        try:
            self.total_out_tokens += int(out["usage"]["completion_tokens"])
        except Exception:
            pass
        text = out["choices"][0]["message"]["content"] or ""
        # Re-attach the prefill so the parser sees the full "CHOICE: n" form.
        return (prefill + text) if prefill else text

    def _ask_thinking(self, system: str, user: str) -> str:
        """Reason within a budget, then force a grammar-constrained answer."""
        import re

        from llama_cpp import LlamaGrammar

        t0 = time.time()
        base = [{"role": "system", "content": system},
                {"role": "user", "content": user}]

        # Stage 1: open reasoning, bounded by max_tokens.
        out = self.llm.create_chat_completion(
            messages=base, max_tokens=self.cfg.max_tokens,
            temperature=self.cfg.temperature,
        )
        self.calls += 1
        think = out["choices"][0]["message"]["content"] or ""
        try:
            self.total_out_tokens += int(out["usage"]["completion_tokens"])
        except Exception:
            pass

        # Already answered inside the budget? Then stage 2 is unnecessary.
        closed = "</think>" in think
        answered = re.search(r"choice\s*[:\-]?\s*\**\s*\d+",
                             think.split("</think>")[-1] if closed else "",
                             re.IGNORECASE)
        if closed and answered:
            self.total_s += time.time() - t0
            return think

        # Stage 2: close the block and force the choice. ~5 tokens.
        if self._answer_grammar is None:
            self._answer_grammar = LlamaGrammar.from_string(
                'root ::= "CHOICE: " [0-9]+', verbose=False)
        stub = think if closed else think + "\n</think>\n\n"
        out2 = self.llm.create_chat_completion(
            messages=base + [{"role": "assistant", "content": stub}],
            max_tokens=8, temperature=self.cfg.temperature,
            grammar=self._answer_grammar,
        )
        self.calls += 1
        tail = out2["choices"][0]["message"]["content"] or ""
        try:
            self.total_out_tokens += int(out2["usage"]["completion_tokens"])
        except Exception:
            pass
        self.total_s += time.time() - t0
        return stub + tail

    @property
    def tokens_per_s(self) -> float:
        return self.total_out_tokens / self.total_s if self.total_s else 0.0


class LLMRunPolicy:
    """Out-of-combat policy driven by a local LLM.

    Drop-in alongside ``KnowledgeRunPolicy`` and the RL run agent: same env,
    same eval protocol, so results are directly comparable to the measured
    random (10.27) and knowledge (10.67) baselines.

    Every decision is a numbered menu built from the simulator's own legal
    actions, so an unparseable or out-of-range reply is *counted*, never
    silently coerced into a legal move. ``fallback`` decides what happens
    then -- defaulting to the knowledge policy so a parse failure degrades to
    a sensible action rather than to option 0, while the failure still shows
    up in the stats.
    """

    def __init__(self, env, llm: LocalLLM, fallback: str = "knowledge",
                 log_path: str | None = None):
        self.env = env
        self.llm = llm
        self.fallback_kind = fallback
        self._fallback = None
        if fallback == "knowledge":
            from sts2_env.knowledge.policy import KnowledgeRunPolicy

            self._fallback = KnowledgeRunPolicy(env)
        self.rng = np.random.default_rng(0)
        self.asked = 0
        self.parsed = 0
        self.parse_failures = 0
        self.no_decision = 0
        self.log_path = log_path
        self.transcript: list[dict] = []

    def act(self, obs, mask) -> int:
        from sts2_env.llm.state_text import (
            SYSTEM_PROMPT,
            parse_choice,
            render_decision,
        )

        mask = np.asarray(mask, dtype=bool)
        legal = np.flatnonzero(mask)
        if not legal.size:
            return 0
        mgr = self.env._mgr
        if mgr is None:
            return int(legal[0])

        decision = render_decision(mgr)
        if decision is None:
            # Phase the model is not asked about (pending choices, etc.).
            self.no_decision += 1
            return self._fallback_action(obs, mask)

        self.asked += 1
        reply = self.llm.ask(SYSTEM_PROMPT, decision.prompt)
        idx = parse_choice(reply, len(decision.options))

        if idx is None:
            self.parse_failures += 1
            action = self._fallback_action(obs, mask)
        else:
            self.parsed += 1
            action = self._resolve(decision.options[idx], decision.options, mask)
            if action is None:
                self.parse_failures += 1
                action = self._fallback_action(obs, mask)

        if self.log_path is not None:
            self.transcript.append({
                "phase": decision.phase,
                "prompt": decision.prompt,
                "reply": reply.strip()[:4000],
                "chosen_index": idx,
                "action": int(action),
            })
        return int(action)

    def _resolve(self, target: dict, actions: list[dict],
                 mask: np.ndarray) -> int | None:
        """Map the chosen option dict to a legal env action index."""
        from sts2_env.gym_env.run_env import _LAYOUT
        from sts2_env.run.run_manager import RunManager

        mgr = self.env._mgr
        starts = {
            RunManager.PHASE_MAP_CHOICE: _LAYOUT.map_start,
            RunManager.PHASE_CARD_REWARD: _LAYOUT.card_reward_start,
            RunManager.PHASE_SHOP: _LAYOUT.shop_start,
            RunManager.PHASE_REST_SITE: _LAYOUT.rest_start,
            RunManager.PHASE_EVENT: _LAYOUT.event_start,
            RunManager.PHASE_TREASURE: _LAYOUT.treasure_start,
            RunManager.PHASE_BOSS_RELIC: _LAYOUT.boss_relic_start,
        }
        base = starts.get(mgr.phase)
        if base is None:
            return None
        try:
            local = mgr.get_available_actions().index(target)
        except ValueError:
            return None
        idx = base + local
        if 0 <= idx < mask.size and mask[idx]:
            return idx
        return None

    def _fallback_action(self, obs, mask) -> int:
        if self._fallback is not None:
            return int(self._fallback.act(obs, mask))
        legal = np.flatnonzero(mask)
        return int(self.rng.choice(legal))

    def stats(self) -> dict:
        return {
            "asked": self.asked,
            "parsed": self.parsed,
            "parse_failures": self.parse_failures,
            "parse_rate": self.parsed / max(self.asked, 1),
            "no_decision_phases": self.no_decision,
            "llm_calls": self.llm.calls,
            "llm_tokens_per_s": round(self.llm.tokens_per_s, 2),
            "llm_total_s": round(self.llm.total_s, 1),
            "s_per_decision": round(self.llm.total_s / max(self.asked, 1), 2),
        }


class LLMFullPolicy(LLMRunPolicy):
    """The LLM makes EVERY decision, combat included.

    ``LLMRunPolicy`` leaves fights to the deterministic planner, so its result
    (13.1 floors) measures out-of-combat play alone. This subclass routes
    combat to the model too, which is the configuration the live bridge
    already runs and whose combat quality was never measured -- see the
    "combat quality is unmeasured" note in ``sts2_env/bridge/llm_policy.py``.

    Two things are tracked separately from the parent's totals, because
    combat is where nearly all the decisions are and mixing them hides
    everything interesting:

    * per-arena counts (asked / parsed / seconds), so a combat parse
      collapse cannot hide behind a healthy out-of-combat rate;
    * combat outcomes (fights entered, won, HP lost), so a bad floors number
      can be attributed to losing fights vs routing badly.

    A combat parse failure falls back to a RANDOM legal action, never to the
    planner and never to option 0. The planner would contaminate exactly the
    thing being measured; option 0 is END TURN, which is the failure that
    cost a live run. Random is neutral and counted -- read ``parse_rate``
    before reading any outcome number.
    """

    def __init__(self, env, llm: LocalLLM, fallback: str = "knowledge",
                 log_path: str | None = None):
        super().__init__(env, llm, fallback=fallback, log_path=log_path)
        self.arena = {
            "combat": {"asked": 0, "parsed": 0, "failed": 0, "s": 0.0},
            "noncombat": {"asked": 0, "parsed": 0, "failed": 0, "s": 0.0},
        }
        self.combats_entered = 0
        self.combats_won = 0
        self._in_combat = False
        self._combat_hp_start: int | None = None
        self.combat_hp_lost: list[int] = []
        #: Per-EPISODE fight counts. The cumulative pair was unreadable in the
        #: log: "fights 65/76" is a running total, so the value for one run is
        #: only recoverable by differencing consecutive lines.
        self.ep_entered = 0
        self.ep_won = 0
        self.ep_hp_lost: list[int] = []

    def act(self, obs, mask) -> int:
        import time as _time

        from sts2_env.llm.state_text import (
            COMBAT_SYSTEM_PROMPT,
            SYSTEM_PROMPT,
            parse_choice,
            render_combat_decision,
            render_run_decision_masked,
        )
        from sts2_env.run.run_manager import RunManager

        mask = np.asarray(mask, dtype=bool)
        legal = np.flatnonzero(mask)
        if not legal.size:
            return 0
        mgr = self.env._mgr
        if mgr is None:
            return int(legal[0])

        in_combat = mgr.phase == RunManager.PHASE_COMBAT
        self._track_combat_boundary(mgr, in_combat)

        # Non-LLM arm for this arena: no prompt, no parse, no LLM stats.
        arm = self._arm_action(obs, mask, in_combat)
        if arm is not None:
            return arm

        if in_combat:
            decision = render_combat_decision(mgr, mask)
            system = COMBAT_SYSTEM_PROMPT
            key = "combat"
        else:
            # Masked variant: only offers options the env can actually take,
            # and carries env_action. See its docstring for the three bugs
            # this closes (20% of out-of-combat choices were being discarded).
            decision = render_run_decision_masked(mgr, mask)
            system = SYSTEM_PROMPT
            key = "noncombat"

        if decision is None:
            self.no_decision += 1
            return self._fallback_action(obs, mask)

        self.asked += 1
        self.arena[key]["asked"] += 1
        t0 = _time.time()
        reply = self.llm.ask(system, decision.prompt)
        self.arena[key]["s"] += _time.time() - t0
        idx = parse_choice(reply, len(decision.options))

        action = None
        if idx is not None:
            # BOTH arenas now carry env_action, validated against the mask when
            # the menu was built, so there is one resolution path and no
            # inverse lookup anywhere. Re-check the bit regardless: cheap, and
            # it turns any future encoding drift into a counted parse failure
            # rather than an illegal step.
            cand = decision.options[idx].get("env_action")
            if cand is not None and 0 <= cand < mask.size and mask[cand]:
                action = int(cand)

        if action is None:
            self.parse_failures += 1
            self.arena[key]["failed"] += 1
            # Combat must not fall back to the knowledge policy (it is
            # out-of-combat only) nor to option 0 (END TURN).
            action = (int(self.rng.choice(legal)) if in_combat
                      else self._fallback_action(obs, mask))
        else:
            self.parsed += 1
            self.arena[key]["parsed"] += 1

        if self.log_path is not None:
            self.transcript.append({
                "phase": decision.phase,
                "arena": key,
                "prompt": decision.prompt,
                "reply": reply.strip()[:4000],
                "chosen_index": idx,
                "action": int(action),
            })
        return int(action)

    def _track_combat_boundary(self, mgr, in_combat: bool) -> None:
        """Count fights entered/won and HP paid, at the phase transitions."""
        if in_combat and not self._in_combat:
            self.combats_entered += 1
            self.ep_entered += 1
            self._combat_hp_start = mgr.run_state.player.current_hp
        elif self._in_combat and not in_combat:
            self._close_combat(mgr, survived=not mgr.run_state.player.is_dead)
        self._in_combat = in_combat

    def _close_combat(self, mgr, survived: bool) -> None:
        if survived:
            self.combats_won += 1
            self.ep_won += 1
        if self._combat_hp_start is not None:
            lost = max(0, self._combat_hp_start - mgr.run_state.player.current_hp)
            self.combat_hp_lost.append(lost)
            self.ep_hp_lost.append(lost)
        self._combat_hp_start = None

    def begin_episode(self) -> None:
        """Zero the per-episode fight counters. Call right after env.reset()."""
        self.ep_entered = 0
        self.ep_won = 0
        self.ep_hp_lost = []
        self._in_combat = False
        self._combat_hp_start = None

    def finish_episode(self, mgr) -> None:
        """Close out the episode's LAST combat. Callers MUST call this.

        Boundaries are otherwise only observed on the next ``act()``, and after
        a fatal step there is no next ``act()`` -- so the combat the run died
        in was never closed. Because ``_in_combat`` also persisted across
        episodes, the first (out-of-combat, full-HP) decision of the NEXT
        episode was then read as "that fight ended and the player is alive" and
        the fatal fight was scored as a WIN.

        Measured effect before this fix: 16 episodes with 15 deaths reported
        97/98 fights won (99%) when the true figure is ~83/98 (~85%). floors,
        won, deck and parse rates were unaffected -- they come from ``info``
        and the sim -- but every combat-level statistic was inflated.
        """
        if self._in_combat:
            self._close_combat(mgr, survived=not mgr.run_state.player.is_dead)
        self._in_combat = False
        self._combat_hp_start = None

    def stats(self) -> dict:
        st = super().stats()
        for key, a in self.arena.items():
            st[f"{key}_asked"] = a["asked"]
            st[f"{key}_parse_rate"] = a["parsed"] / max(a["asked"], 1)
            st[f"{key}_s_per_decision"] = round(a["s"] / max(a["asked"], 1), 2)
            st[f"{key}_total_s"] = round(a["s"], 1)
        st["combats_entered"] = self.combats_entered
        st["combats_won"] = self.combats_won
        st["combat_win_rate"] = self.combats_won / max(self.combats_entered, 1)
        st["mean_combat_hp_lost"] = (
            round(float(np.mean(self.combat_hp_lost)), 2)
            if self.combat_hp_lost else 0.0)
        return st

    # -- policy arms ---------------------------------------------------------
    #
    # Set by the eval script so every arm (llm / planner / random / knowledge)
    # runs through THIS class, on the same env, seeds and metrics. Necessary
    # because the simulator is not bit-identical across CPU architectures --
    # card draw order diverges after a reshuffle -- so a baseline measured on
    # another machine is not a valid reference and each arm has to be measured
    # where the LLM arm is measured.
    run_policy_kind: str = "llm"
    combat_policy_kind: str = "llm"

    def install_arms(self) -> None:
        """Materialise the non-LLM arms named by ``*_policy_kind``."""
        self._planner = None
        self._knowledge = None
        if self.combat_policy_kind == "planner":
            from sts2_env.search.combat_planner import (
                EVAL_LADDER,
                PlannedCombatController,
            )
            self._planner = PlannedCombatController(self.env, ladder=EVAL_LADDER)
        if self.run_policy_kind == "knowledge":
            from sts2_env.knowledge.policy import KnowledgeRunPolicy
            self._knowledge = KnowledgeRunPolicy(self.env)

    def _arm_action(self, obs, mask, in_combat: bool):
        """Action from a non-LLM arm, or None when this arm IS the LLM."""
        kind = self.combat_policy_kind if in_combat else self.run_policy_kind
        if kind == "llm":
            return None
        if kind == "random":
            return int(self.rng.choice(np.flatnonzero(mask)))
        if kind == "planner" and self._planner is not None:
            return int(self._planner.act(obs, mask))
        if kind == "knowledge" and self._knowledge is not None:
            return int(self._knowledge.act(obs, mask))
        return int(self.rng.choice(np.flatnonzero(mask)))
