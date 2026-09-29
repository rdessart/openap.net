using OpenAP.Performance;
using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class EmissionModelTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    private static EmissionModel CreateA320(
        string? engineName = null)
    {
        var aircraft =
            AircraftDatabase.FromOpenApDataDirectory(DataDirectory);
        var engines =
            EngineDatabase.FromOpenApDataDirectory(DataDirectory);

        return new EmissionModel(
            "A320",
            aircraft,
            engines,
            engineName);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 1580.0)]
    [InlineData(1.0, 3160.0)]
    [InlineData(2.0, 6320.0)]
    public void CarbonDioxide_MatchesOpenAp(
        double fuelFlowKgPerSecond,
        double expectedGramPerSecond)
    {
        AssertClose(
            expectedGramPerSecond,
            EmissionModel.CarbonDioxide(fuelFlowKgPerSecond),
            1e-12);
    }

    [Fact]
    public void DirectFuelProportionalEmissions_MatchOpenAp()
    {
        const double fuelFlow = 1.5;

        AssertClose(
            1845.0,
            EmissionModel.Water(fuelFlow),
            1e-12);

        AssertClose(
            0.045,
            EmissionModel.Soot(fuelFlow),
            1e-15);

        AssertClose(
            1.8,
            EmissionModel.SulfurOxides(fuelFlow),
            1e-15);
    }

    [Theory]
    [InlineData(2.0, 250.0, 0.0, 0.0, 49.57102893095223)]
    [InlineData(1.0, 250.0, 10_000.0, 0.0, 15.25577356126364)]
    [InlineData(0.7, 450.0, 35_000.0, 0.0, 8.819522994160566)]
    [InlineData(1.0, 250.0, 10_000.0, 15.0, 16.461311021737515)]
    public void NitrogenOxides_MatchesPythonOpenApReference(
        double fuelFlowKgPerSecond,
        double tasKnots,
        double altitudeFeet,
        double temperatureDeviationKelvin,
        double expectedGramPerSecond)
    {
        var emission = CreateA320();

        var result = emission.NitrogenOxides(
            fuelFlowKgPerSecond,
            tasKnots,
            altitudeFeet,
            temperatureDeviationKelvin);

        AssertClose(expectedGramPerSecond, result, 1e-10);
    }

    [Theory]
    [InlineData(2.0, 250.0, 0.0, 0.0, 1.0077431458560342)]
    [InlineData(1.0, 250.0, 10_000.0, 0.0, 1.910931762292317)]
    [InlineData(0.7, 450.0, 35_000.0, 0.0, 2.078890933058769)]
    [InlineData(1.0, 250.0, 10_000.0, 15.0, 1.7650927623060475)]
    public void CarbonMonoxide_MatchesPythonOpenApReference(
        double fuelFlowKgPerSecond,
        double tasKnots,
        double altitudeFeet,
        double temperatureDeviationKelvin,
        double expectedGramPerSecond)
    {
        var emission = CreateA320();

        var result = emission.CarbonMonoxide(
            fuelFlowKgPerSecond,
            tasKnots,
            altitudeFeet,
            temperatureDeviationKelvin);

        AssertClose(expectedGramPerSecond, result, 1e-11);
    }

    [Theory]
    [InlineData(2.0, 250.0, 0.0, 0.0, 0.20154862917120686)]
    [InlineData(1.0, 250.0, 10_000.0, 0.0, 0.13853178587328382)]
    [InlineData(0.7, 450.0, 35_000.0, 0.0, 0.1512059023185761)]
    [InlineData(1.0, 250.0, 10_000.0, 15.0, 0.15716294643377324)]
    public void Hydrocarbons_MatchPythonOpenApReference(
        double fuelFlowKgPerSecond,
        double tasKnots,
        double altitudeFeet,
        double temperatureDeviationKelvin,
        double expectedGramPerSecond)
    {
        var emission = CreateA320();

        var result = emission.Hydrocarbons(
            fuelFlowKgPerSecond,
            tasKnots,
            altitudeFeet,
            temperatureDeviationKelvin);

        AssertClose(expectedGramPerSecond, result, 1e-11);
    }

    [Fact]
    public void Interpolation_ClampsBelowIdleLikeNumpyInterp()
    {
        var emission = CreateA320();

        // This yields per-engine sea-level-equivalent flow below the first
        // ICAO point, so numpy.interp/OpenAP clamps to idle EI values.
        var co = emission.CarbonMonoxide(
            totalFuelFlowKgPerSecond: 0.2,
            trueAirspeedKnots: 0.0);

        var hc = emission.Hydrocarbons(
            totalFuelFlowKgPerSecond: 0.2,
            trueAirspeedKnots: 0.0);

        AssertClose(6.38, co, 1e-12);
        AssertClose(0.774, hc, 1e-12);
    }

    [Fact]
    public void ExplicitEngine_IsAllowedLikePythonOpenAp()
    {
        var emission = CreateA320("CFM56-5B4/P");

        Assert.Equal("CFM56-5B4/P", emission.EngineName);
        Assert.True(
            double.IsFinite(
                emission.NitrogenOxides(
                    1.0,
                    250.0,
                    10_000.0)));
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
