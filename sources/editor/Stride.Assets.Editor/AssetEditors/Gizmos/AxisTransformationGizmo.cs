// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Mathematics;
using Stride.Assets.Editor.AssetEditors.GameEditor.Game;

namespace Stride.Assets.Editor.AssetEditors.Gizmos
{
    /// <summary>
    /// A transformation gizmo based on axis.
    /// </summary>
    public abstract class AxisTransformationGizmo : TransformationGizmo
    {
        protected Vector3 DragTranslationWorld;

        /// <summary>
        /// Gets the axes of a gizmo whose positive side points away from the camera of a view: the gizmo shows the opposite
        /// side of these axes, and turns the planes next to them, so that its handles face that camera.
        /// </summary>
        /// <param name="gizmoWorldMatrix">The world matrix of the gizmo.</param>
        /// <param name="viewMatrix">The view matrix of the camera.</param>
        internal static GizmoTransformationAxes GetBackfacingAxes(Matrix gizmoWorldMatrix, Matrix viewMatrix)
        {
            var cameraInGizmoSpace = (Matrix.Invert(viewMatrix) * Matrix.Invert(gizmoWorldMatrix)).TranslationVector;
            var axes = GizmoTransformationAxes.None;
            for (var axis = 0; axis < 3; axis++)
            {
                if (cameraInGizmoSpace[axis] < 0)
                    axes |= (GizmoTransformationAxes)(1 << axis);
            }
            return axes;
        }

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
