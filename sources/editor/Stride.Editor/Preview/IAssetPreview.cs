// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// This interface represents an object that can manage the preview of an asset.
    /// </summary>
    public interface IAssetPreview
    {
        /// <summary>
        /// Gets the preview view model of the asset previewed. This property can be <c>null</c>.
        /// </summary>
        IAssetPreviewViewModel PreviewViewModel { get; }

        /// <summary>
        /// Gets the view model of the asset previewed.
        /// </summary>
        AssetViewModel AssetViewModel { get; }

        /// <summary>
        /// Gets the rendering mode for this preview;
        /// </summary>
        /// <value>The rendering mode.</value>
        RenderingMode RenderingMode { get; }

        /// <summary>
        /// Initializes the preview of an asset.
        /// </summary>
        /// <param name="asset">The view model of the asset to preview.</param>
        /// <param name="builder">The preview builder that is initializing this preview.</param>
        /// <returns>A task returning an object that is the view associated to the preview.</returns>
        Task<object> Initialize(AssetViewModel asset, IPreviewBuilder builder);

        /// <summary>
        /// Waits for the preview to be initialized.
        /// </summary>
        /// <returns>A task that will complete when the preview is initialized.</returns>
        Task IsInitialized();

        /// <summary>
        /// Updates the preview of an asset after a change in its property.
        /// </summary>
        /// <returns>A task that will complete when the update is done.</returns>
        Task Update();

        /// <summary>
        /// Dispose the preview of the current asset.
        /// </summary>
        /// <returns>A task that will complete when the preview is disposed.</returns>
        Task Dispose();

        /// <summary>
        /// Function called when the view corresponding to the preview has been inserted into the element hierarchy.
        /// </summary>
        void OnViewAttached();
    }
}
