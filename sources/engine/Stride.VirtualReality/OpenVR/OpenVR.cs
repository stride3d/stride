// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Text;
using SharpDX.Direct3D11;
using Valve.VR;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.VirtualReality
{
    internal static class OpenVR
    {
        public class Controller
        {
            // This helper can be used in a variety of ways.  Beware that indices may change
            // as new devices are dynamically added or removed, controllers are physically
            // swapped between hands, arms crossed, etc.
            public enum Hand
            {
                Left,
                Right,
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
                Valve.VR.OpenVR.System.GetControllerState(ControllerIndex, ref State, (uint)Utilities.SizeOf<VRControllerState_t>());
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
            NativeLibrary.PreloadLibrary("openvr_api.dll", typeof(OpenVR));
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

            //this makes the camera behave like oculus rift default!
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
                eType = EGraphicsAPIConvention.API_DirectX,
                eColorSpace = EColorSpace.Auto,
                handle = texture.NativeResource.NativePointer,
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

        public static void GetEyeToHead(int eyeIndex, out Matrix pose)
        {
            GetEyeToHeadUnsafe(eyeIndex, out pose);
        }

        private static unsafe void GetEyeToHeadUnsafe(int eyeIndex, out Matrix pose)
        {
            pose = Matrix.Identity;
            var eye = eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right;
            var eyeToHead = Valve.VR.OpenVR.System.GetEyeToHeadTransform(eye);
            Utilities.CopyMemory((IntPtr)Interop.Fixed(ref pose), (IntPtr)Interop.Fixed(ref eyeToHead), Utilities.SizeOf<HmdMatrix34_t>());
        }

        public static void UpdatePoses()
        {
            Valve.VR.OpenVR.Compositor.WaitGetPoses(DevicePoses, GamePoses);
        }

        public static void Recenter()
        {
            Valve.VR.OpenVR.System.ResetSeatedZeroPose();
        }

        public static void SetTrackingSpace(ETrackingUniverseOrigin space)
        {
            Valve.VR.OpenVR.Compositor.SetTrackingSpace(space);
        }

        public static DeviceState GetControllerPose(int controllerIndex, out Matrix pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            return GetControllerPoseUnsafe(controllerIndex, out pose, out velocity, out angVelocity);
        }

        private static unsafe DeviceState GetControllerPoseUnsafe(int controllerIndex, out Matrix pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            var currentIndex = 0;

            pose = Matrix.Identity;
            velocity = Vector3.Zero;
            angVelocity = Vector3.Zero;

            for (uint index = 0; index < DevicePoses.Length; index++)
            {
                if (Valve.VR.OpenVR.System.GetTrackedDeviceClass(index) == ETrackedDeviceClass.Controller)
                {
                    if (currentIndex == controllerIndex)
                    {
                        Utilities.CopyMemory((IntPtr)Interop.Fixed(ref pose), (IntPtr)Interop.Fixed(ref DevicePoses[index].mDeviceToAbsoluteTracking), Utilities.SizeOf<HmdMatrix34_t>());
                        Utilities.CopyMemory((IntPtr)Interop.Fixed(ref velocity), (IntPtr)Interop.Fixed(ref DevicePoses[index].vVelocity), Utilities.SizeOf<HmdVector3_t>());
                        Utilities.CopyMemory((IntPtr)Interop.Fixed(ref angVelocity), (IntPtr)Interop.Fixed(ref DevicePoses[index].vAngularVelocity), Utilities.SizeOf<HmdVector3_t>());

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
                    currentIndex++;
                }
            }

            return DeviceState.Invalid;
        }

        public static DeviceState GetTrackerPose(int trackerIndex, out Matrix pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            return GetTrackerPoseUnsafe(trackerIndex, out pose, out velocity, out angVelocity);
        }

        private static unsafe DeviceState GetTrackerPoseUnsafe(int trackerIndex, out Matrix pose, out Vector3 velocity, out Vector3 angVelocity)
        {
            pose = Matrix.Identity;
            velocity = Vector3.Zero;
            angVelocity = Vector3.Zero;
            var index = trackerIndex;

            Utilities.CopyMemory((IntPtr)Interop.Fixed(ref pose), (IntPtr)Interop.Fixed(ref DevicePoses[index].mDeviceToAbsoluteTracking), Utilities.SizeOf<HmdMatrix34_t>());
            Utilities.CopyMemory((IntPtr)Interop.Fixed(ref velocity), (IntPtr)Interop.Fixed(ref DevicePoses[index].vVelocity), Utilities.SizeOf<HmdVector3_t>());
            Utilities.CopyMemory((IntPtr)Interop.Fixed(ref angVelocity), (IntPtr)Interop.Fixed(ref DevicePoses[index].vAngularVelocity), Utilities.SizeOf<HmdVector3_t>());

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

        public static DeviceState GetHeadPose(out Matrix pose, out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            return GetHeadPoseUnsafe(out pose, out linearVelocity, out angularVelocity);
        }

        private static unsafe DeviceState GetHeadPoseUnsafe(out Matrix pose, out Vector3 linearVelocity, out Vector3 angularVelocity)
        {
            pose = Matrix.Identity;
            linearVelocity = Vector3.Zero;
            angularVelocity = Vector3.Zero;
            for (uint index = 0; index < DevicePoses.Length; index++)
            {
                if (Valve.VR.OpenVR.System.GetTrackedDeviceClass(index) == ETrackedDeviceClass.HMD)
                {
                    Utilities.CopyMemory((IntPtr)Interop.Fixed(ref pose), (IntPtr)Interop.Fixed(ref DevicePoses[index].mDeviceToAbsoluteTracking), Utilities.SizeOf<HmdMatrix34_t>());
                    Utilities.CopyMemory((IntPtr)Interop.Fixed(ref linearVelocity), (IntPtr)Interop.Fixed(ref DevicePoses[index].vVelocity), Utilities.SizeOf<HmdVector3_t>());
                    Utilities.CopyMemory((IntPtr)Interop.Fixed(ref angularVelocity), (IntPtr)Interop.Fixed(ref DevicePoses[index].vAngularVelocity), Utilities.SizeOf<HmdVector3_t>());

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

            return DeviceState.Invalid;
        }

        public static void GetProjection(int eyeIndex, float near, float far, out Matrix projection)
        {
            GetProjectionUnsafe(eyeIndex, near, far, out projection);
        }

        private static unsafe void GetProjectionUnsafe(int eyeIndex, float near, float far, out Matrix projection)
        {
            projection = Matrix.Identity;
            var eye = eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right;
            var proj = Valve.VR.OpenVR.System.GetProjectionMatrix(eye, near, far, EGraphicsAPIConvention.API_DirectX);
            Utilities.CopyMemory((IntPtr)Interop.Fixed(ref projection), (IntPtr)Interop.Fixed(ref proj), Utilities.SizeOf<Matrix>());
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
            var nativeDevice = device.NativeDevice.NativePointer;
            var eyeTexSrv = IntPtr.Zero;
            Valve.VR.OpenVR.Compositor.GetMirrorTextureD3D11(eyeIndex == 0 ? EVREye.Eye_Left : EVREye.Eye_Right, nativeDevice, ref eyeTexSrv);
            var tex = new Texture(device);
            tex.InitializeFromImpl(new ShaderResourceView(eyeTexSrv));
            return tex;
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
                eType = EGraphicsAPIConvention.API_DirectX,
                eColorSpace = EColorSpace.Auto,
                handle = texture.NativeResource.NativePointer,
            };
           
            return Valve.VR.OpenVR.Overlay.SetOverlayTexture(overlayId, ref tex) == EVROverlayError.None;
        }

        public static unsafe void SetOverlayParams(ulong overlayId, Matrix transform, bool followsHead, Vector2 surfaceSize)
        {
            Valve.VR.OpenVR.Overlay.SetOverlayWidthInMeters(overlayId, 1.0f);

            transform = Matrix.Scaling(new Vector3(surfaceSize.X, surfaceSize.Y, 1.0f)) * transform;

            if (followsHead)
            {
                HmdMatrix34_t pose = new HmdMatrix34_t();
                Utilities.CopyMemory((IntPtr)Interop.Fixed(ref pose), (IntPtr)Interop.Fixed(ref transform), Utilities.SizeOf<HmdMatrix34_t>());
                Valve.VR.OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(overlayId, 0, ref pose);
            }
            else
            {
                HmdMatrix34_t pose = new HmdMatrix34_t();
                Utilities.CopyMemory((IntPtr)Interop.Fixed(ref pose), (IntPtr)Interop.Fixed(ref transform), Utilities.SizeOf<HmdMatrix34_t>());
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
