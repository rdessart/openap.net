"""Generate emission reference values from Python OpenAP."""

from openap import Emission


def main() -> None:
    emission = Emission("A320")

    for fuel_flow in (0.0, 0.5, 1.0, 2.0):
        print(
            f"co2 ff={fuel_flow:g}: "
            f"{float(emission.co2(fuel_flow)):.17g}"
        )

    cases = (
        (2.0, 250.0, 0.0, 0.0),
        (1.0, 250.0, 10000.0, 0.0),
        (0.7, 450.0, 35000.0, 0.0),
        (1.0, 250.0, 10000.0, 15.0),
        (0.2, 0.0, 0.0, 0.0),
    )

    for ff, tas, alt, dt in cases:
        print(
            f"case ff={ff:g} tas={tas:g} alt={alt:g} dT={dt:g}: "
            f"nox={float(emission.nox(ff, tas, alt, dt)):.17g}, "
            f"co={float(emission.co(ff, tas, alt, dt)):.17g}, "
            f"hc={float(emission.hc(ff, tas, alt, dt)):.17g}"
        )


if __name__ == "__main__":
    main()
