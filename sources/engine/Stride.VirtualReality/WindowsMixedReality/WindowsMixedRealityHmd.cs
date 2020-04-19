// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_PLATFORM_UWP

using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Holographic;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace Stride.VirtualReality
{
    internal class WindowsMixedRealityHmd : VRDevice
    {
        private Matrix currentView = Matrix.Identity;
        private WindowsMixedRealityGraphicsPresenter graphicsPresenter;
        private HolographicCamera holographicCamera;
        private HolographicSpace holographicSpace;
        private SpatialInteractionManager interactionManager;
        private DeviceState internalState;
        private WindowsMixedRealityTouchController leftHandController;
        private WindowsMixedRealityTouchController rightHandController;
        private SpatialLocation spatialLocation;
        private SpatialLocator spatialLocator;
        private SpatialStationaryFrameOfReference stationaryReferenceFrame;

        public WindowsMixedRealityHmd()
        {
            VRApi = VRApi.WindowsMixedReality;
            SupportsOverlays = false;
        }

        public override Size2 OptimalRenderFrameSize => ActualRenderFrameSize;

        public override Size2 ActualRenderFrameSize
        {
            get => new Size2((int)holographicCamera.RenderTargetSize.Width * 2, (int)holographicCamera.RenderTargetSize.Height);
            protected set { }
        }

        public override Texture MirrorTexture { get; protected set; }

        public override float RenderFrameScaling
        {
            get => (float)holographicCamera.ViewportScaleFactor;
            set { if (holographicCamera != null) holographicCamera.ViewportScaleFactor = value; }
        }

        public override DeviceState State => internalState;

        public override Vector3 HeadPosition => spatialLocation.Position.ToVector3();

        public override Quaternion HeadRotation => spatialLocation.Orientation.ToQuaternion();

        public override Vector3 HeadLinearVelocity => spatialLocation.AbsoluteLinearVelocity.ToVector3();

        public override Vector3 HeadAngularVelocity => spatialLocation.AbsoluteAngularVelocity.ToQuaternion().YawPitchRoll;

        public override TouchController LeftHand => leftHandController;

        public override TouchController RightHand => rightHandController;

        public override TrackedItem[] TrackedItems => new TrackedItem[0];

        public override bool CanInitialize => true;

        public override void Commit(CommandList commandList, Texture renderFrame)
        {
            // On versions of the platform that support the CommitDirect3D11DepthBuffer API, we can
            // provide the depth buffer to the system, and it will use depth information to stabilize
            // the image at a per-pixel level.
            var pose = graphicsPresenter.HolographicFrame.CurrentPrediction.CameraPoses[0];
            HolographicCameraRenderingParameters renderingParameters = graphicsPresenter.HolographicFrame.GetRenderingParameters(pose);
            SharpDX.Direct3D11.Texture2D depthBuffer = new SharpDX.Direct3D11.Texture2D(commandList.DepthStencilBuffer.NativeResource.NativePointer);

            // Direct3D interop APIs are used to provide the buffer to the WinRT API.
            SharpDX.DXGI.Resource1 depthStencilResource = depthBuffer.QueryInterface<SharpDX.DXGI.Resource1>();
            SharpDX.DXGI.Surface2 depthDxgiSurface = new SharpDX.DXGI.Surface2(depthStencilResource, 0);
            IDirect3DSurface depthD3DSurface = WindowsMixedRealityGraphicsPresenter.CreateDirect3DSurface(depthDxgiSurface.NativePointer);

            if (depthD3DSurface != null)
            {
                // Calling CommitDirect3D11DepthBuffer causes the system to queue Direct3D commands to
                // read the depth buffer. It will then use that information to stabilize the image as
                // the HolographicFrame is presented.
                renderingParameters.CommitDirect3D11DepthBuffer(depthD3DSurface);
            }
        }

        public override void Draw(GameTime gameTime)
        {
        }

        public override void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
            HolographicSpace.IsAvailableChanged += HolographicSpace_IsAvailableChanged;
            HolographicSpace_IsAvailableChanged(null, null);

            graphicsPresenter = device.Presenter as WindowsMixedRealityGraphicsPresenter;

            holographicSpace = graphicsPresenter.NativePresenter as HolographicSpace;
            holographicCamera = graphicsPresenter.HolographicFrame.CurrentPrediction.CameraPoses[0].HolographicCamera;

            interactionManager = SpatialInteractionManager.GetForCurrentView();

            leftHandController = new WindowsMixedRealityTouchController(TouchControllerHand.Left, interactionManager);
            rightHandController = new WindowsMixedRealityTouchController(TouchControllerHand.Right, interactionManager);

            MirrorTexture = graphicsPresenter.BackBuffer;
        }

        public override void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection)
        {
            var cameraPose = graphicsPresenter.HolographicFrame.CurrentPrediction.CameraPoses[0];

            cameraPose.HolographicCamera.SetNearPlaneDistance(near);
            cameraPose.HolographicCamera.SetFarPlaneDistance(far);

            var viewTransform = cameraPose.TryGetViewTransform(stationaryReferenceFrame.CoordinateSystem);

            if (viewTransform.HasValue)
            {
                currentView = eye == Eyes.Left
                    ? viewTransform.Value.Left.ToMatrix()
                    : viewTransform.Value.Right.ToMatrix();
            }

            if (ignoreHeadPosition)
            {
                currentView.TranslationVector = Vector3.Zero;
            }

            if (ignoreHeadRotation)
            {
                // keep the scale just in case
                currentView.Row1 = new Vector4(currentView.Row1.Length(), 0, 0, 0);
                currentView.Row2 = new Vector4(0, currentView.Row2.Length(), 0, 0);
                currentView.Row3 = new Vector4(0, 0, currentView.Row3.Length(), 0);
            }

            view = Matrix.Translation(-cameraPosition) * cameraRotation * currentView;

            projection = eye == Eyes.Left
                ? cameraPose.ProjectionTransform.Left.ToMatrix()
                : cameraPose.ProjectionTransform.Right.ToMatrix();
        }

        public override void Update(GameTime gameTime)
        {
            HolographicFramePrediction prediction = graphicsPresenter.HolographicFrame.CurrentPrediction;

            SpatialCoordinateSystem coordinateSystem = stationaryReferenceFrame.CoordinateSystem;
            spatialLocation = spatialLocator.TryLocateAtTimestamp(prediction.Timestamp, coordinateSystem);

            leftHandController.Update(prediction.Timestamp, coordinateSystem);
            rightHandController.Update(prediction.Timestamp, coordinateSystem);
        }

        private void HolographicSpace_IsAvailableChanged(object sender, object e)
        {
            internalState = HolographicSpace.IsAvailable ? DeviceState.Valid : DeviceState.Invalid;

            HolographicDisplay holographicDisplay = HolographicDisplay.GetDefault();
            SpatialLocator newSpatialLocator = holographicDisplay?.SpatialLocator;

            if (spatialLocator != newSpatialLocator)
            {
                if (spatialLocator != null)
                {
                    spatialLocator.LocatabilityChanged -= SpatialLocator_LocatabilityChanged;
                    spatialLocator = null;
                }

                if (newSpatialLocator != null)
                {
                    spatialLocator = newSpatialLocator;
                    spatialLocator.LocatabilityChanged += SpatialLocator_LocatabilityChanged;
                    stationaryReferenceFrame = spatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();
                }
            }
        }

        private void SpatialLocator_LocatabilityChanged(SpatialLocator sender, object args)
        {
            switch (sender.Locatability)
            {
                case SpatialLocatability.Unavailable:
                    internalState = DeviceState.Invalid;
                    break;
                default:
                    internalState = DeviceState.Valid;
                    break;
            }
        }
    }
}

#endif
