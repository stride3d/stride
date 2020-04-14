// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xenko.Graphics;

namespace Xenko.Rendering
{
    /// <summary>
    /// Instantiation of an Effect for a given <see cref="StaticEffectObjectNodeReference"/>.
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
        /// Compiled pipeline state.
        /// </summary>
        public PipelineState PipelineState;

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
    }
}
