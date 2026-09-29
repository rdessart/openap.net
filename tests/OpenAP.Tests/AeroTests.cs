using OpenAP.Aero;
using Xunit;
using AeroModel = OpenAP.Aero.Aero;

namespace OpenAP.Tests;

public sealed class AeroTests
{
    [Theory]
    [InlineData(0.0, 101324.9985008625, 1.225, 288.15)]
    [InlineData(5000.0, 54013.628555649106, 0.7360302489478526, 255.65)]
    [InlineData(11000.0, 22625.79115479623, 0.36381716667724334, 216.65)]
    [InlineData(15000.0, 12041.151244516379, 0.1936187556643062, 216.65)]
    public void Atmosphere_MatchesPythonOpenApReference(
        double altitudeMeters,
        double expectedPressure,
        double expectedDensity,
        double expectedTemperature)
    {
        var result = AeroModel.Atmosphere(altitudeMeters);

        AssertClose(expectedPressure, result.PressurePascal, 1e-8);
        AssertClose(expectedDensity, result.DensityKgPerCubicMeter, 1e-12);
        AssertClose(expectedTemperature, result.TemperatureKelvin, 1e-12);
    }

    [Theory]
    [InlineData(0.0, 340.293988026089)]
    [InlineData(5000.0, 320.5293944425378)]
    [InlineData(11000.0, 295.0694935090715)]
    public void SpeedOfSound_MatchesPythonOpenApReference(
        double altitudeMeters,
        double expectedMetersPerSecond)
    {
        AssertClose(
            expectedMetersPerSecond,
            AeroModel.SpeedOfSound(altitudeMeters),
            1e-10);
    }

    [Theory]
    [InlineData(101325.0, 0.0)]
    [InlineData(22630.0, 11000.0)]
    [InlineData(12000.0, 15022.929460481037)]
    public void IsaAltitude_MatchesPythonOpenApReference(
        double pressurePascal,
        double expectedAltitudeMeters)
    {
        AssertClose(
            expectedAltitudeMeters,
            AeroModel.IsaAltitude(pressurePascal),
            1e-9);
    }

    [Fact]
    public void CasTasConversions_MatchPythonOpenApReference()
    {
        const double altitudeMeters = 3048.0;
        var speed = 250.0 * AeroConstants.KnotToMetersPerSecond;

        var casFromTas =
            AeroModel.TasToCas(speed, altitudeMeters) /
            AeroConstants.KnotToMetersPerSecond;

        var tasFromCas =
            AeroModel.CasToTas(speed, altitudeMeters) /
            AeroConstants.KnotToMetersPerSecond;

        AssertClose(216.0784741565601, casFromTas, 1e-9);
        AssertClose(288.71179552545414, tasFromCas, 1e-9);
    }

    [Fact]
    public void CrossoverAltitude_MatchesPythonOpenApReference()
    {
        var cas = 300.0 * AeroConstants.KnotToMetersPerSecond;

        var altitudeFeet =
            AeroModel.CrossoverAltitude(cas, 0.78) /
            AeroConstants.FootToMeter;

        AssertClose(29314.13874040608, altitudeFeet, 1e-8);
    }

    [Fact]
    public void Atmosphere_ClampsTemperatureDeviationLikeOpenAp()
    {
        var plus15 = AeroModel.Atmosphere(0.0, 15.0);
        var plus100 = AeroModel.Atmosphere(0.0, 100.0);

        Assert.Equal(plus15, plus100);
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
