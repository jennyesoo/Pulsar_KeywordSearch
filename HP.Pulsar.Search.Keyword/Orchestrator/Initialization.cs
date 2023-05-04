using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

public class Initialization
{
    private readonly List<IInitializationOrchestrator> _orchestrators;

    public Initialization(KeywordSearchInfo info)
    {
        _orchestrators = new()
        {
            new ProductOrchestrator(info),
            new ComponentRootOrchestrator(info),
            new ComponentVersionOrchestrator(info),
            new FeatureOrchestrator(info),
            new ChangeRequestOrchestrator(info),
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
    }
}
