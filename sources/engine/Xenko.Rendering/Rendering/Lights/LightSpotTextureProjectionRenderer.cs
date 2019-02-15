// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    public interface ITextureProjectionShaderGroupData // TODO: Move to separate file!
    {
        void ApplyShader(ShaderMixinSource mixin);
        void UpdateLayout(string compositionName);
        void UpdateLightCount(int lightLastCount, int lightCurrentCount);
        void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox);
    }

    /// <summary>
    /// Interface to project a texture onto geometry.
    /// </summary>
    public interface ITextureProjectionRenderer // TODO: Move to separate file!
    {
        ITextureProjectionShaderGroupData CreateShaderGroupData();
        void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap);
    }

    public struct SpotLightTextureParameters
    {
        public Texture ProjectionTexture;
        public LightSpot.FlipModeEnum FlipMode;
        public Vector2 UVScale;
        public Vector2 UVOffset;

        public static SpotLightTextureParameters Default = new SpotLightTextureParameters
        {
            ProjectionTexture = null,
            FlipMode = LightSpot.FlipModeEnum.None,
            UVScale = Vector2.One,
            UVOffset = Vector2.Zero,
        };

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
            if (ReferenceEquals(null, obj)) return false;
            return obj is SpotLightTextureParameters && Equals((SpotLightTextureParameters)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ProjectionTexture != null ? ProjectionTexture.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)FlipMode;
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
        private PoolListStruct<LightSpotTextureProjectionShaderData> shaderDataPool;
        private readonly SpotLightTextureParameters lightParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpotTextureProjectionRenderer"/> class.
        /// </summary>
        public LightSpotTextureProjectionRenderer(SpotLightTextureParameters parameters)
        {
            // TODO: What is this?
            shaderDataPool = new PoolListStruct<LightSpotTextureProjectionShaderData>(8, CreateLightSpotTextureProjectionShaderData);
            lightParameters = parameters;
        }

        /*
        public override void Reset(RenderContext context)   // TODO: Implement this?
        {
            shaderDataPool.Clear();
        }
        */

        public ITextureProjectionShaderGroupData CreateShaderGroupData() // TODO: Override!
        {
            return new LightSpotTextureProjectionGroupShaderData(lightParameters);
        }

        // Computes the view-projection matrix without any offset or scaling cascade.
        private static Matrix ComputeWorldToTextureUVMatrix(RenderLight light)
        {
            var spotLight = (LightSpot)light.Type;

            Matrix viewMatrix = light.WorldMatrix;
            viewMatrix.Invert();

            // TODO: PERFORMANCE: This does redundant work. The view projection matrix is already calculated within "LightSpotShadowMapRenderer".

            // TODO: Calculation of near and far is hardcoded/approximated. We should find a better way to calculate it.
            var nearClip = 0.01f;   // TODO: This should be configurable.
            var farClip = spotLight.Range;  // Removed the multiplication by two because I didn't see the point in it.
            var projectionMatrix = Matrix.PerspectiveFovRH(spotLight.AngleOuterInRadians, spotLight.AspectRatio, nearClip, farClip); // Perspective Projection for spotlights
            Matrix viewProjectionMatrix;
            Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);

            // TODO: Add an offset so we don't have to do it in the shader?

            return viewProjectionMatrix; // View-projection matrix without offset to cascade.
        }

        // Computes the view-projection matrix without any offset or scaling cascade.
        private static Matrix ComputeProjectorPlaneMatrix(RenderLight light)
        {
            var spotLight = (LightSpot)light.Type;

            // Calculate the width and height of the near plane in world space:
            var nearClipDistance = Math.Min(spotLight.ProjectionPlaneDistance, spotLight.Range);
            float angleOuterInRadians = MathUtil.DegreesToRadians(Math.Max(spotLight.AngleInner, spotLight.AngleOuter));
            float height = (float)Math.Tan(angleOuterInRadians / 2.0f) * nearClipDistance;  // TODO: Is this correct?
            float width = height * spotLight.AspectRatio;  // TODO: Is this correct?

            Matrix viewMatrix = light.WorldMatrix;

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

        // TODO: This function is not being called anywhere. Find a way to integrate it and do the light attribute extraction here instead of within "ApplyDrawParameters()".
        public void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap) // TODO: Remove the shadow map parameter.
        {
            // Computes the cascade splits
            var lightComponent = lightShadowMap.RenderLight;
            var spotLight = (LightSpot)lightComponent.Type;

            // Get new shader data from pool
            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;

            shaderData.ProjectiveTextureMipMapLevel = (float)(lightParameters.ProjectionTexture.MipLevels - 1) * spotLight.MipMapScale; // "- 1" because the lowest mip level is 0, not 1.
            shaderData.WorldToTextureUV = ComputeWorldToTextureUVMatrix(lightComponent); // View-projection matrix without offset to cascade.
        }

        // TODO: Find a way to use this class!
        private class LightSpotTextureProjectionShaderData : ILightShadowMapShaderData
        {
            public float ProjectiveTextureMipMapLevel; 
            public Matrix WorldToTextureUV; 
        }

        public class LightSpotTextureProjectionGroupShaderData : ITextureProjectionShaderGroupData // TODO: Make private
        {
            private const string ShaderNameBase = "TextureProjectionReceiverSpot";
            private const string ShaderNameAttenuation = "LightSpotAttenuationRectangular";

            // Values:
            private Matrix[] worldToTextureUV;
            private Matrix[] projectorPlaneMatrices;
            private float[] projectionTextureMipMapLevels;
            private float[] transitionAreas;
            private readonly SpotLightTextureParameters lightParameters;

            // Keys:
            private ObjectParameterKey<Texture> projectiveTextureKey;
            private ValueParameterKey<Vector2> uvScale;
            private ValueParameterKey<Vector2> uvOffset;
            private ValueParameterKey<Matrix> worldToProjectiveTextureUVsKey;
            private ValueParameterKey<Matrix> projectorPlaneMatricesKey;
            private ValueParameterKey<float> projectionTextureMipMapLevelsKey;
            private ValueParameterKey<float> transitionAreasKey;
            public ShaderMixinSource TextureProjectionShader { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="LightSpotTextureProjectionGroupShaderData" /> class.
            /// </summary>
            /// <param name="shadowType">Type of the shadow.</param>
            /// <param name="lightCountMax">The light count maximum.</param>
            public LightSpotTextureProjectionGroupShaderData(SpotLightTextureParameters parameters)
            {
                lightParameters = parameters;
            }

            public virtual void ApplyShader(ShaderMixinSource mixin)
            {
                mixin.CloneFrom(TextureProjectionShader);
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
                TextureProjectionShader = new ShaderMixinSource();

                // Add the shader for projecting the texture:
                TextureProjectionShader.Mixins.Add(new ShaderClassSource(ShaderNameBase, "PerDraw.Lighting", lightCurrentCount, (int)lightParameters.FlipMode));

                // Add the rectangular attenuation shader so the light doesn't use angular attenuation anymore because we want to show the full, square texture:
                TextureProjectionShader.Mixins.Add(new ShaderClassSource(ShaderNameAttenuation));

                Array.Resize(ref worldToTextureUV, lightCurrentCount);
                Array.Resize(ref projectorPlaneMatrices, lightCurrentCount);
                Array.Resize(ref projectionTextureMipMapLevels, lightCurrentCount);
                Array.Resize(ref transitionAreas, lightCurrentCount);
            }

            public void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
            {
                var boundingBoxCasted = (BoundingBox)boundingBox;
                int lightIndex = 0;

                for (int i = 0; i < currentLights.Count; ++i)
                {
                    var lightEntry = currentLights[i];
                    var light = lightEntry.Light;

                    if (light.BoundingBox.Intersects(ref boundingBoxCasted))
                    {
                        var spotLight = (LightSpot)light.Type;

                        /*
                        // TODO: Just save the shaderdata struct directly within "LightDynamicEntry"?
                        var singleLightData = (LightSpotTextureProjectionShaderData)lightEntry.ShadowMapTexture.ShaderData;   // TODO: This must not depend on the shadow map texture!
                        
                        worldToTextureUV[lightIndex] = singleLightData.WorldToTextureUV;
                        projectionTextureMipMapLevels[lightIndex] = singleLightData.ProjectiveTextureMipMapLevel;
                        projectiveTexture = singleLightData.ProjectiveTexture;
                        */

                        // TODO: Move this to "Collect()" and use "LightSpotTextureProjectionShaderData", but IDK how!
                        worldToTextureUV[lightIndex] = ComputeWorldToTextureUVMatrix(light);
                        projectorPlaneMatrices[lightIndex] = ComputeProjectorPlaneMatrix(light);

                        // We use the maximum number of mips instead of the actual number,
                        // so things like video textures behave more consistently when changing the number of mip maps to generate.
                        int maxMipMapCount = Texture.CountMips(lightParameters.ProjectionTexture.Width, lightParameters.ProjectionTexture.Height);
                        float projectiveTextureMipMapLevel = (float)(maxMipMapCount - 1) * spotLight.MipMapScale; // "- 1" because the lowest mip level is 0, not 1.
                        projectionTextureMipMapLevels[lightIndex] = projectiveTextureMipMapLevel;
                        transitionAreas[lightIndex] = Math.Max(spotLight.TransitionArea, 0.001f);   // Keep the value just above zero. This is to prevent some issues with the "smoothstep()" function on OpenGL and OpenGL ES.

                        ++lightIndex;
                    }
                }

                // TODO: Why is this set if it's already in the collection?
                // TODO: Does this get set once per group or something? 
                parameters.Set(projectiveTextureKey, lightParameters.ProjectionTexture);
                parameters.Set(uvScale, lightParameters.UVScale);
                parameters.Set(uvOffset, lightParameters.UVOffset);
                parameters.Set(worldToProjectiveTextureUVsKey, worldToTextureUV); 
                parameters.Set(projectorPlaneMatricesKey, projectorPlaneMatrices);
                parameters.Set(projectionTextureMipMapLevelsKey, projectionTextureMipMapLevels);
                parameters.Set(transitionAreasKey, transitionAreas);
            }
        }

        private static LightSpotTextureProjectionShaderData CreateLightSpotTextureProjectionShaderData()
        {
            return new LightSpotTextureProjectionShaderData();
        }
    }
}
