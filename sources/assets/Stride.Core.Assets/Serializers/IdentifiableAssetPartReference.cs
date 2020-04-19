// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// An implementation of <see cref="IAssetPartReference"/> that represents an asset part implementing <see cref="IIdentifiable"/>.
    /// </summary>
    /// <remarks>
    /// This type is the default type used when <see cref="AssetPartReferenceAttribute.ReferenceType"/> is undefined.
    /// </remarks>
    [DataContract]
    [DataStyle(DataStyle.Compact)]
    public class IdentifiableAssetPartReference : IAssetPartReference
    {
        /// <summary>
        /// Gets or sets the identifier of the asset part represented by this reference.
        /// </summary>
        public Guid Id { get; set; }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public Type InstanceType { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{AssetPartReference: {Id}}}";
        }

        /// <inheritdoc/>
        public void FillFromPart(object assetPart)
        {
            if (assetPart != null && !(assetPart is IIdentifiable))
                throw new InvalidOperationException($"Cannot serialize an object of type {assetPart.GetType().Name} as an asset part reference: the type does not implement {typeof(IIdentifiable).Name}");

            var identifiable = (IIdentifiable)assetPart;
            Id = identifiable?.Id ?? Guid.Empty;
        }

        /// <inheritdoc/>
        public object GenerateProxyPart(Type partType)
        {
            if (!typeof(IIdentifiable).IsAssignableFrom(partType))
                throw new InvalidOperationException($"Cannot serialize an object of type {partType.Name} as an asset part reference: the type does not implement {typeof(IIdentifiable).Name}");

            if (Id == Guid.Empty)
                return null;

            var assetPart = (IIdentifiable)Activator.CreateInstance(partType);
            assetPart.Id = Id;
            return assetPart;
        }
    }
}
