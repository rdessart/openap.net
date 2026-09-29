using OpenAP.Aero;
using OpenAP.Properties;

namespace OpenAP.Performance;

/// <summary>
/// OpenAP emission model based on the ICAO engine emissions databank.
///
/// Inputs intentionally follow Python OpenAP:
/// - total aircraft fuel flow in kg/s
/// - TAS in knots
/// - altitude in feet
/// - temperature deviation in K/degC
/// Results are total aircraft emissions in g/s.
/// </summary>
public sealed class EmissionModel
{
    private readonly EngineDefinition _engine;
    private readonly int _engineCount;

    public string AircraftIcaoCode { get; }
    public string EngineName => _engine.Name;

    public EmissionModel(
        string aircraftIcaoCode,
        AircraftDatabase aircraftDatabase,
        EngineDatabase engineDatabase,
        string? engineName = null,
        bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);
        ArgumentNullException.ThrowIfNull(aircraftDatabase);
        ArgumentNullException.ThrowIfNull(engineDatabase);

        var aircraft =
            aircraftDatabase.Get(aircraftIcaoCode, useSynonym);

        var selectedEngineName =
            engineName ?? aircraft.Engines.DefaultEngine;

        _engine = engineDatabase.Get(selectedEngineName);
        _engineCount = aircraft.Engines.Number;

        AircraftIcaoCode = aircraft.IcaoCode;
    }

    /// <summary>
    /// CO2 emission rate in g/s.
    /// </summary>
    public static double CarbonDioxide(double totalFuelFlowKgPerSecond)
        => totalFuelFlowKgPerSecond * 3160.0;

    /// <summary>
    /// H2O emission rate in g/s.
    /// </summary>
    public static double Water(double totalFuelFlowKgPerSecond)
        => totalFuelFlowKgPerSecond * 1230.0;

    /// <summary>
    /// Soot emission rate in g/s.
    /// </summary>
    public static double Soot(double totalFuelFlowKgPerSecond)
        => totalFuelFlowKgPerSecond * 0.03;

    /// <summary>
    /// SOx emission rate in g/s.
    /// </summary>
    public static double SulfurOxides(double totalFuelFlowKgPerSecond)
        => totalFuelFlowKgPerSecond * 1.2;

    /// <summary>
    /// NOx emission rate in g/s using Boeing Fuel Flow Method 2.
    /// </summary>
    public double NitrogenOxides(
        double totalFuelFlowKgPerSecond,
        double trueAirspeedKnots,
        double altitudeFeet = 0.0,
        double temperatureDeviationKelvin = 0.0)
    {
        var (fuelFlowSeaLevel, ratio) =
            FlightLevelToSeaLevel(
                totalFuelFlowKgPerSecond,
                trueAirspeedKnots,
                altitudeFeet,
                temperatureDeviationKelvin);

        var emissions = _engine.Emissions;
        var flows = _engine.FuelFlows;

        var noxSeaLevel = InterpolateClamped(
            fuelFlowSeaLevel,
            flows.IdleKgPerSecond,
            flows.ApproachKgPerSecond,
            flows.ClimboutKgPerSecond,
            flows.TakeoffKgPerSecond,
            emissions.NitrogenOxidesIdle,
            emissions.NitrogenOxidesApproach,
            emissions.NitrogenOxidesClimbout,
            emissions.NitrogenOxidesTakeoff);

        var omega =
            1e-3 *
            Math.Exp(
                -0.0001426 *
                (altitudeFeet - 12_900.0));

        var noxFlightLevel =
            noxSeaLevel *
            Math.Sqrt(1.0 / ratio) *
            Math.Exp(
                -19.0 *
                (omega - 0.00634));

        return noxFlightLevel *
               totalFuelFlowKgPerSecond;
    }

    /// <summary>
    /// CO emission rate in g/s using Boeing Fuel Flow Method 2.
    /// </summary>
    public double CarbonMonoxide(
        double totalFuelFlowKgPerSecond,
        double trueAirspeedKnots,
        double altitudeFeet = 0.0,
        double temperatureDeviationKelvin = 0.0)
    {
        var (fuelFlowSeaLevel, ratio) =
            FlightLevelToSeaLevel(
                totalFuelFlowKgPerSecond,
                trueAirspeedKnots,
                altitudeFeet,
                temperatureDeviationKelvin);

        var emissions = _engine.Emissions;
        var flows = _engine.FuelFlows;

        var coSeaLevel = InterpolateClamped(
            fuelFlowSeaLevel,
            flows.IdleKgPerSecond,
            flows.ApproachKgPerSecond,
            flows.ClimboutKgPerSecond,
            flows.TakeoffKgPerSecond,
            emissions.CarbonMonoxideIdle,
            emissions.CarbonMonoxideApproach,
            emissions.CarbonMonoxideClimbout,
            emissions.CarbonMonoxideTakeoff);

        return coSeaLevel *
               ratio *
               totalFuelFlowKgPerSecond;
    }

    /// <summary>
    /// Unburned hydrocarbon emission rate in g/s using Boeing Fuel Flow
    /// Method 2.
    /// </summary>
    public double Hydrocarbons(
        double totalFuelFlowKgPerSecond,
        double trueAirspeedKnots,
        double altitudeFeet = 0.0,
        double temperatureDeviationKelvin = 0.0)
    {
        var (fuelFlowSeaLevel, ratio) =
            FlightLevelToSeaLevel(
                totalFuelFlowKgPerSecond,
                trueAirspeedKnots,
                altitudeFeet,
                temperatureDeviationKelvin);

        var emissions = _engine.Emissions;
        var flows = _engine.FuelFlows;

        var hcSeaLevel = InterpolateClamped(
            fuelFlowSeaLevel,
            flows.IdleKgPerSecond,
            flows.ApproachKgPerSecond,
            flows.ClimboutKgPerSecond,
            flows.TakeoffKgPerSecond,
            emissions.HydrocarbonIdle,
            emissions.HydrocarbonApproach,
            emissions.HydrocarbonClimbout,
            emissions.HydrocarbonTakeoff);

        return hcSeaLevel *
               ratio *
               totalFuelFlowKgPerSecond;
    }

    private (double FuelFlowSeaLevel, double Ratio)
        FlightLevelToSeaLevel(
            double totalFuelFlowKgPerSecond,
            double trueAirspeedKnots,
            double altitudeFeet,
            double temperatureDeviationKelvin)
    {
        var altitudeMeters =
            altitudeFeet *
            AeroConstants.FootToMeter;

        var mach = Aero.Aero.TasToMach(
            trueAirspeedKnots *
            AeroConstants.KnotToMetersPerSecond,
            altitudeMeters,
            temperatureDeviationKelvin);

        var beta =
            Math.Exp(0.2 * mach * mach);

        var theta =
            (
                Aero.Aero.Temperature(
                    altitudeMeters,
                    temperatureDeviationKelvin) /
                288.15
            ) /
            beta;

        // Preserve upstream BFFM2 expression, which uses altitude in feet.
        var delta =
            Math.Pow(
                1.0 -
                0.0019812 *
                altitudeFeet /
                288.15,
                5.255876) /
            Math.Pow(beta, 3.5);

        var ratio =
            Math.Pow(theta, 3.3) /
            Math.Pow(delta, 1.02);

        var fuelFlowSeaLevel =
            (totalFuelFlowKgPerSecond / _engineCount) *
            Math.Pow(theta, 3.8) /
            delta *
            beta;

        return (fuelFlowSeaLevel, ratio);
    }

    /// <summary>
    /// Equivalent to numpy.interp for the four monotonically increasing
    /// ICAO fuel-flow points: linear interpolation with endpoint clamping.
    /// </summary>
    private static double InterpolateClamped(
        double x,
        double x0,
        double x1,
        double x2,
        double x3,
        double y0,
        double y1,
        double y2,
        double y3)
    {
        if (x <= x0)
            return y0;

        if (x >= x3)
            return y3;

        if (x <= x1)
            return Lerp(x, x0, x1, y0, y1);

        if (x <= x2)
            return Lerp(x, x1, x2, y1, y2);

        return Lerp(x, x2, x3, y2, y3);
    }

    private static double Lerp(
        double x,
        double x0,
        double x1,
        double y0,
        double y1)
    {
        var fraction =
            (x - x0) /
            (x1 - x0);

        return y0 +
               fraction *
               (y1 - y0);
    }
}
