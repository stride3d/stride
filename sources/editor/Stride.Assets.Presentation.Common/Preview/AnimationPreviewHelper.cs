// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// Helper methods shared by the preview and the thumbnail of an <see cref="AnimationAsset"/>.
    /// </summary>
    public static class AnimationPreviewHelper
    {
        /// <summary>
        /// Finds the model asset to animate in the preview and the thumbnail of the given animation asset.
        /// </summary>
        /// <param name="assetItem">The animation asset item.</param>
        /// <returns>The model asset item if the animation has a preview model with a skeleton; otherwise, <c>null</c>.</returns>
        public static AssetItem FindModelForPreview(AssetItem assetItem)
        {
            var animationAsset = (AnimationAsset)assetItem.Asset;
            var previewModelAsset = animationAsset?.PreviewModel != null ? assetItem.Package.FindAssetFromProxyObject(animationAsset.PreviewModel) : null;
            return (previewModelAsset?.Asset as ModelAsset)?.Skeleton != null ? previewModelAsset : null;
        }
    }
}
