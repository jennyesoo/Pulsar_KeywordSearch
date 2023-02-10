using HP.Pulsar.KeywordSearch.CommonDataStructures;
using HP.Pulsar.KeywordSearch.DataTransformation;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;

namespace HP.Pulsar.KeywordSearch.DataReader
{
    public class ProductReader
    {
        public SqlDataReader? SqlDataResult;
        public SqlConnection? SqlDataConnect;
        public PulsarEnvironment? SqlDataEnv;
        DataProcessing DataTransform = new DataProcessing();

        public ProductReader(PulsarEnvironment env, string Command)
        {
            ConnectionStringProvider connect = new ConnectionStringProvider(env,Command);
            SqlDataResult = connect.Result;
            SqlDataConnect = connect.SqlDataConnect;
            SqlDataEnv = env;
        }

        // This function is to get all products
        public List<ProductDataModel> GetProducts()
        {
            List<ProductDataModel> AllProductData = new List<ProductDataModel>();
            PropertyProcessing propertyprocessing = new PropertyProcessing();   
            /*
            if (SqlDataResult.Read())
            {
                for (int i = 0; i < SqlDataResult.FieldCount; i++)
                {
                    columns.Add(SqlDataResult.GetName(i));
                }
            }
            */
            int Count = 0;  
            while (SqlDataResult.Read())
            {
                ProductDataModel productDataModel = new ProductDataModel();
                productDataModel.ProductId = Convert.ToInt16(SqlDataResult["ProductId"]);
                productDataModel.ProductName = SqlDataResult["ProductName"].ToString();
                productDataModel.Partner = SqlDataResult["Partner"].ToString();
                productDataModel.DevCenter = SqlDataResult["DevCenter"].ToString();
                productDataModel.Brands = SqlDataResult["Brands"].ToString();
                productDataModel.SystemBoardId = SqlDataResult["SystemBoardId"].ToString();
                productDataModel.ServiceLifeDate = SqlDataResult["ServiceLifeDate"].ToString();
                productDataModel.ProductStatus = SqlDataResult["ProductStatus"].ToString();
                productDataModel.BusinessSegment = SqlDataResult["BusinessSegment"].ToString();
                productDataModel.CreatorName = SqlDataResult["CreatorName"].ToString();
                productDataModel.CreatedDate = SqlDataResult["CreatedDate"].ToString();
                productDataModel.LastUpdaterName = SqlDataResult["LastUpdaterName"].ToString();
                productDataModel.LatestUpdateDate = SqlDataResult["LatestUpdateDate"].ToString();
                productDataModel.SystemManager = SqlDataResult["SystemManager"].ToString();
                productDataModel.PlatformDevelopmentPM = SqlDataResult["PlatformDevelopmentPM"].ToString();
                productDataModel.PlatformDevelopmentPMEmail = SqlDataResult["PlatformDevelopmentPMEmail"].ToString();
                productDataModel.SupplyChain = SqlDataResult["SupplyChain"].ToString();
                productDataModel.SupplyChainEmail = SqlDataResult["SupplyChainEmail"].ToString();
                productDataModel.ODMSystemEngineeringPM = SqlDataResult["ODMSystemEngineeringPM"].ToString();
                productDataModel.ODMSystemEngineeringPMEmail = SqlDataResult["ODMSystemEngineeringPMEmail"].ToString();
                productDataModel.ConfigurationManager = SqlDataResult["ConfigurationManager"].ToString();
                productDataModel.ConfigurationManagerEmail = SqlDataResult["ConfigurationManagerEmail"].ToString();
                productDataModel.CommodityPM = SqlDataResult["CommodityPM"].ToString();
                productDataModel.CommodityPMEmail = SqlDataResult["CommodityPMEmail"].ToString();
                productDataModel.Service = SqlDataResult["Service"].ToString();
                productDataModel.ServiceEmail = SqlDataResult["ServiceEmail"].ToString();
                productDataModel.ODMHWPM = SqlDataResult["ODMHWPM"].ToString();
                productDataModel.ODMHWPMEmail = SqlDataResult["ODMHWPMEmail"].ToString();
                productDataModel.ProgramOfficeProgramManager = SqlDataResult["ProgramOfficeProgramManager"].ToString();
                productDataModel.ProgramOfficeProgramManagerEmail = SqlDataResult["ProgramOfficeProgramManagerEmail"].ToString();
                productDataModel.Quality = SqlDataResult["Quality"].ToString();
                productDataModel.QualityEmail = SqlDataResult["QualityEmail"].ToString();
                productDataModel.PlanningPM = SqlDataResult["PlanningPM"].ToString();
                productDataModel.PlanningPMEmail = SqlDataResult["PlanningPMEmail"].ToString();
                productDataModel.BIOSPM = SqlDataResult["BIOSPM"].ToString();
                productDataModel.BIOSPMEmail = SqlDataResult["BIOSPMEmail"].ToString();
                productDataModel.SystemsEngineeringPM = SqlDataResult["SystemsEngineeringPM"].ToString();
                productDataModel.SystemsEngineeringPMEmail = SqlDataResult["SystemsEngineeringPMEmail"].ToString();
                productDataModel.MarketingProductMgmt = SqlDataResult["MarketingProductMgmt"].ToString();
                productDataModel.MarketingProductMgmtEmail = SqlDataResult["MarketingProductMgmtEmail"].ToString();
                productDataModel.ProcurementPM = SqlDataResult["ProcurementPM"].ToString();
                productDataModel.ProcurementPMEmail = SqlDataResult["ProcurementPMEmail"].ToString();
                productDataModel.SWMarketing = SqlDataResult["SWMarketing"].ToString();
                productDataModel.SWMarketingEmail = SqlDataResult["SWMarketingEmail"].ToString();
                productDataModel.ProductFamily = SqlDataResult["ProductFamily"].ToString();
                productDataModel.ODM = SqlDataResult["ODM"].ToString();
                productDataModel.ReleaseTeam = SqlDataResult["ReleaseTeam"].ToString();
                productDataModel.RegulatoryModel = SqlDataResult["RegulatoryModel"].ToString();
                productDataModel.Releases = SqlDataResult["Releases"].ToString();
                productDataModel.Description = SqlDataResult["Description"].ToString();
                productDataModel.ProductLine = SqlDataResult["ProductLine"].ToString();
                productDataModel.PreinstallTeam = SqlDataResult["PreinstallTeam"].ToString();
                productDataModel.MachinePNPID = SqlDataResult["MachinePNPID"].ToString();
                productDataModel.EndOfProduction = propertyprocessing.EndOfProduction((PulsarEnvironment)SqlDataEnv, SqlDataResult["ProductId"].ToString());
                productDataModel.WHQLstatus = propertyprocessing.WHQLstatus((PulsarEnvironment)SqlDataEnv, SqlDataResult["ProductId"].ToString());
                productDataModel.LeadProduct = propertyprocessing.Leadproduct((PulsarEnvironment)SqlDataEnv, SqlDataResult["BusinessSegmentID"].ToString());
                productDataModel.Chipsets = propertyprocessing.Chipsets((PulsarEnvironment)SqlDataEnv, SqlDataResult["BusinessSegmentID"].ToString());
                productDataModel.ComponentItems = "";
                productDataModel.ProductGroups = propertyprocessing.ProductGroups((PulsarEnvironment)SqlDataEnv, SqlDataResult["ProductId"].ToString());
                productDataModel.CurrentBIOSVersions = propertyprocessing.CurrentBIOSVersions((PulsarEnvironment)SqlDataEnv, SqlDataResult["ProductId"].ToString(), SqlDataResult["ProductStatus"].ToString());
                productDataModel.Target = "Product Version";
                productDataModel.ID = Count;
                Count++;

                Console.Write($"EndOfProduction: {productDataModel.ServiceLifeDate}\t\n" +
                                $"WHQLstatus: {productDataModel.CreatedDate}\t\n" +
                                $"LeadProduct: {productDataModel.LatestUpdateDate}\t\n" +
                                $"Chipsets: {productDataModel.EndOfProduction}\t\n" +
                                $"ProductGroups: {productDataModel.ProductGroups}\t\n" +
                                $"CurrentBIOSVersions: {productDataModel.CurrentBIOSVersions}\t\n" +
                                $"Target: {productDataModel.Target}\t\n" +
                                $"ID: {productDataModel.ID}\t\n"+
                                "-----------------------------------------------\t\n");

                AllProductData.Add(productDataModel);
            }
            SqlDataConnect.Close();

            return AllProductData; // if AllProductData == Null ??
        }

        // This function is to get a specific product
        public ProductDataModel GetProduct(int productId)
        {
            throw new NotImplementedException();
        }
    }
}
