// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A container used to serialize collection whose items have identifiers.
    /// </summary>
    /// <typeparam name="TItem">The type of item contained in the collection.</typeparam>
    [DataContract]
    public class CollectionWithItemIds<TItem> : OrderedDictionary<ItemId, TItem>
    {
    }
}
