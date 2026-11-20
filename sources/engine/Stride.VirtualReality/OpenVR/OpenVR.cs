// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Silk.NET.Direct3D11;
using Valve.VR;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

using System.Numerics;

namespace Stride.VirtualReality
{
    internal static unsafe class OpenVR
    {
        public class Controller
        {
            // This helper can be used in a variety of ways.  Beware that indices may change
            // as new devices are dynamically added or removed, controllers are physically
            // swapped between hands, arms crossed, etc.
            public enum Hand
            {
                Left,
                Right
            }

            public static int GetDeviceIndex(Hand hand)
            {
                var currentIndex = 0;
                for (uint index = 0; index < DevicePoses.Length; index++)
                {
                    if (Valve.VR.OpenVR.System.GetTrackedDeviceClass(index) == ETrackedDeviceClass.Controller)
                    {
                        if (hand == Hand.Left && Valve.VR.OpenVR.System.GetControllerRoleForTrackedDeviceIndex(index) == ETrackedControllerRole.LeftHand)
                        {
                            return currentIndex;
                        }

                        if (hand == Hand.Right && Valve.VR.OpenVR.System.GetControllerRoleForTrackedDeviceIndex(index) == ETrackedControllerRole.RightHand)
                        {
                            return currentIndex;
                        }

                        currentIndex++;
                    }
                }

                return -1;
            }

            public class ButtonMask
            {
                public const ulong System = (1ul << (int)EVRButtonId.k_EButton_System); // reserved
                public const ulong ApplicationMenu = (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu);
                public const ulong Grip = (1ul << (int)EVRButtonId.k_EButton_Grip);
                public const ulong Axis0 = (1ul << (int)EVRButtonId.k_EButton_Axis0);
                public const ulong Axis1 = (1ul << (int)EVRButtonId.k_EButton_Axis1);
                public const ulong Axis2 = (1ul << (int)EVRButtonId.k_EButton_Axis2);
                public const ulong Axis3 = (1ul << (int)EVRButtonId.k_EButton_Axis3);
                public const ulong Axis4 = (1ul << (int)EVRButtonId.k_EButton_Axis4);
                public const ulong Touchpad = (1ul << (int)EVRButtonId.k_EButton_SteamVR_Touchpad);
                public const ulong Trigger = (1ul << (int)EVRButtonId.k_EButton_SteamVR_Trigger);
            }

            public enum ButtonId
            {
                ButtonSystem = 0,
                ButtonApplicationMenu = 1,
                ButtonGrip = 2,
                ButtonDPadLeft = 3,
                ButtonDPadUp = 4,
                ButtonDPadRight = 5,
                ButtonDPadDown = 6,
                ButtonA = 7,
                ButtonProximitySensor = 31,
                ButtonAxis0 = 32,
                ButtonAxis1 = 33,
                ButtonAxis2 = 34,
                ButtonAxis3 = 35,
                ButtonAxis4 = 36,
                ButtonSteamVrTouchpad = 32,
                ButtonSteamVrTrigger = 33,
                ButtonDashboardBack = 2,
                ButtonMax = 64,
            }

            public Controller(int controllerIndex)
            {
                var currentIndex = 0;
                for (uint index = 0; index < DevicePoses.Length; index++)
                {
                    if (Valve.VR.OpenVR.System.GetTrackedDeviceClass(index) == ETrackedDeviceClass.Controller)
                    {
                        if (currentIndex == controllerIndex)
                        {
                            ControllerIndex = index;
                            break;
                        }
                        currentIndex++;
                    }
                }
            }

            internal uint ControllerIndex;
            internal VRControllerState_t State;
            internal VRControllerState_t PreviousState;

            public bool GetPress(ulong buttonMask) { return (State.ulButtonPressed & buttonMask) != 0; }

            public bool GetPressDown(ulong buttonMask) { return (State.ulButtonPressed & buttonMask) != 0 && (PreviousState.ulButtonPressed & buttonMask) == 0; }

            public bool GetPressUp(ulong buttonMask) { return (State.ulButtonPressed & buttonMask) == 0 && (PreviousState.ulButtonPressed & buttonMask) != 0; }

            public bool GetPress(ButtonId buttonId) { return GetPress(1ul << (int)buttonId); }

            public bool GetPressDown(ButtonId buttonId) { return GetPressDown(1ul << (int)buttonId); }

            public bool GetPressUp(ButtonId buttonId) { return GetPressUp(1ul << (int)buttonId); }

            public bool GetTouch(ulong buttonMask) { return (State.ulButtonTouched & buttonMask) != 0; }

            public bool GetTouchDown(ulong buttonMask) { return (State.ulButtonTouched & buttonMask) != 0 && (PreviousState.ulButtonTouched & buttonMask) == 0; }

            public bool GetTouchUp(ulong buttonMask) { return (State.ulButtonTouched & buttonMask) == 0 && (PreviousState.ulButtonTouched & buttonMask) != 0; }

            public bool GetTouch(ButtonId buttonId) { return GetTouch(1ul << (int)buttonId); }

            public bool GetTouchDown(ButtonId buttonId) { return GetTouchDown(1ul << (int)buttonId); }

            public bool GetTouchUp(ButtonId buttonId) { return GetTouchUp(1ul << (int)buttonId); }

            public Vector2 GetAxis(ButtonId buttonId = ButtonId.ButtonSteamVrTouchpad)
            {
                var axisId = (uint)buttonId - (uint)EVRButtonId.k_EButton_Axis0;
                switch (axisId)
                {
                    case 0: return new Vector2(State.rAxis0.x, State.rAxis0.y);
                    case 1: return new Vector2(State.rAxis1.x, State.rAxis1.y);
                    case 2: return new Vector2(State.rAxis2.x, State.rAxis2.y);
                    case 3: return new Vector2(State.rAxis3.x, State.rAxis3.y);
                    case 4: return new Vector2(State.rAxis4.x, State.rAxis4.y);
                }
                return Vector2.Zero;
            }

            public void Update()
            {
                PreviousState = State;
                Valve.VR.OpenVR.System.GetControllerState(ControllerIndex, ref State, (uint)Unsafe.SizeOf<VRControllerState_t>());
            }
        }

        public class TrackedDevice
        {
            public TrackedDevice(int trackerIndex)
            {
                TrackerIndex = trackerIndex;
            }

            const int StringBuilderSize = 64;
            StringBuilder serialNumberStringBuilder = new StringBuilder(StringBuilderSize);
            internal string SerialNumber
            {
                get
                {
                    var error = ETrackedPropertyError.TrackedProp_Success;
                    serialNumberStringBuilder.Clear();
                    Valve.VR.OpenVR.System.GetStringTrackedDeviceProperty((uint)TrackerIndex, ETrackedDeviceProperty.Prop_SerialNumber_String, serialNumberStringBuilder, StringBuilderSize, ref error);
                    if (error == ETrackedPropertyError.TrackedProp_Success)
                        return serialNumberStringBuilder.ToString();
                    else
                        return "";
                }
            }

            internal float BatteryPercentage
            {
                get
                {
                    var error = ETrackedPropertyError.TrackedProp_Success;
                    var value = Valve.VR.OpenVR.System.GetFloatTrackedDeviceProperty((uint)TrackerIndex, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref error);
                    if (error == ETrackedPropertyError.TrackedProp_Success)
                        return value;
                    else
                        return 0;
                }
            }

            internal int TrackerIndex;
            internal ETrackedDeviceClass DeviceClass => Valve.VR.OpenVR.System.GetTrackedDeviceClass((uint)TrackerIndex);
        }

        private static readonly TrackedDevicePose_t[] DevicePoses = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        private static readonly TrackedDevicePose_t[] GamePoses = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];

        static OpenVR()
        {
            NativeLibraryHelper.PreloadLibrary("openvr_api", typeof(OpenVR));
        }

        public static bool InitDone = false;

        public static bool Init()
        {
            var err = EVRInitError.None;
            Valve.VR.OpenVR.Init(ref err);
            if (err != EVRInitError.None)
            {
                return false;
            }

            InitDone = true;

            //set Universe to Seated by default
            Valve.VR.OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseSeated);

            return true;
        }

        public static void Shutdown()
        {
            if (!InitDone) return;
            Valve.VR.OpenVR.Shutdown();
            InitDone = false;
        }

        public static bool Submit(int eyeIndex, Texture texture, ref RectangleF viewport)
        {
            var tex = new Texture_t
            {
                eType = ETextureType.DirectX,
                eColorSpace = EColorSpace.Auto,
                handle = (nint) texture.NativeResource.Handle
            };
            var bounds = new VRTextureBounds_t
            {
                uMin = viewport.X,
                uMax = viewport.Width,
                vMin = viewport.Y,
                vMax = viewport.Height,
            };

            return Valve.VR.OpenVR.Compositor.Submit(eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right, ref tex, ref bounds, EVRSubmitFlags.Submit_Default) == EVRCompositorError.None;
        }

        public static void GetEyeToHead(int eyeIndex, out Matrix4x4 pose)
        {
            GetEyeToHeadUnsafe(eyeIndex, out pose);
        }

        private static void GetEyeToHeadUnsafe(int eyeIndex, out Matrix4x4 pose)
        {
            var eye = eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right;
            pose = ToMatrix(Valve.VR.OpenVR.System.GetEyeToHeadTransform(eye));
        }

        // OpenVR puts the vector on the right of a matrix: its rows are the columns of a Matrix.
        private static Matrix4x4 ToMatrix(in HmdMatrix34_t m) => new(
            m.m0, m.m4, m.m8, 0,
            m.m1, m.m5, m.m9, 0,
            m.m2, m.m6, m.m10, 0,
            m.m3, m.m7, m.m11, 1);

        private static HmdMatrix34_t ToHmdMatrix34(in Matrix4x4 m) => new()
        {
            m0 = m.M11, m1 = m.M21, m2 = m.M31, m3 = m.M41,
            m4 = m.M12, m5 = m.M22, m6 = m.M32, m7 = m.M42,
            m8 = m.M13, m9 = m.M23, m10 = m.M33, m11 = m.M43,
        };

        public static void UpdatePoses()
        {
            Valve.VR.OpenVR.Compositor.WaitGetPoses(DevicePoses, GamePoses);
        }

        public static void Recenter()
        {
            Valve.VR.OpenVR.Chaperone.ResetZeroPose(ETrackingUniverseOrigin.TrackingUniverseSeated);
        }

        public static void SetTrackingSpace(ETrackingUniverseOrigin space)
        {
            Valve.VR.OpenVR.Compositor.SetTrackingSpace(space);
        }

        public static DeviceState GetControllerPose(int controllerIndex, out Matrix4x4 pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            return GetControllerPoseUnsafe(controllerIndex, out pose, out velocity, out angVelocity);
        }

        private static unsafe DeviceState GetControllerPoseUnsafe(int controllerIndex, out Matrix4x4 pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            Debug.Assert(Unsafe.SizeOf<Vector3>() == Unsafe.SizeOf<HmdVector3_t>());
            Debug.Assert(Unsafe.SizeOf<Matrix4x4>() > Unsafe.SizeOf<HmdMatrix34_t>());
            var currentIndex = 0;

            pose = Matrix4x4.Identity;

            for (uint index = 0; index < DevicePoses.Length; index++)
            {
                if (Valve.VR.OpenVR.System.GetTrackedDeviceClass(index) == ETrackedDeviceClass.Controller)
                {
                    if (currentIndex == controllerIndex)
                    {
                        ref var devicePose = ref DevicePoses[index];
                        pose = ToMatrix(devicePose.mDeviceToAbsoluteTracking);
                        velocity = Unsafe.As<HmdVector3_t, Vector3>(ref devicePose.vVelocity);
                        angVelocity = Unsafe.As<HmdVector3_t, Vector3>(ref devicePose.vAngularVelocity);

                        var state = DeviceState.Invalid;
                        if (devicePose.bDeviceIsConnected && devicePose.bPoseIsValid)
                        {
                            state = DeviceState.Valid;
                        }
                        else if (devicePose.bDeviceIsConnected && !devicePose.bPoseIsValid && devicePose.eTrackingResult == ETrackingResult.Running_OutOfRange)
                        {
                            state = DeviceState.OutOfRange;
                        }

                        return state;
                    }
                    currentIndex++;
                }
            }
            velocity = default;
            angVelocity = default;
            return DeviceState.Invalid;
        }

        public static DeviceState GetTrackerPose(int trackerIndex, out Matrix4x4 pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            return GetTrackerPoseUnsafe(trackerIndex, out pose, out velocity, out angVelocity);
        }

        private static unsafe DeviceState GetTrackerPoseUnsafe(int trackerIndex, out Matrix4x4 pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            Debug.Assert(Unsafe.SizeOf<Vector3>() == Unsafe.SizeOf<HmdVector3_t>());
            ref var devicePose = ref DevicePoses[trackerIndex];

            pose = ToMatrix(devicePose.mDeviceToAbsoluteTracking);
            velocity = Unsafe.As<HmdVector3_t, Vector3>(ref devicePose.vVelocity);
            angVelocity = Unsafe.As<HmdVector3_t, Vector3>(ref devicePose.vAngularVelocity);

            var state = DeviceState.Invalid;
            if (devicePose.bDeviceIsConnected && devicePose.bPoseIsValid)
            {
                state = DeviceState.Valid;
            }
            else if (devicePose.bDeviceIsConnected && !devicePose.bPoseIsValid && devicePose.eTrackingResult == ETrackingResult.Running_OutOfRange)
            {
                state = DeviceState.OutOfRange;
            }

            return state;
        }

        public static DeviceState GetHeadPose(out Matrix4x4 pose, out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            return GetHeadPoseUnsafe(out pose, out linearVelocity, out angularVelocity);
        }

        private static unsafe DeviceState GetHeadPoseUnsafe(out Matrix4x4 pose, out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            Debug.Assert(Unsafe.SizeOf<Vector3>() == Unsafe.SizeOf<HmdVector3_t>());
            Debug.Assert(Unsafe.SizeOf<Matrix4x4>() > Unsafe.SizeOf<HmdMatrix34_t>());
            pose = Matrix4x4.Identity;
            for (uint index = 0; index < DevicePoses.Length; index++)
            {
                ref var devicePose = ref DevicePoses[index];
                if (Valve.VR.OpenVR.System.GetTrackedDeviceClass(index) == ETrackedDeviceClass.HMD)
                {
                    pose = ToMatrix(devicePose.mDeviceToAbsoluteTracking);
                    linearVelocity = Unsafe.As<HmdVector3_t, Vector3>(ref devicePose.vVelocity);
                    angularVelocity = Unsafe.As<HmdVector3_t, Vector3>(ref devicePose.vAngularVelocity);

                    var state = DeviceState.Invalid;
                    if (DevicePoses[index].bDeviceIsConnected && DevicePoses[index].bPoseIsValid)
                    {
                        state = DeviceState.Valid;
                    }
                    else if (DevicePoses[index].bDeviceIsConnected && !DevicePoses[index].bPoseIsValid && DevicePoses[index].eTrackingResult == ETrackingResult.Running_OutOfRange)
                    {
                        state = DeviceState.OutOfRange;
                    }

                    return state;
                }
            }

            linearVelocity = default;
            angularVelocity = default;
            return DeviceState.Invalid;
        }

        public static void GetProjection(int eyeIndex, float near, float far, out Matrix4x4 projection)
        {
            GetProjectionUnsafe(eyeIndex, near, far, out projection);
        }

        private static unsafe void GetProjectionUnsafe(int eyeIndex, float near, float far, out Matrix4x4 projection)
        {
            Debug.Assert(Unsafe.SizeOf<HmdMatrix44_t>() == Unsafe.SizeOf<Matrix4x4>());
            var eye = eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right;
            var proj = Valve.VR.OpenVR.System.GetProjectionMatrix(eye, near, far);
            // Same elements as a Matrix, stored for a vector on the right: the transpose
            projection = Matrix4x4.Transpose(Unsafe.As<HmdMatrix44_t, Matrix4x4>(ref proj));
        }

        public static void ShowMirror()
        {
            Valve.VR.OpenVR.Compositor.ShowMirrorWindow();
        }

        public static void HideMirror()
        {
            Valve.VR.OpenVR.Compositor.HideMirrorWindow();
        }

        public static Texture GetMirrorTexture(GraphicsDevice device, int eyeIndex)
        {
            var nativeDevice = device.NativeDevice;

            var eyeTextureSrv = IntPtr.Zero;
            Valve.VR.OpenVR.Compositor.GetMirrorTextureD3D11(eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right, (nint) nativeDevice.Handle, ref eyeTextureSrv);

            var srv = (ID3D11ShaderResourceView*) eyeTextureSrv;

            var texture = new Texture(device).InitializeFromImpl(srv);

            // We don't need to take ownership of the COM pointer.
            //   We are already AddRef()ing in Texture.InitializeFromImpl when storing the COM pointer;
            //   compensate with Release() to return the reference count to its previous value
            srv->Release();

            return texture;
        }

        public static void GetRecommendedRenderTargetSize(out (uint x, uint y) size)
        {
            size = default;
            Valve.VR.OpenVR.System.GetRecommendedRenderTargetSize(ref size.x, ref size.y);
        }

        public static ulong CreateOverlay()
        {
            var layerKeyName = Guid.NewGuid().ToString();
            ulong handle = 0;
            return Valve.VR.OpenVR.Overlay.CreateOverlay(layerKeyName, layerKeyName, ref handle) == EVROverlayError.None ? handle : 0;
        }

        public static void InitOverlay(ulong overlayId)
        {
            Valve.VR.OpenVR.Overlay.SetOverlayInputMethod(overlayId, VROverlayInputMethod.None);
            Valve.VR.OpenVR.Overlay.SetOverlayFlag(overlayId, VROverlayFlags.SortWithNonSceneOverlays, true);
        }

        public static bool SubmitOverlay(ulong overlayId, Texture texture)
        {
            var tex = new Texture_t
            {
                eType = ETextureType.DirectX,
                eColorSpace = EColorSpace.Auto,
                handle = (nint) texture.NativeResource.Handle
            };

            return Valve.VR.OpenVR.Overlay.SetOverlayTexture(overlayId, ref tex) == EVROverlayError.None;
        }

        public static unsafe void SetOverlayParams(ulong overlayId, Matrix4x4 transform, bool followsHead, Vector2 surfaceSize)
        {
            Valve.VR.OpenVR.Overlay.SetOverlayWidthInMeters(overlayId, 1.0f);

            transform = Matrix4x4.CreateScale(new Vector3(surfaceSize.X, surfaceSize.Y, 1.0f)) * transform;

            var pose = ToHmdMatrix34(transform);
            if (followsHead)
            {
                Valve.VR.OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(overlayId, 0, ref pose);
            }
            else
            {
                Valve.VR.OpenVR.Overlay.SetOverlayTransformAbsolute(overlayId, ETrackingUniverseOrigin.TrackingUniverseSeated, ref pose);
            }
        }

        public static void SetOverlayEnabled(ulong overlayId, bool enabled)
        {
            if (enabled)
                Valve.VR.OpenVR.Overlay.ShowOverlay(overlayId);
            else
                Valve.VR.OpenVR.Overlay.HideOverlay(overlayId);
        }
    }
}

#endif
