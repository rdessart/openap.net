"""Numerical APIs must work without importing the optional plotting stack."""

import io
import subprocess
import sys
from pathlib import Path

import pytest


def test_numerical_apis_without_matplotlib():
    # A fresh process also detects accidental imports when Matplotlib is installed.
    script = """
import importlib.abc
import sys

class NoMatplotlib(importlib.abc.MetaPathFinder):
    def find_spec(self, fullname, path=None, target=None):
        if fullname == "matplotlib" or fullname.startswith("matplotlib."):
            raise ModuleNotFoundError("Matplotlib is absent", name="matplotlib")

sys.meta_path.insert(0, NoMatplotlib())
import numpy as np
import openap
from openap.extra import filters, statistics

assert openap.prop.aircraft("A320")["limits"]["MTOW"] > 0
assert np.isfinite(openap.Drag("A320").clean(65000, 250, 35000))
assert np.isfinite(openap.Thrust("A320").climb(250, 35000, 0))
fuel = openap.FuelFlow("A320")
assert np.isfinite(fuel.enroute(65000, 250, 35000))
phase = openap.FlightPhase()
t = np.arange(120.)
phase.set_trajectory(t, np.full(120, 35000.), np.full(120, 450.), np.zeros(120))
labels = phase.phaselabel()
assert len(labels) == len(t) and labels[0] == "CR"
_, smoothed = filters.SavitzkyGolay().filter(t, t)
np.testing.assert_allclose(smoothed, t, atol=1e-10)
assert np.isfinite(statistics.fit(t, "norm")["norm"]["error"])
assert not any(name.startswith("matplotlib") for name in sys.modules)

for plot in (
    phase.plot_logics,
    lambda: fuel.plot_model(plot=False),
    lambda: filters.BaseFilter().filterplot(t, t, t, t),
    lambda: statistics.fitplot(t, "norm"),
):
    try:
        plot()
    except ImportError as exc:
        assert 'pip install "openap[plot]"' in str(exc), str(exc)
    else:
        raise AssertionError("Plotting must report the missing dependency")
"""
    result = subprocess.run(
        [sys.executable, "-c", script],
        cwd=Path(__file__).resolve().parents[1],
        capture_output=True,
        text=True,
        timeout=60,
    )
    assert result.returncode == 0, result.stdout + result.stderr


@pytest.mark.parametrize("plot_kind", ["phase", "fuel", "filter", "statistics"])
def test_plotting_with_matplotlib(plot_kind, monkeypatch):
    matplotlib = pytest.importorskip("matplotlib")
    matplotlib.use("Agg", force=True)
    from matplotlib import pyplot as plt

    import numpy as np
    import openap
    from openap.extra import filters, statistics

    monkeypatch.setattr(plt, "show", lambda: None)
    data = np.linspace(0, 10, 100)
    try:
        if plot_kind == "phase":
            openap.FlightPhase().plot_logics()
        elif plot_kind == "fuel":
            assert openap.FuelFlow("A320").plot_model(plot=False) is plt
        elif plot_kind == "filter":
            filters.BaseFilter().filterplot(data, data, data, data)
        else:
            assert statistics.fitplot(data, "norm") is plt
        assert plt.gcf().axes
        png = io.BytesIO()
        plt.gcf().savefig(png, format="png")
        assert png.getvalue().startswith(b"\x89PNG")
    finally:
        plt.close("all")
