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
        int _writetimes = models.Count / 20000 ;
        if (_writetimes.Equals(0))
        {
            await index.AddDocumentsAsync(models);
        }
        else
        {
            int firstnumber = 0;
            for (int i = 0; i < _writetimes ; i++)
            {
                await index.AddDocumentsAsync(models.GetRange(firstnumber, 20000));
                firstnumber += 20000;
            }
            await index.AddDocumentsAsync(models.GetRange(firstnumber, models.Count % 20000));
        }
        // TODO : study full functions in "index"
    }

    public async Task UpdateSetting()
    {
        Settings newSettings = new Settings
        {
            RankingRules = new string[]
             {
                "words"
            }
        };
        TaskInfo task = await _client.Index(_index).UpdateSettingsAsync(newSettings);
    }
}
