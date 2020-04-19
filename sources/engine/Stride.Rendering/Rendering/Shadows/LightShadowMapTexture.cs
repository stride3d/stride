// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Stride.Rendering.Shadows
{
    /// <summary>
    /// An allocated shadow map texture associated to a light.
    /// </summary>
    public class LightShadowMapTexture
    {
        public RenderView RenderView { get; private set; }

        public RenderLight RenderLight { get; private set; }

        public IDirectLight Light { get; private set; }

        public LightShadowMap Shadow { get; private set; }

        public Type FilterType { get; private set; }

        public byte TextureId { get; internal set; }

        public LightShadowType ShadowType { get; internal set; }

        public int Size { get; private set; }

        public int CascadeCount { get; set; }

        public float CurrentMinDistance
        {
            get { return currentMinDistance; }
            set
            {
                if (value <= DistanceMinRange)
                    return;

                currentMinDistance = value;
                if (currentMaxDistance < currentMinDistance + DistanceMinRange)
                    currentMaxDistance = currentMinDistance + DistanceMinRange;
            }
        }

        public float CurrentMaxDistance
        {
            get { return currentMaxDistance; }
            set
            {
                if (value <= DistanceMinRangeDouble)
                    return;

                currentMaxDistance = value;
                if (currentMinDistance > currentMaxDistance - DistanceMinRange)
                    currentMinDistance = currentMaxDistance - DistanceMinRange;
            }
        }

        private float currentMinDistance = 1f;

        private float currentMaxDistance = 2f;

        private const float DistanceMinRange = 0.001f;

        private const float DistanceMinRangeDouble = DistanceMinRange * 2;

        public ShadowMapAtlasTexture Atlas { get; internal set; }

        public ILightShadowMapRenderer Renderer;

        public ILightShadowMapShaderData ShaderData;

        public void Initialize(RenderView renderView, RenderLight renderLight, IDirectLight light, LightShadowMap shadowMap, int size, ILightShadowMapRenderer renderer)
        {
            if (renderView == null) throw new ArgumentNullException(nameof(renderView));
            if (renderLight == null) throw new ArgumentNullException(nameof(renderLight));
            if (light == null) throw new ArgumentNullException(nameof(light));
            if (shadowMap == null) throw new ArgumentNullException(nameof(shadowMap));
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));

            RenderView = renderView;
            RenderLight = renderLight;
            Light = light;
            Shadow = shadowMap;
            Size = size;
            FilterType = Shadow.Filter == null || !Shadow.Filter.RequiresCustomBuffer() ? null : Shadow.Filter.GetType();
            Renderer = renderer;
            Atlas = null; // Reset the atlas, It will be setup after
            CascadeCount = 1;

            ShadowType = renderer.GetShadowType(Shadow);
        }

        public Rectangle GetRectangle(int i)
        {
            if (i < 0 || i > CascadeCount || i > MaxRectangles)
            {
                throw new ArgumentOutOfRangeException("i", "Must be in the range [0, CascadeCount]");
            }
            unsafe
            {
                fixed (void* ptr = &rectangle0)
                {
                    return ((Rectangle*)ptr)[i];
                }
            }
        }

        public void SetRectangle(int i, Rectangle value)
        {
            if (i < 0 || i > CascadeCount || i > MaxRectangles)
            {
                throw new ArgumentOutOfRangeException("i", "Must be in the range [0, CascadeCount]");
            }
            unsafe
            {
                fixed (void* ptr = &rectangle0)
                {
                    ((Rectangle*)ptr)[i] = value;
                }
            }
        }

        // Even if C# things Rectangle1, Rectangle2 and Rectangle3 are not used,
        // they are indirectly in `GetRectangle' and `SetRectangle' through pointer
        // arithmetics.
        // MaxRectangles should be updated to match the actual number of rectangles to detected out of range errors
        public const int MaxRectangles = 6;
        private Rectangle rectangle0;
#pragma warning disable 169
        private Rectangle rectangle1;
        private Rectangle rectangle2;
        private Rectangle rectangle3;
        private Rectangle rectangle4;
        private Rectangle rectangle5;
#pragma warning restore 169

    }
}
