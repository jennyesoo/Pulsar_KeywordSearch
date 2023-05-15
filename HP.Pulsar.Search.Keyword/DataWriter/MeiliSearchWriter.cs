using System.Xml.Linq;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.DataWriter;

public class MeiliSearchWriter
{
    private readonly MeilisearchClient _client;
    private readonly string _uid;

    public MeiliSearchWriter(string url, string uid)
    {
        _client = new(url, "masterKey");
        _uid = uid;
    }

    public async Task CreateIndexAsync()
    {
        if (await UidExistsAsync(_uid))
        {
            throw new ArgumentException("Duplicated uid");
        }

        await _client.CreateIndexAsync(_uid, "Id");
    }

    public async Task DeleteIndexAsync()
    {
        if (await UidExistsAsync(_uid))
        {
            await _client.DeleteIndexAsync(_uid);
            return;
        }

        throw new ArgumentException("UID not found");
    }

    public async Task<bool> UidExistsAsync(string uid)
    {
        ResourceResults<IEnumerable<Meilisearch.Index>> indexes = await _client.GetAllIndexesAsync();

        foreach (Meilisearch.Index index in indexes.Results)
        {
            if (string.Equals(index.Uid, uid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public async Task AddElementsAsync(IEnumerable<CommonDataModel> elements)
    {
        if (!await UidExistsAsync(_uid))
        {
            throw new ArgumentException("UID not found");
        }

        if (elements?.Any() != true)
        {
            return;
        }

        Meilisearch.Index index = _client.Index(_uid);
        List<IReadOnlyDictionary<string, string>> pairs = new();
        foreach (CommonDataModel product in elements)
        {
            pairs.Add(product.GetElements());
        }
        await index.AddDocumentsAsync(pairs);
    }
  

    // TODO - update element missing

    public async Task UpdateSettingAsync()
    {
        if (!await UidExistsAsync(_uid))
        {
            throw new ArgumentException("UID not found");
        }

        Settings newSettings = new()
        {
            RankingRules = new string[]
            {
                "words"
            }
        };

        await _client.Index(_uid).UpdateSettingsAsync(newSettings);
    }

    public async Task UpdatePaginationAsync()
    {
        if (!await UidExistsAsync(_uid))
        {
            throw new ArgumentException("UID not found");
        }

        var pagination = new Pagination
        {
            MaxTotalHits = 1000000
        };

        await _client.Index(_uid).UpdatePaginationAsync(pagination);
    }

    public async Task UpdateSearchableAttributesAsync(IEnumerable<string> searchableAttributes)
    {
        if (!await UidExistsAsync(_uid))
        {
            throw new ArgumentException("UID not found");
        }

        if (searchableAttributes == null)
        {
            throw new ArgumentException("searchableAttributes not found");
        }

        if (searchableAttributes?.Any() != true)
        {
            return;
        }

        await _client.Index(_uid).UpdateSearchableAttributesAsync(searchableAttributes);
    }
}
