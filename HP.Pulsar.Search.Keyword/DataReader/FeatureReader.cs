using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class FeatureReader
{
    private readonly KeywordSearchInfo _info;

    public FeatureReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public async Task<CommonDataModel> GetDataAsync(int featureId)
    {
        CommonDataModel feature = await GetFeaturesAsync(featureId);

        if (!feature.GetElements().Any())
        {
            return null;
        }

        HandlePropertyValue(feature);
        await FillComponentInitiatedLinkageAsync(feature);

        return feature;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> features = await GetFeaturesAsync();

        List<Task> tasks = new()
        {
            FillComponentInitiatedLinkagesAsync(features),
            HandlePropertyValueAsync(features)
        };

        await Task.WhenAll(tasks);

        return features;
    }

    private string GetFeaturesCommandText()
    {
        return @"
SELECT F.FeatureId as 'Feature Id',
    F.FeatureName as 'Feature Name',
    Fc.Name AS 'Feature Category',
    CASE 
        WHEN Fc.FeatureClassID = 1
            THEN 'Documentation'
        WHEN Fc.FeatureClassID = 2
            THEN 'Firmware'
        WHEN Fc.FeatureClassID = 3
            THEN 'Hardware'
        WHEN Fc.FeatureClassID = 4
            THEN 'Software'
        WHEN Fc.FeatureClassID = 5
            THEN 'Base Unit'
        END AS 'Feature Class',
    Dt.Name AS 'Delivery Type',
    F.CodeName as 'Code Name',
    F.RuleID as 'Rule ID',
    F.ChinaGPIdentifier as 'China GP Identifier',
    F.PromoteCode as 'Promote Code',
    F.RequiresRoot as 'Requires a Root', 
    F.Notes,
    Fs.Name AS Status,
    F.CreatedBy as 'Created by',
    F.Created,
    F.UpdatedBy as 'Updated by',
    F.Updated,
    F.overrideReason as 'Previous Reason for Override Request',
    pcc.PRLBaseUnitGroupName AS 'Platform Name',
    o.Name AS 'Linked Operating System'
FROM Feature F
LEFT JOIN FeatureCategory Fc ON Fc.FeatureCategoryID = F.FeatureCategoryID
LEFT JOIN DeliveryType Dt ON Dt.DeliveryTypeID = F.DeliveryTypeID
LEFT JOIN FeatureStatus Fs ON Fs.StatusID = F.StatusID
LEFT JOIN PlatformChassisCategory pcc ON pcc.PlatformID = f.PlatformID
LEFT JOIN OSLookup o ON o.id = F.osid
WHERE (
        @FeatureId = - 1
        OR F.FeatureId = @FeatureId
        )

";
    }

    private async Task<CommonDataModel> GetFeaturesAsync(int featureId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetFeaturesCommandText(), connection);
        SqlParameter parameter = new("FeatureId", featureId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel feature = new();
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

                if (columnName.Equals(TargetName.Feature, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                feature.Add(columnName, value);
            }
            feature.Add("Target", TargetTypeValue.Feature);
            feature.Add("Id", SearchIdName.Feature + feature.GetValue("Feature Id"));
        }
        return feature;
    }

    private async Task<IEnumerable<CommonDataModel>> GetFeaturesAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetFeaturesCommandText(), connection);
        SqlParameter parameter = new("FeatureId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();
        while (await reader.ReadAsync())
        {
            CommonDataModel feature = new();
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

                if (columnName.Equals(TargetName.Feature, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                feature.Add(columnName, value);
            }
            feature.Add("Target", TargetTypeValue.Feature);
            feature.Add("Id", SearchIdName.Feature + feature.GetValue("Feature Id"));
            output.Add(feature);
        }

        return output;
    }

    private string GetComponentInitiatedLinkageCommandText()
    {
        return @"
SELECT fril.FeatureId AS FeatureId,
    dr.ID AS ComponentId,
    dr.Name AS ComponentName
FROM Feature_Root_InitiatedLinkage fril WITH (NOLOCK)
LEFT JOIN DeliverableRoot dr WITH (NOLOCK) ON dr.Id = fril.ComponentRootId
WHERE (
        @FeatureId = - 1
        OR fril.FeatureId = @FeatureId
        )
";
    }

    private async Task FillComponentInitiatedLinkageAsync(CommonDataModel feature)
    {
        if (!feature.GetElements().Any()
            || !int.TryParse(feature.GetValue("FeatureId"), out int featureId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetComponentInitiatedLinkageCommandText(), connection);
        SqlParameter parameter = new("FeatureId", featureId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> componentInitiatedLinkage = new();

        if (!await reader.ReadAsync()
            || !int.TryParse(reader["FeatureId"].ToString(), out int dbFeatureId))
        {
            return;
        }

        if (componentInitiatedLinkage.ContainsKey(dbFeatureId))
        {
            componentInitiatedLinkage[dbFeatureId] = $"{componentInitiatedLinkage[dbFeatureId]} {reader["ComponentId"]} - {reader["ComponentName"]} , ";
        }
        else
        {
            componentInitiatedLinkage[dbFeatureId] = $"{reader["ComponentId"]} - {reader["ComponentName"]} , ";
        }

        if (componentInitiatedLinkage.ContainsKey(featureId))
        {
            string[] componentInitiatedLinkageList = componentInitiatedLinkage[featureId].Split(',');
            for (int i = 0; i < componentInitiatedLinkageList.Length; i++)
            {
                feature.Add("Component Initiated Linkage " + i, componentInitiatedLinkageList[i]);
            }
        }
    }

    private async Task FillComponentInitiatedLinkagesAsync(IEnumerable<CommonDataModel> features)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetComponentInitiatedLinkageCommandText(), connection);
        SqlParameter parameter = new("FeatureId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> componentInitiatedLinkage = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["FeatureId"].ToString(), out int featureId))
            {
                continue;
            }

            if (componentInitiatedLinkage.ContainsKey(featureId))
            {
                componentInitiatedLinkage[featureId] = $"{componentInitiatedLinkage[featureId]} {reader["ComponentId"]} - {reader["ComponentName"]} , ";
            }
            else
            {
                componentInitiatedLinkage[featureId] = $"{reader["ComponentId"]} - {reader["ComponentName"]} , ";
            }
        }

        foreach (CommonDataModel feature in features)
        {
            if (int.TryParse(feature.GetValue("FeatureId"), out int featureId)
                && componentInitiatedLinkage.ContainsKey(featureId))
            {
                string[] componentInitiatedLinkageList = componentInitiatedLinkage[featureId].Split(',');
                for (int i = 0; i < componentInitiatedLinkageList.Length; i++)
                {
                    feature.Add("Component Initiated Linkage " + i, componentInitiatedLinkageList[i]);
                }
            }
        }
    }

    private static CommonDataModel HandlePropertyValue(CommonDataModel feature)
    {
        if (!feature.GetElements().Any())
        {
            return null;
        }

        if (feature.GetValue("Requires a Root").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            feature.Add("Requires a Root", "Requires a Root");
        }
        else
        {
            feature.Delete("Requires a Root");
        }

        return feature;
    }

    private static Task HandlePropertyValueAsync(IEnumerable<CommonDataModel> features)
    {
        if (!features.Any())
        {
            return null;
        }

        foreach (CommonDataModel feature in features)
        {
            if (feature.GetValue("Requires a Root").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                feature.Add("Requires a Root", "Requires a Root");
            }
            else
            {
                feature.Delete("Requires a Root");
            }
        }

        return Task.CompletedTask;
    }
}
