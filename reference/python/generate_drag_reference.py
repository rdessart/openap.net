"""Generate drag reference values from Python OpenAP."""

from openap import Drag


def main() -> None:
    drag = Drag("A320")
    drag_wave = Drag("A320", wave_drag=True)

    clean_cases = (
        (65000.0, 250.0, 10000.0, 0.0),
        (65000.0, 250.0, 10000.0, 1500.0),
        (65000.0, 450.0, 35000.0, 0.0),
    )

    for mass, tas, alt, vs in clean_cases:
        value = drag.clean(
            mass=mass,
            tas=tas,
            alt=alt,
            vs=vs,
        )
        print(
            f"clean mass={mass:g} tas={tas:g} alt={alt:g} vs={vs:g}: "
            f"{float(value):.17g}"
        )

    value = drag_wave.clean(
        mass=65000.0,
        tas=450.0,
        alt=35000.0,
    )
    print(f"clean wave: {float(value):.17g}")

    nonclean_cases = (
        (65000.0, 160.0, 0.0, 10.0, False),
        (65000.0, 150.0, 0.0, 15.0, True),
        (65000.0, 140.0, 0.0, 30.0, True),
    )

    for mass, tas, alt, flap, gear in nonclean_cases:
        value = drag.nonclean(
            mass=mass,
            tas=tas,
            alt=alt,
            flap_angle=flap,
            landing_gear=gear,
        )
        print(
            f"nonclean mass={mass:g} tas={tas:g} alt={alt:g} "
            f"flap={flap:g} gear={gear}: {float(value):.17g}"
        )


if __name__ == "__main__":
    main()
