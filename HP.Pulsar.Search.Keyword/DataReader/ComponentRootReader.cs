using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace HP.Pulsar.Search.Keyword.DataReader
{
    internal class ComponentRootReader : IKeywordSearchDataReader
    {
        public ComponentRootReader(KeywordSearchInfo info)
        {
            _csProvider = new(info.Environment);
        }

        private ConnectionStringProvider _csProvider;

        public Task<CommonDataModel> GetDataAsync(int componentRootId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
        {
            IEnumerable<CommonDataModel> componentRoot = await GetComponentRootAsync();
            componentRoot = await GetPropertyValueAsync(componentRoot);
            componentRoot = await GetComponentRootListAsync(componentRoot);
            return componentRoot;
        }

        private string GetAllComponentRootSqlCommandText()
        {
            return @"
                    SELECT root.id AS ComponentRootId,
                        root.name AS ComponentRootName,
                        root.description,
                        vendor.Name AS VendorName,
                        cate.name As SIFunctionTestGroupCategory,
                        user1.FirstName + ' ' + user1.LastName AS ComponentPM,
                        user1.Email AS ComponentPMEmail,
                        user2.FirstName + ' ' + user2.LastName AS Developer,
                        user2.Email AS DeveloperEmail,
                        user3.FirstName + ' ' + user3.LastName AS TestLead,
                        user3.Email AS TestLeadEmail,
                        coreteam.Name AS SICoreTeam,
                        CASE WHEN root.TypeID = 1 THEN 'Hardware'
                                WHEN root.TypeID = 2 THEN 'Software'
                                WHEN root.TypeID = 3 THEN 'Firmware'
                                WHEN root.TypeID = 4 THEN 'Documentation'
                                WHEN root.TypeID = 5 THEN 'Image'
                                WHEN root.TypeID = 6 THEN 'Certification'
                                WHEN root.TypeID = 7 THEN 'Softpaq'
                                WHEN root.TypeID = 8 THEN 'Factory'
                                END AS ComponentType,                        
                        root.Preinstall,
                        root.active as Visibility,
                        root.TargetPartition,
                        root.Reboot,
                        root.CDImage,
                        root.ISOImage,
                        root.CAB,
                        root.Binary,
                        root.FloppyDisk,
                        root.PreinstallROM,
                        root.CertRequired as WHQLCertificationRequire,
                        root.ScriptPaq,
                        root.Softpaq,
                        root.MultiLanguage,
                        Sc.name As SoftpaqCategory,
                        root.Created,
                        root.IconDesktop,
                        root.IconMenu,
                        root.IconTray,
                        root.IconPanel,
                        root.PropertyTabs,
                        root.AR,
                        root.RoyaltyBearing,
                        sws.DisplayName As RecoveryOption,
                        root.KitNumber,
                        root.KitDescription,
                        root.DeliverableSpec as FunctionalSpec,
                        root.IconInfoCenter,
                        cts.Name As TransferServer,
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
                        user4.FirstName + ' ' + user4.LastName As SIOApprover,
                        root.KoreanCertificationRequired,
                        root.BaseFilePath,
                        root.SubmissionPath,
                        root.IRSPartNumberLastSpin,
                        root.IRSBasePartNumber,
                        CPSW.Description As PrismSWType,
                        root.LimitFuncTestGroupVisability,
                        root.IconTile,
                        root.IconTaskBarIcon,
                        root.SettingFWML,
                        root.SettingUWPCompliant,
                        root.FtpSitePath,
                        root.DrDvd,
                        root.MsStore,
                        root.ErdComments

                    FROM DeliverableRoot root
                    left JOIN vendor ON root.vendorid = vendor.id
                    left JOIN componentCategory cate ON cate.CategoryId = root.categoryid
                    left JOIN UserInfo user1 ON user1.userid = root.devmanagerid
                    left JOIN userinfo user2 ON user2.userid = root.DeveloperID
                    left JOIN userinfo user3 ON user3.userid = root.TesterID
                    left JOIN userinfo user4 ON user4.userid = root.SioApproverId
                    left JOIN componentcoreteam coreteam ON coreteam.ComponentCoreTeamId = root.CoreTeamID 
                    left JOIN ComponentPrismSWType CPSW on CPSW.PRISMTypeID = root.PrismSWType 
                    left Join SWSetupCategory sws on sws.ID = root.SWSetupCategoryID
                    left Join ComponentTransferServer cts on cts.Id = root.TransferServerId
                    left Join SoftpaqCategory Sc on Sc.id = root.SoftpaqCategoryID
                    WHERE (@ComponentRootId = - 1 OR root.id = @ComponentRootId);";
        }

        private string GetTSQLProductListCommandText()
        {
            return @"select  DR.Id as ComponentRoot,
                            stuff((SELECT ' , ' + (CONVERT(Varchar, p.Id) + ' ' +  p.DOTSName)
                                    FROM ProductVersion p
                                    JOIN ProductStatus ps ON ps.id = p.ProductStatusID
                                    JOIN Product_DelRoot pr on pr.ProductVersionId = p.id
                                    JOIN DeliverableRoot root ON root.Id = pr.DeliverableRootId
                                    WHERE root.Id = DR.Id  and ps.Name <> 'Inactive' and p.FusionRequirements = 1 order by root.Id
                                    for xml path('')),1,3,'') As Product
                    FROM DeliverableRoot DR
                    where DR.Id = @ComponentRootId
                    group by DR.Id";

            //SELECT p.dotsname, r.Name
            //FROM DeliverableRoot root
            //join Product_DelRoot pr on root.Id = pr.DeliverableRootId
            //join ProductVersion p on pr.ProductVersionID = p.id
            //join ProductVersion_Release pr2 on pr2.ProductVersionID = p.Id
            //join ProductVersionRelease r on r.Id = pr2.ReleaseId
            //where root.id = 36886
            //order by p.dotsname
        }

        // TODO - performance improvement needed
        private async Task<IEnumerable<CommonDataModel>> GetComponentRootListAsync(IEnumerable<CommonDataModel> componentRoots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();

            foreach (CommonDataModel root in componentRoots)
            {
                SqlCommand command = new(GetTSQLProductListCommandText(), connection);
                SqlParameter parameter = new SqlParameter("ComponentRootId", root.GetValue("ComponentRootId"));
                command.Parameters.Add(parameter);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (reader["Product"].ToString().IsNullOrEmpty() == false)
                    {
                        root.Add("ProductList", reader["Product"].ToString());
                    }
                }
            }
            return componentRoots;
        }

        private async Task<IEnumerable<CommonDataModel>> GetComponentRootAsync()
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            SqlCommand command = new(GetAllComponentRootSqlCommandText(), connection);

            SqlParameter parameter = new("ComponentRootId", -1);
            command.Parameters.Add(parameter);

            await connection.OpenAsync();

            using SqlDataReader reader = command.ExecuteReader();

            List<CommonDataModel> output = new();

            while (reader.Read())
            {
                //string businessSegmentId;
                CommonDataModel root = new();
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
                        root.Add(columnName, value);
                    }
                }
                root.Add("target", "Component Root");
                output.Add(root);
            }
            return output;
        }

        private async Task<IEnumerable<CommonDataModel>> GetPropertyValueAsync(IEnumerable<CommonDataModel> componentRoots)
        {
            List<CommonDataModel> output = new List<CommonDataModel>();

            foreach (CommonDataModel root in componentRoots)
            {
                if (root.GetValue("Preinstall").Equals("1"))
                {
                    root.Add("Preinstall", "Preinstall");
                }
                else
                {
                    root.Delete("Preinstall");
                }

                if (root.GetValue("DrDvd").Equals("True"))
                {
                    root.Add("DrDvd", "DRDVD");
                }
                else
                {
                    root.Delete("DrDvd");
                }

                if (root.GetValue("ScriptPaq").Equals("True"))
                {
                    root.Add("ScriptPaq", "SoftPaq");
                }
                else
                {
                    root.Delete("ScriptPaq");
                }

                if (root.GetValue("MsStore").Equals("True"))
                {
                    root.Add("MsStore", "Ms Store");
                }
                else
                {
                    root.Delete("MsStore");
                }

                if (root.GetValue("FloppyDisk").Equals("1"))
                {
                    root.Add("FloppyDisk", "Internal Tool");
                }
                else
                {
                    root.Delete("FloppyDisk");
                }

                if (root.GetValue("Patch").Equals("1"))
                {
                    root.Add("Patch", "Patch");
                }
                else
                {
                    root.Delete("Patch");
                }

                if (root.GetValue("Binary").Equals("1"))
                {
                    root.Add("Binary", "Binary");
                }
                else
                {
                    root.Delete("Binary");
                }

                if (root.GetValue("PreinstallROM").Equals("1"))
                {
                    root.Add("PreinstallROM", "Preinstall");
                }
                else
                {
                    root.Delete("PreinstallROM");
                }

                if (root.GetValue("CAB").Equals("1"))
                {
                    root.Add("CAB", "CAB");
                }
                else
                {
                    root.Delete("CAB");
                }

                if (root.GetValue("Softpaq").Equals("1"))
                {
                    root.Add("Softpaq", "Softpaq");
                }
                else
                {
                    root.Delete("Softpaq");
                }

                if (root.GetValue("IconDesktop").Equals("True"))
                {
                    root.Add("IconDesktop", "Desktop");
                }
                else
                {
                    root.Delete("IconDesktop");
                }
              
                if (root.GetValue("IconMenu").Equals("True"))
                {
                    root.Add("IconMenu", "Start Menu");
                }
                else
                {
                    root.Delete("IconMenu");
                }

                if (root.GetValue("IconTray").Equals("True"))
                {
                    root.Add("IconTray", "System Tray");
                }
                else
                {
                    root.Delete("IconTray");
                }

                if (root.GetValue("IconPanel").Equals("True"))
                {
                    root.Add("IconPanel", "Control Panel");
                }
                else
                {
                    root.Delete("IconPanel");
                }

                if (root.GetValue("IconInfoCenter").Equals("True"))
                {
                    root.Add("IconInfoCenter", "Info Center");
                }
                else
                {
                    root.Delete("IconInfoCenter");
                }

                if (root.GetValue("IconTile").Equals("True"))
                {
                    root.Add("IconTile", "Start Menu Tile");
                }
                else
                {
                    root.Delete("IconTile");
                }

                if (root.GetValue("IconTaskBarIcon").Equals("True"))
                {
                    root.Add("IconTaskBarIcon", "Taskbar Pinned Icon ");
                }
                else
                {
                    root.Delete("IconTaskBarIcon");
                }

                if (root.GetValue("SettingFWML").Equals("True"))
                {
                    root.Add("SettingFWML", "FWML");
                }
                else
                {
                    root.Delete("SettingFWML");
                }

                if (root.GetValue("SettingUWPCompliant").Equals("True"))
                {
                    root.Add("SettingUWPCompliant", "FWML");
                }
                else
                {
                    root.Delete("SettingUWPCompliant");
                }

                if (root.GetValue("RoyaltyBearing").Equals("True"))
                {
                    root.Add("RoyaltyBearing", "Royalty Bearing");
                }
                else
                {
                    root.Delete("RoyaltyBearing");
                }

                if (root.GetValue("KoreanCertificationRequired").Equals("True"))
                {
                    root.Add("KoreanCertificationRequired", "Korean Certification Required");
                }
                else
                {
                    root.Delete("KoreanCertificationRequired");
                }

                if (root.GetValue("WHQLCertificationRequire").Equals("1"))
                {
                    root.Add("WHQLCertificationRequire", "WHQL Certification Require");
                }
                else
                {
                    root.Delete("WHQLCertificationRequire");
                }

                if (root.GetValue("LimitFuncTestGroupVisability").Equals("True"))
                {
                    root.Add("LimitFuncTestGroupVisability", "Limit partner visibility");
                }
                else
                {
                    root.Delete("LimitFuncTestGroupVisability");
                }

                if (root.GetValue("Visibility").Equals("True"))
                {
                    root.Add("Visibility", "Active");
                }
                else
                {
                    root.Delete("Visibility");
                }

                if (GetCDAsync(root).Equals(1))
                {
                    root.Add("CD", "CD");
                }
                root.Delete("CDImage");
                root.Delete("ISOImage");
                root.Delete("AR");
                output.Add(root);
            }
            return output;
        }

        private Task<int> GetCDAsync(CommonDataModel root)
        {
            if (root.GetValue("CDImage").Equals("1"))
            {
                return Task.FromResult(1);
            }
            if (root.GetValue("ISOImage").Equals("1"))
            {
                return Task.FromResult(1);
            }
            if (root.GetValue("AR").Equals("1"))
            {
                return Task.FromResult(1);
            }
            return Task.FromResult(0);
        }

    }
}
