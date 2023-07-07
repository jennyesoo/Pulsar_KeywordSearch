using System.Xml.Linq;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal class ProductDropReader : IKeywordSearchDataReader
{
    private readonly KeywordSearchInfo _info;

    public ProductDropReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public async Task<CommonDataModel> GetDataAsync(int productDropId)
    {
        CommonDataModel productDrop = await GetProductDropAsync(productDropId);

        if (!productDrop.GetElements().Any())
        {
            return null;
        }

        HandlePropertyValue(productDrop);
        FillProductPath(productDrop);

        List<Task> tasks = new()
        {
            FillOwnerbyAsync(productDrop),
            FillMlNameAsync(productDrop),
            FillSystemBoardsAsync(productDrop),
            FillODMPartnerAsync(productDrop)
        };

        await Task.WhenAll(tasks);

        return productDrop;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> productDrop = await GetProductDropAsync();

        FillProductPath(productDrop);

        List<Task> tasks = new()
        {
            FillOwnerbysAsync(productDrop),
            FillMlNamesAsync(productDrop),
            HandlePropertyValueAsync(productDrop),
            FillSystemBoardsAsync(productDrop),
            FillODMPartnerAsync(productDrop)
        };

        await Task.WhenAll(tasks);

        return productDrop;
    }

    private string GetProductDropCommandText()
    {
        return @"
SELECT pd.ProductDropId as 'Product Drop Id',
    ps.Name AS Status,
    pd.Creator AS 'Created by',
    pd.Updater AS 'Last Updated by',
    pd.TimeCreated AS 'Created Date',
    pd.TimeChanged AS 'Updated Date',
    pd.name AS 'Product Drop Name',
    pd.Description,
    pd.ScheduledDate AS 'Upcoming RTM Date',
    CASE 
        WHEN pd.STATE & 8 > 0
            THEN 1
        ELSE 0
        END AS Locked,
    pd.HideInTree as 'Hide In Tree',
    pd.AllowSameRootDeliverables as 'Allow to add components with the same parent part number',
    pd.AllowPDAutoSelected as 'Component Replacement Product Drop auto selected',
    pd.AllowSWCompToSI as 'Feed SW Components to SI',
    pd.ODMViewOnly as 'ODM R&D Sites can only View',
    (
        SELECT min(prel.ReleaseYear)
        FROM Productversion pv
        LEFT JOIN productversion_Release pvr WITH (NOLOCK) ON pv.id = pvr.ProductversionID
        LEFT JOIN productversionRelease prel ON pvr.ReleaseID = prel.ID
        WHERE pv.id IN (
                SELECT pvr2.ProductversionID
                FROM productversion_Release pvr2 WITH (NOLOCK)
                LEFT JOIN Productdrop pd2 WITH (NOLOCK) ON pvr2.id = pd2.ProductversionReleaseID
                WHERE pd2.ProductDropID = pd.ProductDropID --PDid
                )
        ) AS 'Release Year',
    Pv.ProductName as 'Product Name',
    bs.Name AS 'Business Segment Name',
    PF.Name AS 'Product Family',
    CASE 
        WHEN bs.BusinessId = 1
            THEN ''
        ELSE rtrim(isnull(pvr.Name, ''))
        END AS 'Release Name',
    rtrim(isnull(bs.Name, '')) + ' / ' + 
        '***' + ' / ' +
            rtrim(isnull(PF.Name, '')) + ' - ' + 
            rtrim(isnull(Pv.ProductName, '')) +  
            CASE WHEN bs.BusinessId = 1 
                THEN '' 
                ELSE ' (' + rtrim(isnull(pvr.Name, '')) + ')'  
                END +
            ' / ' + rtrim(isnull(PD.Name, '')) AS 'Product Path'
FROM ProductDrop AS pd
LEFT JOIN ProductdropStatus ps ON ps.statusid = pd.StatusID
LEFT JOIN ProductVersion_Release P WITH (NOLOCK) ON P.ID = pd.ProductversionReleaseID
LEFT JOIN Productversion pv ON Pv.ID = P.ProductVersionID
LEFT JOIN BusinessSegment bs ON pv.BusinessSegmentID = bs.BusinessSegmentID
LEFT JOIN ProductFamily PF ON Pv.ProductFamilyID = PF.ID
LEFT JOIN ProductVersionRelease pvr WITH (NOLOCK) ON p.ReleaseID = pvr.ID
WHERE (
        @ProductDropId = - 1
        OR pd.ProductDropId = @ProductDropId
        )
";
    }

    private string GetOwnedByCommandText()
    {
        return @"
SELECT p.ProductDropId,
    stuff((
            SELECT ', ' + UG.Name
            FROM ProductDrop_Team PU,
                Team UG
            WHERE PU.TeamID = UG.TeamID
                AND PU.ProductDropID = p.ProductDropID
            FOR XML path('')
            ), 1, 2, '') AS Ownerby
FROM ProductDrop_Team p
WHERE (
        @ProductDropId = - 1
        OR p.ProductDropId = @ProductDropId
        )
GROUP BY p.ProductDropID
";
    }

    private string GetMlNameCommandText()
    {
        return @"
SELECT ml2.ProductDropId,
    stuff((
            SELECT ', ' + ml.Name
            FROM ML_INI ml
            WHERE ml.ProductDropId = ml2.ProductDropId
                AND ml.Name != ''
            FOR XML path('')
            ), 1, 2, '') AS MLName
FROM ML_INI ml2
WHERE (
        @ProductDropId = - 1
        OR ml2.ProductDropId = @ProductDropId
        )
GROUP BY ml2.ProductDropId
";
    }

    private string GetProductIDAndProductReleaseIDCommandText()
    {
        return @"
select pd.ProductdropID,
    productversion.ID AS 'ProductId',
    pd.ProductversionReleaseID AS 'ProductReleaseId'
from productversion with (Nolock) 
inner join ProductVersion_Release with (Nolock) on productversion.ID = ProductVersion_Release.ProductVersionID 
inner join productdrop pd with (nolock) on ProductVersion_Release.ID=pd.ProductversionReleaseID 
WHERE (
        @ProductDropId = - 1
        OR pd.ProductdropID = @ProductDropId
        )
";
    }

    private string GetProductReleaseIDToProductIDCommandText()
    {
        return @"
select productversionID AS 'ProductId',
    ProductversionReleaseID AS 'ProductReleaseId' 
from ProductExplorer_CombinedProducts 
";
    }

    private string GetSystenBoardsCommandText()
    {
        return @"
select distinct  
    pf.PlatformID, 
    pf.ProductFamily AS 'System Boards - Product Family', 
    pf.PCA AS 'System Boards - System Board', 
    pf.SystemID AS 'System Boards - System Board ID', 
    rtrim(pf.MktNameMaster) AS 'System Boards - Marketing Name', 
    PlatformChassisCategory.Chassis AS 'System Boards - Chassis', 
    isnull(convert(varchar(10), pf.softpaqNeedDate, 101), '') AS 'System Boards - Softpaq Need Date',
    isnull(PM.PRODUCT_NAME_NAME, '') AS 'System Boards - SOAR Product Description' ,
    pvp.ProductVersionID AS 'ProductId'
from 	Platform pf with (nolock)  
inner join PlatformChassisCategory on pf.PlatformID = PlatformChassisCategory.PlatformID 
inner join ProductVersion_Platform  pvp  on pf.PlatformID = pvp.PlatformID 
left outer join ProductDrop_Platform pp on pf.PlatformID = pp.PlatformID 
LEFT OUTER JOIN (PMaster_Product PM INNER JOIN 
                SOAR_Mapping SOAR ON PM.PRODUCT_NAME_OID = SOAR.OID and SOAR.ObjectTypeID=209) ON pf.PlatformID = SOAR.ObjectID  
where (case when pp.PlatformID is null then 0 else 1 end) != 0
    AND (
        @ProductId = - 1
        OR pvp.ProductVersionID = @ProductId
        )
";
    }

    private string GetODMPartnerCommandText()
    {
        return @"
select	productdrop_Partner.ProductDropId, 
        Partner.Name AS 'ODM R&D Sites that can see this Product Drop'
from	Partner with (nolock) 
inner join productdrop_Partner with (nolock) on Partner.PartnerId = productdrop_Partner.PartnerID
WHERE (
        @ProductDropId = - 1
        OR productdrop_Partner.ProductDropId = @ProductDropId
        )
";
    }

    private async Task FillODMPartnerAsync(CommonDataModel productDrop)
    {
        if (!int.TryParse(productDrop.GetValue("Product Drop Id"), out int productDropID))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetODMPartnerCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", productDropID);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> odmPartner = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropId"].ToString(), out int dbProductDropID)
                && !string.IsNullOrWhiteSpace(reader["ODM R&D Sites that can see this Product Drop"].ToString()))
            {
                if (!odmPartner.ContainsKey(dbProductDropID))
                {
                    odmPartner[dbProductDropID] = new List<string> { reader["ODM R&D Sites that can see this Product Drop"].ToString() };
                }
                else
                {
                    odmPartner[dbProductDropID].Add(reader["ODM R&D Sites that can see this Product Drop"].ToString());
                }
            }
        }

        if (odmPartner.ContainsKey(productDropID))
        {
            for (int i = 0; i < odmPartner[productDropID].Count; i++)
            {
                productDrop.Add("ODM R&D Sites that can see this Product Drop " + i, odmPartner[productDropID][i]);
            }
        }
    }

    private async Task FillODMPartnerAsync(IEnumerable<CommonDataModel> productDrop)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetODMPartnerCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> odmPartner = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropId"].ToString(), out int productDropID)
                && !string.IsNullOrWhiteSpace(reader["ODM R&D Sites that can see this Product Drop"].ToString()))
            {
                if (!odmPartner.ContainsKey(productDropID))
                {
                    odmPartner[productDropID] = new List<string> { reader["ODM R&D Sites that can see this Product Drop"].ToString() };
                }
                else
                {
                    odmPartner[productDropID].Add(reader["ODM R&D Sites that can see this Product Drop"].ToString());
                }
            }
        }

        foreach (CommonDataModel pd in productDrop)
        {
            if (int.TryParse(pd.GetValue("Product Drop Id"), out int productDropID)
            && odmPartner.ContainsKey(productDropID))
            {
                for (int i = 0; i < odmPartner[productDropID].Count; i++)
                {
                    pd.Add("ODM R&D Sites that can see this Product Drop " + i, odmPartner[productDropID][i]);
                }
            }
        }
    }

    private async Task<Dictionary<int, List<int>>> GetProductReleaseIDToProductIDAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductReleaseIDToProductIDCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();

        Dictionary<int, List<int>> productReleaseIDToProductID = new Dictionary<int, List<int>>();

        while (reader.Read())
        {
            if (!int.TryParse(reader["ProductReleaseId"].ToString(), out int productReleaseId))
            {
                continue;
            }

            if (int.TryParse(reader["ProductId"].ToString(), out int productId)
                && !productReleaseIDToProductID.ContainsKey(productReleaseId))
            {
                if (!productReleaseIDToProductID.ContainsKey(productReleaseId))
                {
                    productReleaseIDToProductID[productReleaseId] = new List<int>() { productId };
                }
                else
                {
                    productReleaseIDToProductID[productReleaseId].Add(productId);
                }
            }
        }

        return productReleaseIDToProductID;
    }

    private async Task<Dictionary<int, List<int>>> GetProductDropToProductIDProductReleaseIDAsync(int productDropId)
    {
        Dictionary<int, List<int>> productReleaseIDToProductID = await GetProductReleaseIDToProductIDAsync();

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductIDAndProductReleaseIDCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", productDropId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        Dictionary<int, List<int>> productDropToProductID = new Dictionary<int, List<int>>();

        while (reader.Read())
        {
            if (int.TryParse(reader["ProductdropID"].ToString(), out int dbProductDropId)
                || int.TryParse(reader["ProductId"].ToString(), out int productId)
                || int.TryParse(reader["ProductReleaseId"].ToString(), out int productReleaseId))
            {
                continue;
            }

            if (productDropToProductID.ContainsKey(dbProductDropId))
            {
                productDropToProductID[dbProductDropId].Add(productId);
            }
            else
            {
                productDropToProductID[dbProductDropId] = new List<int>() { productId };
            }

            if (!productReleaseIDToProductID.ContainsKey(productReleaseId))
            {
                continue;
            }

            foreach (int item in productReleaseIDToProductID[productReleaseId])
            {
                if (!productDropToProductID[dbProductDropId].Contains(item))
                {
                    productDropToProductID[dbProductDropId].Add(item);
                }
            }
        }

        return productDropToProductID;
    }

    private async Task<Dictionary<int, List<int>>> GetProductDropToProductIDProductReleaseIDAsync()
    {
        Dictionary<int, List<int>> productReleaseIDToProductID = await GetProductReleaseIDToProductIDAsync();

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductIDAndProductReleaseIDCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        Dictionary<int, List<int>> productDropToProductID = new Dictionary<int, List<int>>();

        while (reader.Read())
        {
            if (!int.TryParse(reader["ProductdropID"].ToString(), out int productDropId)
                || !int.TryParse(reader["ProductId"].ToString(), out int productId)
                || !int.TryParse(reader["ProductReleaseId"].ToString(), out int productReleaseId))
            {
                continue;
            }

            if (productDropToProductID.ContainsKey(productDropId))
            {
                productDropToProductID[productDropId].Add(productId);
            }
            else
            {
                productDropToProductID[productDropId] = new List<int>() { productId };
            }

            if (!productReleaseIDToProductID.ContainsKey(productReleaseId))
            {
                continue;
            }

            foreach (int item in productReleaseIDToProductID[productReleaseId])
            {
                if (!productDropToProductID[productDropId].Contains(item))
                {
                    productDropToProductID[productDropId].Add(item);
                }
            }
        }

        return productDropToProductID;
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetSystemBoardsAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetSystenBoardsCommandText(), connection);
        SqlParameter parameter = new("ProductId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        Dictionary<int, List<CommonDataModel>> systemBoards = new Dictionary<int, List<CommonDataModel>>();

        while (reader.Read())
        {
            if (!int.TryParse(reader["ProductId"].ToString(), out int productID))
            {
                continue;
            }

            CommonDataModel item = new();
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

                item.Add(columnName, value);
            }

            if (systemBoards.ContainsKey(productID))
            {
                systemBoards[productID].Add(item);
            }
            else
            {
                systemBoards[productID] = new List<CommonDataModel>() { item };
            }
        }

        return systemBoards;
    }

    private async Task<Dictionary<int, List<CommonDataModel>>> GetSystemBoardsAsync(int productId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetSystenBoardsCommandText(), connection);
        SqlParameter parameter = new("ProductId", productId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        Dictionary<int, List<CommonDataModel>> systemBoards = new Dictionary<int, List<CommonDataModel>>();

        if (reader.Read())
        {
            if (!int.TryParse(reader["ProductID"].ToString(), out int productID))
            {
                return systemBoards;
            }

            CommonDataModel item = new();
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

                item.Add(columnName, value);
            }

            if (systemBoards.ContainsKey(productID))
            {
                systemBoards[productID].Add(item);
            }
            else
            {
                systemBoards[productID] = new List<CommonDataModel>() { item };
            }
        }

        return systemBoards;
    }

    private async Task FillSystemBoardsAsync(CommonDataModel productDrop)
    {
        if (!int.TryParse(productDrop.GetValue("Product Drop Id"), out int productDropId))
        {
            return;
        }

        Dictionary<int, List<int>> productDropToProductID = await GetProductDropToProductIDProductReleaseIDAsync(productDropId);

        if (!productDropToProductID.ContainsKey(productDropId))
        {
            return;
        }

        for (int i = 0; i < productDropToProductID[productDropId].Count; i++)
        {
            Dictionary<int, List<CommonDataModel>> systemBoards = await GetSystemBoardsAsync(productDropToProductID[productDropId][i]);

            if (!systemBoards.ContainsKey(productDropToProductID[productDropId][i]))
            {
                continue;
            }

            int num = 0;

            foreach (CommonDataModel item in systemBoards[productDropToProductID[productDropId][i]])
            {
                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Product Family")))
                {
                    productDrop.Add("System Boards - Product Family " + i + '-' + num, item.GetValue("System Boards - Product Family"));
                }

                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - System Board")))
                {
                    productDrop.Add("System Boards - System Board " + i + '-' + num, item.GetValue("System Boards - System Board"));
                }

                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - System Board ID")))
                {
                    productDrop.Add("System Boards - System Board ID " + i + '-' + num, item.GetValue("System Boards - System Board ID"));
                }

                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Marketing Name")))
                {
                    productDrop.Add("System Boards - Marketing Name " + i + '-' + num, item.GetValue("System Boards - Marketing Name"));
                }

                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Chassis")))
                {
                    productDrop.Add("System Boards - Chassis " + i + '-' + num, item.GetValue("System Boards - Chassis"));
                }

                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Softpaq Need Date")))
                {
                    productDrop.Add("System Boards - Softpaq Need Date " + i + '-' + num, item.GetValue("System Boards - Softpaq Need Date"));
                }

                if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - SOAR Product Description")))
                {
                    productDrop.Add("System Boards - SOAR Product Description " + i + '-' + num, item.GetValue("System Boards - SOAR Product Description"));
                }

                num++;
            }
        }
    }

    private async Task FillSystemBoardsAsync(IEnumerable<CommonDataModel> productDrop)
    {
        Dictionary<int, List<int>> productDropToProductID = await GetProductDropToProductIDProductReleaseIDAsync();
        Dictionary<int, List<CommonDataModel>> systemBoards = await GetSystemBoardsAsync();

        foreach (CommonDataModel pd in productDrop)
        {
            if (!int.TryParse(pd.GetValue("Product Drop Id"), out int productDropId)
                || !productDropToProductID.ContainsKey(productDropId))
            {
                continue;
            }

            for (int i = 0; i < productDropToProductID[productDropId].Count; i++)
            {
                if (!systemBoards.ContainsKey(productDropToProductID[productDropId][i]))
                {
                    continue;
                }

                int num = 0;

                foreach (CommonDataModel item in systemBoards[productDropToProductID[productDropId][i]])
                {
                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Product Family")))
                    {
                        pd.Add("System Boards - Product Family " + i + '-' + num, item.GetValue("System Boards - Product Family"));
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - System Board")))
                    {
                        pd.Add("System Boards - System Board " + i + '-' + num, item.GetValue("System Boards - System Board"));
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - System Board ID")))
                    {
                        pd.Add("System Boards - System Board ID " + i + '-' + num, item.GetValue("System Boards - System Board ID"));
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Marketing Name")))
                    {
                        pd.Add("System Boards - Marketing Name " + i + '-' + num, item.GetValue("System Boards - Marketing Name"));
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Chassis")))
                    {
                        pd.Add("System Boards - Chassis " + i + '-' + num, item.GetValue("System Boards - Chassis"));
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - Softpaq Need Date")))
                    {
                        pd.Add("System Boards - Softpaq Need Date " + i + '-' + num, item.GetValue("System Boards - Softpaq Need Date"));
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetValue("System Boards - SOAR Product Description")))
                    {
                        pd.Add("System Boards - SOAR Product Description " + i + '-' + num, item.GetValue("System Boards - SOAR Product Description"));
                    }

                    num++;
                }
            }
        }
    }

    private static CommonDataModel FillProductPath(CommonDataModel productDrop)
    {
        if (!string.IsNullOrWhiteSpace(productDrop.GetValue("Product Path"))
            && !string.IsNullOrWhiteSpace(productDrop.GetValue("Release Year")))
        {
            productDrop.Add("Product Path", productDrop.GetValue("Product Path").Replace("***", productDrop.GetValue("Release Year")));
        }

        return productDrop;
    }

    private static IEnumerable<CommonDataModel> FillProductPath(IEnumerable<CommonDataModel> productDrop)
    {
        foreach (CommonDataModel pd in productDrop)
        {
            if (!string.IsNullOrWhiteSpace(pd.GetValue("Product Path"))
                && !string.IsNullOrWhiteSpace(pd.GetValue("Release Year")))
            {
                pd.Add("Product Path", pd.GetValue("Product Path").Replace("***", pd.GetValue("Release Year")));
            }
        }

        return productDrop;
    }

    private async Task<CommonDataModel> GetProductDropAsync(int productDropId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductDropCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", productDropId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel productDrop = new();
        if (reader.Read())
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

                if (columnName.Equals(TargetName.ProductDrop, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                productDrop.Add(columnName, value);
            }

            productDrop.Add("Target", TargetTypeValue.ProductDrop);
            productDrop.Add("Id", SearchIdName.ProductDrop + productDrop.GetValue("Product Drop Id"));
        }

        return productDrop;
    }

    private async Task<IEnumerable<CommonDataModel>> GetProductDropAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetProductDropCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();
        while (reader.Read())
        {
            CommonDataModel productDrop = new();
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

                if (columnName.Equals(TargetName.ProductDrop, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                productDrop.Add(columnName, value);
            }

            productDrop.Add("Target", TargetTypeValue.ProductDrop);
            productDrop.Add("Id", SearchIdName.ProductDrop + productDrop.GetValue("Product Drop Id"));
            output.Add(productDrop);
        }

        return output;
    }

    private async Task FillOwnerbyAsync(CommonDataModel productDrop)
    {
        if (!int.TryParse(productDrop.GetValue("Product Drop Id"), out int productDropId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetOwnedByCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", productDropId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> ownerBy = new();

        if (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropId"].ToString(), out int dbProductDropId)
                && !string.IsNullOrWhiteSpace(reader["Ownerby"].ToString()))
            {
                ownerBy[dbProductDropId] = reader["Ownerby"].ToString();
            }
        }

        if (ownerBy.ContainsKey(productDropId))
        {
            string[] ownerByList = ownerBy[productDropId].Split(',');
            for (int i = 0; i < ownerByList.Length; i++)
            {
                productDrop.Add("Owner by " + i, ownerByList[i]);
            }
        }
    }

    private async Task FillOwnerbysAsync(IEnumerable<CommonDataModel> productDrop)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetOwnedByCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> ownerBy = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropId"].ToString(), out int productDropID)
                && !string.IsNullOrWhiteSpace(reader["Ownerby"].ToString()))
            {
                ownerBy[productDropID] = reader["Ownerby"].ToString();
            }
        }

        foreach (CommonDataModel pd in productDrop)
        {
            if (int.TryParse(pd.GetValue("Product Drop Id"), out int productDropID)
            && ownerBy.ContainsKey(productDropID))
            {
                string[] ownerByList = ownerBy[productDropID].Split(',');
                for (int i = 0; i < ownerByList.Length; i++)
                {
                    pd.Add("Owner by " + i, ownerByList[i]);
                }
            }
        }
    }

    private async Task FillMlNameAsync(CommonDataModel productDrop)
    {
        if (!int.TryParse(productDrop.GetValue("ProductDropId"), out int productDropId))
        {
            return;
        }
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetMlNameCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", productDropId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> mlName = new();

        if (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropId"].ToString(), out int dbProductDropId)
                && !string.IsNullOrWhiteSpace(reader["MLName"].ToString()))
            {
                mlName[dbProductDropId] = reader["MLName"].ToString();
            }
        }

        if (mlName.ContainsKey(productDropId))
        {
            string[] mlNameList = mlName[productDropId].Split(',');
            for (int i = 0; i < mlNameList.Length; i++)
            {
                productDrop.Add("ML Name " + i, mlNameList[i]);
            }
        }
    }

    private async Task FillMlNamesAsync(IEnumerable<CommonDataModel> productDrop)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetMlNameCommandText(), connection);
        SqlParameter parameter = new("ProductDropId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> mlName = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropId"].ToString(), out int productDropID)
                && !string.IsNullOrWhiteSpace(reader["MLName"].ToString()))
            {
                mlName[productDropID] = reader["MLName"].ToString();
            }
        }

        foreach (CommonDataModel pd in productDrop)
        {
            if (int.TryParse(pd.GetValue("Product Drop Id"), out int productDropID)
            && mlName.ContainsKey(productDropID))
            {
                string[] mlNameList = mlName[productDropID].Split(',');
                for (int i = 0; i < mlNameList.Length; i++)
                {
                    pd.Add("ML Name " + i, mlNameList[i]);
                }
            }
        }
    }

    private static CommonDataModel HandlePropertyValue(CommonDataModel productDrop)
    {
        if (productDrop.GetValue("Locked").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("Locked", "Locked");
        }
        else
        {
            productDrop.Delete("Locked");
        }

        if (productDrop.GetValue("Hide In Tree").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("Hide In Tree", "Hide In Tree");
        }
        else
        {
            productDrop.Delete("Hide In Tree");
        }

        if (productDrop.GetValue("Allow to add components with the same parent part number").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("Allow to add components with the same parent part number", "Allow to add components with the same parent part number");
        }
        else
        {
            productDrop.Delete("Allow to add components with the same parent part number");
        }

        if (productDrop.GetValue("Component Replacement Product Drop auto selected").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("Component Replacement Product Drop auto selected", "Component Replacement Product Drop auto selected");
        }
        else
        {
            productDrop.Delete("Component Replacement Product Drop auto selected");
        }

        if (productDrop.GetValue("Feed SW Components to SI").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("Feed SW Components to SI", "Feed SW Components to SI");
        }
        else
        {
            productDrop.Delete("Feed SW Components to SI");
        }

        if (productDrop.GetValue("ODM R&D Sites can only View").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("ODM R&D Sites can only View", "ODM R&D Sites can only View");
        }
        else
        {
            productDrop.Delete("ODM R&D Sites can only View");
        }
        return productDrop;
    }


    private static Task HandlePropertyValueAsync(IEnumerable<CommonDataModel> productDrop)
    {
        foreach (CommonDataModel pd in productDrop)
        {
            if (pd.GetValue("Locked").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("Locked", "Locked");
            }
            else
            {
                pd.Delete("Locked");
            }

            if (pd.GetValue("Hide In Tree").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("Hide In Tree", "Hide In Tree");
            }
            else
            {
                pd.Delete("Hide In Tree");
            }

            if (pd.GetValue("Allow to add components with the same parent part number").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("Allow to add components with the same parent part number", "Allow to add components with the same parent part number");
            }
            else
            {
                pd.Delete("Allow to add components with the same parent part number");
            }

            if (pd.GetValue("Component Replacement Product Drop auto selected").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("Component Replacement Product Drop auto selected", "Component Replacement Product Drop auto selected");
            }
            else
            {
                pd.Delete("Component Replacement Product Drop auto selected");
            }

            if (pd.GetValue("Feed SW Components to SI").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("Feed SW Components to SI", "Feed SW Components to SI");
            }
            else
            {
                pd.Delete("Feed SW Components to SI");
            }
            if (pd.GetValue("ODM R&D Sites can only View").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("ODM R&D Sites can only View", "ODM R&D Sites can only View");
            }
            else
            {
                pd.Delete("ODM R&D Sites can only View");
            }
        }

        return Task.CompletedTask;
    }
}
