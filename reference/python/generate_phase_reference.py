"""Generate representative flight-phase references from Python OpenAP."""

import numpy as np

from openap import FlightPhase


def main() -> None:
    phase = FlightPhase()

    ts = np.arange(360, dtype=float)
    alt = np.zeros(360)
    spd = np.zeros(360)
    roc = np.zeros(360)

    alt[60:120] = 1000
    spd[60:120] = 250
    roc[60:120] = 1500

    alt[120:180] = 1000
    spd[120:180] = 250
    roc[120:180] = -1500

    alt[180:240] = 35000
    spd[180:240] = 480

    alt[240:300] = 10000
    spd[240:300] = 300

    phase.set_trajectory(ts, alt, spd, roc)

    labels = phase.phaselabel()

    for start in range(0, 360, 60):
        print(start, labels[start : start + 60][0])

    # The last window remains NA because phase.py iterates:
    # range(0, int(max(twindows))).
    print("last-window-unique:", sorted(set(labels[300:])))


if __name__ == "__main__":
    main()
