// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// Defines and sets a <see cref="Rendering.RenderView"/> and set it up using <see cref="Camera"/> or current context camera.
    /// </summary>
    /// <remarks>
    /// Since it sets a view, it is usually not shareable for multiple rendering.
    /// </remarks>
    [Display("Camera Renderer")]
    public partial class SceneCameraRenderer : SceneRendererBase
    {
        [DataMemberIgnore]
        public RenderView RenderView { get; } = new RenderView();
        
        /// <summary>
        /// Gets or sets the camera.
        /// </summary>
        /// <value>The camera.</value>
        /// <userdoc>The camera to use to render the scene.</userdoc>
        public SceneCameraSlot Camera { get; set; }

        public ISceneRenderer Child { get; set; }

        public RenderGroupMask RenderMask { get; set; } = RenderGroupMask.All;

        [DataMemberIgnore]
        public Logger Logger = GlobalLogger.GetLogger(nameof(SceneCameraRenderer));

        private bool cameraSlotResolutionFailed;
        private bool cameraResolutionFailed;

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);

            // Find camera
            var camera = ResolveCamera(context);
            if (camera == null)
                return;

            // Setup render view
            context.RenderSystem.Views.Add(RenderView);
            UpdateCameraToRenderView(context, RenderView, camera);

            using (context.PushRenderViewAndRestore(RenderView))
            using (context.PushTagAndRestore(CameraComponentRendererExtensions.Current, camera))
            {
                CollectInner(context);
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            // Find camera
            var camera = ResolveCamera(context);
            if (camera == null)
                return;

            using (context.PushRenderViewAndRestore(RenderView))
            using (context.PushTagAndRestore(CameraComponentRendererExtensions.Current, camera))
            {
                DrawInner(drawContext);
            }
        }

        /// <summary>
        /// Resolves camera to the one contained in slot <see cref="Camera"/>.
        /// </summary>
        protected virtual CameraComponent ResolveCamera(RenderContext renderContext)
        {
            if (Camera == null && !cameraSlotResolutionFailed)
                Logger.Warning($"{nameof(SceneCameraRenderer)} [{Id}] has no camera set. Make sure to set camera to the renderer via the Graphic Compositor Editor.");

            cameraSlotResolutionFailed = Camera == null;
            if (cameraSlotResolutionFailed)
                return null;

            var camera = Camera?.Camera;
            if (camera == null && !cameraResolutionFailed)
                    Logger.Warning($"{nameof(SceneCameraRenderer)} [{Id}] has no camera assigned to its {nameof(CameraComponent.Slot)}[{Camera.Name}]. Make sure a camera is enabled and assigned to the corresponding {nameof(CameraComponent.Slot)}.");

            cameraResolutionFailed = camera == null;

            return camera;
        }

        protected virtual void CollectInner(RenderContext renderContext)
        {
            RenderView.CullingMask = RenderMask;

            Child?.Collect(renderContext);
        }

        protected virtual void DrawInner(RenderDrawContext renderContext)
        {
            Child?.Draw(renderContext);
        }

        public static void UpdateCameraToRenderView(RenderContext context, RenderView renderView, CameraComponent camera)
        {
            if (context == null || renderView == null)
                return;

            // TODO: Multiple viewports?
            var currentViewport = context.ViewportState.Viewport0;
            renderView.ViewSize = new Vector2(currentViewport.Width, currentViewport.Height);

            if (camera == null)
                return;

            // Setup viewport size
            var aspectRatio = currentViewport.AspectRatio;

            // Update the aspect ratio
            if (camera.UseCustomAspectRatio)
            {
                aspectRatio = camera.AspectRatio;
            }

            // If the aspect ratio is calculated automatically from the current viewport, update matrices here
            camera.Update(aspectRatio);

            // Copy camera data
            renderView.View = camera.ViewMatrix;
            renderView.Projection = camera.ProjectionMatrix;
            renderView.NearClipPlane = camera.NearClipPlane;
            renderView.FarClipPlane = camera.FarClipPlane;
            renderView.Frustum = camera.Frustum;

            // Enable frustum culling
            renderView.CullingMode = CameraCullingMode.Frustum;

            Matrix.Multiply(ref renderView.View, ref renderView.Projection, out renderView.ViewProjection);
        }
    }
}
