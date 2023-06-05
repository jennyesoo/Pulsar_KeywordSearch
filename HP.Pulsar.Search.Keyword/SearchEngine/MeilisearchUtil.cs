using System;
using System.Collections.Generic;
using System.Text;
using Meilisearch;
using System.Text.Json;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

internal static class MeilisearchUtil
{
    public static IReadOnlyDictionary<SearchType, List<SingleOutputModel>> ConvertOutput(string json)
    {
        Dictionary<SearchType, List<SingleOutputModel>> dict = new();

        try
        {
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement hits = doc.RootElement.GetProperty("hits");

            foreach (JsonElement hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("target", out JsonElement targetElement))
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

                    if (string.Equals(item.Name, "id", StringComparison.OrdinalIgnoreCase)
                        && TryGetId(item.Value.ToString(), out int idValue))
                    {
                        id = idValue;
                    }

                    if (string.Equals(item.Name, "name", StringComparison.OrdinalIgnoreCase))
                    {
                        name = item.Value.ToString();
                    }

                    pairs.Add(new KeyValuePair<string, string>(item.Name, item.Value.ToString()));
                }

                SingleOutputModel model = new(searchType, id, name, pairs, hitProperties);

                if (dict.ContainsKey(searchType))
                {
                    dict[searchType].Add(model);
                }
                else
                {
                    dict.Add(searchType, new List<SingleOutputModel> { model });
                }
            }
        }
        catch
        {
        }

        return dict;
    }

    private static bool TryGetId(string input, out int id)
    {
        string[] temp = input.Split(new char[] { '-' });

        if (temp.Length == 2 )
        {
            id = int.Parse(temp[1]);
            return true;
        }

        id = -1;
        return false;
    }

    private static SearchType GetSearchType(string input)
    {
        if (string.Equals(input, "product", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Product;
        }

        if (string.Equals(input, "ComponentVersion", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Version;
        }

        if (string.Equals(input, "ComponentRoot", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Root;
        }

        if (string.Equals(input, "dcr", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.DCR;
        }

        if (string.Equals(input, "AmoPartNumber", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.AmoPartNumber;
        }

        if (string.Equals(input, "ProductDrop", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.ProductDrop;
        }

        if (string.Equals(input, "Feature", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.Feature;
        }

        if (string.Equals(input, "PRL", StringComparison.OrdinalIgnoreCase))
        {
            return SearchType.PRL;
        }

        return SearchType.None;
    }
}
