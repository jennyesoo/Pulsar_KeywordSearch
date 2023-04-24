using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader
{
    public class FeatureReader
    {
        private ConnectionStringProvider _csProvider;

        public FeatureReader(KeywordSearchInfo info)
        {
            _csProvider = new(info.Environment);
        }

        public async Task<CommonDataModel> GetDataAsync(int featureId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
        {
            IEnumerable<CommonDataModel> features = await GetFeaturesAsync();

            List<Task> tasks = new()
            {
                GetComponentInitiatedLinkageAsync(features),
                GetPropertyValueAsync(features)
            };

            await Task.WhenAll(tasks);

            return features;
        }

        private string GetFeaturesCommandText()
        {
            return @"
SELECT F.FeatureId,
    F.FeatureName,
    Fc.Name AS FeatureCategory,
    CASE WHEN Fc.FeatureClassID = 1 THEN 'Documentation'
    WHEN Fc.FeatureClassID = 2 THEN 'Firmware'
    WHEN Fc.FeatureClassID = 3 THEN 'Hardware'
    WHEN Fc.FeatureClassID = 4 THEN 'Software'
    WHEN Fc.FeatureClassID = 5 THEN 'Base Unit'
    END AS FeatureClass,
    Dt.Name AS DeliveryType,
    F.CodeName,
    F.RuleID,
    F.ChinaGPIdentifier,
    F.PromoteCode,
    F.RequiresRoot,
    F.Notes,
    Fs.Name AS STATUS,
    F.CreatedBy,
    F.Created,
    F.UpdatedBy,
    F.Updated
FROM Feature F
JOIN FeatureCategory Fc ON Fc.FeatureCategoryID = F.FeatureCategoryID
JOIN DeliveryType Dt ON Dt.DeliveryTypeID = F.DeliveryTypeID
JOIN FeatureStatus Fs ON Fs.StatusID = F.StatusID
WHERE (
        @FeatureId = - 1
        OR F.FeatureId = @FeatureId
        )
";
        }

        private async Task<IEnumerable<CommonDataModel>> GetFeaturesAsync()
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();

            SqlCommand command = new(GetFeaturesCommandText(), connection);
            SqlParameter parameter = new SqlParameter("FeatureId", "-1");
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            List<CommonDataModel> output = new();
            while (reader.Read())
            {
                CommonDataModel feature = new();
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
                        string value = reader[i].ToString();
                        feature.Add(columnName, value);
                    }
                }
                feature.Add("target", "feature");
                feature.Add("Id", SearchIdName.Feature + feature.GetValue("FeatureId"));
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
JOIN DeliverableRoot dr WITH (NOLOCK) ON dr.Id = fril.ComponentRootId
";
        }

        private async Task GetComponentInitiatedLinkageAsync(IEnumerable<CommonDataModel> features)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();

            SqlCommand command = new(GetComponentInitiatedLinkageCommandText(), connection);
            using SqlDataReader reader = command.ExecuteReader();
            Dictionary<int,string> componentInitiatedLinkage = new();

            while (await reader.ReadAsync())
            {
                if (int.TryParse(reader["FeatureId"].ToString(), out int featureId))
                {
                    if (componentInitiatedLinkage.ContainsKey(featureId))
                    {
                        componentInitiatedLinkage[featureId] = $"{componentInitiatedLinkage[featureId]} {reader["ComponentId"]} - {reader["ComponentName"]} , ";
                    }
                    else
                    {
                        componentInitiatedLinkage[featureId] = $"{reader["ComponentId"]} - {reader["ComponentName"]} , ";
                    }

                }
            }

            foreach (CommonDataModel feature in features)
            {
                if (int.TryParse(feature.GetValue("FeatureId"), out int featureId)
                    && componentInitiatedLinkage.ContainsKey(featureId))
                {
                    feature.Add("ComponentInitiatedLinkage", componentInitiatedLinkage[featureId]);
                }
            }
        }

        private async Task GetPropertyValueAsync(IEnumerable<CommonDataModel> features)
        {
            foreach (CommonDataModel feature in features)
            {
                if (feature.GetValue("RequiresRoot").Equals("True"))
                {
                    feature.Add("RequiresRoot", "Truly Linked");
                }
                else
                {
                    feature.Delete("RequiresRoot");
                }
            }
        }

    }
}
