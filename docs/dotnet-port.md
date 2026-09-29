# OpenAP.NET port

This repository is a fork of OpenAP. The original Python implementation remains
in place as the behavioral reference while the .NET implementation is developed
under `src/`.

## Initial scope

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

## Compatibility strategy

Reference values are generated from the Python implementation and asserted by
the xUnit project in `tests/OpenAP.Tests`. New ports should follow the same
pattern so formula changes cannot silently break OpenAP parity.

The .NET library is marked AOT-compatible and trimmable. Runtime reflection,
dynamic code generation and runtime YAML/CSV parsing should be avoided in hot
simulation paths.

## Licensing

The upstream source code is licensed under LGPL-3.0. The `openap/data`
directory carries a separate GPL-3.0 license. Keep those licensing boundaries
in mind when deciding how model data will be packaged by a future NuGet
release.
