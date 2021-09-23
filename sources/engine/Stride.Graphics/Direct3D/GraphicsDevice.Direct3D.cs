// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using SharpDX;
//using SharpDX.Direct3D11;
using Stride.Core;
using Silk.NET.Direct3D11;
using Stride.Graphics.Direct3D;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;

namespace Stride.Graphics
{
    public partial class GraphicsDevice
    {
        internal readonly int ConstantBufferDataPlacementAlignment = 16;

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D11;

        private bool simulateReset = false;
        private string rendererName;

        public ID3D11Device nativeDevice;
        public ID3D11DeviceContext nativeDeviceContext;
        private readonly Queue<ID3D11Query> disjointQueries = new Queue<ID3D11Query>(4);
        private readonly Stack<ID3D11Query> currentDisjointQueries = new Stack<ID3D11Query>(2);

        internal GraphicsProfile RequestedProfile;

        //private SharpDX.Direct3D11.DeviceCreationFlags creationFlags;
        private CreateDeviceFlag creationFlags;

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

                var result = (long)NativeDevice.GetDeviceRemovedReason();
                if (result == (long)ReturnCodes.DEVICE_REMOVED)
                {
                    return GraphicsDeviceStatus.Removed;
                }

                if (result == (long)ReturnCodes.DEVICE_RESET)
                {
                    return GraphicsDeviceStatus.Reset;
                }

                if (result == (long)ReturnCodes.DEVICE_HUNG)
                {
                    return GraphicsDeviceStatus.Hung;
                }

                if (result == (long)ReturnCodes.DRIVER_INTERNAL_ERROR)
                {
                    return GraphicsDeviceStatus.InternalError;
                }

                if (result == (long)ReturnCodes.INVALID_CALL)
                {
                    return GraphicsDeviceStatus.InvalidCall;
                }

                if (result < 0)
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
        internal Silk.NET.Direct3D11.ID3D11Device NativeDevice
        {
            get
            {
                return nativeDevice;
            }
        }

        /// <summary>
        /// Gets the native device context.
        /// </summary>
        /// <value>The native device context.</value>
        internal ID3D11DeviceContext NativeDeviceContext
        {
            get
            {
                return nativeDeviceContext;
            }
        }

        /// <summary>
        ///     Marks context as active on the current thread.
        /// </summary>
        public void Begin()
        {
            FrameTriangleCount = 0;
            FrameDrawCalls = 0;
            // TODO : Need review
            
            ID3D11Query currentDisjointQuery = disjointQueries.Peek();
            var result = new QueryDataTimestampDisjoint();
            unsafe
            {
                ID3D11Query* currentDisjointQueryPtr = &currentDisjointQuery;

                nativeDeviceContext.GetData((ID3D11Asynchronous*)currentDisjointQueryPtr, ref result, currentDisjointQuery.GetDataSize(), (int)AsyncGetdataFlag.AsyncGetdataDonotflush);

                if (disjointQueries.Count > 0 )
                {
                    TimestampFrequency = (long)result.Frequency;
                    currentDisjointQuery = disjointQueries.Dequeue();
                }
                else
                {
                    var disjointQueryDescription = new QueryDesc { MiscFlags = 3 };
                    var pDquery = &currentDisjointQuery;
                    NativeDevice.CreateQuery(ref disjointQueryDescription, &pDquery);
                }
                currentDisjointQueries.Push(currentDisjointQuery);
                NativeDeviceContext.Begin((ID3D11Asynchronous*)currentDisjointQueryPtr);
            }
            // Try to read back the oldest disjoint query and reuse it. If not ready, create a new one.
            

            
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
            unsafe
            {
                var ptr = &currentDisjointQuery;
                NativeDeviceContext.End((ID3D11Asynchronous*)ptr);
            }
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
        private void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            nativeDevice.Release();
            unsafe
            {
                AdapterDesc1 d = new AdapterDesc1();
                Adapter.NativeAdapter.GetDesc1(&d);
                rendererName = new string(d.Description);
            }

            // Profiling is supported through pix markers
            IsProfilingSupported = true;

            // Map GraphicsProfile to D3D11 FeatureLevel
            creationFlags = (CreateDeviceFlag)deviceCreationFlags;

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
                        if (level < D3DFeatureLevel.D3DFeatureLevel100)
                            level = D3DFeatureLevel.D3DFeatureLevel100;
                    }

                    if (Core.Platform.Type == PlatformType.Windows
                        && GetModuleHandle("renderdoc.dll") != IntPtr.Zero)
                    {
                        if (level < D3DFeatureLevel.D3DFeatureLevel110)
                            level = D3DFeatureLevel.D3DFeatureLevel110;
                    }
                    unsafe
                    {
                        // TODO : Correct the creation
                        fixed(IDXGIAdapter1* ad = &Adapter.adapter)
                        fixed(ID3D11Device* dev = &nativeDevice)
                            D3D11Overloads.CreateDevice(D3D11.GetApi(), (IDXGIAdapter*)ad, D3DDriverType.D3DDriverTypeUnknown, 0, (uint)creationFlags, &level, 1, D3D11.SdkVersion, &dev, null, null);
                    }
                    //nativeDevice = new SharpDX.Direct3D11.Device(Adapter.NativeAdapter, creationFlags, level);
                    


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

            unsafe
            {
                var c = new ID3D11DeviceContext();
                var cP = &c;
                nativeDevice.GetImmediateContext(&cP);
                nativeDeviceContext = c;
            }
            // We keep one reference so that it doesn't disappear with InternalMainCommandList
            ((IUnknown)nativeDeviceContext).AddRef();
            if (IsDebugMode)
            {
                //GraphicsResourceBase.SetDebugName(this, nativeDeviceContext, "ImmediateContext");
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
                query.Release();
            }
            disjointQueries.Clear();

            unsafe 
            {
                // Display D3D11 ref counting info
                fixed(ID3D11DeviceContext* ctx = &nativeDeviceContext)
                {
                    // TODO : Unsafe and not sure if this should be working this way
                    NativeDevice.GetImmediateContext(&ctx);
                    nativeDeviceContext.ClearState();
                    nativeDeviceContext.Flush();
                }
                
            }
            

            if (IsDebugMode)
            {
                //var debugDevice = NativeDevice.QueryInterfaceOrNull<SharpDX.Direct3D11.DeviceDebug>();
                //if (debugDevice != null)
                //{
                //    debugDevice.ReportLiveDeviceObjects(SharpDX.Direct3D11.ReportingLevel.Detail);
                //    debugDevice.Dispose();
                //}
            }

            //nativeDevice.DisposeBy
            nativeDevice.Release();
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
    }
}
#endif
