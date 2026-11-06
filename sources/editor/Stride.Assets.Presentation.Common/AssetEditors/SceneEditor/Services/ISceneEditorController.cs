// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.SceneEditor.Services
{
    /// <summary>
    /// The controller of the scene editor, as the editor game services of the plugins see it.
    /// </summary>
    public interface ISceneEditorController : IEntityHierarchyEditorController
    {
        /// <summary>
        /// Gets the identifier of the scene asset a game-side scene was loaded from.
        /// </summary>
        /// <param name="scene">The game-side scene.</param>
        /// <returns>The identifier of its asset.</returns>
        AssetId GetSceneAssetId([NotNull] Scene scene);
    }
}
