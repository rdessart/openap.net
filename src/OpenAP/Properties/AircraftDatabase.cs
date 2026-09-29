using System.Collections.Concurrent;

namespace OpenAP.Properties;

/// <summary>
/// Loads strongly typed aircraft properties from the OpenAP aircraft YAML files.
///
/// The data files remain external to this assembly. This keeps the OpenAP data
/// licensing boundary explicit and avoids runtime reflection-based serialization.
/// </summary>
public sealed class AircraftDatabase
{
    private readonly string _aircraftDirectory;
    private readonly string _synonymFile;
    private readonly ConcurrentDictionary<string, AircraftDefinition> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<IReadOnlyDictionary<string, string>> _synonyms;

    public AircraftDatabase(string aircraftDirectory, string? synonymFile = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftDirectory);

        _aircraftDirectory = Path.GetFullPath(aircraftDirectory);
        _synonymFile = synonymFile is null
            ? Path.Combine(_aircraftDirectory, "_synonym.csv")
            : Path.GetFullPath(synonymFile);

        _synonyms = new Lazy<IReadOnlyDictionary<string, string>>(
            LoadSynonyms,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public static AircraftDatabase FromOpenApDataDirectory(string dataDirectory)
        => new(Path.Combine(dataDirectory, "aircraft"));

    public IReadOnlyList<string> AvailableAircraft(bool includeSynonyms = false)
    {
        var aircraft = Directory
            .EnumerateFiles(_aircraftDirectory, "*.yml", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (includeSynonyms)
            aircraft.AddRange(_synonyms.Value.Keys.OrderBy(name => name, StringComparer.Ordinal));

        return aircraft;
    }

    public AircraftDefinition Get(string icaoCode, bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icaoCode);

        var requested = icaoCode.Trim().ToLowerInvariant();
        var resolved = requested;
        var file = GetAircraftPath(resolved);

        if (!File.Exists(file) && useSynonym &&
            _synonyms.Value.TryGetValue(requested, out var synonym))
        {
            resolved = synonym;
            file = GetAircraftPath(resolved);
        }

        if (!File.Exists(file))
        {
            var suffix = useSynonym
                ? " and no synonym was found"
                : ". Try useSynonym=true if an OpenAP synonym is acceptable";

            throw new KeyNotFoundException(
                $"Aircraft '{requested}' is not available{suffix}.");
        }

        return _cache.GetOrAdd(
            resolved,
            static (code, state) => ParseAircraft(code, state),
            file);
    }

    public IReadOnlyList<string> GetEngineOptions(string icaoCode, bool useSynonym = false)
        => Get(icaoCode, useSynonym).Engines.Options;

    private string GetAircraftPath(string code)
        => Path.Combine(_aircraftDirectory, code + ".yml");

    private IReadOnlyDictionary<string, string> LoadSynonyms()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(_synonymFile))
            return result;

        var lines = File.ReadLines(_synonymFile).Skip(1);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = CsvReader.ParseLine(line);
            if (fields.Count < 2)
                continue;

            result[fields[0].Trim()] = fields[1].Trim();
        }

        return result;
    }

    private static AircraftDefinition ParseAircraft(string icaoCode, string file)
    {
        var yaml = SimpleYamlDocument.Parse(File.ReadAllText(file));

        var variantOptions = yaml.GetMapping("engine.options");
        var listOptions = yaml.GetSequence("engine.options");

        IReadOnlyList<string> engineOptions = variantOptions.Count > 0
            ? variantOptions.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : listOptions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return new AircraftDefinition(
            IcaoCode: icaoCode.ToUpperInvariant(),
            Name: yaml.GetRequiredString("aircraft"),
            Limits: new AircraftLimits(
                yaml.GetRequiredDouble("mtow"),
                yaml.GetRequiredDouble("mlw"),
                yaml.GetRequiredDouble("oew"),
                yaml.GetRequiredDouble("mfc"),
                yaml.GetRequiredDouble("vmo"),
                yaml.GetRequiredDouble("mmo"),
                yaml.GetRequiredDouble("ceiling")),
            Passengers: new PassengerCapacity(
                yaml.GetRequiredInt("pax.max"),
                yaml.GetRequiredInt("pax.low"),
                yaml.GetRequiredInt("pax.high")),
            Fuselage: new FuselageGeometry(
                yaml.GetRequiredDouble("fuselage.length"),
                yaml.GetRequiredDouble("fuselage.height"),
                yaml.GetRequiredDouble("fuselage.width")),
            Wing: new WingGeometry(
                yaml.GetRequiredDouble("wing.area"),
                yaml.GetRequiredDouble("wing.span"),
                yaml.GetRequiredDouble("wing.mac"),
                yaml.GetRequiredDouble("wing.sweep"),
                yaml.GetOptionalDouble("wing.t/c")),
            Flaps: new FlapGeometry(
                yaml.GetRequiredString("flaps.type"),
                yaml.GetRequiredDouble("flaps.area"),
                yaml.GetRequiredDouble("flaps.bf/b"),
                yaml.GetRequiredDouble("flaps.lambda_f"),
                yaml.GetRequiredDouble("flaps.cf/c"),
                yaml.GetRequiredDouble("flaps.Sf/S")),
            Cruise: new CruiseDefinition(
                yaml.GetRequiredDouble("cruise.height"),
                yaml.GetRequiredDouble("cruise.mach"),
                yaml.GetRequiredDouble("cruise.range")),
            Engines: new EngineInstallation(
                yaml.GetRequiredString("engine.type"),
                yaml.GetRequiredString("engine.mount"),
                yaml.GetRequiredInt("engine.number"),
                yaml.GetRequiredString("engine.default"),
                engineOptions,
                variantOptions),
            Drag: new DragDefinition(
                yaml.GetRequiredDouble("drag.cd0"),
                yaml.GetRequiredDouble("drag.k"),
                yaml.GetRequiredDouble("drag.e"),
                yaml.GetRequiredDouble("drag.gears")),
            Fuel: new FuelDefinition(
                yaml.GetRequiredString("fuel.engine"),
                yaml.GetRequiredDouble("fuel.fuel_coef")));
    }
}
