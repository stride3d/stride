// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// The controller of an entity hierarchy editor (scene or prefab), as the editor game services of the plugins see it:
    /// a service declares it with <c>[EditorGameService(typeof(IEntityHierarchyEditorController))]</c> and takes it in its constructor.
    /// </summary>
    public interface IEntityHierarchyEditorController : IEditorGameController
    {
        /// <summary>
        /// Gets the identifier of an entity of the game-side scene.
        /// </summary>
        /// <param name="entity">The game-side entity.</param>
        /// <returns>The identifier of the entity in its asset.</returns>
        AbsoluteId GetAbsoluteId([NotNull] Entity entity);
    }
}
