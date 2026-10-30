// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.Heightfield.Assets;

internal class NormalFromHeightAssetFactory : AssetFactory<NormalFromHeightAsset>
{
    public static NormalFromHeightAsset Create()
    {
        return new NormalFromHeightAsset { HeightInput = null };
    }

    public override NormalFromHeightAsset New()
    {
        return Create();
    }
}
