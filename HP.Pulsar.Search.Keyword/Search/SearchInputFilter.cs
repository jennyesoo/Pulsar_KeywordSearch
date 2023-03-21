namespace HP.Pulsar.Search.Keyword.Search;

internal static class SearchInputFilter
{
    public static IEnumerable<string> Filter(string[] inputs)
    {
        if (inputs == null || inputs.Length == 0)
        {
            return Enumerable.Empty<string>();
        }

        List<string> output = new();

        foreach (string input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (TryParseInteger(input, out string o1))
            {
                output.Add(o1);
                continue;
            }

            if (TryParseDateTime(input, out string o2))
            {
                output.Add(o2);
                continue;
            }

            output.Add(input);
        }

        return output;
    }

    private static bool TryParseInteger(string input, out string output)
    {
        if (int.TryParse(input, out int o1))
        {
            output = $"\"{o1}\"";
            return true;
        }

        output = string.Empty;
        return false;
    }

    private static bool TryParseDateTime(string input, out string output)
    {
        if (DateTime.TryParse(input, out DateTime o1))
        {
            output = $"\"{o1:yyyy/MM/dd}\"";
            return true;
        }

        output = string.Empty;
        return false;
    }
}
