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

### Geography

`OpenAP.Geography.Geo` ports the scalar geographic functionality from
`openap/geo.py`:

- Haversine great-circle distance on OpenAP's 6,371,000 m mean Earth radius
- initial great-circle bearing
- forward destination point from distance and bearing
- Spencer-1971 solar zenith angle
- altitude-adjusted spherical radius for distance/destination calculations
- UTC Unix-timestamp support for solar calculations

The geographic functions use degrees for latitude/longitude/bearing and meters
for distance/altitude, exactly as upstream. `Aero.Distance()`,
`Aero.Bearing()`, and `Aero.LatLon()` are retained as compatibility
wrappers because Python OpenAP still re-exports those functions from
`aero.py`.

The .NET port intentionally implements scalar math rather than reproducing
Python's NumPy/JAX/CasADi backend abstraction.

### Flight phases

`FlightPhaseModel` ports `openap/phase.py` without bringing NumPy or
scikit-fuzzy into the .NET runtime.

It includes:

- fuzzy phase labeling: `GND`, `CL`, `DE`, `CR`, `LVL`
- 60-second default aggregation windows
- takeoff / initial-climb segmentation
- climb, cruise and descent boundaries
- final-approach and landing detection
- phase-index extraction for `TO`, `IC`, `CL`, `CR`, `DE`, `FA`,
  `LD`, and `END`

Input units match upstream OpenAP: time in seconds, altitude in feet, TAS in
knots and rate of climb in ft/min.

The port intentionally preserves OpenAP's current window-loop semantics:
the highest numbered time bucket is not processed by `phaselabel()`, so that
bucket remains `NA`. This behavior is covered by parity tests rather than
silently corrected in the compatibility layer.

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

### Mass estimation

`MassModel.FromRange()` ports `openap/mass.py`. It estimates aircraft mass
from flight distance and payload load factor using the same OpenAP assumptions:

- range fraction is clipped to 0.2–1.0
- `mfc` is interpreted as fuel capacity in **liters**
- fuel volume is converted with the upstream fixed factor `0.8025 kg/L`
- payload capacity is derived from MTOW, OEW and maximum fuel mass
- the result can be returned either in kilograms or as a fraction of MTOW

The typed aircraft model now names `mfc` as
`MaximumFuelCapacityLiters` and cruise range as `RangeNauticalMiles` so
their units are explicit.

### Kinematic / WRAP

`WrapDatabase` and `WrapModel` port `openap/kinematic.py` and the WRAP
fixed-width datasets under `openap/data/wrap`.

The parser is dependency-free and preserves the complete statistical record for
each WRAP variable:

- default/optimal value
- minimum and maximum
- statistical model name
- statistical-model parameters
- variable key, flight phase and descriptive name

All 33 public accessors currently exposed by Python OpenAP's `WRAP` class are
available on `WrapModel`. Generic access through `GetVariable()` also makes
the remaining dataset rows available without adding a dedicated API method.

WRAP synonym resolution is enabled by default, matching Python OpenAP. The raw
WRAP units are intentionally preserved for parity: speeds are generally m/s,
ranges are km, altitude values are km, vertical rates are m/s, accelerations are
m/s², and Mach/angle values are dimensionless/degrees as represented by the
upstream dataset.

### Emissions

`EmissionModel` ports `openap/emission.py` and uses the typed ICAO engine
emission indices already exposed by `EngineDatabase`.

Implemented emissions are:

- CO₂
- H₂O
- soot
- SOx
- NOx
- CO
- unburned hydrocarbons (HC)

CO₂, H₂O, soot and SOx scale directly with total aircraft fuel flow. NOx, CO
and HC use the Boeing Fuel Flow Method 2 sea-level-equivalent correction and
the same piecewise-linear interpolation over idle, approach, climb-out and
takeoff engine data as Python OpenAP.

Public units match upstream OpenAP: total aircraft fuel flow in kg/s, TAS in
knots, altitude in feet, temperature deviation in K/°C and emission rate in
g/s.

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
