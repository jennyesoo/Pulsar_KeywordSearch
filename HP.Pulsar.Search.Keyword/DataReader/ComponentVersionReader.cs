using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class ComponentVersionReader : IKeywordSearchDataReader
{
    private readonly KeywordSearchInfo _info;

    public ComponentVersionReader(KeywordSearchInfo info)
    {
        _info = info;
    }

    public async Task<CommonDataModel> GetDataAsync(int componentVersionsId)
    {
        CommonDataModel componentVersion = await GetComponentVersionAsync(componentVersionsId);

        if (!componentVersion.GetElements().Any())
        {
            return null;
        }

        HandlePropertyValue(componentVersion);
        HandleDifferentPropertyNameBasedOnCategory(componentVersion);
        return componentVersion;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> componentVersions = await GetComponentVersionAsync();

        List<Task> tasks = new()
            {
                HandlePropertyValuesAsync(componentVersions),
                HandleDifferentPropertyNameBasedOnCategoryAsync(componentVersions)
            };
        await Task.WhenAll(tasks);
        return componentVersions;
    }

    private async Task<CommonDataModel> GetComponentVersionAsync(int componentVersionsId)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        SqlCommand command = new(GetTSQLComponentVersionCommandText(), connection);
        SqlParameter parameter = new("ComponentVersionId", componentVersionsId);
        command.Parameters.Add(parameter);
        await connection.OpenAsync();
        using SqlDataReader reader = command.ExecuteReader();

        CommonDataModel componentVersion = new();
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

                if (columnName.Equals(TargetName.ComponentVersion, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                componentVersion.Add(columnName, value);
            }

            componentVersion.Add("Target", TargetTypeValue.ComponentVersion);
            componentVersion.Add("Id", SearchIdName.ComponentVersion + componentVersion.GetValue("ComponentVersionID"));

        }

        return componentVersion;
    }

    private async Task<IEnumerable<CommonDataModel>> GetComponentVersionAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        SqlCommand command = new(GetTSQLComponentVersionCommandText(),
                                connection);
        SqlParameter parameter = new("ComponentVersionId", -1);
        command.Parameters.Add(parameter);
        await connection.OpenAsync();
        using SqlDataReader reader = command.ExecuteReader();
        List<CommonDataModel> output = new();

        while (await reader.ReadAsync())
        {
            CommonDataModel componentVersion = new();
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

                if (columnName.Equals(TargetName.ComponentVersion, StringComparison.OrdinalIgnoreCase))
                {
                    columnName = "Name";
                }

                componentVersion.Add(columnName, value);
            }

            componentVersion.Add("Target", TargetTypeValue.ComponentVersion);
            componentVersion.Add("Id", SearchIdName.ComponentVersion + componentVersion.GetValue("ComponentVersionID"));
            output.Add(componentVersion);
        }
        return output;
    }

    private static string GetTSQLComponentVersionCommandText()
    {
        return @"SELECT Dv.ID AS ComponentVersionID,
    Dv.DeliverableRootID AS ComponentRootID,
    Dv.DeliverableName AS ComponentVersionName,
    Dv.Version,
    Dv.Revision,
    CPSW.Description AS PrismSWType,
    Dv.Pass,
    user1.FirstName + ' ' + user1.LastName AS Developer,
    user1.Email AS DeveloperEmail,
    user2.FirstName + ' ' + user2.LastName AS TestLead,
    user2.Email AS TestLeadEmail,
    v.Name AS Vendor,
    Dv.IRSPartNumber AS SWPartNumber,
    cbl.Name AS BuildLevel,
    sws.DisplayName AS RecoveryOption,
    Dv.MD5,
    Dv.SHA256,
    Dv.PropertyTabs,
    Dv.Preinstall,
    Dv.DrDvd,
    Dv.Scriptpaq,
    Dv.MsStore,
    Dv.FloppyDisk,
    Dv.CDImage,
    Dv.ISOImage,
    Dv.AR,
    Dv.IconDesktop,
    Dv.IconMenu,
    Dv.IconTray,
    Dv.IconPanel,
    Dv.IconInfoCenter,
    Dv.IconTile,
    Dv.IconTaskBarIcon,
    Dv.SettingFWML,
    Dv.SettingUWPCompliant,
    Dv.Active AS Visibility,
    cts.Name AS TransferServer,
    Dv.SubmissionPath,
    Dv.VendorVersion,
    Dv.Comments,
    Dv.EndOfLifeDate,
    Dv.Rompaq,
    Dv.PreinstallROM,
    Dv.CAB,
    Dv.IsSoftPaqInPreinstall,
    Dv.SampleDate,
    Dv.ModelNumber,
    Dv.PartNumber,
    Dv.CodeName,
    gs.Name AS GreenSpecLevel,
    Dv.IntroDate AS MassProduction,
    CASE 
        WHEN root.TypeID = 1
            THEN 'Hardware'
        WHEN root.TypeID = 2
            THEN 'Software'
        WHEN root.TypeID = 3
            THEN 'Firmware'
        WHEN root.TypeID = 4
            THEN 'Documentation'
        WHEN root.TypeID = 5
            THEN 'Image'
        WHEN root.TypeID = 6
            THEN 'Certification'
        WHEN root.TypeID = 7
            THEN 'Softpaq'
        WHEN root.TypeID = 8
            THEN 'Factory'
        END AS ComponentType
FROM DeliverableVersion Dv
LEFT JOIN ComponentPrismSWType CPSW ON CPSW.PRISMTypeID = Dv.PrismSWType
LEFT JOIN userinfo user1 ON user1.userid = Dv.DeveloperID
LEFT JOIN userinfo user2 ON user2.userid = Dv.TestLeadId
LEFT JOIN Vendor v ON v.ID = Dv.VendorID
LEFT JOIN ComponentBuildLevel cbl ON cbl.ComponentBuildLevelId = Dv.LevelID
LEFT JOIN SWSetupCategory sws ON sws.ID = Dv.SWSetupCategoryID
LEFT JOIN ComponentTransferServer cts ON cts.Id = Dv.TransferServerId
LEFT JOIN GreenSpec gs ON gs.id = Dv.GreenSpecID
LEFT JOIN DeliverableRoot root ON root.id = Dv.DeliverableRootID
WHERE (
        @ComponentVersionId = - 1
        OR Dv.ID = @ComponentVersionId
        )

";
    }

    private static CommonDataModel HandlePropertyValue(CommonDataModel componentVersion)
    {
        if (componentVersion.GetValue("Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Preinstall", "Packaging Preinstall");
        }
        else
        {
            componentVersion.Delete("Preinstall");
        }

        if (componentVersion.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("DrDvd", "DRDVD");
        }
        else
        {
            componentVersion.Delete("DrDvd");
        }

        if (componentVersion.GetValue("Scriptpaq").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Scriptpaq", "Packaging Softpaq");
        }
        else
        {
            componentVersion.Delete("Scriptpaq");
        }


        if (componentVersion.GetValue("MsStore").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("MsStore", "Ms Store");
        }
        else
        {
            componentVersion.Delete("MsStore");
        }

        if (componentVersion.GetValue("FloppyDisk").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("FloppyDisk", "Internal Tool");
        }
        else
        {
            componentVersion.Delete("FloppyDisk");
        }

        if (componentVersion.GetValue("Rompaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Rompaq", "ROM Component Binary");
        }
        else
        {
            componentVersion.Delete("Rompaq");
        }

        if (componentVersion.GetValue("PreinstallROM").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("PreinstallROM", "ROM Component Preinstall");
        }
        else
        {
            componentVersion.Delete("PreinstallROM");
        }

        if (componentVersion.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("CAB", "CAB");
        }
        else
        {
            componentVersion.Delete("CAB");
        }

        if (componentVersion.GetValue("SettingFWML").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("SettingFWML", "FWML");
        }
        else
        {
            componentVersion.Delete("SettingFWML");
        }

        if (componentVersion.GetValue("SettingUWPCompliant").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("SettingUWPCompliant", "UWP Compliant");
        }
        else
        {
            componentVersion.Delete("SettingUWPCompliant");
        }

        if (componentVersion.GetValue("IconDesktop").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconDesktop", "Desktop");
        }
        else
        {
            componentVersion.Delete("IconDesktop");
        }

        if (componentVersion.GetValue("IconMenu").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconMenu", "Start Menu");
        }
        else
        {
            componentVersion.Delete("IconMenu");
        }

        if (componentVersion.GetValue("IconTray").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconTray", "System Tray");
        }
        else
        {
            componentVersion.Delete("IconTray");
        }

        if (componentVersion.GetValue("IconPanel").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconPanel", "Control Panel");
        }
        else
        {
            componentVersion.Delete("IconPanel");
        }

        if (componentVersion.GetValue("IconInfoCenter").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconInfoCenter", "Info Center");
        }
        else
        {
            componentVersion.Delete("IconInfoCenter");
        }

        if (componentVersion.GetValue("IconTile").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconTile", "Start Menu Tile");
        }
        else
        {
            componentVersion.Delete("IconTile");
        }

        if (componentVersion.GetValue("IconTaskBarIcon").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IconTaskBarIcon", "Task Pinned Icon");
        }
        else
        {
            componentVersion.Delete("IconTaskBarIcon");
        }

        if (componentVersion.GetValue("IsSoftPaqInPreinstall").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("IsSoftPaqInPreinstall", "SoftPaq In Preinstall");
        }
        else
        {
            componentVersion.Delete("IsSoftPaqInPreinstall");
        }

        if (componentVersion.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Visibility", "Active");
        }
        else
        {
            componentVersion.Delete("Visibility");
        }

        if (GetCdAsync(componentVersion).Equals(1))
        {
            componentVersion.Add("CD", "CD");
        }
        componentVersion.Delete("CDImage");
        componentVersion.Delete("ISOImage");
        componentVersion.Delete("AR");
        return componentVersion;
    }

    private static Task HandlePropertyValuesAsync(IEnumerable<CommonDataModel> componentVersions)
    {
        foreach (CommonDataModel rootversion in componentVersions)
        {
            if (rootversion.GetValue("Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("Preinstall", "Packaging Preinstall");
            }
            else
            {
                rootversion.Delete("Preinstall");
            }

            if (rootversion.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("DrDvd", "DRDVD");
            }
            else
            {
                rootversion.Delete("DrDvd");
            }

            if (rootversion.GetValue("Scriptpaq").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("Scriptpaq", "Packaging Softpaq");
            }
            else
            {
                rootversion.Delete("Scriptpaq");
            }


            if (rootversion.GetValue("MsStore").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("MsStore", "Ms Store");
            }
            else
            {
                rootversion.Delete("MsStore");
            }

            if (rootversion.GetValue("FloppyDisk").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("FloppyDisk", "Internal Tool");
            }
            else
            {
                rootversion.Delete("FloppyDisk");
            }

            if (rootversion.GetValue("Rompaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("Rompaq", "ROM Component Binary");
            }
            else
            {
                rootversion.Delete("Rompaq");
            }

            if (rootversion.GetValue("PreinstallROM").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("PreinstallROM", "ROM Component Preinstall");
            }
            else
            {
                rootversion.Delete("PreinstallROM");
            }

            if (rootversion.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("CAB", "CAB");
            }
            else
            {
                rootversion.Delete("CAB");
            }

            if (rootversion.GetValue("SettingFWML").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("SettingFWML", "FWML");
            }
            else
            {
                rootversion.Delete("SettingFWML");
            }

            if (rootversion.GetValue("SettingUWPCompliant").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("SettingUWPCompliant", "UWP Compliant");
            }
            else
            {
                rootversion.Delete("SettingUWPCompliant");
            }

            if (rootversion.GetValue("IconDesktop").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconDesktop", "Desktop");
            }
            else
            {
                rootversion.Delete("IconDesktop");
            }

            if (rootversion.GetValue("IconMenu").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconMenu", "Start Menu");
            }
            else
            {
                rootversion.Delete("IconMenu");
            }

            if (rootversion.GetValue("IconTray").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconTray", "System Tray");
            }
            else
            {
                rootversion.Delete("IconTray");
            }

            if (rootversion.GetValue("IconPanel").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconPanel", "Control Panel");
            }
            else
            {
                rootversion.Delete("IconPanel");
            }

            if (rootversion.GetValue("IconInfoCenter").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconInfoCenter", "Info Center");
            }
            else
            {
                rootversion.Delete("IconInfoCenter");
            }

            if (rootversion.GetValue("IconTile").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconTile", "Start Menu Tile");
            }
            else
            {
                rootversion.Delete("IconTile");
            }

            if (rootversion.GetValue("IconTaskBarIcon").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IconTaskBarIcon", "Task Pinned Icon");
            }
            else
            {
                rootversion.Delete("IconTaskBarIcon");
            }

            if (rootversion.GetValue("IsSoftPaqInPreinstall").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("IsSoftPaqInPreinstall", "SoftPaq In Preinstall");
            }
            else
            {
                rootversion.Delete("IsSoftPaqInPreinstall");
            }

            if (rootversion.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                rootversion.Add("Visibility", "Active");
            }
            else
            {
                rootversion.Delete("Visibility");
            }

            if (GetCdAsync(rootversion).Equals(1))
            {
                rootversion.Add("CD", "CD");
            }
            rootversion.Delete("CDImage");
            rootversion.Delete("ISOImage");
            rootversion.Delete("AR");
        }

        return Task.CompletedTask;
    }

    private static Task<int> GetCdAsync(CommonDataModel rootversion)
    {
        if (rootversion.GetValue("CDImage").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(1);
        }

        if (rootversion.GetValue("ISOImage").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(1);
        }

        if (rootversion.GetValue("AR").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    private CommonDataModel HandleDifferentPropertyNameBasedOnCategory(CommonDataModel componentVersion)
    {
        if (!componentVersion.GetValue("ComponentType").Equals("Hardware"))
        {
            return componentVersion;
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Version")))
        {
            componentVersion.Add("HardwareVersion", componentVersion.GetValue("Version"));
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Revision")))
        {
            componentVersion.Add("FirmwareVersion", componentVersion.GetValue("Revision"));
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Pass")))
        {
            componentVersion.Add("Rev", componentVersion.GetValue("Pass"));
        }

        componentVersion.Delete("Version");
        componentVersion.Delete("Revision");
        componentVersion.Delete("Pass");

        return componentVersion;
    }

    private Task HandleDifferentPropertyNameBasedOnCategoryAsync(IEnumerable<CommonDataModel> componentVersions)
    {
        foreach (CommonDataModel rootversion in componentVersions)
        {
            if (!rootversion.GetValue("ComponentType").Equals("Hardware"))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(rootversion.GetValue("Version")))
            {
                rootversion.Add("HardwareVersion", rootversion.GetValue("Version"));
            }

            if (!string.IsNullOrWhiteSpace(rootversion.GetValue("Revision")))
            {
                rootversion.Add("FirmwareVersion", rootversion.GetValue("Revision"));
            }

            if (!string.IsNullOrWhiteSpace(rootversion.GetValue("Pass")))
            {
                rootversion.Add("Rev", rootversion.GetValue("Pass"));
            }

            rootversion.Delete("Version");
            rootversion.Delete("Revision");
            rootversion.Delete("Pass");
        }

        return Task.CompletedTask;
    }
}
