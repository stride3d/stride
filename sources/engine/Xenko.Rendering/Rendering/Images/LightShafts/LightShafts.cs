// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Extensions;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Images
{
    [DataContract("LightShafts")]
    public class LightShafts : ImageEffect
    {
        /// <summary>
        /// Property key to access the current collection of <see cref="List{RenderLightShaft}"/> from <see cref="VisibilityGroup.Tags"/>.
        /// </summary>
        [DataMemberIgnore]
        public static readonly PropertyKey<List<RenderLightShaft>> CurrentLightShafts = new PropertyKey<List<RenderLightShaft>>("LightShafts.CurrentLightShafts", typeof(LightShafts));

        /// <summary>
        /// The number of times the resolution is lowered for the light buffer
        /// </summary>
        /// <userdoc>
        /// Lower values produce more precise volume buffer areas, but use more GPU
        /// </userdoc>
        [DataMemberRange(1, 64, 1, 1, 0)]
        public int LightBufferDownsampleLevel { get; set; } = 2;

        /// <summary>
        /// The amount of time the resolution is lowered for the bounding volume buffer
        /// </summary>
        /// <userdoc>
        /// Lower values produce sharper light shafts, but use more GPU
        /// </userdoc>
        [DataMemberRange(1, 64, 1, 1, 0)]
        public int BoundingVolumeBufferDownsampleLevel { get; set; } = 8;
        
        /// <summary>
        /// Size of the orthographic projection used to find minimum bounding volume distance behind the camera
        /// </summary>
        private const float BackSideOrthographicSize = 0.0001f;

        private ImageEffectShader lightShaftsEffectShader;

        private ImageEffectShader applyLightEffectShader;
        private DynamicEffectInstance minmaxVolumeEffectShader;
        private GaussianBlur blur;

        private IShadowMapRenderer shadowMapRenderer;
        private List<RenderLightShaft> lightShafts;

        private MutablePipelineState[] minmaxPipelineStates = new MutablePipelineState[2];
        private EffectBytecode previousMinmaxEffectBytecode;

        private RenderLightShaftBoundingVolume[] singleBoundingVolume = new RenderLightShaftBoundingVolume[1];

        // This could be used at some point when we have colored shadows
        private bool needsColorLightBuffer = true;

        private int usageCounter = 0;

        private Dictionary<IDirectLight, LightShaftRenderData> renderData = new Dictionary<IDirectLight, LightShaftRenderData>();
        private List<IDirectLight> unusedLights = new List<IDirectLight>();

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // Light accumulation shader
            lightShaftsEffectShader = ToLoadAndUnload(new ImageEffectShader("LightShaftsEffect"));

            // Additive blending shader
            applyLightEffectShader = ToLoadAndUnload(new ImageEffectShader("AdditiveLightEffect"));
            applyLightEffectShader.BlendState = new BlendStateDescription(Blend.One, Blend.One);

            minmaxVolumeEffectShader = new DynamicEffectInstance("VolumeMinMaxShader");
            minmaxVolumeEffectShader.Initialize(Context.Services);

            blur = ToLoadAndUnload(new GaussianBlur());

            // Need the shadow map renderer in order to render light shafts
            var meshRenderFeature = Context.RenderSystem.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault();
            if (meshRenderFeature == null)
                throw new InvalidOperationException("Missing mesh render feature");
            
            var forwardLightingFeature = meshRenderFeature.RenderFeatures.OfType<ForwardLightingRenderFeature>().FirstOrDefault();
            if (forwardLightingFeature == null)
                throw new InvalidOperationException("Missing forward lighting render feature");

            shadowMapRenderer = forwardLightingFeature.ShadowMapRenderer;

            for (int i = 0; i < 2; ++i)
            {
                var minmaxPipelineState = new MutablePipelineState(Context.GraphicsDevice);
                minmaxPipelineState.State.SetDefaults();

                minmaxPipelineState.State.BlendState.RenderTarget0.BlendEnable = true;
                minmaxPipelineState.State.BlendState.RenderTarget0.ColorSourceBlend = Blend.One;
                minmaxPipelineState.State.BlendState.RenderTarget0.ColorDestinationBlend = Blend.One;
                minmaxPipelineState.State.BlendState.RenderTarget0.ColorBlendFunction = i == 0 ? BlendFunction.Min : BlendFunction.Max;
                minmaxPipelineState.State.BlendState.RenderTarget0.ColorWriteChannels = i == 0 ? ColorWriteChannels.Red : ColorWriteChannels.Green;

                minmaxPipelineState.State.DepthStencilState.DepthBufferEnable = false;
                minmaxPipelineState.State.DepthStencilState.DepthBufferWriteEnable = false;
                minmaxPipelineState.State.RasterizerState.DepthClipEnable = true;
                minmaxPipelineState.State.RasterizerState.CullMode = i == 0 ? CullMode.Back : CullMode.Front;

                minmaxPipelineStates[i] = minmaxPipelineState;
            }
        }

        protected override void Destroy()
        {
            base.Destroy();
            minmaxVolumeEffectShader.Dispose();
        }

        public void Collect(RenderContext context)
        {
            lightShafts = context.VisibilityGroup.Tags.Get(CurrentLightShafts);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            if (lightShafts == null)
                return; // Not collected

            if (LightBufferDownsampleLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(LightBufferDownsampleLevel));
            if (BoundingVolumeBufferDownsampleLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(BoundingVolumeBufferDownsampleLevel));

            var depthInput = GetSafeInput(0);

            // Create a min/max buffer generated from scene bounding volumes
            var targetBoundingBoxBufferSize = new Size2(Math.Max(1, depthInput.Width / BoundingVolumeBufferDownsampleLevel), Math.Max(1, depthInput.Height / BoundingVolumeBufferDownsampleLevel));
            var boundingBoxBuffer = NewScopedRenderTarget2D(targetBoundingBoxBufferSize.Width, targetBoundingBoxBufferSize.Height, PixelFormat.R32G32_Float);

            // Buffer that holds the minimum distance in case of being inside the bounding box
            var backSideRaycastBuffer = NewScopedRenderTarget2D(1, 1, PixelFormat.R32G32_Float);

            // Create a single channel light buffer
            PixelFormat lightBufferPixelFormat = needsColorLightBuffer ? PixelFormat.R16G16B16A16_Float : PixelFormat.R16_Float;
            var targetLightBufferSize = new Size2(Math.Max(1, depthInput.Width / LightBufferDownsampleLevel), Math.Max(1, depthInput.Height / LightBufferDownsampleLevel));
            var lightBuffer = NewScopedRenderTarget2D(targetLightBufferSize.Width, targetLightBufferSize.Height, lightBufferPixelFormat);
            lightShaftsEffectShader.SetOutput(lightBuffer);
            var lightShaftsParameters = lightShaftsEffectShader.Parameters;
            lightShaftsParameters.Set(DepthBaseKeys.DepthStencil, depthInput); // Bind scene depth

            if (!Initialized)
                Initialize(context.RenderContext);

            var renderView = context.RenderContext.RenderView;
            var viewInverse = Matrix.Invert(renderView.View);
            lightShaftsParameters.Set(TransformationKeys.ViewInverse, viewInverse);
            lightShaftsParameters.Set(TransformationKeys.Eye, new Vector4(viewInverse.TranslationVector, 1));

            // Setup parameters for Z reconstruction
            lightShaftsParameters.Set(CameraKeys.ZProjection, CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane));

            Matrix projectionInverse;
            Matrix.Invert(ref renderView.Projection, out projectionInverse);
            lightShaftsParameters.Set(TransformationKeys.ProjectionInverse, projectionInverse);

            applyLightEffectShader.SetOutput(GetSafeOutput(0));

            foreach (var lightShaft in lightShafts)
            {
                if (lightShaft.Light == null)
                    continue; // Skip entities without a light component

                // Set sample count for this light
                lightShaftsParameters.Set(LightShaftsEffectKeys.SampleCount, lightShaft.SampleCount);

                // Setup the shader group used for sampling shadows
                var shadowMapTexture = shadowMapRenderer.FindShadowMap(renderView.LightingView ?? renderView, lightShaft.Light);
                SetupLight(context, lightShaft, shadowMapTexture, lightShaftsParameters);

                // Check if we can pack bounding volumes together or need to draw them one by one
                var boundingVolumeLoop = lightShaft.SeparateBoundingVolumes ? lightShaft.BoundingVolumes.Count : 1;
                var lightBufferUsed = false;
                for (int i = 0; i < boundingVolumeLoop; ++i)
                {
                    // Generate list of bounding volume (either all or one by one depending on SeparateBoundingVolumes)
                    var currentBoundingVolumes = (lightShaft.SeparateBoundingVolumes) ? singleBoundingVolume : lightShaft.BoundingVolumes;
                    if (lightShaft.SeparateBoundingVolumes)
                        singleBoundingVolume[0] = lightShaft.BoundingVolumes[i];
                    
                    using (context.PushRenderTargetsAndRestore())
                    {
                        // Clear bounding box buffer
                        context.CommandList.Clear(boundingBoxBuffer, new Color4(1.0f, 0.0f, 0.0f, 0.0f));
                        context.CommandList.SetRenderTargetAndViewport(null, boundingBoxBuffer);
                        
                        // If nothing visible, skip second part
                        if (!DrawBoundingVolumeMinMax(context, currentBoundingVolumes))
                            continue;

                        context.CommandList.Clear(backSideRaycastBuffer, new Color4(1.0f, 0.0f, 0.0f, 0.0f));
                        context.CommandList.SetRenderTargetAndViewport(null, backSideRaycastBuffer);

                        // If nothing visible, skip second part
                        DrawBoundingVolumeBackside(context, currentBoundingVolumes);
                    }

                    if (!lightBufferUsed)
                    {
                        // First pass: replace (avoid a clear and blend state)
                        lightShaftsEffectShader.BlendState = BlendStates.Opaque;
                        lightBufferUsed = true;
                    }
                    else
                    {
                        // Then: add
                        var desc = BlendStates.Additive;
                        desc.RenderTarget0.ColorSourceBlend = Blend.One; // But without multiplying alpha
                        lightShaftsEffectShader.BlendState = desc;
                    }

                    if (lightShaft.SampleCount < 1)
                        throw new ArgumentOutOfRangeException(nameof(lightShaft.SampleCount));

                    // Set min/max input
                    lightShaftsEffectShader.SetInput(0, boundingBoxBuffer);
                    lightShaftsEffectShader.SetInput(1, backSideRaycastBuffer);

                    // Light accumulation pass (on low resolution buffer)
                    DrawLightShaft(context, lightShaft);
                }

                // Everything was outside, skip
                if (!lightBufferUsed)
                    continue;

                if (LightBufferDownsampleLevel != 1)
                {
                    // Blur the result
                    blur.Radius = LightBufferDownsampleLevel;
                    blur.SetInput(lightBuffer);
                    blur.SetOutput(lightBuffer);
                    blur.Draw(context);
                }

                // Additive blend pass
                Color3 lightColor = lightShaft.Light2.ComputeColor(context.GraphicsDevice.ColorSpace, lightShaft.Light.Intensity);
                applyLightEffectShader.Parameters.Set(AdditiveLightShaderKeys.LightColor, lightColor);
                applyLightEffectShader.Parameters.Set(AdditiveLightEffectKeys.Color, needsColorLightBuffer);
                applyLightEffectShader.SetInput(lightBuffer);
                applyLightEffectShader.Draw(context);
            }

            // Clean up unused render data
            unusedLights.Clear();
            foreach (var data in renderData)
            {
                if (data.Value.UsageCounter != usageCounter)
                    unusedLights.Add(data.Key);
            }
            foreach (var unusedLight in unusedLights)
            {
                renderData.Remove(unusedLight);
            }
            usageCounter++;
        }

        public void Draw(RenderDrawContext drawContext, Texture inputDepthStencil, Texture output)
        {
            SetInput(0, inputDepthStencil);
            SetOutput(output);
            Draw(drawContext);
        }

        private void UpdateRenderData(RenderDrawContext context, LightShaftRenderData data, RenderLightShaft lightShaft, LightShadowMapTexture shadowMapTexture)
        {
            if (lightShaft.Light2 is LightPoint)
            {
                data.GroupRenderer = new LightPointGroupRenderer();
            }
            else if (lightShaft.Light2 is LightSpot)
            {
                data.GroupRenderer = new LightSpotGroupRenderer();
            }
            else if (lightShaft.Light2 is LightDirectional)
            {
                data.GroupRenderer = new LightDirectionalGroupRenderer();
            }
            else
            {
                throw new InvalidOperationException("Unsupported light type");
            }

            ILightShadowMapShaderGroupData shadowGroup = null;
            if (shadowMapTexture != null)
            {
                data.ShadowType = shadowMapTexture.ShadowType;
                data.ShadowMapRenderer = shadowMapTexture.Renderer;
                shadowGroup = data.ShadowMapRenderer.CreateShaderGroupData(data.ShadowType);
            }
            else
            {
                data.ShadowType = 0;
                data.ShadowMapRenderer = null;
            }
            data.ShaderGroup = data.GroupRenderer.CreateLightShaderGroup(context, shadowGroup);   // TODO: Implement support for texture projection and attenuation?
        }

        private void SetupLight(RenderDrawContext context, RenderLightShaft lightShaft, LightShadowMapTexture shadowMapTexture, ParameterCollection lightParameterCollection)
        {
            BoundingBoxExt box = new BoundingBoxExt(new Vector3(-float.MaxValue), new Vector3(float.MaxValue)); // TODO

            LightShaftRenderData data;
            if (!renderData.TryGetValue(lightShaft.Light2, out data))
            {
                data = new LightShaftRenderData();
                renderData.Add(lightShaft.Light2, data);
                UpdateRenderData(context, data, lightShaft, shadowMapTexture);
            }

            if (shadowMapTexture != null && data.ShadowMapRenderer != null)
            {
                // Detect changed shadow map renderer or type
                if (data.ShadowMapRenderer != shadowMapTexture.Renderer || data.ShadowType != shadowMapTexture.ShadowType)
                    UpdateRenderData(context, data, lightShaft, shadowMapTexture);
            }
            else if (shadowMapTexture?.Renderer != data.ShadowMapRenderer) // Change from no shadows to shadows
            {
                UpdateRenderData(context, data, lightShaft, shadowMapTexture);
            }

            data.RenderViews[0] = context.RenderContext.RenderView;
            data.ShaderGroup.Reset();
            data.ShaderGroup.SetViews(data.RenderViews);
            data.ShaderGroup.AddView(0, context.RenderContext.RenderView, 1);

            data.ShaderGroup.AddLight(lightShaft.Light, shadowMapTexture);
            data.ShaderGroup.UpdateLayout("lightGroup");

            lightParameterCollection.Set(LightShaftsEffectKeys.LightGroup, data.ShaderGroup.ShaderSource);

            // Update the effect here so the layout is correct
            lightShaftsEffectShader.EffectInstance.UpdateEffect(GraphicsDevice);

            data.ShaderGroup.ApplyViewParameters(context, 0, lightParameterCollection);
            data.ShaderGroup.ApplyDrawParameters(context, 0, lightParameterCollection, ref box);

            data.UsageCounter = usageCounter;
        }

        private void DrawLightShaft(RenderDrawContext context, RenderLightShaft lightShaft)
        {
            lightShaftsEffectShader.Parameters.Set(LightShaftsShaderKeys.DensityFactor, lightShaft.DensityFactor);

            lightShaftsEffectShader.Draw(context, "Light shaft");
        }

        private bool DrawBoundingVolumeMinMax(RenderDrawContext context, IReadOnlyList<RenderLightShaftBoundingVolume> boundingVolumes)
        {
            return DrawBoundingVolumes(context, boundingVolumes, context.RenderContext.RenderView.ViewProjection);
        }

        private void DrawBoundingVolumeBackside(RenderDrawContext context, IReadOnlyList<RenderLightShaftBoundingVolume> boundingVolumes)
        {
            float backSideMaximumDistance = context.RenderContext.RenderView.FarClipPlane;
            float backSideMinimumDistance = -context.RenderContext.RenderView.NearClipPlane;
            Matrix backSideProjection = context.RenderContext.RenderView.View * Matrix.Scaling(1, 1, -1) * Matrix.OrthoRH(BackSideOrthographicSize, BackSideOrthographicSize, backSideMinimumDistance, backSideMaximumDistance);
            DrawBoundingVolumes(context, boundingVolumes, backSideProjection);
        }

        private bool DrawBoundingVolumes(RenderDrawContext context, IReadOnlyList<RenderLightShaftBoundingVolume> boundingVolumes, Matrix viewProjection)
        {
            var commandList = context.CommandList;

            bool effectUpdated = minmaxVolumeEffectShader.UpdateEffect(GraphicsDevice);
            if (minmaxVolumeEffectShader.Effect == null)
                return false;

            var needEffectUpdate = effectUpdated || previousMinmaxEffectBytecode != minmaxVolumeEffectShader.Effect.Bytecode;
            bool visibleMeshes = false;

            for (int pass = 0; pass < 2; ++pass)
            {
                var minmaxPipelineState = minmaxPipelineStates[pass];

                bool pipelineDirty = false;
                if (needEffectUpdate)
                {
                    // The EffectInstance might have been updated from outside
                    previousMinmaxEffectBytecode = minmaxVolumeEffectShader.Effect.Bytecode;

                    minmaxPipelineState.State.RootSignature = minmaxVolumeEffectShader.RootSignature;
                    minmaxPipelineState.State.EffectBytecode = minmaxVolumeEffectShader.Effect.Bytecode;

                    minmaxPipelineState.State.Output.RenderTargetCount = 1;
                    minmaxPipelineState.State.Output.RenderTargetFormat0 = commandList.RenderTarget.Format;
                    pipelineDirty = true;
                }

                MeshDraw currentDraw = null;
                var frustum = new BoundingFrustum(ref viewProjection);
                foreach (var volume in boundingVolumes)
                {
                    if (volume.Model == null)
                        continue;

                    // Update parameters for the minmax shader
                    Matrix worldViewProjection = Matrix.Multiply(volume.World, viewProjection);
                    minmaxVolumeEffectShader.Parameters.Set(VolumeMinMaxShaderKeys.WorldViewProjection, worldViewProjection);

                    foreach (var mesh in volume.Model.Meshes)
                    {
                        // Frustum culling
                        BoundingBox meshBoundingBox;
                        Matrix world = volume.World;
                        BoundingBox.Transform(ref mesh.BoundingBox, ref world, out meshBoundingBox);
                        var boundingBoxExt = new BoundingBoxExt(meshBoundingBox);
                        if (boundingBoxExt.Extent != Vector3.Zero
                            && !VisibilityGroup.FrustumContainsBox(ref frustum, ref boundingBoxExt, true))
                            continue;

                        visibleMeshes = true;

                        var draw = mesh.Draw;

                        if (currentDraw != draw)
                        {
                            if (minmaxPipelineState.State.PrimitiveType != draw.PrimitiveType)
                            {
                                minmaxPipelineState.State.PrimitiveType = draw.PrimitiveType;
                                pipelineDirty = true;
                            }

                            var inputElements = draw.VertexBuffers.CreateInputElements();
                            if (inputElements.ComputeHash() != minmaxPipelineState.State.InputElements.ComputeHash())
                            {
                                minmaxPipelineState.State.InputElements = inputElements;
                                pipelineDirty = true;
                            }

                            // Update mesh
                            for (int i = 0; i < draw.VertexBuffers.Length; i++)
                            {
                                var vertexBuffer = draw.VertexBuffers[i];
                                commandList.SetVertexBuffer(i, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
                            }
                            if (draw.IndexBuffer != null)
                                commandList.SetIndexBuffer(draw.IndexBuffer.Buffer, draw.IndexBuffer.Offset, draw.IndexBuffer.Is32Bit);
                            currentDraw = draw;
                        }

                        if (pipelineDirty)
                        {
                            minmaxPipelineState.Update();
                            pipelineDirty = false;
                        }

                        context.CommandList.SetPipelineState(minmaxPipelineState.CurrentState);

                        minmaxVolumeEffectShader.Apply(context.GraphicsContext);

                        // Draw
                        if (currentDraw.IndexBuffer == null)
                            commandList.Draw(currentDraw.DrawCount, currentDraw.StartLocation);
                        else
                            commandList.DrawIndexed(currentDraw.DrawCount, currentDraw.StartLocation);
                    }
                }
            }

            return visibleMeshes;
        }

        private class LightShaftRenderData
        {
            public LightGroupRendererDynamic GroupRenderer;
            public LightShaderGroupDynamic ShaderGroup;
            public IDirectLight Light;
            public FastList<RenderView> RenderViews = new FastList<RenderView>(new RenderView[1]);
            public LightShadowType ShadowType;
            public ILightShadowMapRenderer ShadowMapRenderer;
            public int UsageCounter = 0;
        }
    }
}
