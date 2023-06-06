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
            SearchEngineUrl = "http://15.36.147.177:7700/",
            SearchEngineIndexName = "pulsar"
        };

        // init
        //InitializationClient init = new(info);
        //await init.InitAsync();

        //// search
        SearchClient searchClient = new SearchClient(info);
        IReadOnlyDictionary<SearchType, List<SingleOutputModel>> models = await searchClient.SearchAsync("Lee Jovi");

        //DateTime end = DateTime.Now;
        //Console.WriteLine("total seconds = " + (end - start).TotalSeconds);

        //update
        //UpdateClient updateClient = new UpdateClient(info);
        //await updateClient.UpdateAsync(SearchType.Version, 2018);
    }
}
