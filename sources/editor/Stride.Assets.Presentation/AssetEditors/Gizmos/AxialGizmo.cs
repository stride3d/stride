// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// Base class for all the gizmo that contain axial information.
    /// </summary>
    public abstract class AxialGizmo : GizmoBase
    {
        protected const int GizmoTessellation = 64;
        protected const float GizmoExtremitySize = 0.15f; // the size of the object placed at the extremity of the gizmo axes
        protected const float GizmoOriginScale = 1.33f; // the scale of the object placed at the origin in comparison to the extremity object
        protected const float GizmoPlaneLength = 0.25f; // the size of the gizmo small planes defining transformation along planes.
        protected const float GizmoDefaultSize = 133f; // the default size of the gizmo on the screen in pixels.

        private static readonly Color RedUniformColor = new Color(0xFC, 0x37, 0x37);
        private static readonly Color GreenUniformColor = new Color(0x32, 0xE3, 0x35);
        private static readonly Color BlueUniformColor = new Color(0x2F, 0x6A, 0xE1);
      
        /// <summary>
        /// A uniform red material
        /// </summary>
        protected Material RedUniformMaterial { get; private set; }

        /// <summary>
        /// A uniform green material
        /// </summary>
        protected Material GreenUniformMaterial { get; private set; }

        /// <summary>
        /// A uniform blue material
        /// </summary>
        protected Material BlueUniformMaterial { get; private set; }

        protected AxialGizmo()
            : base()
        {
        }

        protected override Entity Create()
        {
            RedUniformMaterial = CreateUniformColorMaterial(RedUniformColor);
            GreenUniformMaterial = CreateUniformColorMaterial(GreenUniformColor);
            BlueUniformMaterial = CreateUniformColorMaterial(BlueUniformColor);
            return null;
        }

        /// <summary>
        /// Gets the default material associated to the provided axis index.
        /// </summary>
        /// <param name="axisIndex">The index of the axis</param>
        /// <returns>The default material associated</returns>
        protected Material GetAxisDefaultMaterial(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0:
                    return RedUniformMaterial;
                case 1:
                    return GreenUniformMaterial;
                case 2:
                    return BlueUniformMaterial;
                default:
                    throw new ArgumentOutOfRangeException("axisIndex");
            }
        }

        /// <summary>
        /// Creates a material having a uniform color.
        /// </summary>
        /// <param name="color">The color of the material</param>
        /// <returns>the material</returns>
        protected Material CreateUniformColorMaterial(Color color)
        {
            return GizmoUniformColorMaterial.Create(GraphicsDevice, color, false);
        }

        protected virtual void UpdateColors()
        {
            if (IsEnabled && RedUniformMaterial != null)
            {
                GizmoUniformColorMaterial.UpdateColor(GraphicsDevice, RedUniformMaterial, RedUniformColor);
                GizmoUniformColorMaterial.UpdateColor(GraphicsDevice, GreenUniformMaterial, GreenUniformColor);
                GizmoUniformColorMaterial.UpdateColor(GraphicsDevice, BlueUniformMaterial, BlueUniformColor);
            }
        }
    }
}
