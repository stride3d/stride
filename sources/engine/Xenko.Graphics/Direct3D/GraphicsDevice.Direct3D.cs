// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D11
using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using Xenko.Core;

namespace Xenko.Graphics
{
    public partial class GraphicsDevice
    {
        internal readonly int ConstantBufferDataPlacementAlignment = 256;

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D11;

        private bool simulateReset = false;
        private string rendererName;

        private Device nativeDevice;
        private DeviceContext nativeDeviceContext;
        private readonly Queue<Query> disjointQueries = new Queue<Query>(4);
        private readonly Stack<Query> currentDisjointQueries = new Stack<Query>(2);

        internal GraphicsProfile RequestedProfile;

        private SharpDX.Direct3D11.DeviceCreationFlags creationFlags;

        /// <summary>
        /// The tick frquency of timestamp queries in Hertz.
        /// </summary>
        public long TimestampFrequency { get; private set; }

        /// <summary>
        ///     Gets the status of this device.
        /// </summary>
        /// <value>The graphics device status.</value>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                if (simulateReset)
                {
                    simulateReset = false;
                    return GraphicsDeviceStatus.Reset;
                }

                var result = NativeDevice.DeviceRemovedReason;
                if (result == SharpDX.DXGI.ResultCode.DeviceRemoved)
                {
                    return GraphicsDeviceStatus.Removed;
                }

                if (result == SharpDX.DXGI.ResultCode.DeviceReset)
                {
                    return GraphicsDeviceStatus.Reset;
                }

                if (result == SharpDX.DXGI.ResultCode.DeviceHung)
                {
                    return GraphicsDeviceStatus.Hung;
                }

                if (result == SharpDX.DXGI.ResultCode.DriverInternalError)
                {
                    return GraphicsDeviceStatus.InternalError;
                }

                if (result == SharpDX.DXGI.ResultCode.InvalidCall)
                {
                    return GraphicsDeviceStatus.InvalidCall;
                }

                if (result.Code < 0)
                {
                    return GraphicsDeviceStatus.Reset;
                }

                return GraphicsDeviceStatus.Normal;
            }
        }

        /// <summary>
        ///     Gets the native device.
        /// </summary>
        /// <value>The native device.</value>
        internal SharpDX.Direct3D11.Device NativeDevice => nativeDevice;

        /// <summary>
        /// Gets the native device context.
        /// </summary>
        /// <value>The native device context.</value>
        internal SharpDX.Direct3D11.DeviceContext NativeDeviceContext => nativeDeviceContext;

        /// <summary>
        ///     Marks context as active on the current thread.
        /// </summary>
        public void Begin()
        {
            FrameTriangleCount = 0;
            FrameDrawCalls = 0;

            Query currentDisjointQuery;

            // Try to read back the oldest disjoint query and reuse it. If not ready, create a new one.
            if (disjointQueries.Count > 0 && NativeDeviceContext.GetData(disjointQueries.Peek(), out QueryDataTimestampDisjoint result))
            {
                TimestampFrequency = result.Frequency;
                currentDisjointQuery = disjointQueries.Dequeue();
            }
            else
            {
                var disjointQueryDiscription = new QueryDescription { Type = SharpDX.Direct3D11.QueryType.TimestampDisjoint };
                currentDisjointQuery = new Query(NativeDevice, disjointQueryDiscription);
            }

            currentDisjointQueries.Push(currentDisjointQuery);
            NativeDeviceContext.Begin(currentDisjointQuery);
        }

        /// <summary>
        /// Enables profiling.
        /// </summary>
        /// <param name="enabledFlag">if set to <c>true</c> [enabled flag].</param>
        public void EnableProfile(bool enabledFlag)
        {
        }

        /// <summary>
        ///     Unmarks context as active on the current thread.
        /// </summary>
        public void End()
        {
            // If this fails, it means Begin()/End() don't match, something is very wrong
            var currentDisjointQuery = currentDisjointQueries.Pop();
            NativeDeviceContext.End(currentDisjointQuery);
            disjointQueries.Enqueue(currentDisjointQuery);
        }

        /// <summary>
        /// Executes a deferred command list.
        /// </summary>
        /// <param name="commandList">The deferred command list.</param>
        public void ExecuteCommandList(CompiledCommandList commandList)
        {
            lock (nativeDeviceContext)
            {
                nativeDeviceContext.ExecuteCommandList(commandList.NativeCommandList, false);
            }

            commandList.NativeCommandList.Dispose();

            FrameTriangleCount += commandList.VertexCount;
            FrameDrawCalls += commandList.DrawCallCount;
        }

        /// <summary>
        /// Executes multiple deferred command lists.
        /// </summary>
        /// <param name="commandLists">The deferred command lists.</param>
        public void ExecuteCommandLists(int count, CompiledCommandList[] commandLists)
        {
            for (int index = 0; index < count; index++)
            {
                ExecuteCommandList(commandLists[index]);
            }
        }

        /// <summary>
        /// Maps a subresource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="subResourceIndex">Index of the sub resource.</param>
        /// <param name="mapMode">The map mode.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <param name="offsetInBytes">The offset information in bytes.</param>
        /// <param name="lengthInBytes">The length information in bytes.</param>
        /// <returns>Pointer to the sub resource to map.</returns>
        public unsafe MappedResource MapSubresource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            lock (nativeDeviceContext)
            {
                if (resource == null) throw new ArgumentNullException("resource");

                // This resource has just been recycled by the GraphicsResourceAllocator, we force a rename to avoid GPU=>GPU sync point
                if (resource.DiscardNextMap && mapMode == MapMode.WriteNoOverwrite)
                {
                    mapMode = MapMode.WriteDiscard;
                    resource.DiscardNextMap = false;
                }

                SharpDX.DataBox dataBox = NativeDeviceContext.MapSubresource(resource.NativeResource, subResourceIndex, (SharpDX.Direct3D11.MapMode)mapMode, doNotWait ? SharpDX.Direct3D11.MapFlags.DoNotWait : SharpDX.Direct3D11.MapFlags.None);
                var databox = *(DataBox*)Interop.Cast(ref dataBox);
                if (!dataBox.IsEmpty)
                {
                    databox.DataPointer = (IntPtr)((byte*)databox.DataPointer + offsetInBytes);
                }
                return new MappedResource(resource, subResourceIndex, databox);
            }
        }

        public void UnmapSubresource(MappedResource unmapped)
        {
            lock (nativeDeviceContext)
                NativeDeviceContext.UnmapSubresource(unmapped.Resource.NativeResource, unmapped.SubResourceIndex);
        }

        public void SimulateReset()
        {
            simulateReset = true;
        }

        private void InitializePostFeatures()
        {
            // Create the main command list
            DefaultCommandList = new CommandList(this);
        }

        private string GetRendererName()
        {
            return rendererName;
        }

        /// <summary>
        ///     Initializes the specified device.
        /// </summary>
        /// <param name="graphicsProfiles">The graphics profiles.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (nativeDevice != null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.NativeAdapter.Description.Description;

            // Profiling is supported through pix markers
            IsProfilingSupported = true;

            // Map GraphicsProfile to D3D11 FeatureLevel
            creationFlags = (SharpDX.Direct3D11.DeviceCreationFlags)deviceCreationFlags;

            // Default fallback
            if (graphicsProfiles.Length == 0)
                graphicsProfiles = new[] { GraphicsProfile.Level_11_0, GraphicsProfile.Level_10_1, GraphicsProfile.Level_10_0, GraphicsProfile.Level_9_3, GraphicsProfile.Level_9_2, GraphicsProfile.Level_9_1 };

            // Create Device D3D11 with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                var graphicsProfile = graphicsProfiles[index];
                try
                {
                    // D3D12 supports only feature level 11+
                    var level = graphicsProfile.ToFeatureLevel();

                    // INTEL workaround: it seems Intel driver doesn't support properly feature level 9.x. Fallback to 10.
                    if (Adapter.VendorId == 0x8086)
                    {
                        if (level < SharpDX.Direct3D.FeatureLevel.Level_10_0)
                            level = SharpDX.Direct3D.FeatureLevel.Level_10_0;
                    }

                    nativeDevice = new SharpDX.Direct3D11.Device(Adapter.NativeAdapter, creationFlags, level);

                    // INTEL workaround: force ShaderProfile to be 10+ as well
                    if (Adapter.VendorId == 0x8086)
                    {
                        if (graphicsProfile < GraphicsProfile.Level_10_0 && (!ShaderProfile.HasValue || ShaderProfile.Value < GraphicsProfile.Level_10_0))
                            ShaderProfile = GraphicsProfile.Level_10_0;
                    }

                    RequestedProfile = graphicsProfile;
                    break;
                }
                catch (Exception)
                {
                    if (index == graphicsProfiles.Length - 1)
                        throw;
                }
            }

            IsDeferred = true;

            nativeDeviceContext = nativeDevice.ImmediateContext;
            // We keep one reference so that it doesn't disappear with InternalMainCommandList
            if (IsDebugMode)
            {
                GraphicsResourceBase.SetDebugName(this, nativeDeviceContext, "ImmediateContext");
            }
        }

        private void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
            // On D3D, default state is Less instead of our LessEqual
            // Let's update default pipeline state so that it correspond to D3D state after a "ClearState()"
            pipelineStateDescription.DepthStencilState.DepthBufferFunction = CompareFunction.Less;
        }

        protected void DestroyPlatformDevice()
        {
            ReleaseDevice();
        }

        private void ReleaseDevice()
        {
            foreach (var query in disjointQueries)
            {
                query.Dispose();
            }
            disjointQueries.Clear();

            // Display D3D11 ref counting info
            nativeDeviceContext.ClearState();
            nativeDeviceContext.Flush();

            if (IsDebugMode)
            {
                var debugDevice = NativeDevice.QueryInterfaceOrNull<SharpDX.Direct3D11.DeviceDebug>();
                if (debugDevice != null)
                {
                    debugDevice.ReportLiveDeviceObjects(SharpDX.Direct3D11.ReportingLevel.Detail);
                    debugDevice.Dispose();
                }
            }

            nativeDevice.Dispose();
            nativeDevice = null;
        }

        internal void OnDestroyed()
        {
        }

        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            if (resourceLink.Resource is GraphicsResource resource)
                resource.DiscardNextMap = true;
        }
    }
}
#endif
