using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal class ProductDropReader : IKeywordSearchDataReader
{
    private ConnectionStringProvider _csProvider;

    public ProductDropReader(KeywordSearchInfo info)
    {
        _csProvider = new(info.Environment);
    }

    public async Task<CommonDataModel> GetDataAsync(int productDropId)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> productDrop = await GetProductDropAsync();

        List<Task> tasks = new()
        {
            GetOwnerbyAsync(productDrop),
            GetMlNameAsync(productDrop),
            GetPropertyValueAsync(productDrop)
        };

        await Task.WhenAll(tasks);
        return productDrop;
    }

    private string GetProductDropCommandText()
    {
        return @"
SELECT pd.ProductDropId,
    ps.Name AS Status,
    pd.Creator AS CreatedBy,
    pd.Updater AS LastUpdatedBy,
    pd.TimeCreated AS CreatedDate,
    pd.TimeChanged AS UpdatedDate,
    pd.name AS ProductDropName,
    pd.Description,
    pd.ScheduledDate AS UpcomingRTMDate,
    CASE 
        WHEN pd.STATE & 8 > 0
            THEN 1
        ELSE 0
        END AS Locked,
    pd.HideInTree,
    pd.AllowSameRootDeliverables,
    pd.AllowPDAutoSelected,
    pd.AllowSWCompToSI,
    pd.ODMViewOnly,
    (SELECT min(prel.ReleaseYear) 
        FROM Productversion pv 
        LEFT  JOIN productversion_Release pvr WITH (NOLOCK) ON pv.id = pvr.ProductversionID 
        LEFT  JOIN productversionRelease prel ON pvr.ReleaseID = prel.ID 
        WHERE pv.id IN ( 
                SELECT pvr2.ProductversionID 
                FROM productversion_Release pvr2 WITH (NOLOCK) 
                LEFT  JOIN Productdrop pd WITH (NOLOCK) ON pvr2.id = pd.ProductversionReleaseID 
                WHERE pd.ProductDropID = pd.ProductDropID  --PDid
                ) ) as ReleaseYear,
        pd.ProductDropID,
        Pv.ProductName,
        bs.Name as BusinessSegmentname,
        PF.Name as ProductFamily,
        CASE WHEN bs.BusinessId = 1 
        THEN '' 
        ELSE rtrim(isnull(pvr.Name, ''))
        END as ReleaseName
FROM ProductDrop AS pd
LEFT JOIN ProductdropStatus ps ON ps.statusid = pd.StatusID
LEFT JOIN ProductVersion_Release P WITH (NOLOCK) ON  P.ID = pd.ProductversionReleaseID 
LEFT Join Productversion pv ON Pv.ID = P.ProductVersionID
LEFT Join BusinessSegment bs ON pv.BusinessSegmentID = bs.BusinessSegmentID 
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
GROUP BY ml2.ProductDropId
";
    }

    private async Task<IEnumerable<CommonDataModel>> GetProductDropAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
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
                if (!string.IsNullOrWhiteSpace(reader[i].ToString()))
                {
                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString().Trim();
                    productDrop.Add(columnName, value.Trim());
                }
            }
            productDrop.Add("Target", "ProductDrop");
            productDrop.Add("Id", SearchIdName.ProductDrop + productDrop.GetValue("ProductDropId"));
            output.Add(productDrop);
        }
        return output;
    }

    private async Task GetOwnerbyAsync(IEnumerable<CommonDataModel> productDrop)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        SqlCommand command = new(GetOwnedByCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> ownerBy = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropID"].ToString(), out int productDropID)
                && !string.IsNullOrWhiteSpace(reader["Ownerby"].ToString()))
            {
                ownerBy[productDropID] = reader["Ownerby"].ToString();
            }
        }

        foreach (CommonDataModel pd in productDrop)
        {
            if (int.TryParse(pd.GetValue("ProductDropID"), out int productDropID)
            && ownerBy.ContainsKey(productDropID))
            {
                string[] ownerByList = ownerBy[productDropID].Split(',');
                for (int i = 0; i < ownerByList.Length; i++)
                {
                    pd.Add("OwnerBy " + i, ownerByList[i]);
                }
            }
        }
    }

    private async Task GetMlNameAsync(IEnumerable<CommonDataModel> productDrop)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        SqlCommand command = new(GetMlNameCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> mlName = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ProductDropID"].ToString(), out int productDropID)
                && !string.IsNullOrWhiteSpace(reader["MLName"].ToString()))
            {
                mlName[productDropID] = reader["MLName"].ToString();
            }
        }

        foreach (CommonDataModel pd in productDrop)
        {
            if (int.TryParse(pd.GetValue("ProductDropID"), out int productDropID)
            && mlName.ContainsKey(productDropID))
            {
                string[] mlNameList = mlName[productDropID].Split(',');
                for (int i = 0; i < mlNameList.Length; i++)
                {
                    pd.Add("MLName " + i, mlNameList[i]);
                }
            }
        }
    }

    private async Task GetPropertyValueAsync(IEnumerable<CommonDataModel> productDrop)
    {
        foreach (CommonDataModel pd in productDrop)
        {
            if (pd.GetValue("Locked").Equals("1"))
            {
                pd.Add("Locked", "Locked");
            }
            else
            {
                pd.Delete("Locked");
            }

            if (pd.GetValue("HideInTree").Equals("True"))
            {
                pd.Add("HideInTree", "Hide In Tree");
            }
            else
            {
                pd.Delete("HideInTree");
            }

            if (pd.GetValue("AllowSameRootDeliverables").Equals("True"))
            {
                pd.Add("AllowSameRootDeliverables", "Allow to add components with the same parent part number");
            }
            else
            {
                pd.Delete("AllowSameRootDeliverables");
            }

            if (pd.GetValue("AllowPDAutoSelected").Equals("True"))
            {
                pd.Add("AllowPDAutoSelected", "Component Replacement Product Drop auto selected");
            }
            else
            {
                pd.Delete("AllowPDAutoSelected");
            }

            if (pd.GetValue("AllowSWcompToSI").Equals("True"))
            {
                pd.Add("AllowSWcompToSI", "Feed SW Componenets to SI");
            }
            else
            {
                pd.Delete("AllowSWcompToSI");
            }
            if (pd.GetValue("ODMViewOnly").Equals("True"))
            {
                pd.Add("ODMViewOnly", "ODM R&D Sites can only View");
            }
            else
            {
                pd.Delete("ODMViewOnly");
            }
        }
    }
}
