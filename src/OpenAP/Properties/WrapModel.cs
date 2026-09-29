namespace OpenAP.Properties;

/// <summary>
/// Typed equivalent of Python OpenAP's WRAP class.
/// </summary>
public sealed class WrapModel
{
    private readonly IReadOnlyDictionary<string, KinematicValue> _variables;

    internal WrapModel(
        string aircraftIcaoCode,
        IReadOnlyDictionary<string, KinematicValue> variables)
    {
        AircraftIcaoCode = aircraftIcaoCode;
        _variables = variables;
    }

    public string AircraftIcaoCode { get; }

    public IReadOnlyCollection<KinematicValue> Variables
        => _variables.Values.ToArray();

    public KinematicValue GetVariable(string variable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);

        return _variables.TryGetValue(variable, out var value)
            ? value
            : throw new KeyNotFoundException(
                $"WRAP variable '{variable}' was not found for {AircraftIcaoCode}.");
    }

    public KinematicValue TakeoffSpeed()
        => GetVariable("to_v_lof");

    public KinematicValue TakeoffDistance()
        => GetVariable("to_d_tof");

    public KinematicValue TakeoffAcceleration()
        => GetVariable("to_acc_tof");

    public KinematicValue InitialClimbVcas()
        => GetVariable("ic_va_avg");

    public KinematicValue InitialClimbVerticalRate()
        => GetVariable("ic_vs_avg");

    public KinematicValue ClimbRange()
        => GetVariable("cl_d_range");

    public KinematicValue ClimbConstantVcas()
        => GetVariable("cl_v_cas_const");

    public KinematicValue ClimbConstantMach()
        => GetVariable("cl_v_mach_const");

    public KinematicValue ClimbCrossoverAltitudeConstantCas()
        => GetVariable("cl_h_cas_const");

    public KinematicValue ClimbCrossoverAltitudeConstantMach()
        => GetVariable("cl_h_mach_const");

    public KinematicValue ClimbVerticalRatePreConstantCas()
        => GetVariable("cl_vs_avg_pre_cas");

    public KinematicValue ClimbVerticalRateConstantCas()
        => GetVariable("cl_vs_avg_cas_const");

    public KinematicValue ClimbVerticalRateConstantMach()
        => GetVariable("cl_vs_avg_mach_const");

    public KinematicValue CruiseRange()
        => GetVariable("cr_d_range");

    public KinematicValue CruiseAltitude()
        => GetVariable("cr_h_mean");

    public KinematicValue CruiseInitialAltitude()
        => GetVariable("cr_h_init");

    public KinematicValue CruiseMaximumAltitude()
        => GetVariable("cr_h_max");

    public KinematicValue CruiseMach()
        => GetVariable("cr_v_mach_mean");

    public KinematicValue CruiseMaximumMach()
        => GetVariable("cr_v_mach_max");

    public KinematicValue CruiseMeanVcas()
        => GetVariable("cr_v_cas_mean");

    public KinematicValue DescentRange()
        => GetVariable("de_d_range");

    public KinematicValue DescentConstantMach()
        => GetVariable("de_v_mach_const");

    public KinematicValue DescentConstantVcas()
        => GetVariable("de_v_cas_const");

    public KinematicValue DescentCrossoverAltitudeConstantMach()
        => GetVariable("de_h_mach_const");

    public KinematicValue DescentCrossoverAltitudeConstantCas()
        => GetVariable("de_h_cas_const");

    public KinematicValue DescentVerticalRateConstantMach()
        => GetVariable("de_vs_avg_mach_const");

    public KinematicValue DescentVerticalRateConstantCas()
        => GetVariable("de_vs_avg_cas_const");

    public KinematicValue DescentVerticalRatePostConstantCas()
        => GetVariable("de_vs_avg_after_cas");

    public KinematicValue FinalApproachVcas()
        => GetVariable("fa_va_avg");

    public KinematicValue FinalApproachVerticalRate()
        => GetVariable("fa_vs_avg");

    public KinematicValue LandingSpeed()
        => GetVariable("ld_v_app");

    public KinematicValue LandingDistance()
        => GetVariable("ld_d_brk");

    public KinematicValue LandingAcceleration()
        => GetVariable("ld_acc_brk");
}
