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
        return @"SELECT root.id AS ComponentRootId,
    root.name AS ComponentRootName,
    root.description,
    vendor.Name AS VendorName,
    cate.name AS SIFunctionTestGroupCategory,
    user1.FirstName + ' ' + user1.LastName AS ComponentPM,
    user1.Email AS ComponentPMEmail,
    user2.FirstName + ' ' + user2.LastName AS Developer,
    user2.Email AS DeveloperEmail,
    user3.FirstName + ' ' + user3.LastName AS TestLead,
    user3.Email AS TestLeadEmail,
    coreteam.Name AS SICoreTeam,
    CASE 
        WHEN root.TypeID = 1
            THEN 'Hardware'
        WHEN root.TypeID = 2
            THEN 'Software'
        WHEN root.TypeID = 3
            THEN 'Firmware'
        WHEN root.TypeID = 4
            THEN 'Documentation'
        END AS ComponentType,
    root.Preinstall,
    root.active AS Visibility,
    root.TargetPartition,
    root.Reboot,
    root.CDImage,
    root.ISOImage,
    root.CAB,
    root.BINARY,
    root.FloppyDisk,
    root.CertRequired AS WHQLCertificationRequire,
    root.ScriptPaq AS PackagingSoftpaq,
    root.MultiLanguage,
    Sc.name AS SoftpaqCategory,
    root.Created,
    root.IconDesktop,
    root.IconMenu,
    root.IconTray,
    root.IconPanel,
    root.PropertyTabs,
    root.AR,
    root.RoyaltyBearing,
    sws.DisplayName AS RecoveryOption,
    root.KitNumber,
    root.KitDescription,
    root.DeliverableSpec AS FunctionalSpec,
    root.IconInfoCenter,
    cts.Name AS TransferServer,
    root.Patch,
    root.SystemBoardID,
    root.IRSTransfers,
    root.DevicesInfPath,
    root.TestNotes,
    root.CreatedBy,
    root.Updated,
    root.UpdatedBy,
    root.Deleted,
    root.DeletedBy,
    root.SupplierID,
    user4.FirstName + ' ' + user4.LastName AS SIOApprover,
    root.KoreanCertificationRequired,
    root.BaseFilePath,
    root.SubmissionPath,
    root.IRSPartNumberLastSpin,
    root.IRSBasePartNumber,
    CPSW.Description AS PrismSWType,
    root.LimitFuncTestGroupVisability,
    root.IconTile,
    root.IconTaskBarIcon,
    root.SettingFWML,
    root.SettingUWPCompliant,
    root.FtpSitePath,
    root.DrDvd,
    root.MsStore,
    root.ErdComments,
    og.Name AS SiFunctionTestGroup,
    root.Active AS Visibility,
    root.Notes AS InternalNotes,
    root.CDImage,
    root.ISOImage,
    root.AR,
    root.FloppyDisk,
    root.IsSoftPaqInPreinstall,
    root.IconMenu,
    root.RootFilename AS ROMFamily,
    root.Rompaq,
    root.PreinstallROM,
    root.CAB,
    root.Softpaq AS ROMSoftpaq
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
                if (!string.IsNullOrWhiteSpace(reader[i].ToString()) &&
                    !string.Equals(reader[i].ToString(), "None"))
                {
                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString().Trim();
                    root.Add(columnName, value);
                }
            }
            root.Add("Target", "ComponentRoot");
            root.Add("Id", SearchIdName.ComponentRoot + root.GetValue("ComponentRootId"));
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
                if (!string.IsNullOrWhiteSpace(reader[i].ToString()) &&
                    !string.Equals(reader[i].ToString(), "None"))
                {
                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString().Trim();
                    root.Add(columnName, value);
                }
            }
            root.Add("Target", "ComponentRoot");
            root.Add("Id", SearchIdName.ComponentRoot + root.GetValue("ComponentRootId"));
            output.Add(root);
        }
        return output;
    }

    private async Task FillProductListAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("ComponentRootId"), out int componentRootId))
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
            componentRoot.Add("ProductList", productList[componentRootId]);
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
            if (int.TryParse(root.GetValue("ComponentRootId"), out int componentRootId)
            && productList.ContainsKey(componentRootId))
            {
                root.Add("ProductList", productList[componentRootId]);
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

        if (componentRoot.GetValue("PackagingSoftpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("PackagingSoftpaq", "Packaging Softpaq");
        }
        else
        {
            componentRoot.Delete("PackagingSoftpaq");
        }

        if (componentRoot.GetValue("MsStore").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("MsStore", "Ms Store");
        }
        else
        {
            componentRoot.Delete("MsStore");
        }

        if (componentRoot.GetValue("FloppyDisk").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("FloppyDisk", "Internal Tool");
        }
        else
        {
            componentRoot.Delete("FloppyDisk");
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

        if (componentRoot.GetValue("PreinstallROM").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("PreinstallROM", "ROM Component Preinstall");
        }
        else
        {
            componentRoot.Delete("PreinstallROM");
        }

        if (componentRoot.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("CAB", "CAB");
        }
        else
        {
            componentRoot.Delete("CAB");
        }

        if (componentRoot.GetValue("ROMSoftpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("ROMSoftpaq", "ROM component Softpaq");
        }
        else
        {
            componentRoot.Delete("ROMSoftpaq");
        }

        if (componentRoot.GetValue("IconDesktop").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconDesktop", "Desktop");
        }
        else
        {
            componentRoot.Delete("IconDesktop");
        }

        if (componentRoot.GetValue("IconMenu").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconMenu", "Start Menu");
        }
        else
        {
            componentRoot.Delete("IconMenu");
        }

        if (componentRoot.GetValue("IconTray").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconTray", "System Tray");
        }
        else
        {
            componentRoot.Delete("IconTray");
        }

        if (componentRoot.GetValue("IconPanel").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconPanel", "Control Panel");
        }
        else
        {
            componentRoot.Delete("IconPanel");
        }

        if (componentRoot.GetValue("IconInfoCenter").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconInfoCenter", "Info Center");
        }
        else
        {
            componentRoot.Delete("IconInfoCenter");
        }

        if (componentRoot.GetValue("IconTile").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconTile", "Start Menu Tile");
        }
        else
        {
            componentRoot.Delete("IconTile");
        }

        if (componentRoot.GetValue("IconTaskBarIcon").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IconTaskBarIcon", "Taskbar Pinned Icon ");
        }
        else
        {
            componentRoot.Delete("IconTaskBarIcon");
        }

        if (componentRoot.GetValue("SettingFWML").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("SettingFWML", "FWML");
        }
        else
        {
            componentRoot.Delete("SettingFWML");
        }

        if (componentRoot.GetValue("SettingUWPCompliant").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("SettingUWPCompliant", "FWML");
        }
        else
        {
            componentRoot.Delete("SettingUWPCompliant");
        }

        if (componentRoot.GetValue("RoyaltyBearing").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("RoyaltyBearing", "Royalty Bearing");
        }
        else
        {
            componentRoot.Delete("RoyaltyBearing");
        }

        if (componentRoot.GetValue("KoreanCertificationRequired").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("KoreanCertificationRequired", "Korean Certification Required");
        }
        else
        {
            componentRoot.Delete("KoreanCertificationRequired");
        }

        if (componentRoot.GetValue("WHQLCertificationRequire").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("WHQLCertificationRequire", "WHQL Certification Require");
        }
        else
        {
            componentRoot.Delete("WHQLCertificationRequire");
        }

        if (componentRoot.GetValue("LimitFuncTestGroupVisability").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("LimitFuncTestGroupVisability", "Limit partner visibility");
        }
        else
        {
            componentRoot.Delete("LimitFuncTestGroupVisability");
        }

        if (componentRoot.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Visibility", "Active");
        }
        else
        {
            componentRoot.Delete("Visibility");
        }

        if (componentRoot.GetValue("IsSoftPaqInPreinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("IsSoftPaqInPreinstall", "SoftPaq In Preinstall");
        }
        else
        {
            componentRoot.Delete("IsSoftPaqInPreinstall");
        }

        if (componentRoot.GetValue("Rompaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Rompaq", "Rompaq Binary");
        }
        else
        {
            componentRoot.Delete("Rompaq");
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

            if (root.GetValue("PackagingSoftpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("PackagingSoftpaq", "Packaging Softpaq");
            }
            else
            {
                root.Delete("PackagingSoftpaq");
            }

            if (root.GetValue("MsStore").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("MsStore", "Ms Store");
            }
            else
            {
                root.Delete("MsStore");
            }

            if (root.GetValue("FloppyDisk").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("FloppyDisk", "Internal Tool");
            }
            else
            {
                root.Delete("FloppyDisk");
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

            if (root.GetValue("PreinstallROM").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("PreinstallROM", "ROM Component Preinstall");
            }
            else
            {
                root.Delete("PreinstallROM");
            }

            if (root.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("CAB", "CAB");
            }
            else
            {
                root.Delete("CAB");
            }

            if (root.GetValue("ROMSoftpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("ROMSoftpaq", "ROM component Softpaq");
            }
            else
            {
                root.Delete("ROMSoftpaq");
            }

            if (root.GetValue("IconDesktop").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconDesktop", "Desktop");
            }
            else
            {
                root.Delete("IconDesktop");
            }

            if (root.GetValue("IconMenu").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconMenu", "Start Menu");
            }
            else
            {
                root.Delete("IconMenu");
            }

            if (root.GetValue("IconTray").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconTray", "System Tray");
            }
            else
            {
                root.Delete("IconTray");
            }

            if (root.GetValue("IconPanel").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconPanel", "Control Panel");
            }
            else
            {
                root.Delete("IconPanel");
            }

            if (root.GetValue("IconInfoCenter").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconInfoCenter", "Info Center");
            }
            else
            {
                root.Delete("IconInfoCenter");
            }

            if (root.GetValue("IconTile").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconTile", "Start Menu Tile");
            }
            else
            {
                root.Delete("IconTile");
            }

            if (root.GetValue("IconTaskBarIcon").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IconTaskBarIcon", "Taskbar Pinned Icon ");
            }
            else
            {
                root.Delete("IconTaskBarIcon");
            }

            if (root.GetValue("SettingFWML").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("SettingFWML", "FWML");
            }
            else
            {
                root.Delete("SettingFWML");
            }

            if (root.GetValue("SettingUWPCompliant").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("SettingUWPCompliant", "FWML");
            }
            else
            {
                root.Delete("SettingUWPCompliant");
            }

            if (root.GetValue("RoyaltyBearing").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("RoyaltyBearing", "Royalty Bearing");
            }
            else
            {
                root.Delete("RoyaltyBearing");
            }

            if (root.GetValue("KoreanCertificationRequired").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("KoreanCertificationRequired", "Korean Certification Required");
            }
            else
            {
                root.Delete("KoreanCertificationRequired");
            }

            if (root.GetValue("WHQLCertificationRequire").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("WHQLCertificationRequire", "WHQL Certification Require");
            }
            else
            {
                root.Delete("WHQLCertificationRequire");
            }

            if (root.GetValue("LimitFuncTestGroupVisability").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("LimitFuncTestGroupVisability", "Limit partner visibility");
            }
            else
            {
                root.Delete("LimitFuncTestGroupVisability");
            }

            if (root.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Visibility", "Active");
            }
            else
            {
                root.Delete("Visibility");
            }

            if (root.GetValue("IsSoftPaqInPreinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("IsSoftPaqInPreinstall", "SoftPaq In Preinstall");
            }
            else
            {
                root.Delete("IsSoftPaqInPreinstall");
            }

            if (root.GetValue("Rompaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Rompaq", "Rompaq Binary");
            }
            else
            {
                root.Delete("Rompaq");
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
        if (!int.TryParse(componentRoot.GetValue("ComponentRootId"), out int componentRootId))
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
                componentRoot.Add("TrulyLinkedFeatures Id" + i, trulyLinkedFeatures[componentRootId][i].Item1);
                componentRoot.Add("TrulyLinkedFeatures Name" + i, trulyLinkedFeatures[componentRootId][i].Item2);
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
            if (int.TryParse(root.GetValue("ComponentRootId"), out int componentRootId)
              && trulyLinkedFeatures.ContainsKey(componentRootId))
            {
                for (int i = 0; i < trulyLinkedFeatures[componentRootId].Count; i++)
                {
                    root.Add("TrulyLinkedFeatures Id" + i, trulyLinkedFeatures[componentRootId][i].Item1);
                    root.Add("TrulyLinkedFeatures Name" + i, trulyLinkedFeatures[componentRootId][i].Item2);

                }
            }
        }
    }

    private async Task FillLinkedFeatureAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("ComponentRootId"), out int componentRootId))
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
                componentRoot.Add("LinkedFeatures Id" + i, linkedFeatures[componentRootId][i].Item1);
                componentRoot.Add("LinkedFeatures Name" + i, linkedFeatures[componentRootId][i].Item2);
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
            if (int.TryParse(root.GetValue("ComponentRootId"), out int componentRootId)
              && linkedFeatures.ContainsKey(componentRootId))
            {
                for (int i = 0; i < linkedFeatures[componentRootId].Count; i++)
                {
                    root.Add("LinkedFeatures Id" + i, linkedFeatures[componentRootId][i].Item1);
                    root.Add("LinkedFeatures Name" + i, linkedFeatures[componentRootId][i].Item2);
                }
            }
        }
    }

    private async Task FillComponentInitiatedLinkageAsync(CommonDataModel componentRoot)
    {
        if (!int.TryParse(componentRoot.GetValue("ComponentRootId"), out int componentRootId))
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
                componentRoot.Add("ComponentInitiatedLinkage Id" + i, componentInitiatedLinkage[componentRootId][i].Item1);
                componentRoot.Add("ComponentInitiatedLinkage Name" + i, componentInitiatedLinkage[componentRootId][i].Item2);
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
            if (int.TryParse(root.GetValue("ComponentRootId"), out int componentRootId)
              && componentInitiatedLinkage.ContainsKey(componentRootId))
            {
                for (int i = 0; i < componentInitiatedLinkage[componentRootId].Count; i++)
                {
                    root.Add("ComponentInitiatedLinkage Id" + i, componentInitiatedLinkage[componentRootId][i].Item1);
                    root.Add("ComponentInitiatedLinkage Name" + i, componentInitiatedLinkage[componentRootId][i].Item2);
                }
            }
        }
    }
}
