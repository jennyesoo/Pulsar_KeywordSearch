using HP.Pulsar.KeywordSearch.CommonDataStructures;
using HP.Pulsar.KeywordSearch.DataReader;
using HP.Pulsar.KeywordSearch.DataTransformation;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP.Pulsar.KeywordSearch.Orchestrator
{
    public class Initialization
    {
        public Initialization()
        {

        }

        public void Init()
        {
            /*
            string source = "My name is Marco and I'm from Italy";
            var tokens = source.Split(" ");

            foreach (string line in tokens)
            {
                Console.WriteLine(line);
            }

            Console.Write(tokens[0]);
            */

            PulsarEnvironment env = PulsarEnvironment.Test;
            SqlServerStringCommand sql_command = new SqlServerStringCommand();
            ProductReader productReader = new(env, sql_command.productversion_command_all);
            List<CommonDataStructures.ProductDataModel> products = productReader.GetProducts();

            /*
            products.ForEach(i => Console.Write("EndOfProduction: {0}\t\n, " +
                                                "WHQLstatus: {1}\t\n" +
                                                "LeadProduct: {2}\t\n" +
                                                "Chipsets: {3}\t\n" +
                                                "ProductGroups: {4}\t\n" +
                                                "CurrentBIOSVersions: {5}\t\n" +
                                                "Target: {6}\t\n" +
                                                "ID: {7}\t\n", (i.EndOfProduction,
                                                                i.WHQLstatus,
                                                                i.LeadProduct,
                                                                i.Chipsets,
                                                                i.ProductGroups,
                                                                i.CurrentBIOSVersions,
                                                                i.Target,
                                                                i.ID)));
            */
            Console.Write("---------------------------------------");


            /*
            //----------------- EndOfProduction test ------------------------
            PropertyProcessing pp = new PropertyProcessing();
            string aaaa = pp.EndOfProduction(env, "2018");
            Console.WriteLine(aaaa);
            //-----------------------------------------
            */








        }

    }
}
