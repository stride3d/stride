// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;

namespace Stride.Core.Assets;

/// <summary>
/// A class containing the information of a hierarchy of asset parts contained in an <see cref="AssetCompositeHierarchy{TAssetPartDesign, TAssetPart}"/>.
/// </summary>
/// <typeparam name="TAssetPartDesign">The type used for the design information of a part.</typeparam>
/// <typeparam name="TAssetPart">The type used for the actual parts,</typeparam>
[DataContract("AssetCompositeHierarchyData")]
public class AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
{
    /// <summary>
    /// Gets a collection if identifier of all the parts that are root of this hierarchy.
    /// </summary>
    [DataMember(10)]
    [NonIdentifiableCollectionItems]
    public List<TAssetPart> RootParts { get; } = [];

    /// <summary>
    /// Gets a collection of all the parts, root or not, contained in this hierarchy.
    /// </summary>
    [DataMember(20)]
    [NonIdentifiableCollectionItems]
    public AssetPartCollection<TAssetPartDesign, TAssetPart> Parts { get; } = [];
}
