"""Generate representative WRAP reference values from Python OpenAP."""

from openap import WRAP


def show(name: str, value: dict) -> None:
    print(
        f"{name}: default={value['default']!r}, "
        f"minimum={value['minimum']!r}, "
        f"maximum={value['maximum']!r}, "
        f"statmodel={value['statmodel']!r}, "
        f"params={value['statmodel_params']!r}"
    )


def main() -> None:
    wrap = WRAP("A320")

    show("takeoff_speed", wrap.takeoff_speed())
    show("takeoff_distance", wrap.takeoff_distance())
    show("climb_const_vcas", wrap.climb_const_vcas())
    show("climb_const_mach", wrap.climb_const_mach())
    show("cruise_mach", wrap.cruise_mach())
    show("cruise_range", wrap.cruise_range())
    show("descent_const_mach", wrap.descent_const_mach())
    show("descent_vs_conmach", wrap.descent_vs_conmach())
    show("finalapp_vcas", wrap.finalapp_vcas())
    show("landing_speed", wrap.landing_speed())
    show("landing_distance", wrap.landing_distance())


if __name__ == "__main__":
    main()
