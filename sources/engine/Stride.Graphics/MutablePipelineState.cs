// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;

namespace Stride.Graphics
{
    using DictionaryOfPipelineStatesByDescription = Dictionary<PipelineStateDescriptionWithHash, PipelineState>;

    public class MutablePipelineState
    {
        private readonly GraphicsDevice graphicsDevice;

        // Per-device cache for already known compiled Pipeline States
        private readonly DictionaryOfPipelineStatesByDescription cache;

        public PipelineStateDescription State { get; } = new();

        /// <summary>
        /// Current compiled state.
        /// </summary>
        public PipelineState? CurrentState { get; private set; }


        public MutablePipelineState(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            // Create a per-device cache for Pipeline States
            cache = graphicsDevice.GetOrCreateSharedData(typeof(PipelineStateCache), device => new PipelineStateCache());

            // Start with the default Pipeline State
            State.SetDefaults();
        }


        /// <summary>
        /// Determine and updates <see cref="CurrentState"/> from <see cref="State"/>.
        /// </summary>
        public void Update()
        {
            // Hash current state
            var hashedState = new PipelineStateDescriptionWithHash(State);

            // Find existing PipelineState object
            PipelineState pipelineState;

            // TODO: GRAPHICS REFACTOR: We could avoid lock by adding them to a ThreadLocal (or RenderContext) and merge at end of frame
            lock (cache)
            {
                if (!cache.TryGetValue(hashedState, out pipelineState))
                {
                    // Otherwise, add it to the cache (a clone, so we can still keep mutating our copy)
                    hashedState = new PipelineStateDescriptionWithHash(State.Clone());
                    cache.Add(hashedState, pipelineState = PipelineState.New(graphicsDevice, State));
                }
            }

            CurrentState = pipelineState;
        }

        #region PipelineStateCache

        private class PipelineStateCache : DictionaryOfPipelineStatesByDescription, IDisposable
        {
            public void Dispose()
            {
                foreach (var pipelineState in Values)
                {
                    ((IReferencable) pipelineState).Release();
                }
                Clear();
            }
        }

        #endregion
    }
}
