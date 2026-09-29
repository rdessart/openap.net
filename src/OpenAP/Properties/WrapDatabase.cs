using System.Collections.Concurrent;

namespace OpenAP.Properties;

/// <summary>
/// Database for OpenAP WRAP kinematic models.
/// </summary>
public sealed class WrapDatabase
{
    private readonly string _wrapDirectory;
    private readonly string _synonymFile;
    private readonly ConcurrentDictionary<string, WrapModel> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<IReadOnlyDictionary<string, string>> _synonyms;

    public WrapDatabase(
        string wrapDirectory,
        string? synonymFile = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(wrapDirectory);

        _wrapDirectory = Path.GetFullPath(wrapDirectory);
        _synonymFile = synonymFile is null
            ? Path.Combine(_wrapDirectory, "_synonym.csv")
            : Path.GetFullPath(synonymFile);

        _synonyms = new Lazy<IReadOnlyDictionary<string, string>>(
            LoadSynonyms,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public static WrapDatabase FromOpenApDataDirectory(string dataDirectory)
        => new(Path.Combine(dataDirectory, "wrap"));

    public IReadOnlyList<string> AvailableAircraft(bool includeSynonyms = false)
    {
        var aircraft = Directory
            .EnumerateFiles(_wrapDirectory, "*.txt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (includeSynonyms)
        {
            aircraft.AddRange(
                _synonyms.Value.Keys
                    .OrderBy(name => name, StringComparer.Ordinal));
        }

        return aircraft;
    }

    /// <summary>
    /// Loads a WRAP model. Synonym resolution defaults to true to match
    /// Python OpenAP's WRAP constructor.
    /// </summary>
    public WrapModel Get(
        string aircraftIcaoCode,
        bool useSynonym = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraftIcaoCode);

        var requested = aircraftIcaoCode.Trim().ToLowerInvariant();
        var resolved = requested;
        var path = GetWrapPath(resolved);

        if (!File.Exists(path) &&
            useSynonym &&
            _synonyms.Value.TryGetValue(requested, out var synonym))
        {
            resolved = synonym;
            path = GetWrapPath(resolved);
        }

        if (!File.Exists(path))
        {
            throw new KeyNotFoundException(
                $"Kinematic model for '{requested}' is not available.");
        }

        return _cache.GetOrAdd(
            resolved,
            static (code, file) => new WrapModel(
                code.ToUpperInvariant(),
                WrapDataParser.Parse(file)),
            path);
    }

    private string GetWrapPath(string aircraftIcaoCode)
        => Path.Combine(_wrapDirectory, aircraftIcaoCode + ".txt");

    private IReadOnlyDictionary<string, string> LoadSynonyms()
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(_synonymFile))
            return result;

        foreach (var line in File.ReadLines(_synonymFile).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = CsvReader.ParseLine(line);
            if (fields.Count < 2)
                continue;

            result[fields[0].Trim()] =
                fields[1].Trim().ToLowerInvariant();
        }

        return result;
    }
}
