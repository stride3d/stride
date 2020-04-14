// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// Changes rules on what types can be naturally contained inside a given member. All other types will be serialized as references.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AssetPartContainedAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPartContainedAttribute"/>.
        /// </summary>
        /// <param name="containedTypes">The collection of asset part types that are naturally contained in the member having this attribute.</param>
        public AssetPartContainedAttribute(params Type[] containedTypes)
        {
            ContainedTypes = containedTypes ?? new Type[0];
        }

        /// <summary>
        /// Gets the types of asset part that will still be fully serialized if contained in a part of the member having this attribute.
        /// </summary>
        public Type[] ContainedTypes { get; }
    }
}
