// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        internal void OnDestroyed(bool immediately = false)
        {
            NullHelper.ToImplement();
        }

        //// <summary>
        ///   Tags a Graphics Resource as no having alive references, meaning it should be safe to dispose it
        ///   or discard its contents during the next <see cref="CommandList.MapSubResource"/> or <c>SetData</c> operation.
        /// </summary>
        /// <param name="resourceLink">
        ///   A <see cref="GraphicsResourceLink"/> object identifying the Graphics Resource along some related allocation information.
        /// </param>
        internal partial void TagResourceAsNotAlive(GraphicsResourceLink resourceLink)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Initialize the platform-specific implementation of the Graphics Device.
        /// </summary>
        /// <param name="graphicsProfiles">A non-<see langword="null"/> list of the graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private unsafe partial void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
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
        ///   Makes platform-specific adjustments to the Pipeline State objects created by the Graphics Device.
        /// </summary>
        /// <param name="pipelineStateDescription">A Pipeline State description that can be modified and adjusted.</param>
        private partial void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Initializes the platform-specific features of the Graphics Device once it has been fully initialized.
        /// </summary>
        private unsafe partial void InitializePostFeatures()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Gets a string that identifies the underlying device used by the Graphics Device to render.
        /// </summary>
        /// <remarks>
        ///   In the case of Direct3D and Vulkan, for example, this will return the name of the Graphics Adapter
        ///   (e.g. <c>"nVIDIA GeForce RTX 2080"</c>). Other platforms may return a different string.
        /// </remarks>
        private partial string GetRendererName()
        {
            NullHelper.ToImplement();
            return rendererName;
        }

        /// <summary>
        ///   Releases the platform-specific Graphics Device and all its associated resources.
        /// </summary>
        protected partial void DestroyPlatformDevice()
        {
            NullHelper.ToImplement();
        }
    }
}
#endif
