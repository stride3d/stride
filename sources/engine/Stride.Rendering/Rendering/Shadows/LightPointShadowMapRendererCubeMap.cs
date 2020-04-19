// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Shaders;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// Renders omnidirectional shadow maps using a simulated cubemap inside of the shadow map atlas
    /// </summary>
    public class LightPointShadowMapRendererCubeMap : LightShadowMapRendererBase
    {
        // Face based on index in this array
        private static readonly Matrix[] ViewFaceRotationMatrices = new[]
        {
            Matrix.RotationY(MathUtil.Pi),          // Front
            Matrix.Identity,                        // Back
            Matrix.RotationY(MathUtil.PiOverTwo),   // Right
            Matrix.RotationY(-MathUtil.PiOverTwo),  // Left
            Matrix.RotationX(-MathUtil.PiOverTwo),  // Up
            Matrix.RotationX(MathUtil.PiOverTwo),   // Down
        };

        // Number of border pixels to add to the cube map in order to allow filtering
        public const int BorderPixels = 8;

        private PoolListStruct<ShaderData> shaderDataPool;

        public LightPointShadowMapRendererCubeMap()
        {
            shaderDataPool = new PoolListStruct<ShaderData>(4, () => new ShaderData());
        }

        public override void Reset(RenderContext context)
        {
            base.Reset(context);

            shaderDataPool.Clear();
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType)
        {
            return new ShaderGroupData(shadowType);
        }

        public override bool CanRenderLight(IDirectLight light)
        {
            var lightPoint = light as LightPoint;
            if (lightPoint != null)
                return ((LightPointShadowMap)lightPoint.Shadow).Type == LightPointShadowMapType.CubeMap;
            return false;
        }

        public override LightShadowMapTexture CreateShadowMapTexture(RenderView renderView, RenderLight renderLight, IDirectLight light, int shadowMapSize)
        {
            var shadowMapTexture = base.CreateShadowMapTexture(renderView, renderLight, light, shadowMapSize);
            shadowMapTexture.CascadeCount = 6; // 6 faces
            return shadowMapTexture;
        }

        private static Vector2 GetLightClippingPlanes(LightPoint pointLight)
        {
            // Note: we don't take exactly the required depth range since this will result in a very poor resolution in most of the light's range
            return new Vector2(0.1f, pointLight.Radius * 2);
        }

        private static void GetViewParameters(LightShadowMapTexture shadowMapTexture, int index, out Matrix view)
        {
            // Apply light position
            var translationMatrix = Matrix.Translation(-shadowMapTexture.RenderLight.Position);

            Matrix.Multiply(ref translationMatrix, ref ViewFaceRotationMatrices[index], out view);
        }

        /// <returns>
        /// x = Near; y = 1/(Far-Near)
        /// </returns>
        private static Vector2 GetShadowMapDepthParameters(LightShadowMapTexture shadowMapTexture)
        {
            var lightPoint = shadowMapTexture.Light as LightPoint;
            Vector2 clippingPlanes = GetLightClippingPlanes(lightPoint);

            return CameraKeys.ZProjectionACalculate(clippingPlanes.X, clippingPlanes.Y);
        }

        public override void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap)
        {
            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            shaderData.DepthBias = lightShadowMap.Light.Shadow.BiasParameters.DepthBias;
            shaderData.OffsetScale = lightShadowMap.Light.Shadow.BiasParameters.NormalOffsetScale;
            shaderData.DepthParameters = GetShadowMapDepthParameters(lightShadowMap);

            var clippingPlanes = GetLightClippingPlanes((LightPoint)lightShadowMap.Light);

            var textureMapSize = lightShadowMap.GetRectangle(0).Size;

            // Calculate angle of the projection with border pixels taken into account to allow filtering
            float halfMapSize = (float)textureMapSize.Width / 2;
            float halfFov = (float)Math.Atan((halfMapSize + BorderPixels) / halfMapSize);
            shaderData.Projection = Matrix.PerspectiveFovRH(halfFov * 2, 1.0f, clippingPlanes.X, clippingPlanes.Y);

            Vector2 atlasSize = new Vector2(lightShadowMap.Atlas.Width, lightShadowMap.Atlas.Height);

            for (int i = 0; i < 6; i++)
            {
                Rectangle faceRectangle = lightShadowMap.GetRectangle(i);
                shaderData.FaceOffsets[i] = new Vector2(faceRectangle.Left + BorderPixels, faceRectangle.Top + BorderPixels) / atlasSize;

                // Compute view parameters
                GetViewParameters(lightShadowMap, i, out shaderData.View[i]);

                // Allocate shadow render view
                var shadowRenderView = CreateRenderView();
                shadowRenderView.RenderView = sourceView;
                shadowRenderView.ShadowMapTexture = lightShadowMap;
                shadowRenderView.Rectangle = faceRectangle;
                shadowRenderView.ViewSize = new Vector2(faceRectangle.Width, faceRectangle.Height);

                shadowRenderView.NearClipPlane = clippingPlanes.X;
                shadowRenderView.FarClipPlane = clippingPlanes.Y;

                shadowRenderView.View = shaderData.View[i];
                shadowRenderView.Projection = shaderData.Projection;
                Matrix.Multiply(ref shadowRenderView.View, ref shadowRenderView.Projection, out shadowRenderView.ViewProjection);

                // Create projection matrix with adjustment
                var textureCoords = new Vector4(
                    (float)shadowRenderView.Rectangle.Left / lightShadowMap.Atlas.Width,
                    (float)shadowRenderView.Rectangle.Top / lightShadowMap.Atlas.Height,
                    (float)shadowRenderView.Rectangle.Right / lightShadowMap.Atlas.Width,
                    (float)shadowRenderView.Rectangle.Bottom / lightShadowMap.Atlas.Height);
                float leftX = (float)lightShadowMap.Size / lightShadowMap.Atlas.Width * 0.5f;
                float leftY = (float)lightShadowMap.Size / lightShadowMap.Atlas.Height * 0.5f;
                float centerX = 0.5f * (textureCoords.X + textureCoords.Z);
                float centerY = 0.5f * (textureCoords.Y + textureCoords.W);

                var projectionToShadow = Matrix.Scaling(leftX, -leftY, 1.0f) * Matrix.Translation(centerX, centerY, 0.0f);
                Matrix.Multiply(ref shadowRenderView.ViewProjection, ref projectionToShadow, out shaderData.WorldToShadow[i]);

                shadowRenderView.VisiblityIgnoreDepthPlanes = false;

                // Add the render view for the current frame
                context.RenderSystem.Views.Add(shadowRenderView);
            }
        }

        private class ShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;

            /// <summary>
            /// Offset to every one of the faces of the cubemap in the atlas
            /// </summary>
            public readonly Vector2[] FaceOffsets = new Vector2[6];

            /// <summary>
            /// Calculated by <see cref="CameraKeys.ZProjectionACalculate"/> to reconstruct linear depth from the depth buffer
            /// </summary>
            public Vector2 DepthParameters;

            /// <summary>
            /// Projection matrix used for each cubemap face
            /// </summary>
            public Matrix Projection;

            /// <summary>
            /// View matrices for all the faces
            /// </summary>
            public Matrix[] View = new Matrix[6];

            /// <summary>
            /// Converts from world space to uv space
            /// </summary>
            public Matrix[] WorldToShadow = new Matrix[6];

            public float DepthBias;
            public float OffsetScale;
        }

        private class ShaderGroupData : LightShadowMapShaderGroupDataBase
        {
            private const string ShaderName = "ShadowMapReceiverPointCubeMap";

            private Texture shadowMapTexture;
            private Vector2 shadowMapTextureSize;
            private Vector2 shadowMapTextureTexelSize;

            private Matrix[] worldToShadow;
            private Matrix[] inverseWorldToShadow;
            private float[] depthBiases;
            private float[] offsetScales;
            private Vector2[] depthParameters;

            private ValueParameterKey<float> depthBiasesKey;
            private ValueParameterKey<float> offsetScalesKey;
            private ValueParameterKey<Matrix> worldToShadowKey;
            private ValueParameterKey<Matrix> inverseWorldToShadowKey;
            private ValueParameterKey<Vector2> depthParametersKey;

            private ObjectParameterKey<Texture> shadowMapTextureKey;
            private ValueParameterKey<Vector2> shadowMapTextureSizeKey;
            private ValueParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            public ShaderGroupData(LightShadowType shadowType) : base(shadowType)
            {
            }

            public override void UpdateLayout(string compositionName)
            {
                shadowMapTextureKey = ShadowMapKeys.ShadowMapTexture.ComposeWith(compositionName);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionName);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionName);
                worldToShadowKey = ShadowMapReceiverPointCubeMapKeys.WorldToShadow.ComposeWith(compositionName);
                inverseWorldToShadowKey = ShadowMapReceiverPointCubeMapKeys.InverseWorldToShadow.ComposeWith(compositionName);
                depthBiasesKey = ShadowMapReceiverPointCubeMapKeys.DepthBiases.ComposeWith(compositionName);
                depthParametersKey = ShadowMapReceiverPointCubeMapKeys.DepthParameters.ComposeWith(compositionName);
                offsetScalesKey = ShadowMapReceiverPointCubeMapKeys.OffsetScales.ComposeWith(compositionName);
            }

            public override void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                base.UpdateLightCount(lightLastCount, lightCurrentCount);

                Array.Resize(ref worldToShadow, lightCurrentCount * 6);
                Array.Resize(ref inverseWorldToShadow, lightCurrentCount * 6);
                Array.Resize(ref depthBiases, lightCurrentCount);
                Array.Resize(ref depthParameters, lightCurrentCount);
                Array.Resize(ref offsetScales, lightCurrentCount);
            }

            public override ShaderClassSource CreateShaderSource(int lightCurrentCount)
            {
                return new ShaderClassSource(ShaderName, lightCurrentCount);
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

                        // Copy per-face data
                        for (int j = 0; j < 6; j++)
                        {
                            worldToShadow[lightIndex * 6 + j] = shaderData.WorldToShadow[j];
                            inverseWorldToShadow[lightIndex * 6 + j] = Matrix.Invert(shaderData.WorldToShadow[j]);
                        }

                        depthBiases[lightIndex] = shaderData.DepthBias;
                        offsetScales[lightIndex] = shaderData.OffsetScale;
                        depthParameters[lightIndex] = shaderData.DepthParameters;
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

                parameters.Set(worldToShadowKey, worldToShadow);
                parameters.Set(inverseWorldToShadowKey, inverseWorldToShadow);
                parameters.Set(depthParametersKey, depthParameters);
                parameters.Set(depthBiasesKey, depthBiases);
                parameters.Set(offsetScalesKey, offsetScales);
            }
        }
    }
}
