"""Regression: importing sts2_env.events before sts2_env.map must not trigger
a circular import.

map/acts.py builds the "Acts from the Past" legacy act candidates lazily
(not at module import) specifically so that the events-first import order used
by the web/CLI play_run entry points doesn't form the cycle
map.acts -> events.* -> run -> map -> map.acts. This test runs that exact
import order in a fresh interpreter so a re-introduced eager import is caught.
"""

import subprocess
import sys


def test_events_first_import_does_not_cycle():
    # Fresh interpreter: import the events package as the very first thing,
    # exactly like `python -m sts2_env.web.play_run` does.
    code = (
        "import sts2_env.events\n"
        "import sts2_env.map.acts as a\n"
        "assert [c.act_id for c in a.act_candidates_for_slot(0)] == "
        "['Overgrowth', 'Underdocks', 'Exordium'], a.act_candidates_for_slot(0)\n"
        "print('OK')\n"
    )
    result = subprocess.run(
        [sys.executable, "-c", code],
        capture_output=True,
        text=True,
    )
    assert result.returncode == 0, result.stderr
    assert "OK" in result.stdout
