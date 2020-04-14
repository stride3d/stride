// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11

using System;
using Xenko.Core.Mathematics;
using Xenko.Games;

namespace Xenko.VirtualReality
{
    internal class OpenVRTrackedDevice : TrackedItem
    {
        private readonly int id;
        private OpenVR.TrackedDevice trackedDevice;
        private DeviceState internalState;
        private Vector3 currentPos;
        private Vector3 currentLinearVelocity;
        private Vector3 currentAngularVelocity;
        private Quaternion currentRot;

        internal OpenVRTrackedDevice(int index)
        {
            trackedDevice = new OpenVR.TrackedDevice(index);
        }

        public override void Update(GameTime gameTime)
        {
            if (trackedDevice != null)
            {
                Matrix mat;
                Vector3 vel, angVel;
                internalState = OpenVR.GetTrackerPose(trackedDevice.TrackerIndex, out mat, out vel, out angVel);
                if (internalState != DeviceState.Invalid)
                {
                    Vector3 scale;
                    mat.Decompose(out scale, out currentRot, out currentPos);
                    currentLinearVelocity = vel;
                    currentAngularVelocity = new Vector3(MathUtil.DegreesToRadians(angVel.X), MathUtil.DegreesToRadians(angVel.Y), MathUtil.DegreesToRadians(angVel.Z));
                }
            }

            base.Update(gameTime);
        }

        public override Vector3 Position => currentPos;

        public override Quaternion Rotation => currentRot;

        public override Vector3 LinearVelocity => currentLinearVelocity;

        public override Vector3 AngularVelocity => currentAngularVelocity;

        public override DeviceState State => internalState;

        public override DeviceClass Class => (DeviceClass)trackedDevice.DeviceClass;

        public override string SerialNumber => trackedDevice.SerialNumber;

        public override float BatteryPercentage => trackedDevice.BatteryPercentage;
    }
}

#endif
