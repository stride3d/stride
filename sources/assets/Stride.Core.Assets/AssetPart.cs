// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// A part asset contained by an asset that is <see cref="IAssetComposite"/>.
/// </summary>
[DataContract("AssetPart")]
[Obsolete("This struct might be removed soon")]
public readonly struct AssetPart : IEquatable<AssetPart>
{
    public AssetPart(Guid partId, BasePart? basePart, Action<BasePart> baseUpdater)
    {
        ArgumentNullException.ThrowIfNull(baseUpdater);
        if (partId == Guid.Empty) throw new ArgumentException("A part Id cannot be empty.", nameof(partId));
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
    public readonly BasePart? Base;

    private readonly Action<BasePart> baseUpdater;

    public readonly void UpdateBase(BasePart newBase)
    {
        baseUpdater(newBase);
    }

    /// <inheritdoc/>
    public readonly bool Equals(AssetPart other)
    {
        return PartId.Equals(other.PartId) &&
               Equals(Base?.BasePartAsset.Id, other.Base?.BasePartAsset.Id) &&
               Equals(Base?.BasePartId, other.Base?.BasePartId) &&
               Equals(Base?.InstanceId, other.Base?.InstanceId);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is AssetPart assetPart && Equals(assetPart);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(PartId, Base);
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
