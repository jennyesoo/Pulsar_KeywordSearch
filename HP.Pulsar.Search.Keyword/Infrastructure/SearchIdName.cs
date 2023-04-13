using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HP.Pulsar.Search.Keyword.CommonDataStructure;


namespace HP.Pulsar.Search.Keyword.Infrastructure
{
    internal static class SearchIdName
    {
        public static string GetIdName(SearchType searchType)
        {
            if (searchType == SearchType.Product)
            {
                return "product-";
            }
            else if (searchType == SearchType.Root)
            {
                return "root-";
            }
            else if (searchType == SearchType.Version)
            {
                return "version-";
            }
            else if (searchType == SearchType.DCR)
            {
                return "dcr-";
            }
            else if (searchType == SearchType.Feature)
            {
                return "feature-";
            }
            else if (searchType == SearchType.SuddenImpact)
            {
                return "suddenimpact-";
            }
            else if (searchType == SearchType.HPAMOPartNumber)
            {
                return "partnumber-";
            }
            return "";
        }
    }
}
