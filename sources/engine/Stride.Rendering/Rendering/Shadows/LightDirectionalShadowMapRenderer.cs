// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Lights;
using Xenko.Shaders;

namespace Xenko.Rendering.Shadows
{
    /// <summary>
    /// Renders a shadow map from a directional light.
    /// </summary>
    public class LightDirectionalShadowMapRenderer : LightShadowMapRendererBase
    {
        private const float DepthIncreaseThreshold = 1.1f;
        private const float DepthDecreaseThreshold = 2.0f;

        /// <summary>
        /// The various UP vectors to try.
        /// </summary>
        private static readonly Vector3[] VectorUps = { Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ };

        /// <summary>
        /// Base points for frustum corners.
        /// </summary>
        private static readonly Vector3[] FrustumBasePoints =
        {
            new Vector3(-1.0f, -1.0f, 0.0f), new Vector3(1.0f, -1.0f, 0.0f), new Vector3(-1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f),
            new Vector3(-1.0f, -1.0f, 1.0f), new Vector3(1.0f, -1.0f, 1.0f), new Vector3(-1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f),
        };

        private readonly float[] cascadeSplitRatios;
        private readonly Vector3[] cascadeFrustumCornersWS;
        private readonly Vector3[] cascadeFrustumCornersVS;
        private readonly Vector3[] frustumCornersWS;
        private readonly Vector3[] frustumCornersVS;

        private PoolListStruct<ShaderData> shaderDataPoolCascade1;
        private PoolListStruct<ShaderData> shaderDataPoolCascade2;
        private PoolListStruct<ShaderData> shaderDataPoolCascade4;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightDirectionalShadowMapRenderer"/> class.
        /// </summary>
        public LightDirectionalShadowMapRenderer()
        {
            cascadeSplitRatios = new float[4];
            cascadeFrustumCornersWS = new Vector3[8];
            cascadeFrustumCornersVS = new Vector3[8];
            frustumCornersWS = new Vector3[8];
            frustumCornersVS = new Vector3[8];
            shaderDataPoolCascade1 = new PoolListStruct<ShaderData>(4, CreateLightDirectionalShadowMapShaderDataCascade1);
            shaderDataPoolCascade2 = new PoolListStruct<ShaderData>(4, CreateLightDirectionalShadowMapShaderDataCascade2);
            shaderDataPoolCascade4 = new PoolListStruct<ShaderData>(4, CreateLightDirectionalShadowMapShaderDataCascade4);
        }

        public override void Reset(RenderContext context)
        {
            base.Reset(context);

            shaderDataPoolCascade1.Clear();
            shaderDataPoolCascade2.Clear();
            shaderDataPoolCascade4.Clear();
        }

        public override LightShadowType GetShadowType(LightShadowMap shadowMapArg)
        {
            var shadowMap = (LightDirectionalShadowMap)shadowMapArg;

            var shadowType = base.GetShadowType(shadowMapArg);

            if (shadowMap.DepthRange.IsAutomatic)
            {
                shadowType |= LightShadowType.DepthRangeAuto;
            }

            if (shadowMap.ComputeTransmittance)
            {
                shadowType |= LightShadowType.ComputeTransmittance;
            }

            if (shadowMap.DepthRange.IsBlendingCascades)
            {
                shadowType |= LightShadowType.BlendCascade;
            }

            return shadowType;
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType)
        {
            return new ShaderGroupData(shadowType);
        }

        public override bool CanRenderLight(IDirectLight light)
        {
            return light is LightDirectional;
        }

        public override LightShadowMapTexture CreateShadowMapTexture(RenderView renderView, RenderLight renderLight, IDirectLight light, int shadowMapSize)
        {
            var shadowMap = base.CreateShadowMapTexture(renderView, renderLight, light, shadowMapSize);
            shadowMap.CascadeCount = ((LightDirectionalShadowMap)light.Shadow).GetCascadeCount();
            // Views with orthographic cameras cannot use cascades, we force it to 1 shadow map here.
            if (renderView.Projection.M44 == 1.0f)
            {
                shadowMap.ShadowType &= ~(LightShadowType.CascadeMask);
                shadowMap.ShadowType |= LightShadowType.Cascade1;
                shadowMap.CascadeCount = (int)LightShadowMapCascadeCount.OneCascade;
            }
            return shadowMap;
        }

        public override void Collect(RenderContext context, RenderView sourceView, LightShadowMapTexture lightShadowMap)
        {
            var shadow = (LightDirectionalShadowMap)lightShadowMap.Shadow;

            // TODO: Min and Max distance can be auto-computed from readback from Z buffer

            Matrix.Invert(ref sourceView.View, out var viewToWorld);

            // Update the frustum infos
            UpdateFrustum(sourceView);

            // Computes the cascade splits
            var minMaxDistance = ComputeCascadeSplits(context, sourceView, ref lightShadowMap);
            var direction = lightShadowMap.RenderLight.Direction;

            // Fake value
            // It will be setup by next loop
            Vector3 side = Vector3.UnitX;
            Vector3 upDirection = Vector3.UnitX;

            // Select best Up vector
            // TODO: User preference?
            foreach (var vectorUp in VectorUps)
            {
                if (Math.Abs(Vector3.Dot(direction, vectorUp)) < (1.0 - 0.0001))
                {
                    side = Vector3.Normalize(Vector3.Cross(vectorUp, direction));
                    upDirection = Vector3.Normalize(Vector3.Cross(direction, side));
                    break;
                }
            }

            int cascadeCount = lightShadowMap.CascadeCount;

            // Get new shader data from pool
            ShaderData shaderData;
            if (cascadeCount == 1)
            {
                shaderData = shaderDataPoolCascade1.Add();
            }
            else if (cascadeCount == 2)
            {
                shaderData = shaderDataPoolCascade2.Add();
            }
            else
            {
                shaderData = shaderDataPoolCascade4.Add();
            }
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            shaderData.DepthBias = shadow.BiasParameters.DepthBias;
            shaderData.OffsetScale = shadow.BiasParameters.NormalOffsetScale;

            float splitMaxRatio = (minMaxDistance.X - sourceView.NearClipPlane) / (sourceView.FarClipPlane - sourceView.NearClipPlane);
            float splitMinRatio = 0;
            for (int cascadeLevel = 0; cascadeLevel < cascadeCount; ++cascadeLevel)
            {
                var oldSplitMinRatio = splitMinRatio;
                // Calculate frustum corners for this cascade
                splitMinRatio = splitMaxRatio;
                splitMaxRatio = cascadeSplitRatios[cascadeLevel];

                for (int j = 0; j < 4; j++)
                {
                    // Calculate frustum in WS and VS
                    float overlap = 0;
                    if (cascadeLevel > 0 && shadow.DepthRange.IsBlendingCascades)
                        overlap = 0.2f * (splitMinRatio - oldSplitMinRatio);

                    var frustumRangeWS = frustumCornersWS[j + 4] - frustumCornersWS[j];
                    var frustumRangeVS = frustumCornersVS[j + 4] - frustumCornersVS[j];

                    cascadeFrustumCornersWS[j] = frustumCornersWS[j] + frustumRangeWS * (splitMinRatio - overlap);
                    cascadeFrustumCornersWS[j + 4] = frustumCornersWS[j] + frustumRangeWS * splitMaxRatio;

                    cascadeFrustumCornersVS[j] = frustumCornersVS[j] + frustumRangeVS * (splitMinRatio - overlap);
                    cascadeFrustumCornersVS[j + 4] = frustumCornersVS[j] + frustumRangeVS * splitMaxRatio;
                }

                Vector3 cascadeMinBoundLS;
                Vector3 cascadeMaxBoundLS;
                Vector3 target;

                if (shadow.StabilizationMode == LightShadowMapStabilizationMode.ViewSnapping || shadow.StabilizationMode == LightShadowMapStabilizationMode.ProjectionSnapping)
                {
                    // Make sure we are using the same direction when stabilizing
                    var boundingVS = BoundingSphere.FromPoints(cascadeFrustumCornersVS);

                    // Compute bounding box center & radius
                    target = Vector3.TransformCoordinate(boundingVS.Center, viewToWorld);
                    var radius = boundingVS.Radius;

                    //if (shadow.AutoComputeMinMax)
                    //{
                    //    var snapRadius = (float)Math.Ceiling(radius / snapRadiusValue) * snapRadiusValue;
                    //    Debug.WriteLine("Radius: {0} SnapRadius: {1} (snap: {2})", radius, snapRadius, snapRadiusValue);
                    //    radius = snapRadius;
                    //}

                    cascadeMaxBoundLS = new Vector3(radius, radius, radius);
                    cascadeMinBoundLS = -cascadeMaxBoundLS;

                    if (shadow.StabilizationMode == LightShadowMapStabilizationMode.ViewSnapping)
                    {
                        // Snap camera to texel units (so that shadow doesn't jitter when light doesn't change direction but camera is moving)
                        // Technique from ShaderX7 - Practical Cascaded Shadows Maps -  p310-311
                        var shadowMapHalfSize = lightShadowMap.Size * 0.5f;
                        float x = (float)Math.Ceiling(Vector3.Dot(target, upDirection) * shadowMapHalfSize / radius) * radius / shadowMapHalfSize;
                        float y = (float)Math.Ceiling(Vector3.Dot(target, side) * shadowMapHalfSize / radius) * radius / shadowMapHalfSize;
                        float z = Vector3.Dot(target, direction);

                        //target = up * x + side * y + direction * R32G32B32_Float.Dot(target, direction);
                        target = upDirection * x + side * y + direction * z;
                    }
                }
                else
                {
                    var cascadeBoundWS = BoundingBox.FromPoints(cascadeFrustumCornersWS);
                    target = cascadeBoundWS.Center;

                    // Computes the bouding box of the frustum cascade in light space
                    var lightViewMatrix = Matrix.LookAtRH(target, target + direction, upDirection);
                    cascadeMinBoundLS = new Vector3(float.MaxValue);
                    cascadeMaxBoundLS = new Vector3(-float.MaxValue);
                    for (int i = 0; i < cascadeFrustumCornersWS.Length; i++)
                    {
                        Vector3 cornerViewSpace;
                        Vector3.TransformCoordinate(ref cascadeFrustumCornersWS[i], ref lightViewMatrix, out cornerViewSpace);

                        cascadeMinBoundLS = Vector3.Min(cascadeMinBoundLS, cornerViewSpace);
                        cascadeMaxBoundLS = Vector3.Max(cascadeMaxBoundLS, cornerViewSpace);
                    }

                    // TODO: Adjust orthoSize by taking into account filtering size
                }

                // Update the shadow camera. The calculation of the eye position assumes RH coordinates.
                var viewMatrix = Matrix.LookAtRH(target - direction * cascadeMaxBoundLS.Z, target, upDirection); // View;;
                var nearClip = 0.0f;
                var farClip = cascadeMaxBoundLS.Z - cascadeMinBoundLS.Z;
                var projectionMatrix = Matrix.OrthoOffCenterRH(cascadeMinBoundLS.X, cascadeMaxBoundLS.X, cascadeMinBoundLS.Y, cascadeMaxBoundLS.Y, nearClip, farClip); // Projection
                Matrix viewProjectionMatrix;
                Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);

                // Stabilize the Shadow matrix on the projection
                if (shadow.StabilizationMode == LightShadowMapStabilizationMode.ProjectionSnapping)
                {
                    var shadowPixelPosition = viewProjectionMatrix.TranslationVector * lightShadowMap.Size * 0.5f; // shouln't it be scale and not translation ?
                    shadowPixelPosition.Z = 0;
                    var shadowPixelPositionRounded = new Vector3((float)Math.Round(shadowPixelPosition.X), (float)Math.Round(shadowPixelPosition.Y), 0.0f);

                    var shadowPixelOffset = new Vector4(shadowPixelPositionRounded - shadowPixelPosition, 0.0f);
                    shadowPixelOffset *= 2.0f / lightShadowMap.Size;
                    projectionMatrix.Row4 += shadowPixelOffset;
                    Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);
                }

                shaderData.ViewMatrix[cascadeLevel] = viewMatrix;
                shaderData.ProjectionMatrix[cascadeLevel] = projectionMatrix;
                shaderData.DepthRange[cascadeLevel] = new Vector2(nearClip, farClip);   //////////////////////

                // Cascade splits in light space using depth: Store depth on first CascaderCasterMatrix in last column of each row
                shaderData.CascadeSplits[cascadeLevel] = MathUtil.Lerp(sourceView.NearClipPlane, sourceView.FarClipPlane, cascadeSplitRatios[cascadeLevel]);

                var shadowMapRectangle = lightShadowMap.GetRectangle(cascadeLevel);

                var cascadeTextureCoords = new Vector4((float)shadowMapRectangle.Left / lightShadowMap.Atlas.Width,
                    (float)shadowMapRectangle.Top / lightShadowMap.Atlas.Height,
                    (float)shadowMapRectangle.Right / lightShadowMap.Atlas.Width,
                    (float)shadowMapRectangle.Bottom / lightShadowMap.Atlas.Height);

                shaderData.TextureCoords[cascadeLevel] = cascadeTextureCoords;

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
                Matrix adjustmentMatrix = Matrix.Scaling(leftX, -leftY, 1.0f) * Matrix.Translation(centerX, centerY, 0.0f);
                // Calculate View Proj matrix from World space to Cascade space
                Matrix.Multiply(ref viewProjectionMatrix, ref adjustmentMatrix, out shaderData.WorldToShadowCascadeUV[cascadeLevel]);

                // Allocate shadow render view
                var shadowRenderView = CreateRenderView();
                shadowRenderView.RenderView = sourceView;
                shadowRenderView.ShadowMapTexture = lightShadowMap;
                shadowRenderView.Rectangle = shadowMapRectangle;
                shadowRenderView.View = viewMatrix;
                shadowRenderView.ViewSize = new Vector2(shadowMapRectangle.Width, shadowMapRectangle.Height);
                shadowRenderView.Projection = projectionMatrix;
                shadowRenderView.ViewProjection = viewProjectionMatrix;
                shadowRenderView.NearClipPlane = nearClip;
                shadowRenderView.FarClipPlane = farClip;

                // Add the render view for the current frame
                context.RenderSystem.Views.Add(shadowRenderView);
            }
        }

        private void UpdateFrustum(RenderView renderView)
        {
            Matrix.Invert(ref renderView.Projection, out var projectionToView);

            // Compute frustum-dependent variables (common for all shadow maps)
            Matrix.Invert(ref renderView.ViewProjection, out var projectionToWorld);

            // Transform Frustum corners in World Space (8 points) - algorithm is valid only if the view matrix does not do any kind of scale/shear transformation
            for (int i = 0; i < 8; ++i)
            {
                Vector3.TransformCoordinate(ref FrustumBasePoints[i], ref projectionToWorld, out frustumCornersWS[i]);
                Vector3.TransformCoordinate(ref FrustumBasePoints[i], ref projectionToView, out frustumCornersVS[i]);
            }
        }

        private static float ToLinearDepth(float depthZ, ref Matrix projectionMatrix)
        {
            // as projection matrix is RH we calculate it like this
            var denominator = (depthZ + projectionMatrix.M33);
            return projectionMatrix.M43 / denominator;
        }

        private static float LogFloor(float value, float newBase)
        {
            var result = Math.Log(value, newBase);
            result = Math.Floor(result);
            result = Math.Pow(newBase, result);
            return (float)result;
        }

        private static float LogCeiling(float value, float newBase)
        {
            var result = Math.Log(value, newBase);
            result = Math.Ceiling(result);
            result = Math.Pow(newBase, result);
            return (float)result;
        }

        private Vector2 ComputeCascadeSplits(RenderContext context, RenderView sourceView, ref LightShadowMapTexture lightShadowMap)
        {
            var shadow = (LightDirectionalShadowMap)lightShadowMap.Shadow;

            var cameraNear = sourceView.NearClipPlane;
            var cameraFar = sourceView.FarClipPlane;
            var cameraRange = cameraFar - cameraNear;

            var minDistance = cameraNear + LightDirectionalShadowMap.DepthRangeParameters.DefaultMinDistance;
            var maxDistance = cameraNear + LightDirectionalShadowMap.DepthRangeParameters.DefaultMaxDistance;

            if (shadow.DepthRange.IsAutomatic)
            {
                minDistance = Math.Max(sourceView.MinimumDistance, cameraNear);
                maxDistance = Math.Max(sourceView.MaximumDistance, minDistance);

                if (lightShadowMap.CurrentMinDistance <= 0)
                    lightShadowMap.CurrentMinDistance = minDistance;

                if (lightShadowMap.CurrentMaxDistance <= 0)
                    lightShadowMap.CurrentMaxDistance = maxDistance;

                // Increase the maximum depth in small logarithmic steps, decrease it in larger logarithmic steps
                var threshold = maxDistance > lightShadowMap.CurrentMaxDistance ? DepthIncreaseThreshold : DepthDecreaseThreshold;
                maxDistance = lightShadowMap.CurrentMaxDistance = LogCeiling(maxDistance / lightShadowMap.CurrentMaxDistance, threshold) * lightShadowMap.CurrentMaxDistance;

                // Increase/decrease the distance between maximum and minimum depth in small/large logarithmic steps
                var range = maxDistance - minDistance;
                var currentRange = lightShadowMap.CurrentMaxDistance - lightShadowMap.CurrentMinDistance;
                threshold = range > currentRange ? DepthIncreaseThreshold : DepthDecreaseThreshold;
                minDistance = maxDistance - LogCeiling(range / currentRange, threshold) * currentRange;
                minDistance = lightShadowMap.CurrentMinDistance = Math.Max(minDistance, cameraNear);
            }
            else
            {
                minDistance = cameraNear + shadow.DepthRange.ManualMinDistance;
                maxDistance = cameraNear + shadow.DepthRange.ManualMaxDistance;
            }

            var manualPartitionMode = shadow.PartitionMode as LightDirectionalShadowMap.PartitionManual;
            var logarithmicPartitionMode = shadow.PartitionMode as LightDirectionalShadowMap.PartitionLogarithmic;
            if (logarithmicPartitionMode != null)
            {
                var minZ = minDistance;
                var maxZ = maxDistance;

                var range = maxZ - minZ;
                var ratio = maxZ / minZ;
                var logRatio = MathUtil.Clamp(1.0f - logarithmicPartitionMode.PSSMFactor, 0.0f, 1.0f);

                for (int cascadeLevel = 0; cascadeLevel < lightShadowMap.CascadeCount; ++cascadeLevel)
                {
                    // Compute cascade split (between znear and zfar)
                    float distrib = (float)(cascadeLevel + 1) / lightShadowMap.CascadeCount;
                    float logZ = (float)(minZ * Math.Pow(ratio, distrib));
                    float uniformZ = minZ + range * distrib;
                    float distance = MathUtil.Lerp(uniformZ, logZ, logRatio);
                    cascadeSplitRatios[cascadeLevel] = distance;
                }
            }
            else if (manualPartitionMode != null)
            {
                if (lightShadowMap.CascadeCount == 1)
                {
                    cascadeSplitRatios[0] = minDistance + manualPartitionMode.SplitDistance1 * maxDistance;
                }
                else if (lightShadowMap.CascadeCount == 2)
                {
                    cascadeSplitRatios[0] = minDistance + manualPartitionMode.SplitDistance1 * maxDistance;
                    cascadeSplitRatios[1] = minDistance + manualPartitionMode.SplitDistance3 * maxDistance;
                }
                else if (lightShadowMap.CascadeCount == 4)
                {
                    cascadeSplitRatios[0] = minDistance + manualPartitionMode.SplitDistance0 * maxDistance;
                    cascadeSplitRatios[1] = minDistance + manualPartitionMode.SplitDistance1 * maxDistance;
                    cascadeSplitRatios[2] = minDistance + manualPartitionMode.SplitDistance2 * maxDistance;
                    cascadeSplitRatios[3] = minDistance + manualPartitionMode.SplitDistance3 * maxDistance;
                }
            }

            // Convert distance splits to ratios cascade in the range [0, 1]
            for (int i = 0; i < cascadeSplitRatios.Length; i++)
            {
                cascadeSplitRatios[i] = (cascadeSplitRatios[i] - cameraNear) / cameraRange;
            }

            return new Vector2(minDistance, maxDistance);
        }

        public class ShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;
            public readonly float[] CascadeSplits;
            public float DepthBias;
            public float OffsetScale;
            public readonly Matrix[] WorldToShadowCascadeUV;
            public readonly Matrix[] ViewMatrix;
            public readonly Matrix[] ProjectionMatrix;
            public readonly Vector2[] DepthRange;   ///////////////////////////////////////////////////
            public readonly Vector4[] TextureCoords;

            public ShaderData(int cascadeCount)
            {
                DepthRange = new Vector2[cascadeCount];
                CascadeSplits = new float[cascadeCount];
                WorldToShadowCascadeUV = new Matrix[cascadeCount];
                ViewMatrix = new Matrix[cascadeCount];
                ProjectionMatrix = new Matrix[cascadeCount];
                TextureCoords = new Vector4[cascadeCount];
            }
        }

        private class ShaderGroupData : LightShadowMapShaderGroupDataBase
        {
            private const string ShaderName = "ShadowMapReceiverDirectional";
            private readonly int cascadeCount;
            private float[] cascadeSplits;
            private Matrix[] worldToShadowCascadeUV;
            private Matrix[] inverseWorldToShadowCascadeUV; /////////////////////////////////////////////////// Required for calculating the correct sampling offset for calculating the thickness.
            private Vector2[] depthRanges;    ///////////////////////////////////////////////////
            private float[] depthBiases;
            private float[] offsetScales;
            private Texture shadowMapTexture;
            private Vector2 shadowMapTextureSize;
            private Vector2 shadowMapTextureTexelSize;
            private ObjectParameterKey<Texture> shadowMapTextureKey;
            private ValueParameterKey<float> cascadeSplitsKey;
            private ValueParameterKey<Matrix> worldToShadowCascadeUVsKey;
            private ValueParameterKey<Matrix> inverseWorldToShadowCascadeUVsKey;    ///////////////////////////////////////////////////
            private ValueParameterKey<Vector2> depthRangesKey;       ///////////////////////////////////////////////////
            private ValueParameterKey<float> depthBiasesKey;
            private ValueParameterKey<float> offsetScalesKey;
            private ValueParameterKey<Vector2> shadowMapTextureSizeKey;
            private ValueParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShaderGroupData" /> class.
            /// </summary>
            /// <param name="shadowType">Type of the shadow.</param>
            /// <param name="lightCountMax">The light count maximum.</param>
            public ShaderGroupData(LightShadowType shadowType) : base(shadowType)
            {
                cascadeCount = 1 << ((int)(shadowType & LightShadowType.CascadeMask) - 1);
            }

            protected override string FilterMemberName { get; } = "PerView.Lighting";

            public override ShaderClassSource CreateShaderSource(int lightCurrentCount)
            {
                var isDepthRangeAuto = (ShadowType & LightShadowType.DepthRangeAuto) != 0;
                return new ShaderClassSource(ShaderName, cascadeCount, lightCurrentCount, (ShadowType & LightShadowType.BlendCascade) != 0, isDepthRangeAuto, (ShadowType & LightShadowType.Debug) != 0, (ShadowType & LightShadowType.ComputeTransmittance) != 0);
            }

            public override void UpdateLayout(string compositionKey)
            {
                shadowMapTextureKey = ShadowMapKeys.ShadowMapTexture.ComposeWith(compositionKey);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionKey);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionKey);
                cascadeSplitsKey = ShadowMapReceiverDirectionalKeys.CascadeDepthSplits.ComposeWith(compositionKey);
                worldToShadowCascadeUVsKey = ShadowMapReceiverBaseKeys.WorldToShadowCascadeUV.ComposeWith(compositionKey);
                inverseWorldToShadowCascadeUVsKey = ShadowMapReceiverBaseKeys.InverseWorldToShadowCascadeUV.ComposeWith(compositionKey);
                depthRangesKey = ShadowMapReceiverBaseKeys.DepthRanges.ComposeWith(compositionKey);      ///////////////////////////////////////////////////
                depthBiasesKey = ShadowMapReceiverBaseKeys.DepthBiases.ComposeWith(compositionKey);
                offsetScalesKey = ShadowMapReceiverBaseKeys.OffsetScales.ComposeWith(compositionKey);
            }

            public override void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                base.UpdateLightCount(lightLastCount, lightCurrentCount);

                Array.Resize(ref cascadeSplits, cascadeCount * lightCurrentCount);
                Array.Resize(ref worldToShadowCascadeUV, cascadeCount * lightCurrentCount);
                Array.Resize(ref inverseWorldToShadowCascadeUV, cascadeCount * lightCurrentCount);  ///////////////////////////////////////////////////
                Array.Resize(ref depthRanges, cascadeCount * lightCurrentCount);    ///////////////////////////////////////////////////
                Array.Resize(ref depthBiases, lightCurrentCount);
                Array.Resize(ref offsetScales, lightCurrentCount);
            }

            public override void ApplyViewParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights)
            {
                for (int lightIndex = 0; lightIndex < currentLights.Count; ++lightIndex)
                {
                    var lightEntry = currentLights[lightIndex];

                    var singleLightData = (ShaderData)lightEntry.ShadowMapTexture.ShaderData;
                    var splits = singleLightData.CascadeSplits;
                    var lightWorldToShadowCascadeUVs = singleLightData.WorldToShadowCascadeUV;
                    Vector2[] lightDepthRanges = singleLightData.DepthRange;
                    int splitIndex = lightIndex * cascadeCount;

                    for (int i = 0; i < splits.Length; i++)
                    {
                        int cascadeIndex = splitIndex + i;
                        cascadeSplits[cascadeIndex] = splits[i];
                        worldToShadowCascadeUV[cascadeIndex] = lightWorldToShadowCascadeUVs[i];
                        inverseWorldToShadowCascadeUV[cascadeIndex] = Matrix.Invert(lightWorldToShadowCascadeUVs[i]);  ///////////////////////////////////////////////////
                        depthRanges[cascadeIndex] = lightDepthRanges[i];    ///////////////////////////////////////////////////
                    }

                    depthBiases[lightIndex] = singleLightData.DepthBias;
                    offsetScales[lightIndex] = singleLightData.OffsetScale;

                    // TODO: should be setup just once at creation time
                    if (lightIndex == 0)
                    {
                        shadowMapTexture = singleLightData.Texture;
                        if (shadowMapTexture != null)
                        {
                            shadowMapTextureSize = new Vector2(shadowMapTexture.Width, shadowMapTexture.Height);
                            shadowMapTextureTexelSize = 1.0f / shadowMapTextureSize;
                        }
                    }
                }

                parameters.Set(shadowMapTextureKey, shadowMapTexture);
                parameters.Set(shadowMapTextureSizeKey, shadowMapTextureSize);
                parameters.Set(shadowMapTextureTexelSizeKey, shadowMapTextureTexelSize);
                parameters.Set(cascadeSplitsKey, cascadeSplits);
                parameters.Set(worldToShadowCascadeUVsKey, worldToShadowCascadeUV);
                parameters.Set(inverseWorldToShadowCascadeUVsKey, inverseWorldToShadowCascadeUV);   ///////////////////////////////////////////////////
                parameters.Set(depthRangesKey, depthRanges);  ///////////////////////////////////////////////////
                parameters.Set(depthBiasesKey, depthBiases);
                parameters.Set(offsetScalesKey, offsetScales);
            }
        }

        private static ShaderData CreateLightDirectionalShadowMapShaderDataCascade1()
        {
            return new ShaderData(1);
        }

        private static ShaderData CreateLightDirectionalShadowMapShaderDataCascade2()
        {
            return new ShaderData(2);
        }

        private static ShaderData CreateLightDirectionalShadowMapShaderDataCascade4()
        {
            return new ShaderData(4);
        }
    }
}
