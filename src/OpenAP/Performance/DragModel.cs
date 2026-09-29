using OpenAP.Aero;
using OpenAP.Properties;

namespace OpenAP.Performance;

/// <summary>
/// OpenAP aircraft drag model.
///
/// Public inputs intentionally follow Python OpenAP:
/// - mass in kg
/// - TAS in knots
/// - altitude in feet
/// - vertical speed in feet per minute
/// - flap angle in degrees
/// - result in newtons
///
/// Note: like upstream OpenAP, this model assumes airborne lift equilibrium
/// when calculating CL. It should not be used unchanged for ground-roll
/// dynamics, where the landing gear carries part of the aircraft weight.
/// </summary>
public sealed class DragModel
{
    private readonly AircraftDefinition _aircraft;
    private readonly DragPolarDefinition _polar;

    public string AircraftIcaoCode => _aircraft.IcaoCode;
    public bool WaveDragEnabled { get; }

    public DragModel(
        string aircraftIcaoCode,
        AircraftDatabase aircraftDatabase,
        DragPolarDatabase dragPolarDatabase,
        bool waveDrag = false,
        bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);
        ArgumentNullException.ThrowIfNull(aircraftDatabase);
        ArgumentNullException.ThrowIfNull(dragPolarDatabase);

        _aircraft = aircraftDatabase.Get(aircraftIcaoCode, useSynonym);
        _polar = dragPolarDatabase.Get(aircraftIcaoCode, useSynonym);
        WaveDragEnabled = waveDrag;
    }

    /// <summary>
    /// Computes clean-configuration drag, optionally including OpenAP's
    /// experimental wave-drag correction.
    /// </summary>
    public double Clean(
        double massKg,
        double trueAirspeedKnots,
        double altitudeFeet,
        double verticalSpeedFeetPerMinute = 0.0,
        double temperatureDeviationKelvin = 0.0)
    {
        var cd0 = _polar.Clean.ZeroLiftCoefficient;
        var k = _polar.Clean.InducedDragFactor;

        if (WaveDragEnabled)
        {
            var mach = Aero.Aero.TasToMach(
                trueAirspeedKnots * AeroConstants.KnotToMetersPerSecond,
                altitudeFeet * AeroConstants.FootToMeter,
                temperatureDeviationKelvin);

            // Upstream drag.py intentionally evaluates this CL with vs=0.
            var (cl, _) = LiftCoefficientAndDynamicPressureArea(
                massKg,
                trueAirspeedKnots,
                altitudeFeet,
                0.0,
                temperatureDeviationKelvin);

            var sweepRadians =
                _aircraft.Wing.SweepDegrees * Math.PI / 180.0;

            var thicknessToChord =
                _aircraft.Wing.ThicknessToChordRatio ?? 0.12;

            var cosineSweep = Math.Cos(sweepRadians);
            const double kappa = 0.95;

            var criticalMach =
                kappa / cosineSweep
                - thicknessToChord / (cosineSweep * cosineSweep)
                - 0.1 * cl /
                  (cosineSweep * cosineSweep * cosineSweep)
                - 0.108;

            var deltaMach = Math.Max(mach - criticalMach, 0.0);
            var waveDragIncrement =
                20.0 *
                deltaMach *
                deltaMach *
                deltaMach *
                deltaMach;

            cd0 += waveDragIncrement;
        }

        return CalculateDrag(
            massKg,
            trueAirspeedKnots,
            altitudeFeet,
            cd0,
            k,
            verticalSpeedFeetPerMinute,
            temperatureDeviationKelvin);
    }

    /// <summary>
    /// Computes drag with flaps and optional landing gear extended.
    /// </summary>
    public double NonClean(
        double massKg,
        double trueAirspeedKnots,
        double altitudeFeet,
        double flapAngleDegrees,
        double verticalSpeedFeetPerMinute = 0.0,
        double temperatureDeviationKelvin = 0.0,
        bool landingGear = false)
    {
        var cd0 = _polar.Clean.ZeroLiftCoefficient;
        var k = _polar.Clean.InducedDragFactor;

        var flapRadians = flapAngleDegrees * Math.PI / 180.0;

        var flapDragIncrement =
            _polar.Flaps.TaperRatio *
            Math.Pow(_polar.Flaps.ChordRatio, 1.38) *
            _polar.Flaps.AreaRatio *
            Math.Pow(Math.Sin(flapRadians), 2.0);

        var gearDragIncrement = landingGear
            ? _aircraft.Limits.MaximumTakeoffMassKg *
              AeroConstants.Gravity /
              _aircraft.Wing.AreaSquareMeters *
              3.16e-5 *
              Math.Pow(
                  _aircraft.Limits.MaximumTakeoffMassKg,
                  -0.215)
            : 0.0;

        var totalCd0 =
            cd0 +
            flapDragIncrement +
            gearDragIncrement;

        var flapOswaldDelta =
            string.Equals(
                _aircraft.Engines.Mount,
                "rear",
                StringComparison.OrdinalIgnoreCase)
                ? 0.0046 * flapAngleDegrees
                : 0.0026 * flapAngleDegrees;

        var aspectRatio =
            _aircraft.Wing.SpanMeters *
            _aircraft.Wing.SpanMeters /
            _aircraft.Wing.AreaSquareMeters;

        var totalK =
            1.0 /
            (1.0 / k +
             Math.PI * aspectRatio * flapOswaldDelta);

        return CalculateDrag(
            massKg,
            trueAirspeedKnots,
            altitudeFeet,
            totalCd0,
            totalK,
            verticalSpeedFeetPerMinute,
            temperatureDeviationKelvin);
    }

    private double CalculateDrag(
        double massKg,
        double trueAirspeedKnots,
        double altitudeFeet,
        double cd0,
        double k,
        double verticalSpeedFeetPerMinute,
        double temperatureDeviationKelvin)
    {
        var (cl, dynamicPressureArea) =
            LiftCoefficientAndDynamicPressureArea(
                massKg,
                trueAirspeedKnots,
                altitudeFeet,
                verticalSpeedFeetPerMinute,
                temperatureDeviationKelvin);

        var cd = cd0 + k * cl * cl;

        return cd * dynamicPressureArea;
    }

    private (double LiftCoefficient, double DynamicPressureArea)
        LiftCoefficientAndDynamicPressureArea(
            double massKg,
            double trueAirspeedKnots,
            double altitudeFeet,
            double verticalSpeedFeetPerMinute,
            double temperatureDeviationKelvin)
    {
        var speedMetersPerSecond =
            trueAirspeedKnots *
            AeroConstants.KnotToMetersPerSecond;

        var altitudeMeters =
            altitudeFeet *
            AeroConstants.FootToMeter;

        var verticalSpeedMetersPerSecond =
            verticalSpeedFeetPerMinute *
            AeroConstants.FootPerMinuteToMetersPerSecond;

        var flightPathAngle =
            Math.Atan2(
                verticalSpeedMetersPerSecond,
                speedMetersPerSecond);

        var density =
            Aero.Aero.Density(
                altitudeMeters,
                temperatureDeviationKelvin);

        var dynamicPressureArea =
            0.5 *
            density *
            speedMetersPerSecond *
            speedMetersPerSecond *
            _aircraft.Wing.AreaSquareMeters;

        dynamicPressureArea =
            Math.Max(dynamicPressureArea, 1e-3);

        var lift =
            massKg *
            AeroConstants.Gravity *
            Math.Cos(flightPathAngle);

        var liftCoefficient =
            lift / dynamicPressureArea;

        return (liftCoefficient, dynamicPressureArea);
    }
}
