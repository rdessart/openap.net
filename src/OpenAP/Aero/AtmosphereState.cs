namespace OpenAP.Aero;

/// <summary>
/// ISA atmospheric state expressed in SI units.
/// </summary>
/// <param name="PressurePascal">Static pressure in Pa.</param>
/// <param name="DensityKgPerCubicMeter">Air density in kg/m³.</param>
/// <param name="TemperatureKelvin">Static temperature in K.</param>
public readonly record struct AtmosphereState(
    double PressurePascal,
    double DensityKgPerCubicMeter,
    double TemperatureKelvin);
