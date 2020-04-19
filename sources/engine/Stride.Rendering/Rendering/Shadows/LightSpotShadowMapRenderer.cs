// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Shaders;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// Renders a shadow map from a directional light.
    /// </summary>
    public class LightSpotShadowMapRenderer : LightShadowMapRendererBase
    {
        private PoolListStruct<LightSpotShadowMapShaderData> shaderDataPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpotShadowMapRenderer"/> class.
        /// </summary>
        public LightSpotShadowMapRenderer()
        {
            shaderDataPool = new PoolListStruct<LightSpotShadowMapShaderData>(8, CreateLightSpotShadowMapShaderData);
        }
        
        public override void Reset(RenderContext context)
        {
            base.Reset(context);

            shaderDataPool.Clear();
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType)
        {
            return new LightSpotShadowMapGroupShaderData(shadowType);
        }
        
        public override bool CanRenderLight(IDirectLight light)
        {
            return light is LightSpot;
        }

        public override void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap)
        {
            // TODO: Min and Max distance can be auto-computed from readback from Z buffer  // Yeah sure... good luck with that.
            var shadow = (LightStandardShadowMap)lightShadowMap.Shadow;

            // Computes the cascade splits
            var renderLight = lightShadowMap.RenderLight;
            var spotLight = (LightSpot)renderLight.Type;

            // Get new shader data from pool
            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            
            shaderData.DepthBias = shadow.BiasParameters.DepthBias;
            shaderData.OffsetScale = shadow.BiasParameters.NormalOffsetScale;
            
            // TODO: Calculation of near and far is hardcoded/approximated. We should find a better way to calculate it.
            var nearClip = 0.01f;   // TODO: This should be configurable.
            var farClip = spotLight.Range * 2.0f;  // TODO: For some reason this multiplication by two is required. This should be investigated and fixed properly.
            shaderData.DepthRange = new Vector2(nearClip, farClip); //////////////////////////////////////////

            // Update the shadow camera
            Matrix.Invert(ref renderLight.WorldMatrix, out var viewMatrix);
            Matrix.PerspectiveFovRH(spotLight.AngleOuterInRadians, spotLight.AspectRatio, nearClip, farClip, out var projectionMatrix); // Perspective Projection for spotlights
            Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out var viewProjectionMatrix);

            var shadowMapRectangle = lightShadowMap.GetRectangle(0);

            var cascadeTextureCoords = new Vector4(
                (float)shadowMapRectangle.Left / lightShadowMap.Atlas.Width,
                (float)shadowMapRectangle.Top / lightShadowMap.Atlas.Height,
                (float)shadowMapRectangle.Right / lightShadowMap.Atlas.Width,
                (float)shadowMapRectangle.Bottom / lightShadowMap.Atlas.Height);

            //// Add border (avoid using edges due to bilinear filtering and blur)
            //var borderSizeU = VsmBlurSize / lightShadowMap.Atlas.Width;
            //var borderSizeV = VsmBlurSize / lightShadowMap.Atlas.Height;
            //cascadeTextureCoords.X += borderSizeU;
            //cascadeTextureCoords.Y += borderSizeV;
            //cascadeTextureCoords.Z -= borderSizeU;
            //cascadeTextureCoords.W -= borderSizeV;

            float leftX = (float)lightShadowMap.Size / lightShadowMap.Atlas.Width * 0.5f;
            float leftY = (float)lightShadowMap.Size / lightShadowMap.Atlas.Height * 0.5f;
            float centerX = 0.5f * (cascadeTextureCoords.X + cascadeTextureCoords.Z);
            float centerY = 0.5f * (cascadeTextureCoords.Y + cascadeTextureCoords.W);

            // Compute receiver view proj matrix
            Matrix.Scaling(leftX, -leftY, 1.0f, out var scaleMatrix);
            Matrix.Translation(centerX, centerY, 0.0f, out var translationMatrix);
            Matrix.Multiply(ref scaleMatrix, ref translationMatrix, out var adjustmentMatrix);
            // Calculate View Proj matrix from World space to Cascade space
            Matrix.Multiply(ref viewProjectionMatrix, ref adjustmentMatrix, out shaderData.WorldToShadowCascadeUV);

            //Matrix rotationMatrix = Matrix.RotationZ(rotationZ);
            //Matrix.Multiply(ref viewProjectionMatrix, ref rotationMatrix, out shaderData.worldToShadowProjectiveTextureUV);

            shaderData.ViewMatrix = viewMatrix;
            shaderData.ProjectionMatrix = projectionMatrix;
            
            // Allocate shadow render view
            var shadowRenderView = CreateRenderView();
            shadowRenderView.RenderView = sourceView;
            shadowRenderView.ShadowMapTexture = lightShadowMap;
            shadowRenderView.Rectangle = shadowMapRectangle;
            // Compute view parameters
            shadowRenderView.View = shaderData.ViewMatrix;
            shadowRenderView.Projection = shaderData.ProjectionMatrix;
            Matrix.Multiply(ref shadowRenderView.View, ref shadowRenderView.Projection, out shadowRenderView.ViewProjection);
            shadowRenderView.ViewSize = new Vector2(shadowMapRectangle.Width, shadowMapRectangle.Height);
            shadowRenderView.NearClipPlane = nearClip;
            shadowRenderView.FarClipPlane = farClip;

            // Add the render view for the current frame
            context.RenderSystem.Views.Add(shadowRenderView);

            // Collect objects in shadow views
            context.VisibilityGroup.TryCollect(shadowRenderView);
        }

        private class LightSpotShadowMapShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;

            public float DepthBias;
            public float OffsetScale;

            public Vector2 DepthRange;

            public Matrix WorldToShadowCascadeUV;

            public Matrix ViewMatrix;
            public Matrix ProjectionMatrix;
        }

        private class LightSpotShadowMapGroupShaderData : LightShadowMapShaderGroupDataBase
        {
            private const string ShaderName = "ShadowMapReceiverSpot";

            // Values:
            private Matrix[] worldToShadowCascadeUV;

            private Matrix[] inverseWorldToShadowCascadeUV;

            private Vector2[] depthRanges;

            private float[] depthBiases;
            private float[] offsetScales;

            private Texture shadowMapTexture;   // TODO: Shouldn't this be a local variable??

            private Vector2 shadowMapTextureSize;
            private Vector2 shadowMapTextureTexelSize;

            // Keys:
            private ObjectParameterKey<Texture> shadowMapTextureKey;

            private ValueParameterKey<Matrix> worldToShadowCascadeUVsKey;

            private ValueParameterKey<Matrix> inverseWorldToShadowCascadeUVsKey;

            private ValueParameterKey<Vector2> depthRangesKey;

            private ValueParameterKey<float> depthBiasesKey;
            private ValueParameterKey<float> offsetScalesKey;

            private ValueParameterKey<Vector2> shadowMapTextureSizeKey;
            private ValueParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            /// <summary>
            /// Initializes a new instance of the <see cref="LightSpotShadowMapGroupShaderData" /> class.
            /// </summary>
            /// <param name="shadowType">Type of the shadow.</param>
            /// <param name="lightCountMax">The light count maximum.</param>
            public LightSpotShadowMapGroupShaderData(LightShadowType shadowType) : base(shadowType)
            {
            }
            
            public override ShaderClassSource CreateShaderSource(int lightCurrentCount)
            {
                return new ShaderClassSource(ShaderName, lightCurrentCount, (ShadowType & LightShadowType.Debug) != 0);
            }

            public override void UpdateLayout(string compositionName)
            {
                shadowMapTextureKey = ShadowMapKeys.ShadowMapTexture.ComposeWith(compositionName);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionName);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionName);
                worldToShadowCascadeUVsKey = ShadowMapReceiverBaseKeys.WorldToShadowCascadeUV.ComposeWith(compositionName);
                inverseWorldToShadowCascadeUVsKey = ShadowMapReceiverBaseKeys.InverseWorldToShadowCascadeUV.ComposeWith(compositionName);
                depthRangesKey = ShadowMapReceiverBaseKeys.DepthRanges.ComposeWith(compositionName);
                depthBiasesKey = ShadowMapReceiverBaseKeys.DepthBiases.ComposeWith(compositionName);
                offsetScalesKey = ShadowMapReceiverBaseKeys.OffsetScales.ComposeWith(compositionName);
            }

            public override void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                base.UpdateLightCount(lightLastCount, lightCurrentCount);

                Array.Resize(ref worldToShadowCascadeUV, lightCurrentCount);
                Array.Resize(ref inverseWorldToShadowCascadeUV, lightCurrentCount);
                Array.Resize(ref depthRanges, lightCurrentCount);
                Array.Resize(ref depthBiases, lightCurrentCount);
                Array.Resize(ref offsetScales, lightCurrentCount);
            }

            public override void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
            {
                var boundingBox2 = (BoundingBox)boundingBox;
                bool shadowMapCreated = false;
                int lightIndex = 0;

                for (int i = 0; i < currentLights.Count; ++i)
                {
                    var lightEntry = currentLights[i];
                    var light = lightEntry.Light;

                    if (light.BoundingBox.Intersects(ref boundingBox2))
                    {
                        var singleLightData = (LightSpotShadowMapShaderData)lightEntry.ShadowMapTexture.ShaderData;
                        worldToShadowCascadeUV[lightIndex] = singleLightData.WorldToShadowCascadeUV;
                        Matrix.Invert(ref singleLightData.WorldToShadowCascadeUV, out inverseWorldToShadowCascadeUV[lightIndex]);

                        depthBiases[lightIndex] = singleLightData.DepthBias;
                        offsetScales[lightIndex] = singleLightData.OffsetScale;
                        depthRanges[lightIndex] = singleLightData.DepthRange;

                        if (!shadowMapCreated)
                        {
                            shadowMapTexture = singleLightData.Texture;
                            if (shadowMapTexture != null)
                            {
                                shadowMapTextureSize = new Vector2(shadowMapTexture.Width, shadowMapTexture.Height);
                                shadowMapTextureTexelSize = 1.0f / shadowMapTextureSize;
                            }
                            shadowMapCreated = true;
                        }

                        lightIndex++;
                    }
                }

                parameters.Set(shadowMapTextureKey, shadowMapTexture);
                parameters.Set(shadowMapTextureSizeKey, shadowMapTextureSize);
                parameters.Set(shadowMapTextureTexelSizeKey, shadowMapTextureTexelSize);
                parameters.Set(worldToShadowCascadeUVsKey, worldToShadowCascadeUV);
                parameters.Set(inverseWorldToShadowCascadeUVsKey, inverseWorldToShadowCascadeUV);
                parameters.Set(depthRangesKey, depthRanges);
                parameters.Set(depthBiasesKey, depthBiases);
                parameters.Set(offsetScalesKey, offsetScales);
            }
        }

        private static LightSpotShadowMapShaderData CreateLightSpotShadowMapShaderData()
        {
            return new LightSpotShadowMapShaderData();
        }
    }
}
