// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

public static class OwnerAssetData
{
    public const string OwnerAsset = nameof(OwnerAsset);
    public static readonly PropertyKey<AssetViewModel> Key = new(OwnerAsset, typeof(OwnerAssetData));
}
