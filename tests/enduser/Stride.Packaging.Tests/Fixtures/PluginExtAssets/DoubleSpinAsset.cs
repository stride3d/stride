// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using StrideAssetPlugin;
using StrideAssetPlugin.Assets;

namespace AcmeSpinPlugin.Assets;

// An asset type deriving from another plugin's (StrideAssetPlugin.Assets), compiled by a compiler deriving from that
// plugin's compiler: the asset compiler needs both Assets companions loaded, in dependency order.
[DataContract("DoubleSpinAsset")]
[AssetDescription(".sddspin")]
[AssetContentType(typeof(SpinData))]
[AssetFormatVersion("AcmeSpinPlugin", "1.0.0.0")]
[Display(110, "Double spin")]
public class DoubleSpinAsset : SpinAsset
{
}

[AssetCompiler(typeof(DoubleSpinAsset), typeof(AssetCompilationContext))]
public class DoubleSpinAssetCompiler : SpinAssetCompiler
{
}
