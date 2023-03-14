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
            new ComponentRootOrchestrator(info),
            new ComponentVersionOrchestrator(info) //time : 40m 
        };

        _info = info;
    }

    public async Task InitAsync()
    {
        int MeilisearchCount = 0; //568794 items
        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            Console.WriteLine("Read Data : " + item);
            MeilisearchCount = await item.InitializeAsync(MeilisearchCount); 
        }

    }

}
