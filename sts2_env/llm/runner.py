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
    answer_prefill: str = "<think>

</think>

CHOICE:"
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
        self.calls = 0
        self.total_s = 0.0
        self.total_out_tokens = 0

    def ask(self, system: str, user: str) -> str:
        t0 = time.time()
        messages = [{"role": "system", "content": system},
                    {"role": "user", "content": user}]
        prefill = "" if self.cfg.enable_thinking else self.cfg.answer_prefill
        if prefill:
            messages.append({"role": "assistant", "content": prefill})
        out = self.llm.create_chat_completion(
            messages=messages,
            max_tokens=self.cfg.max_tokens,
            temperature=self.cfg.temperature,
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
                "reply": reply.strip()[:400],
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
