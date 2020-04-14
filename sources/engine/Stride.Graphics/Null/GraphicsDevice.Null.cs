// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL 

namespace Stride.Graphics
{
    public partial class GraphicsDevice
    {
        /// <summary>
        /// Identification of the current graphic platform.
        /// </summary>
        /// <implement>To be implemented.</implement>
        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Null;
        private string rendererName = "Null";

        /// <summary>
        /// Buffer data placement alignment.
        /// </summary>
        /// <implement>To be implemented.</implement>
        internal int ConstantBufferDataPlacementAlignment = 256;

        /// <summary>
        /// Action called when device is destroyed.
        /// </summary>
        internal void OnDestroyed()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Increases usage of <param name="resourceLink"/> if its usage is dynamic.
        /// </summary>
        /// <param name="resourceLink">The resource link.</param>
        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Initializes this device.
        /// </summary>
        /// <param name="graphicsProfiles">The graphics profiles.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Gets status of this device.
        /// </summary>
        /// <value>The graphics device status.</value>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                NullHelper.ToImplement();
                return GraphicsDeviceStatus.Normal;
            }
        }

        /// <summary>
        /// Executes a deferred command list.
        /// </summary>
        /// <param name="commandList">The deferred command list.</param>
        public void ExecuteCommandList(CompiledCommandList commandList)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Executes multiple deferred command lists.
        /// </summary>
        /// <param name="count">Number of command lists to execute.</param>
        /// <param name="commandLists">The deferred command lists.</param>
        public void ExecuteCommandLists(int count, CompiledCommandList[] commandLists)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Begin new drawing on current device.
        /// </summary>
        public void Begin()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// End drawing on current device.
        /// </summary>
        public void End()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Adjust default pipeline state description.
        /// </summary>
        /// <param name="pipelineStateDescription">The pipeline state description to be adjusted.</param>
        private void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Initialize post features.
        /// </summary>
        private void InitializePostFeatures()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Name of the renderer for the current device.
        /// </summary>
        /// <returns>Name of renderer.</returns>
        private string GetRendererName()
        {
            NullHelper.ToImplement();
            return rendererName;
        }

        /// <summary>
        /// Destroy device.
        /// </summary>
        /// <remarks>Called from <see cref="GraphicsDevice.Destroy"/></remarks>
        private void DestroyPlatformDevice()
        {
            NullHelper.ToImplement();
        }
    }
} 
#endif
