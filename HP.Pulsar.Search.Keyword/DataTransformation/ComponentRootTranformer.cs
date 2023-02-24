using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    internal class ComponentRootTranformer : IDataTranformer
    {
        public static List<string> DataPropertyList = new List<string> { "" };

        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> componentRoots)
        {
            foreach (CommonDataModel root in componentRoots)
            {
                foreach (string key in root.GetAllKeys())
                {
                    root.Add(key, DataProcessingInitializationCombination(root.GetValue(key), key));
                }
            }

            return componentRoots;
        }

        private string DataProcessingInitializationCombination(string propertyValue, string propertyName)
        {
            if (DataPropertyList.Contains(propertyName.ToLower()))
            {
                propertyValue = ChangeDateFormat(propertyValue);
            }

            propertyValue = AddPropertyName(propertyName, propertyValue);

            return propertyValue;
        }

        private string ChangeDateFormat(string propertyValue)
        {
            return propertyValue.Split(" ")[0];
        }

        private string AddPropertyName(string propertyName, string propertyValue)
        {
            if (string.Equals(propertyName, "ComponentRootId"))
            {
                return "Component Root Id : " + propertyValue;
            }
            else if (propertyName == "ComponentRootName")
            {
                return "Component Root Name : " + propertyValue;
            }
            else if (propertyName == "description")
            {
                return "description : " + propertyValue;
            }
            else if (propertyName == "VendorName")
            {
                return "Vendor Name : " + propertyValue;
            }
            else if (propertyName == "Category")
            {
                return "Category : " + propertyValue;
            }
            else if (propertyName == "PM")
            {
                return "PM : " + propertyValue;
            }
            else if (propertyName == "DeveloperName")
            {
                return "Developer Name : " + propertyValue;
            }
            else if (propertyName == "TesterName")
            {
                return "Tester Name : " + propertyValue;
            }
            else if (propertyName == "CoreTeam")
            {
                return "Core Team : " + propertyValue;
            }
            else if (propertyName == "ComponentType")
            {
                return "Component Type : " + propertyValue;
            }
            else if (propertyName == "ProductList")
            {
                return "Product : " + propertyValue;
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
}
