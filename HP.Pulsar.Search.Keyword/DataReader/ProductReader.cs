using HP.Pulsar.Search.Keyword.CommonDataStructure;
using HP.Pulsar.Search.Keyword.Infrastructure;
using Microsoft.Data.SqlClient;

namespace HP.Pulsar.Search.Keyword.DataReader;

public class ProductReader : IKeywordSearchDataReader
{
    private ConnectionStringProvider _csProvider;

    public ProductReader(KeywordSearchInfo info)
    {
        _csProvider = new(info.Environment);
    }

    public async Task<IEnumerable<CommonDataModel>> GetDataAsync()
    {
        Console.WriteLine("Read Data");
        (IEnumerable<CommonDataModel> Products, Dictionary<string, string> BusinessSegments) = await GetProductsAsync();
        IEnumerable<CommonDataModel>  products = await GetEndOfProductionAsync(Products);
        products = await GetProductGroupsAsync(products);
        products = await GetWHQLstatusAsync(products);
        products = await GetLeadProductAsync(products, BusinessSegments);
        products = await GetChipsetsAsync(products);
        products = await GetCurrentBIOSVersionsAsync(products);
        return products;
    }

    private string GetAllProductsSqlCommandText()
    {
        return @"
                SELECT p.id AS ProductId,
                    DOTSName AS ProductName,
                    partner.name AS Partner,
                    pdc.Name AS DevCenter,
                    Brands,
                    p.SystemBoardId,
                    ServiceLifeDate,
                    ps.Name AS ProductStatus,
                    sg.Name AS BusinessSegment,
                    p.CreatedBy AS CreatorName,
                    p.Created AS CreatedDate,
                    p.UpdatedBy AS LastUpdaterName,
                    p.Updated AS LatestUpdateDate,
                    user_SMID.FirstName + ' ' + user_SMID.LastName AS SystemManager,
                    user_SMID.Email AS SystemManagerEmail,
                    user_PDPM.FirstName + ' ' + user_PDPM.LastName AS PlatformDevelopmentPM,
                    user_PDPM.Email AS PlatformDevelopmentPMEmail,
                    user_SCID.FirstName + ' ' + user_SCID.LastName AS SupplyChain,
                    user_SCID.Email AS SupplyChainEmail,
                    user_ODMSEPM.FirstName + ' ' + user_ODMSEPM.LastName AS ODMSystemEngineeringPM,
                    user_ODMSEPM.Email AS ODMSystemEngineeringPMEmail,
                    user_CM.FirstName + ' ' + user_CM.LastName AS ConfigurationManager,
                    user_CM.Email AS ConfigurationManagerEmail,
                    user_CPM.FirstName + ' ' + user_CPM.LastName AS CommodityPM,
                    user_CPM.Email AS CommodityPMEmail,
                    user_Service.FirstName + ' ' + user_Service.LastName AS Service,
                    user_Service.Email AS ServiceEmail,
                    user_ODMHWPM.FirstName + ' ' + user_ODMHWPM.LastName AS ODMHWPM,
                    user_ODMHWPM.Email AS ODMHWPMEmail,
                    user_POPM.FirstName + ' ' + user_POPM.LastName AS ProgramOfficeProgramManager,
                    user_POPM.Email AS ProgramOfficeProgramManagerEmail,
                    user_Quality.FirstName + ' ' + user_Quality.LastName AS Quality,
                    user_Quality.Email AS QualityEmail,
                    user_PPM.FirstName + ' ' + user_PPM.LastName AS PlanningPM,
                    user_PPM.Email AS PlanningPMEmail,
                    user_BIOSPM.FirstName + ' ' + user_BIOSPM.LastName AS BIOSPM,
                    user_BIOSPM.Email AS BIOSPMEmail,
                    user_SEPM.FirstName + ' ' + user_SEPM.LastName AS SystemsEngineeringPM,
                    user_SEPM.Email AS SystemsEngineeringPMEmail,
                    user_MPM.FirstName + ' ' + user_MPM.LastName AS MarketingProductMgmt,
                    user_MPM.Email AS MarketingProductMgmtEmail,
                    user_ProPM.FirstName + ' ' + user_ProPM.LastName AS ProcurementPM,
                    user_ProPM.Email AS ProcurementPMEmail,
                    user_SWM.FirstName + ' ' + user_SWM.LastName AS SWMarketing,
                    user_SWM.Email AS SWMarketingEmail,
                    pf.Name AS ProductFamily,
                    partner.name AS ODM,
                    pis.Name AS ReleaseTeam,
                    p.RegulatoryModel AS RegulatoryModel,
                    STUFF((
                            SELECT ',' + new_releases.Releases
                            FROM (
                                SELECT ProductVersion.id AS ProductId,
                                    pvr.Name AS Releases
                                FROM ProductVersion
                                FULL JOIN ProductVersion_Release pv_r ON pv_r.ProductVersionID = ProductVersion.ID
                                FULL JOIN ProductVersionRelease pvr ON pvr.ID = pv_r.ReleaseID
                                FULL JOIN ProductStatus ps ON ps.id = ProductVersion.ProductStatusID
                                WHERE ps.Name <> 'Inactive'
                                    AND p.FusionRequirements = 1
                                    AND ProductVersion.ID = p.id
                                ) AS new_releases
                            FOR XML PATH('')
                            ), 1, 1, '') AS Releases,
                    p.Description,
                    pl.Name + '-' + pl.Description AS ProductLine,
                    CASE 
                        WHEN p.PreinstallTeam = - 1
                            THEN ''
                        WHEN p.PreinstallTeam = 1
                            THEN 'Houston'
                        WHEN p.PreinstallTeam = 2
                            THEN 'Taiwan'
                        WHEN p.PreinstallTeam = 3
                            THEN 'Singapore'
                        WHEN p.PreinstallTeam = 4
                            THEN 'Brazil'
                        WHEN p.PreinstallTeam = 5
                            THEN 'CDC'
                        WHEN p.PreinstallTeam = 6
                            THEN 'Houston – Thin Client'
                        WHEN p.PreinstallTeam = 7
                            THEN 'Mobility'
                        WHEN p.PreinstallTeam = 8
                            THEN ''
                        END AS PreinstallTeam,
                    p.MachinePNPID AS MachinePNPID,
                    p.BusinessSegmentID
                FROM ProductVersion p
                FULL JOIN ProductFamily pf ON p.ProductFamilyId = pf.id
                FULL JOIN Partner partner ON partner.id = p.PartnerId
                FULL JOIN ProductDevCenter pdc ON pdc.ProductDevCenterId = DevCenter
                FULL JOIN ProductStatus ps ON ps.id = p.ProductStatusID
                FULL JOIN BusinessSegment sg ON sg.BusinessSegmentID = p.BusinessSegmentID
                FULL JOIN PreinstallTeam pis ON pis.ID = p.ReleaseTeam
                FULL JOIN UserInfo user_SMID ON user_SMID.userid = p.SMID
                FULL JOIN UserInfo user_PDPM ON user_PDPM.userid = p.PlatformDevelopmentID
                FULL JOIN UserInfo user_SCID ON user_SCID.userid = p.SupplyChainID
                FULL JOIN UserInfo user_ODMSEPM ON user_ODMSEPM.userid = p.ODMSEPMID
                FULL JOIN UserInfo user_CM ON user_CM.userid = p.PMID
                FULL JOIN UserInfo user_CPM ON user_CPM.userid = p.PDEID
                FULL JOIN UserInfo user_Service ON user_Service.userid = p.ServiceID
                FULL JOIN UserInfo user_ODMHWPM ON user_ODMHWPM.userid = p.ODMHWPMID
                FULL JOIN UserInfo user_POPM ON user_POPM.userid = p.TDCCMID
                FULL JOIN UserInfo user_Quality ON user_Quality.userid = p.QualityID
                FULL JOIN UserInfo user_PPM ON user_PPM.userid = p.PlanningPMID
                FULL JOIN UserInfo user_BIOSPM ON user_BIOSPM.userid = p.BIOSLeadID
                FULL JOIN UserInfo user_SEPM ON user_SEPM.userid = p.SEPMID
                FULL JOIN UserInfo user_MPM ON user_MPM.userid = p.ConsMarketingID
                FULL JOIN UserInfo user_ProPM ON user_ProPM.userid = p.ProcurementPMID
                FULL JOIN UserInfo user_SWM ON user_SWM.userid = p.SwMarketingId
                FULL JOIN ProductLine pl ON pl.Id = p.ProductLineId
                WHERE ps.Name <> 'Inactive'
                    AND p.FusionRequirements = 1
                    AND (@ProductId = -1 OR p.ProductVersionId = @ProductId)
                ";
    }

    // This function is to get all products
    private async Task<(IEnumerable<CommonDataModel>, Dictionary<string, string>)> GetProductsAsync()
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        SqlCommand command = new(GetAllProductsSqlCommandText(), connection);

        SqlParameter parameter = new("ProductId", -1);
        command.Parameters.Add(parameter);

        await connection.OpenAsync();

        using SqlDataReader reader = command.ExecuteReader();

        // This is for ID in Meilisearch
        int count = 0;
        List<CommonDataModel> output = new();
        Dictionary<string, string> businessSegments = new();

        while (reader.Read())
        {
            //string businessSegmentId;
            CommonDataModel product = new();
            int fieldCount = reader.FieldCount;
            string businessSegmentId = string.Empty;
            string productId = string.Empty;

            for (int i = 0; i < fieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i))
                {
                    continue;
                }

                string columnName = reader.GetName(i);
                string value = reader[i].ToString();

                if (string.Equals("BusinessSegmentID", columnName, StringComparison.OrdinalIgnoreCase))
                {
                    businessSegmentId = value;
                }
                else 
                {
                    product.Add(columnName, value);
                }

                if (string.Equals("ProductId", columnName, StringComparison.OrdinalIgnoreCase))
                {
                    productId = value;
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
            businessSegments[productId.ToString()] = businessSegmentId;
            product.Add("target", "product");
            product.Add("Id", count.ToString());
            output.Add(product);
            count++;
        }
        return (output, businessSegments);
    }

    private string GetTSQLEndOfProductionCommandText()
    {
        return @"Exec usp_SelectEndOfProduction @ProductId";
    }
    private string GetTSQLProductGroupsCommandText()
    {
        return @"Exec usp_ListWHQLSubmissions @ProductId";
    }
    private string GetTSQLWHQLstatusCommandText()
    {
        return @"Exec usp_ListWHQLSubmissions @ProductId";
    }
    private string GetTSQLLeadproductCommandText()
    {
        return
                "Declare @leadproductTable Table(ID int, " +
                "Name varchar(100)," +
                "Active INT, " +
                "OnProduct int," +
                "BusinessSegmantID int," +
                "Cyc int," +
                "Yrs int ," +
                "LeadProductreleaseID int ," +
                "LeadProductreleaseDesc varchar(100)," +
                "LeadproductVersionId int ," +
                "LeadproductVersionReleaseId int ," +
                "RtmWave varchar(100))" +
                @"Insert @leadproductTable exec usp_ProductVersion_release @BusinessSegmentID " +
                "Select CONCAT (LeadProductreleaseDesc, ',') as LeadProductreleaseDesc  from @leadproductTable where LeadProductreleaseDesc != '' ";
    }
    private string GetTSQLChipsetsCommandText()
    {
        return @"Exec usp_getproductchipsets  @ProductId";
    }
    private string GetTSQLCurrentBIOSVersionsCommandText()
    {
        return @"Select currentROM,currentWebROM From ProductVersion where ID =  @ProductId";
    }
    private string GetTSQLCurrentROMCommandText()
    {
        return @"exec spListTargetedbiosversions @ProductId";
    }


    private async Task<IEnumerable<CommonDataModel>> GetEndOfProductionAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> EndOfProduction = new List<string>();
            SqlCommand command = new(GetTSQLEndOfProductionCommandText(), connection);
            SqlParameter parameter = new SqlParameter("ProductId", product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                EndOfProduction.Add(reader["EndOfProduction"].ToString());
            }

            if (EndOfProduction.Count == 0)
            {
                product.Add("EndOfProduction","");
            }
            else
            {
                product.Add("EndOfProduction", EndOfProduction[0]);
            }
        }

        return products;
    }

    private async Task<IEnumerable<CommonDataModel>> GetProductGroupsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> ProductGroups = new List<string>();
            SqlCommand command = new(GetTSQLProductGroupsCommandText(), connection);
            SqlParameter parameter = new SqlParameter("ProductId", product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                ProductGroups.Add(reader["fullname"].ToString());
            }

            if (ProductGroups.Count == 0)
            {
                product.Add("ProductGroups", "");
            }
            else
            {
                product.Add("ProductGroups", ProductGroups[0]);
            }
        }

        return products;
    }

    private async Task<IEnumerable<CommonDataModel>> GetWHQLstatusAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> WHQLstatus = new List<string>();
            SqlCommand command = new(GetTSQLWHQLstatusCommandText(), connection);
            SqlParameter parameter = new SqlParameter("ProductId", product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                WHQLstatus.Add(reader["ID"].ToString());
            }

            if (WHQLstatus.Count == 0)
            {
                product.Add("WHQLstatus", "incomplete");
            }
            else
            {
                product.Add("WHQLstatus", "Unknown");
            }
        }

        return products;
    }

    private async Task<IEnumerable<CommonDataModel>> GetLeadProductAsync(IEnumerable<CommonDataModel> products, Dictionary<string, string> BusinessSegments)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        int count = 0;

        foreach (CommonDataModel product in products)
        {
            List<string> Leadproduct = new List<string>();
            SqlCommand command = new(GetTSQLLeadproductCommandText(), connection);
            SqlParameter parameter = new SqlParameter("BusinessSegmentID", BusinessSegments[product.GetValue("ProductId")]);
            count++;
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Leadproduct.Add(reader["LeadProductreleaseDesc"].ToString());
            }

            if (Leadproduct.Count == 0)
            {
                product.Add("LeadProduct", "");
            }
            else
            {
                product.Add("LeadProduct", Leadproduct[0]);
            }
        }

        return products;
    }

    private async Task<IEnumerable<CommonDataModel>> GetChipsetsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> Chipsets = new List<string>();
            SqlCommand command = new(GetTSQLChipsetsCommandText(), connection);
            SqlParameter parameter = new SqlParameter("ProductId", product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            string chipsets_value = "";
            while (reader.Read())
            {
                if (reader["Selected"].ToString() == "1")
                {
                    if (reader["State"].ToString() == "True")
                    {
                        if (chipsets_value == "")
                        {
                            chipsets_value += reader["CodeName"];
                        }
                        else
                        {
                            chipsets_value += " , " + reader["CodeName"];
                        }
                    }
                    else
                    {
                        if (chipsets_value == "")
                        {
                            chipsets_value += reader["CodeName"] + "Inactive";
                        }
                        else
                        {
                            chipsets_value += chipsets_value += " , " + reader["CodeName"];
                        }
                    }
                }
                Chipsets.Add(chipsets_value);
            }

            product.Add("Chipsets", Chipsets[0]);
        }
        return products;
    }

    private async Task<IEnumerable<CommonDataModel>> GetCurrentBIOSVersionsAsync(IEnumerable<CommonDataModel> products)
    {
        using SqlConnection connection = new(_csProvider.GetSqlServerConnectionString());
        await connection.OpenAsync();

        foreach (CommonDataModel product in products)
        {
            List<string> CurrentBIOSVersions = new List<string>();
            SqlCommand command = new(GetTSQLCurrentBIOSVersionsCommandText(), connection);
            SqlParameter parameter = new SqlParameter("ProductId", product.GetValue("ProductId"));
            command.Parameters.Add(parameter);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string CurrentROM_value = reader["currentROM"].ToString();
                string CurrentWebROM_value = reader["currentWebROM"].ToString();
                string[] value_List = { "Development", "Definition" };

                if (CurrentROM_value == "" && (product.GetValue("ProductStatus") == "Development" || product.GetValue("ProductStatus") == "Definition"))
                {
                    using SqlConnection connection_CurrentROM = new(_csProvider.GetSqlServerConnectionString());
                    SqlCommand command_CurrentROM = new(GetTSQLCurrentROMCommandText(), connection_CurrentROM);
                    await connection_CurrentROM.OpenAsync();
                    SqlParameter parameter_CurrentROM = new SqlParameter("ProductId", product.GetValue("ProductId"));
                    command_CurrentROM.Parameters.Add(parameter_CurrentROM);
                    using SqlDataReader reader_CurrentROM = command_CurrentROM.ExecuteReader();
                    while (reader_CurrentROM.Read())
                    {
                        CurrentROM_value = "Targeted: " + reader_CurrentROM["TargetedVersions"];
                    }
                }
                else if (!value_List.Contains(product.GetValue("ProductStatus")))
                {
                    if (CurrentROM_value == "")
                    {
                        CurrentROM_value = "Factory: ";
                    }
                    else
                    {
                        CurrentROM_value = "Factory: UnKnown";
                    }
                }
                if (CurrentROM_value != "" && CurrentWebROM_value != "")
                {
                    CurrentROM_value += "Web: " + CurrentWebROM_value;
                }
                else if (CurrentROM_value == "" && CurrentWebROM_value != "")
                {
                    CurrentROM_value = "Web: " + CurrentWebROM_value;
                }
                product.Add("CurrentBIOSVersions", CurrentROM_value);
            }
        }

        return products;
    }
}
