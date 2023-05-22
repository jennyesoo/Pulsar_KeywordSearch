using HP.Pulsar.Search.Keyword.DataWriter;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

public class Initialization
{
    private readonly List<IInitializationOrchestrator> _orchestrators;

    private readonly KeywordSearchInfo _keywordSearchInfo;

    public Initialization(KeywordSearchInfo info)
    {
        _keywordSearchInfo = info;
        _orchestrators = new()
        {
            new ProductOrchestrator(info),
            new ComponentRootOrchestrator(info),
            new ComponentVersionOrchestrator(info),
            new FeatureOrchestrator(info),
            new ChangeRequestOrchestrator(info),
            new ProductDropOrchestrator(info),
            new HpAMOPartNumberOrchestrator(info)
        };
    }

    public async Task InitAsync()
    {
        List<Task> tasks = new();

        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            tasks.Add(item.InitializeAsync());
        }

        await Task.WhenAll(tasks);

        MeiliSearchWriter writer = new(_keywordSearchInfo.SearchEngineUrl, _keywordSearchInfo.SearchEngineIndexName);

        if (await writer.UidExistsAsync(_keywordSearchInfo.SearchEngineIndexName))
        {
            await writer.UpdateSearchableAttributesAsync(ElementKeyContainer.Get());
        }

        // TODO - Determine if meilisearch finishs the job

    }
}
