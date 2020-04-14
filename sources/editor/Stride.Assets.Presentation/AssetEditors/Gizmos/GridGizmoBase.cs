// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    public abstract class GridGizmoBase : GizmoBase
    {
        public const RenderGroup GridGroup = RenderGroup.Group2;
        public const RenderGroupMask GridGroupMask = RenderGroupMask.Group2;

        protected static readonly ValueParameterKey<Color4> GridColorKey = ParameterKeys.NewValue<Color4>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GridGizmoBase" /> class.
        /// </summary>
        protected GridGizmoBase()
        {
            RenderGroup = GridGroup;
        }

        /// <summary>
        /// The sub-division base of the grid.
        /// </summary>
        protected virtual int GridBase { get; } = 10;

        /// <summary>
        /// The size of the grid for a grid unit of 1.
        /// </summary>
        protected virtual int GridSize { get; } = 10;

        public void Update(Color3 gridColor, float sceneUnit)
        {
            if (GraphicsDevice != null)
            {
                gridColor = gridColor.ToColorSpace(GraphicsDevice.ColorSpace);
            }

            UpdateBase(gridColor, sceneUnit);
        }

        protected override Entity Create()
        {
            // Add a plane model the grid entity
            return new Entity("Scene grid");
        }

        protected abstract void UpdateBase(Color3 gridColor, float sceneUnit);
    }
}
