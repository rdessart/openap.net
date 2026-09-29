namespace OpenAP.Properties;

public sealed record DragPolarDefinition(
    string IcaoCode,
    string AircraftName,
    DragPolarClean Clean,
    double GearDragCoefficient,
    DragPolarFlaps Flaps);

public readonly record struct DragPolarClean(
    double ZeroLiftCoefficient,
    double InducedDragFactor,
    double OswaldEfficiency);

public readonly record struct DragPolarFlaps(
    double TaperRatio,
    double ChordRatio,
    double AreaRatio);
