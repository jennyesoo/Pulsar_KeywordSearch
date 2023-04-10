using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    public class FeatureDataTranformer
    {
        //private string _filePath;
        //private readonly Lemmatizer lemmatizer;
        //private static List<string> _noLemmatization = new() { "bios", "fxs", "os", "obs", "ots" };
        private static List<string> _dataPropertyList = new() { "created", "updated" };

        public FeatureDataTranformer()
        {
            //_filePath = "References\\full7z-mlteast-en-modified.lem";
            //FileStream stream = File.OpenRead(_filePath);
            //lemmatizer = new Lemmatizer(stream);
        }
        
        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> features)
        {
            foreach (CommonDataModel feature in features)
            {
                foreach (string key in feature.GetKeys())
                {
                    feature.Add(key, DataProcessingInitializationCombination(feature.GetValue(key), key));
                }
            }
            return features;
        }

        private string DataProcessingInitializationCombination(string propertyValue, string propertyName)
        {
            if (_dataPropertyList.Contains(propertyName.ToLower()))
            {
                propertyValue = ChangeDateFormat(propertyValue);
            }
            return propertyValue;
        }

        private string ChangeDateFormat(string propertyValue)
        {
            DateTime dateValue;
            if (DateTime.TryParse(propertyValue, out dateValue))
            {
                return dateValue.ToString("yyyy/MM/dd");
            }
            else
            {
                return propertyValue;
            }
        }

    }
}
