using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.SearchEngine;

internal class MeiliSearchClient
{
    private readonly MeilisearchClient _client;
    private readonly string _meilisearchEngineUrl;
    private readonly string _indexName;
    private readonly string _idKeyName = "Id";
    private readonly string _targetKeyName = "Target";

    public MeiliSearchClient(string meilisearchEngineUrl, string indexName)
    {
        if (string.IsNullOrWhiteSpace(meilisearchEngineUrl))
        {
            throw new ArgumentNullException(nameof(meilisearchEngineUrl));
        }

        if (string.IsNullOrWhiteSpace(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        _client = new(meilisearchEngineUrl, "masterKey");
        _indexName = indexName;

        _meilisearchEngineUrl = meilisearchEngineUrl;
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to create an index initialization step
    /// When this method completes, it doesn't mean the index has been created in meilisearch.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task InitializeIndexCreationStepsAsync(IEnumerable<CommonDataModel> allDocuments, IReadOnlyCollection<string> allProperty, int num)
    {
        await SendIndexDeletionAsync();
        await SendIndexCreationAsync();
        await SendUpdateSettingAsync();
        await SendUpdatePaginationAsync();
        await SendElementsCreationAsync(allDocuments, num);
        await UpdateSearchableAttributesAsync(allProperty);
        await UpdateDisplayedAttributesAsync(allProperty);
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to create an index
    /// When this method completes, it doesn't mean the index has been created in meilisearch.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task SendIndexCreationAsync()
    {
        await _client.CreateIndexAsync(_indexName, "Id");
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to delete an index
    /// When this method completes, it doesn't mean the index has been deleted in meilisearch.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task SendIndexDeletionAsync()
    {
        await _client.DeleteIndexAsync(_indexName);
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to create a new element.
    /// When this method completes, it doesn't mean the new element has been created in meilisearch.
    /// </summary>
    /// <param name="elements"></param>
    /// <exception cref="ArgumentException"></exception>
    public async Task SendElementsCreationAsync(IEnumerable<CommonDataModel> elements, int num)
    {
        if (elements?.Any() != true)
        {
            return;
        }

        Meilisearch.Index index = _client.Index(_indexName);
        List<IReadOnlyDictionary<string, string>> pairs = new();
        foreach (CommonDataModel product in elements)
        {
            pairs.Add(product.GetElements());
        }

        if (pairs.Count > num)
        {
            for (int i = 0; i <= (pairs.Count) / num; i++)
            {
                Console.WriteLine("First: " + num * i);
                Console.WriteLine("Final: " + (num * i + num));
                //Console.WriteLine("(pairs.Count) / num): " + (pairs.Count) / num);
                //Console.WriteLine("pairs.Count - num * i: " + (pairs.Count - num * i));
                //Console.WriteLine("i: " + i);
                //Console.WriteLine("-----------------------");

                if (i.Equals((pairs.Count) / num))
                {
                    await index.AddDocumentsAsync(pairs.GetRange(num * i, (pairs.Count - num * i)));
                }
                else
                {
                    await index.AddDocumentsAsync(pairs.GetRange(num * i, num));
                    //Console.WriteLine("-----------------------");
                }
            }
        }
        else
        {
            await index.AddDocumentsAsync(pairs);
        }
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to update a new element.
    /// When this method completes, it doesn't mean the new element has been updated in meilisearch.
    /// </summary>
    /// <param name="elements"></param>
    /// <exception cref="ArgumentException"></exception>
    public async Task SendElementUpdationAsync(CommonDataModel element)
    {
        if (element.GetElements()?.Any() != true)
        {
            return;
        }

        Meilisearch.Index index = _client.Index(_indexName);
        List<IReadOnlyDictionary<string, string>> pairs = new()
        {
            element.GetElements()
        };
        await index.UpdateDocumentsAsync(pairs);
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to update index setting.
    /// When this method completes, it doesn't mean the setting has been updated in meilisearch.
    /// </summary>
    public async Task SendUpdateSettingAsync()
    {
        Settings newSettings = new()
        {
            RankingRules = new string[]
            {
                "words"
            }
        };

        await _client.Index(_indexName).UpdateSettingsAsync(newSettings);
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to update pagination.
    /// When this method completes, it doesn't mean the pagination has been updated in meilisearch.
    /// </summary>
    public async Task SendUpdatePaginationAsync()
    {
        Pagination pagination = new()
        {
            MaxTotalHits = 1000000
        };

        await _client.Index(_indexName).UpdatePaginationAsync(pagination);
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to update search-able attributes.
    /// When this method completes, it doesn't mean the attributes has been updated in meilisearch.
    /// </summary>
    /// <param name="displayedAttributes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task UpdateDisplayedAttributesAsync(IEnumerable<string> displayedAttributes)
    {
        if (displayedAttributes == null)
        {
            throw new ArgumentNullException(nameof(displayedAttributes));
        }

        if (displayedAttributes?.Any() != true)
        {
            return;
        }

        await _client.Index(_indexName).UpdateDisplayedAttributesAsync(displayedAttributes);
    }

    /// <summary>
    /// This method is only for sending signal to meilisearch to update search-able attributes.
    /// When this method completes, it doesn't mean the attributes has been updated in meilisearch.
    /// </summary>
    /// <param name="searchableAttributes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task UpdateSearchableAttributesAsync(IEnumerable<string> searchableAttributes)
    {
        if (searchableAttributes == null)
        {
            throw new ArgumentNullException(nameof(searchableAttributes));
        }

        if (searchableAttributes?.Any() != true)
        {
            return;
        }

        await _client.Index(_indexName).UpdateSearchableAttributesAsync(searchableAttributes);
    }

    public async Task<IEnumerable<SingleOutputModel>> SearchAsync(SearchParameters parameters)
    {
        IEnumerable<SingleOutputModel> output = await TempSearchAsync(JsonSerializer.Serialize(parameters));

        return output;
    }

    private string GetSearchUrl() => $"{_meilisearchEngineUrl}/indexes/{_indexName}/search";

    private async Task<IEnumerable<SingleOutputModel>> TempSearchAsync(string json)
    {
        // TODO - This is a temp solution. Need to work on HttpClientFactory in library
        using HttpClient client = new();

        StringContent content = new(json, Encoding.UTF8, "application/json");

        //msg.Headers.Add("Content-Type", "application/json");
        using HttpResponseMessage response = await client.PostAsync(GetSearchUrl(), content);
        string result = await response.Content.ReadAsStringAsync();

        // data value parsing logic
        var output = MeilisearchUtil.ConvertOutput(result);

        return output;
    }

    private bool TryGetTargetId(IReadOnlyDictionary<string, string> input, out int id)
    {
        id = 0;

        if (input?.Any() != true)
        {
            return false;
        }

        if (input.ContainsKey(_idKeyName))
        {
            string[] temp = input[_idKeyName].Split('-');

            if (temp.Length != 2)
            {
                return false;
            }

            if (int.TryParse(temp[1], out id))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetTargetName(IReadOnlyDictionary<string, string> input, out string name)
    {
        name = string.Empty;

        if (input?.Any() != true)
        {
            return false;
        }

        if (input.TryGetValue(TargetName.AmoPartNumber, out string value1))
        {
            name = value1;
            return true;
        }

        if (input.TryGetValue(TargetName.ComponentVersion, out string value2))
        {
            name = value2;
            return true;
        }

        if (input.TryGetValue(TargetName.ComponentRoot, out string value3))
        {
            name = value3;
            return true;
        }

        if (input.TryGetValue(TargetName.Dcr, out string value4))
        {
            name = value4;
            return true;
        }

        if (input.TryGetValue(TargetName.Feature, out string value5))
        {
            name = value5;
            return true;
        }

        if (input.TryGetValue(TargetName.Product, out string value6))
        {
            name = value6;
            return true;
        }

        if (input.TryGetValue(TargetName.ProductDrop, out string value7))
        {
            name = value7;
            return true;
        }

        return false;
    }

    private bool TryGetTargetType(IReadOnlyDictionary<string, string> input, out SearchType type)
    {
        if (input?.Any() != true
            || !input.ContainsKey(_targetKeyName))
        {
            type = SearchType.None;
            return false;
        }

        string targetValue = input[_targetKeyName];

        if (string.Equals(targetValue, TargetTypeValue.Product, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.Product;
            return true;
        }

        if (string.Equals(targetValue, TargetTypeValue.ComponentRoot, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.Root;
            return true;
        }

        if (string.Equals(targetValue, TargetTypeValue.ComponentVersion, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.Version;
            return true;
        }

        if (string.Equals(targetValue, TargetTypeValue.ProductDrop, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.ProductDrop;
            return true;
        }

        if (string.Equals(targetValue, TargetTypeValue.Dcr, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.DCR;
            return true;
        }

        if (string.Equals(targetValue, TargetTypeValue.Feature, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.Feature;
            return true;
        }

        if (string.Equals(targetValue, TargetTypeValue.AmoPartNumber, StringComparison.OrdinalIgnoreCase))
        {
            type = SearchType.AmoPartNumber;
            return true;
        }

        type = SearchType.None;
        return false;
    }

    private List<KeyValuePair<string, string>> GetHitProperties(IReadOnlyDictionary<string, string> input)
    {
        return input.ToList();
    }
}
