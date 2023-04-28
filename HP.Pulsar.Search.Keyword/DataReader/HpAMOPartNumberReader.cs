using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader
{
    internal class HpAMOPartNumberReader : IKeywordSearchDataReader
    {
        private ConnectionStringProvider _csProvider;

        public HpAMOPartNumberReader(KeywordSearchInfo info)
        {
            _csProvider = new(info.Environment);
        }

        public async Task<CommonDataModel> GetDataAsync(int changeRequestId)
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
SELECT  hppn.AmoHpPartNumberID as HpAMOPartNumberId,
    hppn.HpPartNo as HpPartNumber,
    CASE  
        WHEN ISNULL(f.PMG100_AMO, '') <> ''  
            THEN hppn.HPPartNo + ' - ' + trim(f.PMG100_AMO + ' ' + ISNULL(r.CountryCode, ''))  
        ELSE hppn.HPPartNo + ' - ' + f.PMG100_AMO  
        END AS Description,
    bs.Name as BusinessSegment,
    scm.Name as ASCMCategory,
    pl.Name as productLine,
    hppn.RTPDate,
    hppn.SADate,
    hppn.GADate,
    hppn.EMDate,
    hppn.GSEOLDate,
    hppn.ESDate,
    amof.PreviousProduct,
    hppn.Comments,
    amos.Name as SKUType,
    f.CodeName,
    u1.firstname + ' ' + u1.lastname as CreatedBy,
    u2.firstname + ' ' + u2.lastname as LastUpdatedBy
FROM Feature f   
INNER JOIN AmoHpPartNo hppn ON f.FeatureID = hppn.FeatureID  
LEFT JOIN Regions r ON hppn.LocalizationId = r.ID  
Left JOIN SCMCategory scm on scm.SCMCategoryID = hppn.ASCMCategoryId
left join AMOFeatureV2 amof on f.featureID = amof.FeatureId
left join BusinessSegment bs on bs.BusinessSegmentid = amof.BusinessSegmentid
left join ProductLine pl on pl.id = hppn.productLineId
left join AMOSkuType amos on amos.SkuTypeId  = hppn.SkuTypeId
left join userinfo u1 on u1.userid = hppn.Creator
left join userinfo u2 on u2.userid = hppn.Updater
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
            SqlParameter parameter = new SqlParameter("HpAMOPartNumberId", "-1");
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
                        string value = reader[i].ToString();
                        hpAMOPartNumber.Add(columnName, value);
                    }
                }
                hpAMOPartNumber.Add("target", "HpAMOPartNumber");
                hpAMOPartNumber.Add("Id", SearchIdName.HpAMOPartNumber + hpAMOPartNumber.GetValue("HpAMOPartNumberId"));
                output.Add(hpAMOPartNumber);
            }
            return output;
        }
    }
}
