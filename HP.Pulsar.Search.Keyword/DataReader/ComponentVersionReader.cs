﻿using System.Globalization;
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
            componentVersion.Add("Id", SearchIdName.ComponentVersion + componentVersion.GetValue("Component Version ID"));

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
            componentVersion.Add("Id", SearchIdName.ComponentVersion + componentVersion.GetValue("Component Version ID"));
            output.Add(componentVersion);
        }
        return output;
    }

    private static string GetTSQLComponentVersionCommandText()
    {
        return @"SELECT Dv.ID AS 'Component Version ID',
    Dv.DeliverableRootID AS 'Component Root ID',
    Dv.DeliverableName AS 'Component Version Name',
    Dv.Version,
    Dv.Revision,
    CPSW.Description AS 'Prism SW Type',
    Dv.Pass,
    user1.FirstName + ' ' + user1.LastName AS Developer,
    user1.Email AS 'Developer Email',
    user2.FirstName + ' ' + user2.LastName AS 'Test Lead',
    user2.Email AS 'Test Lead Email',
    v.Name AS Vendor,
    Dv.IRSPartNumber AS 'SW Part Number',
    cbl.Name AS 'Build Level',
    sws.DisplayName AS 'Recovery Option',
    Dv.MD5,
    Dv.SHA256,
    Dv.PropertyTabs AS 'Property Tabs Added',
    Dv.Preinstall as 'Packaging Preinstall',
    Dv.DrDvd,
    Dv.Scriptpaq as 'Softpaq', 
    Dv.MsStore as 'Ms Store',
    Dv.FloppyDisk as 'Internal Tool',
    Dv.CDImage,
    Dv.ISOImage,
    Dv.AR,
    Dv.IconDesktop as 'Desktop',
    Dv.IconMenu as 'Start Menu',
    Dv.IconTray as 'System Tray',
    Dv.IconPanel as 'Control Panel',
    Dv.IconInfoCenter as 'Info Center',
    Dv.IconTile as 'Start Menu Tile',
    Dv.IconTaskBarIcon as 'Taskbar Pinned Icon',
    Dv.SettingFWML as 'FWML',
    Dv.SettingUWPCompliant as 'UWP Compliant',
    Dv.Active AS Visibility,
    cts.Name AS 'Transfer Server',
    Dv.SubmissionPath as 'Submission Path',
    Dv.VendorVersion as 'Vendor Version',
    Dv.Comments,
    Dv.EndOfLifeDate as 'End Of Life Date',
    Dv.Binary as 'ROM Component Binary',
    Dv.PreinstallROM as 'ROM Component Preinstall',
    Dv.CAB,
    Dv.IsSoftPaqInPreinstall as 'SoftPaq In Preinstall',
    Dv.SampleDate as 'Samples Available',
    Dv.ModelNumber as 'Model Number',
    Dv.PartNumber as 'HP Part Number',
    Dv.CodeName as 'Code Name',
    gs.Name AS 'Green Spec Level',
    Dv.IntroDate AS 'Intro Date',
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
        if (componentVersion.GetValue("Packaging Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Packaging Preinstall", "Packaging Preinstall");
        }
        else
        {
            componentVersion.Delete("Packaging Preinstall");
        }

        if (componentVersion.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("DrDvd", "DRDVD");
        }
        else
        {
            componentVersion.Delete("DrDvd");
        }

        if (componentVersion.GetValue("Softpaq").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Softpaq", "Packaging Softpaq");
        }
        else
        {
            componentVersion.Delete("Softpaq");
        }


        if (componentVersion.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Ms Store", "Ms Store");
        }
        else
        {
            componentVersion.Delete("Ms Store");
        }

        if (componentVersion.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Internal Tool", "Internal Tool");
        }
        else
        {
            componentVersion.Delete("Internal Tool");
        }

        if (componentVersion.GetValue("ROM Component Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("ROM Component Binary", "ROM Component Binary");
        }
        else
        {
            componentVersion.Delete("ROM Component Binary");
        }

        if (componentVersion.GetValue("ROM Component Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("ROM Component Preinstall", "ROM Component Preinstall");
        }
        else
        {
            componentVersion.Delete("ROM Component Preinstall");
        }

        if (componentVersion.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("CAB", "CAB");
        }
        else
        {
            componentVersion.Delete("CAB");
        }

        if (componentVersion.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("FWML", "FWML");
        }
        else
        {
            componentVersion.Delete("FWML");
        }

        if (componentVersion.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("UWP Compliant", "UWP Compliant");
        }
        else
        {
            componentVersion.Delete("UWP Compliant");
        }

        if (componentVersion.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Desktop", "Desktop");
        }
        else
        {
            componentVersion.Delete("Desktop");
        }

        if (componentVersion.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Start Menu", "Start Menu");
        }
        else
        {
            componentVersion.Delete("Start Menu");
        }

        if (componentVersion.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("System Tray", "System Tray");
        }
        else
        {
            componentVersion.Delete("System Tray");
        }

        if (componentVersion.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Control Panel", "Control Panel");
        }
        else
        {
            componentVersion.Delete("Control Panel");
        }

        if (componentVersion.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Info Center", "Info Center");
        }
        else
        {
            componentVersion.Delete("Info Center");
        }

        if (componentVersion.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Start Menu Tile", "Start Menu Tile");
        }
        else
        {
            componentVersion.Delete("Start Menu Tile");
        }

        if (componentVersion.GetValue("Task Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("Task Pinned Icon", "Task Pinned Icon");
        }
        else
        {
            componentVersion.Delete("Task Pinned Icon");
        }

        if (componentVersion.GetValue("SoftPaq In Preinstall").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("SoftPaq In Preinstall", "SoftPaq In Preinstall");
        }
        else
        {
            componentVersion.Delete("SoftPaq In Preinstall");
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
        foreach (CommonDataModel version in componentVersions)
        {
            if (version.GetValue("Packaging Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Packaging Preinstall", "Packaging Preinstall");
            }
            else
            {
                version.Delete("Packaging Preinstall");
            }

            if (version.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("DrDvd", "DRDVD");
            }
            else
            {
                version.Delete("DrDvd");
            }

            if (version.GetValue("Softpaq").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Softpaq", "Packaging Softpaq");
            }
            else
            {
                version.Delete("Softpaq");
            }


            if (version.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Ms Store", "Ms Store");
            }
            else
            {
                version.Delete("Ms Store");
            }

            if (version.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Internal Tool", "Internal Tool");
            }
            else
            {
                version.Delete("Internal Tool");
            }

            if (version.GetValue("ROM Component Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("ROM Component Binary", "ROM Component Binary");
            }
            else
            {
                version.Delete("ROM Component Binary");
            }

            if (version.GetValue("ROM Component Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("ROM Component Preinstall", "ROM Component Preinstall");
            }
            else
            {
                version.Delete("ROM Component Preinstall");
            }

            if (version.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("CAB", "CAB");
            }
            else
            {
                version.Delete("CAB");
            }

            if (version.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("FWML", "FWML");
            }
            else
            {
                version.Delete("FWML");
            }

            if (version.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("UWP Compliant", "UWP Compliant");
            }
            else
            {
                version.Delete("UWP Compliant");
            }

            if (version.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Desktop", "Desktop");
            }
            else
            {
                version.Delete("Desktop");
            }

            if (version.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Start Menu", "Start Menu");
            }
            else
            {
                version.Delete("Start Menu");
            }

            if (version.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("System Tray", "System Tray");
            }
            else
            {
                version.Delete("System Tray");
            }

            if (version.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Control Panel", "Control Panel");
            }
            else
            {
                version.Delete("Control Panel");
            }

            if (version.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Info Center", "Info Center");
            }
            else
            {
                version.Delete("Info Center");
            }

            if (version.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Start Menu Tile", "Start Menu Tile");
            }
            else
            {
                version.Delete("Start Menu Tile");
            }

            if (version.GetValue("Task Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Task Pinned Icon", "Task Pinned Icon");
            }
            else
            {
                version.Delete("Task Pinned Icon");
            }

            if (version.GetValue("SoftPaq In Preinstall").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("SoftPaq In Preinstall", "SoftPaq In Preinstall");
            }
            else
            {
                version.Delete("SoftPaq In Preinstall");
            }

            if (version.GetValue("Visibility").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("Visibility", "Active");
            }
            else
            {
                version.Delete("Visibility");
            }

            if (GetCdAsync(version).Equals(1))
            {
                version.Add("CD", "CD");
            }
            version.Delete("CDImage");
            version.Delete("ISOImage");
            version.Delete("AR");
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
        if (!componentVersion.GetValue("Component Type").Equals("Hardware"))
        {
            return componentVersion;
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Version")))
        {
            componentVersion.Add("Hardware Version", componentVersion.GetValue("Version"));
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Revision")))
        {
            componentVersion.Add("Firmware Version", componentVersion.GetValue("Revision"));
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Pass")))
        {
            componentVersion.Add("Rev", componentVersion.GetValue("Pass"));
        }

        if (!string.IsNullOrWhiteSpace(componentVersion.GetValue("Intro Date")))
        {
            componentVersion.Add("Mass Production", componentVersion.GetValue("Intro Date"));
        }

        componentVersion.Delete("Version");
        componentVersion.Delete("Revision");
        componentVersion.Delete("Pass");
        componentVersion.Delete("Intro Date");

        return componentVersion;
    }

    private Task HandleDifferentPropertyNameBasedOnCategoryAsync(IEnumerable<CommonDataModel> componentVersions)
    {
        foreach (CommonDataModel version in componentVersions)
        {
            if (!version.GetValue("Component Type").Equals("Hardware"))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(version.GetValue("Version")))
            {
                version.Add("Hardware Version", version.GetValue("Version"));
            }

            if (!string.IsNullOrWhiteSpace(version.GetValue("Revision")))
            {
                version.Add("Firmware Version", version.GetValue("Revision"));
            }

            if (!string.IsNullOrWhiteSpace(version.GetValue("Pass")))
            {
                version.Add("Rev", version.GetValue("Pass"));
            }

            if (!string.IsNullOrWhiteSpace(version.GetValue("Intro Date")))
            {
                version.Add("Mass Production", version.GetValue("Intro Date"));
            }

            version.Delete("Version");
            version.Delete("Revision");
            version.Delete("Pass");
            version.Delete("Intro Date");
        }

        return Task.CompletedTask;
    }
}
