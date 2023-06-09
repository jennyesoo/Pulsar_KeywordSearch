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
        
        List<Task> tasks = new()
        {
            FillOwnerbyAsync(productDrop),
            FillMlNameAsync(productDrop)
        };

        await Task.WhenAll(tasks);

        return productDrop;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> productDrop = await GetProductDropAsync();

        List<Task> tasks = new()
        {
            FillOwnerbysAsync(productDrop),
            FillMlNamesAsync(productDrop),
            HandlePropertyValueAsync(productDrop)
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
        END AS 'Release Name'
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

        if (productDrop.GetValue("Feed SW Componenets to SI").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            productDrop.Add("Feed SW Componenets to SI", "Feed SW Componenets to SI");
        }
        else
        {
            productDrop.Delete("Feed SW Componenets to SI");
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

            if (pd.GetValue("Feed SW Componenets to SI").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                pd.Add("Feed SW Componenets to SI", "Feed SW Componenets to SI");
            }
            else
            {
                pd.Delete("Feed SW Componenets to SI");
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
