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
        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            Console.WriteLine("Read Data : " + item);
            await item.InitializeAsync();
        }

        MeiliSearchWriter writer = new(_keywordSearchInfo.SearchEngineUrl, _keywordSearchInfo.SearchEngineIndexName);
        if (await writer.UidExistsAsync(_keywordSearchInfo.SearchEngineIndexName))
        {
            await writer.UpdateSearchableAttributesAsync(ElementKeyContainer.Get());
        }
    }
}
