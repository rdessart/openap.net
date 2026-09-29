namespace OpenAP.Properties;

public sealed record AircraftDefinition(
    string IcaoCode,
    string Name,
    AircraftLimits Limits,
    PassengerCapacity Passengers,
    FuselageGeometry Fuselage,
    WingGeometry Wing,
    FlapGeometry Flaps,
    CruiseDefinition Cruise,
    EngineInstallation Engines,
    DragDefinition Drag,
    FuelDefinition Fuel);

public readonly record struct AircraftLimits(
    double MaximumTakeoffMassKg,
    double MaximumLandingMassKg,
    double OperatingEmptyMassKg,
    double MaximumFuelCapacityKg,
    double MaximumOperatingSpeedKnots,
    double MaximumOperatingMach,
    double CeilingMeters);

public readonly record struct PassengerCapacity(
    int Maximum,
    int LowDensity,
    int HighDensity);

public readonly record struct FuselageGeometry(
    double LengthMeters,
    double HeightMeters,
    double WidthMeters);

public readonly record struct WingGeometry(
    double AreaSquareMeters,
    double SpanMeters,
    double MeanAerodynamicChordMeters,
    double SweepDegrees,
    double? ThicknessToChordRatio);

public readonly record struct FlapGeometry(
    string Type,
    double AreaSquareMeters,
    double SpanRatio,
    double TaperRatio,
    double ChordRatio,
    double AreaRatio);

public sealed record EngineInstallation(
    string Type,
    string Mount,
    int Number,
    string DefaultEngine,
    IReadOnlyList<string> Options,
    IReadOnlyDictionary<string, string> VariantOptions);

public readonly record struct CruiseDefinition(
    double HeightMeters,
    double Mach,
    double Range);

public readonly record struct DragDefinition(
    double ZeroLiftCoefficient,
    double InducedDragFactor,
    double OswaldEfficiency,
    double GearDragIncrement);

public readonly record struct FuelDefinition(
    string Engine,
    double FuelCoefficient);
