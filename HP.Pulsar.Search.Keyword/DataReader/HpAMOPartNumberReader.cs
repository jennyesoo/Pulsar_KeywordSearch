using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal class HpAMOPartNumberReader : IKeywordSearchDataReader
{
    private readonly KeywordSearchInfo _info;

    public HpAMOPartNumberReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public async Task<CommonDataModel> GetDataAsync(int hpAMOPartNumberId)
    {
        CommonDataModel hpAMOPartNumber = await GetHpAMOPartNumberAsync(hpAMOPartNumberId);
        
        if (!hpAMOPartNumber.GetElements().Any())
        {
            return null;
        }

        return hpAMOPartNumber;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        return await GetHpAMOPartNumberAsync();
    }

    private string GetHpAMOPartNumberCommandText()
    {
        return @"SELECT hppn.AmoHpPartNumberID AS 'Hp AMO Part Number Id',
    hppn.HpPartNo AS 'Hp Part Number',
    CASE 
        WHEN ISNULL(f.PMG100_AMO, '') <> ''
            THEN hppn.HPPartNo + ' - ' + trim(f.PMG100_AMO + ' ' + ISNULL(r.CountryCode, ''))
        ELSE hppn.HPPartNo + ' - ' + f.PMG100_AMO
        END AS Description,
    bs.Name AS 'Business Segment',
    scm.Name AS 'ASCM Category',
    pl.Name AS 'Product Line',
    hppn.RTPDate as 'RTP/MR Date',
    hppn.SADate as 'Select Availability (SA) Date',
    hppn.GADate as 'General Availability (GA) Date',
    hppn.EMDate as 'End of Manufacturing (EM) Date',
    hppn.GSEOLDate as 'Global Series Planned End Date',
    hppn.ESDate as 'End of Sales (ES) Date',
    amof.PreviousProduct as 'Previous Product',
    hppn.Comments,
    amos.Name AS 'SKU Type',
    f.CodeName as 'Code Name',
    u1.firstname + ' ' + u1.lastname AS 'Created by',
    u2.firstname + ' ' + u2.lastname AS 'Last Updated by'
FROM Feature f
RIGHT JOIN AmoHpPartNo hppn ON f.FeatureID = hppn.FeatureID
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

    private async Task<CommonDataModel> GetHpAMOPartNumberAsync(int hpAMOPartNumberId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetHpAMOPartNumberCommandText(), connection);
        SqlParameter parameter = new("HpAMOPartNumberId", hpAMOPartNumberId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel hpAMOPartNumber = new();
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

                if (columnName.Equals(TargetName.AmoPartNumber, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                hpAMOPartNumber.Add(columnName, value);
            }

            hpAMOPartNumber.Add("Target", TargetTypeValue.AmoPartNumber);
            hpAMOPartNumber.Add("Id", SearchIdName.AmoPartNumber + hpAMOPartNumber.GetValue("Hp AMO Part Number Id"));
        }

        return hpAMOPartNumber;
    }

    private async Task<IEnumerable<CommonDataModel>> GetHpAMOPartNumberAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetHpAMOPartNumberCommandText(), connection);
        SqlParameter parameter = new("HpAMOPartNumberId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();

        while (await reader.ReadAsync())
        {
            CommonDataModel hpAMOPartNumber = new();
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

                if (columnName.Equals(TargetName.AmoPartNumber, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                hpAMOPartNumber.Add(columnName, value);
            }

            hpAMOPartNumber.Add("Target", TargetTypeValue.AmoPartNumber);
            hpAMOPartNumber.Add("Id", SearchIdName.AmoPartNumber + hpAMOPartNumber.GetValue("Hp AMO Part Number Id"));
            output.Add(hpAMOPartNumber);
        }

        return output;
    }
}
