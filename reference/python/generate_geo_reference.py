"""Generate geographic reference values from Python OpenAP."""

from datetime import datetime

from openap import geo


def main() -> None:
    print(
        "distance london-paris:",
        f"{float(geo.distance(51.5, -0.1, 48.85, 2.35)):.17g}",
    )
    print(
        "distance london-paris at 10km:",
        f"{float(geo.distance(51.5, -0.1, 48.85, 2.35, h=10000)):.17g}",
    )
    print(
        "bearing london-paris:",
        f"{float(geo.bearing(51.5, -0.1, 48.85, 2.35)):.17g}",
    )

    for distance, bearing in (
        (111000.0, 0.0),
        (111000.0, 90.0),
        (100000.0, 45.0),
    ):
        lat, lon = geo.latlon(
            0.0 if bearing != 45.0 else 51.5,
            0.0 if bearing != 45.0 else -0.1,
            distance,
            bearing,
        )
        print(
            f"latlon d={distance:g} brg={bearing:g}: "
            f"{float(lat):.17g}, {float(lon):.17g}"
        )

    solar_cases = (
        (0.0, 0.0, datetime(2024, 3, 20, 12, 0, 0)),
        (45.0, 0.0, datetime(2024, 6, 21, 0, 0, 0)),
        (23.44, 0.0, datetime(2024, 6, 21, 12, 0, 0)),
    )

    for lat, lon, timestamp in solar_cases:
        value = geo.solar_zenith_angle(lat, lon, timestamp)
        print(
            f"solar lat={lat:g} lon={lon:g} timestamp={timestamp.isoformat()}: "
            f"{float(value):.17g}"
        )


if __name__ == "__main__":
    main()
