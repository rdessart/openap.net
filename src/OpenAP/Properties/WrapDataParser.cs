using System.Globalization;

namespace OpenAP.Properties;

internal static class WrapDataParser
{
    public static IReadOnlyDictionary<string, KinematicValue> Parse(string path)
    {
        using var reader = new StreamReader(path);

        var header = reader.ReadLine()
            ?? throw new InvalidDataException("WRAP data file is empty.");

        var phaseStart = RequiredColumn(header, "flight phase");
        var nameStart = RequiredColumn(header, "name");
        var optStart = RequiredColumn(header, "opt");
        var minStart = RequiredColumn(header, "min");
        var maxStart = RequiredColumn(header, "max");
        var modelStart = RequiredColumn(header, "model");
        var parametersStart = RequiredColumn(header, "parameters");

        var result =
            new Dictionary<string, KinematicValue>(
                StringComparer.OrdinalIgnoreCase);

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var variable = Slice(line, 0, phaseStart);
            var phase = Slice(line, phaseStart, nameStart);
            var name = Slice(line, nameStart, optStart);
            var defaultValue = ParseDouble(Slice(line, optStart, minStart));
            var minimum = ParseDouble(Slice(line, minStart, maxStart));
            var maximum = ParseDouble(Slice(line, maxStart, modelStart));
            var model = Slice(line, modelStart, parametersStart);

            var parameterText =
                parametersStart < line.Length
                    ? line[parametersStart..].Trim()
                    : string.Empty;

            var parameters = parameterText
                .Split(
                    '|',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(ParseDouble)
                .ToArray();

            if (variable.Length == 0)
                continue;

            result[variable] = new KinematicValue(
                Variable: variable,
                FlightPhase: phase,
                Name: name,
                DefaultValue: defaultValue,
                Minimum: minimum,
                Maximum: maximum,
                StatisticalModel: model,
                StatisticalModelParameters: parameters);
        }

        return result;
    }

    private static int RequiredColumn(string header, string name)
    {
        var index = header.IndexOf(name, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidDataException(
                $"WRAP header does not contain required column '{name}'.");
        }

        return index;
    }

    private static string Slice(
        string line,
        int start,
        int end)
    {
        if (start >= line.Length)
            return string.Empty;

        var length = Math.Min(end, line.Length) - start;
        if (length <= 0)
            return string.Empty;

        return line.Substring(start, length).Trim();
    }

    private static double ParseDouble(string value)
        => double.Parse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
}
