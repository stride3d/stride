// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Analysis;

/// <summary>
/// Represent a link between Assets.
/// </summary>
public struct AssetLink : IContentLink
{
    /// <summary>
    /// The asset item pointed by the dependency.
    /// </summary>
    public readonly AssetItem Item;

    private ContentLinkType type;

    private readonly IReference reference;

    /// <summary>
    /// Create an asset dependency of type <paramref name="type"/> and pointing to <paramref name="item"/>
    /// </summary>
    /// <param name="item">The item the dependency is pointing to</param>
    /// <param name="type">The type of the dependency between the items</param>
    public AssetLink(AssetItem item, ContentLinkType type)
    {
        ArgumentNullException.ThrowIfNull(item);

        Item = item;
        this.type = type;
        reference = item.ToReference();
    }

    // This constructor exists for better factorization of code in AssetDependencies. 
    // It should not be turned into public as AssetItem is not valid.
    internal AssetLink(IReference reference, ContentLinkType type)
    {
        ArgumentNullException.ThrowIfNull(reference);

        Item = null!;
        this.type = type;
        this.reference = reference;
    }

    public ContentLinkType Type
    {
        readonly get { return type; }
        set { type = value; }
    }

    public readonly IReference Element { get { return reference; } }

    /// <summary>
    /// Gets a clone copy of the asset dependency.
    /// </summary>
    /// <returns>the clone instance</returns>
    public readonly AssetLink Clone()
    {
        return new AssetLink(Item.Clone(true), Type);
    }
}
