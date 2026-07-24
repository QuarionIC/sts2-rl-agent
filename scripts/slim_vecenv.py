"""A torch-free SubprocVecEnv for simulator workers.

Why this exists
---------------
SB3's ``SubprocVecEnv`` spawns workers that execute
``stable_baselines3.common.vec_env.subproc_vec_env._worker``. On Windows
(spawn start method) each worker therefore imports ``stable_baselines3``,
whose package ``__init__`` imports torch: ~870 MB of committed memory per
worker that only steps the (torch-free) simulator. With 16-24 workers on a
16 GB machine this alone exhausts the Windows commit limit (observed:
``numpy._core._exceptions._ArrayMemoryError`` in the learner and
``WinError 1455`` at spawn).

``SlimSubprocVecEnv`` spawns ``_slim_worker`` from THIS module instead. The
worker's transitive imports are gymnasium + numpy + the simulator only
(~40 MB private with single-threaded BLAS -- set OPENBLAS_NUM_THREADS=1 in
the launcher; the trainer does this before importing numpy). Every pickled
object crossing the process boundary must also avoid sb3 modules, hence the
local ``_CloudpickleFn`` (instead of sb3's ``CloudpickleWrapper``) and the
parent-side ``env_is_wrapped`` override (instead of round-tripping a
``Monitor`` class object whose unpickling would import sb3 in the worker).

The command protocol mirrors the installed sb3 2.9 worker exactly
(step/reset/render/close/get_spaces/env_method/get_attr/has_attr/set_attr/
is_wrapped) so the parent-side ``SubprocVecEnv`` methods keep working
unchanged.
"""

from __future__ import annotations

import multiprocessing as mp
from collections.abc import Callable
from typing import Any

import cloudpickle
import gymnasium as gym

from stable_baselines3.common.vec_env import SubprocVecEnv


class _CloudpickleFn:
    """Minimal stand-in for sb3's CloudpickleWrapper.

    Lives in this torch-free module so unpickling it in the worker does not
    import stable_baselines3.
    """

    def __init__(self, fn: Callable[[], gym.Env]):
        self.fn = fn

    def __getstate__(self) -> bytes:
        return cloudpickle.dumps(self.fn)

    def __setstate__(self, state: bytes) -> None:
        self.fn = cloudpickle.loads(state)


def _slim_worker(
    remote: mp.connection.Connection,
    parent_remote: mp.connection.Connection,
    env_fn_wrapper: "_CloudpickleFn",
) -> None:
    parent_remote.close()
    env = env_fn_wrapper.fn()
    reset_info: dict[str, Any] | None = {}
    while True:
        try:
            cmd, data = remote.recv()
            if cmd == "step":
                observation, reward, terminated, truncated, info = env.step(data)
                done = terminated or truncated
                info["TimeLimit.truncated"] = truncated and not terminated
                if done:
                    info["terminal_observation"] = observation
                    observation, reset_info = env.reset()
                remote.send((observation, reward, done, info, reset_info))
            elif cmd == "reset":
                maybe_options = {"options": data[1]} if data[1] else {}
                observation, reset_info = env.reset(seed=data[0], **maybe_options)
                remote.send((observation, reset_info))
            elif cmd == "render":
                remote.send(env.render())
            elif cmd == "close":
                env.close()
                remote.close()
                break
            elif cmd == "get_spaces":
                remote.send((env.observation_space, env.action_space))
            elif cmd == "env_method":
                method = env.get_wrapper_attr(data[0])
                remote.send(method(*data[1], **data[2]))
            elif cmd == "get_attr":
                remote.send(env.get_wrapper_attr(data))
            elif cmd == "has_attr":
                try:
                    env.get_wrapper_attr(data)
                    remote.send(True)
                except AttributeError:
                    remote.send(False)
            elif cmd == "set_attr":
                remote.send(setattr(env, data[0], data[1]))  # type: ignore[func-returns-value]
            elif cmd == "is_wrapped":
                # Answered parent-side by SlimSubprocVecEnv.env_is_wrapped;
                # kept for protocol completeness. Checking by class NAME
                # avoids unpickling an sb3 class in this process.
                target = data if isinstance(data, str) else getattr(data, "__name__", str(data))
                current: Any = env
                found = False
                while current is not None:
                    if type(current).__name__ == target:
                        found = True
                        break
                    current = getattr(current, "env", None)
                remote.send(found)
            else:
                raise NotImplementedError(f"`{cmd}` is not implemented in the worker")
        except EOFError:
            break
        except KeyboardInterrupt:
            break


class SlimSubprocVecEnv(SubprocVecEnv):
    """SubprocVecEnv whose workers never import stable_baselines3/torch."""

    def __init__(self, env_fns: list[Callable[[], gym.Env]], start_method: str | None = None):
        # Reimplements SubprocVecEnv.__init__ spawn loop with _slim_worker;
        # everything after spawning (pipes, spaces handshake) matches sb3.
        from stable_baselines3.common.vec_env.base_vec_env import VecEnv

        self.waiting = False
        self.closed = False
        n_envs = len(env_fns)

        if start_method is None:
            forkserver_available = "forkserver" in mp.get_all_start_methods()
            start_method = "forkserver" if forkserver_available else "spawn"
        ctx = mp.get_context(start_method)

        self.remotes, self.work_remotes = zip(
            *[ctx.Pipe() for _ in range(n_envs)], strict=True
        )
        self.processes = []
        for work_remote, remote, env_fn in zip(self.work_remotes, self.remotes, env_fns, strict=True):
            args = (work_remote, remote, _CloudpickleFn(env_fn))
            process = ctx.Process(target=_slim_worker, args=args, daemon=True)
            process.start()
            self.processes.append(process)
            work_remote.close()

        self.remotes[0].send(("get_spaces", None))
        observation_space, action_space = self.remotes[0].recv()

        VecEnv.__init__(self, n_envs, observation_space, action_space)

    def env_is_wrapped(self, wrapper_class: type, indices=None) -> list[bool]:
        """Answer parent-side by wrapper-class NAME.

        Sending the class object to workers would make them import its
        (sb3) module on unpickle, defeating the whole point. Our env stack
        is known (RichSTS2RunEnv, optionally ActionMasker); episode stats
        come from VecMonitor at the vec level, so Monitor-style queries are
        answered False and SB3 falls back to VecMonitor's stats correctly.
        """
        target = getattr(wrapper_class, "__name__", str(wrapper_class))
        indices = range(self.num_envs) if indices is None else indices
        results = []
        for remote_idx in (indices if hasattr(indices, "__iter__") else [indices]):
            self.remotes[remote_idx].send(("is_wrapped", target))
            results.append(self.remotes[remote_idx].recv())
        return results
