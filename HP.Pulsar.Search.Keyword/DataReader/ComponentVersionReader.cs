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

        List<Task> tasks = new()
            {
                FillSystemBoardAsync(componentVersion),
                FillIsWorkflowCompletedAsync(componentVersion)
            };

        await Task.WhenAll(tasks);

        HandlePropertyValue(componentVersion);
        HandleDifferentPropertyNameBasedOnCategory(componentVersion);
        DeleteProperty(componentVersion);

        return componentVersion;
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        IEnumerable<CommonDataModel> componentVersions = await GetComponentVersionAsync();

        List<Task> tasks = new()
            {
                HandlePropertyValuesAsync(componentVersions),
                HandleDifferentPropertyNameBasedOnCategoryAsync(componentVersions),
                FillSystemBoardAsync(componentVersions),
                FillIsWorkflowCompletedAsync(componentVersions)
            };

        await Task.WhenAll(tasks);

        DeleteProperty(componentVersions);

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
    cv.Softpaq, 
    Dv.MsStore as 'Ms Store',
    cv.InternalTool as 'Internal Tool',
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
    cv.FactoryEOADate AS 'Engineering Team - Available Until Date',
    CASE WHEN Dv.BiosIntegrationType = 1 Then 'DPTF'
        WHEN Dv.BiosIntegrationType = 2 Then 'HW'
        WHEN Dv.BiosIntegrationType = 3 Then 'COMM'
        WHEN Dv.BiosIntegrationType = 4 Then 'Thermal'
        WHEN Dv.BiosIntegrationType = 5 Then 'Audio'
        WHEN Dv.BiosIntegrationType = 6 Then 'Power'
        WHEN Dv.BiosIntegrationType = 7 Then 'F10'
        WHEN Dv.BiosIntegrationType is null Then ''
        End AS 'This is for BIOS Integration',
    DV.HFCN AS 'This is an HFCN release',
    cate.Abbreviation,
    cate.FccRequired,
    cate.RequiredPrismSWType,
    cv.CDFiles AS 'CD Types : CD Files - Files copied from a CD will be released',
    Dv.IsoImage AS 'CD Types : ISO Image -An ISO image of a CD will be released',
    Dv.Ar AS 'CD Types : Replicator Only - Only available from the Replicator',
    cv.SWPartNumber,
    Dv.FtpSitePath AS 'FTP Site',
    Dv.Replicater AS 'CDs Replicated By',
    Dv.CDKitNumber AS 'Kit Number',
    Dv.CdPartNumber AS 'CD/DVD Part Number',
    Dv.KoreanCertificationId,
    Dv.KoreanCertificationRequired,
    cate.RequiresTTS,
    Dv.edid + ' MHZ' AS 'WWAN EDID',
    Dv.TTS AS 'WWAN TTS Results',
    Dv.WWANTestSpecRev AS 'WWAN TTS Spec Rev',
    cate.TeamID,
    Dv.SecondaryRFKill AS 'RF Kill Mechanism',
    Dv.FCCID AS 'FCC ID',
    Dv.Anatel,
    Dv.ICASA,
    Dv.Rompaq AS 'Rom Component Binary(Rompaq)',
    Dv.Patch,
    Case When Dv.TransferServerId > 0 Then 'True'
            WHEN Dv.TransferServerId <= 0 Then 'False'
            WHEN Dv.TransferServerId is null Then 'False'
        End AS 'HidePrism',
    Case When cate.IrsCategoryId is null Then 'True'
             When cate.IrsCategoryId > 0 Then 'False'
             When cate.IrsCategoryId <= 0 Then 'True'
        End AS IrsCategoryHidePrism,

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
LEFT JOIN componentCategory cate ON cate.CategoryId = root.categoryid
LEFT JOIN ComponentVersion cv on cv.ComponentVersionid = Dv.id 
WHERE (
        @ComponentVersionId = - 1
        OR Dv.ID = @ComponentVersionId
        )
";
    }

    private static string GetSystemBoardCommandText()
    {
        return @"
select Dv.ID As ComponentVersionID,
    sysBoard.SystemBoard AS 'System Board',
    sysBoard.ProductFamily AS 'Product Families (Intro Year)',
    sysBoard.SystemId AS 'ROM System Board ID (Hex)',
    V.VendorPartNumber AS 'Vendor Part Number',
    V.MaxMemorySize + V.MaxMemorySizeUnit AS 'Maximum Memory Size',
    V.CPUSpeed + V.CPUSpeedUnit As 'CPU Speed',
    V.ReworkDescription AS 'Rework Description',
    V.ID AS 'SBHardwareComponentId'
from DeliverableVersion Dv 
left join SBHardwareComponent V on V.DeliverableVersionId = Dv.ID
left join PlatformAndSystemBoard sysBoard on sysBoard.SystemBoard = Dv.DeliverableName
left join DeliverableRoot root on root.id = Dv.DeliverableRootID
left JOIN componentCategory cate ON cate.CategoryId = root.categoryid
where cate.Abbreviation = 'SBD'
        AND (
                @ComponentVersionId = - 1
                OR Dv.ID = @ComponentVersionId
                )
order by sysBoard.SystemId desc
";
    }


    private static string GetIsWorkflowCompletedCommandText()
    {
        return @"
select  ws.ComponentVersionID,
        Case When ws.StatusID = 3 Then 'true'
        When ws.StatusID !=3 Then 'false'
        End AS IsWorkFlowCompleted
from ComponentVersionWorkflowStep ws
where ws.MilestoneOrder = (select min(ws1.MilestoneOrder) 
                            from ComponentVersionWorkflowStep ws1
                            where ws1.ComponentVersionID = ws.ComponentVersionID)
        AND (
                @ComponentVersionId = - 1
                OR ws.ComponentVersionID = @ComponentVersionId
                )
";
    }

    private static string GetChipSetTableCommandText()
    {
        return @"
SELECT 
    val.SBHardwareComponentId AS 'SBHardwareComponentId',
    rows.Value AS Type,
    cs.value AS ChipSet,
    c.value AS Component,
    s.value AS Step
FROM
    SBChipSetTable rows
LEFT JOIN
    (
        SELECT *
        FROM SB_ChipSetTable
    ) val ON rows.ID = val.ChipSetTableId
left join SBChipSetStep s on s.id = val.ChipSetStepId
left join SBChipSetComponent c on c.id = val.ChipSetComponentId
left join SBChipSet cs on cs.id = val.ChipSetId
ORDER BY
    rows.ID;
";
    }

    private static string GetSBExpansionSlotCommandText()
    {
        return @"
SELECT 
    vals.SBHardwareComponentId,
    code.Value AS Type,
    vals.Quantity AS Quantity
FROM
    SBExpansionSlot code
LEFT JOIN
    SB_ExpansionSlot vals ON code.ID = vals.ExpansionSlotId 
WHERE
    code.Disabled IS NULL
ORDER BY
    code.Value;
";
    }

    private static string GetSBConnectorCommandText()
    {
        return @"
SELECT 
    vals.SBHardwareComponentId,
    code.Value AS Type,
    vals.Quantity AS Quantity
FROM
    SBExpansionSlot code
LEFT JOIN
    SB_ExpansionSlot vals ON code.ID = vals.ExpansionSlotId 
WHERE
    code.Disabled IS NULL
ORDER BY
    code.Value;
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

            if (string.IsNullOrEmpty(componentVersion.GetValue("ROM components")))
            {
                componentVersion.Add("ROM components", "Softpaq");
            }
            else
            {
                componentVersion.Add("ROM components", componentVersion.GetValue("ROM components") + ", Softpaq");
            }
        }

        if (componentVersion.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase)
            && string.Equals(componentVersion.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase))
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

        if (componentVersion.GetValue("Rom Component Binary(Rompaq)").Equals("1", StringComparison.OrdinalIgnoreCase))
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

            if (componentVersion.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                componentVersion.Add("CD Types : CD Files - Files copied from a CD will be released", "CD Types : CD Files - Files copied from a CD will be released");
            }
            else
            {
                componentVersion.Delete("CD Types : CD Files - Files copied from a CD will be released");
            }

            if (componentVersion.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                componentVersion.Add("CD Types : Replicator Only - Only available from the Replicator", "CD Types : Replicator Only - Only available from the Replicator");
            }
            else
            {
                componentVersion.Delete("CD Types : Replicator Only - Only available from the Replicator");
            }

            if (componentVersion.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                componentVersion.Add("CD Types : ISO Image -An ISO image of a CD will be released", "CD Types : ISO Image -An ISO image of a CD will be released");
            }
            else
            {
                componentVersion.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
            }

            componentVersion.Delete("Prism SW Type");

        }
        else
        {
            componentVersion.Delete("CD Types : CD Files - Files copied from a CD will be released");
            componentVersion.Delete("CD Types : Replicator Only - Only available from the Replicator");
            componentVersion.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
            componentVersion.Delete("FTP Site");
            componentVersion.Delete("Kit Number");
            componentVersion.Delete("CD/DVD Part Number");
        }

        if (componentVersion.GetValue("This is an HFCN release").Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            componentVersion.Add("This is an HFCN release", "This is an HFCN release");
        }
        else
        {
            componentVersion.Delete("This is an HFCN release");
        }

        componentVersion.Delete("Rom Component Binary(Rompaq)");
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

                if (string.IsNullOrEmpty(version.GetValue("ROM components")))
                {
                    version.Add("ROM components", "Softpaq");
                }
                else
                {
                    version.Add("ROM components", version.GetValue("ROM components") + ", Softpaq");
                }
            }

            if (version.GetValue("Ms Store").Equals("True", StringComparison.OrdinalIgnoreCase)
                && string.Equals(version.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase))
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

            if (version.GetValue("Rom Component Binary(Rompaq)").Equals("1", StringComparison.OrdinalIgnoreCase))
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

                if (version.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    version.Add("CD Types : CD Files - Files copied from a CD will be released", "CD Types : CD Files - Files copied from a CD will be released");
                }
                else
                {
                    version.Delete("CD Types : CD Files - Files copied from a CD will be released");
                }

                if (version.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    version.Add("CD Types : Replicator Only - Only available from the Replicator", "CD Types : Replicator Only - Only available from the Replicator");
                }
                else
                {
                    version.Delete("CD Types : Replicator Only - Only available from the Replicator");
                }

                if (version.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    version.Add("CD Types : ISO Image -An ISO image of a CD will be released", "CD Types : ISO Image -An ISO image of a CD will be released");
                }
                else
                {
                    version.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
                }

                version.Delete("Prism SW Type");

            }
            else
            {
                version.Delete("CD Types : CD Files - Files copied from a CD will be released");
                version.Delete("CD Types : Replicator Only - Only available from the Replicator");
                version.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
                version.Delete("FTP Site");
                version.Delete("Kit Number");
                version.Delete("CD/DVD Part Number");
                version.Delete("Component Location - FileName");
            }

            if (version.GetValue("This is an HFCN release").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                version.Add("This is an HFCN release", "This is an HFCN release");
            }
            else
            {
                version.Delete("This is an HFCN release");
            }

            version.Delete("Rom Component Binary(Rompaq)");
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
        }

        return Task.CompletedTask;
    }

    private static Task<int> GetCdAsync(CommonDataModel rootversion)
    {
        if (rootversion.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(1);
        }

        if (rootversion.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("1", StringComparison.OrdinalIgnoreCase))
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

    private async Task<Dictionary<string, string>> GetSBConnectorSlotAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetSBConnectorCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<string, string> connector = new();

        while (await reader.ReadAsync())
        {
            if (string.IsNullOrWhiteSpace(reader["SBHardwareComponentId"].ToString())
                || string.Equals(reader["Quantity"].ToString(), "0", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string rowResult = string.Empty;

            if (!string.IsNullOrWhiteSpace(reader["Type"].ToString()))
            {
                rowResult += "Type : " + reader["Type"].ToString() + " ";
            }

            if (!string.IsNullOrWhiteSpace(reader["Quantity"].ToString()))
            {
                rowResult += "Quantity : " + reader["Quantity"].ToString() + " ";
            }

            if (!connector.ContainsKey(reader["SBHardwareComponentId"].ToString()))
            {
                connector[reader["SBHardwareComponentId"].ToString()] = rowResult.Trim();
            }
            else
            {
                connector[reader["SBHardwareComponentId"].ToString()] += " , " + rowResult.Trim();
            }
        }

        return connector;
    }

    private async Task<Dictionary<string, string>> GetSBExpansionSlotAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetSBExpansionSlotCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<string, string> expansionSlot = new();

        while (await reader.ReadAsync())
        {
            if (string.IsNullOrWhiteSpace(reader["SBHardwareComponentId"].ToString())
                || string.Equals(reader["Quantity"].ToString(), "0", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string rowResult = string.Empty;

            if (!string.IsNullOrWhiteSpace(reader["Type"].ToString()))
            {
                rowResult += "Type : " + reader["Type"].ToString() + " ";
            }

            if (!string.IsNullOrWhiteSpace(reader["Quantity"].ToString()))
            {
                rowResult += "Quantity : " + reader["Quantity"].ToString() + " ";
            }

            if (!expansionSlot.ContainsKey(reader["SBHardwareComponentId"].ToString()))
            {
                expansionSlot[reader["SBHardwareComponentId"].ToString()] = rowResult.Trim();
            }
            else
            {
                expansionSlot[reader["SBHardwareComponentId"].ToString()] += " , " + rowResult.Trim();
            }
        }

        return expansionSlot;
    }

    private async Task<Dictionary<string, string>> GetChipSetTableAsync()
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetChipSetTableCommandText(), connection);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<string, string> chipSet = new();

        while (await reader.ReadAsync())
        {
            if (string.IsNullOrWhiteSpace(reader["SBHardwareComponentId"].ToString()))
            {
                continue;
            }

            string rowResult = string.Empty;

            if (!string.IsNullOrWhiteSpace(reader["Type"].ToString()))
            {
                rowResult += "Type : " + reader["Type"].ToString() + " ";
            }

            if (!string.IsNullOrWhiteSpace(reader["ChipSet"].ToString()))
            {
                rowResult += "ChipSet : " + reader["ChipSet"].ToString() + " ";
            }

            if (!string.IsNullOrWhiteSpace(reader["Component"].ToString()))
            {
                rowResult += "Component : " + reader["Component"].ToString() + " ";
            }

            if (!string.IsNullOrWhiteSpace(reader["Step"].ToString()))
            {
                rowResult += "Step : " + reader["Step"].ToString() + " ";
            }

            if (!chipSet.ContainsKey(reader["SBHardwareComponentId"].ToString()))
            {
                chipSet[reader["SBHardwareComponentId"].ToString()] = rowResult.Trim();
            }
            else
            {
                chipSet[reader["SBHardwareComponentId"].ToString()] += " , " + rowResult.Trim();
            }
        }

        return chipSet;
    }

    private async Task FillSystemBoardAsync(CommonDataModel root)
    {
        if (!int.TryParse(root.GetValue("Component Version Id"), out int componentVersionId))
        {
            return;
        }

        Dictionary<string, string> chipSet = await GetChipSetTableAsync();
        Dictionary<string, string> expansionSlot = await GetSBExpansionSlotAsync();
        Dictionary<string, string> connector = await GetSBConnectorSlotAsync();

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetSystemBoardCommandText(), connection);
        SqlParameter parameter = new("ComponentVersionId", componentVersionId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, CommonDataModel> systemBoard = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentVersionId"].ToString(), out int dbComponentVersionId))
            {
                continue;
            }

            CommonDataModel item = new();
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

                item.Add(columnName, value);
            }

            if (!systemBoard.ContainsKey(dbComponentVersionId))
            {
                systemBoard[dbComponentVersionId] = item;
            }
        }

        if (systemBoard.ContainsKey(componentVersionId))
        {
            foreach (string item in systemBoard[componentVersionId].GetKeys())
            {
                if (!string.Equals(item, "SBHardwareComponentId", StringComparison.OrdinalIgnoreCase))
                {
                    root.Add(item, systemBoard[componentVersionId].GetValue(item));
                    continue;
                }

                if (chipSet.ContainsKey(systemBoard[componentVersionId].GetValue(item)))
                {
                    root.Add("ChipSet Table", chipSet[systemBoard[componentVersionId].GetValue(item)]);
                }

                if (expansionSlot.ContainsKey(systemBoard[componentVersionId].GetValue(item)))
                {
                    root.Add("Expansion Slot", expansionSlot[systemBoard[componentVersionId].GetValue(item)]);
                }

                if (connector.ContainsKey(systemBoard[componentVersionId].GetValue(item)))
                {
                    root.Add("Connector", expansionSlot[systemBoard[componentVersionId].GetValue(item)]);
                }
            }
        }
    }

    private async Task FillSystemBoardAsync(IEnumerable<CommonDataModel> roots)
    {
        Dictionary<string, string> chipSet = await GetChipSetTableAsync();
        Dictionary<string, string> expansionSlot = await GetSBExpansionSlotAsync();
        Dictionary<string, string> connector = await GetSBConnectorSlotAsync();

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetSystemBoardCommandText(), connection);
        SqlParameter parameter = new("ComponentVersionId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, CommonDataModel> systemBoard = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentVersionId"].ToString(), out int componentVersionId))
            {
                continue;
            }

            CommonDataModel item = new();
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

                item.Add(columnName, value);
            }

            if (!systemBoard.ContainsKey(componentVersionId))
            {
                systemBoard[componentVersionId] = item;
            }
        }

        foreach (CommonDataModel root in roots)
        {
            if (int.TryParse(root.GetValue("Component Version Id"), out int componentVersionId)
              && systemBoard.ContainsKey(componentVersionId))
            {
                foreach (string item in systemBoard[componentVersionId].GetKeys())
                {
                    if (!string.Equals(item, "SBHardwareComponentId", StringComparison.OrdinalIgnoreCase))
                    {
                        root.Add(item, systemBoard[componentVersionId].GetValue(item));
                        continue;
                    }

                    if (chipSet.ContainsKey(systemBoard[componentVersionId].GetValue(item)))
                    {
                        root.Add("ChipSet Table", chipSet[systemBoard[componentVersionId].GetValue(item)]);
                    }

                    if (expansionSlot.ContainsKey(systemBoard[componentVersionId].GetValue(item)))
                    {
                        root.Add("Expansion Slot", expansionSlot[systemBoard[componentVersionId].GetValue(item)]);
                    }

                    if (connector.ContainsKey(systemBoard[componentVersionId].GetValue(item)))
                    {
                        root.Add("Connector", expansionSlot[systemBoard[componentVersionId].GetValue(item)]);
                    }
                }
            }
        }
    }

    private async Task FillIsWorkflowCompletedAsync(CommonDataModel root)
    {
        if (!int.TryParse(root.GetValue("Component Version Id"), out int componentVersionId))
        {
            return;
        }

        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetIsWorkflowCompletedCommandText(), connection);
        SqlParameter parameter = new("ComponentVersionId", componentVersionId);
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> isWorkflowCompleted = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentVersionId"].ToString(), out int dbComponentVersionId))
            {
                continue;
            }

            if (!isWorkflowCompleted.ContainsKey(dbComponentVersionId))
            {
                isWorkflowCompleted[dbComponentVersionId] = reader["IsWorkflowCompleted"].ToString();
            }
        }

        if (isWorkflowCompleted.ContainsKey(componentVersionId))
        {
            root.Add("IsWorkflowCompleted", isWorkflowCompleted[componentVersionId]);
        }
    }

    private async Task FillIsWorkflowCompletedAsync(IEnumerable<CommonDataModel> roots)
    {
        using SqlConnection connection = new(_info.DatabaseConnectionString);
        await connection.OpenAsync();
        SqlCommand command = new(GetIsWorkflowCompletedCommandText(), connection);
        SqlParameter parameter = new("ComponentVersionId", "-1");
        command.Parameters.Add(parameter);
        using SqlDataReader reader = command.ExecuteReader();
        Dictionary<int, string> isWorkflowCompleted = new();

        while (await reader.ReadAsync())
        {
            if (!int.TryParse(reader["ComponentVersionId"].ToString(), out int componentVersionId))
            {
                continue;
            }

            if (!isWorkflowCompleted.ContainsKey(componentVersionId))
            {
                isWorkflowCompleted[componentVersionId] = reader["IsWorkflowCompleted"].ToString();
            }
        }

        foreach (CommonDataModel root in roots)
        {
            if (int.TryParse(root.GetValue("Component Version Id"), out int componentVersionId)
              && isWorkflowCompleted.ContainsKey(componentVersionId))
            {
                root.Add("IsWorkflowCompleted", isWorkflowCompleted[componentVersionId]);
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
        if (!string.Equals(root.GetValue("RequiredPrismSWType"), "True", StringComparison.OrdinalIgnoreCase)
            || (root.GetValue("Packagings").Contains("Internal Tool")
                && !root.GetValue("Packagings").Contains("Preinstall")
                && !root.GetValue("Packagings").Contains("CD")
                && !root.GetValue("Packagings").Contains("Softpaq")
                && string.Equals(root.GetValue("Patch"), "0", StringComparison.OrdinalIgnoreCase))
            || ((string.Equals(root.GetValue("Patch"), "1", StringComparison.OrdinalIgnoreCase)
                 || root.GetValue("Packagings").Contains("Softpaq"))
                && !root.GetValue("Packagings").Contains("Preinstall")
                && !root.GetValue("Packagings").Contains("CD")
                && !root.GetValue("Rom Components").Contains("Binary")
                && !root.GetValue("Rom Components").Contains("ROM Component Preinstall"))
            || (string.IsNullOrWhiteSpace(root.GetValue("SWPartNumber"))
                || string.Equals(root.GetValue("SWPartNumber"), "N/A", StringComparison.OrdinalIgnoreCase))
            || string.Equals(root.GetValue("HidePrism"),"True",StringComparison.OrdinalIgnoreCase)
            || string.Equals(root.GetValue("IrsCategoryHidePrism"),"True",StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Prism SW Type");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Firmware", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("ROM Components");
        }

        if (!root.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("CD Types : Replicator Only - Only available from the Replicator", StringComparison.OrdinalIgnoreCase)
            && !root.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("CD Types : ISO Image -An ISO image of a CD will be released", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("CDs Replicated By");
        }

        if (!root.GetValue("CD Types : Replicator Only - Only available from the Replicator").Equals("CD Types : Replicator Only - Only available from the Replicator", StringComparison.OrdinalIgnoreCase)
            && !root.GetValue("CD Types : ISO Image -An ISO image of a CD will be released").Equals("CD Types : ISO Image -An ISO image of a CD will be released", StringComparison.OrdinalIgnoreCase)
            && !root.GetValue("CD Types : CD Files - Files copied from a CD will be released").Equals("CD Types : CD Files - Files copied from a CD will be released", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("FTP Site");
            root.Delete("Kit Number");
            root.Delete("CD/DVD Part Number");
        }

        if (root.GetValue("Packagings").Contains("Internal Tool"))
        {
            root.Delete("Touch Points");
            root.Delete("Other Setting");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Hardware", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(root.GetValue("Category"), "PCA Setting", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("This is for BIOS Integration");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Hardware", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("This is an HFCN release");
            root.Delete("Samples Available");
            root.Delete("Samples Confidence");
            root.Delete("End Of Life Date");
            root.Delete("Service Team - Available Until Date");
            root.Delete("Engineering Team - Available Until Date");
            root.Delete("Code Name");
            root.Delete("HP Part Number");
            root.Delete("Model Number");
            root.Delete("Green Spec Level");
        }
        else
        {
            root.Delete("Recovery Option");
            root.Delete("MD5");
            root.Delete("Property Tabs Added");
            root.Delete("Touch Points");
            root.Delete("Other Setting");
            root.Delete("Transfer Server");
            root.Delete("Submission Path");
            root.Delete("Component Location - FileName");
            root.Delete("SW Part Number");
        }

        if (string.Equals(root.GetValue("Component Type"), "Firmware", StringComparison.OrdinalIgnoreCase)
            || string.Equals(root.GetValue("Abbreviation"), "SBD", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Revision");
        }

        if (string.Equals(root.GetValue("Abbreviation"), "SBD", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Pass");
            root.Delete("Build Level");
            root.Delete("Vendor Version");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("SHA256");
        }

        if (!string.Equals(root.GetValue("Component Type"), "Software", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(root.GetValue("Component Type"), "Documentation", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("Packaging");
        }

        if (!string.IsNullOrEmpty(root.GetValue("SWPartNumber"))
           && !string.Equals(root.GetValue("SWPartNumber"), "Pending...", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(root.GetValue("SWPartNumber"), "N/A", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(root.GetValue("FccRequired"), "False", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(root.GetValue("IsWorkflowCompleted"), "True", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("CD Types : Replicator Only -Only available from the Replicator");
            root.Delete("CD Types : ISO Image -An ISO image of a CD will be released");
            root.Delete("CD Types : CD Files - Files copied from a CD will be released");
            root.Delete("FTP Site");
            root.Delete("Component Location - FileName");
        }
        else if (root.GetValue("Packagings").Contains("CD"))
        {
            root.Delete("CVA Path");
        }

        if (!string.Equals(root.GetValue("KoreanCertificationRequired"), "True", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("KoreanCertificationId");
        }

        if (!string.Equals(root.GetValue("RequiresTts"), "True", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("WWAN EDID");
            root.Delete("WWAN TTS Results");
            root.Delete("WWAN TTS Spec Rev");
        }

        if (!string.Equals(root.GetValue("TeamID"), "3", StringComparison.OrdinalIgnoreCase))
        {
            root.Delete("FCC ID");
            root.Delete("Anatel");
            root.Delete("ICASA");
            root.Delete("RF Kill Mechanism");
        }

        root.Delete("TeamID");
        root.Delete("Abbreviation");
        root.Delete("SWPartNumber");
        root.Delete("IsWorkflowCompleted");
        root.Delete("KoreanCertificationRequired");
        root.Delete("FccRequired");
        root.Delete("RequiresTTS");
        root.Delete("Component Type");
        root.Delete("Patch");
        root.Delete("HidePrism");

        return root;
    }
}
