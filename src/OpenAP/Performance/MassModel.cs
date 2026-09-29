using OpenAP.Properties;

namespace OpenAP.Performance;

/// <summary>
/// Aircraft mass estimation helpers ported from openap/mass.py.
/// </summary>
public static class MassModel
{
    /// <summary>
    /// Fixed fuel density used by upstream OpenAP to convert maximum fuel
    /// capacity from liters to kilograms.
    /// </summary>
    public const double FuelDensityKgPerLiter = 0.8025;

    /// <summary>
    /// Estimates aircraft mass from route distance and payload load factor.
    ///
    /// Inputs and behavior intentionally match Python OpenAP:
    /// - distance is in nautical miles
    /// - loadFactor is nominally between 0 and 1, but is not clamped
    /// - range fraction is clipped to [0.2, 1.0]
    /// - result is kg unless fraction=true
    /// </summary>
    public static double FromRange(
        string aircraftIcaoCode,
        double distanceNauticalMiles,
        AircraftDatabase aircraftDatabase,
        double loadFactor = 0.8,
        bool fraction = false,
        bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);
        ArgumentNullException.ThrowIfNull(aircraftDatabase);

        var aircraft =
            aircraftDatabase.Get(aircraftIcaoCode, useSynonym);

        return FromRange(
            aircraft,
            distanceNauticalMiles,
            loadFactor,
            fraction);
    }

    /// <summary>
    /// Estimates aircraft mass using an already loaded aircraft definition.
    /// </summary>
    public static double FromRange(
        AircraftDefinition aircraft,
        double distanceNauticalMiles,
        double loadFactor = 0.8,
        bool fraction = false)
    {
        ArgumentNullException.ThrowIfNull(aircraft);

        var rangeFraction =
            distanceNauticalMiles /
            aircraft.Cruise.RangeNauticalMiles;

        rangeFraction =
            Math.Clamp(rangeFraction, 0.2, 1.0);

        var maximumFuelMassKg =
            aircraft.Limits.MaximumFuelCapacityLiters *
            FuelDensityKgPerLiter;

        var fuelMassKg =
            rangeFraction *
            maximumFuelMassKg;

        var payloadMassKg =
            (
                aircraft.Limits.MaximumTakeoffMassKg -
                maximumFuelMassKg -
                aircraft.Limits.OperatingEmptyMassKg
            ) *
            loadFactor;

        var massKg =
            aircraft.Limits.OperatingEmptyMassKg +
            fuelMassKg +
            payloadMassKg;

        return fraction
            ? massKg / aircraft.Limits.MaximumTakeoffMassKg
            : massKg;
    }
}
