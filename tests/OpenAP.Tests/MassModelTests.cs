using OpenAP.Performance;
using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class MassModelTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    private static AircraftDatabase CreateDatabase()
        => AircraftDatabase.FromOpenApDataDirectory(DataDirectory);

    [Theory]
    [InlineData(0.0, 0.8, 59_262.885)]
    [InlineData(500.0, 0.8, 59_262.885)]
    [InlineData(1_000.0, 0.8, 59_262.885)]
    [InlineData(2_500.0, 0.8, 65_091.4425)]
    [InlineData(5_000.0, 0.8, 74_805.705)]
    [InlineData(7_000.0, 0.8, 74_805.705)]
    public void FromRange_MatchesPythonOpenApReference(
        double distanceNauticalMiles,
        double loadFactor,
        double expectedMassKg)
    {
        var result = MassModel.FromRange(
            "A320",
            distanceNauticalMiles,
            CreateDatabase(),
            loadFactor);

        AssertClose(expectedMassKg, result, 1e-9);
    }

    [Theory]
    [InlineData(0.0, 0.759780576923077)]
    [InlineData(2_500.0, 0.834505673076923)]
    [InlineData(5_000.0, 0.9590475)]
    public void FromRange_FractionMatchesPythonOpenApReference(
        double distanceNauticalMiles,
        double expectedFraction)
    {
        var result = MassModel.FromRange(
            "A320",
            distanceNauticalMiles,
            CreateDatabase(),
            fraction: true);

        AssertClose(expectedFraction, result, 1e-15);
    }

    [Fact]
    public void FromRange_UsesUpstreamFuelVolumeConversion()
    {
        var aircraft = CreateDatabase().Get("A320");

        var maximumFuelMassKg =
            aircraft.Limits.MaximumFuelCapacityLiters *
            MassModel.FuelDensityKgPerLiter;

        AssertClose(19_428.525, maximumFuelMassKg, 1e-12);
    }

    [Fact]
    public void FromRange_DoesNotClampLoadFactorLikePythonOpenAp()
    {
        var database = CreateDatabase();

        var normal = MassModel.FromRange(
            "A320",
            2_500.0,
            database,
            loadFactor: 1.0);

        var overbooked = MassModel.FromRange(
            "A320",
            2_500.0,
            database,
            loadFactor: 1.1);

        Assert.True(overbooked > normal);
    }

    [Fact]
    public void FromRange_CanUseAircraftSynonym()
    {
        var database = CreateDatabase();

        Assert.Throws<KeyNotFoundException>(
            () => MassModel.FromRange(
                "A310",
                1_000.0,
                database));

        var result = MassModel.FromRange(
            "A310",
            1_000.0,
            database,
            useSynonym: true);

        Assert.True(double.IsFinite(result));
        Assert.True(result > 0.0);
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
