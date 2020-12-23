// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;

namespace Stride.Core
{
    /// <summary>
    /// Represents the absolute identifier of an identifiable object in an asset.
    /// </summary>
    [DataContract("AbsoluteId")]
    public struct AbsoluteId : IEquatable<AbsoluteId>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AbsoluteId"/>.
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="objectId"></param>
        /// <exception cref="ArgumentException"><paramref name="assetId"/> and <paramref name="objectId"/> cannot both be empty.</exception>
        public AbsoluteId(AssetId assetId, Guid objectId)
        {
            if (assetId == AssetId.Empty && objectId == Guid.Empty)
                throw new ArgumentException($"{nameof(assetId)} and {nameof(objectId)} cannot both be empty.");

            AssetId = assetId;
            ObjectId = objectId;
        }

        /// <summary>
        /// The identifier of the containing asset.
        /// </summary>
        public AssetId AssetId { get; }

        /// <summary>
        /// The identifier of the object in the asset.
        /// </summary>
        public Guid ObjectId { get; }

        public static bool operator ==(AbsoluteId left, AbsoluteId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AbsoluteId left, AbsoluteId right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public bool Equals(AbsoluteId other)
        {
            return AssetId.Equals(other.AssetId) && ObjectId.Equals(other.ObjectId);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is AbsoluteId id && Equals(id);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (AssetId.GetHashCode() * 397) ^ ObjectId.GetHashCode();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{AssetId}/{ObjectId}";
        }
    }
}
