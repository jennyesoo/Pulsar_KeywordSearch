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

    public async Task<CommonDataModel> GetDataAsync(int productId)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> products = await GetProductsAsync();

        List<Task> tasks = new()
        {
            GetEndOfProductionDateAsync(products),
            GetProductGroupsAsync(products),
            GetLeadProductAsync(products),
            GetChipsetsAsync(products),
            GetCurrentBIOSVersionsAsync(products),
            GetAvDetailAsync(products)
            GetFactoryNameAsync(products)
//            GetComponentRootListAsync(products)
        };

        await Task.WhenAll(tasks);

        return products;
    }

    private string GetProductsCommandText()
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
            THEN 'Houston - Thin Client'
        WHEN p.PreinstallTeam = 7
            THEN 'Mobility'
        WHEN p.PreinstallTeam = 8
            THEN ''
        END AS PreinstallTeam,
    p.MachinePNPID AS MachinePNPID,
    p.RCTOSites,
    p.IsNdaProduct
FROM ProductVersion p
left JOIN ProductFamily pf ON p.ProductFamilyId = pf.id
left JOIN Partner partner ON partner.id = p.PartnerId
left JOIN ProductDevCenter pdc ON pdc.ProductDevCenterId = DevCenter
left JOIN ProductStatus ps ON ps.id = p.ProductStatusID
left JOIN BusinessSegment sg ON sg.BusinessSegmentID = p.BusinessSegmentID
left JOIN PreinstallTeam pis ON pis.ID = p.ReleaseTeam
left JOIN UserInfo user_SMID ON user_SMID.userid = p.SMID
left JOIN UserInfo user_PDPM ON user_PDPM.userid = p.PlatformDevelopmentID
left JOIN UserInfo user_SCID ON user_SCID.userid = p.SupplyChainID
left JOIN UserInfo user_ODMSEPM ON user_ODMSEPM.userid = p.ODMSEPMID
left JOIN UserInfo user_CM ON user_CM.userid = p.PMID
left JOIN UserInfo user_CPM ON user_CPM.userid = p.PDEID
left JOIN UserInfo user_Service ON user_Service.userid = p.ServiceID
left JOIN UserInfo user_ODMHWPM ON user_ODMHWPM.userid = p.ODMHWPMID
left JOIN UserInfo user_POPM ON user_POPM.userid = p.TDCCMID
left JOIN UserInfo user_Quality ON user_Quality.userid = p.QualityID
left JOIN UserInfo user_PPM ON user_PPM.userid = p.PlanningPMID
left JOIN UserInfo user_BIOSPM ON user_BIOSPM.userid = p.BIOSLeadID
left JOIN UserInfo user_SEPM ON user_SEPM.userid = p.SEPMID
left JOIN UserInfo user_MPM ON user_MPM.userid = p.ConsMarketingID
left JOIN UserInfo user_ProPM ON user_ProPM.userid = p.ProcurementPMID
left JOIN UserInfo user_SWM ON user_SWM.userid = p.SwMarketingId
left JOIN ProductLine pl ON pl.Id = p.ProductLineId
WHERE   (
        @ProductId = - 1
        OR p.Id = @ProductId
        )
";
    }

    // This function is to get all products
    private async Task<IEnumerable<CommonDataModel>> GetProductsAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        SqlCommand command = new(GetProductsCommandText(), connection);
        SqlParameter parameter = new SqlParameter("ProductId", "-1");
        command.Parameters.Add(parameter);
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
                if (!string.IsNullOrWhiteSpace(reader[i].ToString()))
                {
                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString().Trim();
                    product.Add(columnName, value);
                }
            }
            product.Add("target", "product");
            product.Add("Id", SearchIdName.Product + product.GetValue("ProductId"));
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
LEFT JOIN PRL_Products pp WITH (NOLOCK) ON pv.Id = pp.ProductVersionId
LEFT JOIN PRL_Delivery_Feature pdf WITH (NOLOCK) ON pp.prlRevisionId = pdf.prlRevisionId
LEFT JOIN PRL_Chassis_Delivery pcd WITH (NOLOCK) ON pdf.DeliveryId = pcd.DeliveryId
LEFT JOIN AmoHpPartNo amo WITH (NOLOCK) ON pdf.FeatureId = amo.FeatureId
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
LEFT JOIN AvDetail_ProductBrand avb WITH (NOLOCK) ON pb.Id = avb.ProductBrandId
LEFT JOIN AvDetail av WITH (NOLOCK) ON av.AvDetailId = avb.AvDetailId
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

    private string GetProductGroupsCommandText() => "select t1.ProductVersionId as ProductId, t2.FullName from ProductVersion_ProductGroup t1 join PROGRAM t2 on t1.ProductGroupId = t2.id";

    private string GetTSQLLeadproductCommandText()
    {
        return @"
SELECT DISTINCT lp.ID AS ProductId,
    lp.DotsName + ' (' + lpr.Name + ')' AS LeadProduct
FROM ProductVersionRelease pr WITH (NOLOCK)
LEFT JOIN ProductVersion_Release pv WITH (NOLOCK) ON pr.id = pv.ReleaseID
LEFT JOIN ProductVersion_Release lpv WITH (NOLOCK) ON lpv.id = pv.LeadProductreleaseID
LEFT JOIN ProductVersion lp WITH (NOLOCK) ON lp.id = lpv.ProductVersionID
LEFT JOIN ProductVersionRelease lpr WITH (NOLOCK) ON lpr.id = lpv.ReleaseID
";
    }

    private string GetChipsetsCommandText()
    {
        return @"
SELECT p_c.ProductVersionID AS ProductId,
    c.CodeName
FROM Chipset c WITH (NOLOCK)
LEFT JOIN Product_Chipset p_c WITH (NOLOCK) ON c.[ID] = p_c.[ChipsetId]
WHERE p_c.ChipsetId IS NOT NULL
";
    }

    private string GetBiosVersionText()
    {
        return @"
SELECT dbo.Concatenate(v.version) AS TargetedVersions,
    pd.ProductVersionId as ProductId
FROM (
    SELECT deliverableversionid,
        pd.productversionid
    FROM product_deliverable pd WITH (NOLOCK)
    WHERE targeted = 1
    ) pd
LEFT JOIN deliverableversion v WITH (NOLOCK) ON v.id = pd.deliverableversionid
LEFT JOIN deliverableroot r WITH (NOLOCK) ON r.id = v.deliverablerootid
WHERE r.categoryid = 161
    AND (
        isnumeric(left(v.version, 1)) = 0
        OR (
            left(v.version, 1) = '0'
            AND substring(v.version, 3, 1) = '.'
            )
        )
GROUP BY pd.productversionid
";
    }

    private string GetFactoryNameCommandText()
    {
        return @"
select Name + ' (' + Code + ')' as FactoryName,
        productversionID as ProductId
from ManufacturingSite
INNER JOIN product_Factory WITH (NOLOCK) ON ManufacturingSite.ManufacturingSiteId = product_Factory.FactoryID 
";
    }

    private string GetCurrentROMText()
    {
        return "Select id,currentROM,currentWebROM From ProductVersion";
    }

    private string GetAvDetailText()
    {
        return @"
SELECT DISTINCT p.ID AS ProductId,
    A.AvNo
FROM productversion p
LEFT JOIN Product_Brand PB ON PB.productVersionID = p.ID
LEFT JOIN AvDetail_ProductBrand APB ON APB.productBrandID = PB.Id
LEFT JOIN AvDetail A ON A.AvDetailID = APB.AvDetailID
WHERE APB.STATUS = 'A'
";
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

        SqlCommand command = new(GetProductGroupsCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> productGroups = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductId"].ToString(), out int productId))
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
            if (int.TryParse(product.GetValue("ProductId"), out int productId)
                && productGroups.ContainsKey(productId))
            {
                product.Add("ProductGroups", string.Join(" ", productGroups[productId]));
            }
        }
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
            if (int.TryParse(product.GetValue("ProductId"), out int productId)
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
            if (int.TryParse(product.GetValue("ProductId"), out int productId)
              && chipsets.ContainsKey(productId))
            {
                product.Add("Chipsets", string.Join(" ", chipsets[productId]));
            }
        }
    }

    private async Task GetCurrentBIOSVersionsAsync(IEnumerable<CommonDataModel> products)
    {
        Dictionary<int, string> biosVersion = await GetTargetBIOSVersionAsync();
        Dictionary<int, (string, string)> currentROM = await GetCurrentROMOrCurrentWebROMAsync();

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("ProductId").ToString(), out int productId))
            {
                product.Add("CurrentBIOSVersions", await GetTargetedVersionsAsync(productId,
                                                                                  product.GetValue("ProductStatus"),
                                                                                  currentROM[productId].Item1,
                                                                                  currentROM[productId].Item2,
                                                                                  biosVersion));
            }
        }
    }

    private async Task<string> GetTargetedVersionsAsync(int productId, string statusName, string currentROM, string currentWebROM, Dictionary<int, string> biosVersion)
    {
        statusName = statusName ?? string.Empty;

        if (string.IsNullOrEmpty(currentROM) && (statusName.Equals("Development", StringComparison.OrdinalIgnoreCase) || statusName.Equals("Definition", StringComparison.OrdinalIgnoreCase)))
        {
            string value = string.Empty;
            if (biosVersion.ContainsKey(productId))
            {
                value = biosVersion[productId];
            }
            currentROM = $"Targeted: {value}";
            
        }
        else if (!statusName.Equals("Development", StringComparison.OrdinalIgnoreCase) && !statusName.Equals("Definition", StringComparison.OrdinalIgnoreCase))
        {
            currentROM = string.IsNullOrEmpty(currentROM) ? $"Factory: {currentROM}" : $"Factory: UnKnown";
        }

        if (!string.IsNullOrEmpty(currentROM) && !string.IsNullOrEmpty(currentWebROM))
        {
            currentROM += $" Web: {currentWebROM}";
        }
        else if (string.IsNullOrEmpty(currentROM) && !string.IsNullOrEmpty(currentWebROM))
        {
            currentROM = $"Web: {currentWebROM}";
        }

        return currentROM;
    }

    public async Task<Dictionary<int, string>> GetTargetBIOSVersionAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();
        SqlCommand command = new(GetBiosVersionText(), connection);

        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> biosVersion = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            biosVersion[productId] = reader["TargetedVersions"].ToString();
        }
        return biosVersion;
    }

    public async Task<Dictionary<int, (string, string)>> GetCurrentROMOrCurrentWebROMAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();
        SqlCommand command = new(GetCurrentROMText(), connection);

        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, (string, string)> currentROM = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["id"].ToString(), out int productId))
            {
                continue;
            }
            currentROM[productId] = (reader["currentROM"].ToString() , reader["currentWebROM"].ToString());
        }
        return currentROM;
    }

    private async Task GetAvDetailAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        SqlCommand command = new(GetAvDetailText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> avDetail = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                if (avDetail.ContainsKey(productId))
                {
                    avDetail[productId].Add(reader["AvNo"].ToString());
                }
                else
                {
                    avDetail[productId] = new List<string> { reader["AvNo"].ToString() };
                }
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("ProductId"), out int productId)
                && avDetail.ContainsKey(productId))
            {
                for ( int i = 0 ; i < avDetail[productId].Count();  i++)
                {
                    product.Add("AvDetail " + i, avDetail[productId][i]);
                }
            }
        }
    }

    private async Task GetFactoryNameAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();
        SqlCommand command = new(GetFactoryNameCommandText(), connection);

        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> factoryName = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (factoryName.ContainsKey(productId))
            {
                factoryName[productId].Add(reader["FactoryName"].ToString());
            }
            else
            {
                factoryName[productId] = new List<string>() { reader["FactoryName"].ToString() };
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("ProductId"), out int productId)
              && factoryName.ContainsKey(productId))
            {
                for (int i = 0; i < factoryName[productId].Count; i++)
                {
                    product.Add("FactoryName" + i, factoryName[productId][i]);
                }
            }
        }
    }
}
