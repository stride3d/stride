// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.VirtualReality
{
    internal class OpenVRHmd : VRDevice
    {
        private RectangleF leftView = new RectangleF(0.0f, 0.0f, 0.5f, 1.0f);
        private RectangleF rightView = new RectangleF(0.5f, 0.0f, 1.0f, 1.0f);
        private Texture bothEyesMirror;
        private Texture leftEyeMirror;
        private Texture rightEyeMirror;
        private DeviceState state;
        private OpenVRTouchController leftHandController;
        private OpenVRTouchController rightHandController;
        private OpenVRTrackedDevice[] trackedDevices;
        private bool needsMirror;
        private Matrix currentHead;
        private Vector3 currentHeadPos;
        private Vector3 currentHeadLinearVelocity;
        private Vector3 currentHeadAngularVelocity;
        private Quaternion currentHeadRot;

        public override bool CanInitialize => OpenVR.InitDone || OpenVR.Init();

        public OpenVRHmd()
        {
            VRApi = VRApi.OpenVR;
            SupportsOverlays = true;
        }

        public override void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
            var width = (int)(OptimalRenderFrameSize.Width * RenderFrameScaling);
            width += width % 2;
            var height = (int)(OptimalRenderFrameSize.Height * RenderFrameScaling);
            height += height % 2;

            ActualRenderFrameSize = new Size2(width, height);

            needsMirror = requireMirror;

            if (needsMirror)
            {
                bothEyesMirror = Texture.New2D(device, width, height, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            }

            leftEyeMirror = OpenVR.GetMirrorTexture(device, 0);
            rightEyeMirror = OpenVR.GetMirrorTexture(device, 1);
            MirrorTexture = bothEyesMirror;

            leftHandController = new OpenVRTouchController(TouchControllerHand.Left);
            rightHandController = new OpenVRTouchController(TouchControllerHand.Right);

            trackedDevices = new OpenVRTrackedDevice[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
            for (int i=0; i<trackedDevices.Length; i++) 
                trackedDevices[i] = new OpenVRTrackedDevice(i);
        }

        public override VROverlay CreateOverlay(int width, int height, int mipLevels, int sampleCount)
        {
            var overlay = new OpenVROverlay();
            return overlay;
        }

        public override void Draw(GameTime gameTime)
        {
            OpenVR.UpdatePoses();
            state = OpenVR.GetHeadPose(out currentHead, out currentHeadLinearVelocity, out currentHeadAngularVelocity);
            Vector3 scale;
            currentHead.Decompose(out scale, out currentHeadRot, out currentHeadPos);
        }

        public override void Update(GameTime gameTime)
        {
            LeftHand.Update(gameTime);
            RightHand.Update(gameTime);
            foreach (var tracker in trackedDevices)
                tracker.Update(gameTime);
        }

        public override void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection)
        {
            Matrix eyeMat, rot;
            Vector3 pos, scale;

            OpenVR.GetEyeToHead(eye == Eyes.Left ? 0 : 1, out eyeMat);
            OpenVR.GetProjection(eye == Eyes.Left ? 0 : 1, near, far, out projection);

            var adjustedHeadMatrix = currentHead;
            if (ignoreHeadPosition)
            {
                adjustedHeadMatrix.TranslationVector = Vector3.Zero;
            }
            if (ignoreHeadRotation)
            {
                // keep the scale just in case
                adjustedHeadMatrix.Row1 = new Vector4(adjustedHeadMatrix.Row1.Length(), 0, 0, 0);
                adjustedHeadMatrix.Row2 = new Vector4(0, adjustedHeadMatrix.Row2.Length(), 0, 0);
                adjustedHeadMatrix.Row3 = new Vector4(0, 0, adjustedHeadMatrix.Row3.Length(), 0);
            }

            eyeMat = eyeMat * adjustedHeadMatrix * Matrix.Scaling(ViewScaling) * cameraRotation * Matrix.Translation(cameraPosition);
            eyeMat.Decompose(out scale, out rot, out pos);
            var finalUp = Vector3.TransformCoordinate(new Vector3(0, 1, 0), rot);
            var finalForward = Vector3.TransformCoordinate(new Vector3(0, 0, -1), rot);
            view = Matrix.LookAtRH(pos, pos + finalForward, finalUp);
        }

        public override void Commit(CommandList commandList, Texture renderFrame)
        {
            OpenVR.Submit(0, renderFrame, ref leftView);
            OpenVR.Submit(1, renderFrame, ref rightView);

            if (needsMirror)
            {
                var wholeRegion = new ResourceRegion(0, 0, 0, ActualRenderFrameSize.Width, ActualRenderFrameSize.Height, 1);
                commandList.CopyRegion(leftEyeMirror, 0, wholeRegion, bothEyesMirror, 0);
                commandList.CopyRegion(rightEyeMirror, 0, wholeRegion, bothEyesMirror, 0, ActualRenderFrameSize.Width / 2);
            }
        }
        public override void Recenter()
        {
            OpenVR.Recenter();
        }

        public override void SetTrackingSpace(TrackingSpace space)
        {
            OpenVR.SetTrackingSpace((Valve.VR.ETrackingUniverseOrigin)space);
        }

        public override DeviceState State => state;

        public override Vector3 HeadPosition => currentHeadPos;

        public override Quaternion HeadRotation => currentHeadRot;

        public override Vector3 HeadLinearVelocity => currentHeadLinearVelocity;

        public override Vector3 HeadAngularVelocity => currentHeadAngularVelocity;

        public override TouchController LeftHand => leftHandController;

        public override TouchController RightHand => rightHandController;

        public override TrackedItem[] TrackedItems => trackedDevices;

        public override Texture MirrorTexture { get; protected set; }

        public override float RenderFrameScaling { get; set; } = 1.4f;

        public override Size2 ActualRenderFrameSize { get; protected set; }

        public override Size2 OptimalRenderFrameSize => new Size2(2160, 1200);

        public override void Dispose()
        {
            OpenVR.Shutdown();
        }
    }
}

#endif
