using OpenAP.Phases;
using Xunit;

namespace OpenAP.Tests;

public sealed class FlightPhaseModelTests
{
    [Fact]
    public void PhaseLabels_MatchRepresentativeOpenApStates()
    {
        var model = new FlightPhaseModel();

        var time = Enumerable.Range(0, 360)
            .Select(i => (double)i)
            .ToArray();

        var altitude = new double[360];
        var speed = new double[360];
        var roc = new double[360];

        Fill(
            altitude,
            speed,
            roc,
            0,
            60,
            0.0,
            0.0,
            0.0);

        Fill(
            altitude,
            speed,
            roc,
            60,
            120,
            1_000.0,
            250.0,
            1_500.0);

        Fill(
            altitude,
            speed,
            roc,
            120,
            180,
            1_000.0,
            250.0,
            -1_500.0);

        Fill(
            altitude,
            speed,
            roc,
            180,
            240,
            35_000.0,
            480.0,
            0.0);

        Fill(
            altitude,
            speed,
            roc,
            240,
            300,
            10_000.0,
            300.0,
            0.0);

        Fill(
            altitude,
            speed,
            roc,
            300,
            360,
            0.0,
            0.0,
            0.0);

        model.SetTrajectory(
            time,
            altitude,
            speed,
            roc);

        var labels =
            model.PhaseLabels();

        Assert.All(
            labels.Take(60),
            label => Assert.Equal("GND", label));

        Assert.All(
            labels.Skip(60).Take(60),
            label => Assert.Equal("CL", label));

        Assert.All(
            labels.Skip(120).Take(60),
            label => Assert.Equal("DE", label));

        Assert.All(
            labels.Skip(180).Take(60),
            label => Assert.Equal("CR", label));

        Assert.All(
            labels.Skip(240).Take(60),
            label => Assert.Equal("LVL", label));

        // Upstream loops to range(max(twindows)), excluding the last bucket.
        Assert.All(
            labels.Skip(300),
            label => Assert.Equal("NA", label));
    }

    [Fact]
    public void PhaseLabels_NormalizesTimestampOrigin()
    {
        var model = new FlightPhaseModel();

        var time = Enumerable.Range(0, 120)
            .Select(i => 10_000.0 + i)
            .ToArray();

        var altitude =
            Enumerable.Repeat(0.0, 120).ToArray();

        var speed =
            Enumerable.Repeat(0.0, 120).ToArray();

        var roc =
            Enumerable.Repeat(0.0, 120).ToArray();

        model.SetTrajectory(
            time,
            altitude,
            speed,
            roc);

        var labels =
            model.PhaseLabels();

        Assert.All(
            labels.Take(60),
            label => Assert.Equal("GND", label));

        Assert.All(
            labels.Skip(60),
            label => Assert.Equal("NA", label));
    }

    [Fact]
    public void FlightPhaseIndices_MatchOpenApHeuristics()
    {
        var model = new FlightPhaseModel();

        var (time, altitude, speed, roc) =
            BuildRepresentativeFlight();

        model.SetTrajectory(
            time,
            altitude,
            speed,
            roc);

        var indices =
            model.GetFlightPhaseIndices();

        Assert.Equal(11, indices.Takeoff);
        Assert.Equal(50, indices.InitialClimb);
        Assert.Equal(120, indices.Climb);
        Assert.Equal(299, indices.Cruise);
        Assert.Equal(480, indices.Descent);
        Assert.Equal(600, indices.FinalApproach);
        Assert.Equal(679, indices.Landing);
        Assert.Equal(711, indices.End);

        var dictionary =
            indices.ToDictionary();

        Assert.Equal(11, dictionary["TO"]);
        Assert.Equal(50, dictionary["IC"]);
        Assert.Equal(120, dictionary["CL"]);
        Assert.Equal(299, dictionary["CR"]);
        Assert.Equal(480, dictionary["DE"]);
        Assert.Equal(600, dictionary["FA"]);
        Assert.Equal(679, dictionary["LD"]);
        Assert.Equal(711, dictionary["END"]);
    }

    [Fact]
    public void PhaseLabels_RejectsUnsetTrajectory()
    {
        var model = new FlightPhaseModel();

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => model.PhaseLabels());

        Assert.Contains(
            "Trajectory data not set",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SetTrajectory_RejectsMismatchedInputLengths()
    {
        var model = new FlightPhaseModel();

        Assert.Throws<InvalidOperationException>(
            () => model.SetTrajectory(
                [0.0, 1.0],
                [0.0],
                [0.0, 1.0],
                [0.0, 0.0]));
    }

    [Fact]
    public void SingleWindow_RemainsNaLikePythonOpenAp()
    {
        var model = new FlightPhaseModel();

        model.SetTrajectory(
            [0.0, 1.0, 2.0],
            [0.0, 0.0, 0.0],
            [0.0, 0.0, 0.0],
            [0.0, 0.0, 0.0]);

        Assert.Equal(
            ["NA", "NA", "NA"],
            model.PhaseLabels());
    }

    private static void Fill(
        double[] altitude,
        double[] speed,
        double[] roc,
        int start,
        int end,
        double altitudeValue,
        double speedValue,
        double rocValue)
    {
        for (var i = start; i < end; i++)
        {
            altitude[i] = altitudeValue;
            speed[i] = speedValue;
            roc[i] = rocValue;
        }
    }

    private static (
        double[] Time,
        double[] Altitude,
        double[] Speed,
        double[] Roc)
        BuildRepresentativeFlight()
    {
        const int count = 780;

        var time = new double[count];
        var altitude = new double[count];
        var speed = new double[count];
        var roc = new double[count];

        for (var i = 0; i < count; i++)
        {
            time[i] = i;
        }

        for (var i = 11; i < 120; i++)
        {
            speed[i] =
                10.0 +
                (i - 11) *
                (150.0 / 108.0);

            roc[i] = 1_500.0;
        }

        for (var i = 50; i < 120; i++)
        {
            altitude[i] =
                20.0 +
                (i - 50) *
                (1_380.0 / 69.0);
        }

        Fill(
            altitude,
            speed,
            roc,
            120,
            300,
            5_000.0,
            300.0,
            1_500.0);

        Fill(
            altitude,
            speed,
            roc,
            300,
            480,
            35_000.0,
            480.0,
            0.0);

        Fill(
            altitude,
            speed,
            roc,
            480,
            600,
            10_000.0,
            300.0,
            -1_500.0);

        for (var i = 600; i < 680; i++)
        {
            altitude[i] = 1_000.0;
        }

        for (var i = 600; i < count; i++)
        {
            speed[i] =
                Math.Max(
                    20.0,
                    140.0 -
                    (i - 600));

            roc[i] =
                i < 680
                    ? -500.0
                    : 0.0;
        }

        return (
            time,
            altitude,
            speed,
            roc);
    }
}
