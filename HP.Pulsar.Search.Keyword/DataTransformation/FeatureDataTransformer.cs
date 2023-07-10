﻿using System.Globalization;
using HP.Pulsar.Search.Keyword.CommonDataStructure;

namespace HP.Pulsar.Search.Keyword.DataTransformation;

public class FeatureDataTransformer
{
    private static readonly List<string> _datePropertyList = new() { "created Date", "updated Date" };
    private static readonly List<string> _userNamePropertyList = new() { "Created by", "Updated by" };

    public IEnumerable<CommonDataModel> Transform(IEnumerable<CommonDataModel> features)
    {
        if (!features.Any())
        {
            return features;
        }

        foreach (CommonDataModel feature in features)
        {
            foreach (string key in feature.GetKeys())
            {
                string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, feature.GetValue(key), key);

                if (!string.IsNullOrWhiteSpace(propertyValue)
                    && !string.Equals(propertyValue, feature.GetValue(key)))
                {
                    feature.Add(key, propertyValue);
                }
                else if (string.IsNullOrWhiteSpace(propertyValue))
                {
                    feature.Delete(key);
                }
            }
        }

        return features;
    }

    public CommonDataModel Transform(CommonDataModel feature)
    {
        if (!feature.GetElements().Any())
        {
            return feature;
        }

        foreach (string key in feature.GetKeys())
        {
            string propertyValue = CommonDataTransformer.DataProcessingInitializationCombination(_datePropertyList, _userNamePropertyList, feature.GetValue(key), key);

            if (!string.IsNullOrWhiteSpace(propertyValue)
                && !string.Equals(propertyValue, feature.GetValue(key)))
            {
                feature.Add(key, propertyValue);
            }
            else if (string.IsNullOrWhiteSpace(propertyValue))
            {
                feature.Delete(key);
            }
        }

        return feature;
    }
}
