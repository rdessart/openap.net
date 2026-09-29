namespace OpenAP.Phases;

/// <summary>
/// Indices where OpenAP identifies major flight-phase boundaries.
/// </summary>
public sealed record FlightPhaseIndices(
    int? Takeoff,
    int? InitialClimb,
    int? Climb,
    int? Cruise,
    int? Descent,
    int? FinalApproach,
    int? Landing,
    int End)
{
    public IReadOnlyDictionary<string, int?> ToDictionary()
        => new Dictionary<string, int?>
        {
            ["TO"] = Takeoff,
            ["IC"] = InitialClimb,
            ["CL"] = Climb,
            ["CR"] = Cruise,
            ["DE"] = Descent,
            ["FA"] = FinalApproach,
            ["LD"] = Landing,
            ["END"] = End
        };
}
