// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System.Threading.Tasks;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Services
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
