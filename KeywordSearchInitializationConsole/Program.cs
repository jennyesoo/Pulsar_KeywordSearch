// See https://aka.ms/new-console-template for more information

using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using HP.Pulsar.Search.Keyword.Orchestrator;
using static System.Net.Mime.MediaTypeNames;

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
            SearchEngineIndexName = "Pulsar3"
        };

        TestForRex test = new(info);
        IEnumerable<CommonDataModel> testData = await test.GetDataAsync();

        //// init
        //InitializationClient init = new(info);
        //await init.InitAsync();

        //// search
        //SearchClient searchClient = new SearchClient(info);
        //IReadOnlyDictionary<SearchType, List<SingleOutputModel>> models = await searchClient.SearchAsync("Michael anna 1.0 Foxconn");

        //DateTime end = DateTime.Now;
        //Console.WriteLine("total seconds = " + (end - start).TotalSeconds);

        // update
        //Update update = new();
        //update.update(HP.Pulsar.KeywordSearch.CommonDataStructures.SearchType.Product, 2018);

    }
}
