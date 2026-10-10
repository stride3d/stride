// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;

namespace StrideAssetPlugin.Assets;

// The plugin's asset type. It lives in the Assets companion package: a game never references it, the asset
// compiler and the editor load it through the runtime package's companion declaration.
[DataContract("SpinAsset")]
[AssetDescription(".sdspin")]
[AssetContentType(typeof(SpinData))]
[AssetFormatVersion("StrideAssetPlugin", "1.0.0.0")]
[Display(100, "Spin")]
public class SpinAsset : Asset
{
    public float Speed { get; set; } = 2.0f;
}
