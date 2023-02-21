// See https://aka.ms/new-console-template for more information

using HP.Pulsar.Search.Keyword.Infrastructure;
using HP.Pulsar.Search.Keyword.Orchestrator;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // init
        KeywordSearchInfo info = new()
        {
            DatabaseConnectionString = "xxxxx",
            Environment = PulsarEnvironment.Dev,
            SearchEngineUrl = "http://15.36.147.177:7700/"
        };

        Initialization init = new(info);
        await init.InitAsync();

        /*
        // search
        Search search = new();
        IEnumerable<HP.Pulsar.KeywordSearch.CommonDataStructures.KeywordSearchOutputModel> models = search.search("System Manager Michael anna 1.0");

        // update
        Update update = new();
        update.update(HP.Pulsar.KeywordSearch.CommonDataStructures.SearchType.Product, 2018);
        */
    }
}





