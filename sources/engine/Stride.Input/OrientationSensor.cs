// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using System.Numerics;

namespace Stride.Input
{
    internal class OrientationSensor : Sensor, IOrientationSensor
    {
        private float yaw;
        private float pitch;
        private float roll;
        private Quaternion quaternion;
        private Matrix4x4 rotationMatrix;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrientationSensor"/> class.
        /// </summary>
        public OrientationSensor(IInputSource source, string systemName) : base(source, systemName, "Orientation")
        {
            Reset();
        }

        public float Yaw => yaw;

        public float Pitch => pitch;

        public float Roll => roll;

        public Quaternion Quaternion => quaternion;

        public Matrix4x4 RotationMatrix => rotationMatrix;

        public void FromQuaternion(Quaternion q)
        {
            quaternion = q;
            rotationMatrix = Matrix4x4.CreateFromQuaternion(quaternion);
            (yaw, pitch, roll) = quaternion.GetYawPitchRoll();
        }

        public void Reset()
        {
            yaw = 0;
            pitch = 0;
            roll = 0;
            quaternion = Quaternion.Identity;
            rotationMatrix = Matrix4x4.Identity;
        }
    }
}
