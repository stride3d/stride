// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.ReferenceCounting;
using System.Threading;

namespace Xenko.Graphics
{
    public partial class PipelineState : GraphicsResourceBase
    {
        public enum PIPELINE_STATE {
            LOADING = 0,
            READY = 1,
            ERROR = 2
        };

        public int InputBindingCount { get; private set; }

        public static PipelineState New(GraphicsDevice graphicsDevice, ref PipelineStateDescription pipelineStateDescription)
        {
            PipelineState pipelineState = null;

            // Hash the current state
            var hashedState = new PipelineStateDescriptionWithHash(pipelineStateDescription);

            // check if it is in the cache, or being worked on...
            bool foundInCache = false;

            lock (graphicsDevice.CachedPipelineStates) {
                foundInCache = graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState);
                if (!foundInCache) graphicsDevice.CachedPipelineStates[hashedState] = null; // mark we will work on this pipeline
            }

            // if we have this cached, wait until it is ready to return
            if (foundInCache) {
                while (pipelineState == null) {
                    Thread.Sleep(1);
                    if (graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState) == false) {
                        // how did this happen?
                        break;
                    }
                }
                if (pipelineState != null) {
                    while (pipelineState.CurrentState() == PIPELINE_STATE.LOADING) {
                        Thread.Sleep(1);
                    }
                    pipelineState.AddReferenceInternal();
                    return pipelineState;
                }
            }

            if (GraphicsDevice.Platform == GraphicsPlatform.Vulkan) {
                // if we are using Vulkan, just make a new pipeline without locking
                pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription);
            } else {
                // D3D seems to have quite bad concurrency when using CreateSampler while rendering
                lock (graphicsDevice.CachedPipelineStates) {
                    pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription);
                }
            }

            // put the completed pipeline in the cache
            graphicsDevice.CachedPipelineStates[hashedState] = pipelineState;

            return pipelineState;
        }
    }
}
