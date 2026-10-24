// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Editor.Preview.View
{
    /// <summary>
    /// An interface that represents the view of a preview.
    /// </summary>
    [Stride.Core.Reflection.AssemblyScan]
    public interface IPreviewView
    {
        /// <summary>
        /// Initializes the view with the given parameters.
        /// </summary>
        /// <param name="previewBuilder">The preview builder used to build the preview.</param>
        /// <param name="assetPreview">The asset preview to display in the view.</param>
        void InitializeView(IPreviewBuilder previewBuilder, IAssetPreview assetPreview);

        /// <summary>
        /// Updates the view, usually after regenerating a new <see cref="IAssetPreview"/> instance.
        /// </summary>
        /// <param name="assetPreview">The asset preview to display in the view.</param>
        void UpdateView(IAssetPreview assetPreview);

        /// <summary>
        /// Gets the size, in pixels, of the area the preview game renders in, once the view is laid out.
        /// </summary>
        /// <returns>The size in pixels, or <c>null</c> when the view is not laid out yet.</returns>
        Vector2? GetLaidOutViewSize();
    }
}
