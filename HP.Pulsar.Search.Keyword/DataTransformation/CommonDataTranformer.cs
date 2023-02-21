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
