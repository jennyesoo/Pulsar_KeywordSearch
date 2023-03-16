using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class ProductReader : IKeywordSearchDataReader
{
    private ConnectionStringProvider _csProvider;

    public ProductReader(KeywordSearchInfo info)
    {
        _csProvider = new(info.Environment);
    }

    public async Task<CommonDataModel> GetDataAsync(int ProductId)
    {
        throw new NotImplementedException();
    }


    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> products = await GetProductsAsync();

        // TODO - performance improvement needed
        List<Task> tasks = new()
        {
            GetEndOfProductionDateAsync(products),
            GetProductGroupsAsync(products),
//            GetWhqlstatusAsync(products),
            GetLeadProductAsync(products),
            GetChipsetsAsync(products),
            GetCurrentBiosVersionsAsync(products),
            GetComponentRootListAsync(products)
        };

        await Task.WhenAll(tasks);

        return products;
    }

    private string GetAllProductsSqlCommandText()
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
    p.TypeId
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
";
    }

    // This function is to get all products
    private async Task<IEnumerable<CommonDataModel>> GetProductsAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        SqlCommand command = new(GetAllProductsSqlCommandText(), connection);

        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();

        while (reader.Read())
        {
            //string businessSegmentId;
            CommonDataModel product = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString();

                product.Add(columnName, value);
            }

            product.Add("target", "product");
            output.Add(product);
        }

        return output;
    }

    private string GetEndOfProductionCommand1Text()
    {
        // This is for product type = 3
        return @"
SELECT pv.Id AS ProductId,
    MAX(amo.EmDate) AS EndOfProductionDate
FROM ProductVersion pv WITH (NOLOCK)
JOIN PRL_Products pp WITH (NOLOCK) ON pv.Id = pp.ProductVersionId
JOIN PRL_Delivery_Feature pdf WITH (NOLOCK) ON pp.prlRevisionId = pdf.prlRevisionId
JOIN PRL_Chassis_Delivery pcd WITH (NOLOCK) ON pdf.DeliveryId = pcd.DeliveryId
JOIN AmoHpPartNo amo WITH (NOLOCK) ON pdf.FeatureId = amo.FeatureId
WHERE (
        pdf.StatusId = 53 --New  
        OR pdf.StatusId = 54 -- Prev  
        )
    AND pcd.DeliveryTypeId = 2 -- AMO 
    AND amo.STATUS = 1 -- active
GROUP BY pv.Id
";
    }

    private string GetEndOfProductionCommand2Text()
    {
        // This is for product type != 3
        return @"
SELECT pb.ProductVersionId AS ProductId,
    MAX(av.RASDiscontinueDt) AS EndOfProductionDate
FROM Product_Brand pb WITH (NOLOCK)
JOIN AvDetail_ProductBrand avb WITH (NOLOCK) ON pb.Id = avb.ProductBrandId
JOIN AvDetail av WITH (NOLOCK) ON av.AvDetailId = avb.AvDetailId
WHERE (
        (
            av.FeatureCategoryId IN (
                1,
                86
                )
            ) -- Base Units for Legacy product 
        OR (av.SCMCategoryID = 6) -- Base Units for Pulsar product
        )
    AND avb.[Status] = 'A'
    AND av.RASDiscontinueDt IS NOT NULL
GROUP BY pb.ProductVersionId
";
    }

    private string GetProductGroupsCommandText() => "select t1.ProductVersionId, t2.FullName from ProductVersion_ProductGroup t1 join PROGRAM t2 on t1.ProductGroupId = t2.id";

    private string GetTSQLLeadproductCommandText()
    {
        return @"
SELECT distinct lp.ID as ProductId,
     lp.DotsName + ' (' + lpr.Name + ')' as LeadProduct
FROM ProductVersionRelease pr WITH (NOLOCK)
JOIN ProductVersion_Release pv WITH (NOLOCK) ON pr.id = pv.ReleaseID
JOIN ProductVersion_Release lpv WITH (NOLOCK) ON lpv.id = pv.LeadProductreleaseID
JOIN ProductVersion lp WITH (NOLOCK) ON lp.id = lpv.ProductVersionID
JOIN ProductVersionRelease lpr WITH (NOLOCK) ON lpr.id = lpv.ReleaseID
";
    }

    private string GetChipsetsCommandText()
    {
        return @"
SELECT p_c.ProductVersionID AS ProductId,
    c.CodeName
FROM Chipset c WITH (NOLOCK)
JOIN Product_Chipset p_c WITH (NOLOCK) ON c.[ID] = p_c.[ChipsetId]
WHERE p_c.ChipsetId IS NOT NULL
";
    }

    private string GetCurrentBIOSVersionsCommandText()
    {
        return @"Select currentROM,currentWebROM From ProductVersion where ID =  @ProductId";
    }

    private string GetCurrentROMCommandText()
    {
        return @"exec spListTargetedbiosversions @ProductId";
    }

    private string GetComponentRootListCommandText()
    {
        return @"select PV.id as ProductId,
                        stuff((select ' , ' + (CONVERT(Varchar, root.Id) + ' ' +  root.Name)
                        FROM ProductVersion p
                        JOIN ProductStatus ps ON ps.id = p.ProductStatusID
                        JOIN Product_DelRoot pr on pr.ProductVersionId = p.id
                        JOIN DeliverableRoot root ON root.Id = pr.DeliverableRootId
                        WHERE p.id=PV.id And ps.Name <> 'Inactive' and p.FusionRequirements = 1 order by p.Id
                        for xml path('')),1,3,'') As ComponentRoot
                FROM ProductVersion PV
                where PV.id = @ProductId
                group by PV.id";
    }

    private async Task<IEnumerable<CommonDataModel>> GetComponentRootListAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> EndOfProduction = new List<string>();
            SqlCommand command = new(GetComponentRootListCommandText(),
                                        connection);
            SqlParameter parameter = new SqlParameter("ProductId",
                                                        product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (await reader.ReadAsync())
            {
                product.Add("ComponentRootList", reader["ComponentRoot"].ToString());
            }
        }
        return products;
    }

    private async Task GetEndOfProductionDateAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();
        SqlCommand command1 = new(GetEndOfProductionCommand1Text(), connection);
        SqlCommand command2 = new(GetEndOfProductionCommand2Text(), connection);

        Dictionary<int, DateTime> eopDates1 = new();
        Dictionary<int, DateTime> eopDates2 = new();

        using (SqlDataReader reader1 = command1.ExecuteReader())
        {
            while (await reader1.ReadAsync())
            {
                if (int.TryParse(reader1["ProductId"].ToString(), out int productId)
                    && DateTime.TryParse(reader1["EndOfProductionDate"].ToString(), out DateTime date1))
                {
                    eopDates1[productId] = date1;
                }
            }
        }

        using (SqlDataReader reader2 = command2.ExecuteReader())
        {
            while (await reader2.ReadAsync())
            {
                if (int.TryParse(reader2["ProductId"].ToString(), out int productId)
                    && DateTime.TryParse(reader2["EndOfProductionDate"].ToString(), out DateTime date2))
                {
                    eopDates2[productId] = date2;
                }
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (!int.TryParse(product.GetValue("TypeId"), out int typeId)
                || !int.TryParse(product.GetValue("ProductId"), out int productId))
            {
                continue;
            }

            if (typeId == 3)
            {
                if (eopDates1.ContainsKey(productId))
                {
                    product.Add("EndOfProduction", eopDates1[productId].ToString("yyyy/MM/dd"));
                }
            }
            else
            {
                if (eopDates2.ContainsKey(productId))
                {
                    product.Add("EndOfProduction", eopDates2[productId].ToString("yyyy/MM/dd"));
                }
            }
        }
    }

    private async Task GetProductGroupsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        List<string> ProductGroups = new();
        SqlCommand command = new(GetProductGroupsCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> productGroups = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductVersionId"].ToString(), out int productId))
            {
                if (productGroups.ContainsKey(productId))
                {
                    productGroups[productId].Add(reader["FullName"].ToString());
                }
                else
                {
                    productGroups[productId] = new List<string> { reader["FullName"].ToString() };
                }
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Id"), out int productId)
                && productGroups.ContainsKey(productId))
            {
                product.Add("ProductGroups", string.Join(" ", productGroups[productId]));
            }
        }
    }

    private async Task<IEnumerable<CommonDataModel>> GetWhqlstatusAsync(IEnumerable<CommonDataModel> products)
    {
        // ProductWHQL is empty, which means PRS doesn't have product whql data. So let's ignore WHQL data.
    }

    private async Task GetLeadProductAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        Dictionary<int, List<string>> leadProducts = new();
        SqlCommand command = new(GetTSQLLeadproductCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                if (leadProducts.ContainsKey(productId))
                {
                    leadProducts[productId].Add(reader["LeadProduct"].ToString());
                }
                else
                {
                    leadProducts[productId] = new List<string>()
                    {
                        reader["LeadProduct"].ToString()
                    };
                }
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Id"), out int productId)
               && leadProducts.ContainsKey(productId))
            {
                product.Add("LeadProduct", string.Join(", ", leadProducts[productId]));
            }
        }
    }

    private async Task GetChipsetsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();
        SqlCommand command = new(GetChipsetsCommandText(), connection);

        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> chipsets = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (chipsets.ContainsKey(productId))
            {
                chipsets[productId].Add(reader["CodeName"].ToString());
            }
            else
            {
                chipsets[productId] = new List<string>() { reader["CodeName"].ToString() };
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Id"), out int productId)
              && chipsets.ContainsKey(productId))
            {
                product.Add("Chipsets", string.Join(" ", chipsets[productId]));
            }
        }
    }

    private async Task<IEnumerable<CommonDataModel>> GetCurrentBiosVersionsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> CurrentBIOSVersions = new List<string>();
            SqlCommand command = new(GetCurrentBIOSVersionsCommandText(), connection);
            SqlParameter parameter = new SqlParameter("ProductId", product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string CurrentROM_value = reader["currentROM"].ToString();
                string CurrentWebROM_value = reader["currentWebROM"].ToString();
                string[] value_List = { "Development", "Definition" };

                if (CurrentROM_value == "" && (product.GetValue("ProductStatus") == "Development" || product.GetValue("ProductStatus") == "Definition"))
                {
                    using SqlConnection connection_CurrentROM = new(_csProvider.GetSqlServerConnectionString());
                    SqlCommand command_CurrentROM = new(GetCurrentROMCommandText(), connection_CurrentROM);
                    await connection_CurrentROM.OpenAsync();
                    SqlParameter parameter_CurrentROM = new SqlParameter("ProductId", product.GetValue("ProductId"));
                    command_CurrentROM.Parameters.Add(parameter_CurrentROM);
                    using SqlDataReader reader_CurrentROM = command_CurrentROM.ExecuteReader();
                    while (reader_CurrentROM.Read())
                    {
                        CurrentROM_value = "Targeted: " + reader_CurrentROM["TargetedVersions"];
                    }
                }
                else if (!value_List.Contains(product.GetValue("ProductStatus")))
                {
                    if (CurrentROM_value == "")
                    {
                        CurrentROM_value = "Factory: ";
                    }
                    else
                    {
                        CurrentROM_value = "Factory: UnKnown";
                    }
                }
                if (CurrentROM_value != "" && CurrentWebROM_value != "")
                {
                    CurrentROM_value += "Web: " + CurrentWebROM_value;
                }
                else if (CurrentROM_value == "" && CurrentWebROM_value != "")
                {
                    CurrentROM_value = "Web: " + CurrentWebROM_value;
                }
                product.Add("CurrentBIOSVersions", CurrentROM_value);
            }
        }

        return products;
    }
}
