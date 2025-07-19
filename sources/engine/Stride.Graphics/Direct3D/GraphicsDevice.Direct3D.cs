// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core;
using Stride.Core.UnsafeExtensions;
using static Stride.Graphics.ComPtrHelpers;
using static Stride.Graphics.DxgiConstants;


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

        private ID3D11InfoQueue* nativeInfoQueue;

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

            if (IsDebugMode)
            {
                // Process any messages in the InfoQueue
                ProcessInfoQueueMessages();
            }
        }

        /// <summary>
        ///   Executes a Compiled Command List.
        /// </summary>
        /// <param name="commandList">The Compiled Command List to execute.</param>
        /// <exception cref="NotImplementedException">Deferred CommandList execution is not implemented for Direct3D 11.</exception>"
        /// <remarks>
        ///   A Compiled Command List is a list of commands that have been recorded for execution on the Graphics Device
        ///   at a later time. This method executes the commands in the list. This is known as <em>deferred execution</em>.
        /// </remarks>
        public void ExecuteCommandList(CompiledCommandList commandList) => throw new NotImplementedException();

        /// <summary>
        ///   Executes multiple Compiled Command Lists.
        /// </summary>
        /// <param name="count">The number of Compiled Command Lists to execute.</param>
        /// <param name="commandLists">The Compiled Command Lists to execute.</param>
        /// <exception cref="NotImplementedException">Deferred CommandList execution is not implemented for Direct3D 11.</exception>"
        /// <remarks>
        ///   A Compiled Command List is a list of commands that have been recorded for execution on the Graphics Device
        ///   at a later time. This method executes the commands in the list. This is known as <em>deferred execution</em>.
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
        private unsafe partial void InitializePostFeatures()
        {
            // Create the main command list
            InternalMainCommandList = new CommandList(this).DisposeBy(this);
        }

        private partial string GetRendererName() => rendererName;

        /// <summary>
        ///   Initialize the platform-specific implementation of the Graphics Device.
        /// </summary>
        /// <param name="graphicsProfiles">A non-<see langword="null"/> list of the graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private unsafe partial void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (nativeDevice is not null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            // Profiling is supported through PIX markers
            IsProfilingSupported = true;

            creationFlags = (CreateDeviceFlag) deviceCreationFlags;

            var d3d11 = D3D11.GetApi(window: null);

            // Create D3D11 Device with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                // Map GraphicsProfiles to D3D11 FeatureLevels
                var graphicsProfile = graphicsProfiles[index];
                var featureLevel = graphicsProfile.ToFeatureLevel();

                // INTEL workaround: it seems Intel driver doesn't support properly feature level 9.x. Fallback to 10.
                if (Adapter.VendorId == 0x8086)
                {
                    // TODO: This is relevant still? Newer Intel Arc HW and drivers should have better support for 9.x
                    if (featureLevel < D3DFeatureLevel.Level100)
                        featureLevel = D3DFeatureLevel.Level100;
                }

                if (Core.Platform.Type == PlatformType.Windows && GetModuleHandle("renderdoc.dll") != IntPtr.Zero)
                {
                    if (featureLevel < D3DFeatureLevel.Level110)
                        featureLevel = D3DFeatureLevel.Level110;
                }

                var adapter = Adapter.NativeAdapter.AsComPtr<IDXGIAdapter1, IDXGIAdapter>();
                ComPtr<ID3D11Device> device = default;
                ComPtr<ID3D11DeviceContext> deviceContext = default;
                D3DFeatureLevel usedFeatureLevel = 0;

                HResult result = d3d11.CreateDevice(adapter, D3DDriverType.Unknown, Software: 0, (uint) creationFlags,
                                                    in featureLevel, FeatureLevels: 1, D3D11.SdkVersion,
                                                    ref device, ref usedFeatureLevel, ref deviceContext);
                if (result.IsFailure)
                {
                    if (index == graphicsProfiles.Length - 1)
                        result.Throw();
                    else
                        continue;
                }

                nativeDevice = device;
                nativeDeviceContext = deviceContext;

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
                HResult result = nativeDevice->QueryInterface(out ComPtr<ID3D11InfoQueue> infoQueue);

                if (result.IsSuccess && infoQueue.IsNotNull())
                {
                    nativeInfoQueue = infoQueue;

                    infoQueue.SetMessageCountLimit(1000);
                    infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                    infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
                    infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, false);
                }
            }
        }

        /// <summary>
        ///   Called when a message is received from the Direct3D 11 InfoQueue.
        ///   This method calls the <see cref="DeviceInfoQueueMessage"/> event handler.
        /// </summary>
        /// <param name="message">The message received from the InfoQueue.</param>
        private void OnDeviceInfoQueueMessage(ref readonly Message message)
        {
            var eventHandler = DeviceInfoQueueMessage;
            if (eventHandler is null)
                return;

            var descriptionSpan = new ReadOnlySpan<byte>(message.PDescription, (int) message.DescriptionByteLength);
            var description = descriptionSpan.GetString();

            eventHandler(in message, description);
        }

        /// <summary>
        ///   Processes all messages stored in the information queue, and invokes the <see cref="DeviceInfoQueueMessage"/> event handler
        ///   for each message.
        /// </summary>
        internal void ProcessInfoQueueMessages()
        {
            Debug.Assert(nativeInfoQueue is not null, "NativeInfoQueue is null. Ensure that the Graphics Device is initialized with the Debug flag.");

            var numMessages = nativeInfoQueue->GetNumStoredMessages();
            if (numMessages == 0)
                return;

            // If no event handler is registered, just clear the messages
            var eventHandler = DeviceInfoQueueMessage;
            if (eventHandler is null)
            {
                nativeInfoQueue->ClearStoredMessages();
                return;
            }

            for (var i = 0ul; i < numMessages; i++)
            {
                ProcessMessage(i);
            }

            nativeInfoQueue->ClearStoredMessages();

            //
            // Retrieves a message from the InfoQueue and invokes the event handler.
            //
            void ProcessMessage(ulong index)
            {
                nuint messageLength = default;
                HResult result = nativeInfoQueue->GetMessageA(index, pMessage: null, ref messageLength);

                if (result.IsFailure)
                    result.Throw();

                Span<byte> messageBytes = stackalloc byte[(int) messageLength];

                ref var message = ref Unsafe.As<byte, Message>(ref messageBytes[0]);
                result = nativeInfoQueue->GetMessageA(index, ref message, ref messageLength);

                if (result.IsFailure)
                    result.Throw();

                OnDeviceInfoQueueMessage(in message);
            }
        }

        /// <summary>
        ///   Represents a method that handles messages related to Graphics Device information.
        /// </summary>
        /// <param name="message">A reference to the message containing graphics device information. This parameter is read-only.</param>
        /// <param name="description">An optional description providing additional context about the message. Can be <see langword="null"/>.</param>
        public delegate void GraphicsDeviceInfoMessageHandler(ref readonly Message message, string? description);

        /// <summary>
        ///   Occurs when a message is received in the Graphics Device information queue.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This event is triggered when the Direct3D 11 InfoQueue receives a message.
        ///     This only happens if the Graphics Device was created with the <see cref="DeviceCreationFlags.Debug"/> flag.
        ///   </para>
        ///   <para>
        ///     Subscribe to this event to handle messages related to Graphics Device information.
        ///     The event handler receives an argument of type <see cref="Message"/>, which contains the message data.
        ///   </para>
        /// </remarks>
        public event GraphicsDeviceInfoMessageHandler? DeviceInfoQueueMessage;

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
            foreach (var query in disjointQueries)
            {
                query.Release();
            }
            disjointQueries.Clear();

            nativeDeviceContext->ClearState();
            nativeDeviceContext->Flush();

            if (IsDebugMode)
            {
                // Display D3D11 ref counting info
                HResult result = nativeDevice->QueryInterface(out ComPtr<ID3D11Debug> debugDevice);

                if (result.IsSuccess && debugDevice.IsNotNull())
                {
                    debugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
                    debugDevice.Release();
                }

                // Process any messages in the InfoQueue before releasing the device
                ProcessInfoQueueMessages();

                // Release the InfoQueue
                nativeInfoQueue->ClearStoredMessages();
                SafeRelease(ref nativeInfoQueue);
            }

            SafeRelease(ref nativeDevice);
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
