// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Serialization;

/// <summary>
/// Base class for <see cref="IUrlReference" /> implementations
/// </summary>
[DataContract("urlref", Inherited = true)]
[DataStyle(DataStyle.Compact)]
public abstract class UrlReferenceBase : IReference, IUrlReference
{
    /// <summary>
    /// Create a new <see cref="UrlReferenceBase"/> instance.
    /// </summary>
    protected UrlReferenceBase()
    {
    }

    /// <summary>
    /// Create a new <see cref="UrlReferenceBase"/> instance.
    /// </summary>
    /// <param name="url"></param>
    /// <exception cref="ArgumentNullException">If <paramref name="url"/> is <c>null</c> or empty.</exception>
    protected UrlReferenceBase(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentNullException(nameof(url), $"{nameof(url)} cannot be null or empty.");
        }

        this.Url = url;
    }

    /// <summary>
    /// Create a new <see cref="UrlReferenceBase"/> instance.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="url"/> is <c>null</c> or empty.</exception>
    protected UrlReferenceBase(AssetId id, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentNullException(nameof(url), $"{nameof(url)} cannot be null or empty.");
        }

        this.Url = url;
        this.Id = id;
    }

    /// <summary>
    /// Gets the Url of the referenced asset.
    /// </summary>
    [DataMember(10)]
    public string Url { get; init; }

    /// <summary>
    /// Gets the Id of the referenced asset.
    /// </summary>
    [DataMember(20)]
    public AssetId Id { get; init; }

    /// <summary>
    /// Gets whether the url is <c>null</c> or empty.
    /// </summary>
    [DataMemberIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(Url);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Url}";
    }

    string IReference.Location => Url;

    public static UrlReferenceBase New(Type urlReferenceType, AssetId id, string url)
    {
        return (UrlReferenceBase)Activator.CreateInstance(urlReferenceType, id, url)!;
    }

    public static bool IsUrlReferenceType(Type? type)
    {
        return typeof(UrlReferenceBase).IsAssignableFrom(type);
    }

    public static bool TryGetAssetType(Type type, [MaybeNullWhen(false)] out Type assetType)
    {
        if (type.IsAssignableTo(typeof(UrlReferenceBase)) && type.IsGenericType)
        {
            assetType = type.GetGenericArguments()[0];
            return true;
        }

        assetType = null;
        return false;
    }
}
