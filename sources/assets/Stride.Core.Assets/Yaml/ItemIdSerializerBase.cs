// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A base class to serialize <see cref="ItemId"/>.
    /// </summary>
    public abstract class ItemIdSerializerBase : AssetScalarSerializerBase
    {
        /// <summary>
        /// A key used in properties of serialization contexts to notify whether an override flag should be appened when serializing the related <see cref="ItemId"/>.
        /// </summary>
        public static PropertyKey<string> OverrideInfoKey = new PropertyKey<string>("OverrideInfo", typeof(ItemIdSerializer));

        /// <inheritdoc/>
        public override string ConvertTo(ref ObjectContext objectContext)
        {
            var result = ((ItemId)objectContext.Instance).ToString();
            string overrideInfo;
            if (objectContext.SerializerContext.Properties.TryGetValue(OverrideInfoKey, out overrideInfo))
            {
                result += overrideInfo;
                objectContext.SerializerContext.Properties.Remove(OverrideInfoKey);
            }
            return result;
        }

    }
}
