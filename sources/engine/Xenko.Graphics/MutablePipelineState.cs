// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xenko.Core;

namespace Xenko.Graphics
{
    public class MutablePipelineState
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly ConcurrentDictionary<PipelineStateDescriptionWithHash, PipelineState> cache;
        public PipelineStateDescription State;

        /// <summary>
        /// Current compiled state.
        /// </summary>
        public PipelineState CurrentState;

        public MutablePipelineState(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            cache = graphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, typeof(MutablePipelineStateCache), device => new MutablePipelineStateCache()).Cache;

            State = new PipelineStateDescription();
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

            if (!cache.TryGetValue(hashedState, out pipelineState))
            {
                // Otherwise, instantiate it
                cache.TryAdd(hashedState, pipelineState = PipelineState.New(graphicsDevice, ref State));
            }

            CurrentState = pipelineState;
        }

        private class MutablePipelineStateCache : IDisposable
        {
            public readonly ConcurrentDictionary<PipelineStateDescriptionWithHash, PipelineState> Cache = new ConcurrentDictionary<PipelineStateDescriptionWithHash, PipelineState>();

            public void Dispose()
            {
                foreach (var pipelineState in Cache)
                {
                    ((IReferencable)pipelineState.Value).Release();
                }

                Cache.Clear();
            }
        }
    }
}
