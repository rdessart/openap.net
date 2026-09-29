# OpenAP.NET port

This repository is a fork of OpenAP. The original Python implementation remains
in place as the behavioral reference while the .NET implementation is developed
under `src/`.

## Current scope

### Aeronautical calculations

The first porting milestone covers the numerical foundation in `openap/aero.py`:

- ISA pressure, density and temperature
- speed of sound
- ISA pressure altitude
- TAS/Mach conversion
- EAS/TAS conversion
- CAS/TAS conversion
- CAS/Mach conversion
- CAS/Mach crossover altitude

The .NET API uses the same SI-unit conventions as the Python implementation.
Unit conversion constants are exposed in `AeroConstants`.

### Aircraft and engine properties

`AircraftDatabase` and `EngineDatabase` provide strongly typed equivalents
of the relevant parts of `openap/prop.py`.

The .NET implementation deliberately does **not** embed the upstream model data
into the core assembly. Instead, callers point the databases at an OpenAP data
directory:

```csharp
var aircraft = AircraftDatabase
    .FromOpenApDataDirectory(dataDirectory)
    .Get("A320");

var engine = EngineDatabase
    .FromOpenApDataDirectory(dataDirectory)
    .Get(aircraft.Engines.DefaultEngine);
```

Aircraft YAML is read with a small schema-oriented parser and engine CSV with a
small CSV reader. Neither uses reflection or runtime code generation.

### Thrust

`ThrustModel` ports the simplified two-shaft turbofan model from
`openap/thrust.py`.

Its public API intentionally uses the same units as Python OpenAP:

- TAS: knots
- altitude: feet
- rate of climb: feet/min
- temperature deviation: K / degC
- thrust: newtons

Implemented operations:

- takeoff thrust
- climb thrust
- cruise thrust
- descent-idle approximation
- aircraft/engine compatibility validation

Example:

```csharp
var aircraftDb = AircraftDatabase.FromOpenApDataDirectory(dataDirectory);
var engineDb = EngineDatabase.FromOpenApDataDirectory(dataDirectory);

var thrust = new ThrustModel("A320", aircraftDb, engineDb);

double takeoffNewton = thrust.Takeoff(
    trueAirspeedKnots: 150,
    altitudeFeet: 0);
```

## Compatibility strategy

Reference values are generated from the Python implementation and asserted by
the xUnit project in `tests/OpenAP.Tests`. New ports should follow the same
pattern so formula changes cannot silently break OpenAP parity.

The .NET library is marked AOT-compatible and trimmable. Runtime reflection,
dynamic code generation and reflection-based YAML/CSV deserialization should be
avoided in hot simulation paths.

## Licensing

The upstream source code is licensed under LGPL-3.0. The `openap/data`
directory carries a separate GPL-3.0 license. Keeping the model files external
to `OpenAP.dll` makes that boundary explicit. Packaging or redistributing
those data files should continue to comply with their own license.
