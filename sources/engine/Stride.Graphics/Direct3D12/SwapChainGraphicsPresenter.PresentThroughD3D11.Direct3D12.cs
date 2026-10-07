// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Threading;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

using Stride.Core.Diagnostics;

using ResourceDimension = Silk.NET.Direct3D12.ResourceDimension;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class SwapChainGraphicsPresenter
    {
        // With STRIDE_GRAPHICS_PRESENT_THROUGH_D3D11=1, a Direct3D 11 Swap-Chain presents the frames: Direct3D 12 renders
        // into a shared Back-Buffer, which Direct3D 11 copies into its Swap-Chain. On an indirect display (IDD) virtual
        // monitor, DWM crashes in a loop while a Direct3D 12 Swap-Chain presents with WARP 1.0.13 or later
        // (Microsoft.Direct3D.WARP, as pinned by the editor tests); Direct3D 11 Swap-Chains are not affected.
        // Workaround only: remove it once the pinned WARP has a fix (repro: https://github.com/xen2/warp-dwm-repro).
#if STRIDE_PLATFORM_UWP
        private static readonly bool presentThroughD3D11 = false;
#else
        private static readonly bool presentThroughD3D11 = Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_PRESENT_THROUGH_D3D11") == "1";
#endif

        private static readonly Logger Log = GlobalLogger.GetLogger(nameof(SwapChainGraphicsPresenter));

        private const uint GenericAll = 0x10000000;

        private ID3D11Device1* d3d11Device;
        private ID3D11DeviceContext* d3d11Context;
        private ID3D11Query* d3d11CopyDone;

        // The shared Back-Buffer, opened in Direct3D 11
        private ID3D11Texture2D* d3d11SharedBackBuffer;

        // A fence both devices share, to order the frame and the copy on the GPU. Null when Direct3D 11 can't open it:
        // the CPU then waits for each side instead.
        private ID3D12Fence* sharedFence;
        private ID3D11Fence* d3d11SharedFence;
        private ID3D11DeviceContext4* d3d11Context4;
        private ulong sharedFenceValue;

        /// <summary>
        ///   Gets the Direct3D 11 Device that presents the frames, created on the Graphics Device's adapter.
        /// </summary>
        private ComPtr<IUnknown> GetD3D11PresentDevice()
        {
            if (d3d11Device is null)
            {
                try
                {
                    CreateD3D11PresentDevice();
                }
                catch
                {
                    ReleaseD3D11Presenter();
                    throw;
                }
            }

            return ToComPtr((IUnknown*) d3d11Device);
        }

        private void CreateD3D11PresentDevice()
        {
            var d3d11 = D3D11.GetApi(window: null);

            ComPtr<ID3D11Device> device = default;
            ComPtr<ID3D11DeviceContext> context = default;
            var featureLevel = D3DFeatureLevel.Level110;
            D3DFeatureLevel usedFeatureLevel = 0;

            HResult result = d3d11.CreateDevice(GraphicsDevice.Adapter.NativeAdapter, D3DDriverType.Unknown, Software: 0,
                                                (uint) CreateDeviceFlag.BgraSupport, in featureLevel, FeatureLevels: 1,
                                                D3D11.SdkVersion, ref device, ref usedFeatureLevel, ref context);
            if (result.IsFailure)
                result.Throw();
            d3d11Context = context;

            result = device.QueryInterface(out ComPtr<ID3D11Device1> device1);
            device.Release();
            if (result.IsFailure)
                result.Throw();
            d3d11Device = device1;

            var queryDescription = new QueryDesc(Query.Event);
            ComPtr<ID3D11Query> query = default;
            result = device1.CreateQuery(in queryDescription, ref query);
            if (result.IsFailure)
                result.Throw();
            d3d11CopyDone = query;

            CreateSharedFence();
        }

        /// <summary>
        ///   Creates the fence that Direct3D 12 signals and Direct3D 11 opens, or leaves it null if Direct3D 11 can't open it.
        /// </summary>
        private void CreateSharedFence()
        {
            var nativeDevice = GraphicsDevice.NativeDevice;
            ComPtr<ID3D11Device5> device5 = default;
            ComPtr<ID3D11DeviceContext4> context4 = default;
            ComPtr<ID3D12Fence> fence = default;
            void* d3d11Fence = null;

            int hr = ToComPtr(d3d11Device).QueryInterface(out device5);
            if (hr >= 0)
                hr = ToComPtr(d3d11Context).QueryInterface(out context4);
            if (hr >= 0)
                hr = nativeDevice.CreateFence(InitialValue: 0, FenceFlags.Shared, out fence);
            if (hr >= 0)
            {
                void* sharedHandle = null;
                hr = nativeDevice.CreateSharedHandle((ID3D12DeviceChild*) fence.Handle, (SecurityAttributes*) null, GenericAll, (char*) null, &sharedHandle);
                if (hr >= 0)
                {
                    var fenceGuid = SilkMarshal.GuidOf<ID3D11Fence>();
                    hr = device5.OpenSharedFence(sharedHandle, &fenceGuid, &d3d11Fence);
                    Win32.CloseHandle((IntPtr) sharedHandle);
                }
            }
            SafeRelease(ref device5);

            if (hr < 0)
            {
                SafeRelease(ref context4);
                SafeRelease(ref fence);
                Log.Warning($"[D3D12] Direct3D 11 can't open a Direct3D 12 fence (0x{hr:X8}); presenting through Direct3D 11 waits on the CPU.");
                return;
            }

            d3d11Context4 = context4;
            sharedFence = fence;
            d3d11SharedFence = (ID3D11Fence*) d3d11Fence;
            sharedFenceValue = 0;
        }

        /// <summary>
        ///   Creates a shared Direct3D 12 Back-Buffer of the Swap-Chain's size, and opens it in Direct3D 11.
        /// </summary>
        /// <returns>The Back-Buffer. It must be released when discarding the reference.</returns>
        private ComPtr<ID3D12Resource> CreateSharedBackBuffer()
        {
            SafeRelease(ref d3d11SharedBackBuffer);

            SwapChainDesc1 swapChainDescription = default;
            HResult result = swapChain->GetDesc1(ref swapChainDescription);
            if (result.IsFailure)
                result.Throw();

            // Direct3D 12 allows sRGB views of a non-sRGB resource only for real Swap-Chain buffers:
            // use the sRGB format when the Back-Buffer is treated as sRGB. The copy keeps the bits.
            var format = (PixelFormat) swapChainDescription.Format;
            if (Description.BackBufferFormat.IsSRgb)
                format = format.ToSRgb();

            var description = new ResourceDesc
            {
                Dimension = ResourceDimension.Texture2D,
                Width = swapChainDescription.Width,
                Height = swapChainDescription.Height,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = (Format) format,
                SampleDesc = new SampleDesc(count: 1, quality: 0),
                Flags = ResourceFlags.AllowRenderTarget
            };
            var heap = new HeapProperties { Type = HeapType.Default };

            // Common is also the Present layout that EndDraw transitions the Back-Buffer to, which Direct3D 11 reads from
            var nativeDevice = GraphicsDevice.NativeDevice;
            result = nativeDevice.CreateCommittedResource(in heap, HeapFlags.Shared, in description, ResourceStates.Common,
                                                          pOptimizedClearValue: null, out ComPtr<ID3D12Resource> texture);
            if (result.IsFailure)
                result.Throw();

            void* sharedHandle = null;
            result = nativeDevice.CreateSharedHandle((ID3D12DeviceChild*) texture.Handle, (SecurityAttributes*) null, GenericAll, (char*) null, &sharedHandle);
            if (result.IsFailure)
            {
                texture.Release();
                result.Throw();
            }

            var textureGuid = SilkMarshal.GuidOf<ID3D11Texture2D>();
            void* d3d11Texture = null;
            result = d3d11Device->OpenSharedResource1(sharedHandle, &textureGuid, &d3d11Texture);
            Win32.CloseHandle((IntPtr) sharedHandle);
            if (result.IsFailure)
            {
                texture.Release();
                result.Throw();
            }

            d3d11SharedBackBuffer = (ID3D11Texture2D*) d3d11Texture;
            return texture;
        }

        /// <summary>
        ///   Copies the rendered Back-Buffer into the Direct3D 11 Swap-Chain, before it presents.
        ///   The frame's command lists are already submitted.
        /// </summary>
        private void CopyBackBufferToD3D11SwapChain()
        {
            var commandQueue = GraphicsDevice.NativeCommandQueue;

            // Direct3D 11 waits for Direct3D 12 to finish writing the Back-Buffer.
            // A removed device refuses the signal: skip the wait, the next BeginDraw reports the loss.
            bool gpuWait = sharedFence is not null && commandQueue.Signal(sharedFence, ++sharedFenceValue) >= 0;
            if (gpuWait)
                d3d11Context4->Wait(d3d11SharedFence, sharedFenceValue);
            else
                GraphicsDevice.WaitForGpuIdle();

            var swapChainBuffer = GetBackBuffer<ID3D11Texture2D>();
            d3d11Context->CopyResource((ID3D11Resource*) swapChainBuffer.Handle, (ID3D11Resource*) d3d11SharedBackBuffer);
            swapChainBuffer.Release();

            // Direct3D 12 waits for the copy before it renders the next frame into the Back-Buffer
            if (gpuWait)
            {
                d3d11Context4->Signal(d3d11SharedFence, ++sharedFenceValue);
                d3d11Context->Flush();
                commandQueue.Wait(sharedFence, sharedFenceValue);
            }
            else
            {
                WaitForD3D11Copy();
            }
        }

        /// <summary>
        ///   Waits on the CPU for Direct3D 11 to finish the copy.
        /// </summary>
        private void WaitForD3D11Copy()
        {
            const int S_FALSE = 1;

            d3d11Context->End((ID3D11Asynchronous*) d3d11CopyDone);
            d3d11Context->Flush();

            int done = 0;
            while (d3d11Context->GetData((ID3D11Asynchronous*) d3d11CopyDone, &done, sizeof(int), GetDataFlags: 0) == S_FALSE)
                Thread.Yield();
        }

        /// <summary>
        ///   Releases the Direct3D 11 Device, the shared fence and the shared Back-Buffer, if any.
        /// </summary>
        private void ReleaseD3D11Presenter()
        {
            SafeRelease(ref d3d11SharedBackBuffer);
            SafeRelease(ref d3d11SharedFence);
            SafeRelease(ref sharedFence);
            SafeRelease(ref d3d11Context4);
            SafeRelease(ref d3d11CopyDone);
            SafeRelease(ref d3d11Context);
            SafeRelease(ref d3d11Device);
        }
    }
}

#endif
