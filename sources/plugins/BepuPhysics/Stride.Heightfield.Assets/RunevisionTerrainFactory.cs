// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.Heightfield.Assets;

internal class RunevisionTerrainAssetFactory : AssetFactory<RunevisionTerrainAsset>
{
    public static RunevisionTerrainAsset Create()
    {
        return new RunevisionTerrainAsset { HeightInput = new RunevisionTerrainAsset.FractalNoise() };
    }

    public override RunevisionTerrainAsset New()
    {
        return Create();
    }
}
