// See https://aka.ms/new-console-template for more information
using HP.Pulsar.KeywordSearch.DataTransformation;
using HP.Pulsar.KeywordSearch.Orchestrator;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        /*
        DataProcessing Data = new DataProcessing();
        Console.WriteLine(Data.Lemmatize("product id / product name / partner / developer center / brands / " +
                                           "system board name / Service Life Date / Product Status / Business Segment / " +
                                           "Creator name / Created Date / Last Updater Name / Latest Update Date / System Manager /" +
                                           "Platform Development PM / Supply Chain Email / ODM System Engineering PM / Configuration Manager / " +
                                           "Commodity PM Email / Service / ODM HW PM / Program Office Program Manager / " +
                                           "Quality / Planning PM / BIOS PM / Systems Engineering PM / Marketing Product Mgmt / " +
                                           "Procurement PM / SW Marketing / Product Family / Release Team / Regulatory Model / " +
                                           "Releases / Description / Product Line / Preinstall Team / Machine PNP ID / " +
                                           "End Of Production / Product Groups / WHQLstatus / Lead Product / Chipsets / component items"));
        */
        // init
        Initialization init = new();
        init.Init();
        
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





