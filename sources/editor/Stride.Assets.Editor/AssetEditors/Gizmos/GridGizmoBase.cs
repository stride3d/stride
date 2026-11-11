// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Assets.Editor.EditorGame;

namespace Stride.Assets.Editor.AssetEditors.Gizmos
{
    [EffectKeys]
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

        public void Update(Color3 gridColor, float alpha, int gridAxisIndex, float sceneUnit)
        {
            if (GraphicsDevice != null)
            {
                gridColor = gridColor.ToColorSpace(GraphicsDevice.ColorSpace);
            }

            UpdateBase(gridColor, alpha, gridAxisIndex, sceneUnit);
        }

        /// <summary>
        /// Updates the grid for the camera of a view: the game draws that view next.
        /// </summary>
        /// <param name="gridColor">The color of the grid.</param>
        /// <param name="alpha">The opacity of the grid.</param>
        /// <param name="gridAxisIndex">The axes the grid is perpendicular to (X = 1, Y = 2, Z = 4).</param>
        /// <param name="sceneUnit">The unit of the scene.</param>
        /// <param name="view">The camera of the view.</param>
        public void Update(Color3 gridColor, float alpha, int gridAxisIndex, float sceneUnit, EditorGameViewCamera view)
        {
            if (GraphicsDevice != null)
            {
                gridColor = gridColor.ToColorSpace(GraphicsDevice.ColorSpace);
            }

            UpdateBase(gridColor, alpha, gridAxisIndex, sceneUnit, view);
        }

        protected override Entity Create()
        {
            // Add a plane model the grid entity
            return new Entity("Scene grid");
        }

        protected abstract void UpdateBase(Color3 gridColor, float alpha, int gridAxisIndex, float sceneUnit);

        /// <summary>
        /// Updates the grid for the camera of a view. By default, as for the editor camera.
        /// </summary>
        protected virtual void UpdateBase(Color3 gridColor, float alpha, int gridAxisIndex, float sceneUnit, EditorGameViewCamera view)
        {
            UpdateBase(gridColor, alpha, gridAxisIndex, sceneUnit);
        }
    }
}
