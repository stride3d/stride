// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_NULL 

namespace Xenko.Graphics
{
    public partial class PipelineState
    {
        internal PipelineState(GraphicsDevice device) : base(device) {
            // just return a memory address to Prepare later
        }

        /// <summary>
        /// Initializes new instance of <see cref="PipelineState"/> for <param name="device"/>
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="pipelineStateDescription">The pipeline state description.</param>
        internal void Prepare(PipelineStateDescription pipelineStateDescription) : base(device)
        {
            NullHelper.ToImplement();
        }

        public PIPELINE_STATE CurrentState() {
            return PIPELINE_STATE.READY;
        }
    }
}

#endif
