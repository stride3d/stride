// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;
using Stride.UI.Renderers;
using System.Numerics;

namespace Stride.Rendering.UI
{
    public partial class UIRenderFeature : RootRenderFeature
    {
        private IGame game;
        private UISystem uiSystem;
        private InputManager input;
        private IGraphicsDeviceService graphicsDeviceService;

        private RendererManager rendererManager;

        private readonly UIRenderingContext renderingContext = new UIRenderingContext();

        private UIBatch batch;

        private readonly object drawLock = new object();

        private readonly LayoutingContext layoutingContext = new LayoutingContext();

        private readonly List<UIElementState> uiElementStates = new List<UIElementState>();

        public override Type SupportedRenderObjectType => typeof(RenderUIElement);

        /// <summary>
        /// Represents the UI-element currently under the mouse cursor.
        /// Only elements with CanBeHitByUser == true are taken into account.
        /// Last processed element_state / ?UIComponent? with a valid element will be used.
        /// </summary>
        public UIElement UIElementUnderMouseCursor => uiSystem.UIElementUnderMouseCursor;

        public UIRenderFeature()
        {
            SortKey = 192;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            Name = "UIComponentRenderer";
            game = RenderSystem.Services.GetService<IGame>();
            input = RenderSystem.Services.GetService<InputManager>();
            uiSystem = RenderSystem.Services.GetOrCreate<UISystem>();
            graphicsDeviceService = RenderSystem.Services.GetSafeServiceAs<IGraphicsDeviceService>();

            rendererManager = new RendererManager(new DefaultRenderersFactory(RenderSystem.Services));

            batch = uiSystem.Batch;
        }

        public override void Flush(RenderDrawContext context)
        {
            base.Flush(context);

            // Runs once the scene has drawn, also when no UI was in it, so picking can tell which targets are current
            if (game != null)
                uiSystem.RecordDrawnFrame(game.DrawTime.FrameCount);
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            lock (drawLock)
            {
                using (context.PushRenderTargetsAndRestore())
                {
                    DrawInternal(context, renderView, renderViewStage, startIndex, endIndex);
                }
            }
        }

        private void DrawInternal(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            base.Draw(context, renderView, renderViewStage, startIndex, endIndex);

            var uiProcessor = SceneInstance.GetCurrent(context.RenderContext).GetProcessor<UIRenderProcessor>();
            if (uiProcessor == null)
                return;


            // build the list of the UI elements to render
            uiElementStates.Clear();
            for (var index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);
                var renderElement = (RenderUIElement)renderNode.RenderObject;

                uiElementStates.Add(new UIElementState(renderElement));
            }

            // evaluate the current draw time (game instance is null for thumbnails)
            var drawTime = game != null ? game.DrawTime : new GameTime();

            // update the rendering context
            renderingContext.GraphicsContext = context.GraphicsContext;
            renderingContext.Time = drawTime;
            renderingContext.RenderTarget = context.CommandList.RenderTargets[0]; // TODO: avoid hardcoded index 0

            // allocate temporary graphics resources if needed
            Texture scopedDepthBuffer = null;
            foreach (var uiElement in uiElementStates)
            {
                if (uiElement.RenderObject.IsFullScreen)
                {
                    var renderTarget = renderingContext.RenderTarget;
                    var description = TextureDescription.New2D(renderTarget.Width, renderTarget.Height, PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);
                    scopedDepthBuffer = context.RenderContext.Allocator.GetTemporaryTexture(description);
                    break;
                }
            }

            var backBuffer = graphicsDeviceService.GraphicsDevice.Presenter?.BackBuffer;
            var backBufferSize = backBuffer != null ? new Vector2(backBuffer.Width, backBuffer.Height) : Vector2.Zero;

            // update view parameters and record what picking needs for the next capture phase
            foreach (var uiElementState in uiElementStates)
            {
                var renderObject = uiElementState.RenderObject;
                var rootElement = renderObject.Page?.RootElement;
                if (rootElement == null)
                    continue;

                // calculate the size of the virtual resolution depending on target size (UI canvas)
                var virtualResolution = renderObject.Resolution;

                if (renderObject.IsFullScreen)
                {
                    //var targetSize = viewportSize;
                    var targetSize = new Vector2(renderingContext.RenderTarget.Width, renderingContext.RenderTarget.Height);

                    // update the virtual resolution of the renderer
                    if (renderObject.ResolutionStretch == ResolutionStretch.FixedWidthAdaptableHeight)
                        virtualResolution.Y = virtualResolution.X * targetSize.Y / targetSize.X;
                    if (renderObject.ResolutionStretch == ResolutionStretch.FixedHeightAdaptableWidth)
                        virtualResolution.X = virtualResolution.Y * targetSize.X / targetSize.Y;

                    uiElementState.Update(renderObject, virtualResolution);
                }
                else
                {
                    var cameraComponent = context.RenderContext.Tags.Get(CameraComponentRendererExtensions.Current);
                    if (cameraComponent != null)
                        uiElementState.Update(renderObject, cameraComponent);
                }

                uiElementState.VirtualResolution = virtualResolution;

                uiSystem.RecordPickingTarget(new UIPickingTarget
                {
                    RenderObject = renderObject,
                    View = renderView,
                    WorldViewProjection = uiElementState.WorldViewProjectionMatrix,
                    VirtualResolution = virtualResolution,
                    Viewport = context.CommandList.Viewport,
                    BackBufferSize = backBufferSize,
                    Frame = drawTime.FrameCount,
                });
            }

            // render the UI elements of all the entities
            foreach (var uiElementState in uiElementStates)
            {
                var renderObject = uiElementState.RenderObject;
                var rootElement = renderObject.Page?.RootElement;
                if (rootElement == null)
                    continue;

                var updatableRootElement = (IUIElementUpdate)rootElement;
                var virtualResolution = uiElementState.VirtualResolution;

                // update the rendering context values specific to this element
                renderingContext.Resolution = virtualResolution;
                renderingContext.ViewProjectionMatrix = uiElementState.WorldViewProjectionMatrix;
                renderingContext.DepthStencilBuffer = renderObject.IsFullScreen ? scopedDepthBuffer : context.CommandList.DepthStencilBuffer;
                renderingContext.ShouldSnapText = renderObject.SnapText && renderObject.IsBillboard && (renderObject.IsFixedSize || renderObject.IsFullScreen);

                // calculate an estimate of the UI real size by projecting the element virtual resolution on the screen
                var virtualOrigin = uiElementState.WorldViewProjectionMatrix.W;
                var virtualWidth = new Vector4(virtualResolution.X / 2, 0, 0, 1);
                var virtualHeight = new Vector4(0, virtualResolution.Y / 2, 0, 1);
                var transformedVirtualWidth = Vector4.Transform(virtualWidth, uiElementState.WorldViewProjectionMatrix);
                var transformedVirtualHeight = Vector4.Transform(virtualHeight, uiElementState.WorldViewProjectionMatrix);

                var viewportSize = context.CommandList.Viewport.Size;
                var projectedOrigin = virtualOrigin.XY() / virtualOrigin.W;
                var projectedVirtualWidth = viewportSize * (transformedVirtualWidth.XY() / transformedVirtualWidth.W - projectedOrigin);
                var projectedVirtualHeight = viewportSize * (transformedVirtualHeight.XY() / transformedVirtualHeight.W - projectedOrigin);

                // set default resource dictionary

                // update layouting context.
                layoutingContext.VirtualResolution = virtualResolution;
                layoutingContext.RealResolution = viewportSize;
                layoutingContext.RealVirtualResolutionRatio = new Vector2(projectedVirtualWidth.Length() / virtualResolution.X, projectedVirtualHeight.Length() / virtualResolution.Y);
                rootElement.LayoutingContext = layoutingContext;

                // perform the time-based updates of the UI element
                updatableRootElement.Update(drawTime);

                // update the UI element disposition
                rootElement.Measure(virtualResolution);
                rootElement.Arrange(virtualResolution, false);

                // update the UI element hierarchical properties
                var rootMatrix = Matrix4x4.CreateTranslation(-virtualResolution / 2); // UI world is translated by a half resolution compared to its quad, which is centered around the origin
                updatableRootElement.UpdateWorldMatrix(ref rootMatrix, rootMatrix != uiElementState.RenderObject.LastRootMatrix);
                updatableRootElement.UpdateElementState(0);
                uiElementState.RenderObject.LastRootMatrix = rootMatrix;

                // clear and set the Depth buffer as required
                if (renderObject.IsFullScreen)
                {
                    context.CommandList.Clear(renderingContext.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
                }
                context.CommandList.SetRenderTarget(renderingContext.DepthStencilBuffer, renderingContext.RenderTarget);

                var samplerState = context.GraphicsDevice.SamplerStates.LinearClamp;
                if (renderObject.Sampler != UIElementSampler.LinearClamp)
                {
                    switch (renderObject.Sampler)
                    {
                        case UIElementSampler.PointClamp:
                            samplerState = context.GraphicsDevice.SamplerStates.PointClamp;
                            break;
                        case UIElementSampler.AnisotropicClamp:
                            samplerState = context.GraphicsDevice.SamplerStates.AnisotropicClamp;
                            break;
                    }
                }

                // start the image draw session
                renderingContext.StencilTestReferenceValue = 0;
                batch.Begin(context.GraphicsContext, ref uiElementState.WorldViewProjectionMatrix, BlendStates.AlphaBlend, samplerState, null, uiSystem.KeepStencilValueState, renderingContext.StencilTestReferenceValue);

                // Render the UI elements in the final render target
                RecursiveDrawWithClipping(context, rootElement, ref uiElementState.WorldViewProjectionMatrix, samplerState);

                // end the image draw session
                batch.End();
            }

            // Revert the Depth-Stencil buffer to the default value
            context.CommandList.SetRenderTargets(context.CommandList.DepthStencilBuffer, context.CommandList.RenderTargets);

            // Release scroped texture
            if (scopedDepthBuffer != null)
            {
                context.RenderContext.Allocator.ReleaseReference(scopedDepthBuffer);
            }
        }

        private void RecursiveDrawWithClipping(RenderDrawContext context, UIElement element, ref Matrix4x4 worldViewProj, SamplerState samplerState)
        {
            // if the element is not visible, we also remove all its children
            if (!element.IsVisible)
                return;

            var renderer = rendererManager.GetRenderer(element);
            renderingContext.DepthBias = element.DepthBias;

            // render the clipping region of the element
            if (element.ClipToBounds)
            {
                // flush current elements
                batch.End();

                // render the clipping region
                batch.Begin(context.GraphicsContext, ref worldViewProj, BlendStates.ColorDisabled, samplerState, null, uiSystem.IncreaseStencilValueState, renderingContext.StencilTestReferenceValue);
                renderer.RenderClipping(element, renderingContext);
                batch.End();

                // update context and restart the batch
                renderingContext.StencilTestReferenceValue += 1;
                batch.Begin(context.GraphicsContext, ref worldViewProj, BlendStates.AlphaBlend, samplerState, null, uiSystem.KeepStencilValueState, renderingContext.StencilTestReferenceValue);
            }

            // render the design of the element
            renderer.RenderColor(element, renderingContext);

            // render the children
            foreach (var child in element.VisualChildrenCollection)
                RecursiveDrawWithClipping(context, child, ref worldViewProj, samplerState);

            // clear the element clipping region from the stencil buffer
            if (element.ClipToBounds)
            {
                // flush current elements
                batch.End();

                renderingContext.DepthBias = element.MaxChildrenDepthBias;

                // render the clipping region
                batch.Begin(context.GraphicsContext, ref worldViewProj, BlendStates.ColorDisabled, samplerState, null, uiSystem.DecreaseStencilValueState, renderingContext.StencilTestReferenceValue);
                renderer.RenderClipping(element, renderingContext);
                batch.End();

                // update context and restart the batch
                renderingContext.StencilTestReferenceValue -= 1;
                batch.Begin(context.GraphicsContext, ref worldViewProj, BlendStates.AlphaBlend, samplerState, null, uiSystem.KeepStencilValueState, renderingContext.StencilTestReferenceValue);
            }
        }

        public ElementRenderer GetRenderer(UIElement element)
        {
            return rendererManager.GetRenderer(element);
        }

        public void RegisterRendererFactory(Type uiElementType, IElementRendererFactory factory)
        {
            rendererManager.RegisterRendererFactory(uiElementType, factory);
        }

        public void RegisterRenderer(UIElement element, ElementRenderer renderer)
        {
            rendererManager.RegisterRenderer(element, renderer);
        }

        private class UIElementState
        {
            public readonly RenderUIElement RenderObject;
            public Matrix4x4 WorldViewProjectionMatrix;
            public Vector3 VirtualResolution;

            public UIElementState(RenderUIElement renderObject)
            {
                RenderObject = renderObject;
                WorldViewProjectionMatrix = Matrix4x4.Identity;
                VirtualResolution = renderObject.Resolution;
            }

            public void Update(RenderUIElement renderObject, CameraComponent camera)
            {
                var frustumHeight = 2 * MathF.Tan(MathUtil.DegreesToRadians(camera.VerticalFieldOfView) / 2);

                var worldMatrix = renderObject.WorldMatrix;

                // rotate the UI element perpendicular to the camera view vector, if billboard is activated
                if (renderObject.IsFullScreen)
                {
                    worldMatrix = Matrix4x4.Identity;
                }
                else
                {
                    Matrix4x4 viewInverse;
                    Matrix4x4.InvertSafe(camera.ViewMatrix, out viewInverse);
                    var forwardVector = viewInverse.Forward;

                    if (renderObject.IsBillboard)
                    {
                        var viewInverseRow1 = viewInverse.X;
                        var viewInverseRow2 = viewInverse.Y;

                        // remove scale of the camera
                        viewInverseRow1 /= viewInverseRow1.XYZ().Length();
                        viewInverseRow2 /= viewInverseRow2.XYZ().Length();

                        // set the scale of the object
                        viewInverseRow1 *= worldMatrix.X.XYZ().Length();
                        viewInverseRow2 *= worldMatrix.Y.XYZ().Length();

                        // set the adjusted world matrix
                        worldMatrix.X = viewInverseRow1;
                        worldMatrix.Y = viewInverseRow2;
                        worldMatrix.Z = viewInverse.Z;
                    }

                    if (renderObject.IsFixedSize)
                    {
                        forwardVector = Vector3.NormalizeSafe(forwardVector);
                        var distVec = (worldMatrix.Translation - viewInverse.Translation);
                        float distScalar;
                        distScalar = Vector3.Dot(forwardVector, distVec);
                        distScalar = Math.Abs(distScalar);

                        var worldScale = frustumHeight * distScalar * UIComponent.FixedSizeVerticalUnit; // FrustumHeight already is 2*Tan(FOV/2)

                        worldMatrix.X *= worldScale;
                        worldMatrix.Y *= worldScale;
                        worldMatrix.Z *= worldScale;
                    }

                    // If the UI component is not drawn fullscreen it should be drawn as a quad with world sizes corresponding to its actual size
                    worldMatrix = Matrix4x4.CreateScale(renderObject.Size / renderObject.Resolution) * worldMatrix;
                }

                // Rotation of Pi along 0x to go from UI space to world space
                worldMatrix.Y = -worldMatrix.Y;
                worldMatrix.Z = -worldMatrix.Z;

                Matrix4x4 worldViewMatrix;
                worldViewMatrix = Matrix4x4.Multiply(worldMatrix, camera.ViewMatrix);
                WorldViewProjectionMatrix = Matrix4x4.Multiply(worldViewMatrix, camera.ProjectionMatrix);
            }

            public void Update(RenderUIElement renderObject, Vector3 virtualResolution)
            {
                var nearPlane = virtualResolution.Z / 2;
                var farPlane = nearPlane + virtualResolution.Z;
                var zOffset = nearPlane + virtualResolution.Z / 2;
                var aspectRatio = virtualResolution.X / virtualResolution.Y;
                var verticalFov = MathF.Atan2(virtualResolution.Y / 2, zOffset) * 2;

                var cameraComponent = new CameraComponent(nearPlane, farPlane)
                {
                    UseCustomAspectRatio = true,
                    AspectRatio = aspectRatio,
                    VerticalFieldOfView = MathUtil.RadiansToDegrees(verticalFov),
                    ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(0, 0, zOffset), Vector3.Zero, Vector3.UnitY),
                    // The camera sees the whole resolution at the distance zOffset: half of it at the near plane, which is half as far
                    ProjectionMatrix = Matrix4x4.CreatePerspective(virtualResolution.X / 2, virtualResolution.Y / 2, nearPlane, farPlane),
                };

                Update(renderObject, cameraComponent);
            }
        }
    }
}
