﻿using System.Globalization;
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

        public async Task<CommonDataModel> GetDataAsync(int productId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
        {
            IEnumerable<CommonDataModel> componentVersions = await GetComponentVersionAsync();
            componentVersions = await GetPackagingAsync(componentVersions);
            componentVersions = await GetTouchPointAsync(componentVersions);
            componentVersions = await GetOtherSettingAsync(componentVersions);
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
                    if (!string.IsNullOrEmpty(reader[i].ToString()))
                    {
                        string columnName = reader.GetName(i);
                        string value = reader[i].ToString();
                        componentVersion.Add(columnName, value);
                    }
                }

                /*
                if (!string.IsNullOrWhiteSpace(businessSegmentId)
                    || !string.IsNullOrWhiteSpace(productId))
                {
                    continue;
                }

                if (!int.TryParse(productId, out int productIdValue))
                {
                    continue;
                }
                */
                componentVersion.Add("target", "ComponentVersion");
                componentVersion.Add("Id", "ComponentVersion-" + componentVersion.GetValue("ComponentVersionID"));
                output.Add(componentVersion);
            }
            return output;
        }

        private string GetTSQLComponentVersionCommandText()
        {
            return @"
                    select  Dv.ID As ComponentVersionID,
                            Dv.DeliverableRootID As ComponentRootID,
                            Dv.DeliverableName As ComponentName,
                            Dv.Version,
                            Dv.Revision,
                            CPSW.Description As PrismSWType,
                            Dv.Pass,
                            user1.FirstName + ' ' + user1.LastName AS Developer,
                            user1.Email AS DeveloperEmail,
                            user2.FirstName + ' ' + user2.LastName AS TestLead,
                            user2.Email AS TestLeadEmail,
                            v.Name As Vendor,
                            Dv.IRSPartNumber As SWPartNumber,
                            cbl.Name As BuildLevel,
                            sws.DisplayName As RecoveryOption,
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
                            Dv.Active,
                            cts.Name As TransferServer,
                            Dv.SubmissionPath,
                            Dv.VendorVersion,
                            Dv.Comments,
                            Dv.IntroDate,
                            Dv.EndOfLifeDate,
                            Dv.Rompaq,
                            Dv.PreinstallROM,
                            Dv.CAB
                    From DeliverableVersion Dv
                    left JOIN ComponentPrismSWType CPSW on CPSW.PRISMTypeID = Dv.PrismSWType 
                    JOIN userinfo user1 ON user1.userid = Dv.DeveloperID
                    JOIN userinfo user2 ON user2.userid = Dv.TestLeadId
                    Join Vendor v ON v.ID = Dv.VendorID 
                    left JOIN ComponentBuildLevel cbl ON cbl.ComponentBuildLevelId = Dv.LevelID
                    left Join SWSetupCategory sws on sws.ID = Dv.SWSetupCategoryID
                    left Join ComponentTransferServer cts on cts.Id = Dv.TransferServerId
                    where (@ComponentVersionId = -1 OR Dv.ID = @ComponentVersionId)";
        }

        private async Task<IEnumerable<CommonDataModel>> GetPackagingAsync(IEnumerable<CommonDataModel> componentVersions)
        {
            List<CommonDataModel> output = new List<CommonDataModel>();

            foreach (CommonDataModel rootversion in componentVersions)
            {
                if (rootversion.GetValue("Preinstall").Equals("1"))
                {
                    rootversion.Add("Preinstall", "Preinstall");
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
                    rootversion.Add("Scriptpaq" , "SoftPaq");
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
                    rootversion.Add("Rompaq", "Binary");
                }
                else
                {
                    rootversion.Delete("Rompaq");
                }

                if (rootversion.GetValue("PreinstallROM").Equals("1"))
                {
                    rootversion.Add("PreinstallROM", "Preinstall");
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

                if (GetCDAsync(rootversion).Equals(1))
                {
                    rootversion.Add("CD", "CD");
                }
                rootversion.Delete("CDImage");
                rootversion.Delete("ISOImage");
                rootversion.Delete("AR");
                output.Add(rootversion);
            }
            return output;
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

        private async Task<IEnumerable<CommonDataModel>> GetTouchPointAsync(IEnumerable<CommonDataModel> componentVersions)
        {
            List<CommonDataModel> output = new List<CommonDataModel>();

            foreach (CommonDataModel rootversion in componentVersions)
            {                
                if (rootversion.GetValue("IconDesktop").Equals("True"))
                {
                    rootversion.Add("IconDesktop" , "Desktop");
                }
                else
                {
                    rootversion.Delete("IconDesktop");
                }

                if (rootversion.GetValue("IconMenu").Equals("True"))
                {
                    rootversion.Add("IconMenu","Start Menu");
                }
                else
                {
                    rootversion.Delete("IconMenu");
                }

                if (rootversion.GetValue("IconTray").Equals("True"))
                {
                    rootversion.Add("IconTray","System Tray");
                }
                else
                {
                    rootversion.Delete("IconTray");
                }

                if (rootversion.GetValue("IconPanel").Equals("True"))
                {
                    rootversion.Add("IconPanel" , "Control Panel");
                }
                else
                {
                    rootversion.Delete("IconPanel");
                }

                if (rootversion.GetValue("IconInfoCenter").Equals("True"))
                {
                    rootversion.Add("IconInfoCenter" , "Info Center");
                }
                else
                {
                    rootversion.Delete("IconInfoCenter");
                }

                if (rootversion.GetValue("IconTile").Equals("True"))
                {
                    rootversion.Add("IconTile" , "Start Menu Tile");
                }
                else
                {
                    rootversion.Delete("IconTile");
                }

                if (rootversion.GetValue("IconTaskBarIcon").Equals("True"))
                {
                    rootversion.Add("IconTaskBarIcon" , "Task Pinned Icon");
                }
                else
                {
                    rootversion.Delete("IconTaskBarIcon");
                }

                output.Add(rootversion);
            }
            return output;
        }

        private async Task<IEnumerable<CommonDataModel>> GetOtherSettingAsync(IEnumerable<CommonDataModel> componentVersions)
        {
            List<CommonDataModel> output = new List<CommonDataModel>();

            foreach (CommonDataModel rootversion in componentVersions)
            {
                if (rootversion.GetValue("SettingFWML").Equals("True"))
                {
                    rootversion.Add("SettingFWML","FWML");
                }
                else
                {
                    rootversion.Delete("SettingFWML");
                }

                if (rootversion.GetValue("SettingUWPCompliant").Equals("True"))
                {
                    rootversion.Add("SettingUWPCompliant","UWP Compliant");
                }
                else
                {
                    rootversion.Delete("SettingUWPCompliant");
                }
                output.Add(rootversion);
            }
            return output;
        }
    }
}
