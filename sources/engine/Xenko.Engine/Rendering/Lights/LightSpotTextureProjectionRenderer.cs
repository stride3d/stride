// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    public struct SpotLightTextureParameters
    {
        public Vector2 UVScale
        {
            get => spotLightParameters.UVScale;
        }

        public Vector2 UVOffset
        {
            get => spotLightParameters.UVOffset;
        }

        public LightSpot.FlipModeEnum FlipMode
        {
            get => spotLightParameters.FlipMode;
        }

        public Texture ProjectionTexture
        {
            get => spotLightParameters.ProjectionTexture;
        }

        /// <summary>
        /// Contains struct data fields
        /// </summary>
        private readonly SpotLightParametersStruct spotLightParameters;

        private struct SpotLightParametersStruct
        {
            internal Vector2 UVScale;
            internal Vector2 UVOffset;
            internal LightSpot.FlipModeEnum FlipMode;
            internal Texture ProjectionTexture;
        }

        public static SpotLightTextureParameters Default
        {
            get => new SpotLightTextureParameters(Vector2.One, Vector2.Zero, null, LightSpot.FlipModeEnum.None);
        }

        public SpotLightTextureParameters(Vector2 uvScale, Vector2 uvOffset, Texture projectionTexture, LightSpot.FlipModeEnum flipMode)
        {
            spotLightParameters = new SpotLightParametersStruct
            {
                UVScale = uvScale,
                UVOffset = uvOffset,
                FlipMode = flipMode,
                ProjectionTexture = projectionTexture
            };
        }

        public bool Equals(ref SpotLightTextureParameters other)
        {
            return Equals(ProjectionTexture, other.ProjectionTexture) &&
                FlipMode == other.FlipMode &&
                UVScale == other.UVScale &&
                UVOffset == other.UVOffset;
        }

        public bool Equals(SpotLightTextureParameters other)
        {
            return Equals(ref other);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is SpotLightTextureParameters targetObj && Equals(targetObj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ProjectionTexture != null ? ProjectionTexture.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)FlipMode; // todo: what is 397?
                hashCode = (hashCode * 397) ^ UVScale.GetHashCode();
                hashCode = (hashCode * 397) ^ UVOffset.GetHashCode();

                return hashCode;
            }
        }
    }

    /// <summary>
    /// </summary>
    public class LightSpotTextureProjectionRenderer : ITextureProjectionRenderer
    {
        //private PoolListStruct<LightSpotTextureProjectionShaderData> shaderDataPool;
        private readonly SpotLightTextureParameters lightParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpotTextureProjectionRenderer"/> class.
        /// </summary>
        public LightSpotTextureProjectionRenderer(SpotLightTextureParameters parameters)
        {
            // TODO: What is this?
            // commented as not used
            //shaderDataPool = new PoolListStruct<LightSpotTextureProjectionShaderData>(8, LightSpotTextureProjectionShaderData.ReturnInstanceFunc);
            lightParameters = parameters;
        }

        /*
        public override void Reset(RenderContext context)   // TODO: Implement this?
        {
            shaderDataPool.Clear();
        }
        */

        public ITextureProjectionShaderGroupData CreateShaderGroupData()   // TODO: Override!
        {
            return new LightSpotTextureProjectionGroupShaderData(lightParameters);
        }

        // Computes the view-projection matrix without any offset or scaling cascade.
        private static Matrix ComputeWorldToTextureUVMatrix(LightComponent lightComponent)
        {
            var spotLight = (LightSpot)lightComponent.Type;

            Matrix viewMatrix = lightComponent.Entity.Transform.WorldMatrix;
            viewMatrix.Invert();

            // TODO: PERFORMANCE: This does redundant work. The view projection matrix is already calculated within "LightSpotShadowMapRenderer".

            // TODO: Calculation of near and far is hardcoded/approximated. We should find a better way to calculate it.
            const float nearClip = 0.01f; // TODO: This should be configurable.
            float farClip = spotLight.Range;  // Removed the multiplication by two because I didn't see the point in it.
            Matrix projectionMatrix = Matrix.PerspectiveFovRH(spotLight.AngleOuterInRadians, spotLight.AspectRatio, nearClip, farClip); // Perspective Projection for spotlights
            Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out Matrix viewProjectionMatrix);

            // TODO: Add an offset so we don't have to do it in the shader?

            return viewProjectionMatrix; // View-projection matrix without offset to cascade.
        }

        // Computes the view-projection matrix without any offset or scaling cascade.
        private static Matrix ComputeProjectorPlaneMatrix(LightComponent lightComponent)
        {
            var spotLight = (LightSpot)lightComponent.Type;

            // Calculate the width and height of the near plane in world space:
            float nearClipDistance = Math.Min(spotLight.ProjectionPlaneDistance, spotLight.Range);
            float angleOuterInRadians = MathUtil.DegreesToRadians(Math.Max(spotLight.AngleInner, spotLight.AngleOuter));
            float height = (float)Math.Tan(angleOuterInRadians / 2.0f) * nearClipDistance;  // TODO: Is this correct?
            float width = height * spotLight.AspectRatio;  // TODO: Is this correct?

            Matrix viewMatrix = lightComponent.Entity.Transform.WorldMatrix;

            // Translate the matrix to position it at the near plane.
            Vector3 translation = viewMatrix.Forward * nearClipDistance;
            viewMatrix.M41 += translation.X;
            viewMatrix.M42 += translation.Y;
            viewMatrix.M43 += translation.Z;

            // Scale the X axis by the plane width:
            viewMatrix.M11 *= -width;
            viewMatrix.M12 *= -width;   // Invert the matrix so we don't have to do it in the shader.
            viewMatrix.M13 *= -width;

            // Scale the Y axis by the plane width:
            viewMatrix.M21 *= -height;
            viewMatrix.M22 *= -height;   // Invert the matrix so we don't have to do it in the shader.
            viewMatrix.M23 *= -height;

            return viewMatrix; // Model matrix of the projector plane.
        }

        // TODO: Find a way to use this class!
        public class LightSpotTextureProjectionShaderData : ILightShadowMapShaderData
        {
            //private static LightSpotTextureProjectionShaderData GetInstance => new LightSpotTextureProjectionShaderData();
            //public static Func<LightSpotTextureProjectionShaderData> ReturnInstanceFunc => () => GetInstance;

            public static ResultStruct GetResultStruct(ref LightSpotTextureProjectionShaderData[] lightSpotTextureProjectionShaderDatas)
            {
                var result = new ResultStruct(lightSpotTextureProjectionShaderDatas.Length);
                for (var i = 0; i < lightSpotTextureProjectionShaderDatas.Length; i++)
                {
                    result.WorldToTextureUV[i] = lightSpotTextureProjectionShaderDatas[i].WorldToTextureUV;
                    result.ProjectorPlaneMatrices[i] = lightSpotTextureProjectionShaderDatas[i].ProjectorPlaneMatric;
                    result.ProjectionTextureMipMapLevels[i] = lightSpotTextureProjectionShaderDatas[i].ProjectiveTextureMipMapLevel;
                    result.TransitionAreas[i] = lightSpotTextureProjectionShaderDatas[i].TransitionArea;
                }

                return result;
            }

            internal float ProjectiveTextureMipMapLevel;
            internal float TransitionArea;
            internal Matrix WorldToTextureUV;
            internal Matrix ProjectorPlaneMatric;

            public struct ResultStruct
            {
                internal readonly Matrix[] WorldToTextureUV;
                internal readonly Matrix[] ProjectorPlaneMatrices;
                internal readonly float[] ProjectionTextureMipMapLevels;
                internal readonly float[] TransitionAreas;

                internal ResultStruct(int arrayLength)
                {
                    WorldToTextureUV = new Matrix[arrayLength];
                    ProjectorPlaneMatrices = new Matrix[arrayLength];
                    ProjectionTextureMipMapLevels = new float[arrayLength];
                    TransitionAreas = new float[arrayLength];
                }
            }
        }

        private sealed class LightSpotTextureProjectionGroupShaderData : ITextureProjectionShaderGroupData
        {
            private const string ShaderNameBase = "TextureProjectionReceiverSpot";
            private const string ShaderNameAttenuation = "LightSpotAttenuationRectangular";

            // Values:
            private LightSpotTextureProjectionShaderData[] lightSpotTextureProjectionShaderData;
            private readonly SpotLightTextureParameters lightParameters;

            // Keys:
            private ObjectParameterKey<Texture> projectiveTextureKey;
            private ValueParameterKey<Vector2> uvScale;
            private ValueParameterKey<Vector2> uvOffset;
            private ValueParameterKey<Matrix> worldToProjectiveTextureUVsKey;
            private ValueParameterKey<Matrix> projectorPlaneMatricesKey;
            private ValueParameterKey<float> projectionTextureMipMapLevelsKey;
            private ValueParameterKey<float> transitionAreasKey;
            private ShaderMixinSource textureProjectionShader;

            /// <summary>
            /// Initializes a new instance of the <see cref="LightSpotTextureProjectionGroupShaderData" /> class.
            /// </summary>
            public LightSpotTextureProjectionGroupShaderData(SpotLightTextureParameters parameters)
            {
                lightParameters = parameters;
            }

            public void ApplyShader(ShaderMixinSource mixin)
            {
                mixin.CloneFrom(textureProjectionShader);
            }

            public void UpdateLayout(string compositionName)
            {
                projectiveTextureKey = TextureProjectionKeys.ProjectionTexture.ComposeWith(compositionName);
                uvScale = TextureProjectionKeys.UVScale.ComposeWith(compositionName);
                uvOffset = TextureProjectionKeys.UVOffset.ComposeWith(compositionName);
                worldToProjectiveTextureUVsKey = TextureProjectionReceiverBaseKeys.WorldToProjectiveTextureUV.ComposeWith(compositionName);
                projectorPlaneMatricesKey = TextureProjectionReceiverBaseKeys.ProjectorPlaneMatrices.ComposeWith(compositionName);
                projectionTextureMipMapLevelsKey = TextureProjectionReceiverBaseKeys.ProjectionTextureMipMapLevels.ComposeWith(compositionName);
                transitionAreasKey = TextureProjectionReceiverBaseKeys.TransitionAreas.ComposeWith(compositionName);
            }

            public void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                textureProjectionShader = new ShaderMixinSource();

                // Add the shader for projecting the texture:
                textureProjectionShader.Mixins.Add(new ShaderClassSource(ShaderNameBase, "PerDraw.Lighting", lightCurrentCount, (int)lightParameters.FlipMode));

                // Add the rectangular attenuation shader so the light doesn't use angular attenuation anymore because we want to show the full, square texture:
                textureProjectionShader.Mixins.Add(new ShaderClassSource(ShaderNameAttenuation));


                lightSpotTextureProjectionShaderData = new LightSpotTextureProjectionShaderData[lightCurrentCount];
                for (var i = 0; i < lightSpotTextureProjectionShaderData.Length; i++)
                    lightSpotTextureProjectionShaderData[i] = new LightSpotTextureProjectionShaderData();

            }

            public void Collect(RenderContext context, RenderView sourceView, int lightIndex, LightComponent lightComponent)
            {
                var spotLight = (LightSpot)lightComponent.Type;
                lightSpotTextureProjectionShaderData[lightIndex].WorldToTextureUV = ComputeWorldToTextureUVMatrix(lightComponent);
                lightSpotTextureProjectionShaderData[lightIndex].ProjectorPlaneMatric = ComputeProjectorPlaneMatrix(lightComponent);

                // We use the maximum number of mips instead of the actual number,
                // so things like video textures behave more consistently when changing the number of mip maps to generate.
                int maxMipMapCount = Texture.CountMips(lightParameters.ProjectionTexture.Width, lightParameters.ProjectionTexture.Height);
                float projectiveTextureMipMapLevel = (maxMipMapCount - 1f) * spotLight.MipMapScale; // "- 1" because the lowest mip level is 0, not 1.
                lightSpotTextureProjectionShaderData[lightIndex].ProjectiveTextureMipMapLevel = projectiveTextureMipMapLevel;
                lightSpotTextureProjectionShaderData[lightIndex].TransitionArea = Math.Max(spotLight.TransitionArea, 0.001f);   // Keep the value just above zero. This is to prevent some issues with the "smoothstep()" function on OpenGL and OpenGL ES.
            }

            public void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
            {
                var boundingBoxCasted = (BoundingBox)boundingBox;
                var lightIndex = 0;

                foreach (LightDynamicEntry lightEntry in currentLights)
                {
                    LightComponent lightComponent = lightEntry.Light;

                    if (!lightComponent.BoundingBox.Intersects(ref boundingBoxCasted)) continue;

                    /*
                        // TODO: Just save the shaderdata struct directly within "LightDynamicEntry"?
                        var singleLightData = (LightSpotTextureProjectionShaderData)lightEntry.ShadowMapTexture.ShaderData;   // TODO: This must not depend on the shadow map texture!
                        
                        worldToTextureUV[lightIndex] = singleLightData.WorldToTextureUV;
                        projectionTextureMipMapLevels[lightIndex] = singleLightData.ProjectiveTextureMipMapLevel;
                        projectiveTexture = singleLightData.ProjectiveTexture;
                        */

                    Collect(null, null, lightIndex, lightComponent);

                    ++lightIndex;
                }

                LightSpotTextureProjectionShaderData.ResultStruct collectResultStruct = LightSpotTextureProjectionShaderData.GetResultStruct(ref lightSpotTextureProjectionShaderData);

                // TODO: Why is this set if it's already in the collection?
                // TODO: Does this get set once per group or something? 
                parameters.Set(projectiveTextureKey, lightParameters.ProjectionTexture);
                parameters.Set(uvScale, lightParameters.UVScale);
                parameters.Set(uvOffset, lightParameters.UVOffset);
                parameters.Set(worldToProjectiveTextureUVsKey, collectResultStruct.WorldToTextureUV);
                parameters.Set(projectorPlaneMatricesKey, collectResultStruct.ProjectorPlaneMatrices);
                parameters.Set(projectionTextureMipMapLevelsKey, collectResultStruct.ProjectionTextureMipMapLevels);
                parameters.Set(transitionAreasKey, collectResultStruct.TransitionAreas);
            }
        }
    }
}
