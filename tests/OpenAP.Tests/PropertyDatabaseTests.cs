using OpenAP.Properties;
using Xunit;

namespace OpenAP.Tests;

public sealed class PropertyDatabaseTests
{
    private static readonly string DataDirectory = OpenApDataPath.Find();

    [Fact]
    public void A320_MatchesOpenApAircraftProperties()
    {
        var database = AircraftDatabase.FromOpenApDataDirectory(DataDirectory);

        var aircraft = database.Get("A320");

        Assert.Equal("A320", aircraft.IcaoCode);
        Assert.Equal("Airbus A320", aircraft.Name);
        Assert.Equal(78_000.0, aircraft.Limits.MaximumTakeoffMassKg);
        Assert.Equal(66_000.0, aircraft.Limits.MaximumLandingMassKg);
        Assert.Equal(42_600.0, aircraft.Limits.OperatingEmptyMassKg);
        Assert.Equal(24_210.0, aircraft.Limits.MaximumFuelCapacityKg);
        Assert.Equal(350.0, aircraft.Limits.MaximumOperatingSpeedKnots);
        Assert.Equal(0.82, aircraft.Limits.MaximumOperatingMach);
        Assert.Equal(12_500.0, aircraft.Limits.CeilingMeters);

        Assert.Equal(124.0, aircraft.Wing.AreaSquareMeters);
        Assert.Equal(35.8, aircraft.Wing.SpanMeters);
        Assert.Equal(25.0, aircraft.Wing.SweepDegrees);
        Assert.Null(aircraft.Wing.ThicknessToChordRatio);

        Assert.Equal(2, aircraft.Engines.Number);
        Assert.Equal("CFM56-5B4", aircraft.Engines.DefaultEngine);
        Assert.Contains("CFM56-5B4", aircraft.Engines.Options);
        Assert.Equal("CFM56-5B4", aircraft.Engines.VariantOptions["A320-214"]);

        Assert.Equal(0.018, aircraft.Drag.ZeroLiftCoefficient);
        Assert.Equal(0.039, aircraft.Drag.InducedDragFactor);
        Assert.Equal(0.017, aircraft.Drag.GearDragIncrement);
    }

    [Fact]
    public void AircraftSynonym_ResolvesLikePythonOpenAp()
    {
        var database = AircraftDatabase.FromOpenApDataDirectory(DataDirectory);

        Assert.Throws<KeyNotFoundException>(() => database.Get("A310"));

        var aircraft = database.Get("A310", useSynonym: true);

        Assert.Equal("A318", aircraft.IcaoCode);
    }

    [Fact]
    public void Engine_Cfm56_5B4_MatchesOpenApProperties()
    {
        var database = EngineDatabase.FromOpenApDataDirectory(DataDirectory);

        var engine = database.Get("CFM56-5B4");

        Assert.Equal("CFM56-5B4", engine.Name);
        Assert.Equal("CFM International", engine.Manufacturer);
        Assert.Equal("TF", engine.Type);
        Assert.Equal(5.9, engine.BypassRatio);
        Assert.Equal(27.1, engine.PressureRatio);
        Assert.Equal(117_900.0, engine.MaximumThrustNewton);
        Assert.Equal(1.166, engine.FuelFlows.TakeoffKgPerSecond);
        Assert.Equal(0.961, engine.FuelFlows.ClimboutKgPerSecond);
        Assert.Equal(0.326, engine.FuelFlows.ApproachKgPerSecond);
        Assert.Equal(0.107, engine.FuelFlows.IdleKgPerSecond);
        Assert.Equal(22_241.0, engine.CruiseThrustNewton);
        Assert.Equal(0.0154, engine.CruiseSpecificFuelConsumption);
        Assert.Equal(0.8, engine.CruiseMach);
        Assert.Equal(35_000.0, engine.CruiseAltitudeFeet);
        Assert.Equal(5.2e-7, engine.FuelCorrectionSlope);
    }

    [Fact]
    public void EnginePrefixSearch_FollowsOpenApBehavior()
    {
        var database = EngineDatabase.FromOpenApDataDirectory(DataDirectory);

        var engines = database.Search("CFM56-5B4");

        Assert.Contains("CFM56-5B4", engines);
        Assert.Contains("CFM56-5B4/2", engines);
        Assert.True(engines.Count >= 4);
    }
}
