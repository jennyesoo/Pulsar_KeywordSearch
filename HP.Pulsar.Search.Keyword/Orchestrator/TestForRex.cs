using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Humanizer;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;

namespace HP.Pulsar.Search.Keyword.Orchestrator
{
    public class TestForRex
    {

        private readonly KeywordSearchInfo _info;

        public TestForRex(KeywordSearchInfo info)
        {
            _info = info;
        }

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
        {
            IEnumerable<CommonDataModel> products = await GetPropertyValueAsync(GetAVdetailCommandText());

            HashSet<string> strings11 = new HashSet<string>();
            HashSet<string> strings10 = new HashSet<string>();
            HashSet<string> strings12 = new HashSet<string>();
            HashSet<string> strings = new HashSet<string>();
            HashSet<string> strings13 = new HashSet<string>();
            HashSet<string> strings4 = new HashSet<string>();
            HashSet<string> strings8 = new HashSet<string>();
            HashSet<string> strings7 = new HashSet<string>();
            HashSet<string> strings15 = new HashSet<string>();
            HashSet<string> strings9 = new HashSet<string>();

            foreach (CommonDataModel product in products)
            {
                if (product.GetValue("Name").Count() == 0)
                {
                    continue;
                }
                //else if (product.GetValue("Name").Count() != 11
                //    && product.GetValue("Name").Count() != 10)
                //{
                //    Console.WriteLine(product.GetValue("Name").Count());
                //    Console.WriteLine(product.GetValue("Name"));
                //}
                //else if (product.GetValue("Name").Count() == 10)
                //{
                //    strings10.Add(product.GetValue("Name"));
                //}
                //else if (Regex.IsMatch(product.GetValue("Name"), @"[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{3}[A-Za-z]{1}[A-Za-z0-9]{3}"))
                //{
                //    strings11.Add(product.GetValue("Name"));
                //}
                //else if (Regex.IsMatch(product.GetValue("Name"), @"[0-9]{2}[A-Za-z0-9]{2}[A-Za-z]{3}[A-Za-z0-9\#]{3}"))
                //{
                //    strings10.Add(product.GetValue("Name"));
                //}
                else if (product.GetValue("Name").Count() == 11)
                {
                    if (Regex.IsMatch(product.GetValue("Name"), @"[A-Za-z0-9]{4}[0-9]{1}[A-Za-z]{2}"))
                    {
                        strings11.Add(product.GetValue("Name"));
                    }
                    else
                    {
                        strings10.Add(product.GetValue("Name"));
                    }
                }
                else if (product.GetValue("Name").Count() == 7)
                {
                    if (Regex.IsMatch(product.GetValue("Name"), @"[A-Za-z0-9]{3}[0-9]{2}AV#[A-Za-z0-9]{3}"))
                    {
                        strings7.Add(product.GetValue("Name"));
                    }
                    else if (Regex.IsMatch(product.GetValue("Name"), @"[A-Za-z0-9]{3}[0-9]{2}AV"))
                    {
                        strings7.Add(product.GetValue("Name"));
                    }
                    else
                    {
                        strings13.Add(product.GetValue("Name"));
                    }
                }
                //else if (product.GetValue("Name").Count() == 10)
                //{
                //    strings10.Add(product.GetValue("Name"));
                //}
                //else if (product.GetValue("Name").Count() == 13)
                //{
                //    strings13.Add(product.GetValue("Name"));
                //}
                else if (product.GetValue("Name").Count() == 12)
                {
                    strings12.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 8)
                {
                    strings8.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 9)
                {
                    strings9.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 4)
                {
                    strings4.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 15)
                {
                    strings15.Add(product.GetValue("Name"));
                }
                else
                {
                    strings.Add(product.GetValue("Name").Count().ToString());
                    //Console.WriteLine(product.GetValue("Name").Count());
                    //Console.WriteLine(product.GetValue("Name"));
                }
            }
            return products;
        }

        public async Task<IEnumerable<CommonDataModel>> GetPartNumberDataAsync()
        {
            IEnumerable<CommonDataModel> products = await GetPropertyValueAsync(GetPartNumberCommandText());

            HashSet<string> strings11 = new HashSet<string>();
            HashSet<string> strings10 = new HashSet<string>();
            HashSet<string> strings15 = new HashSet<string>();
            HashSet<string> strings = new HashSet<string>();
            HashSet<string> strings13 = new HashSet<string>();
            HashSet<string> strings14 = new HashSet<string>();
            HashSet<string> strings16 = new HashSet<string>();
            HashSet<string> strings17 = new HashSet<string>();
            HashSet<string> strings18 = new HashSet<string>();

            foreach (CommonDataModel product in products)
            {
                if (product.GetValue("Name").Count() == 0)
                {
                    continue;
                }
                //else if (product.GetValue("Name").Count() != 11
                //    && product.GetValue("Name").Count() != 10)
                //{
                //    Console.WriteLine(product.GetValue("Name").Count());
                //    Console.WriteLine(product.GetValue("Name"));
                //}
                else if (product.GetValue("Name").Count() == 10)
                {
                    if (Regex.IsMatch(product.GetValue("Name"), @"[A-Za-z0-9]{1}[0-9]{2}[A-Za-z0-9]{3}\-[A-Za-z0-9]{3}"))
                    {
                        strings10.Add(product.GetValue("Name"));
                    }
                    else
                    {
                        strings.Add(product.GetValue("Name"));
                    }
                }
                else if (product.GetValue("Name").Count() == 11)
                {
                    strings11.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 15)
                {
                    strings15.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 13)
                {
                    strings13.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 14)
                {
                    strings14.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 16)
                {
                    strings16.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 17)
                {
                    strings17.Add(product.GetValue("Name"));
                }
                else if (product.GetValue("Name").Count() == 18)
                {
                    strings18.Add(product.GetValue("Name"));
                }
                //else if (Regex.IsMatch(product.GetValue("Name"), @"[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{3}[A-Za-z]{1}[A-Za-z0-9]{3}"))
                //{
                //    strings11.Add(product.GetValue("Name"));
                //}
                //else if (Regex.IsMatch(product.GetValue("Name"), @"[0-9]{2}[A-Za-z0-9]{2}[A-Za-z]{3}[A-Za-z0-9\#]{3}"))
                //{
                //    strings10.Add(product.GetValue("Name"));
                //}
                else
                {
                    Console.WriteLine(product.GetValue("Name").Count());
                    Console.WriteLine(product.GetValue("Name"));
                }
            }
            return products;
        }

        public async Task<IEnumerable<CommonDataModel>> GetMLNameDataAsync()
        {
            IEnumerable<CommonDataModel> products = await GetPropertyValueAsync(GetMLNameCommandText());

            HashSet<string> strings11 = new HashSet<string>();
            HashSet<string> strings10 = new HashSet<string>();

            foreach (CommonDataModel product in products)
            {
                if (product.GetValue("Name").Count() == 0)
                {
                    continue;
                }
                else if (product.GetValue("Name").Count() != 11
                    && product.GetValue("Name").Count() != 10)
                {
                    Console.WriteLine(product.GetValue("Name").Count());
                    Console.WriteLine(product.GetValue("Name"));
                }
                //else if (product.GetValue("Name").Count() == 10)
                //{
                //    strings10.Add(product.GetValue("Name"));
                //}
                else if (Regex.IsMatch(product.GetValue("Name"), @"[0-9]{2}[A-Za-z]{2}[A-Za-z0-9]{3}[A-Za-z]{1}[A-Za-z0-9]{3}"))
                {
                    strings11.Add(product.GetValue("Name"));
                }
                else if (Regex.IsMatch(product.GetValue("Name"), @"[0-9]{2}[A-Za-z0-9]{2}[A-Za-z]{3}[A-Za-z0-9\#]{3}"))
                {
                    strings10.Add(product.GetValue("Name"));
                }
                else
                {
                    Console.WriteLine(product.GetValue("Name").Count());
                    Console.WriteLine(product.GetValue("Name"));
                }
            }
            return products;
        }

        private async Task<IEnumerable<CommonDataModel>> GetPropertyValueAsync(string commandText)
        {
            using SqlConnection connection = new(_info.DatabaseConnectionString);
            await connection.OpenAsync();

            SqlCommand command = new(commandText, connection);
            using SqlDataReader reader = command.ExecuteReader();

            List<CommonDataModel> output = new();
            while (reader.Read())
            {
                CommonDataModel product = new();
                int fieldCount = reader.FieldCount;

                for (int i = 0; i < fieldCount; i++)
                {
                    if (await reader.IsDBNullAsync(i))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(reader[i].ToString())
                        && !string.IsNullOrEmpty(reader[i].ToString()))
                    {
                        string columnName = reader.GetName(i);
                        string value = reader[i].ToString().Trim();
                        product.Add(columnName, value);
                    }
                }
                output.Add(product);
            }

            return output;
        }

        private string GetMLNameCommandText()
        {
            return @"
SELECT ml.Name
FROM ML_INI ml
";
        }

        private string GetPartNumberCommandText()
        {
            return @"
select Dv.IRSPartNumber as Name
FROM DeliverableVersion Dv
";
        }

        private string GetAVdetailCommandText()
        {
            return @"
SELECT DISTINCT p.ID AS ProductId,
    A.AvNo as Name
FROM productversion p
LEFT JOIN Product_Brand PB ON PB.productVersionID = p.ID
LEFT JOIN AvDetail_ProductBrand APB ON APB.productBrandID = PB.Id
LEFT JOIN AvDetail A ON A.AvDetailID = APB.AvDetailID
WHERE APB.STATUS = 'A'
order by p.ID
";
        }
    }
}
