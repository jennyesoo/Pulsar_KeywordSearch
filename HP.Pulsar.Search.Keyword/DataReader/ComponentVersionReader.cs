using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader
{
    public class ComponentVersionReader : IKeywordSearchDataReader
    {
        private ConnectionStringProvider _csProvider;

        public ComponentVersionReader(KeywordSearchInfo info)
        {
            _csProvider = new(info.Environment);
        }

        public async Task<CommonDataModel> GetDataAsync(int componentVersionsId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
        {
            IEnumerable<CommonDataModel> componentVersions = await GetComponentVersionAsync();
            List<Task> tasks = new()
            {
                GetPropertyValueAsync(componentVersions)
            };
            await Task.WhenAll(tasks);
            return componentVersions;
        }

        private async Task<IEnumerable<CommonDataModel>> GetComponentVersionAsync()
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            SqlCommand command = new(GetTSQLComponentVersionCommandText(),
                                    connection);
            SqlParameter parameter = new("ComponentVersionId", -1);
            command.Parameters.Add(parameter);
            await connection.OpenAsync();
            using SqlDataReader reader = command.ExecuteReader();
            List<CommonDataModel> output = new();

            while (reader.Read())
            {
                CommonDataModel componentVersion = new();
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
                        componentVersion.Add(columnName, value);
                    }
                }

                componentVersion.Add("target", "ComponentVersion");
                componentVersion.Add("Id", SearchIdName.Version + componentVersion.GetValue("ComponentVersionID"));
                output.Add(componentVersion);
            }
            return output;
        }

        private string GetTSQLComponentVersionCommandText()
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
    Dv.Active as Visibility,
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
    gs.Name as GreenSpecLevel,
    Dv.IntroDate as MassProduction,
    CASE WHEN root.TypeID = 1 THEN 'Hardware'
    WHEN root.TypeID = 2 THEN 'Software'
    WHEN root.TypeID = 3 THEN 'Firmware'
    WHEN root.TypeID = 4 THEN 'Documentation'
    WHEN root.TypeID = 5 THEN 'Image'
    WHEN root.TypeID = 6 THEN 'Certification'
    WHEN root.TypeID = 7 THEN 'Softpaq'
    WHEN root.TypeID = 8 THEN 'Factory'
    END AS ComponentType
FROM DeliverableVersion Dv
LEFT JOIN ComponentPrismSWType CPSW ON CPSW.PRISMTypeID = Dv.PrismSWType
left JOIN userinfo user1 ON user1.userid = Dv.DeveloperID
left JOIN userinfo user2 ON user2.userid = Dv.TestLeadId
left JOIN Vendor v ON v.ID = Dv.VendorID
LEFT JOIN ComponentBuildLevel cbl ON cbl.ComponentBuildLevelId = Dv.LevelID
LEFT JOIN SWSetupCategory sws ON sws.ID = Dv.SWSetupCategoryID
LEFT JOIN ComponentTransferServer cts ON cts.Id = Dv.TransferServerId
LEFT JOIN GreenSpec gs on gs.id = Dv.GreenSpecID
LEFT JOIN DeliverableRoot root on root.id = Dv.DeliverableRootID
WHERE (
        @ComponentVersionId = - 1
        OR Dv.ID = @ComponentVersionId
        )
";
        }

        private async Task GetPropertyValueAsync(IEnumerable<CommonDataModel> componentVersions)
        {
            foreach (CommonDataModel rootversion in componentVersions)
            {
                if (rootversion.GetValue("Preinstall").Equals("1"))
                {
                    rootversion.Add("Preinstall", "Packaging Preinstall");
                }
                else
                {
                    rootversion.Delete("Preinstall");
                }

                if (rootversion.GetValue("DrDvd").Equals("True"))
                {
                    rootversion.Add("DrDvd", "DRDVD");
                }
                else
                {
                    rootversion.Delete("DrDvd");
                }

                if (rootversion.GetValue("Scriptpaq").Equals("True"))
                {
                    rootversion.Add("Scriptpaq" , "Packaging Softpaq");
                }
                else
                {
                    rootversion.Delete("Scriptpaq");
                }


                if (rootversion.GetValue("MsStore").Equals("True"))
                {
                    rootversion.Add("MsStore", "Ms Store");
                }
                else
                {
                    rootversion.Delete("MsStore");
                }

                if (rootversion.GetValue("FloppyDisk").Equals("1"))
                {
                    rootversion.Add("FloppyDisk","Internal Tool");
                }
                else
                {
                    rootversion.Delete("FloppyDisk");
                }

                if (rootversion.GetValue("Rompaq").Equals("1"))
                {
                    rootversion.Add("Rompaq", "ROM Component Binary");
                }
                else
                {
                    rootversion.Delete("Rompaq");
                }

                if (rootversion.GetValue("PreinstallROM").Equals("1"))
                {
                    rootversion.Add("PreinstallROM", "ROM Component Preinstall");
                }
                else
                {
                    rootversion.Delete("PreinstallROM");
                }

                if (rootversion.GetValue("CAB").Equals("1"))
                {
                    rootversion.Add("CAB", "CAB");
                }
                else
                {
                    rootversion.Delete("CAB");
                }

                if (rootversion.GetValue("SettingFWML").Equals("True"))
                {
                    rootversion.Add("SettingFWML", "FWML");
                }
                else
                {
                    rootversion.Delete("SettingFWML");
                }

                if (rootversion.GetValue("SettingUWPCompliant").Equals("True"))
                {
                    rootversion.Add("SettingUWPCompliant", "UWP Compliant");
                }
                else
                {
                    rootversion.Delete("SettingUWPCompliant");
                }

                if (rootversion.GetValue("IconDesktop").Equals("True"))
                {
                    rootversion.Add("IconDesktop", "Desktop");
                }
                else
                {
                    rootversion.Delete("IconDesktop");
                }

                if (rootversion.GetValue("IconMenu").Equals("True"))
                {
                    rootversion.Add("IconMenu", "Start Menu");
                }
                else
                {
                    rootversion.Delete("IconMenu");
                }

                if (rootversion.GetValue("IconTray").Equals("True"))
                {
                    rootversion.Add("IconTray", "System Tray");
                }
                else
                {
                    rootversion.Delete("IconTray");
                }

                if (rootversion.GetValue("IconPanel").Equals("True"))
                {
                    rootversion.Add("IconPanel", "Control Panel");
                }
                else
                {
                    rootversion.Delete("IconPanel");
                }

                if (rootversion.GetValue("IconInfoCenter").Equals("True"))
                {
                    rootversion.Add("IconInfoCenter", "Info Center");
                }
                else
                {
                    rootversion.Delete("IconInfoCenter");
                }

                if (rootversion.GetValue("IconTile").Equals("True"))
                {
                    rootversion.Add("IconTile", "Start Menu Tile");
                }
                else
                {
                    rootversion.Delete("IconTile");
                }

                if (rootversion.GetValue("IconTaskBarIcon").Equals("True"))
                {
                    rootversion.Add("IconTaskBarIcon", "Task Pinned Icon");
                }
                else
                {
                    rootversion.Delete("IconTaskBarIcon");
                }
                
                if (rootversion.GetValue("IsSoftPaqInPreinstall").Equals("True"))
                {
                    rootversion.Add("IsSoftPaqInPreinstall", "SoftPaq In Preinstall");
                }
                else
                {
                    rootversion.Delete("IsSoftPaqInPreinstall");
                }
                
                if (rootversion.GetValue("Visibility").Equals("True"))
                {
                    rootversion.Add("Visibility", "Active");
                }
                else
                {
                    rootversion.Delete("Visibility");
                }
                
                if (GetCDAsync(rootversion).Equals(1))
                {
                    rootversion.Add("CD", "CD");
                }
                rootversion.Delete("CDImage");
                rootversion.Delete("ISOImage");
                rootversion.Delete("AR");
            }
        }

        private async Task<int> GetCDAsync(CommonDataModel rootversion)
        {
            if (rootversion.GetValue("CDImage").Equals("1"))
            {
                return 1;
            }
            if (rootversion.GetValue("ISOImage").Equals("1"))
            {
                return 1;
            }
            if (rootversion.GetValue("AR").Equals("1"))
            {
                return 1;
            }
            return 0;
        }

        private async Task GetDifferentPropertyNameBasedOnCategoryAsync(IEnumerable<CommonDataModel> componentVersions)
        {
            foreach (CommonDataModel rootversion in componentVersions)
            {
                if (rootversion.GetValue("ComponentType").Equals("Hardware"))
                {
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
            }
        }
    }
}
