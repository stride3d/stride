// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// An importer producing assets that keep a reference to their source file.
/// </summary>
public interface IRawAssetImporter : IAssetImporter
{
    /// <summary>
    /// Imports <paramref name="rawAssetPath"/> into <paramref name="asset"/>, created by the caller with the
    /// defaults it wants, and returns the resulting items (one per output, at or under <paramref name="location"/>).
    /// </summary>
    IEnumerable<AssetItem> Import(UFile rawAssetPath, UFile location, Asset asset);
}

public abstract class RawAssetImporterBase<TAsset> : AssetImporterBase, IRawAssetImporter
    where TAsset : Asset, IAssetWithSource, new()
{
    /// <inheritdoc />
    public sealed override IEnumerable<Type> RootAssetTypes { get { yield return typeof(TAsset); } }

    /// <inheritdoc />
    public sealed override IEnumerable<AssetItem> Import(UFile rawAssetPath, AssetImporterParameters importParameters)
    {
        ArgumentNullException.ThrowIfNull(rawAssetPath);
        return Import(rawAssetPath, new UFile(rawAssetPath.GetFileNameWithoutExtension()), new TAsset());
    }

    IEnumerable<AssetItem> IRawAssetImporter.Import(UFile rawAssetPath, UFile location, Asset asset)
    {
        return Import(rawAssetPath, location, (TAsset)asset);
    }

    /// <summary>
    /// Imports <paramref name="rawAssetPath"/> into <paramref name="asset"/>. The default sets the source and returns
    /// the asset at <paramref name="location"/>; override to read more from the file or produce several assets.
    /// </summary>
    protected virtual IEnumerable<AssetItem> Import(UFile rawAssetPath, UFile location, TAsset asset)
    {
        asset.Source = rawAssetPath;
        return new AssetItem(location, asset).Yield()!;
    }
}
