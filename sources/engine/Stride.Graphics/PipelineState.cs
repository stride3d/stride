// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.ReferenceCounting;

namespace Stride.Graphics
{
    public partial class PipelineState : GraphicsResourceBase
    {
        public int InputBindingCount { get; private set; }

        public static PipelineState New(GraphicsDevice graphicsDevice, ref PipelineStateDescription pipelineStateDescription)
        {
            // Hash the current state
            var hashedState = new PipelineStateDescriptionWithHash(pipelineStateDescription);

            // Store SamplerState in a cache (D3D seems to have quite bad concurrency when using CreateSampler while rendering)
            PipelineState pipelineState;
            lock (graphicsDevice.CachedPipelineStates)
            {
                if (graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState))
                {
                    // TODO: Appropriate destroy
                    pipelineState.AddReferenceInternal();
                }
                else
                {
                    pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription);
                    graphicsDevice.CachedPipelineStates.Add(hashedState, pipelineState);
                }
            }
            return pipelineState;
        }
    }
}
