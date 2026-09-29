using OpenAP.Performance;
using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class ThrustModelTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    private static ThrustModel CreateA320()
    {
        var aircraft = AircraftDatabase.FromOpenApDataDirectory(DataDirectory);
        var engines = EngineDatabase.FromOpenApDataDirectory(DataDirectory);

        return new ThrustModel("A320", aircraft, engines);
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0, 235799.99818550606)]
    [InlineData(100.0, 0.0, 0.0, 200722.99758761944)]
    [InlineData(150.0, 0.0, 0.0, 185981.0993593941)]
    [InlineData(150.0, 5000.0, 0.0, 170235.12128230234)]
    [InlineData(150.0, 5000.0, 15.0, 176837.77440295203)]
    public void Takeoff_MatchesPythonOpenApReference(
        double tasKnots,
        double altitudeFeet,
        double dT,
        double expectedNewton)
    {
        var thrust = CreateA320();

        var result = thrust.Takeoff(tasKnots, altitudeFeet, dT);

        AssertClose(expectedNewton, result, 1e-6);
    }

    [Theory]
    [InlineData(250.0, 5000.0, 2000.0, 99970.13589408441)]
    [InlineData(300.0, 15000.0, 2500.0, 78881.73984863042)]
    [InlineData(450.0, 35000.0, 1000.0, 46159.58086889595)]
    public void Climb_MatchesPythonOpenApReference(
        double tasKnots,
        double altitudeFeet,
        double rocFeetPerMinute,
        double expectedNewton)
    {
        var thrust = CreateA320();

        var result = thrust.Climb(
            tasKnots,
            altitudeFeet,
            rocFeetPerMinute);

        AssertClose(expectedNewton, result, 1e-6);
    }

    [Fact]
    public void Climb_UsesAbsoluteRateOfClimbLikePythonOpenAp()
    {
        var thrust = CreateA320();

        var climbing = thrust.Climb(300.0, 15000.0, 2500.0);
        var descending = thrust.Climb(300.0, 15000.0, -2500.0);

        AssertClose(climbing, descending, 1e-12);
    }

    [Fact]
    public void Cruise_MatchesClimbAtZeroRoc()
    {
        var thrust = CreateA320();

        var cruise = thrust.Cruise(450.0, 35000.0);
        var climb = thrust.Climb(450.0, 35000.0, 0.0);

        AssertClose(46159.58086889595, cruise, 1e-6);
        AssertClose(climb, cruise, 1e-12);
    }

    [Fact]
    public void DescentIdle_IsSevenPercentOfTakeoffThrust()
    {
        var thrust = CreateA320();

        var result = thrust.DescentIdle(250.0, 10000.0);

        AssertClose(9308.41443386764, result, 1e-6);
    }

    [Fact]
    public void Constructor_RejectsAircraftEngineMismatch()
    {
        var aircraft = AircraftDatabase.FromOpenApDataDirectory(DataDirectory);
        var engines = EngineDatabase.FromOpenApDataDirectory(DataDirectory);

        Assert.Throws<ArgumentException>(
            () => new ThrustModel(
                "A320",
                aircraft,
                engines,
                engineName: "CFM56-7B26"));
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
