using System.Globalization;

namespace OpenAP.Properties;

/// <summary>
/// Loads the empirical aircraft fuel models from OpenAP's fuel_models.csv.
/// </summary>
public sealed class FuelModelDatabase
{
    private readonly IReadOnlyDictionary<string, FuelModelDefinition> _models;

    public FuelModelDatabase(string fuelModelsCsvFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fuelModelsCsvFile);

        var path = Path.GetFullPath(fuelModelsCsvFile);
        _models = Load(path);
    }

    public static FuelModelDatabase FromOpenApDataDirectory(string dataDirectory)
        => new(Path.Combine(dataDirectory, "fuel", "fuel_models.csv"));

    /// <summary>
    /// Resolves the aircraft-specific fuel model or the OpenAP default model.
    /// </summary>
    public FuelModelDefinition Resolve(string aircraftIcaoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);

        var key = aircraftIcaoCode.Trim();

        if (_models.TryGetValue(key, out var model))
            return model;

        if (_models.TryGetValue("default", out var fallback))
            return fallback;

        throw new InvalidDataException(
            "OpenAP fuel model database does not contain a default model.");
    }

    public bool TryGet(
        string aircraftIcaoCode,
        out FuelModelDefinition model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);
        return _models.TryGetValue(aircraftIcaoCode.Trim(), out model);
    }

    private static IReadOnlyDictionary<string, FuelModelDefinition> Load(
        string path)
    {
        using var reader = new StreamReader(path);

        var headerLine = reader.ReadLine()
            ?? throw new InvalidDataException("OpenAP fuel-model CSV is empty.");

        var headers = CsvReader.ParseLine(headerLine);
        var indices = headers
            .Select((header, index) => (Header: header.Trim(), Index: index))
            .ToDictionary(
                item => item.Header,
                item => item.Index,
                StringComparer.OrdinalIgnoreCase);

        var result =
            new Dictionary<string, FuelModelDefinition>(
                StringComparer.OrdinalIgnoreCase);

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = CsvReader.ParseLine(line);

            string RequiredString(string name)
            {
                if (!indices.TryGetValue(name, out var index) ||
                    index >= fields.Count ||
                    string.IsNullOrWhiteSpace(fields[index]))
                {
                    throw new InvalidDataException(
                        $"Fuel-model CSV value '{name}' is missing.");
                }

                return fields[index].Trim();
            }

            double RequiredDouble(string name)
                => double.Parse(
                    RequiredString(name),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);

            var model = new FuelModelDefinition(
                TypeCode: RequiredString("typecode"),
                EngineType: RequiredString("engine_type"),
                C1: RequiredDouble("c1"),
                C2: RequiredDouble("c2"),
                C3: RequiredDouble("c3"));

            result[model.TypeCode] = model;
        }

        return result;
    }
}
