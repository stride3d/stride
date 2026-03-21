// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// Viewport screenshot capture service.
    /// </summary>
    public interface IEditorGameScreenshotService : IEditorGameViewModelService
    {
        /// <summary>
        /// Captures the current viewport as a PNG image.
        /// </summary>
        /// <returns>The PNG image data as a byte array.</returns>
        Task<byte[]> CaptureViewportAsync();
    }
}
