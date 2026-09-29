namespace OpenAP.Geography;

/// <summary>
/// Geographic and solar-position calculations ported from openap/geo.py.
/// </summary>
public static class Geo
{
    /// <summary>
    /// Average Earth radius used by OpenAP, in meters.
    /// </summary>
    public const double EarthMeanRadiusMeters = 6_371_000.0;

    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>
    /// Computes great-circle distance with the Haversine formula.
    /// </summary>
    public static double Distance(
        double latitude1Degrees,
        double longitude1Degrees,
        double latitude2Degrees,
        double longitude2Degrees,
        double altitudeMeters = 0.0)
    {
        var latitude1 = latitude1Degrees * DegreesToRadians;
        var longitude1 = longitude1Degrees * DegreesToRadians;
        var latitude2 = latitude2Degrees * DegreesToRadians;
        var longitude2 = longitude2Degrees * DegreesToRadians;

        var deltaLongitude = longitude2 - longitude1;
        var deltaLatitude = latitude2 - latitude1;

        var sinHalfLatitude =
            Math.Sin(deltaLatitude / 2.0);

        var sinHalfLongitude =
            Math.Sin(deltaLongitude / 2.0);

        var a =
            sinHalfLatitude * sinHalfLatitude +
            Math.Cos(latitude1) *
            Math.Cos(latitude2) *
            sinHalfLongitude *
            sinHalfLongitude;

        var centralAngle =
            2.0 *
            Math.Asin(Math.Sqrt(a));

        return centralAngle *
               (EarthMeanRadiusMeters + altitudeMeters);
    }

    /// <summary>
    /// Computes the initial great-circle bearing in degrees [0, 360).
    /// </summary>
    public static double Bearing(
        double latitude1Degrees,
        double longitude1Degrees,
        double latitude2Degrees,
        double longitude2Degrees)
    {
        var latitude1 = latitude1Degrees * DegreesToRadians;
        var longitude1 = longitude1Degrees * DegreesToRadians;
        var latitude2 = latitude2Degrees * DegreesToRadians;
        var longitude2 = longitude2Degrees * DegreesToRadians;

        var deltaLongitude =
            longitude2 - longitude1;

        var x =
            Math.Sin(deltaLongitude) *
            Math.Cos(latitude2);

        var y =
            Math.Cos(latitude1) *
            Math.Sin(latitude2) -
            Math.Sin(latitude1) *
            Math.Cos(latitude2) *
            Math.Cos(deltaLongitude);

        var initialBearing =
            Math.Atan2(x, y) *
            RadiansToDegrees;

        // Python uses fmod(initial_bearing + 360, 360).
        return (initialBearing + 360.0) % 360.0;
    }

    /// <summary>
    /// Computes a destination coordinate from start point, great-circle
    /// distance, and initial bearing.
    ///
    /// Longitude is intentionally not normalized, matching Python OpenAP.
    /// </summary>
    public static GeoPoint Destination(
        double latitudeDegrees,
        double longitudeDegrees,
        double distanceMeters,
        double bearingDegrees,
        double altitudeMeters = 0.0)
    {
        var latitude1 =
            latitudeDegrees *
            DegreesToRadians;

        var longitude1 =
            longitudeDegrees *
            DegreesToRadians;

        var bearing =
            bearingDegrees *
            DegreesToRadians;

        var angularDistance =
            distanceMeters /
            (EarthMeanRadiusMeters + altitudeMeters);

        var latitude2 =
            Math.Asin(
                Math.Sin(latitude1) *
                Math.Cos(angularDistance) +
                Math.Cos(latitude1) *
                Math.Sin(angularDistance) *
                Math.Cos(bearing));

        var longitude2 =
            longitude1 +
            Math.Atan2(
                Math.Sin(bearing) *
                Math.Sin(angularDistance) *
                Math.Cos(latitude1),
                Math.Cos(angularDistance) -
                Math.Sin(latitude1) *
                Math.Sin(latitude2));

        return new GeoPoint(
            latitude2 * RadiansToDegrees,
            longitude2 * RadiansToDegrees);
    }

    /// <summary>
    /// Alias matching Python's latlon() naming.
    /// </summary>
    public static GeoPoint LatLon(
        double latitudeDegrees,
        double longitudeDegrees,
        double distanceMeters,
        double bearingDegrees,
        double altitudeMeters = 0.0)
        => Destination(
            latitudeDegrees,
            longitudeDegrees,
            distanceMeters,
            bearingDegrees,
            altitudeMeters);

    /// <summary>
    /// Calculates solar zenith angle in degrees using the Spencer (1971)
    /// approximation used by OpenAP.
    ///
    /// The supplied DateTime components are interpreted as UTC clock fields,
    /// mirroring OpenAP's documented expectation that timestamps are UTC.
    /// </summary>
    public static double SolarZenithAngle(
        double latitudeDegrees,
        double longitudeDegrees,
        DateTime timestampUtc)
        => SolarZenithAngleCore(
            latitudeDegrees,
            longitudeDegrees,
            timestampUtc.DayOfYear,
            timestampUtc.Hour +
            timestampUtc.Minute / 60.0 +
            timestampUtc.Second / 3600.0);

    /// <summary>
    /// Calculates solar zenith angle for an offset-aware timestamp after
    /// conversion to UTC.
    /// </summary>
    public static double SolarZenithAngle(
        double latitudeDegrees,
        double longitudeDegrees,
        DateTimeOffset timestamp)
    {
        var utc = timestamp.UtcDateTime;

        return SolarZenithAngle(
            latitudeDegrees,
            longitudeDegrees,
            utc);
    }

    /// <summary>
    /// Calculates solar zenith angle from Unix seconds, matching the numeric
    /// timestamp path in Python OpenAP.
    /// </summary>
    public static double SolarZenithAngleFromUnixSeconds(
        double latitudeDegrees,
        double longitudeDegrees,
        double unixTimestampSeconds)
    {
        var timestamp =
            DateTimeOffset.UnixEpoch
                .AddSeconds(unixTimestampSeconds);

        return SolarZenithAngle(
            latitudeDegrees,
            longitudeDegrees,
            timestamp);
    }

    private static double SolarZenithAngleCore(
        double latitudeDegrees,
        double longitudeDegrees,
        int dayOfYear,
        double utcHour)
    {
        // Upstream intentionally divides by 365 even for leap years.
        var gamma =
            2.0 *
            Math.PI *
            (dayOfYear - 1) /
            365.0;

        var declination =
            0.006918 -
            0.399912 * Math.Cos(gamma) +
            0.070257 * Math.Sin(gamma) -
            0.006758 * Math.Cos(2.0 * gamma) +
            0.000907 * Math.Sin(2.0 * gamma) -
            0.002697 * Math.Cos(3.0 * gamma) +
            0.00148 * Math.Sin(3.0 * gamma);

        var equationOfTimeMinutes =
            229.18 *
            (
                0.000075 +
                0.001868 * Math.Cos(gamma) -
                0.032077 * Math.Sin(gamma) -
                0.014615 * Math.Cos(2.0 * gamma) -
                0.040849 * Math.Sin(2.0 * gamma)
            );

        var latitude =
            latitudeDegrees *
            DegreesToRadians;

        var timeOffsetMinutes =
            equationOfTimeMinutes +
            4.0 * longitudeDegrees;

        var solarTimeMinutes =
            utcHour * 60.0 +
            timeOffsetMinutes;

        var hourAngleDegrees =
            solarTimeMinutes / 4.0 -
            180.0;

        var hourAngle =
            hourAngleDegrees *
            DegreesToRadians;

        var cosineZenith =
            Math.Sin(latitude) *
            Math.Sin(declination) +
            Math.Cos(latitude) *
            Math.Cos(declination) *
            Math.Cos(hourAngle);

        cosineZenith =
            Math.Clamp(
                cosineZenith,
                -1.0,
                1.0);

        return Math.Acos(cosineZenith) *
               RadiansToDegrees;
    }
}
