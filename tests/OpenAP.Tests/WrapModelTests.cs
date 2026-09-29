using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class WrapModelTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    private static WrapDatabase CreateDatabase()
        => WrapDatabase.FromOpenApDataDirectory(DataDirectory);

    [Fact]
    public void A320_TakeoffSpeed_MatchesPythonOpenAp()
    {
        var wrap = CreateDatabase().Get("A320");

        var value = wrap.TakeoffSpeed();

        Assert.Equal("to_v_lof", value.Variable);
        Assert.Equal("takeoff", value.FlightPhase);
        Assert.Equal("Liftoff speed", value.Name);
        Assert.Equal(85.3, value.DefaultValue);
        Assert.Equal(74.5, value.Minimum);
        Assert.Equal(96.0, value.Maximum);
        Assert.Equal("norm", value.StatisticalModel);
        Assert.Equal([85.29, 7.47], value.StatisticalModelParameters);
    }

    [Fact]
    public void A320_CruiseMach_MatchesPythonOpenApExample()
    {
        var wrap = CreateDatabase().Get("A320");

        var value = wrap.CruiseMach();

        Assert.Equal(0.78, value.DefaultValue);
        Assert.Equal(0.75, value.Minimum);
        Assert.Equal(0.8, value.Maximum);
        Assert.Equal("beta", value.StatisticalModel);
        Assert.Equal(
            [17.82, 5.05, 0.62, 0.20],
            value.StatisticalModelParameters);
    }

    [Fact]
    public void A320_GammaDistribution_ParametersAreParsed()
    {
        var wrap = CreateDatabase().Get("A320");

        var value = wrap.CruiseRange();

        Assert.Equal(856.0, value.DefaultValue);
        Assert.Equal(487.0, value.Minimum);
        Assert.Equal(4352.0, value.Maximum);
        Assert.Equal("gamma", value.StatisticalModel);
        Assert.Equal(
            [1.71, 453.95, 569.12],
            value.StatisticalModelParameters);
    }

    [Fact]
    public void A320_NegativeDescentValues_AreParsed()
    {
        var wrap = CreateDatabase().Get("A320");

        var value = wrap.DescentVerticalRateConstantMach();

        Assert.Equal(-5.76, value.DefaultValue);
        Assert.Equal(-13.45, value.Minimum);
        Assert.Equal(-2.26, value.Maximum);
        Assert.Equal("beta", value.StatisticalModel);
        Assert.Equal(
            [3.52, 1.95, -19.00, 18.22],
            value.StatisticalModelParameters);
    }

    [Fact]
    public void AllPythonWrapAccessors_MapToExpectedVariables()
    {
        var wrap = CreateDatabase().Get("A320");

        (Func<KinematicValue> Accessor, string Variable)[] mappings =
        [
            (wrap.TakeoffSpeed, "to_v_lof"),
            (wrap.TakeoffDistance, "to_d_tof"),
            (wrap.TakeoffAcceleration, "to_acc_tof"),
            (wrap.InitialClimbVcas, "ic_va_avg"),
            (wrap.InitialClimbVerticalRate, "ic_vs_avg"),
            (wrap.ClimbRange, "cl_d_range"),
            (wrap.ClimbConstantVcas, "cl_v_cas_const"),
            (wrap.ClimbConstantMach, "cl_v_mach_const"),
            (wrap.ClimbCrossoverAltitudeConstantCas, "cl_h_cas_const"),
            (wrap.ClimbCrossoverAltitudeConstantMach, "cl_h_mach_const"),
            (wrap.ClimbVerticalRatePreConstantCas, "cl_vs_avg_pre_cas"),
            (wrap.ClimbVerticalRateConstantCas, "cl_vs_avg_cas_const"),
            (wrap.ClimbVerticalRateConstantMach, "cl_vs_avg_mach_const"),
            (wrap.CruiseRange, "cr_d_range"),
            (wrap.CruiseAltitude, "cr_h_mean"),
            (wrap.CruiseInitialAltitude, "cr_h_init"),
            (wrap.CruiseMaximumAltitude, "cr_h_max"),
            (wrap.CruiseMach, "cr_v_mach_mean"),
            (wrap.CruiseMaximumMach, "cr_v_mach_max"),
            (wrap.CruiseMeanVcas, "cr_v_cas_mean"),
            (wrap.DescentRange, "de_d_range"),
            (wrap.DescentConstantMach, "de_v_mach_const"),
            (wrap.DescentConstantVcas, "de_v_cas_const"),
            (wrap.DescentCrossoverAltitudeConstantMach, "de_h_mach_const"),
            (wrap.DescentCrossoverAltitudeConstantCas, "de_h_cas_const"),
            (wrap.DescentVerticalRateConstantMach, "de_vs_avg_mach_const"),
            (wrap.DescentVerticalRateConstantCas, "de_vs_avg_cas_const"),
            (wrap.DescentVerticalRatePostConstantCas, "de_vs_avg_after_cas"),
            (wrap.FinalApproachVcas, "fa_va_avg"),
            (wrap.FinalApproachVerticalRate, "fa_vs_avg"),
            (wrap.LandingSpeed, "ld_v_app"),
            (wrap.LandingDistance, "ld_d_brk"),
            (wrap.LandingAcceleration, "ld_acc_brk")
        ];

        Assert.Equal(33, mappings.Length);

        foreach (var (accessor, variable) in mappings)
        {
            Assert.Equal(variable, accessor().Variable);
        }
    }

    [Fact]
    public void GenericAccess_ExposesRowsWithoutDedicatedPythonAccessor()
    {
        var wrap = CreateDatabase().Get("A320");

        var cruiseMaxCas = wrap.GetVariable("cr_v_cas_max");
        var approachAngle = wrap.GetVariable("fa_agl");

        Assert.Equal(135.0, cruiseMaxCas.DefaultValue);
        Assert.Equal(3.06, approachAngle.DefaultValue);
    }

    [Fact]
    public void SynonymResolution_DefaultsToEnabledLikePythonOpenAp()
    {
        var database = CreateDatabase();

        var a20n = database.Get("A20N");

        Assert.Equal("A320", a20n.AircraftIcaoCode);
        Assert.Equal(0.78, a20n.CruiseMach().DefaultValue);
    }

    [Fact]
    public void SynonymResolution_CanBeDisabled()
    {
        var database = CreateDatabase();

        Assert.Throws<KeyNotFoundException>(
            () => database.Get("A20N", useSynonym: false));
    }

    [Fact]
    public void EveryUpstreamWrapFile_ParsesSuccessfully()
    {
        var database = CreateDatabase();

        foreach (var aircraft in database.AvailableAircraft())
        {
            var wrap = database.Get(aircraft);

            Assert.True(wrap.Variables.Count >= 30);
            Assert.True(double.IsFinite(wrap.TakeoffSpeed().DefaultValue));
            Assert.True(double.IsFinite(wrap.CruiseMach().DefaultValue));
            Assert.True(double.IsFinite(wrap.LandingSpeed().DefaultValue));
        }
    }

    [Fact]
    public void MissingVariable_ThrowsUsefulException()
    {
        var wrap = CreateDatabase().Get("A320");

        var exception = Assert.Throws<KeyNotFoundException>(
            () => wrap.GetVariable("not_a_wrap_variable"));

        Assert.Contains(
            "not_a_wrap_variable",
            exception.Message,
            StringComparison.Ordinal);
    }
}
