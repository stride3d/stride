// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// The extension of the Assets package's SpinAsset, so the games referencing this package get typed asset URL constants
[assembly: Stride.Core.Serialization.AssetFileExtension(".sdspin", typeof(StrideAssetPlugin.SpinData))]
