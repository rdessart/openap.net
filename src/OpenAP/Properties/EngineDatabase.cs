using System.Globalization;

namespace OpenAP.Properties;

/// <summary>
/// Strongly typed reader for the OpenAP ICAO engine database.
/// </summary>
public sealed class EngineDatabase
{
    private readonly string _engineCsvFile;
    private readonly Lazy<IReadOnlyList<EngineDefinition>> _engines;

    public EngineDatabase(string engineCsvFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineCsvFile);

        _engineCsvFile = Path.GetFullPath(engineCsvFile);
        _engines = new Lazy<IReadOnlyList<EngineDefinition>>(
            LoadEngines,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public static EngineDatabase FromOpenApDataDirectory(string dataDirectory)
        => new(Path.Combine(dataDirectory, "engine", "engines.csv"));

    /// <summary>
    /// Matches OpenAP prop.search_engine(): prefix search over engine names.
    /// </summary>
    public IReadOnlyList<string> Search(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var normalized = prefix.Trim().ToUpperInvariant();

        return _engines.Value
            .Where(engine => engine.Name.StartsWith(normalized, StringComparison.Ordinal))
            .Select(engine => engine.Name)
            .ToArray();
    }

    /// <summary>
    /// Matches OpenAP prop.engine(): case-insensitive prefix lookup, returning
    /// the first database row that matches the requested engine identifier.
    /// </summary>
    public EngineDefinition Get(string engineName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineName);

        var requested = engineName.Trim();

        var engine = _engines.Value.FirstOrDefault(
            item => item.Name.StartsWith(requested, StringComparison.OrdinalIgnoreCase));

        if (engine is null)
            throw new KeyNotFoundException($"Engine '{requested}' is not available.");

        // Python OpenAP replaces the returned dictionary's name with the request.
        return engine with { Name = requested };
    }

    private IReadOnlyList<EngineDefinition> LoadEngines()
    {
        using var reader = new StreamReader(_engineCsvFile);

        var headerLine = reader.ReadLine()
            ?? throw new InvalidDataException("OpenAP engine CSV is empty.");

        var headers = CsvReader.ParseLine(headerLine);
        var indices = headers
            .Select((header, index) => (header, index))
            .ToDictionary(item => item.header, item => item.index, StringComparer.OrdinalIgnoreCase);

        var result = new List<EngineDefinition>();

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = CsvReader.ParseLine(line);

            // A few rows in the upstream ICAO table are intentionally sparse.
            // Pandas represents those cells as NaN. For the strongly typed
            // performance model we only materialize rows with the complete
            // core data required by OpenAP thrust/fuel calculations.
            if (!HasCoreEngineData(fields, indices))
                continue;

            string RequiredString(string name)
            {
                var value = GetField(fields, indices, name);
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException($"Engine CSV value '{name}' is missing.");
                return value;
            }

            double RequiredDouble(string name)
                => double.Parse(
                    RequiredString(name),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);

            double? OptionalDouble(string name)
            {
                var value = GetField(fields, indices, name);
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return double.Parse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }

            var maximumThrust = RequiredDouble("max_thrust");
            var takeoffFuelFlow = RequiredDouble("ff_to");
            var cruiseSfc = OptionalDouble("cruise_sfc");
            var cruiseAltitude = OptionalDouble("cruise_alt");

            var fuelCorrectionSlope =
                cruiseSfc.HasValue && cruiseAltitude.HasValue &&
                double.IsFinite(cruiseSfc.Value) &&
                double.IsFinite(cruiseAltitude.Value)
                    ? Math.Round(
                        (cruiseSfc.Value -
                         takeoffFuelFlow / (maximumThrust / 1000.0)) /
                        (cruiseAltitude.Value * 0.3048),
                        8,
                        MidpointRounding.ToEven)
                    : 6.7e-7;

            result.Add(
                new EngineDefinition(
                    Uid: RequiredString("uid"),
                    Name: RequiredString("name"),
                    Manufacturer: RequiredString("manufacturer"),
                    Type: RequiredString("type"),
                    BypassRatio: RequiredDouble("bpr"),
                    PressureRatio: RequiredDouble("pr"),
                    MaximumThrustNewton: maximumThrust,
                    Emissions: new EngineEmissionIndices(
                        RequiredDouble("ei_hc_to"),
                        RequiredDouble("ei_hc_co"),
                        RequiredDouble("ei_hc_app"),
                        RequiredDouble("ei_hc_idl"),
                        RequiredDouble("ei_co_to"),
                        RequiredDouble("ei_co_co"),
                        RequiredDouble("ei_co_app"),
                        RequiredDouble("ei_co_idl"),
                        RequiredDouble("ei_nox_to"),
                        RequiredDouble("ei_nox_co"),
                        RequiredDouble("ei_nox_app"),
                        RequiredDouble("ei_nox_idl")),
                    FuelFlows: new EngineFuelFlows(
                        takeoffFuelFlow,
                        RequiredDouble("ff_co"),
                        RequiredDouble("ff_app"),
                        RequiredDouble("ff_idl")),
                    LandingTakeoffFuelKg: RequiredDouble("fuel_lto"),
                    CruiseThrustNewton: OptionalDouble("cruise_thrust"),
                    CruiseSpecificFuelConsumption: cruiseSfc,
                    CruiseMach: OptionalDouble("cruise_mach"),
                    CruiseAltitudeFeet: cruiseAltitude,
                    FuelCorrectionSlope: fuelCorrectionSlope));
        }

        return result;
    }

    private static bool HasCoreEngineData(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> indices)
    {
        string[] required =
        [
            "uid", "name", "manufacturer", "type", "bpr", "pr", "max_thrust",
            "ei_hc_to", "ei_hc_co", "ei_hc_app", "ei_hc_idl",
            "ei_co_to", "ei_co_co", "ei_co_app", "ei_co_idl",
            "ei_nox_to", "ei_nox_co", "ei_nox_app", "ei_nox_idl",
            "ff_to", "ff_co", "ff_app", "ff_idl", "fuel_lto"
        ];

        return required.All(
            name => !string.IsNullOrWhiteSpace(GetField(fields, indices, name)));
    }

    private static string GetField(
        IReadOnlyList<string> fields,
        IReadOnlyDictionary<string, int> indices,
        string name)
    {
        if (!indices.TryGetValue(name, out var index) || index >= fields.Count)
            return string.Empty;

        return fields[index].Trim();
    }
}
