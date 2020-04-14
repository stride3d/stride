// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Keys
{
    public static class OwnerAssetData
    {
        public const string OwnerAsset = nameof(OwnerAsset);
        public static readonly PropertyKey<AssetViewModel> Key = new PropertyKey<AssetViewModel>(OwnerAsset, typeof(OwnerAssetData));
    }
}
