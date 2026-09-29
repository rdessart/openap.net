"""Generate thrust reference values from Python OpenAP."""

from openap import Thrust


def main() -> None:
    thrust = Thrust("A320")

    takeoff_cases = (
        (0.0, 0.0, 0.0),
        (100.0, 0.0, 0.0),
        (150.0, 0.0, 0.0),
        (150.0, 5000.0, 0.0),
        (150.0, 5000.0, 15.0),
    )

    for tas, alt, dt in takeoff_cases:
        value = thrust.takeoff(tas=tas, alt=alt, dT=dt)
        print(f"takeoff tas={tas:g} alt={alt:g} dT={dt:g}: {float(value):.17g}")

    climb_cases = (
        (250.0, 5000.0, 2000.0),
        (300.0, 15000.0, 2500.0),
        (450.0, 35000.0, 1000.0),
    )

    for tas, alt, roc in climb_cases:
        value = thrust.climb(tas=tas, alt=alt, roc=roc)
        print(f"climb tas={tas:g} alt={alt:g} roc={roc:g}: {float(value):.17g}")

    print(f"cruise: {float(thrust.cruise(450.0, 35000.0)):.17g}")
    print(f"idle: {float(thrust.descent_idle(250.0, 10000.0)):.17g}")


if __name__ == "__main__":
    main()
