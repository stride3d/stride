// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Graphics;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// Cubemap capture service.
    /// </summary>
    public interface IEditorGameCubemapService : IEditorGameViewModelService
    {
        /// <summary>
        /// Captures a cubemap from the current camera position, using <see cref="GraphicsCompositor.SingleView"/>.
        /// </summary>
        /// <remarks>
        /// This cubemap can then be prefiltered to be used for diffuse or specular lighting.
        /// </remarks>
        /// <returns>The captured cubemap.</returns>
        Task<Image> CaptureCubemap();
    }
}
