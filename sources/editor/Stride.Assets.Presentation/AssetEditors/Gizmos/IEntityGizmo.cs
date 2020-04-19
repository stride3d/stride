// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The interface for gizmos representing entity components
    /// </summary>
    public interface IEntityGizmo : IGizmo
    {
        /// <summary>
        /// Gets or sets the selected state of the gizmo.
        /// </summary>
        bool IsSelected { get; set; }

        /// <summary>
        /// Gets the entity of the scene associated to the gizmo.
        /// </summary>
        Entity ContentEntity { get; }

        /// <summary>
        /// Initializes the <see cref="ContentEntity"/> property.
        /// </summary>
        /// <param name="contentEntity">The content entity.</param>
        /// <remarks>This method must be invoked before <see cref="IGizmo.Initialize"/>.</remarks>
        void InitializeContentEntity(Entity contentEntity);

        /// <summary>
        /// Updates the gizmo state.
        /// </summary>
        void Update();
    }
}
