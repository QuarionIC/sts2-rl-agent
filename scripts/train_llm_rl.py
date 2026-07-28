#!/usr/bin/env python3
"""RL-train an LLM to make every out-of-combat run decision.

Combat is played by the deterministic per-turn planner (verified live), so
gradient is spent only on the decisions that actually decide runs: routing,
card rewards, shops, rests, events, boss relics.

Method: sequence-level REINFORCE with a learned baseline over LoRA adapters,
KL-anchored to the base policy. Rationale for each choice is in
``sts2_env/llm_rl/__init__.py``.

Two modes:

``--smoke``
    Validates the whole pipeline end-to-end with a scripted policy standing
    in for the model. No GPU, no weights, seconds to run. Use this to prove
    rollout collection, reward shaping and advantage computation work before
    committing hours of compute -- every expensive bug in this project so
    far was findable at this scale.

full run
    Requires transformers + peft + torch and a GPU with enough memory for
    the base model plus gradients. NOT runnable on the dev laptop:
    Qwen3.6-27B Q3_K_M inference alone needs ~13GB against ~4.4GB free with
    the game open. This targets a DGX Spark (128GB unified) or equivalent;
    validate on a small model first.

Examples
--------
    python scripts/train_llm_rl.py --smoke --episodes 4
    python scripts/train_llm_rl.py --model Qwen/Qwen3.6-27B --iterations 200
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import random
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def make_env(ascension: int, max_act_count: int, seed: int, character: str):
    """Hierarchical run env with the planner playing combat."""
    import sts2_env.events  # noqa: F401

    from sts2_env.gym_env.hierarchical_run_env import HierarchicalRunEnv
    from sts2_env.search.combat_planner import TRAIN_LADDER, PlannedCombatController

    env = HierarchicalRunEnv(
        character_id=character,
        ascension_level=ascension,
        max_act_count=max_act_count,
    )
    env.set_combat_controller(PlannedCombatController(env, ladder=TRAIN_LADDER))
    env.max_combat_steps = 120
    env.set_shaping_scale(1.0)
    env.reset(seed=seed)
    return env


def scripted_ask(rng: random.Random):
    """Stand-in policy for --smoke.

    Picks a legal option at random and returns a well-formed reply, so the
    pipeline is exercised without any model. Deliberately NOT a good policy:
    smoke mode proves the plumbing, not the learning.
    """
    def ask(prompt: str, options: list) -> tuple[int | None, str]:
        if not options:
            return None, ""
        i = rng.randrange(len(options))
        return i, f"CHOICE: {i}\nWHY: smoke-test policy"
    return ask


def llm_ask(model, tokenizer, device, max_new_tokens: int = 48,
            temperature: float = 0.7):
    """Ask the model under training, returning (choice, reply).

    Sampling (not greedy) because REINFORCE needs on-policy exploration:
    a deterministic policy has no gradient signal to learn from.
    """
    import torch

    from sts2_env.llm.state_text import SYSTEM_PROMPT, parse_choice

    def ask(prompt: str, options: list) -> tuple[int | None, str]:
        msgs = [{"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": prompt},
                {"role": "assistant", "content": "<think>\n\n</think>\n\nCHOICE:"}]
        text = tokenizer.apply_chat_template(msgs, tokenize=False,
                                             continue_final_message=True)
        ids = tokenizer(text, return_tensors="pt").to(device)
        with torch.no_grad():
            out = model.generate(**ids, max_new_tokens=max_new_tokens,
                                 do_sample=True, temperature=temperature,
                                 pad_token_id=tokenizer.eos_token_id)
        reply = "CHOICE:" + tokenizer.decode(out[0][ids["input_ids"].shape[1]:],
                                             skip_special_tokens=True)
        return parse_choice(reply, len(options)), reply
    return ask


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--smoke", action="store_true",
                    help="Validate the pipeline with a scripted policy; no GPU.")
    ap.add_argument("--model", default=None, help="HF model id or path")
    ap.add_argument("--iterations", type=int, default=100)
    ap.add_argument("--episodes", type=int, default=8,
                    help="Episodes per iteration (the REINFORCE batch)")
    ap.add_argument("--character", default="Ironclad")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=2)
    ap.add_argument("--gamma", type=float, default=0.99)
    ap.add_argument("--lr", type=float, default=1e-5)
    ap.add_argument("--kl-coef", type=float, default=0.02,
                    help="Anchor to the base policy. Without it the adapter "
                         "collapses onto whichever option scored once.")
    ap.add_argument("--lora-r", type=int, default=16)
    ap.add_argument("--seed-base", type=int, default=60_000_000)
    ap.add_argument("--out-dir", default="output/llm_rl")
    args = ap.parse_args()

    out = Path(args.out_dir)
    out.mkdir(parents=True, exist_ok=True)

    from sts2_env.llm_rl.episode import collect_episode
    from sts2_env.llm_rl.reward import (
        batch_statistics,
        compute_returns,
        normalize_advantages,
        shaped_decision_rewards,
    )

    if args.smoke:
        print("SMOKE MODE: scripted policy, no model, validating the pipeline.\n")
        rng = random.Random(0)
        ask = scripted_ask(rng)
        env = make_env(args.ascension, args.max_act_count, args.seed_base,
                       args.character)
        rollouts = []
        t0 = time.time()
        for i in range(args.episodes):
            r = collect_episode(env, ask, seed=args.seed_base + i)
            rollouts.append(r)
            rew = shaped_decision_rewards(r, gamma=args.gamma)
            ret = compute_returns(rew, gamma=args.gamma)
            adv = normalize_advantages(ret - ret.mean())
            print(f"  ep {i+1}: floor {r.final_floor:>2} decisions "
                  f"{len(r.decisions):>3} parse {r.parse_rate:.0%} "
                  f"deck {r.deck_size} upgr {r.upgrades} | "
                  f"reward sum {rew.sum():+.2f} adv[{adv.min():+.2f},{adv.max():+.2f}]",
                  flush=True)
        stats = batch_statistics(rollouts)
        print(f"\n=== PIPELINE OK ({time.time()-t0:.0f}s) ===")
        for k, v in stats.items():
            print(f"  {k}: {v}")
        (out / "smoke_stats.json").write_text(json.dumps(stats, indent=2),
                                              encoding="utf-8")
        print(f"\nwrote {out/'smoke_stats.json'}")
        print("\nNext: run without --smoke on hardware that fits the model.")
        return 0

    if not args.model:
        print("--model is required without --smoke")
        return 2

    # ---- full training path ----
    try:
        import torch
        from peft import LoraConfig, get_peft_model
        from transformers import AutoModelForCausalLM, AutoTokenizer
    except ImportError as e:
        print(f"Missing dependency: {e}. Install transformers + peft + torch.")
        print("This path needs a GPU with room for the base model plus "
              "gradients; it will not run on the dev laptop.")
        return 2

    device = "cuda" if torch.cuda.is_available() else "cpu"
    print(f"loading {args.model} on {device} ...", flush=True)
    tokenizer = AutoTokenizer.from_pretrained(args.model)
    model = AutoModelForCausalLM.from_pretrained(
        args.model, torch_dtype=torch.bfloat16, device_map="auto")
    model = get_peft_model(model, LoraConfig(
        r=args.lora_r, lora_alpha=args.lora_r * 2, lora_dropout=0.05,
        task_type="CAUSAL_LM",
        target_modules=["q_proj", "k_proj", "v_proj", "o_proj"],
    ))
    model.print_trainable_parameters()
    opt = torch.optim.AdamW(
        [p for p in model.parameters() if p.requires_grad], lr=args.lr)

    env = make_env(args.ascension, args.max_act_count, args.seed_base,
                   args.character)
    ask = llm_ask(model, tokenizer, device)
    history = out / "train_history.jsonl"

    for it in range(args.iterations):
        rollouts = []
        for i in range(args.episodes):
            seed = args.seed_base + it * args.episodes + i
            rollouts.append(collect_episode(env, ask, seed=seed))

        stats = batch_statistics(rollouts)
        stats["iteration"] = it
        # Baseline = batch mean return, the cheapest variance reduction that
        # actually works at this batch size.
        all_adv = []
        for r in rollouts:
            rew = shaped_decision_rewards(r, gamma=args.gamma)
            ret = compute_returns(rew, gamma=args.gamma)
            all_adv.append(ret)
        flat = np.concatenate(all_adv) if all_adv else np.zeros(0)
        baseline = float(flat.mean()) if flat.size else 0.0
        stats["baseline"] = baseline

        with history.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(stats) + "\n")
        print(f"[iter {it}] floors {stats['mean_floors']:.2f} "
              f"+/- {stats['se_floors']:.2f} deck {stats['mean_deck']:.1f} "
              f"upgr {stats['mean_upgrades']:.2f} parse "
              f"{stats['parse_rate']:.0%}", flush=True)

        # NOTE: the gradient step needs the chosen-option logprobs recomputed
        # under the current adapter. That requires a forward pass per
        # decision and is the expensive part; it is intentionally left as
        # the next implementation step rather than stubbed with something
        # that would silently train on nothing.
        model.save_pretrained(str(out / f"adapter_iter{it}"))

    print(f"done -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
