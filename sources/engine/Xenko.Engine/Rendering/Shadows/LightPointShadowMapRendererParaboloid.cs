// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Lights;
using Xenko.Shaders;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// Renders omnidirectional shadow maps using paraboloid shadow maps
    /// </summary>
    public class LightPointShadowMapRendererParaboloid : LightShadowMapRendererBase
    {
        private PoolListStruct<ShaderData> shaderDataPool;
        private PoolListStruct<ShadowMapTexture> shadowMaps;

        public LightPointShadowMapRendererParaboloid()
        {
            shaderDataPool = new PoolListStruct<ShaderData>(4, () => new ShaderData());
            shadowMaps = new PoolListStruct<ShadowMapTexture>(16, () => new ShadowMapTexture());
        }

        public override void Reset(RenderContext context)
        {
            base.Reset(context);

            shadowMaps.Clear();
            shaderDataPool.Clear();
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType)
        {
            return new ShaderGroupData(shadowType);
        }

        public override bool CanRenderLight(IDirectLight light)
        {
            var pl = light as LightPoint;
            if (pl != null)
            {
                var type = ((LightPointShadowMap)pl.Shadow).Type;
                return type == LightPointShadowMapType.DualParaboloid;
            }
            return false;
        }

        public override LightShadowMapTexture CreateShadowMapTexture(RenderView renderView, RenderLight renderLight, IDirectLight light, int shadowMapSize)
        {
            var shadowMap = shadowMaps.Add();
            shadowMap.Initialize(renderView, renderLight, light, light.Shadow, shadowMapSize, this);
            shadowMap.CascadeCount = 2; // 2 faces
            return shadowMap;
        }

        public override void ApplyViewParameters(RenderDrawContext context, ParameterCollection parameters, LightShadowMapTexture shadowMapTexture)
        {
            parameters.Set(ShadowMapCasterParaboloidProjectionKeys.DepthParameters, GetShadowMapDepthParameters(shadowMapTexture));
        }

        public override void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap)
        {
            CalculateViewDirection(lightShadowMap);

            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            shaderData.DepthBias = lightShadowMap.Light.Shadow.BiasParameters.DepthBias;

            Vector2 atlasSize = new Vector2(lightShadowMap.Atlas.Width, lightShadowMap.Atlas.Height);

            Rectangle frontRectangle = lightShadowMap.GetRectangle(0);

            // Coordinates have 1 border pixel so that the shadow receivers don't accidentally sample outside of the texture area
            shaderData.FaceSize = new Vector2(frontRectangle.Width - 2, frontRectangle.Height - 2) / atlasSize;
            shaderData.Offset = new Vector2(frontRectangle.Left + 1, frontRectangle.Top + 1) / atlasSize;

            Rectangle backRectangle = lightShadowMap.GetRectangle(1);
            shaderData.BackfaceOffset = new Vector2(backRectangle.Left + 1, backRectangle.Top + 1) / atlasSize - shaderData.Offset;

            shaderData.DepthParameters = GetShadowMapDepthParameters(lightShadowMap);

            for (int i = 0; i < 2; i++)
            {
                // Allocate shadow render view
                var shadowRenderView = CreateRenderView();
                shadowRenderView.RenderView = sourceView;
                shadowRenderView.ShadowMapTexture = lightShadowMap;
                shadowRenderView.Rectangle = lightShadowMap.GetRectangle(i);
                shadowRenderView.NearClipPlane = 0.0f;
                shadowRenderView.FarClipPlane = GetShadowMapFarPlane(lightShadowMap);
                shadowRenderView.ViewSize = new Vector2(lightShadowMap.GetRectangle(i).Width, lightShadowMap.GetRectangle(i).Height);

                // Compute view parameters
                // Note: we only need view here since we are doing paraboloid projection in the vertex shader
                GetViewParameters(lightShadowMap, i, out shadowRenderView.View, true);

                // Also set the first view matrix on the shader data
                if (i == 0)
                    shaderData.View = shadowRenderView.View;

                Matrix virtualProjectionMatrix = shadowRenderView.View;
                virtualProjectionMatrix *= Matrix.Scaling(1.0f / shadowRenderView.FarClipPlane);

                shadowRenderView.ViewProjection = virtualProjectionMatrix;

                shadowRenderView.VisiblityIgnoreDepthPlanes = false;

                // Add the render view for the current frame
                context.RenderSystem.Views.Add(shadowRenderView);
            }
        }

        /// <summary>
        /// Calculates the direction of the split between the shadow maps
        /// </summary>
        private void CalculateViewDirection(LightShadowMapTexture shadowMapTexture)
        {
            var pointShadowMapTexture = shadowMapTexture as ShadowMapTexture;
            Matrix.Orthonormalize(ref shadowMapTexture.RenderLight.WorldMatrix, out pointShadowMapTexture.ForwardMatrix);
            pointShadowMapTexture.ForwardMatrix.Invert();
        }

        /// <summary>
        /// Computes shadow map depth parameters.
        /// </summary>
        /// <returns>
        /// x = Near; y = 1/(Far-Near)
        /// </returns>
        private Vector2 GetShadowMapDepthParameters(LightShadowMapTexture shadowMapTexture)
        {
            var lightPoint = shadowMapTexture.Light as LightPoint;
            Vector2 clippingPlanes = GetLightClippingPlanes(lightPoint);
            return new Vector2(clippingPlanes.X, 1.0f / (clippingPlanes.Y - clippingPlanes.X));
        }

        private Vector2 GetLightClippingPlanes(LightPoint pointLight)
        {
            return new Vector2(0.0f, pointLight.Radius + 2.0f);
        }

        private float GetShadowMapFarPlane(LightShadowMapTexture shadowMapTexture)
        {
            return GetLightClippingPlanes(shadowMapTexture.Light as LightPoint).Y;
        }

        private void GetViewParameters(LightShadowMapTexture shadowMapTexture, int index, out Matrix view, bool forCasting)
        {
            var pointShadowMapTexture = shadowMapTexture as ShadowMapTexture;
            Matrix flippingMatrix = Matrix.Identity;

            // Flip Y for rendering shadow maps
            if (forCasting)
            {
                // Render upside down, so reading doesn't need any modification
                flippingMatrix.Up = -flippingMatrix.Up;
            }

            // Apply light position
            view = Matrix.Translation(-shadowMapTexture.RenderLight.Position);

            // Apply mapping plane rotatation
            view *= pointShadowMapTexture.ForwardMatrix;

            if (index == 0)
            {
                // Camera (Front)
                // no rotation
            }
            else
            {
                // Camera (Back)
                flippingMatrix.Forward = -flippingMatrix.Forward;
            }
            view *= flippingMatrix;
        }

        private class ShadowMapTexture : LightShadowMapTexture
        {
            public Matrix ForwardMatrix;
        }

        private class ShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;

            /// <summary>
            /// Normalized offset of the front face of the shadow map in normalized coordinates
            /// </summary>
            public Vector2 Offset;

            /// <summary>
            /// Offset from fromnt to back face in normalized texture coordinates in the atlas
            /// </summary>
            public Vector2 BackfaceOffset;

            /// <summary>
            /// Size of a single face of the shadow map
            /// </summary>
            public Vector2 FaceSize;

            /// <summary>
            /// Matrix that converts from world space to the front face space of the light's shadow map
            /// </summary>
            public Matrix View;

            /// <summary>
            /// Radius of the point light, used to determine the range of the depth buffer
            /// </summary>
            public Vector2 DepthParameters;

            public float DepthBias;
        }

        private class ShaderGroupData : LightShadowMapShaderGroupDataBase
        {
            private const string ShaderName = "ShadowMapReceiverPointParaboloid";

            private Texture shadowMapTexture;
            private Vector2 shadowMapTextureSize;
            private Vector2 shadowMapTextureTexelSize;

            private Matrix[] viewMatrices;
            private Vector2[] offsets;
            private Vector2[] backfaceOffsets;
            private Vector2[] faceSizes;
            private Vector2[] depthParameters;
            private float[] depthBiases;

            private ValueParameterKey<float> depthBiasesKey;
            private ValueParameterKey<Matrix> viewKey;
            private ValueParameterKey<Vector2> offsetsKey;
            private ValueParameterKey<Vector2> backfaceOffsetsKey;
            private ValueParameterKey<Vector2> faceSizesKey;
            private ValueParameterKey<Vector2> depthParametersKey;

            private ObjectParameterKey<Texture> shadowMapTextureKey;
            private ValueParameterKey<Vector2> shadowMapTextureSizeKey;
            private ValueParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            public ShaderGroupData(LightShadowType shadowType) : base(shadowType)
            {
            }

            public override ShaderClassSource CreateShaderSource(int lightCurrentCount)
            {
                return new ShaderClassSource(ShaderName, lightCurrentCount);
            }

            public override void UpdateLayout(string compositionName)
            {
                shadowMapTextureKey = ShadowMapKeys.ShadowMapTexture.ComposeWith(compositionName);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionName);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionName);
                offsetsKey = ShadowMapReceiverPointParaboloidKeys.FaceOffsets.ComposeWith(compositionName);
                backfaceOffsetsKey = ShadowMapReceiverPointParaboloidKeys.BackfaceOffsets.ComposeWith(compositionName);
                faceSizesKey = ShadowMapReceiverPointParaboloidKeys.FaceSizes.ComposeWith(compositionName);
                depthParametersKey = ShadowMapReceiverPointParaboloidKeys.DepthParameters.ComposeWith(compositionName);
                viewKey = ShadowMapReceiverPointParaboloidKeys.View.ComposeWith(compositionName);
                depthBiasesKey = ShadowMapReceiverPointParaboloidKeys.DepthBiases.ComposeWith(compositionName);
            }

            public override void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                base.UpdateLightCount(lightLastCount, lightCurrentCount);

                Array.Resize(ref offsets, lightCurrentCount);
                Array.Resize(ref backfaceOffsets, lightCurrentCount);
                Array.Resize(ref faceSizes, lightCurrentCount);
                Array.Resize(ref depthParameters, lightCurrentCount);
                Array.Resize(ref viewMatrices, lightCurrentCount);
                Array.Resize(ref depthBiases, lightCurrentCount);
            }

            public override void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
            {
                var boundingBox2 = (BoundingBox)boundingBox;
                bool shadowMapCreated = false;
                int lightIndex = 0;

                for (int i = 0; i < currentLights.Count; ++i)
                {
                    var lightEntry = currentLights[i];
                    if (lightEntry.Light.BoundingBox.Intersects(ref boundingBox2))
                    {
                        var shaderData = (ShaderData)lightEntry.ShadowMapTexture.ShaderData;
                        offsets[lightIndex] = shaderData.Offset;
                        backfaceOffsets[lightIndex] = shaderData.BackfaceOffset;
                        faceSizes[lightIndex] = shaderData.FaceSize;
                        depthParameters[lightIndex] = shaderData.DepthParameters;
                        depthBiases[lightIndex] = shaderData.DepthBias;
                        viewMatrices[lightIndex] = shaderData.View;
                        lightIndex++;

                        // TODO: should be setup just once at creation time
                        if (!shadowMapCreated)
                        {
                            shadowMapTexture = shaderData.Texture;
                            if (shadowMapTexture != null)
                            {
                                shadowMapTextureSize = new Vector2(shadowMapTexture.Width, shadowMapTexture.Height);
                                shadowMapTextureTexelSize = 1.0f / shadowMapTextureSize;
                            }
                            shadowMapCreated = true;
                        }
                    }
                }

                parameters.Set(shadowMapTextureKey, shadowMapTexture);
                parameters.Set(shadowMapTextureSizeKey, shadowMapTextureSize);
                parameters.Set(shadowMapTextureTexelSizeKey, shadowMapTextureTexelSize);

                parameters.Set(viewKey, viewMatrices);
                parameters.Set(offsetsKey, offsets);
                parameters.Set(backfaceOffsetsKey, backfaceOffsets);
                parameters.Set(faceSizesKey, faceSizes);
                parameters.Set(depthParametersKey, depthParameters);

                parameters.Set(depthBiasesKey, depthBiases);
            }
        }
    }
}
