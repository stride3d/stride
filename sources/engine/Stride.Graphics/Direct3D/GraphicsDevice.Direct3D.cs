// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;

using Stride.Core;

using static Stride.Graphics.ComPtrHelpers;


namespace Stride.Graphics
{
    public unsafe partial class GraphicsDevice
    {
        internal readonly int ConstantBufferDataPlacementAlignment = 16;

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D11;

        private bool simulateReset = false;
        private string rendererName;

        private ID3D11Device* nativeDevice;
        private ID3D11DeviceContext* nativeDeviceContext;

        /// <summary>
        ///   Gets the internal Direct3D 11 Device.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        public ComPtr<ID3D11Device> NativeDevice => ToComPtr(nativeDevice);

        /// <summary>
        ///   Gets the internal Direct3D 11 Device Context.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D11DeviceContext> NativeDeviceContext => ToComPtr(nativeDeviceContext);

        private readonly Queue<ComPtr<ID3D11Query>> disjointQueries = new(4);
        private readonly Stack<ComPtr<ID3D11Query>> currentDisjointQueries = new(2);

        /// <summary>
        ///   The requested graphics profile for the Graphics Device.
        /// </summary>
        internal GraphicsProfile RequestedProfile;

        //private SharpDX.Direct3D11.DeviceCreationFlags creationFlags;
        private CreateDeviceFlag creationFlags;

        /// <summary>
        ///   Gets the tick frquency of timestamp queries, in hertz.
        /// </summary>
        public ulong TimestampFrequency { get; private set; }

        /// <summary>
        ///   Gets the current status of the Graphics Device.
        /// </summary>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                if (simulateReset)
                {
                    simulateReset = false;
                    return GraphicsDeviceStatus.Reset;
                }

                var result = (DeviceRemoveReason) nativeDevice->GetDeviceRemovedReason();

                return result switch
                {
                    DeviceRemoveReason.DeviceRemoved => GraphicsDeviceStatus.Removed,
                    DeviceRemoveReason.DeviceReset => GraphicsDeviceStatus.Reset,
                    DeviceRemoveReason.DeviceHung => GraphicsDeviceStatus.Hung,
                    DeviceRemoveReason.DriverInternalError => GraphicsDeviceStatus.InternalError,
                    DeviceRemoveReason.InvalidCall => GraphicsDeviceStatus.InvalidCall,

                    < 0 => GraphicsDeviceStatus.Reset,
                    _ => GraphicsDeviceStatus.Normal
                };
            }
        }

        #region Graphics device status codes

        // From DXGI_ERROR constants in Winerror.h
        private enum DeviceRemoveReason : int
        {
            None = 0,   // S_OK -- No error

            DeviceHung = unchecked((int) 0x887A0006),           // DEVICE_HUNG
            DeviceRemoved = unchecked((int) 0x887A0005),        // DEVICE_REMOVED
            DeviceReset = unchecked((int) 0x887A0007),          // DEVICE_RESET
            DriverInternalError = unchecked((int) 0x887A0020),  // DRIVER_INTERNAL_ERROR
            InvalidCall = unchecked((int) 0x887A0001)           // INVALID_CALL
        }

        #endregion

        /// <summary>
        ///   Marks the Graphics Device Context as <strong>active</strong> on the current thread.
        /// </summary>
        public void Begin()
        {
            FrameTriangleCount = 0;
            FrameDrawCalls = 0;

            Unsafe.SkipInit(out QueryDataTimestampDisjoint queryResult);

            ComPtr<ID3D11Query> currentDisjointQuery = default;

            // Try to read back the oldest disjoint query and reuse it. If not ready, create a new one
            if (disjointQueries.Count > 0)
            {
                currentDisjointQuery = disjointQueries.Peek();

                var asyncQuery = currentDisjointQuery;
                var dataSize = currentDisjointQuery.GetDataSize();
                HResult result = nativeDeviceContext->GetData(asyncQuery, ref queryResult, dataSize, (int) AsyncGetdataFlag.Donotflush);

                if (result.IsFailure)
                    result.Throw();

                TimestampFrequency = queryResult.Frequency;
                currentDisjointQuery = disjointQueries.Dequeue();
            }
            else
            {
                var disjointQueryDescription = new QueryDesc(Query.TimestampDisjoint);

                HResult result = nativeDevice->CreateQuery(in disjointQueryDescription, ref currentDisjointQuery);

                if (result.IsFailure)
                    result.Throw();
            }

            currentDisjointQueries.Push(currentDisjointQuery);

            nativeDeviceContext->Begin(currentDisjointQuery);
        }

        /// <summary>
        ///   Enables or disables profiling.
        /// </summary>
        /// <param name="enabledFlag"><see langword="true"/> to enable profiling; <see langword="false"/> to disable it.</param>
        public void EnableProfile(bool enabledFlag) { }  // TODO: Implement profiling with PIX markers? Currently, profiling is only implemented for OpenGL

        /// <summary>
        ///   Marks the Graphics Device Context as <strong>inactive</strong> on the current thread.
        /// </summary>
        public void End()
        {
            // If this fails, it means Begin() / End() don't match, something is very wrong
            var currentDisjointQuery = currentDisjointQueries.Pop();
            nativeDeviceContext->End(currentDisjointQuery);
            disjointQueries.Enqueue(currentDisjointQuery);
        }

        /// <summary>
        ///   Executes a Compiled Command List.
        /// </summary>
        /// <param name="commandList">The Compiled Command List to execute.</param>
        /// <exception cref="NotImplementedException">Deferred CommandList execution is not implemented for Direct3D 11.</exception>"
        /// <remarks>
        ///   A Compiled Command List is a list of commands that have been pre-compiled and optimized for execution on the
        ///   Graphics Device at a later time. This method executes the commands in the list. This is known as <em>deferred execution</em>.
        /// </remarks>
        public void ExecuteCommandList(CompiledCommandList commandList) => throw new NotImplementedException();

        /// <summary>
        ///   Executes multiple Compiled Command Lists.
        /// </summary>
        /// <param name="commandLists">The Compiled Command Lists to execute.</param>
        /// <exception cref="NotImplementedException">Deferred CommandList execution is not implemented for Direct3D 11.</exception>"
        /// <remarks>
        ///   A Compiled Command List is a list of commands that have been pre-compiled and optimized for execution on the
        ///   Graphics Device at a later time. This method executes the commands in the list. This is known as <em>deferred execution</em>.
        /// </remarks>
        public void ExecuteCommandLists(int count, CompiledCommandList[] commandLists) => throw new NotImplementedException();

        /// <summary>
        ///   Sets the Graphics Device to simulate a situation in which the device is lost and then reset.
        /// </summary>
        public void SimulateReset()
        {
            simulateReset = true;
        }

        /// <summary>
        ///   Initializes the platform-specific features of the Graphics Device once it has been fully initialized.
        /// </summary>
        private partial void InitializePostFeatures()
        {
            // Create the main command list
            InternalMainCommandList = new CommandList(this);
        }

        private partial string GetRendererName() => rendererName;

        /// <summary>
        ///   Initialize the platform-specific implementation of the Graphics Device.
        /// </summary>
        /// <param name="graphicsProfiles">A non-<see langword="null"/> list of the graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private partial void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (nativeDevice != null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            // Profiling is supported through PIX markers
            IsProfilingSupported = true;

            creationFlags = (CreateDeviceFlag) deviceCreationFlags;

            // Create D3D11 Device with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                // Map GraphicsProfiles to D3D11 FeatureLevels
                var graphicsProfile = graphicsProfiles[index];
                var level = graphicsProfile.ToFeatureLevel();

                // INTEL workaround: it seems Intel driver doesn't support properly feature level 9.x. Fallback to 10.
                if (Adapter.VendorId == 0x8086)
                {
                    // TODO: This is relevant still? Newer Intel Arc HW and drivers should have better support for 9.x
                    if (level < D3DFeatureLevel.Level100)
                        level = D3DFeatureLevel.Level100;
                }

                if (Core.Platform.Type == PlatformType.Windows && GetModuleHandle("renderdoc.dll") != IntPtr.Zero)
                {
                    if (level < D3DFeatureLevel.Level110)
                        level = D3DFeatureLevel.Level110;
                }

                var d3d11 = D3D11.GetApi(window: null);

                var adapter = Adapter.NativeAdapter.AsComPtr<IDXGIAdapter1, IDXGIAdapter>();
                ComPtr<ID3D11Device> device = default;
                ComPtr<ID3D11DeviceContext> deviceContext = default;
                D3DFeatureLevel usedFeatureLevel = 0;

                HResult result = d3d11.CreateDevice(adapter, D3DDriverType.Unknown, Software: 0, (uint) creationFlags,
                                                    in level, FeatureLevels: 1, D3D11.SdkVersion,
                                                    ref device, ref usedFeatureLevel, ref deviceContext);
                if (result.IsFailure)
                {
                    if (index == graphicsProfiles.Length - 1)
                        result.Throw();
                    else
                        continue;
                }

                nativeDevice = device.DisposeBy(this);
                nativeDeviceContext = deviceContext.DisposeBy(this);

                // We keep one reference so that it doesn't disappear with InternalMainCommandList
                nativeDeviceContext->AddRef();

                // INTEL workaround: force ShaderProfile to be 10+ as well
                if (Adapter.VendorId == 0x8086)
                {
                    // TODO: This is relevant still? Newer Intel Arc HW and drivers should have better support for 9.x
                    if (graphicsProfile < GraphicsProfile.Level_10_0 && (!ShaderProfile.HasValue || ShaderProfile.Value < GraphicsProfile.Level_10_0))
                        ShaderProfile = GraphicsProfile.Level_10_0;
                }

                RequestedProfile = graphicsProfile;
                break;
            }

            if (IsDebugMode)
            {
                NativeDeviceContext.SetDebugName("ImmediateContext");
            }
        }

        /// <summary>
        ///   Makes Direct3D 11-specific adjustments to the Pipeline State objects created by the Graphics Device.
        /// </summary>
        /// <param name="pipelineStateDescription">A Pipeline State description that can be modified and adjusted.</param>
        private partial void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
            // On D3D, default state is Less instead of our LessEqual
            // Let's update default pipeline state so that it correspond to D3D state after a "ClearState()"
            pipelineStateDescription.DepthStencilState.DepthBufferFunction = CompareFunction.Less;
        }

        /// <summary>
        ///   Releases the platform-specific Graphics Device and all its associated resources.
        /// </summary>
        protected partial void DestroyPlatformDevice()
        {
            ReleaseDevice();
        }

        /// <summary>
        ///   Disposes the Direct3D 11 Device and all its associated resources.
        /// </summary>
        private void ReleaseDevice()
        {
            foreach (var queryPtr in disjointQueries)
            {
                queryPtr.Release();
            }
            disjointQueries.Clear();

            ComPtr<ID3D11DeviceContext> immediateContext = default;
            nativeDevice->GetImmediateContext(ref immediateContext);

            NativeDeviceContext.RemoveDisposeBy(this);
            immediateContext.ClearState();
            immediateContext.Flush();
            immediateContext.Release();
            nativeDeviceContext = null;

            // Display D3D11 ref counting info
            if (IsDebugMode)
            {
                HResult result = nativeDevice->QueryInterface(out ComPtr<ID3D11Debug> debugDevice);

                if (result.IsSuccess && debugDevice.IsNotNull())
                {
                    debugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
                    debugDevice.Release();
                }
            }

            NativeDevice.RemoveDisposeBy(this);
            NativeDevice.Dispose();
            nativeDevice = null;
        }

        /// <summary>
        ///   Called when the Graphics Device is being destroyed.
        /// </summary>
        internal void OnDestroyed()
        {
        }


        /// <summary>
        ///   Tags a Graphics Resource as no having alive references, meaning it should be safe to dispose it
        ///   or discard its contents during the next <see cref="CommandList.MapSubResource"/> or <c>SetData</c> operation.
        /// </summary>
        /// <param name="resourceLink">
        ///   A <see cref="GraphicsResourceLink"/> object identifying the Graphics Resource along some related allocation information.
        /// </param>
        internal partial void TagResourceAsNotAlive(GraphicsResourceLink resourceLink)
        {
            if (resourceLink.Resource is GraphicsResource resource)
                resource.DiscardNextMap = true;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}

#endif
