namespace OpenAP.Properties;

/// <summary>
/// Empirical OpenAP fuel-flow model coefficients.
/// </summary>
public readonly record struct FuelModelDefinition(
    string TypeCode,
    string EngineType,
    double C1,
    double C2,
    double C3);
