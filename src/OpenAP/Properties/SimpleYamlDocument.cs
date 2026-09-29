using System.Globalization;

namespace OpenAP.Properties;

internal sealed class SimpleYamlDocument
{
    private readonly Dictionary<string, string> _scalars;
    private readonly Dictionary<string, List<string>> _sequences;

    private SimpleYamlDocument(
        Dictionary<string, string> scalars,
        Dictionary<string, List<string>> sequences)
    {
        _scalars = scalars;
        _sequences = sequences;
    }

    public static SimpleYamlDocument Parse(string text)
    {
        var scalars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sequences = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<(int Indent, string Key)>();

        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var trimmedStart = rawLine.TrimStart();
            if (trimmedStart.StartsWith('#'))
                continue;

            var indent = rawLine.Length - trimmedStart.Length;

            while (stack.Count > 0 && stack[^1].Indent >= indent)
                stack.RemoveAt(stack.Count - 1);

            var line = StripInlineComment(trimmedStart).TrimEnd();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                var path = BuildPath(stack, null);
                if (!sequences.TryGetValue(path, out var values))
                {
                    values = [];
                    sequences[path] = values;
                }

                values.Add(Unquote(line[2..].Trim()));
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon < 0)
                continue;

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();

            if (value.Length == 0)
            {
                stack.Add((indent, key));
                continue;
            }

            scalars[BuildPath(stack, key)] = Unquote(value);
        }

        return new SimpleYamlDocument(scalars, sequences);
    }

    public string GetRequiredString(string path)
        => _scalars.TryGetValue(path, out var value)
            ? value
            : throw new InvalidDataException($"Required YAML value '{path}' was not found.");

    public string? GetOptionalString(string path)
    {
        if (!_scalars.TryGetValue(path, out var value))
            return null;

        return value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    public double GetRequiredDouble(string path)
        => double.Parse(
            GetRequiredString(path),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

    public double? GetOptionalDouble(string path)
    {
        var value = GetOptionalString(path);
        return value is null
            ? null
            : double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public int GetRequiredInt(string path)
        => int.Parse(
            GetRequiredString(path),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture);

    public IReadOnlyList<string> GetSequence(string path)
        => _sequences.TryGetValue(path, out var values)
            ? values
            : [];

    public IReadOnlyDictionary<string, string> GetMapping(string path)
    {
        var prefix = path + ".";
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in _scalars)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var child = key[prefix.Length..];
            if (!child.Contains('.'))
                result[child] = value;
        }

        return result;
    }

    private static string BuildPath(
        IReadOnlyList<(int Indent, string Key)> stack,
        string? leaf)
    {
        if (stack.Count == 0)
            return leaf ?? string.Empty;

        var prefix = string.Join('.', stack.Select(item => item.Key));
        return leaf is null ? prefix : $"{prefix}.{leaf}";
    }

    private static string StripInlineComment(string value)
    {
        var singleQuoted = false;
        var doubleQuoted = false;

        for (var i = 0; i < value.Length; i++)
        {
            switch (value[i])
            {
                case '\'' when !doubleQuoted:
                    singleQuoted = !singleQuoted;
                    break;
                case '"' when !singleQuoted:
                    doubleQuoted = !doubleQuoted;
                    break;
                case '#' when !singleQuoted && !doubleQuoted &&
                              (i == 0 || char.IsWhiteSpace(value[i - 1])):
                    return value[..i].TrimEnd();
            }
        }

        return value;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
