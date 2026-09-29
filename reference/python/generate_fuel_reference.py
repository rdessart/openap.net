"""Generate fuel-flow reference values from Python OpenAP."""

from openap import FuelFlow


def main() -> None:
    fuel = FuelFlow("A320")

    for thrust in (0.0, 50000.0, 100000.0, 200000.0):
        print(
            f"at_thrust thrust={thrust:g}: "
            f"{float(fuel.at_thrust(thrust)):.17g}"
        )

    for tas, alt, throttle in (
        (0.0, 0.0, 1.0),
        (100.0, 0.0, 1.0),
        (150.0, 0.0, 1.0),
        (150.0, 0.0, 0.5),
    ):
        print(
            f"takeoff tas={tas:g} alt={alt:g} throttle={throttle:g}: "
            f"{float(fuel.takeoff(tas, alt, throttle)):.17g}"
        )

    for mass, tas, alt, vs, acc in (
        (65000.0, 250.0, 10000.0, 0.0, 0.0),
        (65000.0, 250.0, 10000.0, 1500.0, 0.0),
        (65000.0, 250.0, 10000.0, 0.0, 0.5),
        (65000.0, 450.0, 35000.0, 0.0, 0.0),
    ):
        print(
            f"enroute mass={mass:g} tas={tas:g} alt={alt:g} "
            f"vs={vs:g} acc={acc:g}: "
            f"{float(fuel.enroute(mass, tas, alt, vs, acc)):.17g}"
        )


if __name__ == "__main__":
    main()
