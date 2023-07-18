// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;
using Stride.Core;

namespace Stride.Graphics
{
    public unsafe partial class GraphicsDevice
    {
        internal readonly int ConstantBufferDataPlacementAlignment = 16;

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D11;

        private bool simulateReset = false;
        private string rendererName;

        /// <summary>
        ///   Gets the native Direct3D 11 device.
        /// </summary>
        public ID3D11Device* NativeDevice { get; private set; }

        /// <summary>
        ///   Gets the native Direct3D 11 device context.
        /// </summary>
        internal ID3D11DeviceContext* NativeDeviceContext { get; private set; }

        private readonly Queue<QueryPtr> disjointQueries = new(4);
        private readonly Stack<QueryPtr> currentDisjointQueries = new(2);

        internal GraphicsProfile RequestedProfile;

        //private SharpDX.Direct3D11.DeviceCreationFlags creationFlags;
        private CreateDeviceFlag creationFlags;

        /// <summary>
        /// The tick frquency of timestamp queries in Hertz.
        /// </summary>
        public long TimestampFrequency { get; private set; }

        /// <summary>
        ///   Gets the current status of this device.
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

                var result = (DeviceRemoveReason) NativeDevice->GetDeviceRemovedReason();

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
        ///   Marks the graphics device context as active on the current thread.
        /// </summary>
        public unsafe void Begin()
        {
            FrameTriangleCount = 0;
            FrameDrawCalls = 0;

            var queryResult = new QueryDataTimestampDisjoint();

            ID3D11Query* currentDisjointQuery = null;

            // Try to read back the oldest disjoint query and reuse it. If not ready, create a new one
            if (disjointQueries.Count > 0)
            {
                currentDisjointQuery = disjointQueries.Peek();

                var asyncQuery = (ID3D11Asynchronous*) currentDisjointQuery;
                var dataSize = currentDisjointQuery->GetDataSize();
                HResult result = NativeDeviceContext->GetData(asyncQuery, ref queryResult, dataSize, (int) AsyncGetdataFlag.Donotflush);

                if (result.IsFailure)
                    result.Throw();

                TimestampFrequency = (long) queryResult.Frequency;
                currentDisjointQuery = disjointQueries.Dequeue();
            }
            else
            {
                var disjointQueryDescription = new QueryDesc { Query = Query.TimestampDisjoint };

                HResult result = NativeDevice->CreateQuery(in disjointQueryDescription, ref currentDisjointQuery);

                if (result.IsFailure)
                    result.Throw();
            }

            // TODO: Implicit op calls AddRef
            currentDisjointQueries.Push(currentDisjointQuery);

            NativeDeviceContext->Begin((ID3D11Asynchronous*) currentDisjointQuery);
        }

        /// <summary>
        /// Enables profiling.
        /// </summary>
        /// <param name="enabledFlag">if set to <c>true</c> [enabled flag].</param>
        public void EnableProfile(bool enabledFlag)
        {
        }

        /// <summary>
        ///   Unmarks the graphics device context as active on the current thread.
        /// </summary>
        public unsafe void End()
        {
            // If this fails, it means Begin() / End() don't match, something is very wrong
            var currentDisjointQuery = currentDisjointQueries.Pop();

            ID3D11Asynchronous* asyncQuery = null;

            ID3D11Query* query = currentDisjointQuery;
            query->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Asynchronous>(), (void**) &asyncQuery);

            NativeDeviceContext->End(asyncQuery);
            disjointQueries.Enqueue(currentDisjointQuery);
        }

        /// <summary>
        /// Executes a deferred command list.
        /// </summary>
        /// <param name="commandList">The deferred command list.</param>
        public void ExecuteCommandList(CompiledCommandList commandList)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes multiple deferred command lists.
        /// </summary>
        /// <param name="commandLists">The deferred command lists.</param>
        public void ExecuteCommandLists(int count, CompiledCommandList[] commandLists)
        {
            throw new NotImplementedException();
        }

        public void SimulateReset()
        {
            simulateReset = true;
        }

        private void InitializePostFeatures()
        {
            // Create the main command list
            InternalMainCommandList = new CommandList(this);
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
        private unsafe void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (NativeDevice != null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            // Profiling is supported through PIX markers
            IsProfilingSupported = true;

            // Map GraphicsProfile to D3D11 FeatureLevel
            creationFlags = (CreateDeviceFlag) deviceCreationFlags;

            // Create Device D3D11 with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                var graphicsProfile = graphicsProfiles[index];

                // D3D12 supports only feature level 11+
                var level = graphicsProfile.ToFeatureLevel();

                // INTEL workaround: it seems Intel driver doesn't support properly feature level 9.x. Fallback to 10.
                if (Adapter.VendorId == 0x8086)
                {
                    if (level < D3DFeatureLevel.Level100)
                        level = D3DFeatureLevel.Level100;
                }

                if (Core.Platform.Type == PlatformType.Windows && GetModuleHandle("renderdoc.dll") != IntPtr.Zero)
                {
                    if (level < D3DFeatureLevel.Level110)
                        level = D3DFeatureLevel.Level110;
                }

                var d3d11 = D3D11.GetApi();

                var featureLevels = stackalloc D3DFeatureLevel[] { level };
                ID3D11Device* device = null;
                ID3D11DeviceContext* deviceContext = null;
                D3DFeatureLevel usedFeatureLevel = 0;

                HResult result = d3d11.CreateDevice((IDXGIAdapter*) Adapter.NativeAdapter, D3DDriverType.Unknown, Software: 0, (uint) creationFlags,
                                                    featureLevels, 1, D3D11.SdkVersion,
                                                    &device, &usedFeatureLevel, &deviceContext);

                if (result.IsFailure)
                {
                    if (index == graphicsProfiles.Length - 1)
                        result.Throw();
                    else
                        continue;
                }

                NativeDevice = device;
                NativeDeviceContext = deviceContext;

                // INTEL workaround: force ShaderProfile to be 10+ as well
                if (Adapter.VendorId == 0x8086)
                {
                    if (graphicsProfile < GraphicsProfile.Level_10_0 && (!ShaderProfile.HasValue || ShaderProfile.Value < GraphicsProfile.Level_10_0))
                        ShaderProfile = GraphicsProfile.Level_10_0;
                }

                RequestedProfile = graphicsProfile;
                break;
            }

            // We keep one reference so that it doesn't disappear with InternalMainCommandList
            //((IUnknown)nativeDeviceContext).AddReference();
            if (IsDebugMode)
            {
                DebugHelpers.SetDebugName((ID3D11DeviceChild*) NativeDeviceContext, "ImmediateContext");
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
            foreach (var queryPtr in disjointQueries)
            {
                queryPtr.Query->Release();
            }
            disjointQueries.Clear();

            // Display D3D11 ref counting info
            ID3D11DeviceContext* immediateContext = null;
            NativeDevice->GetImmediateContext(ref immediateContext);

            immediateContext->ClearState();
            immediateContext->Flush();

            if (IsDebugMode)
            {
                ID3D11Debug* debugDevice = null;
                HResult result = NativeDevice->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Debug>(), (void**) &debugDevice);

                if (result.IsSuccess && debugDevice != null)
                {
                    debugDevice->ReportLiveDeviceObjects(RldoFlags.Detail);
                    debugDevice->Release();
                }
            }

            NativeDevice->Release();
            NativeDevice = null;
        }

        internal void OnDestroyed()
        {
        }

        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            if (resourceLink.Resource is GraphicsResource resource)
                resource.DiscardNextMap = true;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #region QueryPtr structure

        // Ancillary struct to store a Query without messing with its reference count (as ComPtr<T> does).
        private readonly record struct QueryPtr(ID3D11Query* Query)
        {
            public static implicit operator ID3D11Query*(QueryPtr queryPtr) => queryPtr.Query;
            public static implicit operator QueryPtr(ID3D11Query* query) => new(query);
        }

        #endregion
    }
}

#endif
