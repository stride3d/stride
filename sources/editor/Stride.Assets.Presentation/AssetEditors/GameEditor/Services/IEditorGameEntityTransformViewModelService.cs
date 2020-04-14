// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services
{
    public interface IEditorGameTransformViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Gets or sets the currently active transformation.
        /// </summary>
        Transformation ActiveTransformation { get; set; }
        /// <summary>
        /// Gets or sets the currently transformation space.
        /// </summary>
        TransformationSpace TransformationSpace { get; set; }

        /// <summary>
        /// Updates the snapping parameters for the given transformation.
        /// </summary>
        /// <param name="transformation">The transformation for which to update snapping parameters.</param>
        /// <param name="value">The value of the snap for the given transformation.</param>
        /// <param name="isActive">A boolean indicating whether the snap is currently active for the given transformation.</param>
        void UpdateSnap(Transformation transformation, float value, bool isActive);
    }
}
