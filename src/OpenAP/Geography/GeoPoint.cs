namespace OpenAP.Geography;

/// <summary>
/// Geographic coordinate in decimal degrees.
/// </summary>
public readonly record struct GeoPoint(
    double LatitudeDegrees,
    double LongitudeDegrees);
