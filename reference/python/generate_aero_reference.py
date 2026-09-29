"""Generate reference values from the upstream Python OpenAP implementation.

Run from the repository root after installing the Python package:

    python reference/python/generate_aero_reference.py
"""

from openap import aero


def main() -> None:
    for altitude_m in (0.0, 5000.0, 11000.0, 15000.0):
        pressure, density, temperature = aero.atmos(altitude_m)
        print(
            f"atmos h={altitude_m:g}: "
            f"p={pressure:.17g}, rho={density:.17g}, T={temperature:.17g}"
        )

    for altitude_m in (0.0, 5000.0, 11000.0):
        print(
            f"vsound h={altitude_m:g}: "
            f"{aero.vsound(altitude_m):.17g}"
        )

    speed = 250.0 * aero.kts
    altitude_m = 3048.0
    print(f"250kt TAS -> CAS @10000ft: {aero.tas2cas(speed, altitude_m) / aero.kts:.17g}")
    print(f"250kt CAS -> TAS @10000ft: {aero.cas2tas(speed, altitude_m) / aero.kts:.17g}")

    crossover_ft = aero.crossover_alt(300.0 * aero.kts, 0.78) / aero.ft
    print(f"300kt/M0.78 crossover: {crossover_ft:.17g} ft")


if __name__ == "__main__":
    main()
