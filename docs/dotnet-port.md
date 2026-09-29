# OpenAP.NET port

This repository is a fork of OpenAP. The original Python implementation remains
in place as the behavioral reference while the .NET implementation is developed
under `src/`.

## Current scope

### Aeronautical calculations

The numerical foundation from `openap/aero.py` includes:

- ISA pressure, density and temperature
- speed of sound
- ISA pressure altitude
- TAS/Mach conversion
- EAS/TAS conversion
- CAS/TAS conversion
- CAS/Mach conversion
- CAS/Mach crossover altitude

The .NET aero API uses SI units internally. Unit conversion constants are
exposed through `AeroConstants`.

### Aircraft, engine and drag-polar properties

`AircraftDatabase`, `EngineDatabase` and `DragPolarDatabase` provide
strongly typed equivalents of the OpenAP property/data access used by the
performance models.

The .NET implementation deliberately does **not** embed upstream model data into
the core assembly. Callers point the databases at an OpenAP data directory.

### Thrust

`ThrustModel` ports the simplified two-shaft turbofan model from
`openap/thrust.py`, including:

- takeoff thrust
- climb thrust
- cruise thrust
- descent-idle approximation
- aircraft/engine compatibility validation

Its public API follows Python OpenAP units: knots, feet, ft/min and newtons.

### Fuel flow

`FuelFlowModel` ports the active performance calculations from
`openap/fuel.py`:

- fuel flow at a requested total aircraft thrust
- takeoff fuel flow using `ThrustModel`
- en-route fuel flow using `DragModel` and the longitudinal force balance
- aircraft-specific fuel-model coefficients with the same engine scaling logic
  as Python OpenAP
- fallback to the upstream `default` fuel model when no aircraft-specific
  model is available

The public API follows Python OpenAP units: thrust in N, mass in kg, TAS in
knots, altitude in feet, vertical speed in ft/min, acceleration in m/s² and
fuel flow in kg/s.

The upstream `limit` argument on `enroute()` is currently not used by
OpenAP's implementation; the .NET port retains it for API parity and documents
that behavior.

### Drag

`DragModel` ports `openap/drag.py`:

- clean drag
- optional experimental wave drag
- flap drag increment
- landing-gear drag increment
- flap-dependent induced-drag adjustment
- drag-polar synonym handling

The public API follows Python OpenAP units: mass in kg, TAS in knots,
altitude in feet, vertical speed in ft/min, flap angle in degrees and drag in N.

Like upstream OpenAP, the drag calculation derives lift coefficient by assuming
the wing supports approximately aircraft weight:

```
L = m g cos(gamma)
CL = L / qS
```

That is appropriate for airborne use but **not** for our future takeoff ground
roll. The ground-roll simulator should reuse the polar coefficients while
calculating lift from angle of attack/configuration and accounting for wheel
normal force separately.

## Compatibility strategy

Reference-value generators under `reference/python/` exercise the original
Python implementation, while xUnit tests assert corresponding .NET results.
New ports should follow this pattern so formula changes cannot silently break
OpenAP parity.

The .NET library is marked AOT-compatible and trimmable. Runtime reflection,
dynamic code generation and reflection-based serialization should be avoided
in hot simulation paths.

## Licensing

The upstream source code is licensed under LGPL-3.0. The `openap/data`
directory carries a separate GPL-3.0 license. Keeping the model files external
to `OpenAP.dll` makes that boundary explicit. Packaging or redistributing
those data files should continue to comply with their own license.
