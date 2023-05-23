using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal class HpAMOPartNumberReader : IKeywordSearchDataReader
{
    private ConnectionStringProvider _csProvider;

    public HpAMOPartNumberReader(KeywordSearchInfo info)
    {
        _csProvider = new(info.Environment);
    }

    public async Task<CommonDataModel> GetDataAsync(int featureId)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        return await GetHpAMOPartNumberAsync();
    }

    private string GetHpAMOPartNumberCommandText()
    {
        return @"
SELECT hppn.AmoHpPartNumberID AS HpAMOPartNumberId,
    hppn.HpPartNo AS HpPartNumber,
    CASE 
        WHEN ISNULL(f.PMG100_AMO, '') <> ''
            THEN hppn.HPPartNo + ' - ' + trim(f.PMG100_AMO + ' ' + ISNULL(r.CountryCode, ''))
        ELSE hppn.HPPartNo + ' - ' + f.PMG100_AMO
        END AS Description,
    bs.Name AS BusinessSegment,
    scm.Name AS ASCMCategory,
    pl.Name AS productLine,
    hppn.RTPDate,
    hppn.SADate,
    hppn.GADate,
    hppn.EMDate,
    hppn.GSEOLDate,
    hppn.ESDate,
    amof.PreviousProduct,
    hppn.Comments,
    amos.Name AS SKUType,
    f.CodeName,
    u1.firstname + ' ' + u1.lastname AS CreatedBy,
    u2.firstname + ' ' + u2.lastname AS LastUpdatedBy
FROM Feature f
right JOIN AmoHpPartNo hppn ON f.FeatureID = hppn.FeatureID
LEFT JOIN Regions r ON hppn.LocalizationId = r.ID
LEFT JOIN SCMCategory scm ON scm.SCMCategoryID = hppn.ASCMCategoryId
LEFT JOIN AMOFeatureV2 amof ON f.featureID = amof.FeatureId
LEFT JOIN BusinessSegment bs ON bs.BusinessSegmentid = amof.BusinessSegmentid
LEFT JOIN ProductLine pl ON pl.id = hppn.productLineId
LEFT JOIN AMOSkuType amos ON amos.SkuTypeId = hppn.SkuTypeId
LEFT JOIN userinfo u1 ON u1.userid = hppn.Creator
LEFT JOIN userinfo u2 ON u2.userid = hppn.Updater
WHERE (
        @HpAMOPartNumberId = - 1
        OR hppn.AmoHpPartNumberID = @HpAMOPartNumberId
        )
";
    }

    private async Task<IEnumerable<CommonDataModel>> GetHpAMOPartNumberAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        SqlCommand command = new(GetHpAMOPartNumberCommandText(), connection);
        SqlParameter parameter = new("HpAMOPartNumberId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();

        while (reader.Read())
        {
            CommonDataModel hpAMOPartNumber = new();
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
                    hpAMOPartNumber.Add(columnName, value);
                }
            }

            hpAMOPartNumber.Add("Target", "HpAmoPartNumber");
            hpAMOPartNumber.Add("Id", SearchIdName.AmoPartNumber + hpAMOPartNumber.GetValue("HpAMOPartNumberId"));
            output.Add(hpAMOPartNumber);
        }

        return output;
    }
}
