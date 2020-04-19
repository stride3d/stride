// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A part asset contained by an asset that is <see cref="IAssetComposite"/>.
    /// </summary>
    [DataContract("AssetPart")]
    [Obsolete("This struct might be removed soon")]
    public struct AssetPart : IEquatable<AssetPart>
    {
        public AssetPart(Guid partId, BasePart basePart, Action<BasePart> baseUpdater)
        {
            if (baseUpdater == null) throw new ArgumentNullException(nameof(baseUpdater));
            if (partId == Guid.Empty) throw new ArgumentException(@"A part Id cannot be empty.", nameof(partId));
            PartId = partId;
            Base = basePart;
            this.baseUpdater = baseUpdater;
        }

        /// <summary>
        /// Asset identifier.
        /// </summary>
        public readonly Guid PartId;

        /// <summary>
        /// Base asset identifier.
        /// </summary>
        public readonly BasePart Base;

        private readonly Action<BasePart> baseUpdater;

        public void UpdateBase(BasePart newBase)
        {
            baseUpdater(newBase);
        }

        /// <inheritdoc/>
        public bool Equals(AssetPart other)
        {
            return PartId.Equals(other.PartId) &&
                   Equals(Base?.BasePartAsset.Id, other.Base?.BasePartAsset.Id) &&
                   Equals(Base?.BasePartId, other.Base?.BasePartId) &&
                   Equals(Base?.InstanceId, other.Base?.InstanceId);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AssetPart && Equals((AssetPart)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PartId.GetHashCode();
                if (Base != null)
                {
                    hashCode = (hashCode*397) ^ Base.GetHashCode();
                }
                return hashCode;
            }
        }

        public static bool operator ==(AssetPart left, AssetPart right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssetPart left, AssetPart right)
        {
            return !left.Equals(right);
        }
    }
}
