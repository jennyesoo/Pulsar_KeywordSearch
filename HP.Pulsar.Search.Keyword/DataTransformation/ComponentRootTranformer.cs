using HP.Pulsar.Search.Keyword.CommonDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    internal class ComponentRootTranformer
    {
        public static List<string> DataPropertyList = new List<string> { "" };

        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> ComponentRoots)
        {
            foreach (CommonDataModel ComponentRoot in ComponentRoots)
            {
                Dictionary<string, string>.KeyCollection keys = ComponentRoot.GetAllKeys();
                foreach (string key in keys)
                {
                    ComponentRoot.Add(key, DataProcessingInitializationCombination(ComponentRoot.GetValue(key), key));
                }
            }
            return ComponentRoots;
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

        private string ChangeDateFormat(string PropertyValue)
        {
            return PropertyValue.Split(" ")[0];
        }

        private string AddPropertyName(string propertyName, string propertyValue)
        {
            if (propertyName == "ComponentRootId")
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
