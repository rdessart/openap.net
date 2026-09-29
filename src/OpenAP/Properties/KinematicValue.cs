namespace OpenAP.Properties;

/// <summary>
/// One statistical variable from an OpenAP WRAP kinematic dataset.
/// Values intentionally preserve the native units stored by WRAP.
/// </summary>
public sealed record KinematicValue(
    string Variable,
    string FlightPhase,
    string Name,
    double DefaultValue,
    double Minimum,
    double Maximum,
    string StatisticalModel,
    IReadOnlyList<double> StatisticalModelParameters);
