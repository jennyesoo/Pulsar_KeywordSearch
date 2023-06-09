using System.Xml.Linq;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

internal class ComponentRootReader : IKeywordSearchDataReader
{
    private readonly KeywordSearchInfo _info;

    public ComponentRootReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public async Task<CommonDataModel> GetDataAsync(int componentRootId)
    {
        CommonDataModel componentRoot = await GetComponentRootAsync(componentRootId);

        if (!componentRoot.GetElements().Any())
        {
            return null;
        }

        HandlePropertyValue(componentRoot);

        List<Task> tasks = new()
        {
            FillProductListAsync(componentRoot),
            FillTrulyLinkedFeatureAsync(componentRoot),
            FillLinkedFeatureAsync(componentRoot),
            FillComponentInitiatedLinkageAsync(componentRoot)
        };

        await Task.WhenAll(tasks);
        return componentRoot;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> componentRoot = await GetComponentRootAsync();

        List<Task> tasks = new()
        {
            HandlePropertyValueAsync(componentRoot),
            FillProductListAsync(componentRoot),
            FillTrulyLinkedFeaturesAsync(componentRoot),
            FillLinkedFeaturesAsync(componentRoot),
            FillComponentInitiatedLinkageAsync(componentRoot)
        };

        await Task.WhenAll(tasks);
        return componentRoot;
    }

    private string GetAllComponentRootSqlCommandText()
    {
        return @"SELECT root.id AS 'Component Root Id',
    root.name AS 'Component Root Name',
    root.Description,
    vendor.Name AS Vendor,
    cate.name AS Category,
    user1.FirstName + ' ' + user1.LastName AS 'Component PM',
    user1.Email AS 'Component PM Email',
    user2.FirstName + ' ' + user2.LastName AS Developer,
    user2.Email AS 'Developer Email',
    user3.FirstName + ' ' + user3.LastName AS TestLead,
    user3.Email AS 'Test Lead Email',
    coreteam.Name AS 'SI Core Team',
    CASE 
        WHEN root.TypeID = 1
            THEN 'Hardware'
        WHEN root.TypeID = 2
            THEN 'Software'
        WHEN root.TypeID = 3
            THEN 'Firmware'
        WHEN root.TypeID = 4
            THEN 'Documentation'
        END AS 'Component Type',
    root.Preinstall,
    root.TargetPartition as 'Target Partition',
    root.CDImage,
    root.ISOImage,
    root.CAB,
    root.BINARY,
    root.FloppyDisk,
    root.CertRequired AS 'WHQL Certification Require',
    root.ScriptPaq AS 'Packaging Softpaq',
    Sc.name AS SoftpaqCategory,
    root.Created,
    root.IconDesktop as 'Desktop',
    root.IconMenu as 'Start Menu',
    root.IconTray as 'System Tray',
    root.IconPanel as 'Control Panel',
    root.PropertyTabs as 'Property Tabs Added',
    root.AR,
    root.RoyaltyBearing as 'Royalty Bearing',
    sws.DisplayName AS 'Recovery Option',
    root.KitNumber as 'Kit Number',
    root.KitDescription as 'Kit Description',
    root.DeliverableSpec AS 'Functional Spec',
    root.IconInfoCenter as 'Info Center',
    cts.Name AS 'Transfer Server',
    root.Patch,
    root.SystemBoardID as 'System Board ID',
    root.CreatedBy as 'Created by',
    root.Updated,
    root.UpdatedBy as 'Updated by',
    root.Deleted,
    root.DeletedBy as 'Deleted by',
    user4.FirstName + ' ' + user4.LastName AS 'SIO Approver',
    root.KoreanCertificationRequired as 'Korean Certification Required',
    root.SubmissionPath as 'Submission Path',
    root.IRSBasePartNumber,
    CPSW.Description AS 'Prism SW Type',
    root.LimitFuncTestGroupVisability as 'Limit Partner Visibility ',
    root.IconTile as 'Start Menu Tile',
    root.IconTaskBarIcon as 'Taskbar Pinned Icon',
    root.SettingFWML as 'FWML',
    root.SettingUWPCompliant as 'UWP Compliant',
    root.FtpSitePath as 'FTP Site',
    root.DrDvd,
    root.MsStore as 'MS Store',
    root.ErdComments as 'ERD Comments',
    og.Name AS 'Si Function Test Group',
    root.Active AS Visibility,
    root.Notes AS 'Internal Notes',
    root.CDImage,
    root.ISOImage,
    root.AR,
    root.FloppyDisk as 'Internal Tool',
    root.IsSoftPaqInPreinstall as 'SoftPaq In Preinstall',
    root.IconMenu as 'Start Menu',
    root.RootFilename AS 'ROM Family',
    root.Rompaq as 'Rompaq Binary',
    root.PreinstallROM as 'ROM Components Preinstall',
    root.CAB,
    root.Softpaq AS 'ROM component Softpaq'
FROM DeliverableRoot root
LEFT JOIN vendor ON root.vendorid = vendor.id
LEFT JOIN componentCategory cate ON cate.CategoryId = root.categoryid
LEFT JOIN UserInfo user1 ON user1.userid = root.devmanagerid
LEFT JOIN userinfo user2 ON user2.userid = root.DeveloperID
LEFT JOIN userinfo user3 ON user3.userid = root.TesterID
LEFT JOIN userinfo user4 ON user4.userid = root.SioApproverId
LEFT JOIN componentcoreteam coreteam ON coreteam.ComponentCoreTeamId = root.CoreTeamID
LEFT JOIN ComponentPrismSWType CPSW ON CPSW.PRISMTypeID = root.PrismSWType
LEFT JOIN SWSetupCategory sws ON sws.ID = root.SWSetupCategoryID
LEFT JOIN ComponentTransferServer cts ON cts.Id = root.TransferServerId
LEFT JOIN SoftpaqCategory Sc ON Sc.id = root.SoftpaqCategoryID
LEFT JOIN OTSFVTOrganizations og ON root.OTSFVTOrganizationID = og.id
WHERE (
        @ComponentRootId = - 1
        OR root.id = @ComponentRootId
        )
    AND root.TypeID IN (
        1,
        2,
        3,
        4
        );
";
    }

    private string GetTSQLTrulyLinkedFeaturesCommandText()
    {
        return @"
SELECT fr.ComponentRootId,
    fr.FeatureId,
    f.FeatureName
FROM Feature_Root fr
JOIN Feature f WITH (NOLOCK) ON fr.FeatureID = f.FeatureID
WHERE ComponentRootId >= 1
    AND AutoLinkage = 1
    AND (
        @ComponentRootId = - 1
        OR fr.ComponentRootId = @ComponentRootId
        )
";
    }

    private string GetTSQLLinkedFeaturesCommandText()
    {
        return @"
SELECT fr.ComponentRootId,
    fr.FeatureId,
    f.FeatureName
FROM Feature_Root fr
JOIN Feature f WITH (NOLOCK) ON fr.FeatureID = f.FeatureID
WHERE ComponentRootId >= 1
    AND AutoLinkage = 0
    AND (
        @ComponentRootId = - 1
        OR fr.ComponentRootId = @ComponentRootId
        )
";
    }

    private string GetTSQLComponentInitiatedLinkageCommandText()
    {
        return @"
SELECT fril.FeatureId AS FeatureId,
    fril.ComponentRootId,
    f.FeatureName AS FeatureName
FROM Feature_Root_InitiatedLinkage fril WITH (NOLOCK)
LEFT JOIN feature f WITH (NOLOCK) ON f.featureID = fril.FeatureId
WHERE (
        @ComponentRootId = - 1
        OR fril.ComponentRootId = @ComponentRootId
        )
";
    }

    private string GetTSQLProductListCommandText()
    {
        return @"SELECT DR.Id AS ComponentRootId,
    stuff((
            SELECT ' , ' + (CONVERT(VARCHAR, p.Id) + ' ' + p.DOTSName)
            FROM ProductVersion p
            LEFT JOIN ProductStatus ps ON ps.id = p.ProductStatusID
            LEFT JOIN Product_DelRoot pr ON pr.ProductVersionId = p.id
            LEFT JOIN DeliverableRoot root ON root.Id = pr.DeliverableRootId
            WHERE root.Id = DR.Id
                AND ps.Name <> 'Inactive'
                AND p.FusionRequirements = 1
            ORDER BY root.Id
            FOR XML path('')
            ), 1, 3, '') AS ProductList
FROM DeliverableRoot DR
WHERE (
        @ComponentRootId = - 1
        OR DR.Id = @ComponentRootId
        )
GROUP BY DR.Id
";

        //SELECT p.dotsname, r.Name
        //FROM DeliverableRoot root
        //join Product_DelRoot pr on root.Id = pr.DeliverableRootId
        //join ProductVersion p on pr.ProductVersionID = p.id
        //join ProductVersion_Release pr2 on pr2.ProductVersionID = p.Id
        //join ProductVersionRelease r on r.Id = pr2.ReleaseId
        //where root.id = 36886
        //order by p.dotsname
    }

    private async Task<CommonDataModel> GetComponentRootAsync(int componentRootId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetAllComponentRootSqlCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", componentRootId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel root = new();
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

                if (columnName.Equals(TargetName.ComponentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                root.Add(columnName, value);
            }
            root.Add("Target", "ComponentRoot");
            root.Add("Id", SearchIdName.ComponentRoot + root.GetValue("Component Root Id"));
        }
        return root;
    }

    private async Task<IEnumerable<CommonDataModel>> GetComponentRootAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();

        SqlCommand command = new(GetAllComponentRootSqlCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", -1);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();

        List<CommonDataModel> output = new();

        while (await reader.ReadAsync())
        {
            CommonDataModel root = new();
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

                if (columnName.Equals(TargetName.ComponentRoot, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                root.Add(columnName, value);
            }
            root.Add("Target", TargetTypeValue.ComponentRoot);
            root.Add("Id", SearchIdName.ComponentRoot + root.GetValue("Component Root Id"));
            output.Add(root);
        }
        return output;
    }

    private async Task FillProductListAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("Component Root Id"), out int componentRootId))
        {
            return;
        }
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLProductListCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", componentRootId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> productList = new();

        if (!await reader.ReadAsync())
        {
            return;
        }

        if (int.TryParse(reader["ComponentRootId"].ToString(), out int dbComponentRootId))
        {
            productList[dbComponentRootId] = reader["ProductList"].ToString();
        }

        if (productList.ContainsKey(componentRootId))
        {
            componentRoot.Add("Product List", productList[componentRootId]);
        }
    }

    private async Task FillProductListAsync(IEnumerable<CommonDataModel> componentRoots)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLProductListCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> productList = new();

        while (await reader.ReadAsync())
        {
            if (int.TryParse(reader["ComponentRootId"].ToString(), out int componentRootId))
            {
                productList[componentRootId] = reader["ProductList"].ToString();
            }
        }

        foreach (CommonDataModel root in componentRoots)
        {
            if (int.TryParse(root.GetValue("Component Root Id"), out int componentRootId)
            && productList.ContainsKey(componentRootId))
            {
                root.Add("Product List", productList[componentRootId]);
            }
        }
    }

    private static CommonDataModel HandlePropertyValue(CommonDataModel componentRoot)
    {
        if (componentRoot.GetValue("Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Preinstall", "Preinstall");
        }
        else
        {
            componentRoot.Delete("Preinstall");
        }

        if (componentRoot.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("DrDvd", "DRDVD");
        }
        else
        {
            componentRoot.Delete("DrDvd");
        }

        if (componentRoot.GetValue("Packaging Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Packaging Softpaq", "Packaging Softpaq");
        }
        else
        {
            componentRoot.Delete("Packaging Softpaq");
        }

        if (componentRoot.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Ms Store", "Ms Store");
        }
        else
        {
            componentRoot.Delete("Ms Store");
        }

        if (componentRoot.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Internal Tool", "Internal Tool");
        }
        else
        {
            componentRoot.Delete("Internal Tool");
        }

        if (componentRoot.GetValue("Patch").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Patch", "Patch");
        }
        else
        {
            componentRoot.Delete("Patch");
        }

        if (componentRoot.GetValue("Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Binary", "Binary");
        }
        else
        {
            componentRoot.Delete("Binary");
        }

        if (componentRoot.GetValue("ROM Components Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("ROM Components Preinstall", "ROM Component Preinstall");
        }
        else
        {
            componentRoot.Delete("ROM Components Preinstall");
        }

        if (componentRoot.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("CAB", "CAB");
        }
        else
        {
            componentRoot.Delete("CAB");
        }

        if (componentRoot.GetValue("ROM component Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("ROM component Softpaq", "ROM component Softpaq");
        }
        else
        {
            componentRoot.Delete("ROM component Softpaq");
        }

        if (componentRoot.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Desktop", "Desktop");
        }
        else
        {
            componentRoot.Delete("Desktop");
        }

        if (componentRoot.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Start Menu", "Start Menu");
        }
        else
        {
            componentRoot.Delete("Start Menu");
        }

        if (componentRoot.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("System Tray", "System Tray");
        }
        else
        {
            componentRoot.Delete("System Tray");
        }

        if (componentRoot.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Control Panel", "Control Panel");
        }
        else
        {
            componentRoot.Delete("Control Panel");
        }

        if (componentRoot.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Info Center", "Info Center");
        }
        else
        {
            componentRoot.Delete("Info Center");
        }

        if (componentRoot.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Start Menu Tile", "Start Menu Tile");
        }
        else
        {
            componentRoot.Delete("Start Menu Tile");
        }

        if (componentRoot.GetValue("Taskbar Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Taskbar Pinned Icon", "Taskbar Pinned Icon");
        }
        else
        {
            componentRoot.Delete("Taskbar Pinned Icon");
        }

        if (componentRoot.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("FWML", "FWML");
        }
        else
        {
            componentRoot.Delete("FWML");
        }

        if (componentRoot.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("UWP Compliant", "UWP Compliant");
        }
        else
        {
            componentRoot.Delete("UWP Compliant");
        }

        if (componentRoot.GetValue("Royalty Bearing").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Royalty Bearing", "Royalty Bearing");
        }
        else
        {
            componentRoot.Delete("Royalty Bearing");
        }

        if (componentRoot.GetValue("Korean Certification Required").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Korean Certification Required", "Korean Certification Required");
        }
        else
        {
            componentRoot.Delete("Korean Certification Required");
        }

        if (componentRoot.GetValue("WHQL Certification Require").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("WHQL Certification Require", "WHQL Certification Require");
        }
        else
        {
            componentRoot.Delete("WHQL Certification Require");
        }

        if (componentRoot.GetValue("Limit partner visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Limit partner visibility", "Limit partner visibility");
        }
        else
        {
            componentRoot.Delete("Limit partner visibility");
        }

        if (componentRoot.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Visibility", "Active");
        }
        else
        {
            componentRoot.Delete("Visibility");
        }

        if (componentRoot.GetValue("SoftPaq In Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("SoftPaq In Preinstall", "SoftPaq In Preinstall");
        }
        else
        {
            componentRoot.Delete("SoftPaq In Preinstall");
        }

        if (componentRoot.GetValue("Rompaq Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Rompaq Binary", "Rompaq Binary");
        }
        else
        {
            componentRoot.Delete("Rompaq Binary");
        }

        if (GetCd(componentRoot).Equals(1))
        {
            componentRoot.Add("CD", "CD");
        }
        componentRoot.Delete("CDImage");
        componentRoot.Delete("ISOImage");
        componentRoot.Delete("AR");

        return componentRoot;
    }

    private Task HandlePropertyValueAsync(IEnumerable<CommonDataModel> componentRoots)
    {
        foreach (CommonDataModel root in componentRoots)
        {
            if (root.GetValue("Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Preinstall", "Preinstall");
            }
            else
            {
                root.Delete("Preinstall");
            }

            if (root.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("DrDvd", "DRDVD");
            }
            else
            {
                root.Delete("DrDvd");
            }

            if (root.GetValue("Packaging Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Packaging Softpaq", "Packaging Softpaq");
            }
            else
            {
                root.Delete("Packaging Softpaq");
            }

            if (root.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Ms Store", "Ms Store");
            }
            else
            {
                root.Delete("Ms Store");
            }

            if (root.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Internal Tool", "Internal Tool");
            }
            else
            {
                root.Delete("Internal Tool");
            }

            if (root.GetValue("Patch").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Patch", "Patch");
            }
            else
            {
                root.Delete("Patch");
            }

            if (root.GetValue("Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Binary", "Binary");
            }
            else
            {
                root.Delete("Binary");
            }

            if (root.GetValue("ROM Components Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("ROM Components Preinstall", "ROM Component Preinstall");
            }
            else
            {
                root.Delete("ROM Components Preinstall");
            }

            if (root.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("CAB", "CAB");
            }
            else
            {
                root.Delete("CAB");
            }

            if (root.GetValue("ROM component Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("ROM component Softpaq", "ROM component Softpaq");
            }
            else
            {
                root.Delete("ROM component Softpaq");
            }

            if (root.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Desktop", "Desktop");
            }
            else
            {
                root.Delete("Desktop");
            }

            if (root.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Start Menu", "Start Menu");
            }
            else
            {
                root.Delete("Start Menu");
            }

            if (root.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("System Tray", "System Tray");
            }
            else
            {
                root.Delete("System Tray");
            }

            if (root.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Control Panel", "Control Panel");
            }
            else
            {
                root.Delete("Control Panel");
            }

            if (root.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Info Center", "Info Center");
            }
            else
            {
                root.Delete("Info Center");
            }

            if (root.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Start Menu Tile", "Start Menu Tile");
            }
            else
            {
                root.Delete("Start Menu Tile");
            }

            if (root.GetValue("Taskbar Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Taskbar Pinned Icon", "Taskbar Pinned Icon");
            }
            else
            {
                root.Delete("Taskbar Pinned Icon");
            }

            if (root.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("FWML", "FWML");
            }
            else
            {
                root.Delete("FWML");
            }

            if (root.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("UWP Compliant", "UWP Compliant");
            }
            else
            {
                root.Delete("UWP Compliant");
            }

            if (root.GetValue("Royalty Bearing").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Royalty Bearing", "Royalty Bearing");
            }
            else
            {
                root.Delete("Royalty Bearing");
            }

            if (root.GetValue("Korean Certification Required").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Korean Certification Required", "Korean Certification Required");
            }
            else
            {
                root.Delete("Korean Certification Required");
            }

            if (root.GetValue("WHQL Certification Require").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("WHQL Certification Require", "WHQL Certification Require");
            }
            else
            {
                root.Delete("WHQL Certification Require");
            }

            if (root.GetValue("Limit partner visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Limit partner visibility", "Limit partner visibility");
            }
            else
            {
                root.Delete("Limit partner visibility");
            }

            if (root.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Visibility", "Active");
            }
            else
            {
                root.Delete("Visibility");
            }

            if (root.GetValue("SoftPaq In Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("SoftPaq In Preinstall", "SoftPaq In Preinstall");
            }
            else
            {
                root.Delete("SoftPaq In Preinstall");
            }

            if (root.GetValue("Rompaq Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Rompaq Binary", "Rompaq Binary");
            }
            else
            {
                root.Delete("Rompaq Binary");
            }

            if (GetCd(root).Equals(1))
            {
                root.Add("CD", "CD");
            }
            root.Delete("CDImage");
            root.Delete("ISOImage");
            root.Delete("AR");
        }

        return Task.CompletedTask;
    }

    private static int GetCd(CommonDataModel root)
    {
        if (root.GetValue("CDImage").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        if (root.GetValue("ISOImage").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        if (root.GetValue("AR").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }
        return 0;
    }

    private async Task FillTrulyLinkedFeatureAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("Component Root Id"), out int componentRootId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLTrulyLinkedFeaturesCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", componentRootId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> trulyLinkedFeatures = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int dbComponentRootId))
            {
                continue;
            }

            if (trulyLinkedFeatures.ContainsKey(dbComponentRootId))
            {
                trulyLinkedFeatures[dbComponentRootId].Add((reader["FeatureId"].ToString(), reader["FeatureName"].ToString()));
            }
            else
            {
                trulyLinkedFeatures[dbComponentRootId] = new List<(string, string)>() { (reader["FeatureId"].ToString(), reader["FeatureName"].ToString()) };
            }
        }

        if (trulyLinkedFeatures.ContainsKey(componentRootId))
        {
            for (int i = 0; i < trulyLinkedFeatures[componentRootId].Count; i++)
            {
                componentRoot.Add("Truly Linked Features Id" + i, trulyLinkedFeatures[componentRootId][i].Item1);
                componentRoot.Add("Truly Linked Features Name" + i, trulyLinkedFeatures[componentRootId][i].Item2);
            }
        }
    }

    private async Task FillTrulyLinkedFeaturesAsync(IEnumerable<CommonDataModel> roots)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLTrulyLinkedFeaturesCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> trulyLinkedFeatures = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int componentRootId))
            {
                continue;
            }

            if (trulyLinkedFeatures.ContainsKey(componentRootId))
            {
                trulyLinkedFeatures[componentRootId].Add((reader["FeatureId"].ToString(), reader["FeatureName"].ToString()));
            }
            else
            {
                trulyLinkedFeatures[componentRootId] = new List<(string, string)>() { (reader["FeatureId"].ToString(), reader["FeatureName"].ToString()) };
            }
        }

        foreach (CommonDataModel root in roots)
        {
            if (int.TryParse(root.GetValue("Component Root Id"), out int componentRootId)
              && trulyLinkedFeatures.ContainsKey(componentRootId))
            {
                for (int i = 0; i < trulyLinkedFeatures[componentRootId].Count; i++)
                {
                    root.Add("Truly Linked Features Id" + i, trulyLinkedFeatures[componentRootId][i].Item1);
                    root.Add("Truly Linked Features Name" + i, trulyLinkedFeatures[componentRootId][i].Item2);

                }
            }
        }
    }

    private async Task FillLinkedFeatureAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("Component Root Id"), out int componentRootId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLLinkedFeaturesCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", componentRootId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> linkedFeatures = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int dbComponentRootId))
            {
                continue;
            }

            if (linkedFeatures.ContainsKey(dbComponentRootId))
            {
                linkedFeatures[dbComponentRootId].Add((reader["FeatureId"].ToString(), reader["FeatureName"].ToString()));
            }
            else
            {
                linkedFeatures[dbComponentRootId] = new List<(string, string)>() { (reader["FeatureId"].ToString(), reader["FeatureName"].ToString()) };
            }
        }

        if (linkedFeatures.ContainsKey(componentRootId))
        {
            for (int i = 0; i < linkedFeatures[componentRootId].Count; i++)
            {
                componentRoot.Add("Linked Features Id" + i, linkedFeatures[componentRootId][i].Item1);
                componentRoot.Add("Linked Features Name" + i, linkedFeatures[componentRootId][i].Item2);
            }
        }
    }

    private async Task FillLinkedFeaturesAsync(IEnumerable<CommonDataModel> roots)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLLinkedFeaturesCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> linkedFeatures = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int componentRootId))
            {
                continue;
            }

            if (linkedFeatures.ContainsKey(componentRootId))
            {
                linkedFeatures[componentRootId].Add((reader["FeatureId"].ToString(), reader["FeatureName"].ToString()));
            }
            else
            {
                linkedFeatures[componentRootId] = new List<(string, string)>() { (reader["FeatureId"].ToString(), reader["FeatureName"].ToString()) };
            }
        }

        foreach (CommonDataModel root in roots)
        {
            if (int.TryParse(root.GetValue("Component Root Id"), out int componentRootId)
              && linkedFeatures.ContainsKey(componentRootId))
            {
                for (int i = 0; i < linkedFeatures[componentRootId].Count; i++)
                {
                    root.Add("Linked Features Id" + i, linkedFeatures[componentRootId][i].Item1);
                    root.Add("Linked Features Name" + i, linkedFeatures[componentRootId][i].Item2);
                }
            }  
        }
    }

    private async Task FillComponentInitiatedLinkageAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("Component Root Id"), out int componentRootId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLComponentInitiatedLinkageCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", componentRootId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> componentInitiatedLinkage = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int dbComponentRootId))
            {
                continue;
            }

            if (componentInitiatedLinkage.ContainsKey(dbComponentRootId))
            {
                componentInitiatedLinkage[dbComponentRootId].Add((reader["FeatureId"].ToString(), reader["FeatureName"].ToString()));
            }
            else
            {
                componentInitiatedLinkage[dbComponentRootId] = new List<(string, string)>() { (reader["FeatureId"].ToString(), reader["FeatureName"].ToString()) };
            }
        }

        if (componentInitiatedLinkage.ContainsKey(componentRootId))
        {
            for (int i = 0; i < componentInitiatedLinkage[componentRootId].Count; i++)
            {
                componentRoot.Add("Component Initiated Linkage Id" + i, componentInitiatedLinkage[componentRootId][i].Item1);
                componentRoot.Add("Component Initiated Linkage Name" + i, componentInitiatedLinkage[componentRootId][i].Item2);
            }
        }
    }

    private async Task FillComponentInitiatedLinkageAsync(IEnumerable<CommonDataModel> roots)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetTSQLComponentInitiatedLinkageCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<(string, string)>> componentInitiatedLinkage = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int componentRootId))
            {
                continue;
            }

            if (componentInitiatedLinkage.ContainsKey(componentRootId))
            {
                componentInitiatedLinkage[componentRootId].Add((reader["FeatureId"].ToString(), reader["FeatureName"].ToString()));
            }
            else
            {
                componentInitiatedLinkage[componentRootId] = new List<(string, string)>() { (reader["FeatureId"].ToString(), reader["FeatureName"].ToString()) };
            }
        }

        foreach (CommonDataModel root in roots)
        {
            if (int.TryParse(root.GetValue("Component Root Id"), out int componentRootId)
              && componentInitiatedLinkage.ContainsKey(componentRootId))
            {
                for (int i = 0; i < componentInitiatedLinkage[componentRootId].Count; i++)
                {
                    root.Add("Component Initiated Linkage Id" + i, componentInitiatedLinkage[componentRootId][i].Item1);
                    root.Add("Component Initiated Linkage Name" + i, componentInitiatedLinkage[componentRootId][i].Item2);
                }
            }
        }
    }
}
