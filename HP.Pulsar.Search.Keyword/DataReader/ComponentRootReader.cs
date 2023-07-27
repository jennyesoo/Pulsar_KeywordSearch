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

        List<Task> tasks = new()
        {
            FillTrulyLinkedFeatureAsync(componentRoot),
            FillLinkedFeatureAsync(componentRoot),
            FillComponentInitiatedLinkageAsync(componentRoot),
            FillFunctionalTestGroupOdmsAsync(componentRoot)
        };

        await Task.WhenAll(tasks);
        HandlePropertyValue(componentRoot);
        DeleteProperty(componentRoot);

        return componentRoot;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> componentRoot = await GetComponentRootAsync();

        List<Task> tasks = new()
        {
            HandlePropertyValueAsync(componentRoot),
            FillTrulyLinkedFeaturesAsync(componentRoot),
            FillLinkedFeaturesAsync(componentRoot),
            FillComponentInitiatedLinkageAsync(componentRoot),
            FillFunctionalTestGroupOdmsAsync(componentRoot)
        };

        await Task.WhenAll(tasks);
        DeleteProperty(componentRoot);

        return componentRoot;
    }

    private string GetAllComponentRootSqlCommandText()
    {
        return @"SELECT root.id AS 'Component Root Id',
    root.name AS 'Component Root Name',
    root.Description,
    Case When root.AgencyLead= 'WLAN' Then 'Wireless LAN'
         When root.AgencyLead= 'WiGig' Then 'WiGig'
         When root.AgencyLead= 'BT' Then 'BT'
         End As 'Agency Lead',
    vendor.Name AS Vendor,
    root.SystemBoardID As 'System Board ID',
    cate.name AS Category,
    cate.RequiredPrismSWType,
    cate.Abbreviation,
    user1.FirstName + ' ' + user1.LastName AS 'Component PM',
    user1.Email AS 'Component PM Email',
    user2.FirstName + ' ' + user2.LastName AS Developer,
    user2.Email AS 'Developer Email',
    user3.FirstName + ' ' + user3.LastName AS 'Test Lead',
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
    root.CertRequired AS 'WHQL Certification Require',
    root.ScriptPaq AS 'Packaging Softpaq',
    root.Created As 'Created Date',
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
    root.CreatedBy as 'Created by',
    root.Updated AS 'Updated Date',
    root.UpdatedBy as 'Updated by',
    root.Deleted AS 'Deleted Date',
    root.DeletedBy as 'Deleted by',
    user4.FirstName + ' ' + user4.LastName AS 'SIO Approver',
    root.KoreanCertificationRequired as 'Korean Certification Required',
    root.SubmissionPath as 'Submission Path',
    CPSW.Description AS 'Prism SW Type',
    root.LimitFuncTestGroupVisability as 'Limit Partner Visibility',
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
    root.Softpaq AS 'ROM component Softpaq',
    ns.name AS 'Naming Standard',
    root.FtpSitePath AS 'FTP Site',
    root.Replicater AS 'CDs Replicated By',
    cr.CDFiles AS 'CD Types : CD Files - Files copied from a CD will be released',
    root.IsoImage AS 'CD Types : ISO Image -An ISO image of a CD will be released',
    root.Ar AS 'CD Types : Replicator Only - Only available from the Replicator'
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
LEFT JOIN NamingStandard ns ON ns.NamingStandardID = root.NamingStandardId
LEFT JOIN ComponentRoot cr on cr.ComponentRootid = root.id 
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

    private string GetFunctionalTestGroupOdmsCommandText()
    {
        return @"
select crp.ComponentRoot_ComponentRootId AS 'ComponentRootId',
        (p.Name +
        case when p.IrsOdmId is not Null Then ' (Desktop and Notebook)'
        when p.IrsOdmId is Null Then ' (Notebook)'
        End ) AS 'Partner Name'
from ComponentRootPartners crp
left join Partner p on crp.Partner_PartnerId = p.PartnerId
left join DeliverableRoot root on root.id = crp.ComponentRoot_ComponentRootId
where root.LimitFuncTestGroupVisability = 1 
    and 
    (
        @ComponentRootId = - 1
        OR crp.ComponentRoot_ComponentRootId = @ComponentRootId
        )
";
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

    private static CommonDataModel HandlePropertyValue(CommonDataModel componentRoot)
    {
        if (componentRoot.GetValue("Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "Preinstall");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", Preinstall");
            }
        }

        if (componentRoot.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(componentRoot.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentRoot.GetValue("Component Type"), "Documentation", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "DRDVD");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", DRDVD");
            }
        }

        if (componentRoot.GetValue("Packaging Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "Softpaq");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", Softpaq");
            }
        }

        if (componentRoot.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase)
            && string.Equals(componentRoot.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "Ms Store");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", Ms Store");
            }
        }

        if (componentRoot.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "Internal Tool");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", Internal Tool");
            }
        }

        if (componentRoot.GetValue("Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("ROM components")))
            {
                componentRoot.Add("ROM components", "Binary");
            }
            else
            {
                componentRoot.Add("ROM components", componentRoot.GetValue("ROM components") + ", Binary");
            }
        }

        if (componentRoot.GetValue("ROM Components Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("ROM components")))
            {
                componentRoot.Add("ROM components", "Preinstall");
            }
            else
            {
                componentRoot.Add("ROM components", componentRoot.GetValue("ROM components") + ", Preinstall");
            }
        }

        if (componentRoot.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("ROM components")))
            {
                componentRoot.Add("ROM components", "CAB");
            }
            else
            {
                componentRoot.Add("ROM components", componentRoot.GetValue("ROM components") + ", CAB");
            }
        }

        if (componentRoot.GetValue("ROM component Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("ROM components")))
            {
                componentRoot.Add("ROM components", "Softpaq");
            }
            else
            {
                componentRoot.Add("ROM components", componentRoot.GetValue("ROM components") + ", Softpaq");
            }
        }

        if (componentRoot.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "Desktop");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", Desktop");
            }
        }

        if (componentRoot.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "Start Menu");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", Start Menu");
            }
        }

        if (componentRoot.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "System Tray");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", System Tray");
            }
        }

        if (componentRoot.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "Control Panel");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", Control Panel");
            }
        }

        if (componentRoot.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "Info Center");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", Info Center");
            }
        }

        if (componentRoot.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "Start Menu Tile");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", Start Menu Tile");
            }
        }

        if (componentRoot.GetValue("Taskbar Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Touch Points")))
            {
                componentRoot.Add("Touch Points", "Taskbar Pinned Icon");
            }
            else
            {
                componentRoot.Add("Touch Points", componentRoot.GetValue("Touch Points") + ", Taskbar Pinned Icon");
            }
        }

        if (componentRoot.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Other Setting")))
            {
                componentRoot.Add("Other Setting", "FWML");
            }
            else
            {
                componentRoot.Add("Other Settings", componentRoot.GetValue("Other Setting") + ", FWML");
            }
        }

        if (componentRoot.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Other Setting")))
            {
                componentRoot.Add("Other Setting", "UWP Compliant");
            }
            else
            {
                componentRoot.Add("Other Settings", componentRoot.GetValue("Other Setting") + ", UWP Compliant");
            }
        }

        if (componentRoot.GetValue("Royalty Bearing").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Special Notes")))
            {
                componentRoot.Add("Special Notes", "Royalty Bearing");
            }
            else
            {
                componentRoot.Add("Special Notes", componentRoot.GetValue("Special Notes") + ", Royalty Bearing");
            }
        }

        if (componentRoot.GetValue("Korean Certification Required").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Special Notes")))
            {
                componentRoot.Add("Special Notes", "Korean Certification Required");
            }
            else
            {
                componentRoot.Add("Special Notes", componentRoot.GetValue("Special Notes") + ", Korean Certification Required");
            }
        }

        if (componentRoot.GetValue("WHQL Certification Require").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("WHQL Certification Require", "WHQL Certification Require");
        }
        else
        {
            componentRoot.Delete("WHQL Certification Require");
        }

        if (componentRoot.GetValue("Limit Partner Visibility").Equals("True", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(componentRoot.GetValue("Si Function Test Group")))
        {
            componentRoot.Add("Limit Partner Visibility", "Limit Partner Visibility");
        }
        else
        {
            componentRoot.Delete("Limit Partner Visibility");
        }

        if (componentRoot.GetValue("Visibility").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentRoot.Add("Visibility", "Active");
        }
        else
        {
            componentRoot.Delete("Visibility");
        }

        if (componentRoot.GetValue("SoftPaq In Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "SoftPaq In Preinstall");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", SoftPaq In Preinstall");
            }
        }

        if (componentRoot.GetValue("Rompaq Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("ROM components")))
            {
                componentRoot.Add("ROM components", "Binary");
            }
            else
            {
                componentRoot.Add("ROM components", componentRoot.GetValue("ROM components") + ", Binary");
            }
        }

        if (GetCd(componentRoot).Equals(1))
        {
            if (string.IsNullOrEmpty(componentRoot.GetValue("Packaging")))
            {
                componentRoot.Add("Packagings", "CD");
            }
            else
            {
                componentRoot.Add("Packagings", componentRoot.GetValue("Packaging") + ", CD");
            }

            if (componentRoot.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                componentRoot.Add("CD Types : CD Files - Files copied from a CD will be released", "CD Types : CD Files - Files copied from a CD will be released");
            }
            else
            {
                componentRoot.Delete("CD Types : CD Files - Files copied from a CD will be released");
            }

            if (componentRoot.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                componentRoot.Add("CD Types : Replicator Only - Only available from the Replicator", "CD Types : Replicator Only - Only available from the Replicator");
            }
            else
            {
                componentRoot.Delete("CD Types : Replicator Only - Only available from the Replicator");
            }

            if (componentRoot.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                componentRoot.Add("CD Types : ISO Image -An ISO image of a CD will be released", "CD Types : ISO Image -An ISO image of a CD will be released");
            }
            else
            {
                componentRoot.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
            }
        }
        else
        {
            componentRoot.Delete("CD Types : CD Files - Files copied from a CD will be released");
            componentRoot.Delete("CD Types : Replicator Only - Only available from the Replicator");
            componentRoot.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
            componentRoot.Delete("FTP Site");
        }

        componentRoot.Delete("CDImage");
        componentRoot.Delete("ISOImage");
        componentRoot.Delete("AR");
        componentRoot.Delete("Royalty Bearing");
        componentRoot.Delete("Korean Certification Required");
        componentRoot.Delete("Preinstall");
        componentRoot.Delete("DrDvd");
        componentRoot.Delete("Packaging Softpaq");
        componentRoot.Delete("Ms Store");
        componentRoot.Delete("Internal Tool");
        componentRoot.Delete("SoftPaq In Preinstall");
        componentRoot.Delete("Binary");
        componentRoot.Delete("ROM Components Preinstall");
        componentRoot.Delete("CAB");
        componentRoot.Delete("ROM component Softpaq");
        componentRoot.Delete("Desktop");
        componentRoot.Delete("Start Menu");
        componentRoot.Delete("System Tray");
        componentRoot.Delete("Control Panel");
        componentRoot.Delete("Info Center");
        componentRoot.Delete("Start Menu Tile");
        componentRoot.Delete("Taskbar Pinned Icon");
        componentRoot.Delete("FWML");
        componentRoot.Delete("UWP Compliant");
        componentRoot.Delete("Rompaq Binary");

        return componentRoot;
    }

    private Task HandlePropertyValueAsync(IEnumerable<CommonDataModel> componentRoots)
    {
        foreach (CommonDataModel root in componentRoots)
        {
            if (root.GetValue("Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "Preinstall");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", Preinstall");
                }
            }

            if (root.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(root.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase)
                || string.Equals(root.GetValue("Component Type"), "Documentation", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "DRDVD");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", DRDVD");
                }
            }

            if (root.GetValue("Packaging Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "Softpaq");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", Softpaq");
                }
            }

            if (root.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase)
                && string.Equals(root.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "Ms Store");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", Ms Store");
                }
            }

            if (root.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "Internal Tool");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", Internal Tool");
                }
            }

            if (root.GetValue("Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("ROM components")))
                {
                    root.Add("ROM components", "Binary");
                }
                else
                {
                    root.Add("ROM components", root.GetValue("ROM components") + ", Binary");
                }
            }

            if (root.GetValue("ROM Components Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("ROM components")))
                {
                    root.Add("ROM components", "Preinstall");
                }
                else
                {
                    root.Add("ROM components", root.GetValue("ROM components") + ", Preinstall");
                }
            }

            if (root.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("ROM components")))
                {
                    root.Add("ROM components", "CAB");
                }
                else
                {
                    root.Add("ROM components", root.GetValue("ROM components") + ", CAB");
                }
            }

            if (root.GetValue("ROM component Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("ROM components")))
                {
                    root.Add("ROM components", "Softpaq");
                }
                else
                {
                    root.Add("ROM components", root.GetValue("ROM components") + ", Softpaq");
                }
            }

            if (root.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "Desktop");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", Desktop");
                }
            }

            if (root.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "Start Menu");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", Start Menu");
                }
            }

            if (root.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "System Tray");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", System Tray");
                }
            }

            if (root.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "Control Panel");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", Control Panel");
                }
            }

            if (root.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "Info Center");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", Info Center");
                }
            }

            if (root.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "Start Menu Tile");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", Start Menu Tile");
                }
            }

            if (root.GetValue("Taskbar Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Touch Points")))
                {
                    root.Add("Touch Points", "Taskbar Pinned Icon");
                }
                else
                {
                    root.Add("Touch Points", root.GetValue("Touch Points") + ", Taskbar Pinned Icon");
                }
            }

            if (root.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Other Setting")))
                {
                    root.Add("Other Setting", "FWML");
                }
                else
                {
                    root.Add("Other Settings", root.GetValue("Other Setting") + ", FWML");
                }
            }

            if (root.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Other Setting")))
                {
                    root.Add("Other Setting", "UWP Compliant");
                }
                else
                {
                    root.Add("Other Settings", root.GetValue("Other Setting") + ", UWP Compliant");
                }
            }

            if (root.GetValue("Royalty Bearing").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Special Notes")))
                {
                    root.Add("Special Notes", "Royalty Bearing");
                }
                else
                {
                    root.Add("Special Notes", root.GetValue("Special Notes") + ", Royalty Bearing");
                }
            }

            if (root.GetValue("Korean Certification Required").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Special Notes")))
                {
                    root.Add("Special Notes", "Korean Certification Required");
                }
                else
                {
                    root.Add("Special Notes", root.GetValue("Special Notes") + ", Korean Certification Required");
                }
            }

            if (root.GetValue("WHQL Certification Require").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("WHQL Certification Require", "WHQL Certification Require");
            }
            else
            {
                root.Delete("WHQL Certification Require");
            }

            if (root.GetValue("Limit Partner Visibility").Equals("True", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(root.GetValue("Si Function Test Group")))
            {
                root.Add("Limit Partner Visibility", "Limit Partner Visibility");
            }
            else
            {
                root.Delete("Limit Partner Visibility");
            }

            if (root.GetValue("Visibility").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                root.Add("Visibility", "Active");
            }
            else
            {
                root.Delete("Visibility");
            }

            if (root.GetValue("SoftPaq In Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "SoftPaq In Preinstall");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", SoftPaq In Preinstall");
                }
            }

            if (root.GetValue("Rompaq Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(root.GetValue("ROM components")))
                {
                    root.Add("ROM components", "Binary");
                }
                else
                {
                    root.Add("ROM components", root.GetValue("ROM components") + ", Binary");
                }
            }

            if (GetCd(root).Equals(1))
            {
                if (string.IsNullOrEmpty(root.GetValue("Packaging")))
                {
                    root.Add("Packagings", "CD");
                }
                else
                {
                    root.Add("Packagings", root.GetValue("Packaging") + ", CD");
                }

                if (root.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    root.Add("CD Types : CD Files - Files copied from a CD will be released", "CD Types : CD Files - Files copied from a CD will be released");
                }
                else
                {
                    root.Delete("CD Types : CD Files - Files copied from a CD will be released");
                }

                if (root.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    root.Add("CD Types : Replicator Only - Only available from the Replicator", "CD Types : Replicator Only - Only available from the Replicator");
                }
                else
                {
                    root.Delete("CD Types : Replicator Only - Only available from the Replicator");
                }

                if (root.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    root.Add("CD Types : ISO Image -An ISO image of a CD will be released", "CD Types : ISO Image -An ISO image of a CD will be released");
                }
                else
                {
                    root.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
                }
            }
            else
            {
                root.Delete("CD Types : CD Files - Files copied from a CD will be released");
                root.Delete("CD Types : Replicator Only - Only available from the Replicator");
                root.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
                root.Delete("FTP Site");
            }



            root.Delete("CDImage");
            root.Delete("ISOImage");
            root.Delete("AR");
            root.Delete("Royalty Bearing");
            root.Delete("Korean Certification Required");
            root.Delete("Preinstall");
            root.Delete("DrDvd");
            root.Delete("Packaging Softpaq");
            root.Delete("Ms Store");
            root.Delete("Internal Tool");
            root.Delete("SoftPaq In Preinstall");
            root.Delete("Binary");
            root.Delete("ROM Components Preinstall");
            root.Delete("CAB");
            root.Delete("ROM component Softpaq");
            root.Delete("Desktop");
            root.Delete("Start Menu");
            root.Delete("System Tray");
            root.Delete("Control Panel");
            root.Delete("Info Center");
            root.Delete("Start Menu Tile");
            root.Delete("Taskbar Pinned Icon");
            root.Delete("FWML");
            root.Delete("UWP Compliant");
            root.Delete("Rompaq Binary");
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
                componentRoot.Add("Truly Linked Features Id " + i, trulyLinkedFeatures[componentRootId][i].Item1);
                componentRoot.Add("Truly Linked Features Name " + i, trulyLinkedFeatures[componentRootId][i].Item2);
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
                    root.Add("Truly Linked Features Id " + i, trulyLinkedFeatures[componentRootId][i].Item1);
                    root.Add("Truly Linked Features Name " + i, trulyLinkedFeatures[componentRootId][i].Item2);

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
                componentRoot.Add("Linked Features Id " + i, linkedFeatures[componentRootId][i].Item1);
                componentRoot.Add("Linked Features Name " + i, linkedFeatures[componentRootId][i].Item2);
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
                    root.Add("Linked Features Id " + i, linkedFeatures[componentRootId][i].Item1);
                    root.Add("Linked Features Name " + i, linkedFeatures[componentRootId][i].Item2);
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
                componentRoot.Add("Component Initiated Linkage Id " + i, componentInitiatedLinkage[componentRootId][i].Item1);
                componentRoot.Add("Component Initiated Linkage Name " + i, componentInitiatedLinkage[componentRootId][i].Item2);
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
                    root.Add("Component Initiated Linkage Id " + i, componentInitiatedLinkage[componentRootId][i].Item1);
                    root.Add("Component Initiated Linkage Name " + i, componentInitiatedLinkage[componentRootId][i].Item2);
                }
            }
        }
    }

    private static IEnumerable<CommonDataModel> DeleteProperty(IEnumerable<CommonDataModel> roots)
    {
        foreach (CommonDataModel root in roots)
        {
            DeleteProperty(root);
        }

        return roots;
    }

    private static CommonDataModel DeleteProperty(CommonDataModel root)
    {
        if (!string.Equals(root.GetValue("RequiredPrismSWType"), "True", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Prism SW Type");
        }

        if (string.Equals(root.GetValue("Component Type"), "Hardware", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Recovery Option");
        }

        if (string.Equals(root.GetValue("Abbreviation"), "SBD", StringComparison.OrdinalIgnoreCase))
        {
            root.Add("System Board", root.GetValue("Component Root Name"));
            root.Delete("Kit Number");
            root.Delete("Kit Description");
        }

        if (!string.Equals(root.GetValue("Agency Lead"), "WLAN", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Agency Lead");
        }

        if (string.Equals(root.GetValue("Component Type"), "Hardware", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Target Partition");
            root.Delete("Packagings");
            root.Delete("WHQL Certification Require");
            root.Delete("Touch Points");
            root.Delete("Other Setting");
            root.Delete("Transfer Server");
            root.Delete("Submission Path");
        }
        else
        {
            root.Delete("Special Notes");
            root.Delete("Kit Number");
            root.Delete("Kit Description");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Firmware", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("ROM Family");
            root.Delete("ROM Components");
        }
        else
        {
            root.Delete("Packagings");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Property Tabs Added");
        }

        if (!root.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("CD Types : Replicator Only - Only available from the Replicator", StringComparison.OrdinalIgnoreCase)
            && !root.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("CD Types : ISO Image -An ISO image of a CD will be released", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("CDs Replicated By");
        }

        if (root.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("CD Types : Replicator Only - Only available from the Replicator", StringComparison.OrdinalIgnoreCase)
            || root.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("CD Types : ISO Image -An ISO image of a CD will be released", StringComparison.OrdinalIgnoreCase)
            || root.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("CD Types : CD Files - Files copied from a CD will be released", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Submission Path");
        }

        if (root.GetValue("Packagings").Contains("Internal Tool"))
        {
            root.Delete("Touch Points");
            root.Delete("Other Setting");
        }

        if (root.GetValue("Packagings").Contains("CD"))
        {
            root.Delete("Submission Path");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Hardware", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(root.GetValue("Category"), "Base Unit", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("System Board ID");
        }

        root.Delete("Abbreviation");
        root.Delete("RequiredPrismSWType");

        return root;
    }

    private async Task FillFunctionalTestGroupOdmsAsync(IEnumerable<CommonDataModel> roots)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetFunctionalTestGroupOdmsCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> odm = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int componentRootId))
            {
                continue;
            }

            if (odm.ContainsKey(componentRootId))
            {
                odm[componentRootId].Add(reader["Partner Name"].ToString());
            }
            else
            {
                odm[componentRootId] = new List<string>() { reader["Partner Name"].ToString() };
            }
        }

        foreach (CommonDataModel root in roots)
        {
            if (int.TryParse(root.GetValue("Component Root Id"), out int componentRootId)
              && odm.ContainsKey(componentRootId))
            {
                for (int i = 0; i < odm[componentRootId].Count; i++)
                {
                    root.Add("Partners that can see this Component in Sudden Impact - Selected " + i, odm[componentRootId][i]);
                }
            }
        }
    }

    private async Task FillFunctionalTestGroupOdmsAsync(CommonDataModel root)
    {
        if (!int.TryParse(root.GetValue("Component Root Id"), out int componentRootId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetFunctionalTestGroupOdmsCommandText(), connection);
        SqlParameter parameter = new("ComponentRootId", componentRootId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, List<string>> odm = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentRootId"].ToString(), out int dbComponentRootId))
            {
                continue;
            }

            if (odm.ContainsKey(dbComponentRootId))
            {
                odm[dbComponentRootId].Add(reader["Partner Name"].ToString());
            }
            else
            {
                odm[dbComponentRootId] = new List<string>() { reader["Partner Name"].ToString() };
            }
        }

        if (odm.ContainsKey(componentRootId))
        {
            for (int i = 0; i < odm[componentRootId].Count; i++)
            {
                root.Add("Partners that can see this Component in Sudden Impact - Selected " + i, odm[componentRootId][i]);
            }
        }
    }
}
