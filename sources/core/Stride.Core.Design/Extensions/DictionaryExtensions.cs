// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Extensions;

public static class DictionaryExtensions
{
    public static TValue? TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> thisObject, TKey key)
    {
        thisObject.TryGetValue(key, out var result);
        return result;
    }

    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> thisObject, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
    {
        foreach (var keyValuePair in keyValuePairs)
        {
            thisObject.Add(keyValuePair);
        }
    }

    public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> thisObject, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
    {
        foreach (var keyValuePair in keyValuePairs)
        {
            thisObject[keyValuePair.Key] = keyValuePair.Value;
        }
    }
}
