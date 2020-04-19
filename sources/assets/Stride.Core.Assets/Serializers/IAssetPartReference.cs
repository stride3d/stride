// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// An interface representing a reference to an asset part that is used for serialization.
    /// </summary>
    public interface IAssetPartReference
    {
        /// <summary>
        /// Gets or sets the actual type of object that is being deserialized.
        /// </summary>
        /// <remarks>
        /// This property is transient and used only during serialization. Therefore, implementations should have the <see cref="Core.DataMemberIgnoreAttribute"/> set on this property.
        /// </remarks>
        Type InstanceType { get; set; }

        /// <summary>
        /// Fills properties of this object from the actual asset part being referenced.
        /// </summary>
        /// <param name="assetPart">The actual asset part being referenced.</param>
        void FillFromPart(object assetPart);

        /// <summary>
        /// Generates a proxy asset part from the information contained in this instance.
        /// </summary>
        /// <param name="partType">The type of asset part to generate.</param>
        /// <returns>A proxy asset part built from this instance.</returns>
        object GenerateProxyPart(Type partType);
    }
}
