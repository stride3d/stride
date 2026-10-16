// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.BepuPhysics.Assets;

internal class HullAssetFactory : AssetFactory<HullAsset>
{
    public static HullAsset Create()
    {
        return new HullAsset();
    }

    public override HullAsset New()
    {
        return Create();
    }
}
