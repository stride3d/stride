// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A container used to serialize dictionary whose entries have identifiers.
    /// </summary>
    /// <typeparam name="TKey">The type of key contained in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of value contained in the dictionary.</typeparam>
    [DataContract]
    public class DictionaryWithItemIds<TKey, TValue> : OrderedDictionary<KeyWithId<TKey>, TValue>
    {

    }
}
