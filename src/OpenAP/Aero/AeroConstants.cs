namespace OpenAP.Aero;

/// <summary>
/// Physical and unit conversion constants used by OpenAP.
/// Values intentionally match the Python reference implementation.
/// </summary>
public static class AeroConstants
{
    public const double KnotToMetersPerSecond = 0.514444;
    public const double FootToMeter = 0.3048;
    public const double FootPerMinuteToMetersPerSecond = 0.00508;
    public const double InchToMeter = 0.0254;
    public const double SquareFootToSquareMeter = 0.09290304;
    public const double NauticalMileToMeter = 1852.0;
    public const double PoundToKilogram = 0.453592;

    public const double Gravity = 9.80665;
    public const double GasConstant = 287.05287;
    public const double SeaLevelPressure = 101325.0;
    public const double SeaLevelDensity = 1.225;
    public const double SeaLevelTemperature = 288.15;

    public const double HeatCapacityRatio = 1.40;
    public const double TemperatureLapseRate = -0.0065;
    public const double EarthMeanRadius = 6_371_000.0;
    public const double SeaLevelSpeedOfSound = 340.293988;
}
