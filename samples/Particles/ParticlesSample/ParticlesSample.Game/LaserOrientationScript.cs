// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Engine;
using Stride.Input;
using Stride.Core.Mathematics;

namespace ParticlesSample
{
    /// <summary>
    /// Script that update the position of the camera.
    /// </summary>
    public class LaserOrientationScript : AsyncScript
    {
        public TransformComponent Target;

        [Display("Do not scale")]
        public bool doNotScale = false;

        [Display("Scale Only Z")]
        public bool scaleOnlyZ = true;

        private Vector3 targetPosition = new Vector3(0, 0, 0);

        // We could expose this if we need to change it
        private Vector3 upVector = new Vector3(0, 1, 0);


        public override async Task Execute()
        {
            while (true)
            {
                targetPosition = Target != null ? Target.WorldMatrix.TranslationVector : new Vector3(0, 0, 0);
                UpdateRotation();

                // wait until next frame
                await Script.NextFrame();
            }
        }

        private void UpdateRotation()
        {
            var eyePosition = Entity.Transform.WorldMatrix.TranslationVector;

            Vector3 xaxis, yaxis, zaxis;
            Vector3.Subtract(ref eyePosition, ref targetPosition, out zaxis);
            var laserLength = zaxis.Length();
            zaxis.Normalize();
            Vector3.Cross(ref upVector, ref zaxis, out xaxis); xaxis.Normalize();
            Vector3.Cross(ref zaxis, ref xaxis, out yaxis);

            var result = Matrix.Identity;
            result.M11 = xaxis.X; result.M12 = xaxis.Y; result.M13 = xaxis.Z;
            result.M21 = yaxis.X; result.M22 = yaxis.Y; result.M23 = yaxis.Z;
            result.M31 = zaxis.X; result.M32 = zaxis.Y; result.M33 = zaxis.Z;

            var rotation = Quaternion.Identity;

            Quaternion.RotationMatrix(ref result, out rotation);

            Entity.Transform.Rotation = rotation;

            if (doNotScale)
                return;

            if (scaleOnlyZ)
            {
                Entity.Transform.Scale = new Vector3(Entity.Transform.Scale.X, Entity.Transform.Scale.Y, laserLength);
            }
            else
            {
                Entity.Transform.Scale = new Vector3(laserLength, laserLength, laserLength);
            }
        }
    }
}
