using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

public class InitializationClient
{
    private readonly List<IInitializationOrchestrator> _orchestrators;
    private readonly KeywordSearchInfo _keywordSearchInfo;
    private readonly MeiliSearchClient _writer; 

    public InitializationClient(KeywordSearchInfo info)
    {
        _keywordSearchInfo = info;
        _writer = new(_keywordSearchInfo.SearchEngineUrl, _keywordSearchInfo.SearchEngineIndexName);
        _orchestrators = new()
        {
            new ProductOrchestrator(info,_writer),
            new ComponentRootOrchestrator(info,_writer),
            new ComponentVersionOrchestrator(info,_writer),
            new FeatureOrchestrator(info,_writer),
            new ChangeRequestOrchestrator(info,_writer),
            new ProductDropOrchestrator(info,_writer),
            new HpAMOPartNumberOrchestrator(info,_writer)
        };
    }

    public async Task InitAsync()
    {
        //await _writer.SendIndexDeletionAsync(); //for test
        await _writer.SendIndexCreationAsync();
        await _writer.SendUpdateSettingAsync();
        await _writer.SendUpdatePaginationAsync();

        List<Task> tasks = new();

        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            tasks.Add(item.InitializeAsync());
        }

        await Task.WhenAll(tasks);

        await _writer.UpdateSearchableAttributesAsync(ElementKeyContainer.Get());
    }
}
