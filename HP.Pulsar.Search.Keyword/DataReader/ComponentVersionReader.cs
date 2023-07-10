using System.Globalization;
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
    Dv.SampleDate as 'Samples Available Date',
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
        END AS 'Component Type',
    root.Softpaq As 'Rom Components Softpaq',
    CASE WHEN dv.IntroConfidence = 0 
            THEN ''
         WHEN dv.IntroConfidence = 1 
            THEN 'High'
         WHEN dv.IntroConfidence = 2 
            THEN 'Medium'
         WHEN dv.IntroConfidence = 3 
            THEN 'Low'
         END AS 'Confidence',
    CASE WHEN dv.SamplesConfidence = 0 
            THEN ''
         WHEN dv.SamplesConfidence = 1 
            THEN 'High'
         WHEN dv.SamplesConfidence = 2 
            THEN 'Medium'
         WHEN dv.SamplesConfidence = 3 
            THEN 'Low'
         END AS 'Samples Confidence',
    Dv.CVASubPath AS 'CVA Path',
    Dv.ServiceEOADate AS 'Service Team - Available Until Date',
    Dv.Path2Location As 'Component Location - FileName',
    cv.FactoryEOADate AS 'Engineering Team - Available Until Date'
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
LEFT JOIN ComponentVersion cv ON cv.componentversionid = Dv.ID 
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
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "Preinstall");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", Preinstall");
            }
        }

        if (componentVersion.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "DRDVD");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", DRDVD");
            }
        }

        if (componentVersion.GetValue("Softpaq").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "Softpaq");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", Softpaq");
            }
        }

        if (componentVersion.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "Ms Store");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", Ms Store");
            }
        }

        if (componentVersion.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "Internal Tool");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", Internal Tool");
            }
        }

        if (componentVersion.GetValue("ROM Component Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("ROM components")))
            {
                componentVersion.Add("ROM components", "Binary");
            }
            else
            {
                componentVersion.Add("ROM components", componentVersion.GetValue("ROM components") + ", Binary");
            }
        }

        if (componentVersion.GetValue("ROM Component Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("ROM components")))
            {
                componentVersion.Add("ROM components", "Preinstall");
            }
            else
            {
                componentVersion.Add("ROM components", componentVersion.GetValue("ROM components") + ", Preinstall");
            }
        }

        if (componentVersion.GetValue("Rom Components Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("ROM components")))
            {
                componentVersion.Add("ROM components", "Softpaq");
            }
            else
            {
                componentVersion.Add("ROM components", componentVersion.GetValue("ROM components") + ", Softpaq");
            }
        }

        if (componentVersion.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("ROM components")))
            {
                componentVersion.Add("ROM components", "CAB");
            }
            else
            {
                componentVersion.Add("ROM components", componentVersion.GetValue("ROM components") + ", CAB");
            }
        }

        if (componentVersion.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Other Setting")))
            {
                componentVersion.Add("Other Setting", "FWML");
            }
            else
            {
                componentVersion.Add("Other Settings", componentVersion.GetValue("Other Setting") + ", FWML");
            }
        }

        if (componentVersion.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Other Setting")))
            {
                componentVersion.Add("Other Setting", "UWP Compliant");
            }
            else
            {
                componentVersion.Add("Other Settings", componentVersion.GetValue("Other Setting") + ", UWP Compliant");
            }
        }

        if (componentVersion.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "Desktop");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", Desktop");
            }
        }

        if (componentVersion.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "Start Menu");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", Start Menu");
            }
        }

        if (componentVersion.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "System Tray");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", System Tray");
            }
        }

        if (componentVersion.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "Control Panel");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", Control Panel");
            }
        }

        if (componentVersion.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "Info Center");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", Info Center");
            }
        }

        if (componentVersion.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "Start Menu Tile");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", Start Menu Tile");
            }
        }

        if (componentVersion.GetValue("Task Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Touch Points")))
            {
                componentVersion.Add("Touch Points", "Taskbar Pinned Icon");
            }
            else
            {
                componentVersion.Add("Touch Points", componentVersion.GetValue("Touch Points") + ", Taskbar Pinned Icon");
            }
        }

        if (componentVersion.GetValue("SoftPaq In Preinstall").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "SoftPaq In Preinstall");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", SoftPaq In Preinstall");
            }
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
            if (string.IsNullOrEmpty(componentVersion.GetValue("Packaging")))
            {
                componentVersion.Add("Packagings", "CD");
            }
            else
            {
                componentVersion.Add("Packaging", componentVersion.GetValue("Packaging") + ", CD");
            }
        }
        componentVersion.Delete("CDImage");
        componentVersion.Delete("ISOImage");
        componentVersion.Delete("AR");
        componentVersion.Delete("Packaging Preinstall");
        componentVersion.Delete("DrDvd");
        componentVersion.Delete("Softpaq");
        componentVersion.Delete("Ms Store");
        componentVersion.Delete("Internal Tool");
        componentVersion.Delete("ROM Component Binary");
        componentVersion.Delete("ROM Component Preinstall");
        componentVersion.Delete("CAB");
        componentVersion.Delete("FWML");
        componentVersion.Delete("UWP Compliant");
        componentVersion.Delete("Desktop");
        componentVersion.Delete("Start Menu");
        componentVersion.Delete("System Tray");
        componentVersion.Delete("Control Panel");
        componentVersion.Delete("Info Center");
        componentVersion.Delete("Start Menu Tile");
        componentVersion.Delete("Task Pinned Icon");
        componentVersion.Delete("SoftPaq In Preinstall");
        componentVersion.Delete("Rom Components Softpaq");
        return componentVersion;
    }

    private static Task HandlePropertyValuesAsync(IEnumerable<CommonDataModel> componentVersions)
    {
        foreach (CommonDataModel version in componentVersions)
        {
            if (version.GetValue("Packaging Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "Preinstall");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", Preinstall");
                }
            }

            if (version.GetValue("DrDvd").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "DRDVD");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", DRDVD");
                }
            }

            if (version.GetValue("Softpaq").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "Softpaq");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", Softpaq");
                }
            }

            if (version.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "Ms Store");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", Ms Store");
                }
            }

            if (version.GetValue("Internal Tool").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "Internal Tool");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", Internal Tool");
                }
            }

            if (version.GetValue("ROM Component Binary").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("ROM components")))
                {
                    version.Add("ROM components", "Binary");
                }
                else
                {
                    version.Add("ROM components", version.GetValue("ROM components") + ", Binary");
                }
            }

            if (version.GetValue("ROM Component Preinstall").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("ROM components")))
                {
                    version.Add("ROM components", "Preinstall");
                }
                else
                {
                    version.Add("ROM components", version.GetValue("ROM components") + ", Preinstall");
                }
            }

            if (version.GetValue("CAB").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("ROM components")))
                {
                    version.Add("ROM components", "CAB");
                }
                else
                {
                    version.Add("ROM components", version.GetValue("ROM components") + ", CAB");
                }
            }

            if (version.GetValue("Rom Components Softpaq").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("ROM components")))
                {
                    version.Add("ROM components", "Softpaq");
                }
                else
                {
                    version.Add("ROM components", version.GetValue("ROM components") + ", Softpaq");
                }
            }

            if (version.GetValue("FWML").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Other Setting")))
                {
                    version.Add("Other Setting", "FWML");
                }
                else
                {
                    version.Add("Other Settings", version.GetValue("Other Setting") + ", FWML");
                }
            }

            if (version.GetValue("UWP Compliant").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Other Setting")))
                {
                    version.Add("Other Setting", "UWP Compliant");
                }
                else
                {
                    version.Add("Other Settings", version.GetValue("Other Setting") + ", UWP Compliant");
                }
            }

            if (version.GetValue("Desktop").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "Desktop");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", Desktop");
                }
            }

            if (version.GetValue("Start Menu").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "Start Menu");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", Start Menu");
                }
            }

            if (version.GetValue("System Tray").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "System Tray");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", System Tray");
                }
            }

            if (version.GetValue("Control Panel").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "Control Panel");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", Control Panel");
                }
            }

            if (version.GetValue("Info Center").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "Info Center");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", Info Center");
                }
            }

            if (version.GetValue("Start Menu Tile").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "Start Menu Tile");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", Start Menu Tile");
                }
            }

            if (version.GetValue("Taskbar Pinned Icon").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Touch Points")))
                {
                    version.Add("Touch Points", "Taskbar Pinned Icon");
                }
                else
                {
                    version.Add("Touch Points", version.GetValue("Touch Points") + ", Taskbar Pinned Icon");
                }
            }

            if (version.GetValue("SoftPaq In Preinstall").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "SoftPaq In Preinstall");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", SoftPaq In Preinstall");
                }
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
                if (string.IsNullOrEmpty(version.GetValue("Packaging")))
                {
                    version.Add("Packagings", "CD");
                }
                else
                {
                    version.Add("Packaging", version.GetValue("Packaging") + ", CD");
                }
            }
            version.Delete("CDImage");
            version.Delete("ISOImage");
            version.Delete("AR");
            version.Delete("Packaging Preinstall");
            version.Delete("DrDvd");
            version.Delete("Softpaq");
            version.Delete("Ms Store");
            version.Delete("Internal Tool");
            version.Delete("ROM Component Binary");
            version.Delete("ROM Component Preinstall");
            version.Delete("CAB");
            version.Delete("FWML");
            version.Delete("UWP Compliant");
            version.Delete("Desktop");
            version.Delete("Start Menu");
            version.Delete("System Tray");
            version.Delete("Control Panel");
            version.Delete("Info Center");
            version.Delete("Start Menu Tile");
            version.Delete("Taskbar Pinned Icon");
            version.Delete("SoftPaq In Preinstall");
            version.Delete("Rom Components Softpaq");
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
            componentVersion.Add("Mass Production Date", componentVersion.GetValue("Intro Date"));
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
                version.Add("Mass Production Date", version.GetValue("Intro Date"));
            }

            version.Delete("Version");
            version.Delete("Revision");
            version.Delete("Pass");
            version.Delete("Intro Date");
        }

        return Task.CompletedTask;
    }
}
