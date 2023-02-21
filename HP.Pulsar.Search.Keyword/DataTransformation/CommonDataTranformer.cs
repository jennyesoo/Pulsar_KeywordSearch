using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;
using LemmaSharp.Classes;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class CommonDataTranformer
{
    public string filePath;
    private readonly Lemmatizer lemmatizer;
    public static List<string> _noLemmatization = new List<string> { "bios", "fxs", "os", "obs", "ots" };
    public static List<string> DataPropertyList = new List<string> { "servicelifedate", "createddate", "latestupdatedate", "endofproduction" };

    public CommonDataTranformer()
    {
        DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        string Path = dir.Parent.Parent.Parent.Parent.FullName;
        filePath = Path + "\\tools\\AIModel\\full7z-mlteast-en-modified.lem";
        var stream = File.OpenRead(filePath);
        lemmatizer = new Lemmatizer(stream);
    }

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> products)
    {
        foreach (CommonDataModel product in products)
        {
            Dictionary<string, string>.KeyCollection keys = product.GetAllKeys();
            foreach (string key in keys)
            {
                product.Add(key, DataProcessingInitializationCombination(product.GetValue(key), key));
            }
        }
        return products;
    }

    /*
    private IEnumerable<CommonDataModel> GetDataProcessingData(IEnumerable<CommonDataModel> products)
    {
        foreach (CommonDataModel product in products)
        {
            Dictionary<string, string>.KeyCollection keys = product.GetAllKeys();
            foreach (string key in keys)
            {
                product.Add(key, DataProcessingInitializationCombination(product.GetValue(key), key));
            }
            
            product.Add("ProductId", DataProcessingInitializationCombination(product.GetValue("ProductId"), "ProductId"));
            product.Add("ProductName", DataProcessingInitializationCombination(product.GetValue("ProductName"), "ProductName"));
            product.Add("DevCenter", DataProcessingInitializationCombination(product.GetValue("DevCenter") , "DevCenter"));
            product.Add("Brands", DataProcessingInitializationCombination(product.GetValue("Brands"), "Brands"));
            product.Add("SystemBoardId", DataProcessingInitializationCombination(product.GetValue("SystemBoardId"), "SystemBoardId"));
            product.Add("ServiceLifeDate", DataProcessingInitializationCombination(product.GetValue("ServiceLifeDate"), "ServiceLifeDate"));
            product.Add("ProductStatus", DataProcessingInitializationCombination(product.GetValue("ProductStatus"), "ProductStatus"));
            product.Add("BusinessSegment", DataProcessingInitializationCombination(product.GetValue("BusinessSegment"), "BusinessSegment"));
            product.Add("CreatorName", DataProcessingInitializationCombination(product.GetValue("CreatorName"), "CreatorName"));
            product.Add("CreatedDate", DataProcessingInitializationCombination(product.GetValue("CreatedDate"), "CreatedDate"));
            product.Add("LastUpdaterName", DataProcessingInitializationCombination(product.GetValue("LastUpdaterName"), "LastUpdaterName"));
            product.Add("LatestUpdateDate", DataProcessingInitializationCombination(product.GetValue("LatestUpdateDate"), "LatestUpdateDate"));
            product.Add("SystemManager", DataProcessingInitializationCombination(product.GetValue("SystemManager"), "SystemManager"));
            product.Add("PlatformDevelopmentPM", DataProcessingInitializationCombination(product.GetValue("PlatformDevelopmentPM"), "PlatformDevelopmentPM"));
            product.Add("PlatformDevelopmentPMEmail", DataProcessingInitializationCombination(product.GetValue("PlatformDevelopmentPMEmail"), "PlatformDevelopmentPMEmail"));
            product.Add("SupplyChain", DataProcessingInitializationCombination(product.GetValue("SupplyChain"), "SupplyChain"));
            product.Add("SupplyChainEmail", DataProcessingInitializationCombination(product.GetValue("SupplyChainEmail"), "SupplyChainEmail"));
            product.Add("ODMSystemEngineeringPM", DataProcessingInitializationCombination(product.GetValue("ODMSystemEngineeringPM"), "ODMSystemEngineeringPM"));
            product.Add("ODMSystemEngineeringPMEmail", DataProcessingInitializationCombination(product.GetValue("ODMSystemEngineeringPMEmail"), "ODMSystemEngineeringPMEmail"));
            product.Add("ConfigurationManager", DataProcessingInitializationCombination(product.GetValue("ConfigurationManager"), "ConfigurationManager"));
            product.Add("ConfigurationManagerEmail", DataProcessingInitializationCombination(product.GetValue("ConfigurationManagerEmail"), "ConfigurationManagerEmail"));
            product.Add("CommodityPM", DataProcessingInitializationCombination(product.GetValue("CommodityPM"), "CommodityPM"));
            product.Add("CommodityPMEmail", DataProcessingInitializationCombination(product.GetValue("CommodityPMEmail"), "CommodityPMEmail"));
            product.Add("Service", DataProcessingInitializationCombination(product.GetValue("Service"), "Service"));
            product.Add("ServiceEmail", DataProcessingInitializationCombination(product.GetValue("ServiceEmail"), "ServiceEmail"));
            product.Add("ODMHWPM", DataProcessingInitializationCombination(product.GetValue("ODMHWPM"), "ODMHWPM"));
            product.Add("ODMHWPMEmail", DataProcessingInitializationCombination(product.GetValue("ODMHWPMEmail"), "ODMHWPMEmail"));
            product.Add("ProgramOfficeProgramManager", DataProcessingInitializationCombination(product.GetValue("ProgramOfficeProgramManager"), "ProgramOfficeProgramManager"));
            product.Add("ProgramOfficeProgramManagerEmail", DataProcessingInitializationCombination(product.GetValue("ProgramOfficeProgramManagerEmail"), "ProgramOfficeProgramManagerEmail"));
            product.Add("Quality", DataProcessingInitializationCombination(product.GetValue("Quality"), "Quality"));

            product. = DataProcessingInitializationCombination(product.SupplyChainEmail, "SupplyChainEmail");
            product. = DataProcessingInitializationCombination(product.ODMSystemEngineeringPM, "ODMSystemEngineeringPM");
            product. = DataProcessingInitializationCombination(product.ODMSystemEngineeringPMEmail, "ODMSystemEngineeringPMEmail");
            product. = DataProcessingInitializationCombination(product.ConfigurationManager, "ConfigurationManager");
            product. = DataProcessingInitializationCombination(product.ConfigurationManagerEmail, "ConfigurationManagerEmail");
            product. = DataProcessingInitializationCombination(product.CommodityPM, "CommodityPM");
            product. = DataProcessingInitializationCombination(product.CommodityPMEmail, "CommodityPMEmail");
            product. = DataProcessingInitializationCombination(product.Service, "Service");
            product. = DataProcessingInitializationCombination(product.ServiceEmail, "ServiceEmail");
            product. = DataProcessingInitializationCombination(product.ODMHWPM, "ODMHWPM");
            product. = DataProcessingInitializationCombination(product.ODMHWPMEmail, "ODMHWPMEmail");
            product. = DataProcessingInitializationCombination(product.ProgramOfficeProgramManager, "ProgramOfficeProgramManager");
            product. = DataProcessingInitializationCombination(product.ProgramOfficeProgramManagerEmail, "ProgramOfficeProgramManagerEmail");
            product. = DataProcessingInitializationCombination(product.Quality, "Quality");
            product.QualityEmail = DataProcessingInitializationCombination(product.QualityEmail, "QualityEmail");
            product.PlanningPM = DataProcessingInitializationCombination(product.PlanningPM, "PlanningPM");
            product.PlanningPMEmail = DataProcessingInitializationCombination(product.PlanningPMEmail, "PlanningPMEmail");
            product.BIOSPM = DataProcessingInitializationCombination(product.BIOSPM, "BIOSPM");
            product.BIOSPMEmail = DataProcessingInitializationCombination(product.BIOSPMEmail, "BIOSPMEmail");
            product.SystemsEngineeringPM = DataProcessingInitializationCombination(product.SystemsEngineeringPM, "SystemsEngineeringPM");
            product.SystemsEngineeringPMEmail = DataProcessingInitializationCombination(product.SystemsEngineeringPMEmail, "SystemsEngineeringPMEmail");
            product.MarketingProductMgmt = DataProcessingInitializationCombination(product.MarketingProductMgmt, "MarketingProductMgmt");
            product.MarketingProductMgmtEmail = DataProcessingInitializationCombination(product.MarketingProductMgmtEmail, "MarketingProductMgmtEmail");
            product.ProcurementPM = DataProcessingInitializationCombination(product.ProcurementPM, "ProcurementPM");
            product.ProcurementPMEmail = DataProcessingInitializationCombination(product.ProcurementPMEmail, "ProcurementPMEmail");
            product.SWMarketing = DataProcessingInitializationCombination(product.SWMarketing, "SWMarketing");
            product.SWMarketingEmail = DataProcessingInitializationCombination(product.SWMarketingEmail, "SWMarketingEmail");
            product.ProductFamily = DataProcessingInitializationCombination(product.ProductFamily, "ProductFamily");
            product.ODM = DataProcessingInitializationCombination(product.ODM, "ODM");
            product.ReleaseTeam = DataProcessingInitializationCombination(product.ReleaseTeam, "ReleaseTeam");
            product.RegulatoryModel = DataProcessingInitializationCombination(product.RegulatoryModel, "RegulatoryModel");
            product.Releases = DataProcessingInitializationCombination(product.Releases, "Releases");
            product.Description = DataProcessingInitializationCombination(product.Description, "Description");
            product.ProductLine = DataProcessingInitializationCombination(product.ProductLine, "ProductLine");
            product.PreinstallTeam = DataProcessingInitializationCombination(product.PreinstallTeam, "PreinstallTeam");
            product.MachinePNPID = DataProcessingInitializationCombination(product.MachinePNPID, "MachinePNPID");
            product.ComponentItems = DataProcessingInitializationCombination(product.ComponentItems, "ComponentItems");
            product.EndOfProduction = DataProcessingInitializationCombination(product.EndOfProduction, "EndOfProduction");
            product.ProductGroups = DataProcessingInitializationCombination(product.ProductGroups, "ProductGroups");
            product.WHQLstatus = DataProcessingInitializationCombination(product.WHQLstatus, "WHQLstatus");
            product.LeadProduct = DataProcessingInitializationCombination(product.LeadProduct, "LeadProduct");
            product.Chipsets = DataProcessingInitializationCombination(product.Chipsets, "Chipsets");
            product.CurrentBIOSVersions = DataProcessingInitializationCombination(product.CurrentBIOSVersions, "CurrentBIOSVersions");
            
        }
        return products;
    }
   */
    private string DataProcessingInitializationCombination(string PropertyValue, string propertyName)
    {
        if (DataPropertyList.Contains(propertyName.ToLower()))
        {
            PropertyValue = ChangeDateFormat(PropertyValue);
        }
        PropertyValue = AddPropertyName(propertyName, PropertyValue);
        return PropertyValue;
    }

    private string PluralToSingular(string sentence)
    {
        PluralizationService service = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
        var tokens = sentence.Split(" ");
        string results = "";
        if (tokens == null || !tokens.Any())
        {
            return results;
        }

        foreach (string token in tokens)
        {
            if (service.IsPlural(token))
            {
                results += " " + service.Singularize(token);
            }
            else
            {
                results += " " + token;
            }
        }
        return results;
    }

    private string Lemmatize(string sentence)
    {
        var tokens = sentence.Split(" ");
        //Console.WriteLine(tokens);
        string results = "";

        if (lemmatizer == null || tokens == null || !tokens.Any())
        {
            return results;
        }

        foreach (string token in tokens)
        {
            if (_noLemmatization.Contains(token.ToLower()))
            {
                results += " " + token;
            }
            else
            {
                results += " " + lemmatizer.Lemmatize(token);
            }
        }

        return results;
    }

    private string ChangeDateFormat(string PropertyValue)
    {
        return PropertyValue.Split(" ")[0];
    }

    private string AddPropertyName(string propertyName, string propertyValue)
    {
        if (propertyName == "ProductId")
        {
            return "product id : " + propertyValue;
        }
        else if (propertyName == "ProductName")
        {
            return "product name : " + propertyValue;
        }
        else if (propertyName == "Partner")
        {
            return "partner : " + propertyValue;
        }
        else if (propertyName == "DevCenter")
        {
            return "developer center : " + propertyValue;
        }
        else if (propertyName == "Brands")
        {
            return "brand  : " + propertyValue;
        }
        else if (propertyName == "SystemBoardId")
        {
            return "system board name : " + propertyValue;
        }
        else if (propertyName == "ServiceLifeDate")
        {
            return "service life date : " + propertyValue;
        }
        else if (propertyName == "ProductStatus")
        {
            return "product status : " + propertyValue;
        }
        else if (propertyName == "BusinessSegment")
        {
            return "business segment : " + propertyValue;
        }
        else if (propertyName == "ProductName")
        {
            return "product name : " + propertyValue;
        }
        else if (propertyName == "CreatorName")
        {
            return "creator name : " + propertyValue;
        }
        else if (propertyName == "CreatedDate")
        {
            return "create date : " + propertyValue;
        }
        else if (propertyName == "LastUpdaterName")
        {
            return "last updater name : " + propertyValue;
        }
        else if (propertyName == "LatestUpdateDate")
        {
            return "last updater date : " + propertyValue;
        }
        else if (propertyName == "SystemManager")
        {
            return "system manager : " + propertyValue;
        }
        else if (propertyName == "PlatformDevelopmentPM")
        {
            return "platform development pm : " + propertyValue;
        }
        else if (propertyName == "PlatformDevelopmentPMEmail")
        {
            return "platform development pm email : " + propertyValue;
        }
        else if (propertyName == "SupplyChain")
        {
            return "supply chain : " + propertyValue;
        }
        else if (propertyName == "SupplyChainEmail")
        {
            return "supply chain email : " + propertyValue;
        }
        else if (propertyName == "ODMSystemEngineeringPM")
        {
            return "ODM System Engineering PM : " + propertyValue;
        }
        else if (propertyName == "ODMSystemEngineeringPMEmail")
        {
            return "ODM System Engineering PM email: " + propertyValue;
        }
        else if (propertyName == "ConfigurationManager")
        {
            return "Configuration Manager : " + propertyValue;
        }
        else if (propertyName == "ConfigurationManagerEmail")
        {
            return "Configuration Manager email : " + propertyValue;
        }
        else if (propertyName == "CommodityPM")
        {
            return "Commodity PM : " + propertyValue;
        }
        else if (propertyName == "CommodityPMEmail")
        {
            return "Commodity PM Email : " + propertyValue;
        }
        else if (propertyName == "Service")
        {
            return "Service : " + propertyValue;
        }
        else if (propertyName == "ServiceEmail")
        {
            return "Service Email : " + propertyValue;
        }
        else if (propertyName == "ODMHWPM")
        {
            return "ODM HW PM : " + propertyValue;
        }
        else if (propertyName == "ODMHWPMEmail")
        {
            return "ODM HW PM Email : " + propertyValue;
        }
        else if (propertyName == "ProgramOfficeProgramManager")
        {
            return "Program Office Program Manager : " + propertyValue;
        }
        else if (propertyName == "Quality")
        {
            return "Quality : " + propertyValue;
        }
        else if (propertyName == "QualityEmail")
        {
            return "Quality email : " + propertyValue;
        }
        else if (propertyName == "PlanningPM")
        {
            return "Plan PM : " + propertyValue;
        }
        else if (propertyName == "PlanningPMEmail")
        {
            return "Plan PM email : " + propertyValue;
        }
        else if (propertyName == "BIOSPM")
        {
            return "BIOS PM : " + propertyValue;
        }
        else if (propertyName == "BIOSPMEmail")
        {
            return "BIOS PM email : " + propertyValue;
        }
        else if (propertyName == "SystemsEngineeringPM")
        {
            return "System Engineering PM : " + propertyValue;
        }
        else if (propertyName == "SystemsEngineeringPMEmail")
        {
            return "System Engineering PM email : " + propertyValue;
        }
        else if (propertyName == "MarketingProductMgmt")
        {
            return "Market Product Mgmt : " + propertyValue;
        }
        else if (propertyName == "MarketingProductMgmtEmail")
        {
            return "Market Product Mgmt email : " + propertyValue;
        }
        else if (propertyName == "ProcurementPM")
        {
            return "Procurement PM : " + propertyValue;
        }
        else if (propertyName == "ProcurementPMEmail")
        {
            return "Procurement PM Email : " + propertyValue;
        }
        else if (propertyName == "SWMarketing")
        {
            return "SW Market : " + propertyValue;
        }
        else if (propertyName == "SWMarketingEmail")
        {
            return "SW Market email : " + propertyValue;
        }
        else if (propertyName == "ProductFamily")
        {
            return "Product Family : " + propertyValue;
        }
        else if (propertyName == "ODM")
        {
            return "ODM : " + propertyValue;
        }
        else if (propertyName == "ReleaseTeam")
        {
            return "Release Team : " + propertyValue;
        }
        else if (propertyName == "RegulatoryModel")
        {
            return "Regulatory Model : " + propertyValue;
        }
        else if (propertyName == "Releases")
        {
            return "Release : " + propertyValue;
        }
        else if (propertyName == "Description")
        {
            return "Description : " + propertyValue;
        }
        else if (propertyName == "ProductLine")
        {
            return "Product Line : " + propertyValue;
        }
        else if (propertyName == "PreinstallTeam")
        {
            return "Preinstall Team : " + propertyValue;
        }
        else if (propertyName == "MachinePNPID")
        {
            return "Machine PNP ID : " + propertyValue;
        }
        else if (propertyName == "EndOfProduction")
        {
            return "End Of Production : " + propertyValue;
        }
        else if (propertyName == "WHQLstatus")
        {
            return "WHQL status : " + propertyValue;
        }
        else if (propertyName == "LeadProduct")
        {
            return "Lead Product : " + propertyValue;
        }
        else if (propertyName == "Chipsets")
        {
            return "Chipset : " + propertyValue;
        }
        else if (propertyName == "ComponentItems")
        {
            return "Component Item : " + propertyValue;
        }
        else if (propertyName == "ProductGroups")
        {
            return "Product Group : " + propertyValue;
        }
        else if (propertyName == "CurrentBIOSVersions")
        {
            return "Current BIOS Version : " + propertyValue;
        }
        else if (propertyName == "target")
        {
            return propertyValue;
        }
        else if (propertyName == "Id")
        {
            return propertyValue;
        }
        return propertyValue;
    }
}
