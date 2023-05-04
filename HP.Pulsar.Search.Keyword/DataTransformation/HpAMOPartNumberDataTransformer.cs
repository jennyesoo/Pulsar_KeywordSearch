using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation
{
    internal class HpAMOPartNumberDataTransformer : IDataTransformer
    {
        private static readonly List<string> _datePropertyList = new() { "RTPDate", "SADate", "GADate", "EMDate", "GSEOLDate", "ESDate" };

        public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> changeRequests)
        {
            foreach (CommonDataModel dcr in changeRequests)
            {
                foreach (string key in dcr.GetKeys())
                {
                    dcr.Add(key, CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, dcr.GetValue(key), key));
                }
            }
            return changeRequests;
        }
    }
}
