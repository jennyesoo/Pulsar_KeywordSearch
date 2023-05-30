using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal class ChangeRequestReader : IKeywordSearchDataReader
{
    private readonly KeywordSearchInfo _info;

    public ChangeRequestReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public Task<CommonDataModel> GetDataAsync(int changeRequestId)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> changeRequest = await GetChangeRequestAsync();

        List<Task> tasks = new()
        {
            HandlePropertyValuesAsync(changeRequest),
            GetApproverAsync(changeRequest)
        };

        await Task.WhenAll(tasks);
        return changeRequest;
    }

    private string GetChangeRequestCommandText()
    {
        return @"
SELECT di.id AS ChangeRequestId,
    CASE 
        WHEN di.ChangeType = 0
            THEN 'Dcr'
        WHEN di.ChangeType = 1
            THEN 'Bcr'
        WHEN di.ChangeType = 2
            THEN 'Scr'
        WHEN di.ChangeType = 3
            THEN 'InfoDcr'
        END AS ChangeType,
    di.Submitter,
    di.Created AS DateSubmitter,
    di.actualDate AS DateClosed,
    pv.Dotsname AS Product,
    DR.Name as ComponentRoot,
    di.summary AS Summary,
    AStatus.Name AS STATUS,
    ui.FirstName + ', ' + ui.LastName AS OWNER,
    di.NA,
    di.LA,
    di.EMEA,
    di.APJ,
    di.Description,
    di.Details,
    di.Justification,
    di.Resolution,
    di.Actions,
    di.ZsrpRequired,
    di.AVRequired,
    di.QualificationRequired,
    di.GlobalSeriesRequired,
    di.AffectsCustomers AS CustomerImpact,
    di.AvailableForTest AS SampleAvailable,
    di.TargetApprovalDate,
    di.Important,
    di.RTPDate,
    di.RASDiscoDate,
    di.OnStatusReport
FROM Deliverableissues di
left join DeliverableRoot DR on DR.id = di.DeliverableRootID
left JOIN ProductVersion pv ON pv.id = di.ProductVersionID
left JOIN ActionStatus AStatus ON AStatus.id = di.STATUS
left JOIN UserInfo ui ON ui.userid = di.OwnerID
WHERE (
        @ChangeRequestId = - 1
        OR di.Id = @ChangeRequestId
        )
";
    }

    private string GetApproversCommandText()
    {
        return @"
SELECT dcr.id as ChangeRequestId,
    stuff((
            SELECT '{' + e.Name 
            FROM ActionApproval AS a WITH (NOLOCK)
            left JOIN Employee AS e WITH (NOLOCK) ON a.ApproverId = e.Id
            left JOIN DeliverableIssues d WITH (NOLOCK) ON a.ActionId = d.Id
            WHERE d.id = dcr.id
            ORDER BY d.id
            FOR XML path('')
            ), 1, 1, '') AS Approvers
FROM DeliverableIssues dcr
GROUP BY dcr.id
";
    }

    private async Task<IEnumerable<CommonDataModel>> GetChangeRequestAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetChangeRequestCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();
        while (reader.Read())
        {
            CommonDataModel changeRequest = new();
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
                    changeRequest.Add(columnName, value);
                }
            }
            changeRequest.Add("Target", "ChangeRequest");
            changeRequest.Add("Id", SearchIdName.Dcr + changeRequest.GetValue("ChangeRequestId"));
            output.Add(changeRequest);
        }
        return output;
    }

    private Task HandlePropertyValuesAsync(IEnumerable<CommonDataModel> changeRequests)
    {
        foreach (CommonDataModel dcr in changeRequests)
        {
            if (dcr.GetValue("ZsrpRequired").Equals("True"))
            {
                dcr.Add("ZsrpRequired", "ZSRP Ready Date Required");
            }
            else
            {
                dcr.Delete("ZsrpRequired");
            }

            if (dcr.GetValue("AVRequired").Equals("True"))
            {
                dcr.Add("AVRequired", "AV Required");
            }
            else
            {
                dcr.Delete("AVRequired");
            }

            if (dcr.GetValue("QualificationRequired").Equals("True"))
            {
                dcr.Add("QualificationRequired", "Qualification Required");
            }
            else
            {
                dcr.Delete("QualificationRequired");
            }

            if (dcr.GetValue("GlobalSeriesRequired").Equals("True"))
            {
                dcr.Add("GlobalSeriesRequired", "Global Series Required");
            }
            else
            {
                dcr.Delete("GlobalSeriesRequired");
            }

            if (dcr.GetValue("CustomerImpact").Equals("1"))
            {
                dcr.Add("CustomerImpact", "Affects images and/or BIOS on shipping products");
            }
            else
            {
                dcr.Delete("CustomerImpact");
            }

            if (dcr.GetValue("OnStatusReport").Equals("1"))
            {
                dcr.Add("OnStatusReport", "Remove from Online Status Reports");
            }
            else
            {
                dcr.Delete("OnStatusReport");
            }

            if (dcr.GetValue("Important").Equals("True"))
            {
                dcr.Add("Important", "Important");
            }
            else
            {
                dcr.Delete("Important");
            }

            if (dcr.GetValue("NA").Equals("True"))
            {
                dcr.Add("NA", "NA");
            }
            else
            {
                dcr.Delete("NA");
            }

            if (dcr.GetValue("LA").Equals("True"))
            {
                dcr.Add("LA", "LA");
            }
            else
            {
                dcr.Delete("LA");
            }

            if (dcr.GetValue("EMEA").Equals("True"))
            {
                dcr.Add("EMEA", "EMEA");
            }
            else
            {
                dcr.Delete("EMEA");
            }

            if (dcr.GetValue("APJ").Equals("True"))
            {
                dcr.Add("APJ", "APJ");
            }
            else
            {
                dcr.Delete("APJ");
            }
        }

        return Task.CompletedTask;
    }

    private async Task GetApproverAsync(IEnumerable<CommonDataModel> changeRequests)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetApproversCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> approvers = new();

        while (await reader.ReadAsync())
        {
            if (!string.IsNullOrWhiteSpace(reader["Approvers"].ToString())
                && int.TryParse(reader["ChangeRequestId"].ToString(), out int changeRequestId))
            {
                approvers[changeRequestId] = reader["Approvers"].ToString();
            }
        }

        foreach (CommonDataModel dcr in changeRequests)
        {
            if (int.TryParse(dcr.GetValue("ChangeRequestId"), out int changeRequestId)
            && approvers.ContainsKey(changeRequestId))
            {
                string[] approverList = approvers[changeRequestId].Split('{');
                for (int i = 0; i < approverList.Length; i++)
                {
                    dcr.Add("Approvals " + i, approverList[i]);
                }
            }
        }
    }
}
