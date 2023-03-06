using HP.Pulsar.Search.Keyword.CommonDataStructure;
using Meilisearch;

namespace HP.Pulsar.Search.Keyword.DataWriter;

public class MeiliSearchWriter
{
    private readonly MeilisearchClient _client;
    private readonly string _index;

    public MeiliSearchWriter(string url, string indexName)
    {
        _client = new(url, "masterKey");
        _index = indexName;
    }

    public async Task CreateIndexAsync()
    {
        await _client.CreateIndexAsync(_index, "Id");
    }

    public async Task DeleteIndexAsync()
    {
        await _client.DeleteIndexAsync(_index);
    }

    public async Task UpsertAsync(List<Dictionary<string, string>> models)
    {
        Meilisearch.Index index = _client.Index(_index);
        await index.AddDocumentsAsync(models);
        // TODO : study full functions in "index"
    }
}
