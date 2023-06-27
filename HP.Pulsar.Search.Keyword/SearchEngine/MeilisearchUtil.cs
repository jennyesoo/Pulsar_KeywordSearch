using System.Text.Json;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

internal static class MeilisearchUtil
{
    public static IEnumerable<SingleOutputModel> ConvertOutput(string json)
    {
        List<SingleOutputModel> output = new();

        try
        {
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement hits = doc.RootElement.GetProperty("hits");

            foreach (JsonElement hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("Target", out JsonElement targetElement))
                {
                    continue;
                }

                string targetValue = targetElement.ToString();
                SearchType searchType = GetSearchType(targetValue);
                HashSet<string> hitProperties = new();
                int id = -1;
                string name = string.Empty;

                if (hit.TryGetProperty("_matchesPosition", out JsonElement matchesPosition))
                {
                    foreach (JsonProperty item in matchesPosition.EnumerateObject())
                    {
                        hitProperties.Add(item.Name);
                    }
                }

                List<KeyValuePair<string, string>> pairs = new();

                foreach (JsonProperty item in hit.EnumerateObject())
                {
                    if (string.Equals(item.Name, "_matchesPosition", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.Equals(item.Name, "Id", StringComparison.OrdinalIgnoreCase)
                        && TryGetId(item.Value.ToString(), out int idValue))
                    {
                        id = idValue;
                    }

                    if (string.Equals(item.Name, "Name", StringComparison.OrdinalIgnoreCase))
                    {
                        name = item.Value.ToString();
                    }

                    pairs.Add(new KeyValuePair<string, string>(item.Name, item.Value.ToString()));
                }

                output.Add(new SingleOutputModel(searchType, id, name, pairs, hitProperties));
            }
        }
        catch
        {
        }

        return output;
    }

    private static bool TryGetId(string input, out int id)
    {
        string[] temp = input.Split(new char[] { '-' });

        if (temp.Length == 2)
        {
            id = int.Parse(temp[1]);
            return true;
        }

        id = -1;
        return false;
    }

    private static SearchType GetSearchType(string input)
    {
        if (string.Equals(input, TargetTypeValue.Product, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Product;
        }

        if (string.Equals(input, TargetTypeValue.ComponentVersion, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Version;
        }

        if (string.Equals(input, TargetTypeValue.ComponentRoot, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Root;
        }

        if (string.Equals(input, TargetTypeValue.Dcr, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.DCR;
        }

        if (string.Equals(input, TargetTypeValue.AmoPartNumber, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.AmoPartNumber;
        }

        if (string.Equals(input, TargetTypeValue.ProductDrop, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.ProductDrop;
        }

        if (string.Equals(input, TargetTypeValue.Feature, StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Feature;
        }

        return SearchType.None;
    }
}
