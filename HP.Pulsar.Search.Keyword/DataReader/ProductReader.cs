using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.DataTransformation;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class ProductReader
{
    public SqlDataReader? reader;
    public SqlConnection? SqlDataConnect;
    public PulsarEnvironment? SqlDataEnv;
    private ConnectionStringProvider _csProvider;

    DataProcessing DataTransform = new DataProcessing();

    public ProductReader(PulsarEnvironment env)
    {
        _csProvider = new(env);

        //SqlDataResult = connect.Result;
        //SqlDataConnect = connect.SqlDataConnect;
        //SqlDataEnv = env;
    }

    private string GetAllProductsTSqlCommandText()
    {
        return @"
SELECT p.id AS ProductId,
    DOTSName AS ProductName,
    partner.name AS Partner,
    pdc.Name AS DevCenter,
    Brands,
    p.SystemBoardId,
    ServiceLifeDate,
    ps.Name AS ProductStatus,
    sg.Name AS BusinessSegment,
    p.CreatedBy AS CreatorName,
    p.Created AS CreatedDate,
    p.UpdatedBy AS LastUpdaterName,
    p.Updated AS LatestUpdateDate,
    user_SMID.FirstName + ' ' + user_SMID.LastName AS SystemManager,
    user_SMID.Email AS SystemManagerEmail,
    user_PDPM.FirstName + ' ' + user_PDPM.LastName AS PlatformDevelopmentPM,
    user_PDPM.Email AS PlatformDevelopmentPMEmail,
    user_SCID.FirstName + ' ' + user_SCID.LastName AS SupplyChain,
    user_SCID.Email AS SupplyChainEmail,
    user_ODMSEPM.FirstName + ' ' + user_ODMSEPM.LastName AS ODMSystemEngineeringPM,
    user_ODMSEPM.Email AS ODMSystemEngineeringPMEmail,
    user_CM.FirstName + ' ' + user_CM.LastName AS ConfigurationManager,
    user_CM.Email AS ConfigurationManagerEmail,
    user_CPM.FirstName + ' ' + user_CPM.LastName AS CommodityPM,
    user_CPM.Email AS CommodityPMEmail,
    user_Service.FirstName + ' ' + user_Service.LastName AS Service,
    user_Service.Email AS ServiceEmail,
    user_ODMHWPM.FirstName + ' ' + user_ODMHWPM.LastName AS ODMHWPM,
    user_ODMHWPM.Email AS ODMHWPMEmail,
    user_POPM.FirstName + ' ' + user_POPM.LastName AS ProgramOfficeProgramManager,
    user_POPM.Email AS ProgramOfficeProgramManagerEmail,
    user_Quality.FirstName + ' ' + user_Quality.LastName AS Quality,
    user_Quality.Email AS QualityEmail,
    user_PPM.FirstName + ' ' + user_PPM.LastName AS PlanningPM,
    user_PPM.Email AS PlanningPMEmail,
    user_BIOSPM.FirstName + ' ' + user_BIOSPM.LastName AS BIOSPM,
    user_BIOSPM.Email AS BIOSPMEmail,
    user_SEPM.FirstName + ' ' + user_SEPM.LastName AS SystemsEngineeringPM,
    user_SEPM.Email AS SystemsEngineeringPMEmail,
    user_MPM.FirstName + ' ' + user_MPM.LastName AS MarketingProductMgmt,
    user_MPM.Email AS MarketingProductMgmtEmail,
    user_ProPM.FirstName + ' ' + user_ProPM.LastName AS ProcurementPM,
    user_ProPM.Email AS ProcurementPMEmail,
    user_SWM.FirstName + ' ' + user_SWM.LastName AS SWMarketing,
    user_SWM.Email AS SWMarketingEmail,
    pf.Name AS ProductFamily,
    partner.name AS ODM,
    pis.Name AS ReleaseTeam,
    p.RegulatoryModel AS RegulatoryModel,
    STUFF((
            SELECT ',' + new_releases.Releases
            FROM (
                SELECT ProductVersion.id AS ProductId,
                    pvr.Name AS Releases
                FROM ProductVersion
                FULL JOIN ProductVersion_Release pv_r ON pv_r.ProductVersionID = ProductVersion.ID
                FULL JOIN ProductVersionRelease pvr ON pvr.ID = pv_r.ReleaseID
                FULL JOIN ProductStatus ps ON ps.id = ProductVersion.ProductStatusID
                WHERE ps.Name <> 'Inactive'
                    AND p.FusionRequirements = 1
                    AND ProductVersion.ID = p.id
                ) AS new_releases
            FOR XML PATH('')
            ), 1, 1, '') AS Releases,
    p.Description,
    pl.Name + '-' + pl.Description AS ProductLine,
    CASE 
        WHEN p.PreinstallTeam = - 1
            THEN ''
        WHEN p.PreinstallTeam = 1
            THEN 'Houston'
        WHEN p.PreinstallTeam = 2
            THEN 'Taiwan'
        WHEN p.PreinstallTeam = 3
            THEN 'Singapore'
        WHEN p.PreinstallTeam = 4
            THEN 'Brazil'
        WHEN p.PreinstallTeam = 5
            THEN 'CDC'
        WHEN p.PreinstallTeam = 6
            THEN 'Houston – Thin Client'
        WHEN p.PreinstallTeam = 7
            THEN 'Mobility'
        WHEN p.PreinstallTeam = 8
            THEN ''
        END AS PreinstallTeam,
    p.MachinePNPID AS MachinePNPID,
    p.BusinessSegmentID
FROM ProductVersion p
FULL JOIN ProductFamily pf ON p.ProductFamilyId = pf.id
FULL JOIN Partner partner ON partner.id = p.PartnerId
FULL JOIN ProductDevCenter pdc ON pdc.ProductDevCenterId = DevCenter
FULL JOIN ProductStatus ps ON ps.id = p.ProductStatusID
FULL JOIN BusinessSegment sg ON sg.BusinessSegmentID = p.BusinessSegmentID
FULL JOIN PreinstallTeam pis ON pis.ID = p.ReleaseTeam
FULL JOIN UserInfo user_SMID ON user_SMID.userid = p.SMID
FULL JOIN UserInfo user_PDPM ON user_PDPM.userid = p.PlatformDevelopmentID
FULL JOIN UserInfo user_SCID ON user_SCID.userid = p.SupplyChainID
FULL JOIN UserInfo user_ODMSEPM ON user_ODMSEPM.userid = p.ODMSEPMID
FULL JOIN UserInfo user_CM ON user_CM.userid = p.PMID
FULL JOIN UserInfo user_CPM ON user_CPM.userid = p.PDEID
FULL JOIN UserInfo user_Service ON user_Service.userid = p.ServiceID
FULL JOIN UserInfo user_ODMHWPM ON user_ODMHWPM.userid = p.ODMHWPMID
FULL JOIN UserInfo user_POPM ON user_POPM.userid = p.TDCCMID
FULL JOIN UserInfo user_Quality ON user_Quality.userid = p.QualityID
FULL JOIN UserInfo user_PPM ON user_PPM.userid = p.PlanningPMID
FULL JOIN UserInfo user_BIOSPM ON user_BIOSPM.userid = p.BIOSLeadID
FULL JOIN UserInfo user_SEPM ON user_SEPM.userid = p.SEPMID
FULL JOIN UserInfo user_MPM ON user_MPM.userid = p.ConsMarketingID
FULL JOIN UserInfo user_ProPM ON user_ProPM.userid = p.ProcurementPMID
FULL JOIN UserInfo user_SWM ON user_SWM.userid = p.SwMarketingId
FULL JOIN ProductLine pl ON pl.Id = p.ProductLineId
WHERE ps.Name <> 'Inactive'
    AND p.FusionRequirements = 1
    AND (@ProductId = -1 OR p.ProductVersionId = @ProductId)
";
    }

    // This function is to get all products
    public async Task<IEnumerable<ProductDataModel>> GetProductsAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        SqlCommand command = new(GetAllProductsTSqlCommandText(), connection);

        SqlParameter parameter = new SqlParameter("ProductId", -1);
        command.Parameters.Add(parameter);

        await connection.OpenAsync();

        using SqlDataReader reader = command.ExecuteReader();

        List<ProductDataModel> products = new List<ProductDataModel>();

        // This is for ID in Meilisearch
        int count = 0;

        while (reader.Read())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                // log 

                continue;
            }

            ProductDataModel productDataModel = new ProductDataModel
            {
                ProductId = productId,
                ProductName = reader["ProductName"].ToString(),
                Partner = reader["Partner"].ToString(),
                DevCenter = reader["DevCenter"].ToString(),
                Brands = reader["Brands"].ToString(),
                SystemBoardId = reader["SystemBoardId"].ToString(),
                ServiceLifeDate = reader["ServiceLifeDate"].ToString(),
                ProductStatus = reader["ProductStatus"].ToString(),
                BusinessSegment = reader["BusinessSegment"].ToString(),
                CreatorName = reader["CreatorName"].ToString(),
                CreatedDate = reader["CreatedDate"].ToString(),
                LastUpdaterName = reader["LastUpdaterName"].ToString(),
                LatestUpdateDate = reader["LatestUpdateDate"].ToString(),
                SystemManager = reader["SystemManager"].ToString(),
                PlatformDevelopmentPM = reader["PlatformDevelopmentPM"].ToString(),
                PlatformDevelopmentPMEmail = reader["PlatformDevelopmentPMEmail"].ToString(),
                SupplyChain = reader["SupplyChain"].ToString(),
                SupplyChainEmail = reader["SupplyChainEmail"].ToString(),
                ODMSystemEngineeringPM = reader["ODMSystemEngineeringPM"].ToString(),
                ODMSystemEngineeringPMEmail = reader["ODMSystemEngineeringPMEmail"].ToString(),
                ConfigurationManager = reader["ConfigurationManager"].ToString(),
                ConfigurationManagerEmail = reader["ConfigurationManagerEmail"].ToString(),
                CommodityPM = reader["CommodityPM"].ToString(),
                CommodityPMEmail = reader["CommodityPMEmail"].ToString(),
                Service = reader["Service"].ToString(),
                ServiceEmail = reader["ServiceEmail"].ToString(),
                ODMHWPM = reader["ODMHWPM"].ToString(),
                ODMHWPMEmail = reader["ODMHWPMEmail"].ToString(),
                ProgramOfficeProgramManager = reader["ProgramOfficeProgramManager"].ToString(),
                ProgramOfficeProgramManagerEmail = reader["ProgramOfficeProgramManagerEmail"].ToString(),
                Quality = reader["Quality"].ToString(),
                QualityEmail = reader["QualityEmail"].ToString(),
                PlanningPM = reader["PlanningPM"].ToString(),
                PlanningPMEmail = reader["PlanningPMEmail"].ToString(),
                BIOSPM = reader["BIOSPM"].ToString(),
                BIOSPMEmail = reader["BIOSPMEmail"].ToString(),
                SystemsEngineeringPM = reader["SystemsEngineeringPM"].ToString(),
                SystemsEngineeringPMEmail = reader["SystemsEngineeringPMEmail"].ToString(),
                MarketingProductMgmt = reader["MarketingProductMgmt"].ToString(),
                MarketingProductMgmtEmail = reader["MarketingProductMgmtEmail"].ToString(),
                ProcurementPM = reader["ProcurementPM"].ToString(),
                ProcurementPMEmail = reader["ProcurementPMEmail"].ToString(),
                SWMarketing = reader["SWMarketing"].ToString(),
                SWMarketingEmail = reader["SWMarketingEmail"].ToString(),
                ProductFamily = reader["ProductFamily"].ToString(),
                ODM = reader["ODM"].ToString(),
                ReleaseTeam = reader["ReleaseTeam"].ToString(),
                RegulatoryModel = reader["RegulatoryModel"].ToString(),
                Releases = reader["Releases"].ToString(),
                Description = reader["Description"].ToString(),
                ProductLine = reader["ProductLine"].ToString(),
                PreinstallTeam = reader["PreinstallTeam"].ToString(),
                MachinePNPID = reader["MachinePNPID"].ToString(),
                //EndOfProduction = propertyprocessing.EndOfProduction((PulsarEnvironment)SqlDataEnv, reader["ProductId"].ToString()),
                //WHQLstatus = propertyprocessing.WHQLstatus((PulsarEnvironment)SqlDataEnv, reader["ProductId"].ToString()),
                //LeadProduct = propertyprocessing.Leadproduct((PulsarEnvironment)SqlDataEnv, reader["BusinessSegmentID"].ToString()),
                //Chipsets = propertyprocessing.Chipsets((PulsarEnvironment)SqlDataEnv, reader["BusinessSegmentID"].ToString()),
                ComponentItems = "",
                //ProductGroups = propertyprocessing.ProductGroups((PulsarEnvironment)SqlDataEnv, reader["ProductId"].ToString()),
                //CurrentBIOSVersions = propertyprocessing.CurrentBIOSVersions((PulsarEnvironment)SqlDataEnv, reader["ProductId"].ToString(), reader["ProductStatus"].ToString()),
                Target = "Product Version",
                ID = count
            };
            count++;

            Console.Write($"EndOfProduction: {productDataModel.ServiceLifeDate}\t\n" +
                            $"WHQLstatus: {productDataModel.CreatedDate}\t\n" +
                            $"LeadProduct: {productDataModel.LatestUpdateDate}\t\n" +
                            $"Chipsets: {productDataModel.EndOfProduction}\t\n" +
                            $"ProductGroups: {productDataModel.ProductGroups}\t\n" +
                            $"CurrentBIOSVersions: {productDataModel.CurrentBIOSVersions}\t\n" +
                            $"Target: {productDataModel.Target}\t\n" +
                            $"ID: {productDataModel.ID}\t\n" +
                            "-----------------------------------------------\t\n");

            products.Add(productDataModel);
        }

        products = await GetEndOfProductionAsync(products);



        return products; // if AllProductData == Null ??
    }

    private string GetTSQLEndOfProductionCommandText()
    {
        return @"

";
    }

    private async Task<List<ProductDataModel>> GetEndOfProductionAsync(List<ProductDataModel> products)
    {

        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        SqlCommand command = new(GetAllProductsTSqlCommandText(), connection);

        await connection.OpenAsync();

        foreach (ProductDataModel product in products)
        {

        }

        return products;
    }

    private List<ProductDataModel> GetWHQLstatus(List<ProductDataModel> products)
    { }

    private List<ProductDataModel> GetLeadProduct(List<ProductDataModel> products)
    { }

    private List<ProductDataModel> GetChipsets(List<ProductDataModel> products)
    { }

    private List<ProductDataModel> GetProductGroups(List<ProductDataModel> products)
    { }

    private List<ProductDataModel> GetCurrentBIOSVersions(List<ProductDataModel> products)
    { }

    // This function is to get a specific product
    public ProductDataModel GetProduct(int productId)
    {
        throw new NotImplementedException();
    }
}
