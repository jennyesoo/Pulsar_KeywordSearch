using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    public class ComponentVersionDataTranformer : IDataTranformer
    {
        //public string _filePath;
        //private readonly Lemmatizer lemmatizer;
        //public static List<string> _noLemmatization = new List<string> { "bios", "fxs", "os", "obs", "ots" };
        public static List<string> _dataPropertyList = new List<string> { "introdate" };

        public ComponentVersionDataTranformer()
        {
            //_filePath = "References\\full7z-mlteast-en-modified.lem";
            //var stream = File.OpenRead(_filePath);
            //lemmatizer = new Lemmatizer(stream);
        }

        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentVersions)
        {
            foreach (CommonDataModel rootversion in componentVersions)
            {
                foreach (string key in rootversion.GetKeys())
                {
                    rootversion.Add(key, DataProcessingInitializationCombination(rootversion.GetValue(key), key));
                }
            }
            return componentVersions;
        }

        private string DataProcessingInitializationCombination(string propertyValue, string propertyName)
        {
            if (_dataPropertyList.Contains(propertyName.ToLower()))
            {
                propertyValue = ChangeDateFormat(propertyValue);
            }
            //PropertyValue = AddPropertyName(propertyName, PropertyValue);
            return propertyValue;
        }

        //private string PluralToSingular(string sentence)
        //{
        //    PluralizationService service = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en-us"));
        //    var tokens = sentence.Split(" ");
        //    string results = "";
        //    if (tokens == null || !tokens.Any())
        //    {
        //        return results;
        //    }

        //    foreach (string token in tokens)
        //    {
        //        if (service.IsPlural(token))
        //        {
        //            results += " " + service.Singularize(token);
        //        }
        //        else
        //        {
        //            results += " " + token;
        //        }
        //    }
        //    return results;
        //}

        //private string Lemmatize(string sentence)
        //{
        //    var tokens = sentence.Split(" ");
        //    //Console.WriteLine(tokens);
        //    string results = "";

        //    if (lemmatizer == null || tokens == null || !tokens.Any())
        //    {
        //        return results;
        //    }

        //    foreach (string token in tokens)
        //    {
        //        if (_noLemmatization.Contains(token.ToLower()))
        //        {
        //            results += " " + token;
        //        }
        //        else
        //        {
        //            results += " " + lemmatizer.Lemmatize(token);
        //        }
        //    }

        //    return results;
        //}

        private string ChangeDateFormat(string propertyValue)
        {
            CultureInfo enUS = new CultureInfo("en-US");
            DateTime dateValue;

            if (DateTime.TryParseExact(propertyValue, "G", enUS,
                                     DateTimeStyles.None, out dateValue))
            {
                return dateValue.ToString("yyyy/MM/dd");
            }
            else
            {
                return propertyValue;
            }
        }

        private string AddPropertyName(string propertyName, string propertyValue)
        {
            if (propertyName == "ComponentVersionID")
            {
                return "Component Version ID : " + propertyValue;
            }
            else if (propertyName == "ComponentRootID")
            {
                return "Component Root ID : " + propertyValue;
            }
            else if (propertyName == "ComponentName")
            {
                return "Component Name : " + propertyValue;
            }
            else if (propertyName == "Version")
            {
                return "Version : " + propertyValue;
            }
            else if (propertyName == "Revision")
            {
                return "Revision : " + propertyValue;
            }
            else if (propertyName == "PrismSWType")
            {
                return "Prism SW Type : " + propertyValue;
            }
            else if (propertyName == "Pass")
            {
                return "Pass : " + propertyValue;
            }
            else if (propertyName == "Developer")
            {
                return "Developer : " + propertyValue;
            }
            else if (propertyName == "TestLead")
            {
                return "Test Lead : " + propertyValue;
            }
            else if (propertyName == "Vendor")
            {
                return "Vendor : " + propertyValue;
            }
            else if (propertyName == "SWPartNumber")
            {
                return "SW Part Number : " + propertyValue;
            }
            else if (propertyName == "BuildLevel")
            {
                return "Build Level : " + propertyValue;
            }
            else if (propertyName == "RecoveryOption")
            {
                return "Recovery Option : " + propertyValue;
            }
            else if (propertyName == "MD5")
            {
                return "MD5 : " + propertyValue;
            }
            else if (propertyName == "SHA256")
            {
                return "SHA256 : " + propertyValue;
            }
            else if (propertyName == "PropertyTabs")
            {
                return "Property Tabs Added : " + propertyValue;
            }
            else if (propertyName == "Active")
            {
                if (propertyValue.ToString() == "True")
                {
                    return "Visibility : Active" ;
                }
                return "Visibility : " ;
            }
            else if (propertyName == "TransferServer")
            {
                return "Transfer Server : " + propertyValue;
            }
            else if (propertyName == "SubmissionPath")
            {
                return "Submission Path : " + propertyValue;
            }
            else if (propertyName == "VendorVersion")
            {
                return "Vendor Version : " + propertyValue;
            }
            else if (propertyName == "Comments")
            {
                return "Comments : " + propertyValue;
            }
            else if (propertyName == "IntroDate")
            {
                return "Intro Date : " + propertyValue;
            }
            else if (propertyName == "EndOfLifeDate")
            {
                return "End Of Life Date : " + propertyValue;
            }
            else if (propertyName == "Preinstall")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "DrDvd")
            {
                return "DrDvd : " + propertyValue;
            }
            else if (propertyName == "Scriptpaq")
            {
                return "Packaging : " + propertyValue;
            }
            else if (propertyName == "MsStore")
            {
                return "Ms Store : " + propertyValue;
            }
            else if (propertyName == "FloppyDisk")
            {
                return "Floppy Disk : " + propertyValue;
            }
            else if (propertyName == "CD")
            {
                return "CD : " + propertyValue;
            }
            else if (propertyName == "IconDesktop")
            {
                return "Desktop : " + propertyValue;
            }
            else if (propertyName == "IconMenu")
            {
                return "Start Menu : " + propertyValue;
            }
            else if (propertyName == "IconTray")
            {
                return "System Tray : " + propertyValue;
            }
            else if (propertyName == "IconPanel")
            {
                return "Control Panel : " + propertyValue;
            }
            else if (propertyName == "IconInfoCenter")
            {
                return "Info Center : " + propertyValue;
            }
            else if (propertyName == "IconTile")
            {
                return "Start Menu Tile : " + propertyValue;
            }
            else if (propertyName == "IconTaskBarIcon")
            {
                return "Task Pinned Icon : " + propertyValue;
            }
            else if (propertyName == "SettingFWML")
            {
                return "FWML : " + propertyValue;
            }
            else if (propertyName == "SettingUWPCompliant")
            {
                return "UWP Compliant : " + propertyValue;
            }
            else if (propertyName == "Rompaq")
            {
                return "Binary : " + propertyValue;
            }
            else if (propertyName == "PreinstallROM")
            {
                return "ROM Components : " + propertyValue;
            }
            else if (propertyName == "CAB")
            {
                return "CAB : " + propertyValue;
            }
            return propertyValue;
        }
    }
}
