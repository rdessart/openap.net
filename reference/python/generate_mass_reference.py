"""Generate mass-estimation reference values from Python OpenAP."""

from openap import mass


def main() -> None:
    for distance in (0.0, 500.0, 1000.0, 2500.0, 5000.0, 7000.0):
        value = mass.from_range(
            "A320",
            distance,
            load_factor=0.8,
        )
        print(
            f"mass distance={distance:g}nm load_factor=0.8: "
            f"{float(value):.17g}"
        )

    for distance in (0.0, 2500.0, 5000.0):
        value = mass.from_range(
            "A320",
            distance,
            load_factor=0.8,
            fraction=True,
        )
        print(
            f"fraction distance={distance:g}nm: "
            f"{float(value):.17g}"
        )


if __name__ == "__main__":
    main()
