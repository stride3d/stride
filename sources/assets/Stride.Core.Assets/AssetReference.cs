// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets;

/// <summary>
/// An asset reference.
/// </summary>
[DataContract("aref")]
[DataStyle(DataStyle.Compact)]
[DataSerializer(typeof(AssetReferenceDataSerializer))]
public sealed class AssetReference : IReference, IEquatable<AssetReference>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetReference"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the asset.</param>
    /// <param name="location">The location.</param>
    public AssetReference(AssetId id, UFile location)
    {
        Location = location;
        Id = id;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the reference asset.
    /// </summary>
    /// <value>The unique identifier of the reference asset..</value>
    [DataMember(10)]
    public AssetId Id { get; init; }

    /// <summary>
    /// Gets or sets the location of the asset.
    /// </summary>
    /// <value>The location.</value>
    [DataMember(20)]
    public string Location { get; init; }

    public bool Equals(AssetReference? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Location, other.Location) && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return Equals(obj as AssetReference);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Location, Id);
    }

    /// <summary>
    /// Implements the ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(AssetReference? left, AssetReference? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Implements the !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(AssetReference? left, AssetReference? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        // WARNING: This should not be modified as it is used for serializing
        return $"{Id}:{Location}";
    }

    /// <summary>
    /// Tries to parse an asset reference in the format "GUID:Location".
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="location">The location.</param>
    /// <returns><c>true</c> if parsing was successful, <c>false</c> otherwise.</returns>
    public static AssetReference New(AssetId id, UFile location)
    {
        return new AssetReference(id, location);
    }

    /// <summary>
    /// Tries to parse an asset reference in the format "[GUID/]GUID:Location". The first GUID is optional and is used to store the ID of the reference.
    /// </summary>
    /// <param name="assetReferenceText">The asset reference.</param>
    /// <param name="id">The unique identifier of asset pointed by this reference.</param>
    /// <param name="location">The location.</param>
    /// <returns><c>true</c> if parsing was successful, <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentNullException">assetReferenceText</exception>
    public static bool TryParse(string assetReferenceText, out AssetId id, [MaybeNullWhen(false)] out UFile location)
    {
        ArgumentNullException.ThrowIfNull(assetReferenceText);

        id = AssetId.Empty;
        location = null;
        var indexFirstSlash = assetReferenceText.IndexOf('/');
        var indexBeforelocation = assetReferenceText.IndexOf(':');
        if (indexBeforelocation < 0)
        {
            return false;
        }
        var startNextGuid = 0;
        if (indexFirstSlash > 0 && indexFirstSlash < indexBeforelocation)
        {
            startNextGuid = indexFirstSlash + 1;
        }

        if (!AssetId.TryParse(assetReferenceText[startNextGuid..indexBeforelocation], out id))
        {
            return false;
        }

        location = new UFile(assetReferenceText[(indexBeforelocation + 1)..]);

        return true;
    }

    /// <summary>
    /// Tries to parse an asset reference in the format "GUID:Location".
    /// </summary>
    /// <param name="assetReferenceText">The asset reference.</param>
    /// <param name="assetReference">The reference.</param>
    /// <returns><c>true</c> if parsing was successful, <c>false</c> otherwise.</returns>
    public static bool TryParse(string assetReferenceText, [MaybeNullWhen(false)] out AssetReference assetReference)
    {
        ArgumentNullException.ThrowIfNull(assetReferenceText);

        if (!TryParse(assetReferenceText, out var assetId, out var location))
        {
            assetReference = null;
            return false;
        }
        assetReference = New(assetId, location);
        return true;
    }
}

/// <summary>
/// Extension methods for <see cref="AssetReference"/>
/// </summary>
public static class AssetReferenceExtensions
{
    /// <summary>
    /// Determines whether the specified asset reference has location. If the reference is null, return <c>false</c>.
    /// </summary>
    /// <param name="assetReference">The asset reference.</param>
    /// <returns><c>true</c> if the specified asset reference has location; otherwise, <c>false</c>.</returns>
    public static bool HasLocation(this AssetReference assetReference)
    {
        return assetReference?.Location != null;
    }
}
