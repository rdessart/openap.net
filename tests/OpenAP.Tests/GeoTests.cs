using OpenAP.Geography;
using Xunit;
using AeroModel = OpenAP.Aero.Aero;

namespace OpenAP.Tests;

public sealed class GeoTests
{
    [Fact]
    public void Distance_LondonToParis_MatchesPythonOpenApReference()
    {
        var distance = Geo.Distance(
            51.5,
            -0.1,
            48.85,
            2.35);

        AssertClose(
            342_400.7470589816,
            distance,
            1e-9);
    }

    [Fact]
    public void Distance_WithAltitude_UsesLargerSphereRadius()
    {
        var ground = Geo.Distance(
            51.5,
            -0.1,
            48.85,
            2.35);

        var atAltitude = Geo.Distance(
            51.5,
            -0.1,
            48.85,
            2.35,
            altitudeMeters: 10_000.0);

        AssertClose(
            342_938.183485067,
            atAltitude,
            1e-9);

        Assert.True(atAltitude > ground);
    }

    [Fact]
    public void Distance_SamePoint_IsZero()
    {
        AssertClose(
            0.0,
            Geo.Distance(
                51.5,
                -0.1,
                51.5,
                -0.1),
            0.0);
    }

    [Fact]
    public void Bearing_LondonToParis_MatchesPythonOpenApReference()
    {
        var bearing = Geo.Bearing(
            51.5,
            -0.1,
            48.85,
            2.35);

        AssertClose(
            148.42264325580805,
            bearing,
            1e-12);
    }

    [Theory]
    [InlineData(0.0, 0.0, 10.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, 0.0, 10.0, 90.0)]
    [InlineData(10.0, 0.0, 0.0, 0.0, 180.0)]
    [InlineData(0.0, 10.0, 0.0, 0.0, 270.0)]
    public void Bearing_CardinalDirections_MatchOpenAp(
        double lat1,
        double lon1,
        double lat2,
        double lon2,
        double expectedDegrees)
    {
        AssertClose(
            expectedDegrees,
            Geo.Bearing(
                lat1,
                lon1,
                lat2,
                lon2),
            1e-12);
    }

    [Fact]
    public void Destination_North_MatchesPythonOpenApReference()
    {
        var point = Geo.Destination(
            0.0,
            0.0,
            111_000.0,
            0.0);

        AssertClose(
            0.9982469825697909,
            point.LatitudeDegrees,
            1e-15);

        AssertClose(
            0.0,
            point.LongitudeDegrees,
            1e-15);
    }

    [Fact]
    public void Destination_East_MatchesPythonOpenApReference()
    {
        var point = Geo.LatLon(
            0.0,
            0.0,
            111_000.0,
            90.0);

        AssertClose(
            6.112190622587783e-17,
            point.LatitudeDegrees,
            1e-28);

        AssertClose(
            0.9982469825697909,
            point.LongitudeDegrees,
            1e-15);
    }

    [Fact]
    public void Destination_RoundTripDistance_IsConsistent()
    {
        var point = Geo.Destination(
            51.5,
            -0.1,
            100_000.0,
            45.0);

        AssertClose(
            52.131403955033484,
            point.LatitudeDegrees,
            1e-12);

        AssertClose(
            0.9359570588338134,
            point.LongitudeDegrees,
            1e-12);

        AssertClose(
            100_000.0,
            Geo.Distance(
                51.5,
                -0.1,
                point.LatitudeDegrees,
                point.LongitudeDegrees),
            1e-8);
    }

    [Theory]
    [InlineData(2024, 3, 20, 12, 0, 0, 0.0, 0.0, 1.9656286513718)]
    [InlineData(2024, 6, 21, 0, 0, 0, 45.0, 0.0, 111.54352049087349)]
    [InlineData(2024, 6, 21, 12, 0, 0, 23.44, 0.0, 0.3552531701818835)]
    public void SolarZenithAngle_MatchesPythonOpenApReference(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        double latitudeDegrees,
        double longitudeDegrees,
        double expectedDegrees)
    {
        var timestamp =
            new DateTime(
                year,
                month,
                day,
                hour,
                minute,
                second,
                DateTimeKind.Utc);

        var result =
            Geo.SolarZenithAngle(
                latitudeDegrees,
                longitudeDegrees,
                timestamp);

        AssertClose(
            expectedDegrees,
            result,
            1e-12);
    }

    [Fact]
    public void SolarZenithAngle_UnixTimestampMatchesDateTime()
    {
        var timestamp =
            new DateTimeOffset(
                2024,
                3,
                20,
                12,
                0,
                0,
                TimeSpan.Zero);

        var fromDate =
            Geo.SolarZenithAngle(
                0.0,
                0.0,
                timestamp);

        var fromUnix =
            Geo.SolarZenithAngleFromUnixSeconds(
                0.0,
                0.0,
                timestamp.ToUnixTimeSeconds());

        AssertClose(
            fromDate,
            fromUnix,
            1e-15);
    }

    [Fact]
    public void Aero_GeographicWrappersMatchGeoModule()
    {
        AssertClose(
            Geo.Distance(
                51.5,
                -0.1,
                48.85,
                2.35),
            AeroModel.Distance(
                51.5,
                -0.1,
                48.85,
                2.35),
            0.0);

        AssertClose(
            Geo.Bearing(
                51.5,
                -0.1,
                48.85,
                2.35),
            AeroModel.Bearing(
                51.5,
                -0.1,
                48.85,
                2.35),
            0.0);

        Assert.Equal(
            Geo.LatLon(
                0.0,
                0.0,
                111_000.0,
                0.0),
            AeroModel.LatLon(
                0.0,
                0.0,
                111_000.0,
                0.0));
    }

    private static void AssertClose(
        double expected,
        double actual,
        double absoluteTolerance)
    {
        Assert.InRange(
            Math.Abs(expected - actual),
            0.0,
            absoluteTolerance);
    }
}
