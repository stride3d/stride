// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11

using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.VirtualReality
{
    internal class OpenVROverlay : VROverlay
    {
        private ulong overlayId;

        public OpenVROverlay()
        {
            overlayId = OpenVR.CreateOverlay();
            if (overlayId == 0)
            {
                throw new System.Exception("Failed to create OpenVR overlay.");
            }

            OpenVR.InitOverlay(overlayId);
            OpenVR.SetOverlayEnabled(overlayId, true);
        }

        public override void Dispose()
        {           
        }

        private bool enabled = true;

        public override bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                OpenVR.SetOverlayEnabled(overlayId, enabled);
            }
        }

        public override void UpdateSurface(CommandList commandList, Texture texture)
        {
            var pose = Matrix.Translation(Position) * Matrix.RotationQuaternion(Rotation);
            OpenVR.SetOverlayParams(overlayId, pose, FollowHeadRotation, SurfaceSize);
            OpenVR.SubmitOverlay(overlayId, texture);
        }
    }
}

#endif
