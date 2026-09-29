using OpenAP.Aero;
using OpenAP.Properties;

namespace OpenAP.Performance;

/// <summary>
/// Simplified two-shaft turbofan thrust model ported from OpenAP thrust.py.
///
/// Public inputs intentionally follow the Python API:
/// - TAS in knots
/// - altitude in feet
/// - rate of climb in feet per minute
/// - temperature deviation in K/degC
/// Results are total aircraft thrust in newtons.
/// </summary>
public sealed class ThrustModel
{
    private readonly double _cruiseAltitudeFeet;
    private readonly double _engineBypassRatio;
    private readonly double _engineMaximumThrustNewton;
    private readonly int _engineCount;
    private readonly double _cruiseMach;
    private readonly double _engineCruiseThrustNewton;

    public string AircraftIcaoCode { get; }
    public string EngineName { get; }

    public ThrustModel(
        string aircraftIcaoCode,
        AircraftDatabase aircraftDatabase,
        EngineDatabase engineDatabase,
        string? engineName = null,
        bool forceEngine = false,
        bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);
        ArgumentNullException.ThrowIfNull(aircraftDatabase);
        ArgumentNullException.ThrowIfNull(engineDatabase);

        var aircraft = aircraftDatabase.Get(aircraftIcaoCode, useSynonym);
        var selectedEngineName = engineName ?? aircraft.Engines.DefaultEngine;
        var engine = engineDatabase.Get(selectedEngineName);

        if (!forceEngine &&
            !aircraft.Engines.Options.Any(
                option => engine.Name.Contains(option, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                $"Engine '{selectedEngineName}' and aircraft '{aircraftIcaoCode}' mismatch. " +
                $"Available engines: {string.Join(", ", aircraft.Engines.Options)}.",
                nameof(engineName));
        }

        AircraftIcaoCode = aircraft.IcaoCode;
        EngineName = engine.Name;

        _cruiseAltitudeFeet =
            aircraft.Cruise.HeightMeters / AeroConstants.FootToMeter;

        _engineBypassRatio = engine.BypassRatio;
        _engineMaximumThrustNewton = engine.MaximumThrustNewton;
        _engineCount = aircraft.Engines.Number;

        if (engine.CruiseMach is > 0.0 &&
            engine.CruiseThrustNewton is > 0.0)
        {
            _cruiseMach = engine.CruiseMach.Value;
            _engineCruiseThrustNewton = engine.CruiseThrustNewton.Value;
        }
        else
        {
            _cruiseMach = aircraft.Cruise.Mach;
            _engineCruiseThrustNewton =
                0.2 * _engineMaximumThrustNewton + 890.0;
        }
    }

    /// <summary>
    /// Calculates total takeoff thrust in newtons.
    /// </summary>
    public double Takeoff(
        double trueAirspeedKnots,
        double altitudeFeet = 0.0,
        double temperatureDeviationKelvin = 0.0)
    {
        // OpenAP intentionally evaluates flight Mach for takeoff at zero altitude.
        var mach = Aero.Aero.TasToMach(
            trueAirspeedKnots * AeroConstants.KnotToMetersPerSecond,
            0.0,
            temperatureDeviationKelvin);

        var gasGeneratorFunction =
            0.0606 * _engineBypassRatio + 0.6337;

        var pressure = Aero.Aero.Pressure(
            altitudeFeet * AeroConstants.FootToMeter,
            temperatureDeviationKelvin);

        var pressureRatio = pressure / AeroConstants.SeaLevelPressure;

        var a =
            -0.4327 * pressureRatio * pressureRatio +
            1.3855 * pressureRatio +
            0.0472;

        var z =
            0.9106 * pressureRatio * pressureRatio * pressureRatio -
            1.7736 * pressureRatio * pressureRatio +
            1.8697 * pressureRatio;

        var x =
            0.1377 * pressureRatio * pressureRatio * pressureRatio -
            0.4374 * pressureRatio * pressureRatio +
            1.3003 * pressureRatio;

        var ratio =
            a -
            0.377 *
            (1.0 + _engineBypassRatio) /
            Math.Sqrt(
                (1.0 + 0.82 * _engineBypassRatio) *
                gasGeneratorFunction) *
            z *
            mach +
            (0.23 + 0.19 * Math.Sqrt(_engineBypassRatio)) *
            x *
            mach *
            mach;

        return ratio *
               _engineMaximumThrustNewton *
               _engineCount;
    }

    /// <summary>
    /// Calculates cruise thrust by using the OpenAP climb model at zero ROC.
    /// </summary>
    public double Cruise(
        double trueAirspeedKnots,
        double altitudeFeet,
        double temperatureDeviationKelvin = 0.0)
        => Climb(
            trueAirspeedKnots,
            altitudeFeet,
            0.0,
            temperatureDeviationKelvin);

    /// <summary>
    /// Calculates total climb thrust in newtons.
    /// </summary>
    public double Climb(
        double trueAirspeedKnots,
        double altitudeFeet,
        double rateOfClimbFeetPerMinute,
        double temperatureDeviationKelvin = 0.0)
    {
        var roc = Math.Abs(rateOfClimbFeetPerMinute);
        var altitudeMeters =
            altitudeFeet * AeroConstants.FootToMeter;

        var tasKnots = Math.Max(10.0, trueAirspeedKnots);
        var tasMetersPerSecond =
            tasKnots * AeroConstants.KnotToMetersPerSecond;

        var mach = Aero.Aero.TasToMach(
            tasMetersPerSecond,
            altitudeMeters,
            temperatureDeviationKelvin);

        var calibratedAirspeed = Aero.Aero.TasToCas(
            tasMetersPerSecond,
            altitudeMeters,
            temperatureDeviationKelvin);

        var pressure = Aero.Aero.Pressure(
            altitudeMeters,
            temperatureDeviationKelvin);

        var pressure10k = Aero.Aero.Pressure(
            10_000.0 * AeroConstants.FootToMeter,
            temperatureDeviationKelvin);

        var pressureCruise = Aero.Aero.Pressure(
            _cruiseAltitudeFeet * AeroConstants.FootToMeter,
            temperatureDeviationKelvin);

        var cruiseThrust =
            _engineCruiseThrustNewton * _engineCount;

        var referenceCas = Aero.Aero.MachToCas(
            _cruiseMach,
            _cruiseAltitudeFeet * AeroConstants.FootToMeter,
            temperatureDeviationKelvin);

        // Segment 3: above 30,000 ft.
        var d = DFunction(mach / _cruiseMach);
        var b = Math.Pow(mach / _cruiseMach, -0.11);

        var segment3Ratio =
            d * Math.Log(pressure / pressureCruise) + b;

        // Segment 2: 10,000 to 30,000 ft.
        var casRatio = calibratedAirspeed / referenceCas;
        var a = Math.Pow(casRatio, -0.1);
        var n = NFunction(roc);

        var segment2Ratio =
            a *
            Math.Pow(
                pressure / pressureCruise,
                -0.355 * casRatio + n);

        // Segment 1: below 10,000 ft.
        var thrust10k =
            cruiseThrust *
            a *
            Math.Pow(
                pressure10k / pressureCruise,
                -0.355 * casRatio + n);

        var m = MFunction(casRatio, roc);

        var segment1Ratio =
            m * (pressure / pressureCruise) +
            (thrust10k / cruiseThrust -
             m * (pressure10k / pressureCruise));

        var ratio = altitudeFeet > 30_000.0
            ? segment3Ratio
            : altitudeFeet > 10_000.0
                ? segment2Ratio
                : segment1Ratio;

        return ratio * cruiseThrust;
    }

    /// <summary>
    /// OpenAP idle-thrust approximation: 7% of available takeoff thrust.
    /// </summary>
    public double DescentIdle(
        double trueAirspeedKnots,
        double altitudeFeet,
        double temperatureDeviationKelvin = 0.0)
        => 0.07 *
           Takeoff(
               trueAirspeedKnots,
               altitudeFeet,
               temperatureDeviationKelvin);

    private static double DFunction(double machRatio)
        => -0.4204 * machRatio + 1.0824;

    private static double NFunction(double rateOfClimbFeetPerMinute)
        => 2.667e-05 * rateOfClimbFeetPerMinute + 0.8633;

    private static double MFunction(
        double casRatio,
        double rateOfClimbFeetPerMinute)
        => -1.2043e-1 * casRatio
           - 8.8889e-9 *
           rateOfClimbFeetPerMinute *
           rateOfClimbFeetPerMinute
           + 2.4444e-5 * rateOfClimbFeetPerMinute
           + 4.7379e-1;
}
