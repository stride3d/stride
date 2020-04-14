// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Rendering.Shadows;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Base class for light renderers.
    /// </summary>
    [DataContract(Inherited = true, DefaultMemberMode = DataMemberMode.Never)]
    public abstract class LightGroupRendererBase
    {
        private static readonly Dictionary<Type, int> LightRendererIds = new Dictionary<Type, int>();

        protected LightGroupRendererBase()
        {
            int lightRendererId;
            lock (LightRendererIds)
            {
                if (!LightRendererIds.TryGetValue(GetType(), out lightRendererId))
                {
                    lightRendererId = LightRendererIds.Count + 1;
                    LightRendererIds.Add(GetType(), lightRendererId);
                }
            }

            LightRendererId = (byte)lightRendererId;
        }

        public bool IsEnvironmentLight { get; protected set; } // TODO: This shouldn't be here. This should be moved to the LightGroupRendererDynamic class at least.

        public byte LightRendererId { get; private set; }

        public abstract Type[] LightTypes { get; }

        public virtual void Initialize(RenderContext context)
        {
        }

        public virtual void Unload()
        {
        }

        public virtual void Reset()
        {
        }

        public virtual void SetViews(FastList<RenderView> views)
        {
        }

        public abstract void ProcessLights(ProcessLightsParameters parameters);

        public struct ProcessLightsParameters
        {
            public RenderDrawContext Context;

            // Information about the view
            public int ViewIndex;
            public RenderView View;
            public FastList<RenderView> Views;

            // Current renderers in this group
            public LightGroupRendererBase[] Renderers;
            // Index into the Renderers array
            public int RendererIndex;
            
            public RenderLightCollection LightCollection;
            public Type LightType;
            
            // Light range to process in LightCollection
            // The light group renderer should remove lights it processes
            public List<int> LightIndices;

            public IShadowMapRenderer ShadowMapRenderer;

            public Dictionary<RenderLight, LightShadowMapTexture> ShadowMapTexturesPerLight;
        }

        public abstract void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry);

        public virtual void PrepareResources(RenderDrawContext drawContext)
        {
        }
    }
}
