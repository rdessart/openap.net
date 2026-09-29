using OpenAP.Performance;
using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class FuelFlowModelTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    private static FuelFlowModel CreateA320(
        string? engineName = null)
    {
        var aircraft =
            AircraftDatabase.FromOpenApDataDirectory(DataDirectory);
        var engines =
            EngineDatabase.FromOpenApDataDirectory(DataDirectory);
        var polars =
            DragPolarDatabase.FromOpenApDataDirectory(DataDirectory);
        var fuelModels =
            FuelModelDatabase.FromOpenApDataDirectory(DataDirectory);

        return new FuelFlowModel(
            "A320",
            aircraft,
            engines,
            polars,
            fuelModels,
            engineName);
    }

    [Fact]
    public void FuelModel_A320_MatchesUpstreamCoefficients()
    {
        var database =
            FuelModelDatabase.FromOpenApDataDirectory(DataDirectory);

        var model = database.Resolve("A320");

        Assert.Equal("A320", model.TypeCode);
        Assert.Equal("CFM56-5B4/P", model.EngineType);
        Assert.Equal(1.0453208160586924, model.C1);
        Assert.Equal(2.3633720747416573, model.C2);
        Assert.Equal(1.2378127479131922, model.C3);
    }

    [Fact]
    public void A320_DefaultEngine_UsesReferenceEngineScaling()
    {
        var fuel = CreateA320();

        Assert.Equal("CFM56-5B4", fuel.EngineName);
        Assert.Equal("A320", fuel.FuelModelTypeCode);

        AssertClose(
            1.030035335689046,
            fuel.FuelScale,
            1e-15);
    }

    [Theory]
    [InlineData(0.0, 0.17326431508278073)]
    [InlineData(50_000.0, 1.0309944492235337)]
    [InlineData(100_000.0, 1.75775391956613)]
    [InlineData(200_000.0, 2.146425978261751)]
    public void AtThrust_MatchesPythonOpenApReference(
        double thrustNewton,
        double expectedKgPerSecond)
    {
        var fuel = CreateA320();

        AssertClose(
            expectedKgPerSecond,
            fuel.AtThrust(thrustNewton),
            1e-12);
    }

    [Theory]
    [InlineData(0.0, 0.0, 1.0, 2.1528123677630226)]
    [InlineData(100.0, 0.0, 1.0, 2.1467180381321667)]
    [InlineData(150.0, 0.0, 1.0, 2.138155063136508)]
    [InlineData(150.0, 0.0, 0.5, 1.069077531568254)]
    public void Takeoff_MatchesPythonOpenApReference(
        double tasKnots,
        double altitudeFeet,
        double throttle,
        double expectedKgPerSecond)
    {
        var fuel = CreateA320();

        AssertClose(
            expectedKgPerSecond,
            fuel.Takeoff(
                tasKnots,
                altitudeFeet,
                throttle),
            1e-12);
    }

    [Theory]
    [InlineData(65_000.0, 250.0, 10_000.0, 0.0, 0.0, 0.716411227102079)]
    [InlineData(65_000.0, 250.0, 10_000.0, 1500.0, 0.0, 1.3935950743009713)]
    [InlineData(65_000.0, 250.0, 10_000.0, 0.0, 0.5, 1.3128776933109878)]
    [InlineData(65_000.0, 450.0, 35_000.0, 0.0, 0.0, 0.7462777499574433)]
    public void Enroute_MatchesPythonOpenApReference(
        double massKg,
        double tasKnots,
        double altitudeFeet,
        double verticalSpeedFeetPerMinute,
        double accelerationMetersPerSecondSquared,
        double expectedKgPerSecond)
    {
        var fuel = CreateA320();

        AssertClose(
            expectedKgPerSecond,
            fuel.Enroute(
                massKg,
                tasKnots,
                altitudeFeet,
                verticalSpeedFeetPerMinute,
                accelerationMetersPerSecondSquared),
            1e-11);
    }

    [Fact]
    public void ReferenceEngine_DoesNotApplyScaling()
    {
        var fuel = CreateA320("CFM56-5B4/P");

        AssertClose(1.0, fuel.FuelScale, 1e-15);
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
