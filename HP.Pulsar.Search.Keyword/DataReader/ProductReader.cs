using System;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;
using static Azure.Core.HttpHeader;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class ProductReader : IKeywordSearchDataReader
{
    private readonly KeywordSearchInfo _info;

    public ProductReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public async Task<CommonDataModel> GetDataAsync(int productId)
    {
        CommonDataModel product = await GetProductAsync(productId);

        if (!product.GetElements().Any())
        {
            return null;
        }

        List<Task> tasks = new()
        {
            FillEndOfProductionDateAsync(product),
            FillProductGroupAsync(product),
            FillLeadProductAsync(product),
            FillChipsetAsync(product),
            FillCurrentBiosVersionAsync(product),
            FillAvDetailAsync(product),
            FillFactoryNameAsync(product),
            FillReferencePlatformAsync(product),
            FillMarketingNamesAndPHWebNamesAsync(product),
            FillKMATAsync(product),
            FillOperatingSystemAsync(product),
            FillBaseUnitGroupsAsync(product),
            FillWHQLAsync(product)
        };

        await Task.WhenAll(tasks);
        SystemBoardId(product);
        DeleteProperty(product);

        return product;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> products = await GetProductsAsync();

        List<Task> tasks = new()
        {
            FillEndOfProductionDatesAsync(products),
            FillProductGroupsAsync(products),
            FillLeadProductsAsync(products),
            FillChipsetsAsync(products),
            FillCurrentBiosVersionsAsync(products),
            FillAvDetailsAsync(products),
            FillFactoryNamesAsync(products),
            FillReferencePlatformAsync(products),
            FillMarketingNamesAndPHWebNamesAsync(products),
            FillKMATAsync(products),
            FillOperatingSystemAsync(products),
            FillBaseUnitGroupsAsync(products),
            FillWHQLAsync(products)
        };

        await Task.WhenAll(tasks);
        SystemBoardId(products);
        DeleteProperty(products);

        return products;
    }

    private string GetProductsCommandText()
    {
        return @"
SELECT p.id AS 'Product Id',
    DOTSName AS 'Product Name',
    CASE WHEN p.DevCenter = 0 THEN ''
        WHEN p.DevCenter = 1 THEN 'Houston'
        WHEN p.DevCenter = 2 THEN 'Taiwan - Consumer'
        WHEN p.DevCenter = 3 THEN 'Taiwan - Commercial'
        WHEN p.DevCenter = 4 THEN 'Singapore'
        WHEN p.DevCenter = 5 THEN 'Brazil'
        WHEN p.DevCenter = 6 THEN 'Mobility'
        WHEN p.DevCenter = 7 THEN 'San Diego'
        WHEN p.DevCenter = 8 THEN 'No Dev. Center'
        WHEN p.DevCenter = 9 THEN 'Fort Collins'
    END AS 'Development Center',
    p.SystemboardComments,
    vw_GetEndOfServiceLifeDate.EndOfServiceLifeDate as 'End Of Service Date',
    ps.Name AS 'Product Phase',
    sg.Name AS 'Business Segment',
    p.CreatedBy AS 'Creator Name',
    p.Created AS 'Created Date',
    p.UpdatedBy AS 'Last Updater Name',
    p.Updated AS 'Latest Update Date',
    user_SMID.FirstName + ' ' + user_SMID.LastName AS 'System Manager',
    user_SMID.Email AS 'System Manager Email',
    user_PDPM.FirstName + ' ' + user_PDPM.LastName AS 'Platform Development PM',
    user_PDPM.Email AS 'Platform Development PM Email',
    user_SCID.FirstName + ' ' + user_SCID.LastName AS 'Supply Chain',
    user_SCID.Email AS 'Supply Chain Email',
    user_ODMSEPM.FirstName + ' ' + user_ODMSEPM.LastName AS 'ODM System Engineering PM',
    user_ODMSEPM.Email AS 'ODM System Engineering PM Email',
    user_CM.FirstName + ' ' + user_CM.LastName AS 'Configuration Manager',
    user_CM.Email AS 'Configuration Manager Email',
    user_CPM.FirstName + ' ' + user_CPM.LastName AS 'Commodity PM',
    user_CPM.Email AS 'Commodity PM Email',
    user_Service.FirstName + ' ' + user_Service.LastName AS Service,
    user_Service.Email AS 'Service Email',
    user_ODMHWPM.FirstName + ' ' + user_ODMHWPM.LastName AS 'ODM HW PM',
    user_ODMHWPM.Email AS 'ODM HW PM Email',
    user_POPM.FirstName + ' ' + user_POPM.LastName AS 'Program Office Program Manager',
    user_POPM.Email AS 'Program Office Program Manager Email',
    user_Quality.FirstName + ' ' + user_Quality.LastName AS Quality,
    user_Quality.Email AS 'Quality Email',
    user_PPM.FirstName + ' ' + user_PPM.LastName AS 'Planning PM',
    user_PPM.Email AS 'Planning PM Email',
    user_BIOSPM.FirstName + ' ' + user_BIOSPM.LastName AS 'BIOS PM',
    user_BIOSPM.Email AS 'BIOS PM Email',
    user_SEPM.FirstName + ' ' + user_SEPM.LastName AS 'Systems Engineering PM',
    user_SEPM.Email AS 'Systems Engineering PM Email',
    user_MPM.FirstName + ' ' + user_MPM.LastName AS 'Marketing/Product Mgmt',
    user_MPM.Email AS 'Marketing/Product Mgmt Email',
    user_ProPM.FirstName + ' ' + user_ProPM.LastName AS 'Procurement PM',
    user_ProPM.Email AS 'Procurement PM Email',
    user_SWM.FirstName + ' ' + user_SWM.LastName AS 'SW Marketing',
    user_SWM.Email AS 'SW Marketing Email',
    pf.Name AS 'Product Family',
    partner.name AS ODM,
    pis.Name AS 'Release Team',
    p.RegulatoryModel AS 'Regulatory Model',
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
    pl.Name + '-' + pl.Description AS 'Product Line',
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
        END AS 'Preinstall Team',
    p.MachinePNPID AS 'Machine PNP ID',
    p.RCTOSites as 'RCTO Sites',
    p.IsNdaProduct as 'Is Nda Product',
    p.TypeId,
    p.ImagePO AS 'Current Image Part Number',
    p.AllowFollowMarketingName,
    isnull(p.FusionRequirements, 0) AS 'FusionRequirements',
    RoHS.Name AS 'Minimum RoHS Level'
FROM ProductVersion p
LEFT JOIN ProductFamily pf ON p.ProductFamilyId = pf.id
LEFT JOIN Partner partner ON partner.id = p.PartnerId
LEFT JOIN ProductStatus ps ON ps.id = p.ProductStatusID
LEFT JOIN BusinessSegment sg ON sg.BusinessSegmentID = p.BusinessSegmentID
LEFT JOIN PreinstallTeam pis ON pis.ID = p.ReleaseTeam
LEFT JOIN UserInfo user_SMID ON user_SMID.userid = p.SMID
LEFT JOIN UserInfo user_PDPM ON user_PDPM.userid = p.PlatformDevelopmentID
LEFT JOIN UserInfo user_SCID ON user_SCID.userid = p.SupplyChainID
LEFT JOIN UserInfo user_ODMSEPM ON user_ODMSEPM.userid = p.ODMSEPMID
LEFT JOIN UserInfo user_CM ON user_CM.userid = p.PMID
LEFT JOIN UserInfo user_CPM ON user_CPM.userid = p.PDEID
LEFT JOIN UserInfo user_Service ON user_Service.userid = p.ServiceID
LEFT JOIN UserInfo user_ODMHWPM ON user_ODMHWPM.userid = p.ODMHWPMID
LEFT JOIN UserInfo user_POPM ON user_POPM.userid = p.TDCCMID
LEFT JOIN UserInfo user_Quality ON user_Quality.userid = p.QualityID
LEFT JOIN UserInfo user_PPM ON user_PPM.userid = p.PlanningPMID
LEFT JOIN UserInfo user_BIOSPM ON user_BIOSPM.userid = p.BIOSLeadID
LEFT JOIN UserInfo user_SEPM ON user_SEPM.userid = p.SEPMID
LEFT JOIN UserInfo user_MPM ON user_MPM.userid = p.ConsMarketingID
LEFT JOIN UserInfo user_ProPM ON user_ProPM.userid = p.ProcurementPMID
LEFT JOIN UserInfo user_SWM ON user_SWM.userid = p.SwMarketingId
LEFT JOIN ProductLine pl ON pl.Id = p.ProductLineId
LEFT JOIN vw_GetEndOfServiceLifeDate on vw_GetEndOfServiceLifeDate.productId = p.Id
LEFT JOIN RoHS WITH (NOLOCK) ON p.MinRoHSLevel = RoHS.ID 

WHERE (
        @ProductId = - 1
        OR p.Id = @ProductId
        )
";
    }

    private async Task<CommonDataModel> GetProductAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductsCommandText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel product = new();
        if (await reader.ReadAsync())
        {
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }

                if (columnName.Equals(TargetName.Product, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                product.Add(columnName, value);
            }

            product.Add("Target", TargetTypeValue.Product);
            product.Add("Id", SearchIdName.Product + product.GetValue("Product Id"));
        }
        return product;
    }

    // This function is to get all products
    private async Task<IEnumerable<CommonDataModel>> GetProductsAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductsCommandText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();
        while (await reader.ReadAsync())
        {
            CommonDataModel product = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }

                if (columnName.Equals(TargetName.Product, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                product.Add(columnName, value);
            }

            product.Add("Target", TargetTypeValue.Product);
            product.Add("Id", SearchIdName.Product + product.GetValue("Product Id"));
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
    AND (
        @ProductId = - 1
        OR pv.Id = @ProductId
        )
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
    AND (
            @ProductId = - 1
            OR pb.ProductVersionId = @ProductId
            )
GROUP BY pb.ProductVersionId
";
    }

    private string GetProductGroupsCommandText() => "select t1.ProductVersionId as ProductId, t2.FullName from ProductVersion_ProductGroup t1 join PROGRAM t2 on t1.ProductGroupId = t2.id WHERE ( @ProductId = - 1 OR t1.ProductVersionId = @ProductId )";

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
WHERE (
        @ProductId = - 1
        OR lp.ID = @ProductId
        )
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
    AND (
        @ProductId = - 1
        OR p_c.ProductVersionID = @ProductId
        )
";
    }

    private string GetBiosVersionText()
    {
        return @"
SELECT dbo.Concatenate(v.version) AS TargetedVersions,
    pd.ProductVersionId AS ProductId
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
    AND (
        @ProductId = - 1
        OR pd.ProductVersionId = @ProductId
        )
GROUP BY pd.productversionid
";
    }

    private string GetFactoryNameCommandText()
    {
        return @"
SELECT Name + ' (' + Code + ')' AS FactoryName,
    productversionID AS ProductId
FROM ManufacturingSite
INNER JOIN product_Factory WITH (NOLOCK) ON ManufacturingSite.ManufacturingSiteId = product_Factory.FactoryID
WHERE (
        @ProductId = - 1
        OR productversionID = @ProductId
        )
";
    }

    private string GetCurrentROMText()
    {
        return "Select id,currentROM,currentWebROM From ProductVersion WHERE ( @ProductId = - 1 OR id = @ProductId )";
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
    AND (
        @ProductId = - 1
        OR p.ID = @ProductId
        )
";
    }

    private string GetReferencePlatformText()
    {
        return @"
Select v.id as ProductId ,f2.name + ' ' + v2.version as ReferencePlatform 
from productversion v with (NOLOCK), productversion v2 with (NOLOCK), productfamily f2 with (NOLOCK) 
where f2.id = v2.productfamilyid 
and v2.id = v.referenceid 
    AND (
        @ProductId = - 1
        OR v.id = @ProductId
        )
";
    }

    private string GetKMATText()
    {
        return @"
SELECT p.ProductVersionID AS 'ProductId',
    isnull(p.KMAT, '') AS 'KMAT', 
    p.LastPublishDt AS 'Last SCM Publish Date'
FROM Product_Brand p WITH (NOLOCK) 
left join ProductVersion pv on pv.ProductVersionID = p.ProductVersionID 
where pv.AllowFollowMarketingName =1
    AND(
        @ProductId = - 1
        OR p.ProductVersionID = @ProductId
        )
";
    }

    private string GetMarketingNamesAndPHWebNamesText()
    {
        return @"
SELECT p.ProductVersionId AS 'ProductId', 
    l.ID, 
    l.Name, 
    l.StreetName, 
    l.StreetName2, 
    l.StreetName3,
    l.Suffix, 
    l.RASSegment, 
    v.version AS 'ProductVersion', 
    v.productname AS 'ProductFamily',  
    p.ID AS 'ProductBrandID', 
    v.dotsname AS 'ProductName', 
    p.LastPublishDt, 
    l.ShowSeriesNumberInLogoBadge, 
    l.ShowSeriesNumberInBrandname, 
    l.SplitSeriesForLogoAndBrand,  
    l.ShowSeriesNumberInShortName,  
    isnull(p.KMAT, '') AS 'KMAT', 
    isnull(p.ServiceTag, '') AS 'ServiceTag', 
    isnull(p.BIOSBranding, '') AS 'BIOSBranding', 
    isnull(p.LogoBadge, '') AS 'LogoBadge', 
    isnull(p.LongName, '') AS 'LongName',  
    isnull(p.ShortName, '') AS 'ShortName', 
    isnull(p.FamilyName, '') AS 'FamilyName', 
    isnull(p.BrandName, '') AS 'BrandName', 
    '' AS 'SeriesName', 
    0 AS 'SeriesID', 
    isnull(p.MasterLabel, '') AS 'MasterLabel', 
    isnull(p.CTOModelNumber, '') AS 'CTOModelNumber' 
FROM product_brand p WITH (NOLOCK) 
INNER JOIN Brand l WITH (NOLOCK) ON p.BrandID = l.ID 
INNER JOIN productversion v WITH (NOLOCK) ON v.id = p.ProductVersionID 
LEFT OUTER JOIN Series s WITH (NOLOCK) ON p.ID = s.ProductBrandId 
WHERE (
        @ProductId = - 1
        OR p.ProductVersionID = @ProductId
        ) 
    AND 
        ( 
            SELECT count(1) 
            FROM Series WITH (NOLOCK) 
            WHERE ProductBrandID = p.ID 
            ) = 0 
            
UNION 
    
SELECT p.ProductVersionId, 
    l.ID, 
    Name = l.Name, 
    l.StreetName, 
    l.StreetName2, 
    l.StreetName3, 
    l.Suffix, 
    l.RASSegment, 
    v.version AS 'ProductVersion', 
    v.productname AS 'ProductFamily', 
    p.ID AS 'ProductBrandID', 
    v.dotsname AS 'Product', 
    p.LastPublishDt, 
    l.ShowSeriesNumberInLogoBadge, 
    l.ShowSeriesNumberInBrandname, 
    l.SplitSeriesForLogoAndBrand, 
    l.ShowSeriesNumberInShortName, 
    isnull(p.KMAT, '') AS 'KMAT',
    isnull(p.ServiceTag, '') AS 'ServiceTag', 
    isnull(Series.BIOSBranding, '') AS 'BIOSBranding', 
    isnull(Series.LogoBadge, '') AS 'LogoBadge', 
    isnull(Series.LongName, '') AS 'LongName', 
    isnull(Series.ShortName, '') AS 'ShortName', 
    isnull(Series.FamilyName, '') AS 'FamilyName', 
    isnull(Series.BrandName, '') AS 'BrandName', 
    isnull(Series.Name, '') AS 'SeriesName', 
    Series.ID AS 'SeriesID', 
    isnull(Series.MasterLabel, '') AS 'MasterLabel', 
    isnull(Series.CTOModelNumber, '') AS 'CTOModelNumber'
FROM product_brand p WITH (NOLOCK) 
INNER JOIN Brand l WITH (NOLOCK) ON p.BrandID = l.ID 
INNER JOIN productversion v WITH (NOLOCK) ON v.id = p.ProductVersionID 
LEFT OUTER JOIN Series WITH (NOLOCK) ON p.ID = Series.ProductBrandID 
WHERE (
        @ProductId = - 1
        OR p.ProductVersionID = @ProductId
        ) 
    AND ( 
        ( 
            SELECT count(1) 
            FROM Series WITH (NOLOCK) 
            WHERE ProductBrandID = p.ID 
            ) > 0 
        ) 
";
    }

    private string GetOperatingSystemText()
    {
        return @"
select po.productversionid as ProductId, 
    o.Name as ShortName,
    po.Preinstall,
    po.Web 
from product_os po with (NOLOCK),
     oslookup o with (NOLOCK) 
where po.osid = o.id 
    and o.id <> 16 
    and (
        @ProductId = - 1
        OR po.productversionid = @ProductId
        )
";
    }

    private string GetPHWebFamilyNameText()
    {
        return @"
SELECT DISTINCT pf.PlatformID ,
        pf.PHWebFamilyName AS 'PHWeb Family Name',
        ISNULL(pb.SCMNumber, 0) AS 'SCMNo',
        pp.ProductVersionID AS 'ProductId',
        pp.ProductBrandID AS 'PBId'
FROM Platform pf 
JOIN ProductVersion_Platform pp ON pf.PlatformID = pp.PlatformID 
JOIN Product_Brand pb ON pp.ProductBrandID = pb.ID 
WHERE (
        @ProductId = - 1
        OR pp.ProductVersionID = @ProductId
        )
";
    }

    private string GetBaseUnitGroupsPartTwoText()
    {
        return @"
SELECT DISTINCT ip.platformid,  
        ic.Description AS 'Base Unit Groups - Chassis', 
        ip.IntroYear AS 'Base Unit Groups - Year', 
        ISNULL(RTRIM(ip.PCA), '') AS 'Base Unit Groups - System Board', 
        ip.MarketingName AS 'Base Unit Groups - Generic Name', 
        mktNameMaster AS 'Base Unit Groups - Marketing Name', 
        ip.SystemID AS 'Base Unit Groups - System Board ID', 
        isnull(CASE isnull(pb.CombinedName, '') 
                WHEN '' 
                    THEN b.Name 
                ELSE pb.CombinedName 
                END, '') AS 'Base Unit Groups - Brand', 
        ip.BrandName AS 'Base Unit Groups - Brand Name',
        CASE  
            WHEN ip.eMMConboard = 1 
                THEN 'Yes' 
            ELSE 'No' 
            END AS 'Base Unit Groups - eMMC onboard', 
        ip.MemoryOnboard AS 'Base Unit Groups - Memory Onboard', 
        ip.PCAGraphicsType AS 'Base Unit Groups - PCA Graphics Type', 
        ip.ModelNumber AS 'Base Unit Groups - Model Number ', 
        ip.GraphicCapacity AS 'Base Unit Groups - Graphic Capacity', 
        CASE  
            WHEN ip.TouchID = 1 
                THEN 'Touch' 
            WHEN ip.TouchID = 2 
                THEN 'Non-Touch' 
            ELSE '' 
            END AS 'Base Unit Groups - Display', 
        ( 
            SELECT count(1) 
            FROM Feature FE 
            INNER JOIN Alias A ON FE.AliasID = A.AliasID 
            INNER JOIN Platform_Alias PA ON PA.AliasID = A.AliasID 
            INNER JOIN ProductVersion_Platform pp WITH (NOLOCK) ON pp.PlatformID = PA.platformid 
            INNER JOIN FeatureStatus ON FE.StatusID = FeatureStatus.StatusID 
            WHERE FeatureStatus.Name = 'Active' 
                AND pp.ProductVersionID = 3160 
                AND pp.PlatformID = ip.PlatformID 
                AND ltrim(rtrim(A.Name)) <> '' 
            ) AS 'Base Unit Groups - Active Base Units', 
        ip.Deployment AS 'Base Unit Groups - Deployment', 
        ip.BrandName AS 'Base Unit Groups - PHWeb Brand Name',
        pp.ProductBrandId,
        pp.ProductVersionID AS 'ProductId',
        p.AllowFollowMarketingName
    FROM [Platform] ip WITH (NOLOCK) 
    INNER JOIN Chassis ic WITH (NOLOCK) ON ic.ChassisID = ip.categoryid 
    INNER JOIN ProductVersion_Platform pp WITH (NOLOCK) ON pp.PlatformID = ip.platformid
    INNER JOIN ProductVersion p on p.Id = pp.ProductVersionID 
    LEFT OUTER JOIN Product_Brand pb WITH (NOLOCK) ON pb.ID = pp.ProductBrandID 
        AND pp.ProductVersionID = pb.ProductVersionID 
    LEFT OUTER JOIN brand b WITH (NOLOCK) ON b.ID = pb.BrandID 
    LEFT OUTER JOIN Series WITH (NOLOCK) ON pb.ID = Series.ProductBrandID 
    LEFT JOIN (SELECT PlatformId, COUNT(PlatformId) ActivePrlChassisCount 
             FROM Prl_Chassis  
             GROUP BY PlatformId) apcc 
       ON apcc.PlatformId = ip.PlatformId 
    WHERE (
            @ProductId = - 1
            OR pp.ProductVersionID = @ProductId
            )  
            AND (
                p.AllowFollowMarketingName != 1 
                OR p.BusinessSegmentID != 1
                )
    ORDER BY ip.platformid
";
    }

    private string GetBaseUnitGroupsPartOneText()
    {
        return @"
SELECT DISTINCT ip.platformid,   
        ic.Description AS 'Base Unit Groups - Chassis', 
        ip.IntroYear AS 'Base Unit Groups - Year', 
        ISNULL(RTRIM(ip.PCA), '') AS 'Base Unit Groups - System Board', 
        ip.MarketingName AS 'Base Unit Groups - Generic Name', 
        mktNameMaster AS 'Base Unit Groups - Marketing Name',
        ip.SystemID AS 'Base Unit Groups - System Board ID', 
        isnull(CASE isnull(pb.CombinedName, '') 
                WHEN '' 
                    THEN b.Name 
                ELSE pb.CombinedName 
                END, '') AS 'Base Unit Groups - Brand', 
        ip.BrandName AS 'Base Unit Groups - Brand Name',
        CASE  
            WHEN ip.eMMConboard = 1 
                THEN 'Yes' 
            ELSE 'No' 
            END AS 'Base Unit Groups - eMMC onboard', 
        ip.MemoryOnboard AS 'Base Unit Groups - Memory Onboard', 
        ip.PCAGraphicsType AS 'Base Unit Groups - PCA Graphics Type', 
        ip.ModelNumber AS 'Base Unit Groups - Model Number', 
        ip.GraphicCapacity AS 'Base Unit Groups - Graphic Capacity', 
        CASE  
            WHEN ip.TouchID = 1 
                THEN 'Touch' 
            WHEN ip.TouchID = 2 
                THEN 'Non-Touch' 
            ELSE '' 
            END AS 'Base Unit Groups - Display', 
        ( 
            SELECT count(1) 
            FROM Feature FE 
            INNER JOIN Alias A ON FE.AliasID = A.AliasID 
            INNER JOIN Platform_Alias PA ON PA.AliasID = A.AliasID 
            INNER JOIN ProductVersion_Platform pp WITH (NOLOCK) ON pp.PlatformID = PA.platformid 
            INNER JOIN FeatureStatus ON FE.StatusID = FeatureStatus.StatusID 
            WHERE FeatureStatus.Name = 'Active' 
                AND pp.ProductVersionID = 3160 
                AND pp.PlatformID = ip.PlatformID 
                AND ltrim(rtrim(A.Name)) <> '' 
            ) AS 'Base Unit Groups - Active Base Units (Total)', 
        ip.Deployment AS 'Base Unit Groups - Deployment', 
        ip.BrandName AS 'Base Unit Groups - PHWeb Brand Name',
        pp.ProductBrandId,
        pp.ProductVersionID AS 'ProductId'
    FROM [Platform] ip WITH (NOLOCK) 
    INNER JOIN Chassis ic WITH (NOLOCK) ON ic.ChassisID = ip.categoryid 
    INNER JOIN ProductVersion_Platform pp WITH (NOLOCK) ON pp.PlatformID = ip.platformid
    INNER JOIN ProductVersion p on p.Id = pp.ProductVersionID
    LEFT OUTER JOIN Product_Brand pb WITH (NOLOCK) ON pb.ID = pp.ProductBrandID 
        AND pp.ProductVersionID = pb.ProductVersionID 
    LEFT OUTER JOIN brand b WITH (NOLOCK) ON b.ID = pb.BrandID 
    LEFT OUTER JOIN Series WITH (NOLOCK) ON pp.SeriesID = Series.ID 
    LEFT JOIN (SELECT PlatformId, COUNT(PlatformId) ActivePrlChassisCount 
             FROM Prl_Chassis 
             GROUP BY PlatformId) apcc 
       ON apcc.PlatformId = ip.PlatformId 
    WHERE (
            @ProductId = - 1
            OR pp.ProductVersionID = @ProductId
            )
            AND(
                p.AllowFollowMarketingName = 1 
            AND p.BusinessSegmentID = 1
            )
    ORDER BY ip.platformid 
";
    }

    private string GetBaseUnitGroupsPartThreeText()
    {
        return @"
SELECT v.Id AS 'ProductId',
    pp.PlatformID,
    isnull(Series.LogoBadge, '') AS 'LogoBadge',
    b.ShowSeriesNumberInLogoBadge, 
    b.SplitSeriesForLogoAndBrand, 
    b.streetname3, 
    isnull(Series.Name, '') AS 'SeriesName', 
    CASE  
        WHEN pp.MasterLabel IS NULL 
            THEN isnull(Series.MasterLabel, '') 
        ELSE isnull(pp.MasterLabel, '') 
        END AS 'MasterLabel',
    CASE  
        WHEN pp.CTOModelNumber IS NULL 
            THEN isnull(Series.CTOModelNumber, '') 
        ELSE isnull(pp.CTOModelNumber, '') 
        END AS 'CTOModelNumber',
    isnull(v.FusionRequirements, 0) AS 'FusionRequirements'
FROM ProductVersion_Platform pp WITH (NOLOCK) 
JOIN product_brand pb WITH (NOLOCK) ON pp.ProductBrandID = pb.ID 
JOIN Brand b WITH (NOLOCK) ON pb.BrandID = b.ID 
JOIN ProductVersion v WITH (NOLOCK) ON v.id = pp.ProductVersionID 
JOIN Partner pn WITH (NOLOCK) ON pn.PartnerId = v.PartnerID 
LEFT OUTER JOIN Series WITH (NOLOCK) ON pp.SeriesID = Series.ID 
WHERE ( 
            SELECT count(1) 
            FROM ProductVersion_Platform WITH (NOLOCK) 
            WHERE ProductVersionID = pp.ProductVersionID
                AND PlatformID = pp.PlatformID
                AND ISNULL(SeriesId, 0) > 0
            ) > 0 
        AND(
        @ProductId = - 1
        OR pp.ProductVersionID = @ProductId
        )
        
UNION

SELECT pp.productversionid AS 'ProductId',
    pp.PlatformID, 
    isnull(pb.LogoBadge, '') AS 'LogoBadge', 
    b.ShowSeriesNumberInLogoBadge, 
    b.SplitSeriesForLogoAndBrand, 
    b.streetname3, 
    '' AS 'SeriesName', 
    CASE  
        WHEN pp.MasterLabel IS NULL 
            THEN isnull(pb.MasterLabel, '') 
        ELSE isnull(pp.MasterLabel, '') 
        END AS 'MasterLabel', 
    CASE  
        WHEN pp.CTOModelNumber IS NULL 
            THEN isnull(pb.CTOModelNumber, '') 
        ELSE isnull(pp.CTOModelNumber, '') 
        END  AS 'CTOModelNumber',
    isnull(v.FusionRequirements, 0) AS 'FusionRequirements'
FROM ProductVersion_Platform pp WITH (NOLOCK) 
JOIN Product_Brand pb WITH (NOLOCK) ON pp.ProductBrandID = pb.ID 
JOIN Brand b WITH (NOLOCK) ON pb.BrandID = b.ID 
JOIN ProductVersion v WITH (NOLOCK) ON v.id = pp.ProductVersionID 
WHERE ( 
            SELECT count(1) 
            FROM ProductVersion_Platform WITH (NOLOCK) 
            WHERE ProductVersionID = pp.ProductVersionID
                AND PlatformID = pp.PlatformID
                AND ISNULL(SeriesId, 0) > 0
            ) = 0 
        AND(
        @ProductId = - 1
        OR pp.ProductVersionID = @ProductId
        )
";
    }

    private string GetWHQLText()
    {
        return @"
SELECT ProductVersionID AS 'ProductId'
FROM ProductWHQL with(NOLOCK)
where (
        @ProductId = - 1
        OR ProductVersionID = @ProductId
        )
";
    }



    private async Task<Dictionary<int, List<CommonDataModel>>> GetBaseUnitGroupsPartTwoAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBaseUnitGroupsPartTwoText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> baseUnitGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            CommonDataModel rowData = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }
                rowData.Add(columnName, value);
            }

            if (baseUnitGroups.ContainsKey(dbProductId))
            {
                baseUnitGroups[dbProductId].Add(rowData);
            }
            else
            {
                baseUnitGroups[dbProductId] = new List<CommonDataModel>() { rowData };
            }
        }

        return baseUnitGroups;
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetBaseUnitGroupsPartTwoAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBaseUnitGroupsPartTwoText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> baseUnitGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            CommonDataModel rowData = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }
                rowData.Add(columnName, value);
            }

            if (baseUnitGroups.ContainsKey(productId))
            {
                baseUnitGroups[productId].Add(rowData);
            }
            else
            {
                baseUnitGroups[productId] = new List<CommonDataModel>() { rowData };
            }
        }

        return baseUnitGroups;
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetBaseUnitGroupsPartOneAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBaseUnitGroupsPartOneText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> baseUnitGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            CommonDataModel rowData = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }
                rowData.Add(columnName, value);
            }

            if (baseUnitGroups.ContainsKey(dbProductId))
            {
                baseUnitGroups[dbProductId].Add(rowData);
            }
            else
            {
                baseUnitGroups[dbProductId] = new List<CommonDataModel>() { rowData };
            }
        }

        return baseUnitGroups;
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetBaseUnitGroupsPartOneAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBaseUnitGroupsPartOneText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> baseUnitGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            CommonDataModel rowData = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }
                rowData.Add(columnName, value);
            }

            if (baseUnitGroups.ContainsKey(productId))
            {
                baseUnitGroups[productId].Add(rowData);
            }
            else
            {
                baseUnitGroups[productId] = new List<CommonDataModel>() { rowData };
            }
        }

        return baseUnitGroups;
    }

    private async Task<Dictionary<string, CommonDataModel>> GetBaseUnitGroupsPartThreeAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBaseUnitGroupsPartThreeText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<string, CommonDataModel> baseUnitGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            CommonDataModel rowData = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }
                rowData.Add(columnName, value);
            }

            if (!baseUnitGroups.ContainsKey(dbProductId + reader["PlatformID"].ToString()))
            {
                baseUnitGroups[dbProductId + reader["PlatformID"].ToString()] = rowData;
            }
        }

        return baseUnitGroups;
    }

    private async Task<Dictionary<string, CommonDataModel>> GetBaseUnitGroupsPartThreeAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBaseUnitGroupsPartThreeText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<string, CommonDataModel> baseUnitGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            CommonDataModel rowData = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                {
                    continue;
                }
                rowData.Add(columnName, value);
            }

            if (!baseUnitGroups.ContainsKey(productId + reader["PlatformID"].ToString()))
            {
                baseUnitGroups[productId + reader["PlatformID"].ToString()] = rowData;
            }
        }

        return baseUnitGroups;
    }

    private async Task<Dictionary<string, (string, string, string)>> GetLogoModelCTOValueAsync(int productId)
    {
        Dictionary<string, CommonDataModel> baseUnitGroupsPartThree = await GetBaseUnitGroupsPartThreeAsync(productId);
        Dictionary<string, (string, string, string)> logoModelCTOValue = new();
        foreach (string item in baseUnitGroupsPartThree.Keys)
        {
            if (!bool.TryParse(baseUnitGroupsPartThree[item].GetValue("FusionRequirements"), out bool isPulsarProduct))
            {
                continue;
            }

            GetLogoName(baseUnitGroupsPartThree[item],
                        baseUnitGroupsPartThree[item].GetValue("LogoBadge"),
                        isPulsarProduct, out string logoBadge);

            GetMarketingNameValue(baseUnitGroupsPartThree[item].GetValue("MasterLabel"),
                                  out string modelNumber);

            GetMarketingNameValue(baseUnitGroupsPartThree[item].GetValue("CTOModelNumber"),
                                  out string ctoModel);

            if (!logoModelCTOValue.ContainsKey(item))
            {
                logoModelCTOValue[item] = (logoBadge, modelNumber, ctoModel);
            }
        }
        return logoModelCTOValue;
    }

    private async Task<Dictionary<string, (string, string, string)>> GetLogoModelCTOValueAsync()
    {
        Dictionary<string, CommonDataModel> baseUnitGroupsPartThree = await GetBaseUnitGroupsPartThreeAsync();
        Dictionary<string, (string, string, string)> logoModelCTOValue = new();
        foreach (string item in baseUnitGroupsPartThree.Keys)
        {
            if (!bool.TryParse(baseUnitGroupsPartThree[item].GetValue("FusionRequirements"), out bool isPulsarProduct))
            {
                continue;
            }

            GetLogoName(baseUnitGroupsPartThree[item],
                        baseUnitGroupsPartThree[item].GetValue("LogoBadge"),
                        isPulsarProduct, out string logoBadge);

            GetMarketingNameValue(baseUnitGroupsPartThree[item].GetValue("MasterLabel"),
                                  out string modelNumber);

            GetMarketingNameValue(baseUnitGroupsPartThree[item].GetValue("CTOModelNumber"),
                                  out string ctoModel);

            if (!logoModelCTOValue.ContainsKey(item))
            {
                logoModelCTOValue[item] = (logoBadge, modelNumber, ctoModel);
            }
        }
        return logoModelCTOValue;
    }

    private static CommonDataModel GetPlatformFollowMKTGridDataModel(Dictionary<int, List<CommonDataModel>> baseUnitGroupsDic,
                                                                           Dictionary<int, (int, string, string, string)> phWebFamilyName,
                                                                           Dictionary<string, (string, string, string)> logoModelCTOValue,
                                                                           CommonDataModel product,
                                                                           int num,
                                                                           int productId)
    {
        if (!int.TryParse(baseUnitGroupsDic[productId][num].GetValue("ProductBrandId"), out int productBrandId))
        {
            return product;
        }

        if (phWebFamilyName.ContainsKey(productBrandId))
        {
            product.Add("Base Unit Groups - PHWeb Family Name " + num, phWebFamilyName[productBrandId].Item2);
        }

        if (logoModelCTOValue.ContainsKey(productId + baseUnitGroupsDic[productId][num].GetValue("platformid")))
        {
            if (!string.IsNullOrWhiteSpace(logoModelCTOValue[productId + baseUnitGroupsDic[productId][num].GetValue("platformid")].Item1))
            {
                product.Add("Base Unit Groups - Logo Badge C Cover " + num, logoModelCTOValue[productId + baseUnitGroupsDic[productId][num].GetValue("platformid")].Item1);
            }

            if (!string.IsNullOrWhiteSpace(logoModelCTOValue[productId + baseUnitGroupsDic[productId][num].GetValue("platformid")].Item2))
            {
                product.Add("Base Unit Groups - Model Number (Service Tag down) " + num, logoModelCTOValue[productId + baseUnitGroupsDic[productId][num].GetValue("platformid")].Item2);
            }

            if (!string.IsNullOrWhiteSpace(logoModelCTOValue[productId + baseUnitGroupsDic[productId][num].GetValue("platformid")].Item3))
            {
                product.Add("Base Unit Groups - CTO Model Number " + num, logoModelCTOValue[productId + baseUnitGroupsDic[productId][num].GetValue("platformid")].Item3);
            }
        }

        if (baseUnitGroupsDic[productId][num].GetKeys().Contains("AllowFollowMarketingName"))
        {
            baseUnitGroupsDic[productId][num].Delete("AllowFollowMarketingName");
        }

        baseUnitGroupsDic[productId][num].Delete("ProductBrandId");
        baseUnitGroupsDic[productId][num].Delete("ProductId");
        baseUnitGroupsDic[productId][num].Delete("platformid");
        baseUnitGroupsDic[productId][num].Delete("Base Unit Groups - Generic Name");
        baseUnitGroupsDic[productId][num].Delete("Base Unit Groups - Display");
        baseUnitGroupsDic[productId][num].Delete("Base Unit Groups - Deployment");

        foreach (string item in baseUnitGroupsDic[productId][num].GetKeys())
        {
            product.Add(item + ' ' + num, baseUnitGroupsDic[productId][num].GetValue(item));
        }
        return product;
    }

    private static CommonDataModel GetPlatformUnFollowMKTGridDataModel(Dictionary<int, List<CommonDataModel>> baseUnitGroupsDic,
                                                                       CommonDataModel product,
                                                                       int num,
                                                                       int productId)
    {
        baseUnitGroupsDic[productId][num].Delete("ProductBrandId");
        baseUnitGroupsDic[productId][num].Delete("ProductId");
        baseUnitGroupsDic[productId][num].Delete("platformid");
        baseUnitGroupsDic[productId][num].Delete("AllowFollowMarketingName");
        baseUnitGroupsDic[productId][num].Delete("Base Unit Groups - Brand Name");
        baseUnitGroupsDic[productId][num].Delete("Base Unit Groups - PHWeb Brand Name");

        foreach (string item in baseUnitGroupsDic[productId][num].GetKeys())
        {
            product.Add(item + ' ' + num, baseUnitGroupsDic[productId][num].GetValue(item));
        }
        return product;
    }
    private async Task FillBaseUnitGroupsAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        Dictionary<int, List<CommonDataModel>> baseUnitGroupsPartTwo = await GetBaseUnitGroupsPartTwoAsync(productId);
        Dictionary<int, List<CommonDataModel>> baseUnitGroupsPartOne = await GetBaseUnitGroupsPartOneAsync(productId);
        Dictionary<int, (int, string, string, string)> phWebFamilyName = await GetPHWebFamilyNameAsync(productId);
        Dictionary<string, (string, string, string)> logoModelCTOValue = await GetLogoModelCTOValueAsync(productId);

        if (baseUnitGroupsPartOne.ContainsKey(productId))
        {
            for (int i = 0; i < baseUnitGroupsPartOne[productId].Count; i++)
            {
                GetPlatformFollowMKTGridDataModel(baseUnitGroupsPartOne,
                                                  phWebFamilyName,
                                                  logoModelCTOValue,
                                                  product,
                                                  i,
                                                  productId);
            }
        }
        else if (baseUnitGroupsPartTwo.ContainsKey(productId))
        {
            for (int i = 0; i < baseUnitGroupsPartTwo[productId].Count; i++)
            {
                if (!bool.TryParse(baseUnitGroupsPartTwo[productId][i].GetValue("AllowFollowMarketingName"), out bool allowFollowMarketingName))
                {
                    continue;
                }

                if (!allowFollowMarketingName)
                {
                    GetPlatformUnFollowMKTGridDataModel(baseUnitGroupsPartTwo,
                                                        product,
                                                        i,
                                                        productId);
                }
                else
                {
                    GetPlatformFollowMKTGridDataModel(baseUnitGroupsPartTwo,
                                                      phWebFamilyName,
                                                      logoModelCTOValue,
                                                      product,
                                                      i,
                                                      productId);
                }
            }
        }
    }
    private async Task FillBaseUnitGroupsAsync(IEnumerable<CommonDataModel> products)
    {
        Dictionary<int, List<CommonDataModel>> baseUnitGroupsPartTwo = await GetBaseUnitGroupsPartTwoAsync();
        Dictionary<int, List<CommonDataModel>> baseUnitGroupsPartOne = await GetBaseUnitGroupsPartOneAsync();
        Dictionary<int, (int, string, string, string)> phWebFamilyName = await GetPHWebFamilyNameAsync();
        Dictionary<string, (string, string, string)> logoModelCTOValue = await GetLogoModelCTOValueAsync();

        foreach (CommonDataModel product in products)
        {
            if (!int.TryParse(product.GetValue("Product Id"), out int productId))
            {
                continue;
            }

            if (baseUnitGroupsPartOne.ContainsKey(productId))
            {
                for (int i = 0; i < baseUnitGroupsPartOne[productId].Count; i++)
                {
                    GetPlatformFollowMKTGridDataModel(baseUnitGroupsPartOne,
                                                      phWebFamilyName,
                                                      logoModelCTOValue,
                                                      product,
                                                      i,
                                                      productId);
                }
            }
            else if (baseUnitGroupsPartTwo.ContainsKey(productId))
            {
                for (int i = 0; i < baseUnitGroupsPartTwo[productId].Count; i++)
                {
                    if (!bool.TryParse(baseUnitGroupsPartTwo[productId][i].GetValue("AllowFollowMarketingName"), out bool allowFollowMarketingName))
                    {
                        continue;
                    }

                    if (!allowFollowMarketingName)
                    {
                        GetPlatformUnFollowMKTGridDataModel(baseUnitGroupsPartTwo,
                                                            product,
                                                            i,
                                                            productId);
                    }
                    else
                    {
                        GetPlatformFollowMKTGridDataModel(baseUnitGroupsPartTwo,
                                                          phWebFamilyName,
                                                          logoModelCTOValue,
                                                          product,
                                                          i,
                                                          productId);
                    }
                }
            }
        }
    }

    private async Task FillEndOfProductionDateAsync(CommonDataModel product)
    {
        if (int.TryParse(product.GetValue("Product Id"), out int productId)
            && int.TryParse(product.GetValue("TypeId"), out int typeId))
        {
            using SqlConnection connection = new(_info.DatabaseConnectionString);
            await connection.OpenAsync();
            SqlCommand command1 = new(GetEndOfProductionCommand1Text(), connection);
            SqlCommand command2 = new(GetEndOfProductionCommand2Text(), connection);
            SqlParameter parameter1 = new("ProductId", productId);
            SqlParameter parameter2 = new("ProductId", productId);
            command1.Parameters.Add(parameter1);
            command2.Parameters.Add(parameter2);
            Dictionary<int, DateTime> eopDates1 = new();
            Dictionary<int, DateTime> eopDates2 = new();

            using (SqlDataReader reader1 = command1.ExecuteReader())
            {
                if (await reader1.ReadAsync())
                {
                    if (int.TryParse(reader1["ProductId"].ToString(), out int dbProductId)
                        && DateTime.TryParse(reader1["EndOfProductionDate"].ToString(), out DateTime date1))
                    {
                        eopDates1[dbProductId] = date1;
                    }
                }
            }

            using (SqlDataReader reader2 = command2.ExecuteReader())
            {
                if (await reader2.ReadAsync())
                {
                    if (int.TryParse(reader2["ProductId"].ToString(), out int dbProductId)
                        && DateTime.TryParse(reader2["EndOfProductionDate"].ToString(), out DateTime date2))
                    {
                        eopDates2[dbProductId] = date2;
                    }
                }
            }

            if (typeId == 3)
            {
                if (eopDates1.ContainsKey(productId))
                {
                    product.Add("End Of Production Date", eopDates1[productId].ToString("yyyy/MM/dd"));

                    if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        product.Add("End Of Sales Date", GetEndOfSalesDate(eopDates1[productId]));
                    }
                }
            }
            else
            {
                if (eopDates2.ContainsKey(productId))
                {
                    product.Add("End Of Production Date", eopDates2[productId].ToString("yyyy/MM/dd"));

                    if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        product.Add("End Of Sales Date", GetEndOfSalesDate(eopDates2[productId]));
                    }
                }
            }
        }
        product.Delete("TypeId");
    }

    private async Task FillEndOfProductionDatesAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command1 = new(GetEndOfProductionCommand1Text(), connection);
        SqlCommand command2 = new(GetEndOfProductionCommand2Text(), connection);
        SqlParameter parameter1 = new("ProductId", "-1");
        SqlParameter parameter2 = new("ProductId", "-1");
        command1.Parameters.Add(parameter1);
        command2.Parameters.Add(parameter2);
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
                || !int.TryParse(product.GetValue("Product Id"), out int productId))
            {
                product.Delete("TypeId");
                continue;
            }

            if (typeId == 3)
            {
                if (eopDates1.ContainsKey(productId))
                {
                    product.Add("End Of Production Date", eopDates1[productId].ToString("yyyy/MM/dd"));

                    if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        product.Add("End Of Sales Date", GetEndOfSalesDate(eopDates1[productId]));
                    }
                }
            }
            else
            {
                if (eopDates2.ContainsKey(productId))
                {
                    product.Add("End Of Production Date", eopDates2[productId].ToString("yyyy/MM/dd"));

                    if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
                    {
                        product.Add("End Of Sales Date", GetEndOfSalesDate(eopDates2[productId]));
                    }
                }
            }
            product.Delete("TypeId");
        }
    }

    private static string GetEndOfSalesDate(DateTime date)
    {
        return GetLastDateOfMonth(date.AddMonths(3)).ToString("yyyy/MM/dd");
    }

    private static DateTime GetFirstDateOfMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    private static DateTime GetLastDateOfMonth(DateTime date)
    {
        return GetFirstDateOfMonth(date).AddMonths(1).AddDays(-1);
    }

    private async Task FillProductGroupAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetProductGroupsCommandText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> productGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (productGroups.ContainsKey(dbProductId))
            {
                productGroups[dbProductId].Add(reader["FullName"].ToString());
            }
            else
            {
                productGroups[dbProductId] = new List<string> { reader["FullName"].ToString() };
            }
        }

        if (productGroups.ContainsKey(productId))
        {
            product.Add("Product Groups", string.Join(" ", productGroups[productId]));
        }
    }

    private async Task FillProductGroupsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductGroupsCommandText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> productGroups = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (productGroups.ContainsKey(productId))
            {
                productGroups[productId].Add(reader["FullName"].ToString());
            }
            else
            {
                productGroups[productId] = new List<string> { reader["FullName"].ToString() };
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
                && productGroups.ContainsKey(productId))
            {
                product.Add("Product Groups", string.Join(" ", productGroups[productId]));
            }
        }
    }

    private async Task FillLeadProductAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        Dictionary<int, List<string>> leadProducts = new();
        SqlCommand command = new(GetTSQLLeadproductCommandText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (leadProducts.ContainsKey(dbProductId))
            {
                leadProducts[dbProductId].Add(reader["LeadProduct"].ToString());
            }
            else
            {
                leadProducts[dbProductId] = new List<string>()
                {
                    reader["LeadProduct"].ToString()
                };
            }
        }

        if (leadProducts.ContainsKey(productId))
        {
            product.Add("Lead Product", string.Join(", ", leadProducts[productId]));
        }
    }

    private async Task FillLeadProductsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        Dictionary<int, List<string>> leadProducts = new();
        SqlCommand command = new(GetTSQLLeadproductCommandText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

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

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
               && leadProducts.ContainsKey(productId))
            {
                product.Add("Lead Product", string.Join(", ", leadProducts[productId]));
            }
        }
    }

    private async Task FillChipsetAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetChipsetsCommandText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> chipsets = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (chipsets.ContainsKey(dbProductId))
            {
                chipsets[dbProductId].Add(reader["CodeName"].ToString());
            }
            else
            {
                chipsets[dbProductId] = new List<string>() { reader["CodeName"].ToString() };
            }
        }

        if (chipsets.ContainsKey(productId))
        {
            product.Add("Chipsets", string.Join(" ", chipsets[productId]));
        }
    }

    private async Task FillChipsetsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetChipsetsCommandText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
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
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
              && chipsets.ContainsKey(productId))
            {
                product.Add("Chipsets", string.Join(" ", chipsets[productId]));
            }
        }
    }

    private async Task FillCurrentBiosVersionAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        Dictionary<int, string> biosVersion = await GetTargetBIOSVersionAsync(productId);
        Dictionary<int, (string, string)> currentROM = await GetCurrentROMOrCurrentWebROMAsync(productId);

        if (currentROM.ContainsKey(productId))
        {
            product.Add("Current BIOS Versions", GetTargetedVersions(productId,
                                                                     product.GetValue("ProductStatus"),
                                                                     currentROM[productId].Item1,
                                                                     currentROM[productId].Item2,
                                                                     biosVersion));
        }
        else
        {
            product.Add("Current BIOS Versions", GetTargetedVersions(productId,
                                                                     product.GetValue("ProductStatus"),
                                                                     string.Empty,
                                                                     string.Empty,
                                                                     biosVersion));
        }
    }

    private async Task FillCurrentBiosVersionsAsync(IEnumerable<CommonDataModel> products)
    {
        Dictionary<int, string> biosVersion = await GetTargetBiosVersionsAsync();
        Dictionary<int, (string, string)> currentROM = await GetCurrentROMsOrCurrentWebROMsAsync();

        foreach (CommonDataModel product in products)
        {
            if (!int.TryParse(product.GetValue("Product Id"), out int productId))
            {
                continue;
            }

            if (currentROM.ContainsKey(productId))
            {
                product.Add("Current BIOS Versions", GetTargetedVersions(productId,
                                                                         product.GetValue("ProductStatus"),
                                                                         currentROM[productId].Item1,
                                                                         currentROM[productId].Item2,
                                                                         biosVersion));
            }
            else
            {
                product.Add("Current BIOS Versions", GetTargetedVersions(productId,
                                                                         product.GetValue("ProductStatus"),
                                                                         string.Empty,
                                                                         string.Empty,
                                                                         biosVersion));
            }
        }
    }

    private string GetTargetedVersions(int productId,
                                       string statusName,
                                       string currentROM,
                                       string currentWebROM,
                                       Dictionary<int, string> biosVersion)
    {
        if (string.IsNullOrEmpty(currentROM)
            && (string.Equals(statusName, "Development", StringComparison.OrdinalIgnoreCase) || string.Equals(statusName, "Definition", StringComparison.OrdinalIgnoreCase)))
        {
            string value = string.Empty;
            if (biosVersion.ContainsKey(productId))
            {
                value = biosVersion[productId];
            }

            currentROM = $"Targeted: {value}";
        }
        else if (!string.Equals(statusName, "Development", StringComparison.OrdinalIgnoreCase)
                 && !string.Equals(statusName, "Definition", StringComparison.OrdinalIgnoreCase))
        {
            currentROM = string.IsNullOrEmpty(currentROM) ? $"Factory: {currentROM}" : "Factory: UnKnown";
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

    private async Task<Dictionary<int, string>> GetTargetBIOSVersionAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBiosVersionText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> biosVersion = new();

        if (await reader.ReadAsync()
            && int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
        {
            biosVersion[productId] = reader["TargetedVersions"].ToString();
        }

        return biosVersion;
    }

    private async Task<Dictionary<int, string>> GetTargetBiosVersionsAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetBiosVersionText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
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

    private async Task<Dictionary<int, (string, string)>> GetCurrentROMOrCurrentWebROMAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetCurrentROMText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, (string, string)> currentROM = new();

        if (await reader.ReadAsync()
            && int.TryParse(reader["id"].ToString(), out int dbProductId))
        {
            currentROM[productId] = (reader["currentROM"].ToString(), reader["currentWebROM"].ToString());
        }
        return currentROM;
    }

    private async Task<Dictionary<int, (string, string)>> GetCurrentROMsOrCurrentWebROMsAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetCurrentROMText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, (string, string)> currentROM = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["id"].ToString(), out int productId))
            {
                continue;
            }
            currentROM[productId] = (reader["currentROM"].ToString(), reader["currentWebROM"].ToString());
        }
        return currentROM;
    }

    private async Task FillAvDetailAsync(CommonDataModel product)
    {
        if (int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            using SqlConnection connection = new(_info.DatabaseConnectionString);
            await connection.OpenAsync();

            SqlCommand command = new(GetAvDetailText(), connection);
            SqlParameter parameter = new("ProductId", productId);
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();
            Dictionary<int, List<string>> avDetail = new();

            while (await reader.ReadAsync())
            {
                if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
                {
                    continue;
                }

                if (avDetail.ContainsKey(dbProductId))
                {
                    avDetail[dbProductId].Add(reader["AvNo"].ToString());
                }
                else
                {
                    avDetail[dbProductId] = new List<string> { reader["AvNo"].ToString() };
                }
            }

            if (avDetail.ContainsKey(productId))
            {
                for (int i = 0; i < avDetail[productId].Count(); i++)
                {
                    product.Add("Av Detail " + i, avDetail[productId][i]);
                }
            }
        }
    }

    private async Task FillAvDetailsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetAvDetailText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> avDetail = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (avDetail.ContainsKey(productId))
            {
                avDetail[productId].Add(reader["AvNo"].ToString());
            }
            else
            {
                avDetail[productId] = new List<string> { reader["AvNo"].ToString() };
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
                && avDetail.ContainsKey(productId))
            {
                for (int i = 0; i < avDetail[productId].Count(); i++)
                {
                    product.Add("Av Detail " + i, avDetail[productId][i]);
                }
            }
        }
    }

    private async Task FillFactoryNameAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetFactoryNameCommandText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> factoryName = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (factoryName.ContainsKey(dbProductId))
            {
                factoryName[dbProductId].Add(reader["FactoryName"].ToString());
            }
            else
            {
                factoryName[dbProductId] = new List<string>() { reader["FactoryName"].ToString() };
            }
        }

        if (factoryName.ContainsKey(productId))
        {
            for (int i = 0; i < factoryName[productId].Count; i++)
            {
                product.Add("Factory " + i, factoryName[productId][i]);
            }
        }
    }

    private async Task FillFactoryNamesAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetFactoryNameCommandText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
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
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
              && factoryName.ContainsKey(productId))
            {
                for (int i = 0; i < factoryName[productId].Count; i++)
                {
                    product.Add("Factory " + i, factoryName[productId][i]);
                }
            }
        }
    }

    private async Task FillReferencePlatformAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetReferencePlatformText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        if (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                return;
            }

            if (dbProductId.Equals(productId))
            {
                product.Add("Reference Platform", reader["ReferencePlatform"].ToString());
            }
        }
    }


    private async Task FillReferencePlatformAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetReferencePlatformText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> referencePlatform = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            referencePlatform[productId] = reader["ReferencePlatform"].ToString();
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
              && referencePlatform.ContainsKey(productId))
            {
                product.Add("Reference Platform", referencePlatform[productId]);
            }
        }
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetMarketingNamesAndPHWebNamesAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetMarketingNamesAndPHWebNamesText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> marketingNamesAndPHWebNames = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            CommonDataModel dataModel = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None")
                    || columnName.Equals("ProductId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dataModel.Add(columnName, value);
            }

            if (!marketingNamesAndPHWebNames.ContainsKey(dbProductId))
            {
                marketingNamesAndPHWebNames[dbProductId] = new List<CommonDataModel> { dataModel };
            }
            else
            {
                marketingNamesAndPHWebNames[dbProductId].Add(dataModel);
            }
        }
        return marketingNamesAndPHWebNames;
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetMarketingNamesAndPHWebNamesAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetMarketingNamesAndPHWebNamesText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> marketingNamesAndPHWebNames = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            CommonDataModel dataModel = new();
            int fieldCount = reader.FieldCount;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None")
                    || columnName.Equals("ProductId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                dataModel.Add(columnName, value);
            }

            if (!marketingNamesAndPHWebNames.ContainsKey(productId))
            {
                marketingNamesAndPHWebNames[productId] = new List<CommonDataModel> { dataModel };
            }
            else
            {
                marketingNamesAndPHWebNames[productId].Add(dataModel);
            }
        }
        return marketingNamesAndPHWebNames;
    }

    private async Task FillMarketingNamesAndPHWebNamesAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId)
            || !bool.TryParse(product.GetValue("FusionRequirements"), out bool isPulsarProduct))
        {
            return;
        }

        Dictionary<int, List<CommonDataModel>> marketingNamesAndPHWebNames = await GetMarketingNamesAndPHWebNamesAsync(productId);

        if (!marketingNamesAndPHWebNames.ContainsKey(productId))
        {
            return;
        }

        List<CommonDataModel> documents = marketingNamesAndPHWebNames[productId];

        for (int i = 0; i < documents.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(documents[i].GetValue("KMAT")))
            {
                FillPHWebNames(product, documents[i], i);
            }

            FillMarketingNames(product, documents[i], i, isPulsarProduct);
        }
    }

    private async Task FillMarketingNamesAndPHWebNamesAsync(IEnumerable<CommonDataModel> products)
    {
        Dictionary<int, List<CommonDataModel>> marketingNamesAndPHWebNames = await GetMarketingNamesAndPHWebNamesAsync();

        foreach (CommonDataModel product in products)
        {
            if (!int.TryParse(product.GetValue("Product Id"), out int productId)
              || !marketingNamesAndPHWebNames.ContainsKey(productId)
              || !bool.TryParse(product.GetValue("FusionRequirements"), out bool isPulsarProduct))
            {
                continue;
            }

            List<CommonDataModel> documents = marketingNamesAndPHWebNames[productId];

            for (int i = 0; i < documents.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(documents[i].GetValue("KMAT")))
                {
                    FillPHWebNames(product, documents[i], i);
                }

                FillMarketingNames(product, documents[i], i, isPulsarProduct);
            }
        }
    }

    private void FillMarketingNames(CommonDataModel product,
                                    CommonDataModel marketingNamesAndPHWebNamesDocuments,
                                    int docNumber,
                                    bool isPulsarProduct)
    {
        if (!string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(product.GetValue("AllowFollowMarketingName"), "True", StringComparison.OrdinalIgnoreCase)))
        {
            if (GetLongName(marketingNamesAndPHWebNamesDocuments, out string longName))
            {
                product.Add("Marketing Names - Long Name " + docNumber, longName);
            }

            if (GetShortName(marketingNamesAndPHWebNamesDocuments, out string shortName))
            {
                product.Add("Marketing Names - Short Name " + docNumber, shortName);
            }

            if (GetLogoName(marketingNamesAndPHWebNamesDocuments, marketingNamesAndPHWebNamesDocuments.GetValue("LogoBadge"), isPulsarProduct, out string logoBadge))
            {
                product.Add("Marketing Names - Logo Badge C Cover " + docNumber, logoBadge);
            }

            if (GetMarketingNameValue(marketingNamesAndPHWebNamesDocuments.GetValue("ServiceTag"), out string serviceTag))
            {
                product.Add("Marketing Names - HP Brand Name (Service Tag up) " + docNumber, serviceTag);
            }

            if (GetMarketingNameValue(marketingNamesAndPHWebNamesDocuments.GetValue("BIOSBranding"), out string biosBranding))
            {
                product.Add("Marketing Names - BIOS Branding " + docNumber, biosBranding);
            }

            if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(product.GetValue("AllowFollowMarketingName"), "True", StringComparison.OrdinalIgnoreCase))
            {
                if (GetMarketingNameValue(marketingNamesAndPHWebNamesDocuments.GetValue("MasterLabel"), out string modelNumber))
                {
                    product.Add("Marketing Names - Model Number (Service Tag down) " + docNumber, modelNumber);
                }

                if (GetMarketingNameValue(marketingNamesAndPHWebNamesDocuments.GetValue("CTOModelNumber"), out string ctoModel))
                {
                    product.Add("Marketing Names - CTO Model Number " + docNumber, ctoModel);
                }
            }
        }
    }

    private static bool GetLogoName(CommonDataModel item, string logoBadge, bool isPulsarProduct, out string resultValue)
    {
        resultValue = string.Empty;
        string logoNameInnerValue = $"{item.GetValue("StreetName3")}";

        if (item.GetValue("ShowSeriesNumberInLogoBadge").Equals("True", StringComparison.OrdinalIgnoreCase)
            && item.GetValue("SplitSeriesForLogoAndBrand").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            logoNameInnerValue += $" {GetIntPrefix(item.GetValue("SeriesName"))}";
        }
        else if (item.GetValue("ShowSeriesNumberInLogoBadge").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            logoNameInnerValue += $"{item.GetValue("SeriesName")}";
        }

        StringBuilder stringBuilder = new StringBuilder();
        if (string.IsNullOrEmpty(logoBadge))
        {
            logoNameInnerValue = $"{item.GetValue("StreetName3")}";

            if (item.GetValue("ShowSeriesNumberInLogoBadge").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (item.GetValue("SplitSeriesForLogoAndBrand").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    logoNameInnerValue += $" {GetIntPrefix(item.GetValue("SeriesName"))}";
                }
                else
                {
                    stringBuilder.Append(GetIntPrefix(item.GetValue("SeriesName")));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(logoBadge) && !string.IsNullOrEmpty(logoNameInnerValue))
        {
            if (!isPulsarProduct)
            {
                resultValue = stringBuilder.Append($"{logoBadge.Trim()}").ToString();
                return true;
            }

            resultValue = stringBuilder.Append(logoBadge.Trim()).ToString();
            return true;
        }
        else if (!string.IsNullOrEmpty(logoNameInnerValue))
        {
            if (!isPulsarProduct)
            {
                resultValue = stringBuilder.Append($"{logoNameInnerValue.Trim()}").ToString();
                return true;
            }

            resultValue = stringBuilder.Append(logoNameInnerValue.Trim()).ToString();
            return true;
        }
        else if (!isPulsarProduct)
        {
            resultValue = stringBuilder.ToString();
            if (!string.IsNullOrEmpty(resultValue))
            {
                return true;
            }
            return false;
        }

        resultValue = stringBuilder.Append(logoBadge).ToString();
        return true;
    }

    public static int GetIntPrefix(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            text = text.TrimStart();
            for (int size = text.Length; size > 0; size--)
            {
                if (int.TryParse(text.Substring(0, size), out int result))
                {
                    return result;
                }
            }
        }

        return 0;
    }

    private static bool GetShortName(CommonDataModel item, out string resultValue)
    {
        string shortNameInnerValue = string.Empty;

        if (!string.IsNullOrEmpty(item.GetValue("ShortName")))
        {
            shortNameInnerValue = item.GetValue("ShortName");
        }
        else
        {
            shortNameInnerValue += $"{item.GetValue("StreetName2")} ";

            if (item.GetValue("ShowSeriesNumberInShortName").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                shortNameInnerValue += item.GetValue("SeriesName");
            }
        }
        bool result = GetMarketingNameValue(shortNameInnerValue, out string markingNameResult);
        resultValue = markingNameResult;
        return result;
    }

    private static bool GetMarketingNameValue(string marketingName, out string resultValue)
    {
        resultValue = string.Empty;

        if (!string.IsNullOrEmpty(marketingName))
        {
            resultValue = marketingName.Trim();
            return true;
        }

        return false;
    }

    private static bool GetLongName(CommonDataModel item, out string longName)
    {
        longName = string.Empty;

        if (!string.IsNullOrEmpty(item.GetValue("LongName")))
        {
            longName = item.GetValue("LongName");
            return true;
        }

        if (!string.IsNullOrEmpty(item.GetValue("StreetName")))
        {
            longName = $"{item.GetValue("StreetName")} {item.GetValue("SeriesName")}";

            if (!string.IsNullOrEmpty(item.GetValue("Suffix")))
            {
                longName += $" {item.GetValue("Suffix")}";
            }
        }

        if (string.IsNullOrEmpty(longName))
        {
            return false;
        }

        return true;
    }

    private void FillPHWebNames(CommonDataModel product, CommonDataModel marketingNamesAndPHWebNamesDocuments, int docNumber)
    {
        if (!string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(product.GetValue("AllowFollowMarketingName"), "True", StringComparison.OrdinalIgnoreCase)))
        {
            if (GetBrandName(marketingNamesAndPHWebNamesDocuments, out string brandName))
            {
                product.Add("PHweb Names - Brand Name " + docNumber, brandName);
            }

            if (GetFamilyName(marketingNamesAndPHWebNamesDocuments, out string familyName))
            {
                product.Add("PHweb Names - Family Name " + docNumber, familyName);
            }

            if (!string.IsNullOrEmpty(marketingNamesAndPHWebNamesDocuments.GetValue("KMAT")))
            {
                product.Add("PHweb Names - KMAT " + docNumber, marketingNamesAndPHWebNamesDocuments.GetValue("KMAT"));
            }

            if (!string.IsNullOrEmpty(marketingNamesAndPHWebNamesDocuments.GetValue("LastPublishDt")))
            {
                product.Add("PHweb Names - Last SCM Publish Date " + docNumber, marketingNamesAndPHWebNamesDocuments.GetValue("LastPublishDt"));
            }
        }
    }

    private static bool GetBrandName(CommonDataModel item, out string brandName)
    {
        if (string.IsNullOrEmpty(item.GetValue("BrandName")))
        {
            brandName = $"{item.GetValue("streetname")} {item.GetValue("SeriesName")}";

            if (!string.IsNullOrEmpty(brandName))
            {
                return true;
            }

            return false;
        }
        else
        {
            brandName = item.GetValue("BrandName");
            return true;
        }
    }

    private static bool GetFamilyName(CommonDataModel item, out string familyName)
    {
        if (!string.IsNullOrEmpty(item.GetValue("FamilyName")))
        {
            familyName = item.GetValue("FamilyName");
            return true;
        }

        if (string.IsNullOrEmpty(item.GetValue("ProductVersion")))
        {
            familyName = $"{item.GetValue("ProductName")} {item.GetValue("RASSegment")}-{item.GetValue("StreetName")} {item.GetValue("SeriesName")}";

            if (!string.IsNullOrEmpty(familyName))
            {
                return true;
            }

            return false;
        }

        if (item.GetValue("ProductFamily").Equals("davos", StringComparison.OrdinalIgnoreCase) && item.GetValue("ProductVersion").Substring(item.GetValue("ProductVersion").Length - 3, 3).Equals("1.0"))
        {
            familyName = $"{item.GetValue("ProductName")}X - {item.GetValue("StreetName")}";

            if (!string.IsNullOrEmpty(familyName))
            {
                return true;
            }

            return false;
        }
        else if (int.TryParse(item.GetValue("ProductVersion").Substring(item.GetValue("ProductVersion").Length - 1, 1), out int number))
        {
            familyName = $"{item.GetValue("ProductName").Substring(0, item.GetValue("ProductName").Length - item.GetValue("ProductVersion").Length)} {item.GetValue("RASSegment")} {item.GetValue("ProductVersion").Substring(0, item.GetValue("ProductVersion").Length - 1)} X - {item.GetValue("StreetName")}";

            if (!string.IsNullOrEmpty(familyName))
            {
                return true;
            }

            return false;
        }
        else if (item.GetValue("ProductVersion").Length > 1)
        {
            familyName = $"{item.GetValue("ProductName").Substring(0, item.GetValue("ProductName").Length - item.GetValue("ProductVersion").Length)} {item.GetValue("RASSegment")} {item.GetValue("ProductVersion").Substring(0, item.GetValue("ProductVersion").Length - 2)} X - {item.GetValue("StreetName")}";

            if (!string.IsNullOrEmpty(familyName))
            {
                return true;
            }

            return false;
        }

        familyName = $"{item.GetValue("ProductName").Substring(0, item.GetValue("ProductName").Length - item.GetValue("ProductVersion").Length)} {item.GetValue("RASSegment")} {item.GetValue("ProductVersion")} X - {item.GetValue("StreetName")}";

        if (!string.IsNullOrEmpty(familyName))
        {
            return true;
        }

        return false;
    }

    private async Task FillKMATAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetKMATText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> kmat = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (!kmat.ContainsKey(dbProductId))
            {
                kmat[dbProductId] = new List<(string, string)>() { (reader["KMAT"].ToString(), reader["Last SCM Publish Date"].ToString()) };
            }
            else
            {
                kmat[dbProductId].Add((reader["KMAT"].ToString(), reader["Last SCM Publish Date"].ToString()));
            }
        }

        if (kmat.ContainsKey(productId)
            && string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
            && string.Equals(product.GetValue("AllowFollowMarketingName"), "True", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < kmat[productId].Count; i++)
            {
                if (!string.IsNullOrEmpty(kmat[productId][i].Item1))
                {
                    product.Add("KMAT/SCM - KMAT " + i, kmat[productId][i].Item1);
                }

                if (!string.IsNullOrEmpty(kmat[productId][i].Item2))
                {
                    product.Add("KMAT/SCM - Last SCM Publish Date " + i, kmat[productId][i].Item2);
                }
            }
        }
    }

    private async Task FillKMATAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetKMATText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> kmat = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (!kmat.ContainsKey(productId))
            {
                kmat[productId] = new List<(string, string)>() { (reader["KMAT"].ToString(), reader["Last SCM Publish Date"].ToString()) };
            }
            else
            {
                kmat[productId].Add((reader["KMAT"].ToString(), reader["Last SCM Publish Date"].ToString()));
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (int.TryParse(product.GetValue("Product Id"), out int productId)
                && kmat.ContainsKey(productId)
                && string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase)
                && string.Equals(product.GetValue("AllowFollowMarketingName"), "True", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < kmat[productId].Count; i++)
                {
                    if (!string.IsNullOrEmpty(kmat[productId][i].Item1)
                        && !string.IsNullOrWhiteSpace(kmat[productId][i].Item1))
                    {
                        product.Add("KMAT/SCM - KMAT " + i, kmat[productId][i].Item1);
                    }

                    if (!string.IsNullOrEmpty(kmat[productId][i].Item2)
                        && !string.IsNullOrWhiteSpace(kmat[productId][i].Item2))
                    {
                        product.Add("KMAT/SCM - Last SCM Publish Date " + i, kmat[productId][i].Item2);
                    }
                }
            }
        }
    }

    private async Task<(Dictionary<int, string>, Dictionary<int, string>)> GetOperatingSystemAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetOperatingSystemText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> operatingSystemPreinstall = new();
        Dictionary<int, string> operatingSystemWeb = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (operatingSystemPreinstall.ContainsKey(dbProductId)
                && string.Equals(reader["Preinstall"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemPreinstall[dbProductId] += " , " + reader["ShortName"].ToString();
            }
            else if (!operatingSystemPreinstall.ContainsKey(dbProductId)
                    && string.Equals(reader["Preinstall"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemPreinstall[dbProductId] = reader["ShortName"].ToString();
            }

            if (operatingSystemWeb.ContainsKey(dbProductId)
                && string.Equals(reader["Web"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemWeb[dbProductId] += " , " + reader["ShortName"].ToString();
            }
            else if (!operatingSystemWeb.ContainsKey(dbProductId)
                    && string.Equals(reader["Web"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemWeb[dbProductId] = reader["ShortName"].ToString();
            }

        }
        return (operatingSystemPreinstall, operatingSystemWeb);
    }

    private async Task FillOperatingSystemAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        (Dictionary<int, string> preinstall, Dictionary<int, string> web) = await GetOperatingSystemAsync(productId);

        if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (preinstall.ContainsKey(productId)
            && !string.IsNullOrEmpty(preinstall[productId])
                && !string.IsNullOrWhiteSpace(preinstall[productId]))
        {
            product.Add("Operating System - Preinstall ", preinstall[productId]);
        }

        if (web.ContainsKey(productId)
            && !string.IsNullOrEmpty(web[productId])
                && !string.IsNullOrWhiteSpace(web[productId]))
        {
            product.Add("Operating System - Web ", web[productId]);
        }
    }

    private async Task<(Dictionary<int, string>, Dictionary<int, string>)> GetOperatingSystemAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetOperatingSystemText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> operatingSystemPreinstall = new();
        Dictionary<int, string> operatingSystemWeb = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (operatingSystemPreinstall.ContainsKey(productId)
                && string.Equals(reader["Preinstall"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemPreinstall[productId] += " , " + reader["ShortName"].ToString();
            }
            else if (!operatingSystemPreinstall.ContainsKey(productId)
                    && string.Equals(reader["Preinstall"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemPreinstall[productId] = reader["ShortName"].ToString();
            }

            if (operatingSystemWeb.ContainsKey(productId)
                && string.Equals(reader["Web"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemWeb[productId] += " , " + reader["ShortName"].ToString();
            }
            else if (!operatingSystemWeb.ContainsKey(productId)
                    && string.Equals(reader["Web"].ToString(), "True", StringComparison.OrdinalIgnoreCase))
            {
                operatingSystemWeb[productId] = reader["ShortName"].ToString();
            }

        }
        return (operatingSystemPreinstall, operatingSystemWeb);
    }

    private async Task FillOperatingSystemAsync(IEnumerable<CommonDataModel> products)
    {
        (Dictionary<int, string> preinstall, Dictionary<int, string> web) = await GetOperatingSystemAsync();

        foreach (CommonDataModel product in products)
        {
            if (!int.TryParse(product.GetValue("Product Id"), out int productId))
            {
                continue;
            }

            if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (preinstall.ContainsKey(productId)
                && !string.IsNullOrEmpty(preinstall[productId])
                    && !string.IsNullOrWhiteSpace(preinstall[productId]))
            {
                product.Add("Operating System - Preinstall ", preinstall[productId]);
            }

            if (web.ContainsKey(productId)
                && !string.IsNullOrEmpty(web[productId])
                    && !string.IsNullOrWhiteSpace(web[productId]))
            {
                product.Add("Operating System - Web ", web[productId]);
            }
        }
    }

    private async Task<Dictionary<int, (int, string, string, string)>> GetPHWebFamilyNameAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetPHWebFamilyNameText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, (int, string, string, string)> phWebFamilyName = new();

        while (await reader.ReadAsync())
        {
            if (string.IsNullOrWhiteSpace(reader["PHWeb Family Name"].ToString())
                || !int.TryParse(reader["PBId"].ToString(), out int pbId)
                || !int.TryParse(reader["PlatformID"].ToString(), out int platformID))
            {
                continue;
            }

            if (phWebFamilyName.ContainsKey(pbId)
                && string.Equals(phWebFamilyName[pbId].Item4, reader["ProductId"].ToString())
                && string.Equals(phWebFamilyName[pbId].Item3, reader["SCMNo"].ToString())
                && phWebFamilyName[pbId].Item1 > platformID)
            {
                phWebFamilyName[pbId] = (platformID,
                                         reader["PHWeb Family Name"].ToString(),
                                         reader["SCMNo"].ToString(),
                                         reader["ProductId"].ToString());
            }
            else if (!phWebFamilyName.ContainsKey(pbId))
            {
                phWebFamilyName[pbId] = (platformID,
                                         reader["PHWeb Family Name"].ToString(),
                                         reader["SCMNo"].ToString(),
                                         reader["ProductId"].ToString());
            }
        }

        return phWebFamilyName;
    }

    private async Task<Dictionary<int, (int, string, string, string)>> GetPHWebFamilyNameAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetPHWebFamilyNameText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, (int, string, string, string)> phWebFamilyName = new();

        while (await reader.ReadAsync())
        {
            if (string.IsNullOrWhiteSpace(reader["PHWeb Family Name"].ToString())
                || !int.TryParse(reader["PBId"].ToString(), out int pbId)
                || !int.TryParse(reader["PlatformID"].ToString(), out int platformID))
            {
                continue;
            }

            if (phWebFamilyName.ContainsKey(pbId)
                && string.Equals(phWebFamilyName[pbId].Item4, reader["ProductId"].ToString())
                && string.Equals(phWebFamilyName[pbId].Item3, reader["SCMNo"].ToString())
                && phWebFamilyName[pbId].Item1 > platformID)
            {
                phWebFamilyName[pbId] = (platformID,
                                         reader["PHWeb Family Name"].ToString(),
                                         reader["SCMNo"].ToString(),
                                         reader["ProductId"].ToString());
            }
            else if (!phWebFamilyName.ContainsKey(pbId))
            {
                phWebFamilyName[pbId] = (platformID,
                                         reader["PHWeb Family Name"].ToString(),
                                         reader["SCMNo"].ToString(),
                                         reader["ProductId"].ToString());
            }
        }

        return phWebFamilyName;
    }

    private static IEnumerable<CommonDataModel> DeleteProperty(IEnumerable<CommonDataModel> products)
    {
        foreach (CommonDataModel product in products)
        {
            DeleteProperty(product);
        }

        return products;
    }

    private static CommonDataModel DeleteProperty(CommonDataModel product)
    {
        if (string.Equals(product.GetValue("Development Center"), "Taiwan - Consumer", StringComparison.OrdinalIgnoreCase))
        {
            product.Delete("Lead Product");
        }
        else
        {
            product.Delete("Reference Platform");
        }

        if (string.Equals(product.GetValue("FusionRequirements"), "True", StringComparison.OrdinalIgnoreCase))
        {
            product.Delete("Minimum RoHS Level");
        }
        else
        {
            product.Delete("Releases");
            product.Delete("Chipsets");
        }

        product.Delete("AllowFollowMarketingName");
        product.Delete("FusionRequirements");

        return product;
    }

    private async Task FillWHQLAsync(CommonDataModel product)
    {
        if (!int.TryParse(product.GetValue("Product Id"), out int productId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetWHQLText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        List<int> whql = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int dbProductId))
            {
                continue;
            }

            if (!whql.Contains(dbProductId))
            {
                whql.Add(dbProductId);
            }
        }

        if (whql.Contains(productId))
        {
            product.Add("WHQL Statu", "Unknown");
        }
        else
        {
            product.Add("WHQL Statu", "incomplete");
        }
    }

    private async Task FillWHQLAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetWHQLText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        List<int> whql = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productId))
            {
                continue;
            }

            if (!whql.Contains(productId))
            {
                whql.Add(productId);
            }
        }

        foreach (CommonDataModel product in products)
        {
            if (!int.TryParse(product.GetValue("Product Id"), out int productId))
            {
                continue;
            }

            if (whql.Contains(productId))
            {
                product.Add("WHQL Statu", "Unknown");
            }
            else
            {
                product.Add("WHQL Statu", "incomplete");
            }
        }
    }

    private static string FormatSystemId(string comments)
    {
        if (!comments.Contains("^") && !comments.Contains("|"))
        {
            return comments;
        }

        StringBuilder sb = new StringBuilder();

        foreach (string item in comments.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!item.Contains("^"))
            {
                sb.Append(", ").Append(item);
                continue;
            }

            string[] splitItems = item.Split(new string[] { "^" }, StringSplitOptions.RemoveEmptyEntries);
            sb.Append(", ").Append(splitItems[0]);

            if (splitItems.Length > 1)
            {
                sb.Append($" ({splitItems[1]})");
            }
        }

        string result = sb.ToString();
        return !string.IsNullOrEmpty(result) && result.Length > 2 ? result.Substring(2) : string.Empty;
    }

    private static IEnumerable<CommonDataModel> SystemBoardId(IEnumerable<CommonDataModel> products)
    {
        foreach (CommonDataModel product in products)
        {
            SystemBoardId(product);
        }
        return products;
    }

    private static CommonDataModel SystemBoardId(CommonDataModel product)
    {
        if (!string.IsNullOrWhiteSpace(product.GetValue("SystemboardComments")))
        {
            product.Add("System Board Id", FormatSystemId(product.GetValue("SystemboardComments")));
            product.Delete("SystemboardComments");
        }

        return product;
    }
}
