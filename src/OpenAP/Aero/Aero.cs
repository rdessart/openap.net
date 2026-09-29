namespace OpenAP.Aero;

/// <summary>
/// Aeronautical atmosphere and airspeed calculations.
///
/// This implementation intentionally follows OpenAP's Python aero.py equations
/// so that the .NET port can be validated numerically against the reference
/// implementation.
/// </summary>
public static class Aero
{
    private const double TropopauseMeters = 11_000.0;
    private const double StratosphereBaseTemperatureKelvin = 216.65;
    private const double StratosphereDensityScaleHeightMeters = 6341.552161;
    private const double TroposphereDensityExponent = 4.256848030018761;

    /// <summary>
    /// Computes static pressure, density and temperature at an altitude.
    /// </summary>
    /// <param name="altitudeMeters">Geopotential altitude in meters.</param>
    /// <param name="temperatureDeviationKelvin">
    /// ISA temperature deviation in K. As in OpenAP, it is clamped to [-25, +15].
    /// </param>
    public static AtmosphereState Atmosphere(
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
    {
        var dT = Math.Clamp(temperatureDeviationKelvin, -25.0, 15.0);
        var shiftedSeaLevelTemperature = AeroConstants.SeaLevelTemperature + dT;

        var troposphereTemperature =
            shiftedSeaLevelTemperature + AeroConstants.TemperatureLapseRate * altitudeMeters;

        var stratosphereTemperature =
            StratosphereBaseTemperatureKelvin + dT;

        var temperature = Math.Max(troposphereTemperature, stratosphereTemperature);
        var heightAboveTropopause = Math.Max(0.0, altitudeMeters - TropopauseMeters);

        var troposphereDensity =
            AeroConstants.SeaLevelDensity *
            Math.Pow(
                temperature / shiftedSeaLevelTemperature,
                TroposphereDensityExponent);

        var density =
            troposphereDensity *
            Math.Exp(-heightAboveTropopause / StratosphereDensityScaleHeightMeters);

        var pressure =
            density *
            AeroConstants.GasConstant *
            temperature;

        return new AtmosphereState(pressure, density, temperature);
    }

    public static double Temperature(
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => Atmosphere(altitudeMeters, temperatureDeviationKelvin).TemperatureKelvin;

    public static double Pressure(
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => Atmosphere(altitudeMeters, temperatureDeviationKelvin).PressurePascal;

    public static double Density(
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => Atmosphere(altitudeMeters, temperatureDeviationKelvin).DensityKgPerCubicMeter;

    public static double SpeedOfSound(
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => Math.Sqrt(
            AeroConstants.HeatCapacityRatio *
            AeroConstants.GasConstant *
            Temperature(altitudeMeters, temperatureDeviationKelvin));

    /// <summary>
    /// Computes ISA altitude in meters for a pressure in Pa.
    /// </summary>
    public static double IsaAltitude(
        double pressurePascal,
        double temperatureDeviationKelvin = 0.0)
    {
        var shiftedSeaLevelTemperature =
            AeroConstants.SeaLevelTemperature + temperatureDeviationKelvin;

        var temperature =
            shiftedSeaLevelTemperature *
            Math.Pow(
                AeroConstants.SeaLevelPressure / pressurePascal,
                AeroConstants.TemperatureLapseRate *
                AeroConstants.GasConstant /
                AeroConstants.Gravity);

        var troposphereAltitude =
            (temperature - shiftedSeaLevelTemperature) /
            AeroConstants.TemperatureLapseRate;

        const double referencePressure = 22_630.0;

        var tropopauseTemperature =
            shiftedSeaLevelTemperature +
            AeroConstants.TemperatureLapseRate * TropopauseMeters;

        var stratosphereAltitude =
            -AeroConstants.GasConstant *
            tropopauseTemperature /
            AeroConstants.Gravity *
            Math.Log(pressurePascal / referencePressure) +
            TropopauseMeters;

        return pressurePascal > referencePressure
            ? troposphereAltitude
            : stratosphereAltitude;
    }

    public static double TasToMach(
        double trueAirspeedMetersPerSecond,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => trueAirspeedMetersPerSecond /
           SpeedOfSound(altitudeMeters, temperatureDeviationKelvin);

    public static double MachToTas(
        double mach,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => mach *
           SpeedOfSound(altitudeMeters, temperatureDeviationKelvin);

    public static double EasToTas(
        double equivalentAirspeedMetersPerSecond,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
    {
        var density = Density(altitudeMeters, temperatureDeviationKelvin);

        return equivalentAirspeedMetersPerSecond *
               Math.Sqrt(AeroConstants.SeaLevelDensity / density);
    }

    public static double TasToEas(
        double trueAirspeedMetersPerSecond,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
    {
        var density = Density(altitudeMeters, temperatureDeviationKelvin);

        return trueAirspeedMetersPerSecond *
               Math.Sqrt(density / AeroConstants.SeaLevelDensity);
    }

    public static double CasToTas(
        double calibratedAirspeedMetersPerSecond,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
    {
        var atmosphere = Atmosphere(altitudeMeters, temperatureDeviationKelvin);
        var vCas = calibratedAirspeedMetersPerSecond;

        var dynamicPressure =
            AeroConstants.SeaLevelPressure *
            (Math.Pow(
                 1.0 +
                 AeroConstants.SeaLevelDensity * vCas * vCas /
                 (7.0 * AeroConstants.SeaLevelPressure),
                 3.5) -
             1.0);

        return Math.Sqrt(
            7.0 *
            atmosphere.PressurePascal /
            atmosphere.DensityKgPerCubicMeter *
            (Math.Pow(
                 1.0 + dynamicPressure / atmosphere.PressurePascal,
                 2.0 / 7.0) -
             1.0));
    }

    public static double TasToCas(
        double trueAirspeedMetersPerSecond,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
    {
        var atmosphere = Atmosphere(altitudeMeters, temperatureDeviationKelvin);
        var vTas = trueAirspeedMetersPerSecond;

        var dynamicPressure =
            atmosphere.PressurePascal *
            (Math.Pow(
                 1.0 +
                 atmosphere.DensityKgPerCubicMeter * vTas * vTas /
                 (7.0 * atmosphere.PressurePascal),
                 3.5) -
             1.0);

        return Math.Sqrt(
            7.0 *
            AeroConstants.SeaLevelPressure /
            AeroConstants.SeaLevelDensity *
            (Math.Pow(
                 dynamicPressure / AeroConstants.SeaLevelPressure + 1.0,
                 2.0 / 7.0) -
             1.0));
    }

    public static double MachToCas(
        double mach,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => TasToCas(
            MachToTas(mach, altitudeMeters, temperatureDeviationKelvin),
            altitudeMeters,
            temperatureDeviationKelvin);

    public static double CasToMach(
        double calibratedAirspeedMetersPerSecond,
        double altitudeMeters,
        double temperatureDeviationKelvin = 0.0)
        => TasToMach(
            CasToTas(
                calibratedAirspeedMetersPerSecond,
                altitudeMeters,
                temperatureDeviationKelvin),
            altitudeMeters,
            temperatureDeviationKelvin);

    /// <summary>
    /// Computes the CAS/Mach crossover altitude in meters.
    /// </summary>
    public static double CrossoverAltitude(
        double calibratedAirspeedMetersPerSecond,
        double mach,
        double temperatureDeviationKelvin = 0.0)
    {
        var shiftedSeaLevelTemperature =
            AeroConstants.SeaLevelTemperature + temperatureDeviationKelvin;

        var safeMach = Math.Max(mach, 1e-4);

        var pressureRatio =
            (Math.Pow(
                 0.2 *
                 Math.Pow(
                     calibratedAirspeedMetersPerSecond /
                     AeroConstants.SeaLevelSpeedOfSound,
                     2.0) +
                 1.0,
                 3.5) -
             1.0) /
            (Math.Pow(0.2 * safeMach * safeMach + 1.0, 3.5) - 1.0);

        return shiftedSeaLevelTemperature /
               AeroConstants.TemperatureLapseRate *
               (Math.Pow(
                    pressureRatio,
                    -AeroConstants.GasConstant *
                    AeroConstants.TemperatureLapseRate /
                    AeroConstants.Gravity) -
                1.0);
    }
}
