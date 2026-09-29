using OpenAP.Aero;
using OpenAP.Properties;

namespace OpenAP.Performance;

/// <summary>
/// OpenAP fuel-flow model based on the ICAO emission databank.
///
/// Public inputs intentionally follow Python OpenAP:
/// - thrust in N
/// - mass in kg
/// - TAS in knots
/// - altitude in feet
/// - vertical speed in ft/min
/// - acceleration in m/s²
/// - result in kg/s
/// </summary>
public sealed class FuelFlowModel
{
    private readonly AircraftDefinition _aircraft;
    private readonly EngineDefinition _engine;
    private readonly ThrustModel _thrust;
    private readonly DragModel _drag;
    private readonly FuelModelDefinition _fuelModel;
    private readonly double _scale;

    public string AircraftIcaoCode => _aircraft.IcaoCode;
    public string EngineName => _engine.Name;
    public string FuelModelTypeCode => _fuelModel.TypeCode;
    public double FuelScale => _scale;

    public FuelFlowModel(
        string aircraftIcaoCode,
        AircraftDatabase aircraftDatabase,
        EngineDatabase engineDatabase,
        DragPolarDatabase dragPolarDatabase,
        FuelModelDatabase fuelModelDatabase,
        string? engineName = null,
        bool forceEngine = false,
        bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);
        ArgumentNullException.ThrowIfNull(aircraftDatabase);
        ArgumentNullException.ThrowIfNull(engineDatabase);
        ArgumentNullException.ThrowIfNull(dragPolarDatabase);
        ArgumentNullException.ThrowIfNull(fuelModelDatabase);

        _aircraft =
            aircraftDatabase.Get(aircraftIcaoCode, useSynonym);

        var selectedEngineName =
            engineName ?? _aircraft.Engines.DefaultEngine;

        _engine = engineDatabase.Get(selectedEngineName);

        _thrust = new ThrustModel(
            aircraftIcaoCode,
            aircraftDatabase,
            engineDatabase,
            selectedEngineName,
            forceEngine,
            useSynonym);

        _drag = new DragModel(
            aircraftIcaoCode,
            aircraftDatabase,
            dragPolarDatabase,
            useSynonym: useSynonym);

        // Python FuelFlow indexes fuel_models.csv with the originally
        // requested aircraft code, not the resolved aircraft synonym.
        _fuelModel =
            fuelModelDatabase.Resolve(aircraftIcaoCode);

        if (_fuelModel.TypeCode.Equals(
                "default",
                StringComparison.OrdinalIgnoreCase))
        {
            _scale = _engine.FuelFlows.TakeoffKgPerSecond;
        }
        else if (!selectedEngineName.Equals(
                     _fuelModel.EngineType,
                     StringComparison.OrdinalIgnoreCase))
        {
            var referenceEngine =
                engineDatabase.Get(_fuelModel.EngineType);

            _scale =
                _engine.FuelFlows.TakeoffKgPerSecond /
                referenceEngine.FuelFlows.TakeoffKgPerSecond;
        }
        else
        {
            _scale = 1.0;
        }
    }

    /// <summary>
    /// Computes total aircraft fuel flow for a requested total net thrust.
    /// </summary>
    public double AtThrust(double totalAircraftThrustNewton)
    {
        var ratio =
            (totalAircraftThrustNewton / _aircraft.Engines.Number) /
            _engine.MaximumThrustNewton;

        // Preserve the smooth limiting expression from openap/fuel.py.
        ratio =
            (
                Math.Log(
                    1.0 +
                    Math.Exp(50.0 * (ratio - 0.03))) -
                Math.Log(
                    1.0 +
                    Math.Exp(45.0 * (ratio - 1.2)))
            ) /
            Math.Log(1.0 + Math.Exp(50.0)) +
            0.03;

        return EvaluateFuelFunction(ratio) *
               _aircraft.Engines.Number;
    }

    /// <summary>
    /// Computes takeoff fuel flow in kg/s.
    ///
    /// As in Python OpenAP, throttle scales fuel flow after full-thrust fuel
    /// flow has been evaluated; it does not first scale requested thrust.
    /// </summary>
    public double Takeoff(
        double trueAirspeedKnots,
        double altitudeFeet = 0.0,
        double throttle = 1.0)
    {
        var maximumThrust =
            _thrust.Takeoff(
                trueAirspeedKnots,
                altitudeFeet);

        return throttle * AtThrust(maximumThrust);
    }

    /// <summary>
    /// Computes fuel flow during climb, cruise, or descent from the
    /// longitudinal force balance used by OpenAP.
    /// </summary>
    public double Enroute(
        double massKg,
        double trueAirspeedKnots,
        double altitudeFeet,
        double verticalSpeedFeetPerMinute = 0.0,
        double accelerationMetersPerSecondSquared = 0.0,
        double temperatureDeviationKelvin = 0.0,
        bool limit = true)
    {
        // Retained for API parity. Upstream fuel.py currently does not use it.
        _ = limit;

        var drag = _drag.Clean(
            massKg,
            trueAirspeedKnots,
            altitudeFeet,
            verticalSpeedFeetPerMinute,
            temperatureDeviationKelvin);

        var flightPathAngle =
            Math.Atan2(
                verticalSpeedFeetPerMinute *
                AeroConstants.FootPerMinuteToMetersPerSecond,
                trueAirspeedKnots *
                AeroConstants.KnotToMetersPerSecond);

        // Preserve upstream's literal 9.81 rather than AeroConstants.Gravity.
        var requiredThrust =
            drag +
            massKg * 9.81 * Math.Sin(flightPathAngle) +
            massKg * accelerationMetersPerSecondSquared;

        return AtThrust(requiredThrust);
    }

    private double EvaluateFuelFunction(double thrustRatio)
        => _scale *
           (
               _fuelModel.C1 -
               Math.Exp(
                   -_fuelModel.C2 *
                   (
                       thrustRatio *
                       Math.Exp(_fuelModel.C3 * thrustRatio) -
                       Math.Log(_fuelModel.C1) /
                       _fuelModel.C2
                   ))
           );
}
