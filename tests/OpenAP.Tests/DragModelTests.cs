using OpenAP.Performance;
using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class DragModelTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    private static DragModel CreateA320(bool waveDrag = false)
    {
        var aircraft =
            AircraftDatabase.FromOpenApDataDirectory(DataDirectory);

        var polars =
            DragPolarDatabase.FromOpenApDataDirectory(DataDirectory);

        return new DragModel(
            "A320",
            aircraft,
            polars,
            waveDrag: waveDrag);
    }

    [Theory]
    [InlineData(65_000.0, 250.0, 10_000.0, 0.0, 33_780.07848280061)]
    [InlineData(65_000.0, 250.0, 10_000.0, 1500.0, 33_720.32375742783)]
    [InlineData(65_000.0, 450.0, 35_000.0, 0.0, 35_264.235865247334)]
    public void Clean_MatchesOpenApReference(
        double massKg,
        double tasKnots,
        double altitudeFeet,
        double verticalSpeedFeetPerMinute,
        double expectedNewton)
    {
        var drag = CreateA320();

        var result = drag.Clean(
            massKg,
            tasKnots,
            altitudeFeet,
            verticalSpeedFeetPerMinute);

        AssertClose(expectedNewton, result, 1e-6);
    }

    [Fact]
    public void Clean_WithWaveDrag_MatchesOpenApReference()
    {
        var drag = CreateA320(waveDrag: true);

        var result = drag.Clean(
            massKg: 65_000.0,
            trueAirspeedKnots: 450.0,
            altitudeFeet: 35_000.0);

        AssertClose(35_486.26788052966, result, 1e-6);
    }

    [Theory]
    [InlineData(65_000.0, 160.0, 0.0, 10.0, false, 39_292.12840218437)]
    [InlineData(65_000.0, 150.0, 0.0, 15.0, true, 49_776.41581230293)]
    [InlineData(65_000.0, 140.0, 0.0, 30.0, true, 51_885.0684009727)]
    public void NonClean_MatchesOpenApReference(
        double massKg,
        double tasKnots,
        double altitudeFeet,
        double flapAngleDegrees,
        bool landingGear,
        double expectedNewton)
    {
        var drag = CreateA320();

        var result = drag.NonClean(
            massKg,
            tasKnots,
            altitudeFeet,
            flapAngleDegrees,
            landingGear: landingGear);

        AssertClose(expectedNewton, result, 1e-6);
    }

    [Fact]
    public void DragPolar_A320_MatchesUpstreamData()
    {
        var database =
            DragPolarDatabase.FromOpenApDataDirectory(DataDirectory);

        var polar = database.Get("A320");

        Assert.Equal("A320", polar.IcaoCode);
        Assert.Equal("Airbus A320", polar.AircraftName);
        Assert.Equal(0.018, polar.Clean.ZeroLiftCoefficient);
        Assert.Equal(0.039, polar.Clean.InducedDragFactor);
        Assert.Equal(0.799, polar.Clean.OswaldEfficiency);
        Assert.Equal(0.017, polar.GearDragCoefficient);
        Assert.Equal(0.900, polar.Flaps.TaperRatio);
        Assert.Equal(0.176, polar.Flaps.ChordRatio);
        Assert.Equal(0.170, polar.Flaps.AreaRatio);
    }

    [Fact]
    public void DragPolarSynonym_IsIndependentFromAircraftSynonym()
    {
        var database =
            DragPolarDatabase.FromOpenApDataDirectory(DataDirectory);

        Assert.Throws<KeyNotFoundException>(
            () => database.Get("A318"));

        var polar = database.Get("A318", useSynonym: true);

        Assert.Equal("A319", polar.IcaoCode);
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
