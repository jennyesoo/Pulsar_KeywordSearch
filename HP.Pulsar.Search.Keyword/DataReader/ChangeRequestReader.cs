using System.Reflection;
using System.Text.Json;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Meilisearch;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

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

        List<Task> tasks = new()
        {
            FillApproverAsync(changeRequest),
            FillReviewerAsync(changeRequest),
            FillProductSiblingsAsync(changeRequest)
        };

        await Task.WhenAll(tasks);

        return changeRequest;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> changeRequest = await GetChangeRequestAsync();

        List<Task> tasks = new()
        {
            HandlePropertyValuesAsync(changeRequest),
            FillApproversAsync(changeRequest),
            FillReviewerAsync(changeRequest),
            FillProductSiblingsAsync(changeRequest)
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
    pv.Dotsname AS Product(s),
    di.ProductVersionId, 
	di.ProductVersionRelease,
    di.DraftProducts
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
    di.AvailableForTest AS 'Samples Available Date',
    di.TargetApprovalDate as 'Target Approval Date',
    di.Important,
    di.RTPDate as 'RTP Date',
    di.RASDiscoDate as 'End of Manufacturing Date',
    di.OnStatusReport as 'Status Report',
    di.Notify as 'Notify on Approval',
    ui2.FullName + ' ' + ui2.LastName AS 'BIOS PM',
    di.Attachment1 AS 'Attachment 1',
    di.Attachment2 AS 'Attachment 2',
    di.Attachment3 AS 'Attachment 3',
    di.Attachment4 AS 'Attachment 4',
    di.Attachment5 AS 'Attachment 5'
FROM Deliverableissues di
LEFT JOIN DeliverableRoot DR ON DR.id = di.DeliverableRootID
LEFT JOIN ProductVersion pv ON pv.id = di.ProductVersionID
LEFT JOIN ActionStatus AStatus ON AStatus.id = di.STATUS
LEFT JOIN UserInfo ui ON ui.userid = di.OwnerID
LEFT JOIN UserInfo us ON us.userid = di.SubmitterID
LEFT JOIN UserInfo ui2 on ui2.userid = di.BiosPmId
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
left join ActionStatus AStatus on AStatus.id = dcr.Status
WHERE AStatus.Name != 'Draft' 
        AND
        AStatus.Name != 'BiosApproval'
        AND
        (
        @ChangeRequestId = - 1
        OR dcr.id = @ChangeRequestId
        )
GROUP BY dcr.id
";
    }

    private string GetReviewerCommandText()
    {
        return @"
select dr.DeliverableIssueId AS 'ChangeRequestId',
        u.firstname + ' ' + u.lastname  as 'ReviewerTable - Reviewer',
        u.email as 'ReviewerTable - Reviewer Email',
        Case when dr.Status = 0 Then 'Awaiting Review'
        When dr.Status = 1 THEN 'Not Applicable'
        When dr.Status = 2 THEN 'Review Completed'
        END as 'ReviewerTable - Status',
        dr.Comments AS 'ReviewerTable - Comments',
        dr.LastModificationTime AS 'ReviewerTable - Updated'
from DeliverableIssue_Reviewer dr
left join Deliverableissues di on di.id = dr.DeliverableIssueId
left join userinfo u on u.userid = dr.ReviewerId
left join ActionStatus AStatus on AStatus.id = di.Status
WHERE di.ChangeType = 0 
        AND 
            AStatus.Name = 'Draft' 
        AND
            (
            @ChangeRequestId = - 1
            OR dr.DeliverableIssueId = @ChangeRequestId
            )
";
    }

    private string GetGroupIdCommandText()
    {
        return @"
 
SELECT i.Id AS 'ChangeRequestId',
    GroupId 
FROM DeliverableIssues i WITH (NOLOCK) 
left join ActionStatus AStatus on AStatus.id = i.Status
where i.ChangeType != 2 
        AND 
             (i.ChangeType != 0 And AStatus.Name != 'Draft') 
        AND
            (
            @ChangeRequestId = - 1
            OR i.Id = @ChangeRequestId
            )
";
    }

    private string GetDCRSiblingsCommandText()
    {
        return @"
SELECT DcrId = i.Id AS 'ChangeRequestId', 
    ProductId = i.ProductVersionId, 
    ProductName = pv.DotsName, 
    ReleaseName = i.ProductVersionRelease, 
    Status = AStatus.Name,
    GroupId = i.GroupId 
FROM DeliverableIssues i WITH (NOLOCK) 
LEFT JOIN ProductVersion pv WITH (NOLOCK) ON pv.ProductVersionId = i.ProductVersionId 
left join ActionStatus AStatus on AStatus.id = i.Status
where i.ChangeType != 2 
        AND 
            (i.ChangeType != 0 And AStatus.Name != 'Draft') 
        AND
            (
            @ChangeRequestId = - 1
            OR i.Id = @ChangeRequestId
            )
ORDER BY i.Id 
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

        if (string.Equals(changeRequest.GetValue("Change Type"), "Scr", StringComparison.OrdinalIgnoreCase))
        {
            changeRequest.Delete("Product(s)");
            changeRequest.Delete("ProductVersionId");
            changeRequest.Delete("ProductVersionRelease");
            changeRequest.Delete("DraftProducts");
        }

        if (string.Equals(changeRequest.GetValue("Change Type"), "Dcr", StringComparison.OrdinalIgnoreCase)
            && string.Equals(changeRequest.GetValue("Status"), "Draft", StringComparison.OrdinalIgnoreCase))
        {
            List<string> result = AnalyzeProducts(changeRequest.GetValue("DraftProducts"));

            for (int i = 0; 0 < result.Count; i++)
            {
                changeRequest.Add("Product(s) Table " + i, result[i]);
            }

            changeRequest.Delete("Product(s)");
            changeRequest.Delete("ProductVersionId");
            changeRequest.Delete("ProductVersionRelease");
            changeRequest.Delete("DraftProducts");
        }

        if (!string.Equals(changeRequest.GetValue("Change Type"), "Scr", StringComparison.OrdinalIgnoreCase))
        {
            changeRequest.Delete("Deliverable Root");
        }

        if (!string.Equals(changeRequest.GetValue("Change Type"), "Bcr", StringComparison.OrdinalIgnoreCase))
        {
            changeRequest.Delete("BIOS PM");
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
        foreach (CommonDataModel changeRequest in changeRequests)
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

            if (string.Equals(changeRequest.GetValue("Change Type"), "Scr", StringComparison.OrdinalIgnoreCase))
            {
                changeRequest.Delete("Product(s)");
                changeRequest.Delete("ProductVersionId");
                changeRequest.Delete("ProductVersionRelease");
                changeRequest.Delete("DraftProducts");
            }

            if (string.Equals(changeRequest.GetValue("Change Type"), "Dcr", StringComparison.OrdinalIgnoreCase)
                && string.Equals(changeRequest.GetValue("Status"), "Draft", StringComparison.OrdinalIgnoreCase))
            {
                List<string> result = AnalyzeProducts(changeRequest.GetValue("DraftProducts"));

                for (int i = 0; 0 < result.Count; i++)
                {
                    changeRequest.Add("Product(s) Table " + i, result[i]);
                }

                changeRequest.Delete("Product(s)");
                changeRequest.Delete("ProductVersionId");
                changeRequest.Delete("ProductVersionRelease");
                changeRequest.Delete("DraftProducts");
            }

            if (!string.Equals(changeRequest.GetValue("Change Type"), "Scr", StringComparison.OrdinalIgnoreCase))
            {
                changeRequest.Delete("Deliverable Root");
            }

            if (!string.Equals(changeRequest.GetValue("Change Type"), "Bcr", StringComparison.OrdinalIgnoreCase))
            {
                changeRequest.Delete("BIOS PM");
            }

            changeRequest.Delete("ZSRP Ready Date Required");
            changeRequest.Delete("AV Required");
            changeRequest.Delete("Qualification Required");
            changeRequest.Delete("Global Series Required");
            changeRequest.Delete("NA");
            changeRequest.Delete("LA");
            changeRequest.Delete("EMEA");
            changeRequest.Delete("APJ");
        }

        return Task.CompletedTask;
    }

    private static List<string> AnalyzeProducts(string jsonValue)
    {
        Dictionary<int, string> products = new Dictionary<int, string>();

        JsonDocument doc = JsonDocument.Parse(jsonValue);

        foreach (JsonElement id in doc.RootElement.EnumerateArray())
        {
            foreach (JsonElement item in id.EnumerateArray())
            {
                if (!item.TryGetProperty("ProductId", out JsonElement productId))
                {
                    continue;
                }

                if (!item.TryGetProperty("ProductName", out JsonElement productName))
                {
                    continue;
                }

                if (!item.TryGetProperty("ReleaseName", out JsonElement releaseName))
                {
                    continue;
                }

                if (products.ContainsKey(productId.GetInt32()))
                {
                    Console.WriteLine(productId.ToString());
                }
                else
                {
                    products[productId.GetInt32()] = productId.ToString() + " " + productName.ToString() + " ( " + releaseName.ToString() + " ) ";
                }
            }
        }

        List<string> results = new List<string>();

        foreach (int item in products.Keys)
        {
            results.Add(products[item]);
        }

        return results;
    }

    private async Task FillApproverAsync(CommonDataModel changeRequest)
    {
        if (!int.TryParse(changeRequest.GetValue("Change Request Id"), out int changeRequestId))
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

    private async Task FillReviewerAsync(CommonDataModel changeRequest)
    {
        if (!int.TryParse(changeRequest.GetValue("Change Request Id"), out int changeRequestId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetReviewerCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", changeRequestId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> reviewer = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ChangeRequestId"].ToString(), out int dbChangeRequestId))
            {
                continue;
            }

            CommonDataModel item = new CommonDataModel();

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
                    || string.Equals(value, "None")
                    || columnName.Equals("ChangeRequestId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                item.Add(columnName, value);
            }

            if (!reviewer.ContainsKey(dbChangeRequestId))
            {
                reviewer[dbChangeRequestId] = new List<CommonDataModel>() { item };
            }
            else
            {
                reviewer[dbChangeRequestId].Add(item);
            }
        }

        if (!reviewer.ContainsKey(changeRequestId))
        {
            return;
        }

        for (int i = 0; i < reviewer[changeRequestId].Count; i++)
        {
            foreach (string item in reviewer[changeRequestId][i].GetKeys())
            {
                changeRequest.Add(item + " " + i, reviewer[changeRequestId][i].GetValue(item));
            }
        }
    }

    private async Task FillReviewerAsync(IEnumerable<CommonDataModel> changeRequests)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetReviewerCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<CommonDataModel>> reviewer = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ChangeRequestId"].ToString(), out int changeRequestId))
            {
                continue;
            }

            CommonDataModel item = new CommonDataModel();

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
                    || string.Equals(value, "None")
                    || columnName.Equals("ChangeRequestId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                item.Add(columnName, value);
            }

            if (!reviewer.ContainsKey(changeRequestId))
            {
                reviewer[changeRequestId] = new List<CommonDataModel>() { item };
            }
            else
            {
                reviewer[changeRequestId].Add(item);
            }
        }

        foreach (CommonDataModel dcr in changeRequests)
        {
            if (!int.TryParse(dcr.GetValue("Change Request Id"), out int changeRequestId)
                || !reviewer.ContainsKey(changeRequestId))
            {
                continue;
            }

            for (int i = 0; i < reviewer[changeRequestId].Count; i++)
            {
                foreach (string item in reviewer[changeRequestId][i].GetKeys())
                {
                    dcr.Add(item + " " + i, reviewer[changeRequestId][i].GetValue(item));
                }
            }
        }
    }

    private async Task FillProductSiblingsAsync(CommonDataModel changeRequest)
    {
        if (!int.TryParse(changeRequest.GetValue("Change Request Id"), out int changeRequestId))
        {
            return;
        }

        Dictionary<int, string> groupId = await GetGroupIdAsync(changeRequestId);
        (Dictionary<int, List<string>> dcrSiblings, Dictionary<string, List<string>> groupIdSiblings) = await GetDCRSiblingsAsync();

        if (int.TryParse(changeRequest.GetValue("Change Request Id"), out int dbChangeRequestId))
        {
            return;
        }

        int number = 0;

        if (dcrSiblings.ContainsKey(changeRequestId))
        {
            foreach (string item in dcrSiblings[changeRequestId])
            {
                changeRequest.Add("Product(s) Table " + number, item);
                number++;
            }
        }

        if (groupId.ContainsKey(changeRequestId)
            && groupIdSiblings.ContainsKey(groupId[changeRequestId]))
        {
            foreach (string item in groupIdSiblings[groupId[changeRequestId]])
            {
                changeRequest.Add("Product(s) Table " + number, item);
                number++;
            }
        }

        string firstProduct = string.Empty;

        if (!string.IsNullOrEmpty(changeRequest.GetValue("ProductVersionId")))
        {
            firstProduct += changeRequest.GetValue("ProductVersionId");
        }

        if (!string.IsNullOrEmpty(changeRequest.GetValue("Product(s)")))
        {
            firstProduct += " " + changeRequest.GetValue("Product(s)");
            firstProduct = firstProduct.Trim();
        }

        if (!string.IsNullOrEmpty(changeRequest.GetValue("ProductVersionRelease")))
        {
            firstProduct += " " + changeRequest.GetValue("ProductVersionRelease");
            firstProduct = firstProduct.Trim();
        }

        changeRequest.Delete("Product(s)");
        changeRequest.Delete("ProductVersionId");
        changeRequest.Delete("ProductVersionRelease");
        changeRequest.Delete("DraftProducts");

        if (!string.IsNullOrEmpty(firstProduct))
        {
            changeRequest.Add("Product(s)", firstProduct);
        }
        
    }

    private async Task FillProductSiblingsAsync(IEnumerable<CommonDataModel> changeRequests)
    {
        Dictionary<int, string> groupId = await GetGroupIdAsync();
        (Dictionary<int, List<string>> dcrSiblings, Dictionary<string, List<string>> groupIdSiblings) = await GetDCRSiblingsAsync();

        foreach (CommonDataModel dcr in changeRequests)
        {
            if (int.TryParse(dcr.GetValue("Change Request Id"), out int changeRequestId))
            {
                continue;
            }

            int number = 0;

            if (dcrSiblings.ContainsKey(changeRequestId))
            {
                foreach (string item in dcrSiblings[changeRequestId])
                {
                    dcr.Add("Product(s) Table " + number, item);
                    number++;
                }
            }

            if (groupId.ContainsKey(changeRequestId)
                && groupIdSiblings.ContainsKey(groupId[changeRequestId]))
            {
                foreach (string item in groupIdSiblings[groupId[changeRequestId]])
                {
                    dcr.Add("Product(s) Table " + number, item);
                    number++;
                }
            }

            string firstProduct = string.Empty;

            if (!string.IsNullOrEmpty(dcr.GetValue("ProductVersionId")))
            {
                firstProduct += dcr.GetValue("ProductVersionId");
            }

            if (!string.IsNullOrEmpty(dcr.GetValue("Product(s)")))
            {
                firstProduct += " " + dcr.GetValue("Product(s)");
                firstProduct = firstProduct.Trim();
            }

            if (!string.IsNullOrEmpty(dcr.GetValue("ProductVersionRelease")))
            {
                firstProduct += " " + dcr.GetValue("ProductVersionRelease");
                firstProduct = firstProduct.Trim();
            }

            dcr.Delete("Product(s)");
            dcr.Delete("ProductVersionId");
            dcr.Delete("ProductVersionRelease");
            dcr.Delete("DraftProducts");

            if (!string.IsNullOrEmpty(firstProduct))
            {
                dcr.Add("Product(s)", firstProduct);
            }
        }
    }

    private async Task<Dictionary<int, string>> GetGroupIdAsync(int dcrId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetGroupIdCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", dcrId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> groupId = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ChangeRequestId"].ToString(), out int changeRequestId))
            {
                groupId[changeRequestId] = reader["GroupId"].ToString();
            }
        }
        return groupId;
    }

    private async Task<Dictionary<int, string>> GetGroupIdAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetGroupIdCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> groupId = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ChangeRequestId"].ToString(), out int changeRequestId))
            {
                groupId[changeRequestId] = reader["GroupId"].ToString();
            }
        }
        return groupId;
    }

    private async Task<(Dictionary<int, List<string>>, Dictionary<string, List<string>>)> GetDCRSiblingsAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetDCRSiblingsCommandText(), connection);
        SqlParameter parameter = new("ChangeRequestId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> dcrSiblings = new();
        Dictionary<string, List<string>> groupIdSiblings = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ChangeRequestId"].ToString(), out int changeRequestId))
            {
                continue;
            }

            string result = $"{reader["ProductId"]} {reader["ProductName"]} ({reader["ReleaseName"]}) {reader["Status"]}";

            if (dcrSiblings.ContainsKey(changeRequestId))
            {
                dcrSiblings[changeRequestId].Add(result);
            }
            else
            {
                dcrSiblings[changeRequestId] = new List<string>() { result };
            }

            if (groupIdSiblings.ContainsKey(reader["GroupId"].ToString()))
            {
                groupIdSiblings[reader["GroupId"].ToString()].Add(result);
            }
            else
            {
                groupIdSiblings[reader["GroupId"].ToString()] = new List<string>() { result };
            }

        }
        return (dcrSiblings, groupIdSiblings);
    }
}
