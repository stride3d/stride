// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Stride.Core;

namespace Stride.Graphics
{
    using DictionaryOfPipelineStatesByDescription = Dictionary<PipelineStateDescriptionWithHash, PipelineState>;

    /// <summary>
    ///   A convenience class that allows to compose a Pipeline State by modifying its configuration
    ///   (as <see cref="PipelineState"/> objects are immutable once compiled), and only compile it
    ///   into a Pipeline State object when needed.
    /// </summary>
    /// <remarks>
    ///   To minimize the creation of <see cref="PipelineState"/> objects, they are cached internally
    ///   per Graphics Device based on their description, so subsequent uses of a Pipeline State with
    ///   the same description uses the same instance instead of allocating a new one.
    /// </remarks>
    public class MutablePipelineState
    {
        private readonly GraphicsDevice graphicsDevice;

        // Per-device cache for already known compiled Pipeline States
        private readonly DictionaryOfPipelineStatesByDescription cache;

        /// <summary>
        ///   Gets the description of the current Pipeline State.
        /// </summary>
        public PipelineStateDescription State { get; } = new();

        /// <summary>
        ///   Gets the current compiled Pipeline State.
        /// </summary>
        /// <value>
        ///   The current compiled Pipeline State.
        ///   If this instance have not compiled any Pipeline State, the value will be <see langword="null"/>.
        /// </value>
        public PipelineState? CurrentState { get; private set; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="MutablePipelineState"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device.</param>
        public MutablePipelineState(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            // Create a per-device cache for Pipeline States
            cache = graphicsDevice.GetOrCreateSharedData(typeof(PipelineStateCache), device => new PipelineStateCache());

            // Start with the default Pipeline State
            State.SetDefaults();
        }


        /// <summary>
        ///   Determines if the mutable Pipeline State has changed, and updates the <see cref="CurrentState"/>.
        /// </summary>
        /// <remarks>
        ///   This method uses the current mutable Pipeline State to look for a cached <see cref="PipelineState"/>
        ///   with the same description. If found, it is returned. If not, a new Pipeline State object is compiled
        ///   from the current description (<see cref="State"/>), and cached for subsequent uses.
        /// </remarks>
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

        /// <summary>
        ///   A cache of compiled Pipeline States identified by their description (hashed).
        /// </summary>
        [DebuggerDisplay("", Name = $"{nameof(MutablePipelineState)}::{nameof(PipelineStateCache)}")]
        private class PipelineStateCache : DictionaryOfPipelineStatesByDescription, IDisposable
        {
            /// <inheritdoc/>
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
