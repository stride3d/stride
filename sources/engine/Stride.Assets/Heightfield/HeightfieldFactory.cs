// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.Heightfield.Assets;

internal class HeightfieldAssetFactory : AssetFactory<HeightfieldAsset>
{
    public static HeightfieldAsset Create()
    {
        return new HeightfieldAsset { Layer = null! };
    }

    public override HeightfieldAsset New()
    {
        return Create();
    }
}
