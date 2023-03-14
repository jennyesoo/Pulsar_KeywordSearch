using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

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
                        user2.FirstName + ' ' + user2.LastName AS Developer,
                        user3.FirstName + ' ' + user3.LastName AS TestLead,
                        coreteam.Name AS SICoreTeam,
                        ComponentType.Name As ComponentType,
                        BasePartNumber,
                        OtherDependencies,
                        root.Preinstall,
                        root.DropInBox,
                        root.active as Visibility,
                        root.RootFilename,
                        root.TargetPartition,
                        root.Install,
                        root.SilentInstall,
                        root.ARCDInstall,
                        root.Reboot,
                        root.CDImage,
                        root.ISOImage,
                        root.Replicater,
                        root.CAB,
                        root.Binary,
                        root.FloppyDisk,
                        root.PreinstallROM,
                        root.SysReboot,
                        root.Admin,
                        root.CertRequired as WHQLCertificationRequire,
                        root.PNPDevices,
                        root.ScriptPaq,
                        root.Softpaq,
                        root.MultiLanguage,
                        Sc.name As SoftpaqCategory,
                        root.Created,
                        root.IconDesktop,
                        root.IconMenu,
                        root.IconTray,
                        root.IconPanel,
                        root.PackageForWeb,
                        root.PropertyTabs,
                        root.PostRTMStatus,
                        root.SubAssembly,
                        root.DeveloperNotification,
                        root.AR,
                        root.RoyaltyBearing,
                        sws.DisplayName As RecoveryOption,
                        root.KitNumber,
                        root.KitDescription,
                        root.DeliverableSpec as FunctionalSpec,
                        root.MUIAware,
                        root.MUIAwareDate,
                        root.IconInfoCenter,
                        root.Notifications,
                        root.OEMReadyRequired,
                        root.OEMReadyException,
                        root.HPPI,
                        root.OTSFVTOrganizationID,
                        cts.Name As TransferServer,
                        root.SoftpaqInstructions,
                        root.LockReleases,
                        root.ReturnCodes,
                        root.OsCode,
                        root.ShowOnStatus,
                        root.InitialOfferingStatus,
                        root.InitialOfferingChangeStatus,
                        root.InitialOfferingChangeStatusArchive,
                        root.LTFAVNo,
                        root.LTFSubassemblyNo,
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
                        root.IrsComponentCloneId,
                        CPSW.Description As PrismSWType,
                        root.LimitFuncTestGroupVisability,
                        Ns.Name As NamingStandard,
                        root.ML,
                        root.AgencyLead,
                        root.SortOrder,
                        root.IconTile,
                        root.IconTaskBarIcon,
                        root.SettingFWML,
                        root.SettingUWPCompliant,
                        root.FtpSitePath,
                        root.DrDvd,
                        root.MsStore,
                        root.ErdComments,
                        root.IsAutoTagingEnabled,
                        root.IsSoftPaqInPreinstall,
                        root.Partner2Id
                    FROM DeliverableRoot root
                    left Join ComponentType on ComponentType.ComponentTypeId = root.TypeID
                    left JOIN vendor ON root.vendorid = vendor.id
                    left JOIN componentCategory cate ON cate.CategoryId = root.categoryid
                    left JOIN UserInfo user1 ON user1.userid = root.devmanagerid
                    left JOIN userinfo user2 ON user2.userid = root.DeveloperID
                    left JOIN userinfo user3 ON user3.userid = root.TesterID
                    left JOIN userinfo user4 ON user4.userid = root.SioApproverId
                    left JOIN componentcoreteam coreteam ON coreteam.ComponentCoreTeamId = root.CoreTeamID 
                    left Join NamingStandard Ns on Ns.NamingStandardID = root.NamingStandardID
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
                    root.Add("ProductList", reader["Product"].ToString());
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

                    string columnName = reader.GetName(i);
                    string value = reader[i].ToString();
                    root.Add(columnName, value);
                }

                root.Add("target", "Component Root");

                output.Add(root);
            }

            return output;
        }

        private async Task<IEnumerable<CommonDataModel>> GetPropertyValueAsync(IEnumerable<CommonDataModel> ComponentRoots)
        {
            List<CommonDataModel> _output = new List<CommonDataModel>();

            foreach (CommonDataModel root in ComponentRoots)
            {
                if (root.GetValue("Preinstall").Equals("1"))
                {
                    root.Add("Preinstall", "Preinstall");
                }
                else
                {
                    root.delete("Preinstall");
                }

                if (root.GetValue("DrDvd").Equals("True"))
                {
                    root.Add("DrDvd", "DRDVD");
                }
                else
                {
                    root.delete("DrDvd");
                }

                if (root.GetValue("ScriptPaq").Equals("True"))
                {
                    root.Add("ScriptPaq", "SoftPaq");
                }
                else
                {
                    root.delete("ScriptPaq");
                }

                if (root.GetValue("MsStore").Equals("True"))
                {
                    root.Add("MsStore", "Ms Store");
                }
                else
                {
                    root.delete("MsStore");
                }

                if (root.GetValue("FloppyDisk").Equals("1"))
                {
                    root.Add("FloppyDisk", "Internal Tool");
                }
                else
                {
                    root.delete("FloppyDisk");
                }

                if (root.GetValue("Patch").Equals("1"))
                {
                    root.Add("Patch", "Patch");
                }
                else
                {
                    root.delete("Patch");
                }

                if (root.GetValue("Binary").Equals("1"))
                {
                    root.Add("Binary", "Binary");
                }
                else
                {
                    root.delete("Binary");
                }

                if (root.GetValue("PreinstallROM").Equals("1"))
                {
                    root.Add("PreinstallROM", "Preinstall");
                }
                else
                {
                    root.delete("PreinstallROM");
                }

                if (root.GetValue("CAB").Equals("1"))
                {
                    root.Add("CAB", "CAB");
                }
                else
                {
                    root.delete("CAB");
                }

                if (root.GetValue("Softpaq").Equals("1"))
                {
                    root.Add("Softpaq", "Softpaq");
                }
                else
                {
                    root.delete("Softpaq");
                }

                if (root.GetValue("IconDesktop").Equals("True"))
                {
                    root.Add("IconDesktop", "Desktop");
                }
                else
                {
                    root.delete("IconDesktop");
                }
              
                if (root.GetValue("IconMenu").Equals("True"))
                {
                    root.Add("IconMenu", "Start Menu");
                }
                else
                {
                    root.delete("IconMenu");
                }

                if (root.GetValue("IconTray").Equals("True"))
                {
                    root.Add("IconTray", "System Tray");
                }
                else
                {
                    root.delete("IconTray");
                }

                if (root.GetValue("IconPanel").Equals("True"))
                {
                    root.Add("IconPanel", "Control Panel");
                }
                else
                {
                    root.delete("IconPanel");
                }

                if (root.GetValue("IconInfoCenter").Equals("True"))
                {
                    root.Add("IconInfoCenter", "Info Center");
                }
                else
                {
                    root.delete("IconInfoCenter");
                }

                if (root.GetValue("IconTile").Equals("True"))
                {
                    root.Add("IconTile", "Start Menu Tile");
                }
                else
                {
                    root.delete("IconTile");
                }

                if (root.GetValue("IconTaskBarIcon").Equals("True"))
                {
                    root.Add("IconTaskBarIcon", "Taskbar Pinned Icon ");
                }
                else
                {
                    root.delete("IconTaskBarIcon");
                }

                if (root.GetValue("SettingFWML").Equals("True"))
                {
                    root.Add("SettingFWML", "FWML");
                }
                else
                {
                    root.delete("SettingFWML");
                }

                if (root.GetValue("SettingUWPCompliant").Equals("True"))
                {
                    root.Add("SettingUWPCompliant", "FWML");
                }
                else
                {
                    root.delete("SettingUWPCompliant");
                }

                if (root.GetValue("RoyaltyBearing").Equals("True"))
                {
                    root.Add("RoyaltyBearing", "Royalty Bearing");
                }
                else
                {
                    root.delete("RoyaltyBearing");
                }

                if (root.GetValue("KoreanCertificationRequired").Equals("True"))
                {
                    root.Add("KoreanCertificationRequired", "Korean Certification Required");
                }
                else
                {
                    root.delete("KoreanCertificationRequired");
                }

                if (root.GetValue("WHQLCertificationRequire").Equals("1"))
                {
                    root.Add("WHQLCertificationRequire", "WHQL Certification Require");
                }
                else
                {
                    root.delete("WHQLCertificationRequire");
                }

                if (root.GetValue("LimitFuncTestGroupVisability").Equals("True"))
                {
                    root.Add("LimitFuncTestGroupVisability", "Limit partner visibility");
                }
                else
                {
                    root.delete("LimitFuncTestGroupVisability");
                }

                if (root.GetValue("Visibility").Equals("True"))
                {
                    root.Add("Visibility", "Active");
                }
                else
                {
                    root.delete("Visibility");
                }

                if (GetCDAsync(root).Equals(1))
                {
                    root.Add("CD", "CD");
                }
                root.delete("CDImage");
                root.delete("ISOImage");
                root.delete("AR");
                _output.Add(root);
            }
            return _output;
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
