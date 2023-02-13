namespace HP.Pulsar.Search.Keyword.Orchestrator;

public class Initialization
{
    private List<IInitializationOrchestrator> _orchestrators;

    public Initialization()
    {
        _orchestrators = new()
        {
            new ProductOrchestrator()
        };

    }

    public async Task InitAsync()
    {
        foreach (IInitializationOrchestrator item in _orchestrators)
        {
            await item.InitializeAsync();
        }

    }

}
