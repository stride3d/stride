// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A transformation gizmo based on axis.
    /// </summary>
    public abstract class AxisTransformationGizmo : TransformationGizmo
    {
        protected Vector3 DragTranslationWorld;

        /// <summary>
        /// Calculate the translation of the mouse drag in the world space depending on the current axis transformation
        /// </summary>
        protected override InitialTransformation CalculateTransformation()
        {
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();
            // determine the position of the start/end points of the drag in the gizmo space
            var gizmoWorldInverse = Matrix.Invert(StartWorldMatrix);
            var gizmoViewInverse = Matrix.Invert(cameraService.ViewMatrix) * gizmoWorldInverse;
            var ray = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, Input.MousePosition, gizmoViewInverse);
            var endPointGizmo = EditorGameHelper.ProjectOnPlaneWithLimitAngle(ray, ProjectionPlane, MinimumRayAngle);
            DragTranslationWorld = endPointGizmo - StartClickPoint;

            // clamp the translation in the gizmo space
            for (int i = 0; i < 3; i++)
            {
                if (((int)TransformationAxes & (1 << i)) == 0)
                    DragTranslationWorld[i] = 0;
            }

            return new InitialTransformation { Rotation = Quaternion.Identity, Scale = Vector3.One };
        }
    }
}
