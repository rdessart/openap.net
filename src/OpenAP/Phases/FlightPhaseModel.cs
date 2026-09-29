namespace OpenAP.Phases;

/// <summary>
/// Fuzzy-logic flight-phase identification ported from openap/phase.py.
/// </summary>
public sealed class FlightPhaseModel
{
    private double[]? _time;
    private double[]? _altitude;
    private double[]? _speed;
    private double[]? _rateOfClimb;

    /// <summary>
    /// Sets trajectory data.
    /// Time is seconds, altitude is feet, speed is knots and rate of climb is ft/min.
    /// </summary>
    public void SetTrajectory(
        IReadOnlyList<double> timeSeconds,
        IReadOnlyList<double> altitudeFeet,
        IReadOnlyList<double> trueAirspeedKnots,
        IReadOnlyList<double> rateOfClimbFeetPerMinute)
    {
        ArgumentNullException.ThrowIfNull(timeSeconds);
        ArgumentNullException.ThrowIfNull(altitudeFeet);
        ArgumentNullException.ThrowIfNull(trueAirspeedKnots);
        ArgumentNullException.ThrowIfNull(rateOfClimbFeetPerMinute);

        if (timeSeconds.Count == 0)
        {
            throw new ArgumentException(
                "Trajectory must contain at least one sample.",
                nameof(timeSeconds));
        }

        if (timeSeconds.Count != altitudeFeet.Count ||
            timeSeconds.Count != trueAirspeedKnots.Count ||
            timeSeconds.Count != rateOfClimbFeetPerMinute.Count)
        {
            throw new InvalidOperationException(
                "Input lists must have same length.");
        }

        var firstTime = timeSeconds[0];

        _time = new double[timeSeconds.Count];
        _altitude = new double[timeSeconds.Count];
        _speed = new double[timeSeconds.Count];
        _rateOfClimb = new double[timeSeconds.Count];

        for (var i = 0; i < timeSeconds.Count; i++)
        {
            _time[i] = timeSeconds[i] - firstTime;
            _altitude[i] = altitudeFeet[i];
            _speed[i] = trueAirspeedKnots[i];
            _rateOfClimb[i] = rateOfClimbFeetPerMinute[i];
        }
    }

    /// <summary>
    /// Returns OpenAP fuzzy phase labels:
    /// GND, CL, DE, CR, LVL, or NA.
    /// </summary>
    public IReadOnlyList<string> PhaseLabels(int timeWindowSeconds = 60)
    {
        EnsureTrajectory();

        if (timeWindowSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeWindowSeconds));
        }

        var time = _time!;
        var altitude = _altitude!;
        var speed = _speed!;
        var rateOfClimb = _rateOfClimb!;

        var labels =
            Enumerable.Repeat("NA", time.Length).ToArray();

        var timeWindows = new int[time.Length];

        for (var i = 0; i < time.Length; i++)
        {
            timeWindows[i] =
                (int)Math.Floor(
                    time[i] / timeWindowSeconds);
        }

        var maximumWindow = timeWindows.Max();

        // Deliberately mirrors:
        // for tw in range(0, int(max(twindows)))
        // The final bucket is therefore left as NA upstream.
        for (var timeWindow = 0;
             timeWindow < maximumWindow;
             timeWindow++)
        {
            var firstIndex = -1;
            var lastIndex = -1;
            var count = 0;

            var altitudeSum = 0.0;
            var speedSum = 0.0;
            var rateOfClimbSum = 0.0;

            for (var i = 0; i < timeWindows.Length; i++)
            {
                if (timeWindows[i] != timeWindow)
                    continue;

                if (firstIndex < 0)
                    firstIndex = i;

                lastIndex = i;
                count++;

                altitudeSum += altitude[i];
                speedSum += speed[i];
                rateOfClimbSum += rateOfClimb[i];
            }

            if (count == 0)
                continue;

            var meanAltitude =
                Math.Clamp(
                    altitudeSum / count,
                    0.0,
                    39_999.0);

            var meanSpeed =
                Math.Clamp(
                    speedSum / count,
                    0.0,
                    599.0);

            var meanRateOfClimb =
                Math.Clamp(
                    rateOfClimbSum / count,
                    -4_000.0,
                    3_999.9);

            var label =
                Classify(
                    meanAltitude,
                    meanSpeed,
                    meanRateOfClimb);

            for (var i = firstIndex; i <= lastIndex; i++)
            {
                labels[i] = label;
            }
        }

        return labels;
    }

    /// <summary>
    /// Returns major flight-phase boundary indices using the same heuristics as
    /// Python OpenAP.
    /// </summary>
    public FlightPhaseIndices GetFlightPhaseIndices()
    {
        EnsureTrajectory();

        var takeoffInitialClimb =
            GetTakeoffAndInitialClimb();

        var climb =
            GetClimb();

        var descent =
            GetDescent();

        var finalApproachLanding =
            GetFinalApproachAndLanding();

        var takeoff =
            takeoffInitialClimb?.Start;

        var initialClimb =
            takeoffInitialClimb?.Liftoff;

        int? climbStart;

        if (takeoffInitialClimb is not null)
        {
            climbStart =
                takeoffInitialClimb.Value.EndExclusive;
        }
        else
        {
            climbStart =
                climb?.Start;
        }

        var cruise =
            climb?.End;

        var descentStart =
            descent?.Start;

        int? finalApproach;
        int? landing;
        int end;

        if (finalApproachLanding is not null)
        {
            finalApproach =
                finalApproachLanding.Value.Start;

            landing =
                finalApproachLanding.Value.Landing;

            end =
                finalApproachLanding.Value.EndExclusive;
        }
        else if (descent is not null)
        {
            finalApproach =
                descent.Value.End;

            landing = null;
            end = _time!.Length;
        }
        else
        {
            finalApproach = null;
            landing = null;
            end = _time!.Length;
        }

        return new FlightPhaseIndices(
            Takeoff: takeoff,
            InitialClimb: initialClimb,
            Climb: climbStart,
            Cruise: cruise,
            Descent: descentStart,
            FinalApproach: finalApproach,
            Landing: landing,
            End: end);
    }

    private static string Classify(
        double altitudeFeet,
        double speedKnots,
        double rateOfClimbFeetPerMinute)
    {
        var altitudeGround =
            ZMembership(
                altitudeFeet,
                0.0,
                200.0);

        var altitudeLow =
            GaussianMembership(
                altitudeFeet,
                10_000.0,
                10_000.0);

        var altitudeHigh =
            GaussianMembership(
                altitudeFeet,
                35_000.0,
                20_000.0);

        var rocZero =
            GaussianMembership(
                rateOfClimbFeetPerMinute,
                0.0,
                100.0);

        var rocPositive =
            SMembership(
                rateOfClimbFeetPerMinute,
                10.0,
                1_000.0);

        var rocNegative =
            ZMembership(
                rateOfClimbFeetPerMinute,
                -1_000.0,
                -10.0);

        var speedHigh =
            GaussianMembership(
                speedKnots,
                600.0,
                100.0);

        var speedMedium =
            GaussianMembership(
                speedKnots,
                300.0,
                100.0);

        var speedLow =
            GaussianMembership(
                speedKnots,
                0.0,
                50.0);

        var rules = new[]
        {
            Math.Min(
                altitudeGround,
                Math.Min(
                    rocZero,
                    speedLow)),
            Math.Min(
                altitudeLow,
                Math.Min(
                    rocPositive,
                    speedMedium)),
            Math.Min(
                altitudeLow,
                Math.Min(
                    rocNegative,
                    speedMedium)),
            Math.Min(
                altitudeHigh,
                Math.Min(
                    rocZero,
                    speedHigh)),
            Math.Min(
                altitudeLow,
                Math.Min(
                    rocZero,
                    speedMedium))
        };

        var maximumMembership =
            double.NegativeInfinity;

        var stateRaw = 0.0;

        // OpenAP's state universe is np.arange(0, 6, 0.01).
        // defuzz(..., "lom") returns the largest state at the maximum
        // aggregated membership, so update on >= rather than >.
        for (var i = 0; i < 600; i++)
        {
            var state = i * 0.01;

            var aggregated = 0.0;

            for (var ruleIndex = 0;
                 ruleIndex < rules.Length;
                 ruleIndex++)
            {
                var stateMembership =
                    GaussianMembership(
                        state,
                        ruleIndex + 1.0,
                        0.1);

                var activated =
                    Math.Min(
                        rules[ruleIndex],
                        stateMembership);

                aggregated =
                    Math.Max(
                        aggregated,
                        activated);
            }

            if (aggregated >= maximumMembership)
            {
                maximumMembership = aggregated;
                stateRaw = state;
            }
        }

        var stateNumber =
            (int)Math.Round(
                stateRaw,
                MidpointRounding.ToEven);

        stateNumber =
            Math.Clamp(
                stateNumber,
                1,
                6);

        return stateNumber switch
        {
            1 => "GND",
            2 => "CL",
            3 => "DE",
            4 => "CR",
            5 => "LVL",
            _ => throw new InvalidOperationException(
                "OpenAP fuzzy phase state resolved outside the label map.")
        };
    }

    private static double GaussianMembership(
        double value,
        double mean,
        double sigma)
    {
        var delta =
            value - mean;

        return Math.Exp(
            -(delta * delta) /
            (2.0 * sigma * sigma));
    }

    private static double ZMembership(
        double value,
        double a,
        double b)
    {
        if (a > b)
            throw new ArgumentException("a <= b is required.");

        if (value < a)
            return 1.0;

        var midpoint =
            (a + b) / 2.0;

        if (value < midpoint)
        {
            var ratio =
                (value - a) /
                (b - a);

            return 1.0 -
                   2.0 *
                   ratio *
                   ratio;
        }

        if (value <= b)
        {
            var ratio =
                (value - b) /
                (b - a);

            return 2.0 *
                   ratio *
                   ratio;
        }

        return 0.0;
    }

    private static double SMembership(
        double value,
        double a,
        double b)
    {
        if (a > b)
            throw new ArgumentException("a <= b is required.");

        if (value <= a)
            return 0.0;

        var midpoint =
            (a + b) / 2.0;

        if (value <= midpoint)
        {
            var ratio =
                (value - a) /
                (b - a);

            return 2.0 *
                   ratio *
                   ratio;
        }

        if (value <= b)
        {
            var ratio =
                (value - b) /
                (b - a);

            return 1.0 -
                   2.0 *
                   ratio *
                   ratio;
        }

        return 1.0;
    }

    private TakeoffInitialClimbIndices? GetTakeoffAndInitialClimb()
    {
        var time = _time!;
        var altitude = _altitude!;
        var speed = _speed!;

        var start = 0;
        var end = 0;

        for (var i = 0; i < time.Length; i++)
        {
            if (altitude[i] < 1_500.0)
            {
                end = i;
                continue;
            }

            break;
        }

        var temporarySpeed =
            speed[end];

        for (var i = end - 1; i >= 0; i--)
        {
            if (speed[i] < 30.0 &&
                speed[i] > temporarySpeed)
            {
                break;
            }

            if (speed[i] < 5.0)
            {
                break;
            }

            start = i;
            temporarySpeed = speed[i];
        }

        if (time[end] - time[start] > 300.0)
            return null;

        if (end - start < 10)
            return null;

        if (altitude[end] < 200.0)
            return null;

        var liftoff = start;

        for (var i = start + 1; i < end; i++)
        {
            if (Math.Abs(
                    altitude[i] -
                    altitude[i - 1]) > 10.0)
            {
                liftoff = i;
                break;
            }
        }

        if (liftoff - start < 5)
            return null;

        return new TakeoffInitialClimbIndices(
            Start: start,
            Liftoff: liftoff,
            EndExclusive: end + 1);
    }

    private ApproachLandingIndices? GetFinalApproachAndLanding()
    {
        var altitude = _altitude!;
        var speed = _speed!;

        var start = 0;
        var end = 0;

        for (var i = altitude.Length - 1;
             i >= 0;
             i--)
        {
            if (altitude[i] < 1_500.0)
            {
                start = i;
            }
            else
            {
                break;
            }
        }

        var temporarySpeed =
            speed[start];

        for (var i = start;
             i < altitude.Length;
             i++)
        {
            if (speed[i] <= 50.0 &&
                speed[i] >= temporarySpeed)
            {
                break;
            }

            if (speed[i] < 30.0)
            {
                break;
            }

            end = i;
            temporarySpeed = speed[i];
        }

        if (altitude[start] < 100.0)
            return null;

        var landing = end;

        // Python:
        // reversed(range(start, end - 1))
        // => end - 2 down to start.
        for (var i = end - 2;
             i >= start;
             i--)
        {
            if (Math.Abs(
                    altitude[i] -
                    altitude[i + 1]) > 10.0)
            {
                landing = i;
                break;
            }
        }

        if (landing - start < 5 ||
            end - landing < 5)
        {
            return null;
        }

        return new ApproachLandingIndices(
            Start: start,
            Landing: landing,
            EndExclusive: end + 1);
    }

    private PhaseRange? GetClimb()
    {
        var labels =
            PhaseLabels();

        var first = -1;
        var last = -1;

        for (var i = 0; i < labels.Count; i++)
        {
            if (!string.Equals(
                    labels[i],
                    "CL",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (first < 0)
                first = i;

            last = i;
        }

        return first < 0
            ? null
            : new PhaseRange(first, last);
    }

    private DescentRange? GetDescent()
    {
        var labels =
            PhaseLabels();

        var first = -1;
        var last = -1;

        for (var i = 0; i < labels.Count; i++)
        {
            if (!string.Equals(
                    labels[i],
                    "DE",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (first < 0)
                first = i;

            last = i;
        }

        if (first < 0)
            return null;

        var continuousDescentApproach = true;

        for (var i = first; i < last; i++)
        {
            if (string.Equals(
                    labels[i],
                    "LVL",
                    StringComparison.Ordinal))
            {
                continuousDescentApproach = false;
                break;
            }
        }

        return new DescentRange(
            first,
            last,
            continuousDescentApproach);
    }

    private void EnsureTrajectory()
    {
        if (_time is null)
        {
            throw new InvalidOperationException(
                "Trajectory data not set, run SetTrajectory(...) first.");
        }
    }

    private readonly record struct PhaseRange(
        int Start,
        int End);

    private readonly record struct DescentRange(
        int Start,
        int End,
        bool IsContinuousDescentApproach);

    private readonly record struct TakeoffInitialClimbIndices(
        int Start,
        int Liftoff,
        int EndExclusive);

    private readonly record struct ApproachLandingIndices(
        int Start,
        int Landing,
        int EndExclusive);
}
