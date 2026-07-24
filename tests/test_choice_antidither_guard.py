"""Tests for the pending-choice anti-dither guard (run_env).

Forensics showed deterministic-argmax policies toggling one option
selected/deselected forever on "choose a card" screens. The guard bounds
every choice screen: per-option toggle limit, hard per-screen step budget
forcing confirm, and a never-deadlock fallback.
"""

import numpy as np

import sts2_env.events  # noqa: F401

from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv


def _make_env(monkeypatch, fake_choice):
    env = RichSTS2RunEnv(character_id="Necrobinder", ascension_level=0, max_act_count=1)
    monkeypatch.setattr(env, "_active_choice_object", lambda: fake_choice)
    return env


def _fresh_mask(n_options: int, can_confirm: bool = True) -> np.ndarray:
    mask = np.zeros(157, dtype=np.int8)
    if can_confirm:
        mask[0] = 1
    mask[1: 1 + n_options] = 1
    return mask


def test_option_masked_after_two_toggles(monkeypatch):
    fake = object()
    env = _make_env(monkeypatch, fake)
    env._note_choice_action(1)  # toggle option 0
    env._note_choice_action(1)  # toggle option 0 again (the observed 2-cycle)
    mask = _fresh_mask(3)
    env._limit_choice_mask(mask, 0, 3, True, fake)
    assert mask[1] == 0          # exhausted option masked
    assert mask[2] == 1 and mask[3] == 1
    assert mask[0] == 1          # confirm untouched


def test_step_budget_forces_confirm_only(monkeypatch):
    fake = object()
    env = _make_env(monkeypatch, fake)
    env._sync_choice_tracking(fake)
    env._choice_steps = env.CHOICE_STEP_BUDGET_BASE + env.CHOICE_STEP_BUDGET_PER_OPTION * 3
    mask = _fresh_mask(3)
    env._limit_choice_mask(mask, 0, 3, True, fake)
    assert mask[0] == 1
    assert mask[1:4].sum() == 0  # toggles all masked -> confirm forced


def test_step_budget_without_confirm_keeps_least_toggled_option(monkeypatch):
    fake = object()
    env = _make_env(monkeypatch, fake)
    env._sync_choice_tracking(fake)
    env._choice_toggles = {0: 3, 1: 1, 2: 2}
    env._choice_steps = 10_000
    mask = _fresh_mask(3, can_confirm=False)
    env._limit_choice_mask(mask, 0, 3, False, fake)
    assert list(mask[1:4]) == [0, 1, 0]  # only least-toggled option remains


def test_never_deadlocks_when_all_options_exhausted(monkeypatch):
    fake = object()
    env = _make_env(monkeypatch, fake)
    env._sync_choice_tracking(fake)
    env._choice_toggles = {0: 2, 1: 2}
    mask = _fresh_mask(2, can_confirm=False)
    env._limit_choice_mask(mask, 0, 2, False, fake)
    assert mask[1:3].sum() >= 1          # restored, not deadlocked
    assert env._choice_toggles == {}      # counts reset for a fresh cycle


def test_screen_change_resets_counts(monkeypatch):
    first, second = object(), object()
    env = _make_env(monkeypatch, first)
    env._note_choice_action(1)
    env._note_choice_action(1)
    assert env._choice_toggles
    mask = _fresh_mask(2)
    env._limit_choice_mask(mask, 0, 2, True, second)  # new screen identity
    assert env._choice_toggles == {}
    assert mask[1] == 1  # no stale masking carried over
