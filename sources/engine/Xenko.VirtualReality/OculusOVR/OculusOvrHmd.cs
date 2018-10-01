// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D11;
using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Graphics;
using CommandList = Xenko.Graphics.CommandList;

namespace Xenko.VirtualReality
{
    internal class OculusOvrHmd : VRDevice
    {
        private static bool initDone;

        //private static readonly Guid dx12ResourceGuid = new Guid("696442be-a72e-4059-bc79-5b5c98040fad");
        internal static readonly Guid Dx11Texture2DGuid = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        private IntPtr ovrSession;
        private Texture[] textures;

        private OculusTouchController leftHandController;
        private OculusTouchController rightHandController;
        private readonly List<OculusOverlay> overlays = new List<OculusOverlay>();
        private IntPtr[] overlayPtrs = new IntPtr[0];

        internal OculusOvrHmd()
        {
            SupportsOverlays = true;
            VRApi = VRApi.Oculus;
        }

        public override void Dispose()
        {
            foreach (var oculusOverlay in overlays)
            {
                oculusOverlay.Dispose();
            }

            if (ovrSession != IntPtr.Zero)
            {
                OculusOvr.DestroySession(ovrSession);
                ovrSession = IntPtr.Zero;
            }
        }

        public override void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
            graphicsDevice = device;
            long adapterId;
            ovrSession = OculusOvr.CreateSessionDx(out adapterId);
            //Game.GraphicsDeviceManager.RequiredAdapterUid = adapterId.ToString(); //should not be needed

            int texturesCount;
            if (!OculusOvr.CreateTexturesDx(ovrSession, device.NativeDevice.NativePointer, out texturesCount, RenderFrameScaling, requireMirror ? mirrorWidth : 0, requireMirror ? mirrorHeight : 0))
            {
                throw new Exception(OculusOvr.GetError());
            }

            if (requireMirror)
            {
                var mirrorTex = OculusOvr.GetMirrorTexture(ovrSession, Dx11Texture2DGuid);
                MirrorTexture = new Texture(device);
                MirrorTexture.InitializeFromImpl(new Texture2D(mirrorTex), false);
            }

            textures = new Texture[texturesCount];
            for (var i = 0; i < texturesCount; i++)
            {
                var ptr = OculusOvr.GetTextureDx(ovrSession, Dx11Texture2DGuid, i);
                if (ptr == IntPtr.Zero)
                {
                    throw new Exception(OculusOvr.GetError());
                }

                textures[i] = new Texture(device);
                textures[i].InitializeFromImpl(new Texture2D(ptr), false);
            }

            ActualRenderFrameSize = new Size2(textures[0].Width, textures[0].Height);

            leftHandController = new OculusTouchController(TouchControllerHand.Left);
            rightHandController = new OculusTouchController(TouchControllerHand.Right);
        }

        private OculusOvr.PosesProperties currentPoses;
        private GraphicsDevice graphicsDevice;

        public override void Update(GameTime gameTime)
        {
            OculusOvr.InputProperties properties;
            OculusOvr.GetInputProperties(ovrSession, out properties);
            leftHandController.UpdateInputs(ref properties);
            rightHandController.UpdateInputs(ref properties);
        }

        public override void Draw(GameTime gameTime)
        {
            OculusOvr.Update(ovrSession);
            OculusOvr.GetPosesProperties(ovrSession, out currentPoses);
            leftHandController.UpdatePoses(ref currentPoses);
            rightHandController.UpdatePoses(ref currentPoses);
        }

        public override Size2 OptimalRenderFrameSize => new Size2(2160, 1200);

        public override Size2 ActualRenderFrameSize { get; protected set; }

        public override Texture MirrorTexture { get; protected set; }

        public override float RenderFrameScaling { get; set; } = 1.2f;

        public override DeviceState State
        {
            get
            {
                var deviceStatus = OculusOvr.GetStatus(ovrSession);
                if (deviceStatus.DisplayLost || !deviceStatus.HmdPresent) return DeviceState.Invalid;
                if (deviceStatus.HmdMounted && deviceStatus.IsVisible) return DeviceState.Valid;
                return DeviceState.OutOfRange;
            }
        }

        public override Vector3 HeadPosition => currentPoses.PosHead;

        public override Quaternion HeadRotation => currentPoses.RotHead;

        public override Vector3 HeadLinearVelocity => currentPoses.LinearVelocityHead;

        public override Vector3 HeadAngularVelocity => currentPoses.AngularVelocityHead;

        public override TouchController LeftHand => leftHandController;

        public override TouchController RightHand => rightHandController;

        public override TrackedItem[] TrackedItems => new TrackedItem[0];

        public override bool CanInitialize
        {
            get
            {
                if (initDone) return true;
                initDone = OculusOvr.Startup();
                if (initDone)
                {
                    long deviceId;
                    var tempSession = OculusOvr.CreateSessionDx(out deviceId);
                    if (tempSession != IntPtr.Zero)
                    {
                        OculusOvr.DestroySession(tempSession);
                        initDone = true;
                    }
                    else
                    {
                        initDone = false;
                    }
                }
                return initDone;
            }
        }
        
        public override void Recenter()
        {
            OculusOvr.Recenter(ovrSession);
        }

        public override void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection)
        {
            var frameProperties = new OculusOvr.FrameProperties
            {
                Near = near,
                Far = far,
            };
            OculusOvr.GetFrameProperties(ovrSession, ref frameProperties);

            var isLeftEye = eye == Eyes.Left;
            var camRot = Quaternion.RotationMatrix(cameraRotation);
            projection = isLeftEye ? frameProperties.ProjLeft : frameProperties.ProjRight;

            var eyePosition = isLeftEye ? frameProperties.PosLeft : frameProperties.PosRight;
            if (ignoreHeadPosition)
                eyePosition -= (frameProperties.PosLeft + frameProperties.PosRight) / 2f;

            var eyeRotation = isLeftEye ? frameProperties.RotLeft : frameProperties.RotRight;
            if (ignoreHeadRotation)
                eyeRotation = Quaternion.Identity;

            var position = cameraPosition + Vector3.Transform(eyePosition * ViewScaling, camRot);
            var rotation = Matrix.RotationQuaternion(eyeRotation) * Matrix.Scaling(ViewScaling) * Matrix.RotationQuaternion(camRot);
            var finalUp = Vector3.TransformCoordinate(new Vector3(0, 1, 0), rotation);
            var finalForward = Vector3.TransformCoordinate(new Vector3(0, 0, -1), rotation);
            view = Matrix.LookAtRH(position, position + finalForward, finalUp);
        }

        public override void Commit(CommandList commandList, Texture renderFrame)
        {
            var index = OculusOvr.GetCurrentTargetIndex(ovrSession);
            commandList.Copy(renderFrame, textures[index]);
            overlayPtrs = overlays.Where(x => x.Enabled).Select(x => x.OverlayPtr).ToArray();
            OculusOvr.CommitFrame(ovrSession, overlayPtrs.Length, overlayPtrs);
        }

        public override VROverlay CreateOverlay(int width, int height, int mipLevels, int sampleCount)
        {
            var overlay = new OculusOverlay(ovrSession, graphicsDevice, width, height, mipLevels, sampleCount);
            overlays.Add(overlay);
            return overlay;
        }

        public override void ReleaseOverlay(VROverlay overlay)
        {
            var oculusOverlay = (OculusOverlay)overlay;
            oculusOverlay.Dispose();
            overlays.Remove(oculusOverlay);
        }
    }
}

#endif
