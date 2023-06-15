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

    public async Task<CommonDataModel> GetDataAsync(int changeRequestId)
    {
        CommonDataModel changeRequest = await GetChangeRequestAsync(changeRequestId);

        if (!changeRequest.GetElements().Any())
        {
            return null;
        }

        HandlePropertyValue(changeRequest);
        await FillApproverAsync(changeRequest);
        return changeRequest;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> changeRequest = await GetChangeRequestAsync();

        List<Task> tasks = new()
        {
            HandlePropertyValuesAsync(changeRequest),
            FillApproversAsync(changeRequest)
        };

        await Task.WhenAll(tasks);
        return changeRequest;
    }

    private string GetChangeRequestCommandText()
    {
        return @"
SELECT di.id AS 'Change Request Id',
    CASE 
        WHEN di.ChangeType = 0
            THEN 'Dcr'
        WHEN di.ChangeType = 1
            THEN 'Bcr'
        WHEN di.ChangeType = 2
            THEN 'Scr'
        WHEN di.ChangeType = 3
            THEN 'InfoDcr'
        END AS 'Change Type',
    us.FirstName + ' ' + us.LastName AS Submitter,
    us.Email as 'Submitter Email',
    di.Created AS 'Date Submitted',
    di.actualDate AS 'Date Closed',
    pv.Dotsname AS Product,
    DR.Name AS 'Deliverable Root',
    di.summary AS Summary,
    AStatus.Name AS Status,
    ui.FirstName + ' ' + ui.LastName AS Owner,
    ui.Email as 'Owner Email',
    di.NA,
    di.LA,
    di.EMEA,
    di.APJ,
    di.Description,
    di.Details,
    di.Justification,
    di.Resolution,
    di.Actions as 'Actions Needed',
    di.ZsrpRequired as 'ZSRP Ready Date Required',
    di.AVRequired as 'AV Required',
    di.QualificationRequired as 'Qualification Required',
    di.GlobalSeriesRequired as 'Global Series Required',
    di.AffectsCustomers AS 'Customer Impact',
    di.AvailableForTest AS 'Samples Available',
    di.TargetApprovalDate as 'Target Approval Date',
    di.Important,
    di.RTPDate as 'RTP Date',
    di.RASDiscoDate as 'End of Manufacturing',
    di.OnStatusReport as 'Status Report',
    di.Notify as 'Notify on Approval'
FROM Deliverableissues di
LEFT JOIN DeliverableRoot DR ON DR.id = di.DeliverableRootID
LEFT JOIN ProductVersion pv ON pv.id = di.ProductVersionID
LEFT JOIN ActionStatus AStatus ON AStatus.id = di.STATUS
LEFT JOIN UserInfo ui ON ui.userid = di.OwnerID
LEFT JOIN UserInfo us ON us.userid = di.SubmitterID
WHERE (
        @ChangeRequestId = - 1
        OR di.Id = @ChangeRequestId
        )
";
    }

    private string GetApproversCommandText()
    {
        return @"
SELECT dcr.id AS ChangeRequestId,
    stuff((
            SELECT '{' + e.Name
            FROM ActionApproval AS a WITH (NOLOCK)
            LEFT JOIN Employee AS e WITH (NOLOCK) ON a.ApproverId = e.Id
            LEFT JOIN DeliverableIssues d WITH (NOLOCK) ON a.ActionId = d.Id
            WHERE d.id = dcr.id
            ORDER BY d.id
            FOR XML path('')
            ), 1, 1, '') AS Approvers
FROM DeliverableIssues dcr
WHERE (
        @ChangeRequestId = - 1
        OR dcr.id = @ChangeRequestId
        )
GROUP BY dcr.id
";
    }

    private async Task<CommonDataModel> GetChangeRequestAsync(int changeRequestId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetChangeRequestCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", changeRequestId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel changeRequest = new();
        while (reader.Read())
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

                if (columnName.Equals(TargetName.Dcr, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                changeRequest.Add(columnName, value);
            }
            changeRequest.Add("Target", TargetTypeValue.Dcr);
            changeRequest.Add("Id", SearchIdName.Dcr + changeRequest.GetValue("Change Request Id"));
        }
        return changeRequest;
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

                string columnName = reader.GetName(i);
                string value = reader[i].ToString().Trim();

                if (string.IsNullOrWhiteSpace(value)
                    || string.Equals(value, "None"))
                { 
                    continue; 
                }

                if (columnName.Equals(TargetName.Dcr, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                changeRequest.Add(columnName, value);
            }
            changeRequest.Add("Target", TargetTypeValue.Dcr);
            changeRequest.Add("Id", SearchIdName.Dcr + changeRequest.GetValue("Change Request Id"));
            output.Add(changeRequest);
        }
        return output;
    }

    private static CommonDataModel HandlePropertyValue(CommonDataModel changeRequest)
    {
        if (changeRequest.GetValue("ZSRP Ready Date Required").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Additional Options")))
            {
                changeRequest.Add("Additional Options", "ZSRP Ready Date Required");
            }
            else
            {
                changeRequest.Add("Additional Options", changeRequest.GetValue("Additional Options") + ", ZSRP Ready Date Required");
            }
        }
        
        if (changeRequest.GetValue("AV Required").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Additional Options")))
            {
                changeRequest.Add("Additional Options", "AV Required");
            }
            else
            {
                changeRequest.Add("Additional Options", changeRequest.GetValue("Additional Options") + ", AV Required");
            }
        }
        
        if (changeRequest.GetValue("Qualification Required").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Additional Options")))
            {
                changeRequest.Add("Additional Options", "Qualification Required");
            }
            else
            {
                changeRequest.Add("Additional Options", changeRequest.GetValue("Additional Options") + ", Qualification Required");
            }
        }
        
        if (changeRequest.GetValue("Global Series Required").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Additional Options")))
            {
                changeRequest.Add("Additional Options", "Global Series Required");
            }
            else
            {
                changeRequest.Add("Additional Options", changeRequest.GetValue("Additional Options") + ", Global Series Required");
            }
        }
        
        if (changeRequest.GetValue("Customer Impact").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            changeRequest.Add("Customer Impact", "Affects images and/or BIOS on shipping products");
        }
        else
        {
            changeRequest.Delete("Customer Impact");
        }

        if (changeRequest.GetValue("Status Report").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            changeRequest.Add("Status Report", "Remove from Online Status Reports");
        }
        else
        {
            changeRequest.Delete("Status Report");
        }

        if (changeRequest.GetValue("Important").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            changeRequest.Add("Important", "Important");
        }
        else
        {
            changeRequest.Delete("Important");
        }

        if (changeRequest.GetValue("NA").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Regions")))
            {
                changeRequest.Add("Regions", "NA");
            }
            else
            {
                changeRequest.Add("Regions", changeRequest.GetValue("Regions") + ", NA");
            }
        }
        
        if (changeRequest.GetValue("LA").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Regions")))
            {
                changeRequest.Add("Regions", "LA");
            }
            else
            {
                changeRequest.Add("Regions", changeRequest.GetValue("Regions") + ", LA");
            }
        }
        
        if (changeRequest.GetValue("EMEA").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Regions")))
            {
                changeRequest.Add("Regions", "EMEA");
            }
            else
            {
                changeRequest.Add("Regions", changeRequest.GetValue("Regions") + ", EMEA");
            }
        }

        if (changeRequest.GetValue("APJ").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(changeRequest.GetValue("Regions")))
            {
                changeRequest.Add("Regions", "APJ");
            }
            else
            {
                changeRequest.Add("Regions", changeRequest.GetValue("Regions") + ", APJ");
            }
        }
        changeRequest.Delete("ZSRP Ready Date Required");
        changeRequest.Delete("AV Required");
        changeRequest.Delete("Qualification Required");
        changeRequest.Delete("Global Series Required");
        changeRequest.Delete("NA");
        changeRequest.Delete("LA");
        changeRequest.Delete("EMEA");
        changeRequest.Delete("APJ");

        return changeRequest;

    }

    private Task HandlePropertyValuesAsync(IEnumerable<CommonDataModel> changeRequests)
    {
        foreach (CommonDataModel dcr in changeRequests)
        {
            if (dcr.GetValue("ZSRP Ready Date Required").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Additional Options")))
                {
                    dcr.Add("Additional Options", "ZSRP Ready Date Required");
                }
                else
                {
                    dcr.Add("Additional Options", dcr.GetValue("Additional Options") + ", ZSRP Ready Date Required");
                }
            }

            if (dcr.GetValue("AV Required").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Additional Options")))
                {
                    dcr.Add("Additional Options", "AV Required");
                }
                else
                {
                    dcr.Add("Additional Options", dcr.GetValue("Additional Options") + ", AV Required");
                }
            }

            if (dcr.GetValue("Qualification Required").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Additional Options")))
                {
                    dcr.Add("Additional Options", "Qualification Required");
                }
                else
                {
                    dcr.Add("Additional Options", dcr.GetValue("Additional Options") + ", Qualification Required");
                }
            }

            if (dcr.GetValue("Global Series Required").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Additional Options")))
                {
                    dcr.Add("Additional Options", "Global Series Required");
                }
                else
                {
                    dcr.Add("Additional Options", dcr.GetValue("Additional Options") + ", Global Series Required");
                }
            }

            if (dcr.GetValue("Customer Impact").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                dcr.Add("Customer Impact", "Affects images and/or BIOS on shipping products");
            }
            else
            {
                dcr.Delete("Customer Impact");
            }

            if (dcr.GetValue("Status Report").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                dcr.Add("Status Report", "Remove from Online Status Reports");
            }
            else
            {
                dcr.Delete("Status Report");
            }

            if (dcr.GetValue("Important").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                dcr.Add("Important", "Important");
            }
            else
            {
                dcr.Delete("Important");
            }

            if (dcr.GetValue("NA").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Regions")))
                {
                    dcr.Add("Regions", "NA");
                }
                else
                {
                    dcr.Add("Regions", dcr.GetValue("Regions") + ", NA");
                }
            }

            if (dcr.GetValue("LA").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Regions")))
                {
                    dcr.Add("Regions", "LA");
                }
                else
                {
                    dcr.Add("Regions", dcr.GetValue("Regions") + ", LA");
                }
            }

            if (dcr.GetValue("EMEA").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Regions")))
                {
                    dcr.Add("Regions", "EMEA");
                }
                else
                {
                    dcr.Add("Regions", dcr.GetValue("Regions") + ", EMEA");
                }
            }

            if (dcr.GetValue("APJ").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(dcr.GetValue("Regions")))
                {
                    dcr.Add("Regions", "APJ");
                }
                else
                {
                    dcr.Add("Regions", dcr.GetValue("Regions") + ", APJ");
                }
            }
            dcr.Delete("ZSRP Ready Date Required");
            dcr.Delete("AV Required");
            dcr.Delete("Qualification Required");
            dcr.Delete("Global Series Required");
            dcr.Delete("NA");
            dcr.Delete("LA");
            dcr.Delete("EMEA");
            dcr.Delete("APJ");
        }

        return Task.CompletedTask;
    }

    private async Task FillApproverAsync(CommonDataModel changeRequest)
    {
        if (!int.TryParse(changeRequest.GetValue("ChangeRequestId"), out int changeRequestId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetApproversCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", changeRequestId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> approvers = new();

        if (await reader.ReadAsync()
            && !string.IsNullOrWhiteSpace(reader["Approvers"].ToString())
            && int.TryParse(reader["ChangeRequestId"].ToString(), out int dbChangeRequestId))
        {
            approvers[dbChangeRequestId] = reader["Approvers"].ToString();
        }

        if (approvers.ContainsKey(changeRequestId))
        {
            string[] approverList = approvers[changeRequestId].Split('{');
            for (int i = 0; i < approverList.Length; i++)
            {
                changeRequest.Add("Approvers " + i, approverList[i]);
            }
        }

    }

    private async Task FillApproversAsync(IEnumerable<CommonDataModel> changeRequests)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetApproversCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", "-1");
        command.Parameters.Add(parameter);
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
            if (int.TryParse(dcr.GetValue("Change Request Id"), out int changeRequestId)
            && approvers.ContainsKey(changeRequestId))
            {
                string[] approverList = approvers[changeRequestId].Split('{');
                for (int i = 0; i < approverList.Length; i++)
                {
                    dcr.Add("Approvers " + i, approverList[i]);
                }
            }
        }
    }
}
