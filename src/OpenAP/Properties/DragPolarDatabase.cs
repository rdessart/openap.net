using System.Collections.Concurrent;

namespace OpenAP.Properties;

/// <summary>
/// Loads OpenAP drag-polar definitions from data/dragpolar.
/// </summary>
public sealed class DragPolarDatabase
{
    private readonly string _dragPolarDirectory;
    private readonly string _synonymFile;
    private readonly ConcurrentDictionary<string, DragPolarDefinition> _cache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<IReadOnlyDictionary<string, string>> _synonyms;

    public DragPolarDatabase(
        string dragPolarDirectory,
        string? synonymFile = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dragPolarDirectory);

        _dragPolarDirectory = Path.GetFullPath(dragPolarDirectory);
        _synonymFile = synonymFile is null
            ? Path.Combine(_dragPolarDirectory, "_synonym.csv")
            : Path.GetFullPath(synonymFile);

        _synonyms = new Lazy<IReadOnlyDictionary<string, string>>(
            LoadSynonyms,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public static DragPolarDatabase FromOpenApDataDirectory(string dataDirectory)
        => new(Path.Combine(dataDirectory, "dragpolar"));

    public IReadOnlyList<string> AvailableDragPolars(bool includeSynonyms = false)
    {
        var result = Directory
            .EnumerateFiles(_dragPolarDirectory, "*.yml", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (includeSynonyms)
        {
            result.AddRange(
                _synonyms.Value.Keys.OrderBy(name => name, StringComparer.Ordinal));
        }

        return result;
    }

    public DragPolarDefinition Get(
        string icaoCode,
        bool useSynonym = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icaoCode);

        var requested = icaoCode.Trim().ToLowerInvariant();
        var resolved = requested;
        var file = GetPolarPath(resolved);

        if (!File.Exists(file) &&
            useSynonym &&
            _synonyms.Value.TryGetValue(requested, out var synonym))
        {
            resolved = synonym;
            file = GetPolarPath(resolved);
        }

        if (!File.Exists(file))
        {
            var suffix = useSynonym
                ? " and no synonym was found"
                : ". Try useSynonym=true if an OpenAP synonym is acceptable";

            throw new KeyNotFoundException(
                $"Drag polar for '{requested}' is not available{suffix}.");
        }

        return _cache.GetOrAdd(
            resolved,
            static (code, path) => Parse(code, path),
            file);
    }

    private string GetPolarPath(string code)
        => Path.Combine(_dragPolarDirectory, code + ".yml");

    private IReadOnlyDictionary<string, string> LoadSynonyms()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(_synonymFile))
            return result;

        foreach (var line in File.ReadLines(_synonymFile).Skip(1))
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

    private static DragPolarDefinition Parse(
        string icaoCode,
        string file)
    {
        var yaml = SimpleYamlDocument.Parse(File.ReadAllText(file));

        return new DragPolarDefinition(
            IcaoCode: icaoCode.ToUpperInvariant(),
            AircraftName: yaml.GetRequiredString("aircraft"),
            Clean: new DragPolarClean(
                yaml.GetRequiredDouble("clean.cd0"),
                yaml.GetRequiredDouble("clean.k"),
                yaml.GetRequiredDouble("clean.e")),
            GearDragCoefficient: yaml.GetRequiredDouble("gears"),
            Flaps: new DragPolarFlaps(
                yaml.GetRequiredDouble("flaps.lambda_f"),
                yaml.GetRequiredDouble("flaps.cf/c"),
                yaml.GetRequiredDouble("flaps.Sf/S")));
    }
}
