using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

public class InitializationClient
{
    private readonly List<IInitializationOrchestrator> _orchestrators;

    private readonly KeywordSearchInfo _keywordSearchInfo;

    public InitializationClient(KeywordSearchInfo info)
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
        MeiliSearchClient writer = new(_keywordSearchInfo.SearchEngineUrl, _keywordSearchInfo.SearchEngineIndexName);
        await writer.SendIndexCreationAsync();
        await writer.SendUpdateSettingAsync();
        await writer.SendUpdatePaginationAsync();

        List<Task> tasks = new();

        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            tasks.Add(item.InitializeAsync());
        }

        await Task.WhenAll(tasks);

        await writer.UpdateSearchableAttributesAsync(ElementKeyContainer.Get());
    }
}
