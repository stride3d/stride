// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.ReferenceCounting;

namespace Stride.Graphics
{
    public partial class SamplerState : GraphicsResourceBase
    {
        /// <summary>
        /// Gets the sampler state description.
        /// </summary>
        public readonly SamplerStateDescription Description;

        // For FakeSamplerState.
        protected SamplerState()
        {
        }

        // For FakeSamplerState.
        private SamplerState(SamplerStateDescription description)
        {
            Description = description;
        }

        public static SamplerState New(GraphicsDevice graphicsDevice, SamplerStateDescription samplerStateDescription)
        {
            // Store SamplerState in a cache (D3D seems to have quite bad concurrency when using CreateSampler while rendering)
            SamplerState samplerState;
            lock (graphicsDevice.CachedSamplerStates)
            {
                if (graphicsDevice.CachedSamplerStates.TryGetValue(samplerStateDescription, out samplerState))
                {
                    // TODO: Appropriate destroy
                    samplerState.AddReferenceInternal();
                }
                else
                {
                    samplerState = new SamplerState(graphicsDevice, samplerStateDescription);
                    graphicsDevice.CachedSamplerStates.Add(samplerStateDescription, samplerState);
                }
            }
            return samplerState;
        }
        
        /// <summary>
        /// Create a new fake sampler state for serialization.
        /// </summary>
        /// <param name="description">The description of the sampler state</param>
        /// <returns>The fake sampler state</returns>
        public static SamplerState NewFake(SamplerStateDescription description)
        {
            return new SamplerState(description);
        }

        protected override void Destroy()
        {
            lock (GraphicsDevice.CachedSamplerStates)
            {
                GraphicsDevice.CachedSamplerStates.Remove(Description);
            }

            base.Destroy();
        }
    }
}
