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
        List<Task> tasks = new()
        {
            FillComponentInitiatedLinkageAsync(feature),
            FillBusinessSegmentAsync(feature),
            FillComboFeatureAsync(feature)
        };

        await Task.WhenAll(tasks);

        return feature;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> features = await GetFeaturesAsync();

        List<Task> tasks = new()
        {
            FillComponentInitiatedLinkagesAsync(features),
            FillBusinessSegmentAsync(features),
            HandlePropertyValueAsync(features),
            FillComboFeatureAsync(features)
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
    o.Name AS 'Linked Operating System',
    ns.name AS 'Naming Standard',
    F.GPGPHweb40_NB As 'GPG-PHweb(40c AV) for NB',
    F.GPSy40_NB AS 'GPSy (40c AV) for NB',
    F.PMG100_NB AS 'PMG (100c AV) for NB',
    F.PMG250_NB AS 'PMG (250c AV) for NB',
    F.GPGPHweb40_DT AS 'GPG-PHweb (40c AV) for DT',
    F.GPSy40_DT AS 'GPSy (40c AV) for DT',
    F.PMG100_DT AS 'PMG (100c AV) for DT',
    F.PMG250_DT AS 'PMG (250c AV) for DT',
    F.SpecControl AS 'Marketing Tech Spec Terminology',
    F.FeatureValue AS 'Feature Value (40c AV)',
    F.MS4Attribute AS 'MS4 Attribute',
    F.GPGPHweb40_AMO AS 'GPG-PHweb (40c) for AMO',
    F.GPSy40_AMO AS 'GPSy (40c) for AMO',
    F.PMG100_AMO AS 'PMG (100c) for AMO',
    F.PMG250_AMO AS 'PMG (250c) for AMO'
FROM Feature F
LEFT JOIN FeatureCategory Fc ON Fc.FeatureCategoryID = F.FeatureCategoryID
LEFT JOIN DeliveryType Dt ON Dt.DeliveryTypeID = F.DeliveryTypeID
LEFT JOIN FeatureStatus Fs ON Fs.StatusID = F.StatusID
LEFT JOIN PlatformChassisCategory pcc ON pcc.PlatformID = f.PlatformID
LEFT JOIN OSLookup o ON o.id = F.osid
LEFT JOIN NamingStandard ns on ns.NamingStandardID = F.NamingStandardId
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

    private string GetBusinessSegmentCommandText()
    {
        return @"
select	fbs.FeatureId, bs.Name AS 'Business Segment Name'
from	BusinessSegment bs 
inner join Feature_BusinessSegment fbs on bs.BusinessSegmentID = fbs.BusinessSegmentId
WHERE (
        @FeatureId = - 1
        OR fbs.FeatureId = @FeatureId
        ) 
";
    }

    private string GetComboFeatureCommandText()
    {
        return @"
SELECT Feature_Combo.FeatureID ,
    Feature.FeatureName,
    Feature_Combo.Quantity ,
    Feature_Combo.ParentFeatureID
From Feature_Combo 
inner join Feature on Feature_Combo.FeatureID = Feature.FeatureID 
where (
        @FeatureId = - 1
        OR Feature_Combo.ParentFeatureID = @FeatureId
        ) 
";
    }

    private async Task FillComboFeatureAsync(CommonDataModel feature)
    {
        if (!int.TryParse(feature.GetValue("Feature Id"), out int featureId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetComboFeatureCommandText(), connection);
        SqlParameter parameter = new("FeatureId", featureId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string, string)>> comboFeature = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ParentFeatureID"].ToString(), out int parentFeatureID))
            {
                continue;
            }

            if (comboFeature.ContainsKey(parentFeatureID))
            {
                comboFeature[parentFeatureID].Add((reader["FeatureName"].ToString(),
                                                   reader["FeatureID"].ToString(),
                                                   reader["Quantity"].ToString()));
            }
            else
            {
                comboFeature[parentFeatureID] = new List<(string, string, string)>() { (reader["FeatureName"].ToString(),
                                                                                        reader["FeatureID"].ToString(),
                                                                                        reader["Quantity"].ToString()) };
            }
        }

        if (comboFeature.ContainsKey(featureId))
        {
            for (int i = 0; i < comboFeature[featureId].Count; i++)
            {
                feature.Add("Combo Feature - Feature Name " + i, comboFeature[featureId][i].Item1);
                feature.Add("Combo Feature - Feature ID " + i, comboFeature[featureId][i].Item2);
                feature.Add("Combo Feature - Quantity " + i, comboFeature[featureId][i].Item3);
            }
        }
    }

    private async Task FillComboFeatureAsync(IEnumerable<CommonDataModel> features)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetComboFeatureCommandText(), connection);
        SqlParameter parameter = new("FeatureId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string, string)>> comboFeature = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ParentFeatureID"].ToString(), out int parentFeatureID))
            {
                continue;
            }

            if (comboFeature.ContainsKey(parentFeatureID))
            {
                comboFeature[parentFeatureID].Add((reader["FeatureName"].ToString(),
                                                   reader["FeatureID"].ToString(),
                                                   reader["Quantity"].ToString()));
            }
            else
            {
                comboFeature[parentFeatureID] = new List<(string, string, string)>() { (reader["FeatureName"].ToString(),
                                                                                        reader["FeatureID"].ToString(),
                                                                                        reader["Quantity"].ToString()) };
            }
        }

        foreach (CommonDataModel feature in features)
        {
            if (int.TryParse(feature.GetValue("Feature Id"), out int featureId)
                && comboFeature.ContainsKey(featureId))
            {
                for (int i = 0; i < comboFeature[featureId].Count; i++)
                {
                    feature.Add("Combo Feature - Feature Name " + i, comboFeature[featureId][i].Item1);
                    feature.Add("Combo Feature - Feature ID " + i, comboFeature[featureId][i].Item2);
                    feature.Add("Combo Feature - Quantity " + i, comboFeature[featureId][i].Item3);
                }
            }
        }
    }

    private async Task FillBusinessSegmentAsync(CommonDataModel feature)
    {
        if (!int.TryParse(feature.GetValue("Feature Id"), out int featureId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetBusinessSegmentCommandText(), connection);
        SqlParameter parameter = new("FeatureId", featureId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> businessSegment = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["FeatureId"].ToString(), out int dbFeatureId))
            {
                continue;
            }

            if (businessSegment.ContainsKey(dbFeatureId))
            {
                businessSegment[dbFeatureId].Add(reader["Business Segment Name"].ToString());
            }
            else
            {
                businessSegment[dbFeatureId] = new List<string>() { reader["Business Segment Name"].ToString() };
            }
        }

        if (businessSegment.ContainsKey(featureId))
        {
            for (int i = 0; i < businessSegment[featureId].Count; i++)
            {
                feature.Add("Business Segment Name " + i, businessSegment[featureId][i]);
            }
        }

    }

    private async Task FillBusinessSegmentAsync(IEnumerable<CommonDataModel> features)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetBusinessSegmentCommandText(), connection);
        SqlParameter parameter = new("FeatureId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> businessSegment = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["FeatureId"].ToString(), out int featureId))
            {
                continue;
            }

            if (businessSegment.ContainsKey(featureId))
            {
                businessSegment[featureId].Add(reader["Business Segment Name"].ToString());
            }
            else
            {
                businessSegment[featureId] = new List<string>() { reader["Business Segment Name"].ToString() };
            }
        }

        foreach (CommonDataModel feature in features)
        {
            if (int.TryParse(feature.GetValue("Feature Id"), out int featureId)
                && businessSegment.ContainsKey(featureId))
            {
                for (int i = 0; i < businessSegment[featureId].Count; i++)
                {
                    feature.Add("Business Segment Name " + i, businessSegment[featureId][i]);
                }
            }
        }
    }

    private async Task FillComponentInitiatedLinkageAsync(CommonDataModel feature)
    {
        if (!feature.GetElements().Any()
            || !int.TryParse(feature.GetValue("Feature Id"), out int featureId))
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
            if (int.TryParse(feature.GetValue("Feature Id"), out int featureId)
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
