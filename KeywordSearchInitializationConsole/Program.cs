// See https://aka.ms/new-console-template for more information

using HP.Pulsar.Search.Keyword.Infrastructure;
using HP.Pulsar.Search.Keyword.Orchestrator;
using HP.Pulsar.Search.Keyword.SearchEngine;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("start...");

        DateTime start = DateTime.Now;

        KeywordSearchInfo info = new()
        {
            DatabaseConnectionString = "server=TdcPulsarItgDb.tpc.rd.hpicorp.net;initial catalog=PRS;integrated security=SSPI",
            SearchEngineUrl = "http://15.36.147.177:7702/"
        };

        // init
        //InitializationClient init = new(info);
        //await init.InitAsync();

        // search
        SearchClient searchClient = new(info);
        IReadOnlyDictionary<SearchType, IEnumerable<SingleOutputModel>> models = await searchClient.SearchAsync("Lee Jovi");

        DateTime end = DateTime.Now;
        Console.WriteLine("total seconds = " + (end - start).TotalSeconds);

        //update
        //UpdateClient updateClient = new UpdateClient(info);
        //await updateClient.UpdateAsync(SearchType.Version, 2018);

        Console.WriteLine("Press any key to exit...");
        Console.Read();
    }
}
