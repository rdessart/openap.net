namespace OpenAP.Properties;

public sealed record EngineDefinition(
    string Uid,
    string Name,
    string Manufacturer,
    string Type,
    double BypassRatio,
    double PressureRatio,
    double MaximumThrustNewton,
    EngineEmissionIndices Emissions,
    EngineFuelFlows FuelFlows,
    double LandingTakeoffFuelKg,
    double? CruiseThrustNewton,
    double? CruiseSpecificFuelConsumption,
    double? CruiseMach,
    double? CruiseAltitudeFeet,
    double FuelCorrectionSlope);

public readonly record struct EngineEmissionIndices(
    double HydrocarbonTakeoff,
    double HydrocarbonClimbout,
    double HydrocarbonApproach,
    double HydrocarbonIdle,
    double CarbonMonoxideTakeoff,
    double CarbonMonoxideClimbout,
    double CarbonMonoxideApproach,
    double CarbonMonoxideIdle,
    double NitrogenOxidesTakeoff,
    double NitrogenOxidesClimbout,
    double NitrogenOxidesApproach,
    double NitrogenOxidesIdle);

public readonly record struct EngineFuelFlows(
    double TakeoffKgPerSecond,
    double ClimboutKgPerSecond,
    double ApproachKgPerSecond,
    double IdleKgPerSecond);
