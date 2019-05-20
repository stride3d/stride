// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.ReferenceCounting;

namespace Xenko.Graphics
{
    public partial class PipelineState : GraphicsResourceBase
    {
        public int InputBindingCount { get; private set; }

        public static PipelineState New(GraphicsDevice graphicsDevice, ref PipelineStateDescription pipelineStateDescription)
        {
            PipelineState pipelineState;

            // Hash the current state
            var hashedState = new PipelineStateDescriptionWithHash(pipelineStateDescription);

            // if we are using Vulkan, just make a new pipeline without locking
            if (GraphicsDevice.Platform == GraphicsPlatform.Vulkan) {
                if (graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState)) {
                    // TODO: Appropriate destroy
                    pipelineState.AddReferenceInternal();
                } else {
                    graphicsDevice.CachedPipelineStates.TryAdd(hashedState, pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription));
                }
            } else {
                // Store SamplerState in a cache (D3D seems to have quite bad concurrency when using CreateSampler while rendering)
                lock (graphicsDevice.CachedPipelineStates) {
                    if (graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState)) {
                        // TODO: Appropriate destroy
                        pipelineState.AddReferenceInternal();
                    } else {
                        pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription);
                        graphicsDevice.CachedPipelineStates.TryAdd(hashedState, pipelineState);
                    }
                }
            }

            return pipelineState;
        }
    }
}
