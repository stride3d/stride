// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Core.Storage;
using Xenko.Graphics;
using Xenko.Rendering.Images;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Shadows;
using Xenko.Rendering.SubsurfaceScattering;
using Xenko.VirtualReality;

namespace Xenko.Rendering.Compositing
{
    /// <summary>
    /// Renders your game. It should use current <see cref="RenderContext.RenderView"/> and <see cref="CameraComponentRendererExtensions.GetCurrentCamera"/>.
    /// </summary>
    [Display("Forward renderer")]
    public partial class ForwardRenderer : SceneRendererBase, ISharedRenderer
    {
        // TODO: should we use GraphicsDeviceManager.PreferredBackBufferFormat?
        public const PixelFormat DepthBufferFormat = PixelFormat.D24_UNorm_S8_UInt;

        private IShadowMapRenderer shadowMapRenderer;
        private Texture depthStencilROCached;
        private MultisampleCount actualMultisampleCount = MultisampleCount.None;
        private VRDeviceSystem vrSystem;

        private readonly Logger logger = GlobalLogger.GetLogger(nameof(ForwardRenderer));

        private readonly FastList<Texture> currentRenderTargets = new FastList<Texture>();
        private readonly FastList<Texture> currentRenderTargetsNonMSAA = new FastList<Texture>();
        private Texture currentDepthStencil;
        private Texture currentDepthStencilNonMSAA;

        protected Texture viewOutputTarget;
        protected Texture viewDepthStencil;

        protected int ViewCount { get; private set; }

        protected int ViewIndex { get; private set; }

        public ClearRenderer Clear { get; set; } = new ClearRenderer();

        /// <summary>
        /// Enable Light Probe.
        /// </summary>
        public bool LightProbes { get; set; } = true;

        /// <summary>
        /// The main render stage for opaque geometry.
        /// </summary>
        public RenderStage OpaqueRenderStage { get; set; }

        /// <summary>
        /// The transparent render stage for transparent geometry.
        /// </summary>
        public RenderStage TransparentRenderStage { get; set; }

        /// <summary>
        /// The shadow map render stages for shadow casters. No shadow rendering will happen if null.
        /// </summary>
        [MemberCollection(NotNullItems = true)]
        public List<RenderStage> ShadowMapRenderStages { get; } = new List<RenderStage>();

        /// <summary>
        /// The G-Buffer render stage to render depth buffer and possibly some other extra info to buffers (i.e. normals)
        /// </summary>
        public RenderStage GBufferRenderStage { get; set; }

        /// <summary>
        /// The post effects renderer.
        /// </summary>
        public IPostProcessingEffects PostEffects { get; set; }

        /// <summary>
        /// Light shafts effect
        /// </summary>
        public LightShafts LightShafts { get; set; }

        /// <summary>
        /// Virtual Reality related settings
        /// </summary>
        public VRRendererSettings VRSettings { get; set; } = new VRRendererSettings();

        /// <summary>
        /// Separable subsurface scattering effect
        /// </summary>
        public SubsurfaceScatteringBlur SubsurfaceScatteringBlurEffect { get; set; }

        /// <summary>
        /// The level of multi-sampling
        /// </summary>
        public MultisampleCount MSAALevel { get; set; } = MultisampleCount.None;

        /// <summary>
        /// MSAA Resolver is used to resolve multi-sampled render targets into normal render targets
        /// </summary>
        [NotNull]
        public MSAAResolver MSAAResolver { get; } = new MSAAResolver();

        /// <summary>
        /// If true, depth buffer generated during <see cref="OpaqueRenderStage"/> will be available as a shader resource named DepthBase.DepthStencil during <see cref="TransparentRenderStage"/>.
        /// </summary>
        /// <remarks>
        /// This is needed by some effects such as particles soft edges.
        /// 
        /// On recent platforms that can bind depth buffer as read-only (<see cref="GraphicsDeviceFeatures.HasDepthAsReadOnlyRT"/>), depth buffer will be used as is. Otherwise, a copy will be generated.
        /// </remarks>
        [DefaultValue(true)]
        public bool BindDepthAsResourceDuringTransparentRendering { get; set; } = true;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            shadowMapRenderer = Context.RenderSystem.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()?.RenderFeatures.OfType<ForwardLightingRenderFeature>().FirstOrDefault()?.ShadowMapRenderer;

            if (MSAALevel != MultisampleCount.None)
            {
                actualMultisampleCount = (MultisampleCount)Math.Min((int)MSAALevel, (int)GraphicsDevice.Features[PixelFormat.R16G16B16A16_Float].MultisampleCountMax);
                actualMultisampleCount = (MultisampleCount)Math.Min((int)actualMultisampleCount, (int)GraphicsDevice.Features[DepthBufferFormat].MultisampleCountMax);

                // Note: we cannot support MSAA on DX10 now
                if (GraphicsDevice.Features.HasMultisampleDepthAsSRV == false && // TODO: Try enabling MSAA on DX9!
                    GraphicsDevice.Platform != GraphicsPlatform.OpenGL &&
                    GraphicsDevice.Platform != GraphicsPlatform.OpenGLES)
                {
                    // OpenGL has MSAA support on every version.
                    // OpenGL ES has MSAA support starting from version 3.0.
                    // Direct3D has MSAA support starting from version 11 because it requires multisample depth buffers as shader resource views.
                    // Therefore we force-disable MSAA on any platform that doesn't support MSAA.

                    actualMultisampleCount = MultisampleCount.None;
                }

                if (actualMultisampleCount != MSAALevel)
                {
                    logger.Warning("Multisample count of " + (int)MSAALevel + " samples not supported. Falling back to highest supported sample count of " + (int)actualMultisampleCount + " samples.");
                }

#if XENKO_PLATFORM_IOS
                // MSAA is not supported on iOS currently because OpenTK doesn't expose "GL.BlitFramebuffer()" on iOS for some reason.
                actualMultisampleCount = MultisampleCount.None;
#endif
            }

            var camera = Context.GetCurrentCamera();

            vrSystem = Services.GetService<VRDeviceSystem>();
            if (vrSystem != null)
            {
                if (VRSettings.Enabled)
                {
                    var requiredDescs = VRSettings.RequiredApis.ToArray();
                    vrSystem.PreferredApis = requiredDescs.Select(x => x.Api).Distinct().ToArray();

                    // remove VR API duplicates and keep first desired config only
                    var preferredScalings = new Dictionary<VRApi, float>();
                    foreach (var desc in requiredDescs)
                    {
                        if (!preferredScalings.ContainsKey(desc.Api))
                            preferredScalings[desc.Api] = desc.ResolutionScale;
                    }
                    vrSystem.PreferredScalings = preferredScalings;

                    vrSystem.RequireMirror = VRSettings.CopyMirror;
                    vrSystem.MirrorWidth = GraphicsDevice.Presenter.BackBuffer.Width;
                    vrSystem.MirrorHeight = GraphicsDevice.Presenter.BackBuffer.Height;

                    vrSystem.Enabled = true; //careful this will trigger the whole chain of initialization!
                    vrSystem.Visible = true;

                    VRSettings.VRDevice = vrSystem.Device;

                    vrSystem.PreviousUseCustomProjectionMatrix = camera.UseCustomProjectionMatrix;
                    vrSystem.PreviousUseCustomViewMatrix = camera.UseCustomViewMatrix;
                    vrSystem.PreviousCameraProjection = camera.ProjectionMatrix;

                    if (VRSettings.VRDevice.SupportsOverlays)
                    {
                        foreach (var overlay in VRSettings.Overlays)
                        {
                            if (overlay != null && overlay.Texture != null)
                            {
                                overlay.Overlay = VRSettings.VRDevice.CreateOverlay(overlay.Texture.Width, overlay.Texture.Height, overlay.Texture.MipLevels, (int)overlay.Texture.MultisampleCount);
                            }
                        }
                    }
                }
                else
                {
                    vrSystem.Enabled = false;
                    vrSystem.Visible = false;

                    VRSettings.VRDevice = null;

                    if (vrSystem.Device != null) //we had a device before so we know we need to restore the camera
                    {
                        camera.UseCustomViewMatrix = vrSystem.PreviousUseCustomViewMatrix;
                        camera.UseCustomProjectionMatrix = vrSystem.PreviousUseCustomProjectionMatrix;
                        camera.ProjectionMatrix = vrSystem.PreviousCameraProjection;
                    }
                }
            }
        }

        protected virtual void CollectStages(RenderContext context)
        {
            if (OpaqueRenderStage != null)
            {
                OpaqueRenderStage.OutputValidator.BeginCustomValidation(context.RenderOutput.DepthStencilFormat, context.RenderOutput.MultisampleCount);
                ValidateOpaqueStageOutput(OpaqueRenderStage.OutputValidator, context);
                OpaqueRenderStage.OutputValidator.EndCustomValidation();
            }

            if (TransparentRenderStage != null)
            {
                TransparentRenderStage.OutputValidator.Validate(ref context.RenderOutput);
            }

            if (GBufferRenderStage != null && LightProbes)
            {
                GBufferRenderStage.Output = new RenderOutputDescription(PixelFormat.None, context.RenderOutput.DepthStencilFormat);
            }
        }

        protected virtual void ValidateOpaqueStageOutput(RenderOutputValidator renderOutputValidator, RenderContext renderContext)
        {
            renderOutputValidator.Add<ColorTargetSemantic>(renderContext.RenderOutput.RenderTargetFormat0);

            if (PostEffects != null)
            {
                if (PostEffects.RequiresNormalBuffer)
                {
#if XENKO_PLATFORM_ANDROID || XENKO_PLATFORM_IOS
                    renderOutputValidator.Add<NormalTargetSemantic>(PixelFormat.R16G16B16A16_Float);
#else
                    renderOutputValidator.Add<NormalTargetSemantic>(PixelFormat.R10G10B10A2_UNorm);
#endif
                }

                if (PostEffects.RequiresSpecularRoughnessBuffer)
                {
                    renderOutputValidator.Add<SpecularColorRoughnessTargetSemantic>(PixelFormat.R8G8B8A8_UNorm);
                }

                if (PostEffects.RequiresVelocityBuffer)
                {
                    renderOutputValidator.Add<VelocityTargetSemantic>(PixelFormat.R16G16_Float);
                }

                if (SubsurfaceScatteringBlurEffect != null)
                {
                    // TODO: This is just a workaround for now, because I can't sample an integer texture in a post process.
                    //       Should use this instead: PixelFormat.R8_UInt
                    renderOutputValidator.Add<MaterialIndexTargetSemantic>(PixelFormat.R16_Float);
                }
            }
        }

        protected virtual void CollectView(RenderContext context)
        {
            // Fill RenderStage formats and register render stages to main view
            if (OpaqueRenderStage != null)
            {
                context.RenderView.RenderStages.Add(OpaqueRenderStage);
            }

            if (TransparentRenderStage != null)
            {
                context.RenderView.RenderStages.Add(TransparentRenderStage);
            }

            if (GBufferRenderStage != null && LightProbes)
            {
                context.RenderView.RenderStages.Add(GBufferRenderStage);
            }
        }

        protected override unsafe void CollectCore(RenderContext context)
        {
            var camera = context.GetCurrentCamera();

            if (context.RenderView == null)
                throw new NullReferenceException(nameof(context.RenderView) + " is null. Please make sure you have your camera correctly set.");

            // Setup pixel formats for RenderStage
            using (context.SaveRenderOutputAndRestore())
            {
                // Mark this view as requiring shadows
                shadowMapRenderer?.RenderViewsWithShadows.Add(context.RenderView);

                context.RenderOutput = new RenderOutputDescription(PostEffects != null ? PixelFormat.R16G16B16A16_Float : context.RenderOutput.RenderTargetFormat0, DepthBufferFormat, MSAALevel);

                CollectStages(context);

                if (VRSettings.Enabled && VRSettings.VRDevice != null)
                {
                    Vector3 cameraPos, cameraScale;
                    Matrix cameraRot;

                    if (!vrSystem.PreviousUseCustomViewMatrix)
                    {
                        camera.Entity.Transform.WorldMatrix.Decompose(out cameraScale, out cameraRot, out cameraPos);
                    }
                    else
                    {
                        camera.ViewMatrix.Decompose(out cameraScale, out cameraRot, out cameraPos);
                        cameraRot.Transpose();
                        Vector3.Negate(ref cameraPos, out cameraPos);
                        Vector3.TransformCoordinate(ref cameraPos, ref cameraRot, out cameraPos);
                    }

                    if (VRSettings.IgnoreCameraRotation)
                    {
                        cameraRot = Matrix.Identity;
                    }

                    // Compute both view and projection matrices
                    Matrix* viewMatrices = stackalloc Matrix[2];
                    Matrix* projectionMatrices = stackalloc Matrix[2];
                    for (var i = 0; i < 2; ++i)
                        VRSettings.VRDevice.ReadEyeParameters(i == 0 ? Eyes.Left : Eyes.Right, camera.NearClipPlane, camera.FarClipPlane, ref cameraPos, ref cameraRot, VRSettings.IgnoreDeviceRotation, VRSettings.IgnoreDevicePosition, out viewMatrices[i], out projectionMatrices[i]);

                    // if the VRDevice disagreed with the near and far plane, we must re-discover them and follow:
                    var near = projectionMatrices[0].M43 / projectionMatrices[0].M33;
                    var far = near * (-projectionMatrices[0].M33 / (-projectionMatrices[0].M33 - 1));
                    if (Math.Abs(near - camera.NearClipPlane) > 1e-8f)
                        camera.NearClipPlane = near;
                    if (Math.Abs(near - camera.FarClipPlane) > 1e-8f)
                        camera.FarClipPlane = far;

                    // Compute a view matrix and projection matrix that cover both eyes for shadow map and culling
                    ComputeCommonViewMatrices(context, viewMatrices, projectionMatrices);
                    var commonView = context.RenderView;

                    // Notify lighting system this view only purpose is for shared lighting, it is not being drawn directly.
                    commonView.Flags |= RenderViewFlags.NotDrawn;

                    // Collect now, and use result for both eyes
                    CollectView(context);
                    context.VisibilityGroup.TryCollect(commonView);

                    for (var i = 0; i < 2; i++)
                    {
                        using (context.PushRenderViewAndRestore(VRSettings.RenderViews[i]))
                        using (context.SaveViewportAndRestore())
                        {
                            context.RenderSystem.Views.Add(context.RenderView);
                            context.RenderView.LightingView = commonView;
                            context.ViewportState.Viewport0 = new Viewport(0, 0, VRSettings.VRDevice.ActualRenderFrameSize.Width / 2.0f, VRSettings.VRDevice.ActualRenderFrameSize.Height);

                            //change camera params for eye
                            camera.ViewMatrix = viewMatrices[i];
                            camera.ProjectionMatrix = projectionMatrices[i];
                            camera.UseCustomProjectionMatrix = true;
                            camera.UseCustomViewMatrix = true;
                            camera.Update();

                            //write params to view
                            SceneCameraRenderer.UpdateCameraToRenderView(context, context.RenderView, camera);

                            // Copy culling results
                            context.VisibilityGroup.Copy(commonView, context.RenderView);

                            CollectView(context);

                            LightShafts?.Collect(context);

                            PostEffects?.Collect(context);
                        }
                    }

                    if (VRSettings.VRDevice.SupportsOverlays)
                    {
                        foreach (var overlay in VRSettings.Overlays)
                        {
                            if (overlay != null && overlay.Texture != null)
                            {
                                overlay.Overlay.Position = overlay.LocalPosition;
                                overlay.Overlay.Rotation = overlay.LocalRotation;
                                overlay.Overlay.SurfaceSize = overlay.SurfaceSize;
                                overlay.Overlay.FollowHeadRotation = overlay.FollowsHeadRotation;
                            }
                        }
                    }
                }
                else
                {
                    //write params to view
                    SceneCameraRenderer.UpdateCameraToRenderView(context, context.RenderView, camera);

                    CollectView(context);

                    LightShafts?.Collect(context);

                    PostEffects?.Collect(context);
                }

                // Set depth format for shadow map render stages
                // TODO: This format should be acquired from the ShadowMapRenderer instead of being fixed here
                foreach (var shadowMapRenderStage in ShadowMapRenderStages)
                {
                    if (shadowMapRenderStage != null)
                        shadowMapRenderStage.Output = new RenderOutputDescription(PixelFormat.None, PixelFormat.D32_Float);
                }
            }

            PostEffects?.Collect(context);
        }

        protected static PixelFormat ComputeNonMSAADepthFormat(PixelFormat format)
        {
            PixelFormat result;

            switch (format)
            {
                case PixelFormat.R16_Float:
                case PixelFormat.R16_Typeless:
                case PixelFormat.D16_UNorm:
                    result = PixelFormat.R16_Float;
                    break;
                case PixelFormat.R32_Float:
                case PixelFormat.R32_Typeless:
                case PixelFormat.D32_Float:
                    result = PixelFormat.R32_Float;
                    break;

                // Note: for those formats we lose stencil buffer information during MSAA -> non-MSAA conversion
                case PixelFormat.R24G8_Typeless:
                case PixelFormat.D24_UNorm_S8_UInt:
                case PixelFormat.R24_UNorm_X8_Typeless:
                    result = PixelFormat.R32_Float;
                    break;
                case PixelFormat.R32G8X24_Typeless:
                case PixelFormat.D32_Float_S8X24_UInt:
                case PixelFormat.R32_Float_X8X24_Typeless:
                    result = PixelFormat.R32_Float;
                    break;

                default:
                    throw new NotSupportedException($"Unsupported depth format [{format}]");
            }

            return result;
        }

        /// <summary>
        /// Resolves the MSAA textures. Converts MSAA currentRenderTargets and currentDepthStencil into currentRenderTargetsNonMSAA and currentDepthStencilNonMSAA.
        /// </summary>
        /// <param name="drawContext">The draw context.</param>
        private void ResolveMSAA(RenderDrawContext drawContext)
        {
            // Resolve render targets
            currentRenderTargetsNonMSAA.Resize(currentRenderTargets.Count, false);
            for (int index = 0; index < currentRenderTargets.Count; index++)
            {
                var input = currentRenderTargets[index];

                var outputDescription = TextureDescription.New2D(input.ViewWidth, input.ViewHeight, 1, input.Format, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                var output = PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(outputDescription));

                currentRenderTargetsNonMSAA[index] = output;
                MSAAResolver.Resolve(drawContext, input, output);
            }

            // Resolve depth buffer
            currentDepthStencilNonMSAA = viewDepthStencil;
            MSAAResolver.Resolve(drawContext, currentDepthStencil, currentDepthStencilNonMSAA);
        }

        protected virtual void DrawView(RenderContext context, RenderDrawContext drawContext, int eyeIndex, int eyeCount)
        {
            var renderSystem = context.RenderSystem;

            PrepareVRConstantBuffer(context, eyeIndex, eyeCount);

            // Z Prepass
            var lightProbes = LightProbes && GBufferRenderStage != null;
            if (lightProbes)
            {
                // Note: Baking lightprobe before GBuffer prepass because we are updating some cbuffer parameters needed by Opaque pass that GBuffer pass might upload early
                PrepareLightprobeConstantBuffer(context);

                // TODO: Temporarily using ShadowMap shader
                using (drawContext.QueryManager.BeginProfile(Color.Green, CompositingProfilingKeys.GBuffer))
                using (drawContext.PushRenderTargetsAndRestore())
                {
                    drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
                    drawContext.CommandList.SetRenderTarget(drawContext.CommandList.DepthStencilBuffer, null);

                    // Draw [main view | z-prepass stage]
                    renderSystem.Draw(drawContext, context.RenderView, GBufferRenderStage);
                }

                // Bake lightprobes against Z-buffer
                BakeLightProbes(context, drawContext);
            }

            using (drawContext.PushRenderTargetsAndRestore())
            {
                // Draw [main view | main stage]
                if (OpaqueRenderStage != null)
                {
                    using (drawContext.QueryManager.BeginProfile(Color.Green, CompositingProfilingKeys.Opaque))
                    {
                        renderSystem.Draw(drawContext, context.RenderView, OpaqueRenderStage);
                    }
                }

                Texture depthStencilSRV = null;

                // Draw [main view | subsurface scattering post process]
                if (SubsurfaceScatteringBlurEffect != null)
                {
                    var materialIndex = OpaqueRenderStage?.OutputValidator.Find<MaterialIndexTargetSemantic>() ?? -1;
                    if (materialIndex != -1)
                    {
                        using (drawContext.PushRenderTargetsAndRestore())
                        {
                            depthStencilSRV = ResolveDepthAsSRV(drawContext);

                            var renderTarget = drawContext.CommandList.RenderTargets[0];
                            var materialIndexRenderTarget = drawContext.CommandList.RenderTargets[materialIndex];

                            SubsurfaceScatteringBlurEffect.Draw(drawContext, renderTarget, materialIndexRenderTarget, depthStencilSRV, renderTarget);
                        }
                    }
                }

                // Draw [main view | transparent stage]
                if (TransparentRenderStage != null)
                {
                    // Some transparent shaders will require the depth as a shader resource - resolve it only once and set it here
                    using (drawContext.QueryManager.BeginProfile(Color.Green, CompositingProfilingKeys.Transparent))
                    using (drawContext.PushRenderTargetsAndRestore())
                    {
                        if (depthStencilSRV == null)
                            depthStencilSRV = ResolveDepthAsSRV(drawContext);

                        renderSystem.Draw(drawContext, context.RenderView, TransparentRenderStage);
                    }
                }

                var colorTargetIndex = OpaqueRenderStage?.OutputValidator.Find(typeof(ColorTargetSemantic)) ?? -1;
                if (colorTargetIndex == -1)
                    return;

                // Resolve MSAA targets
                var renderTargets = currentRenderTargets;
                var depthStencil = currentDepthStencil;
                if (actualMultisampleCount != MultisampleCount.None)
                {
                    using (drawContext.QueryManager.BeginProfile(Color.Green, CompositingProfilingKeys.MsaaResolve))
                    {
                        ResolveMSAA(drawContext);
                    }

                    renderTargets = currentRenderTargetsNonMSAA;
                    depthStencil = currentDepthStencilNonMSAA;
                }

                // Shafts if we have them
                if (LightShafts != null)
                {
                    using (drawContext.QueryManager.BeginProfile(Color.Green, CompositingProfilingKeys.LightShafts))
                    {
                        LightShafts.Draw(drawContext, depthStencil, renderTargets[colorTargetIndex]);
                    }
                }

                if (PostEffects != null)
                {
                    // Run post effects
                    // Note: OpaqueRenderStage can't be null otherwise colorTargetIndex would be -1
                    PostEffects.Draw(drawContext, OpaqueRenderStage.OutputValidator, renderTargets.Items, depthStencil, viewOutputTarget);
                }
                else
                {
                    if (actualMultisampleCount != MultisampleCount.None)
                    {
                        using (drawContext.QueryManager.BeginProfile(Color.Green, CompositingProfilingKeys.MsaaResolve))
                        {
                            drawContext.CommandList.Copy(renderTargets[colorTargetIndex], viewOutputTarget);
                        }
                    }
                }

                // Free the depth texture since we won't need it anymore
                if (depthStencilSRV != null)
                {
                    drawContext.Resolver.ReleaseDepthStenctilAsShaderResource(depthStencilSRV);
                }
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var viewport = drawContext.CommandList.Viewport;

            using (drawContext.PushRenderTargetsAndRestore())
            {
                // Render Shadow maps
                shadowMapRenderer?.Draw(drawContext);

                if (VRSettings.Enabled && VRSettings.VRDevice != null)
                {
                    var isFullViewport = (int)viewport.X == 0 && (int)viewport.Y == 0
                                         && (int)viewport.Width == drawContext.CommandList.RenderTarget.ViewWidth
                                         && (int)viewport.Height == drawContext.CommandList.RenderTarget.ViewHeight;
                    if (!isFullViewport)
                        return;

                    var hasPostEffects = PostEffects != null; // When we have post effect we need to bind a different framebuffer for each view to be sure effects impinge on the other view.

                    Texture vrFullSurface;
                    using (drawContext.PushRenderTargetsAndRestore())
                    {
                        var currentRenderTarget = drawContext.CommandList.RenderTarget;
                        var vrFullFrameSize = VRSettings.VRDevice.ActualRenderFrameSize;
                        var desiredRenderTargetSize = !hasPostEffects ? vrFullFrameSize : new Size2(vrFullFrameSize.Width / 2, vrFullFrameSize.Height);
                        if (hasPostEffects || desiredRenderTargetSize.Width != currentRenderTarget.Width || desiredRenderTargetSize.Height != currentRenderTarget.Height)
                            drawContext.CommandList.SetRenderTargets(null, null); // force to create and bind a new render target

                        PrepareRenderTargets(drawContext, desiredRenderTargetSize);

                        //prepare the final VR target
                        vrFullSurface = viewOutputTarget;
                        if (hasPostEffects)
                        {
                            var frameSize = VRSettings.VRDevice.ActualRenderFrameSize;
                            var renderTargetDescription = TextureDescription.New2D(frameSize.Width, frameSize.Height, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                            vrFullSurface = PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(renderTargetDescription));
                        }

                        //draw per eye
                        using (context.SaveViewportAndRestore())
                        using (drawContext.PushRenderTargetsAndRestore())
                        {
                            ViewCount = 2;
                            bool isWindowsMixedReality = false;

                            for (var i = 0; i < 2; i++)
                            {
#if XENKO_PLATFORM_UWP
                                if (GraphicsDevice.Platform == GraphicsPlatform.Direct3D11 && drawContext.GraphicsDevice.Presenter is WindowsMixedRealityGraphicsPresenter graphicsPresenter)
                                {
                                    isWindowsMixedReality = true;

                                    MSAALevel = MultisampleCount.None;
                                    currentRenderTargets.Clear();

                                    if (i == 0)
                                    {
                                        currentRenderTargets.Add(graphicsPresenter.LeftEyeBuffer);
                                    }
                                    else
                                    {
                                        currentRenderTargets.Add(graphicsPresenter.RightEyeBuffer);
                                    }
                                }
#endif

                                drawContext.CommandList.SetRenderTargets(currentDepthStencil, currentRenderTargets.Count, currentRenderTargets.Items);

                                if (!hasPostEffects && !isWindowsMixedReality) // need to change the viewport between each eye
                                {
                                    var frameSize = VRSettings.VRDevice.ActualRenderFrameSize;
                                    drawContext.CommandList.SetViewport(new Viewport(i * frameSize.Width / 2, 0, frameSize.Width / 2, frameSize.Height));
                                }
                                else if (i == 0) // the viewport is the same for both eyes so we set it only once
                                {
                                    drawContext.CommandList.SetViewport(new Viewport(0.0f, 0.0f, VRSettings.VRDevice.ActualRenderFrameSize.Width / 2.0f, VRSettings.VRDevice.ActualRenderFrameSize.Height));
                                }

                                using (context.PushRenderViewAndRestore(VRSettings.RenderViews[i]))
                                {
                                    // Clear render target and depth stencil
                                    if (hasPostEffects || i == 0) // need to clear for each eye in the case we have two different render targets
                                        Clear?.Draw(drawContext);

                                    ViewIndex = i;

                                    DrawView(context, drawContext, i, 2);

                                    if (hasPostEffects) // copy the rendered view into the vr full view framebuffer
                                        drawContext.CommandList.CopyRegion(viewOutputTarget, 0, null, vrFullSurface, 0, VRSettings.VRDevice.ActualRenderFrameSize.Width / 2 * i);
                                }
                            }

                            if (VRSettings.VRDevice.SupportsOverlays)
                            {
                                foreach (var overlay in VRSettings.Overlays)
                                {
                                    if (overlay != null && overlay.Texture != null)
                                    {
                                        overlay.Overlay.UpdateSurface(drawContext.CommandList, overlay.Texture);
                                    }
                                }
                            }

                            VRSettings.VRDevice.Commit(drawContext.CommandList, vrFullSurface);
                        }
                    }

                    //draw mirror to backbuffer (if size is matching and full viewport)
                    if (VRSettings.CopyMirror)
                    {
                        CopyOrScaleTexture(drawContext, VRSettings.VRDevice.MirrorTexture, drawContext.CommandList.RenderTarget);
                    }
                    else if (hasPostEffects)
                    {
                        CopyOrScaleTexture(drawContext, vrFullSurface, drawContext.CommandList.RenderTarget);
                    }
                }
                else
                {
                    PrepareRenderTargets(drawContext, new Size2((int)viewport.Width, (int)viewport.Height));

                    ViewCount = 1;
                    ViewIndex = 0;

                    //var sssMaterialIndexRenderTarget = GenerateSSSMaterialIndexRenderTarget(context, viewport);

                    using (drawContext.PushRenderTargetsAndRestore())
                    {
                        drawContext.CommandList.SetRenderTargets(currentDepthStencil, currentRenderTargets.Count, currentRenderTargets.Items);

                        // Clear render target and depth stencil
                        Clear?.Draw(drawContext);

                        DrawView(context, drawContext, 0, 1);
                    }
                }
            }

            // Clear intermediate results
            currentRenderTargets.Clear();
            currentRenderTargetsNonMSAA.Clear();
            currentDepthStencil = null;
            currentDepthStencilNonMSAA = null;
        }

        private void CopyOrScaleTexture(RenderDrawContext drawContext, Texture input, Texture output)
        {
            if (input.Size != output.Size)
            {
                VRSettings.MirrorScaler.SetInput(0, input);
                VRSettings.MirrorScaler.SetOutput(output);
                VRSettings.MirrorScaler.Draw(drawContext);
            }
            else
            {
                drawContext.CommandList.Copy(input, output);
            }
        }

        private Texture ResolveDepthAsSRV(RenderDrawContext context)
        {
            if (!BindDepthAsResourceDuringTransparentRendering)
                return null;

            var depthStencil = context.CommandList.DepthStencilBuffer;
            var depthStencilSRV = context.Resolver.ResolveDepthStencil(context.CommandList.DepthStencilBuffer);

            var renderView = context.RenderContext.RenderView;

            foreach (var renderFeature in context.RenderContext.RenderSystem.RenderFeatures)
            {
                if (!(renderFeature is RootEffectRenderFeature))
                    continue;

                var depthLogicalKey = ((RootEffectRenderFeature)renderFeature).CreateViewLogicalGroup("Depth");
                var viewFeature = renderView.Features[renderFeature.Index];

                // Copy ViewProjection to PerFrame cbuffer
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                    var depthLogicalGroup = viewLayout.GetLogicalGroup(depthLogicalKey);
                    if (depthLogicalGroup.Hash == ObjectId.Empty)
                        continue;

                    // Might want to use ProcessLogicalGroup if more than 1 Recource
                    resourceGroup.DescriptorSet.SetShaderResourceView(depthLogicalGroup.DescriptorSlotStart, depthStencilSRV);
                }
            }

            context.CommandList.SetRenderTargets(null, context.CommandList.RenderTargetCount, context.CommandList.RenderTargets);

            depthStencilROCached = context.Resolver.GetDepthStencilAsRenderTarget(depthStencil, depthStencilROCached);
            context.CommandList.SetRenderTargets(depthStencilROCached, context.CommandList.RenderTargetCount, context.CommandList.RenderTargets);

            return depthStencilSRV;
        }

        private void PrepareRenderTargets(RenderDrawContext drawContext, Texture outputRenderTarget, Texture outputDepthStencil)
        {
            if (OpaqueRenderStage == null)
                return;

            var renderTargets = OpaqueRenderStage.OutputValidator.RenderTargets;

            currentRenderTargets.Resize(renderTargets.Count, false);

            for (int index = 0; index < renderTargets.Count; index++)
            {
                if (renderTargets[index].Semantic is ColorTargetSemantic && PostEffects == null && actualMultisampleCount == MultisampleCount.None)
                {
                    currentRenderTargets[index] = outputRenderTarget;
                }
                else
                {
                    var description = renderTargets[index];
                    var textureDescription = TextureDescription.New2D(outputRenderTarget.Width, outputRenderTarget.Height, 1, description.Format, TextureFlags.RenderTarget | TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Default, actualMultisampleCount);
                    currentRenderTargets[index] = PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(textureDescription));
                }

                drawContext.CommandList.ResourceBarrierTransition(currentRenderTargets[index], GraphicsResourceState.RenderTarget);
            }

            // Prepare depth buffer
            if (actualMultisampleCount == MultisampleCount.None)
            {
                currentDepthStencil = outputDepthStencil;
            }
            else
            {
                var description = outputDepthStencil.Description;
                var textureDescription = TextureDescription.New2D(description.Width, description.Height, 1, description.Format, TextureFlags.DepthStencil | TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Default, actualMultisampleCount);
                currentDepthStencil = PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(textureDescription));
            }
            drawContext.CommandList.ResourceBarrierTransition(currentDepthStencil, GraphicsResourceState.DepthWrite);
        }

        /// <summary>
        /// Prepares targets per frame, caching and handling MSAA etc.
        /// </summary>
        /// <param name="drawContext">The current draw context</param>
        /// <param name="renderTargetsSize">The render target size</param>
        protected virtual void PrepareRenderTargets(RenderDrawContext drawContext, Size2 renderTargetsSize)
        {
            viewOutputTarget = drawContext.CommandList.RenderTarget;
            if (drawContext.CommandList.RenderTargetCount == 0)
                viewOutputTarget = null;
            viewDepthStencil = drawContext.CommandList.DepthStencilBuffer;

            // Create output if needed
            if (viewOutputTarget == null || viewOutputTarget.MultisampleCount != MultisampleCount.None)
            {
                viewOutputTarget = PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(
                    TextureDescription.New2D(renderTargetsSize.Width, renderTargetsSize.Height, 1, PixelFormat.R8G8B8A8_UNorm_SRgb,
                        TextureFlags.ShaderResource | TextureFlags.RenderTarget)));
            }

            // Create depth if needed
            if (viewDepthStencil == null || viewDepthStencil.MultisampleCount != MultisampleCount.None)
            {
                viewDepthStencil = PushScopedResource(drawContext.GraphicsContext.Allocator.GetTemporaryTexture2D(
                    TextureDescription.New2D(renderTargetsSize.Width, renderTargetsSize.Height, 1, DepthBufferFormat,
                        TextureFlags.ShaderResource | TextureFlags.DepthStencil)));
            }

            PrepareRenderTargets(drawContext, viewOutputTarget, viewDepthStencil);
        }

        protected override void Destroy()
        {
            PostEffects?.Dispose();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PerViewVR
        {
            public int EyeIndex;
            public int EyeCount;
        }

        private unsafe void PrepareVRConstantBuffer(RenderContext context, int eyeIndex, int eyeCount)
        {
            foreach (var renderFeature in context.RenderSystem.RenderFeatures)
            {
                if (!(renderFeature is RootEffectRenderFeature))
                    continue;

                var renderView = context.RenderView;
                var logicalKey = ((RootEffectRenderFeature)renderFeature).CreateViewLogicalGroup("GlobalVR");
                var viewFeature = renderView.Features[renderFeature.Index];

                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var resourceGroup = viewLayout.Entries[renderView.Index].Resources;

                    var logicalGroup = viewLayout.GetLogicalGroup(logicalKey);
                    if (logicalGroup.Hash == ObjectId.Empty)
                        continue;

                    var mappedCB = (PerViewVR*)(resourceGroup.ConstantBuffer.Data + logicalGroup.ConstantBufferOffset);
                    mappedCB->EyeIndex = eyeIndex;
                    mappedCB->EyeCount = eyeCount;
                }
            }
        }
    }
}
