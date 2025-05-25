// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.
using Stride.Core.Mathematics;
using Stride.Engine;

namespace MySurvivalGame.Game.Core // MODIFIED: Namespace updated
{
    public static class Utils
    {
        public static Vector3 LogicDirectionToWorldDirection(Vector2 logicDirection, CameraComponent camera, Vector3 upVector)
        {
            camera.Update(); // Ensure camera matrices are up-to-date
            var inverseView = Matrix.Invert(camera.ViewMatrix);

            var forward = Vector3.Cross(upVector, inverseView.Right);
            forward.Normalize();

            var right = Vector3.Cross(forward, upVector); // This should be inverseView.Right, normalized
            // Or, more robustly:
            // var right = inverseView.Right;
            // right.Normalize();
            // To ensure it's orthogonal to the calculated forward and upVector:
            right = Vector3.Cross(upVector, forward); // Re-calculate right based on new forward and world up
            right.Normalize();


            var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
            // Normalize only if logicDirection is not zero, to avoid issues with zero vectors
            if (worldDirection.LengthSquared() > float.Epsilon)
            {
                worldDirection.Normalize();
            }
            return worldDirection;
        }
    }
}
