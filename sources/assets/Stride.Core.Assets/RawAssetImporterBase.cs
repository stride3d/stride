// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.IO;

namespace Stride.Core.Assets;

public abstract class RawAssetImporterBase<TAsset> : AssetImporterBase
    where TAsset : Asset, IAssetWithSource, new()
{
    /// <inheritdoc />
    public sealed override IEnumerable<Type> RootAssetTypes { get { yield return typeof(TAsset); } }

    /// <inheritdoc />
    public sealed override IEnumerable<AssetItem> Import(UFile rawAssetPath, AssetImporterParameters importParameters)
    {
        ArgumentNullException.ThrowIfNull(rawAssetPath);

        var asset = new TAsset { Source = rawAssetPath };
        // Creates the url to the raw asset
        var rawAssetUrl = new UFile(rawAssetPath.GetFileNameWithoutExtension());
        return new AssetItem(rawAssetUrl, asset).Yield()!;
    }
}
