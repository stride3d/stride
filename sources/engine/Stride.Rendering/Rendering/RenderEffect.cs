// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Instantiation of an Effect for a given <see cref="EffectObjectNodeReference"/>.
    /// </summary>
    public class RenderEffect
    {
        // Request effect selector
        public readonly EffectSelector EffectSelector;

        public int LastFrameUsed { get; private set; }

        /// <summary>
        /// Describes what state the effect is in (compiling, error, etc..)
        /// </summary>
        public RenderEffectState State;

        /// <summary>
        /// Describes when to try again after a previous error (UTC).
        /// </summary>
        public DateTime RetryTime = DateTime.MaxValue;

        public bool IsReflectionUpdateRequired;

        public Effect Effect;
        public RenderEffectReflection Reflection;

        /// <summary>
        /// Compiled pipeline state for <see cref="pipelineStateOutput"/>. Fast path for the common case
        /// where the effect is only drawn into a single output format.
        /// </summary>
        public PipelineState PipelineState;

        private RenderOutputDescription pipelineStateOutput;

        /// <summary>
        /// Additional pipeline states when the same effect is drawn into more than one output format
        /// (e.g. an object rendered both into an RGBA render-texture and a BGRA backbuffer). Lazily
        /// allocated; empty in the single-output case.
        /// </summary>
        private List<KeyValuePair<RenderOutputDescription, PipelineState>> extraPipelineStates;

        /// <summary>
        /// True if the pipeline writes depth (DepthBufferWriteEnable) or stencil (StencilWriteMask != 0).
        /// Captured from the PipelineStateDescription when the PipelineState is first built, used by
        /// RenderSystem.Draw to auto-detect a stage's depth access mode before worker fan-out.
        /// </summary>
        public bool WritesDepth;

        /// <summary>
        /// Validates if effect needs to be compiled or recompiled.
        /// </summary>
        public EffectValidator EffectValidator;

        /// <summary>
        /// Pending effect being compiled.
        /// </summary>
        public Task<Effect> PendingEffect;

        public EffectParameterUpdater FallbackParameterUpdater;
        public ParameterCollection FallbackParameters;

        public RenderEffect(EffectSelector effectSelector)
        {
            EffectSelector = effectSelector;
            EffectValidator.Initialize();
        }

        /// <summary>
        /// Mark effect as used during this frame.
        /// </summary>
        /// <returns>True if state changed (object was not mark as used during this frame until now), otherwise false.</returns>
        public bool MarkAsUsed(RenderSystem renderSystem)
        {
            if (LastFrameUsed == renderSystem.FrameCounter)
                return false;

            LastFrameUsed = renderSystem.FrameCounter;
            return true;
        }

        public bool IsUsedDuringThisFrame(RenderSystem renderSystem)
        {
            return LastFrameUsed == renderSystem.FrameCounter;
        }

        public void ClearFallbackParameters()
        {
            FallbackParameterUpdater = default(EffectParameterUpdater);
            FallbackParameters = null;
        }

        /// <summary>
        /// Gets the pipeline state cached for <paramref name="output"/>, or null if none was built yet.
        /// </summary>
        public PipelineState GetPipelineState(in RenderOutputDescription output)
        {
            if (PipelineState != null && pipelineStateOutput == output)
                return PipelineState;

            if (extraPipelineStates != null)
            {
                foreach (var entry in extraPipelineStates)
                {
                    if (entry.Key == output)
                        return entry.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Caches <paramref name="pipelineState"/> for <paramref name="output"/>, so the same effect
        /// drawn into views with different target formats keeps a matching pipeline state per format.
        /// </summary>
        public void SetPipelineState(in RenderOutputDescription output, PipelineState pipelineState)
        {
            if (PipelineState == null || pipelineStateOutput == output)
            {
                PipelineState = pipelineState;
                pipelineStateOutput = output;
                return;
            }

            extraPipelineStates ??= new List<KeyValuePair<RenderOutputDescription, PipelineState>>();
            for (int i = 0; i < extraPipelineStates.Count; i++)
            {
                if (extraPipelineStates[i].Key == output)
                {
                    extraPipelineStates[i] = new KeyValuePair<RenderOutputDescription, PipelineState>(output, pipelineState);
                    return;
                }
            }
            extraPipelineStates.Add(new KeyValuePair<RenderOutputDescription, PipelineState>(output, pipelineState));
        }

        /// <summary>
        /// Invalidates all cached pipeline states (e.g. after the effect is recompiled or its input layout changes).
        /// </summary>
        public void InvalidatePipelineState()
        {
            PipelineState = null;
            extraPipelineStates?.Clear();
        }
    }
}
