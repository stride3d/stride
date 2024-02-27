// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;

namespace Stride.Editor.Preview;

public delegate IAssetPreview AssetPreviewFactory(IPreviewBuilder builder, PreviewGame game, AssetItem asset);
