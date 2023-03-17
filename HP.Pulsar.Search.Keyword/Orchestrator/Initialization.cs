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
            new ComponentVersionOrchestrator(info) //time : 40m 
        };
    }

    public async Task InitAsync()
    {
        int count = 0; //568794 items

        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            Console.WriteLine("Read Data : " + item);
            count = await item.InitializeAsync(count);
        }
    }
}
