using HP.Pulsar.Search.Keyword.Infrastructure;

namespace HP.Pulsar.Search.Keyword.Orchestrator;

public class Initialization
{
    private readonly KeywordSearchInfo _info;
    private List<IInitializationOrchestrator> _orchestrators;

    public Initialization(KeywordSearchInfo info)
    {
        _orchestrators = new()
        {
            new ProductOrchestrator(info),
            new ComponentRootOrchestrator(info)
        };

        _info = info;
    }

    public async Task InitAsync()
    {
        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            await item.InitializeAsync();
        }

    }

}
