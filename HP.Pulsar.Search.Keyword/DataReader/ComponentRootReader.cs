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

            List<Task> tasks = new()
            {
                GetPropertyValueAsync(componentRoot),
                GetComponentRootListAsync(componentRoot),
                GetTrulyLinkedFeaturesAsync(componentRoot),
                GetLinkedFeaturesAsync(componentRoot),
                GetComponentInitiatedLinkageAsync(componentRoot)
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
    root.Binary,
    root.FloppyDisk,
    root.CertRequired AS WHQLCertificationRequire,
    root.ScriptPaq as PackagingSoftpaq,
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
    og.Name as SiFunctionTestGroup, 
    root.Active as Visibility,
    root.Notes as InternalNotes,
    root.CDImage,
    root.ISOImage,
    root.AR,
    root.FloppyDisk,
    root.IsSoftPaqInPreinstall,
    root.IconMenu,
    root.RootFilename as ROMFamily,
    root.Rompaq,
    root.PreinstallROM,
    root.CAB,
    root.Softpaq as ROMSoftpaq
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
LEFT JOIN OTSFVTOrganizations og on root.OTSFVTOrganizationID  = og.id 
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
select fr.ComponentRootId,
        fr.FeatureId,
        f.FeatureName
from Feature_Root fr
JOIN Feature f WITH (NOLOCK) ON fr.FeatureID = f.FeatureID 
where ComponentRootId >= 1 and AutoLinkage = 1";
        }

        private string GetTSQLLinkedFeaturesCommandText()
        {
            return @"
select fr.ComponentRootId,
        fr.FeatureId,
        f.FeatureName
from Feature_Root fr
JOIN Feature f WITH (NOLOCK) ON fr.FeatureID = f.FeatureID 
where ComponentRootId >= 1 and AutoLinkage = 0";
        }

        private string GetTSQLComponentInitiatedLinkageCommandText()
        {
            return @"
SELECT fril.FeatureId AS FeatureId,
    fril.ComponentRootId,
    f.FeatureName AS FeatureName
FROM Feature_Root_InitiatedLinkage fril WITH (NOLOCK)
LEFT JOIN feature f WITH (NOLOCK) ON f.featureID = fril.FeatureId";
        }

        private string GetTSQLProductListCommandText()
        {
            return @"SELECT DR.Id AS ComponentRootId,
    stuff((
            SELECT ' , ' + (CONVERT(VARCHAR, p.Id) + ' ' + p.DOTSName)
            FROM ProductVersion p
            left JOIN ProductStatus ps ON ps.id = p.ProductStatusID
            left JOIN Product_DelRoot pr ON pr.ProductVersionId = p.id
            left JOIN DeliverableRoot root ON root.Id = pr.DeliverableRootId
            WHERE root.Id = DR.Id
                AND ps.Name <> 'Inactive'
                AND p.FusionRequirements = 1
            ORDER BY root.Id
            FOR XML path('')
            ), 1, 3, '') AS ProductList
FROM DeliverableRoot DR
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

        // TODO - performance improvement needed
        private async Task GetComponentRootListAsync(IEnumerable<CommonDataModel> componentRoots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();
            SqlCommand command = new(GetTSQLProductListCommandText(), connection);
            using SqlDataReader reader = command.ExecuteReader();
            Dictionary<int, string> productList = new();

            while (reader.Read())
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

        private async Task<IEnumerable<CommonDataModel>> GetComponentRootAsync()
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();

            SqlCommand command = new(GetAllComponentRootSqlCommandText(), connection);
            SqlParameter parameter = new("ComponentRootId", -1);
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            List<CommonDataModel> output = new();

            while (reader.Read())
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
                        !string.Equals(reader[i].ToString(),"None"))
                    {
                        string columnName = reader.GetName(i);
                        string value = reader[i].ToString().Trim();
                        root.Add(columnName, value);
                    }
                }
                root.Add("target", "Component Root");
                root.Add("Id", SearchIdName.Root + root.GetValue("ComponentRootId"));
                output.Add(root);
            }
            return output;
        }

        private async Task GetPropertyValueAsync(IEnumerable<CommonDataModel> componentRoots)
        {
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

                if (root.GetValue("PackagingSoftpaq").Equals("1"))
                {
                    root.Add("PackagingSoftpaq", "Packaging Softpaq");
                }
                else
                {
                    root.Delete("PackagingSoftpaq");
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
                    root.Add("PreinstallROM", "ROM Component Preinstall");
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

                if (root.GetValue("ROMSoftpaq").Equals("1"))
                {
                    root.Add("ROMSoftpaq", "ROM component Softpaq");
                }
                else
                {
                    root.Delete("ROMSoftpaq");
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

                if (root.GetValue("IsSoftPaqInPreinstall").Equals("1"))
                {
                    root.Add("IsSoftPaqInPreinstall", "SoftPaq In Preinstall");
                }
                else
                {
                    root.Delete("IsSoftPaqInPreinstall");
                }

                if (root.GetValue("Rompaq").Equals("1"))
                {
                    root.Add("Rompaq", "Rompaq Binary");
                }
                else
                {
                    root.Delete("Rompaq");
                }

                if (GetCDAsync(root).Equals(1))
                {
                    root.Add("CD", "CD");
                }
                root.Delete("CDImage");
                root.Delete("ISOImage");
                root.Delete("AR");
            }
        }

        private async Task<int> GetCDAsync(CommonDataModel root)
        {
            if (root.GetValue("CDImage").Equals("1"))
            {
                return 1;
            }
            if (root.GetValue("ISOImage").Equals("1"))
            {
                return 1;
            }
            if (root.GetValue("AR").Equals("1"))
            {
                return 1;
            }
            return 0;
        }

        private async Task GetTrulyLinkedFeaturesAsync(IEnumerable<CommonDataModel> roots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();
            SqlCommand command = new(GetTSQLTrulyLinkedFeaturesCommandText(), connection);

            using SqlDataReader reader = command.ExecuteReader();
            Dictionary<int, List<(string,string)>> trulyLinkedFeatures = new();

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

        private async Task GetLinkedFeaturesAsync(IEnumerable<CommonDataModel> roots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();
            SqlCommand command = new(GetTSQLLinkedFeaturesCommandText(), connection);

            using SqlDataReader reader = command.ExecuteReader();
            Dictionary<int, List<(string,string)>> linkedFeatures = new();

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

        private async Task GetComponentInitiatedLinkageAsync(IEnumerable<CommonDataModel> roots)
        {
            using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
            await connection.OpenAsync();
            SqlCommand command = new(GetTSQLComponentInitiatedLinkageCommandText(), connection);

            using SqlDataReader reader = command.ExecuteReader();
            Dictionary<int, List<(string,string)>> componentInitiatedLinkage = new();

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
}
