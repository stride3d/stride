// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;

#if STRIDE_GRAPHICS_API_DIRECT3D11
using BackBufferResourceType = Silk.NET.Direct3D11.ID3D11Texture2D;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
using BackBufferResourceType = Silk.NET.Direct3D12.ID3D12Resource;
#endif

namespace Stride.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public unsafe class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private readonly Texture backBuffer;

        private readonly bool flipModelSupport;
        private readonly bool tearingSupport;

        private bool useFlipModel;

        private IDXGISwapChain* swapChain;

        private int bufferCount;

#if STRIDE_GRAPHICS_API_DIRECT3D12
        private uint bufferSwapIndex;
#endif

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            PresentInterval = presentationParameters.PresentationInterval;

            CheckDeviceFeatures(out flipModelSupport, out tearingSupport);

            // Initialize the swap chain
            swapChain = CreateSwapChain();

            var nativeBackBuffer = GetBackBuffer<BackBufferResourceType>(0);

            backBuffer = new Texture(device).InitializeFromImpl(nativeBackBuffer, Description.BackBufferFormat.IsSRgb());

            // Reload should get backbuffer from swapchain as well
            //backBuffer.Reload = graphicsResource => ((Texture)graphicsResource).Recreate(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture>(0));

            void CheckDeviceFeatures(out bool supportsFlipModel, out bool supportsTearing)
            {
                var nativeAdapter = device.Adapter.NativeAdapter;

#if STRIDE_GRAPHICS_API_DIRECT3D11
                supportsFlipModel = CheckFlipModelSupport(nativeAdapter);

#elif STRIDE_GRAPHICS_API_DIRECT3D12

                // From MSDN: https://learn.microsoft.com/en-us/windows/win32/api/dxgi/ne-dxgi-dxgi_swap_effect

                //   DXGI_SWAP_EFFECT_DISCARD or DXGI_SWAP_EFFECT_SEQUENTIAL:
                //     This enumeration value is never supported. D3D12 apps must use DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL
                //     or DXGI_SWAP_EFFECT_FLIP_DISCARD.

                supportsFlipModel = true;
#endif
                supportsTearing = CheckTearingSupport(nativeAdapter);
            }

#if STRIDE_GRAPHICS_API_DIRECT3D11
            static bool CheckFlipModelSupport(IDXGIAdapter1* adapter)
            {
                // From https://github.com/walbourn/directx-vs-templates/blob/main/d3d11game_win32_dr/DeviceResources.cpp#L138
                IDXGIFactory4* dxgiAdapterFactory4 = null;
                HResult result = adapter->GetParent(SilkMarshal.GuidPtrOf<IDXGIFactory4>(), (void**) &dxgiAdapterFactory4);

                // The requested interfaces need at least Windows 8
                var supportsFlipModel = result.IsSuccess && dxgiAdapterFactory4 is not null;

                dxgiAdapterFactory4->Release();
                return supportsFlipModel;
            }
#endif

            static unsafe bool CheckTearingSupport(IDXGIAdapter1* adapter)
            {
                // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
                IDXGIFactory5* dxgiAdapterFactory5 = null;
                HResult result = adapter->GetParent(SilkMarshal.GuidPtrOf<IDXGIFactory5>(), (void**) &dxgiAdapterFactory5);

                if (result.IsFailure || dxgiAdapterFactory5 is null)
                    return false;

                // The requested interfaces need at least Windows 10
                int allowTearing = 0;
                result = dxgiAdapterFactory5->CheckFeatureSupport(Feature.PresentAllowTearing, ref allowTearing, sizeof(int));

                var supportsTearing = result.IsSuccess && allowTearing != 0;

                dxgiAdapterFactory5->Release();
                return supportsTearing;
            }
        }

        /// <summary>
        ///   Gets one of the swap-chain back buffers.
        /// </summary>
        /// <typeparam name="TD3DResource">The interface of the surface to resolve from the back buffer.</typeparam>
        /// <param name="index">
        ///   A zero-based buffer index. If the swap effect is not DXGI_SWAP_EFFECT_SEQUENTIAL, this method only has
        ///   access to the first buffer; for this case, set the index to zero.
        /// </param>
        /// <returns>Returns a reference to a back-buffer interface.</returns>
        private TD3DResource* GetBackBuffer<TD3DResource>(uint index) where TD3DResource : unmanaged
        {
            TD3DResource* resource;

            swapChain->GetBuffer(index, SilkMarshal.GuidPtrOf<TD3DResource>(), (void**) &resource);

            //return CppObject.FromPointer<TD3DResource>(surfaceOut);
            return resource;
        }

        public override Texture BackBuffer => backBuffer;

        public override object NativePresenter => new ComPtr<IDXGISwapChain> { Handle = swapChain };

        public override bool IsFullScreen
        {
            get
            {
#if STRIDE_PLATFORM_UWP
                return false;
#else
                bool isFullScreen = GetFullScreenState(out var output);

                if (output is not null) output->Release();

                return isFullScreen;
#endif
            }

            set
            {
#if !STRIDE_PLATFORM_UWP
                if (swapChain is null)
                    return;

                var outputIndex = Description.PreferredFullScreenOutputIndex;

                var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length
                    ? GraphicsDevice.Adapter.Outputs[outputIndex]
                    // There are no outputs connected to the current graphics adapter
                    : null;

                bool isCurrentlyFullscreen = GetFullScreenState(out var currentOutput);

                if (currentOutput is not null)
                    currentOutput->Release();

                // Check if the current fullscreen monitor is the same as the new one.
                // If not fullscreen, currentOutput will be null but output won't be, so don't compare them
                if (isCurrentlyFullscreen == value &&
                    (isCurrentlyFullscreen == false || (output != null && currentOutput != null && currentOutput == output.NativeOutput)))
                    return;

                bool switchToFullScreen = value;

                // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
                var description = new ModeDesc()
                {
                    Width = (uint) backBuffer.ViewWidth,
                    Height = (uint) backBuffer.ViewHeight,
                    RefreshRate = Description.RefreshRate.ToSilk(),
                    Format = (Format) Description.BackBufferFormat
                };
                if (switchToFullScreen)
                {
                    OnDestroyed();

                    Description.IsFullScreen = true;

                    OnRecreated();
                }
                else
                {
                    Description.IsFullScreen = false;
                    HResult result = swapChain->SetFullscreenState(Fullscreen: 0, pTarget: null);

                    if (result.IsFailure)
                        result.Throw();

                    // Call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                    Resize(backBuffer.ViewWidth, backBuffer.ViewHeight, backBuffer.ViewFormat);
                }

                // If going to window mode:
                if (!switchToFullScreen)
                {
                    // Call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                    description.RefreshRate = default;
                    HResult result = swapChain->ResizeTarget(in description);

                    if (result.IsFailure)
                        result.Throw();
                }
#endif
            }
        }

        private bool GetFullScreenState(out IDXGIOutput* fullScreenOutput)
        {
            int isFullScreen;
            IDXGIOutput* output;
            swapChain->GetFullscreenState(&isFullScreen, &output);

            fullScreenOutput = output;
            return isFullScreen != 0;
        }

        public override void BeginDraw(CommandList commandList)
        {
        }

        public override void EndDraw(CommandList commandList, bool present)
        {
        }

        public override void Present()
        {
            var presentInterval = GraphicsDevice.Tags.Get(ForcedPresentInterval) ?? PresentInterval;

            // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
            //   DXGI_PRESENT_ALLOW_TEARING can only be used with sync interval 0. It is recommended to always pass this
            //   tearing flag when using sync interval 0 if CheckFeatureSupport reports that tearing is supported and the
            //   app is in a windowed mode - including border-less fullscreen mode.

            var presentFlags = useFlipModel && tearingSupport && presentInterval == PresentInterval.Immediate && !Description.IsFullScreen
                ? DXGI.PresentAllowTearing
                : 0;

            HResult result = swapChain->Present((uint) presentInterval,  presentFlags);

            if (result.IsFailure)
            {
                var deviceStatus = GraphicsDevice.GraphicsDeviceStatus;

                var exception = Marshal.GetExceptionForHR(result);
                throw new GraphicsException($"Unexpected error on Present (device status: {deviceStatus})", exception, deviceStatus);
            }

#if STRIDE_GRAPHICS_API_DIRECT3D12
            // Manually swap back buffer
            backBuffer.NativeResource->Release();
            bufferSwapIndex = (uint)((++bufferSwapIndex) % bufferCount);
            var nextBackBuffer = GetBackBuffer<BackBufferResourceType>(bufferSwapIndex);
            backBuffer.InitializeFromImpl(nextBackBuffer, Description.BackBufferFormat.IsSRgb());
#endif
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();

            if (Name != null && GraphicsDevice != null && GraphicsDevice.IsDebugMode && swapChain != null)
            {
                DebugHelpers.SetDebugName((IDXGIObject*) swapChain, Name);
            }
        }

        protected internal override void OnDestroyed()
        {
            // Manually update back buffer texture
            backBuffer.OnDestroyed();
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Destroyed;

            swapChain->Release();
            swapChain = null;

            base.OnDestroyed();
        }

        public override void OnRecreated()
        {
            base.OnRecreated();

            // Recreate the swap chain
            swapChain = CreateSwapChain();

            // Get the newly created native texture
            var backBufferTexture = GetBackBuffer<BackBufferResourceType>(0);

            // Put it in our back buffer texture
            // TODO: Update new size
            backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb());
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Active;
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            HResult result;

            // Manually update back buffer texture
            backBuffer.OnDestroyed();

            // Manually update all children textures
            var childrenTextures = DestroyChildrenTextures(backBuffer);

#if STRIDE_PLATFORM_UWP
            if (Description.DeviceWindowHandle.NativeWindow is Windows.UI.Xaml.Controls.SwapChainPanel swapChainPanel)
            {
                IDXGISwapChain2* swapChain2;
                result = swapChain->QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain2>(), (void**)&swapChain2);

                if (result.IsSuccess && swapChain2 is not null)
                {
                    Matrix3X2F transform = new()
                    {
                        DXGI11 = 1f / swapChainPanel.CompositionScaleX,
                        DXGI22 = 1f / swapChainPanel.CompositionScaleY
                    };

                    swapChain2->SetMatrixTransform(ref transform);
                    swapChain2->Release();
                }
            }
#endif

            if (useFlipModel)
                format = ToSupportedFlipModelFormat(format); // See CreateSwapChainForDesktop

            // If format is same as before, using Unknown (None) will keep the current
            // We do that because on Win10/RT, actual format might be the non-srgb one and we don't want to switch to srgb one by mistake (or need #ifdef)
            // Eideren: the comment above isn't very clear, I think they mean that we don't want to swap to srgb because it'll crash with flip model
            //          I've added the flip model check above because the previous logic wasn't enough, see issue #1770
            //          Testing against swapChain format instead of the backbuffer as they may not match.

            SwapChainDesc swapChainDesc;
            result = swapChain->GetDesc(&swapChainDesc);

            if (result.IsFailure)
                result.Throw();

            if ((Format) format == swapChainDesc.BufferDesc.Format)
                format = PixelFormat.None;

            result = swapChain->ResizeBuffers((uint) bufferCount, (uint) width, (uint) height, (Format) format, (uint) GetSwapChainFlags());

            if (result.IsFailure)
                result.Throw();

            // Get newly created native texture
            var backBufferTexture = GetBackBuffer<BackBufferResourceType>(0);

            // Put it in our back buffer texture
            backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb());

            foreach (var texture in childrenTextures)
            {
                texture.InitializeFrom(backBuffer, texture.ViewDescription);
            }
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescription = DepthStencilBuffer.Description with
            {
                Width = width,
                Height = height
            };

            // Manually update the texture
            DepthStencilBuffer.OnDestroyed();

            // Manually update all children textures
            var fastList = DestroyChildrenTextures(DepthStencilBuffer);

            // Put it in our back buffer texture
            DepthStencilBuffer.InitializeFrom(newTextureDescription);

            foreach (var texture in fastList)
            {
                texture.InitializeFrom(DepthStencilBuffer, texture.ViewDescription);
            }
        }

        /// <summary>
        /// Calls <see cref="Texture.OnDestroyed"/> for all children of the specified texture
        /// </summary>
        /// <param name="parentTexture">Specified parent texture</param>
        /// <returns>A list of the children textures which were destroyed</returns>
        private List<Texture> DestroyChildrenTextures(Texture parentTexture)
        {
            var childrenTextures = new List<Texture>();
            var resources = GraphicsDevice.Resources;
            
            lock (resources)
            {
                foreach (var resource in resources)
                {
                    if (resource is Texture texture && texture.ParentTexture == parentTexture)
                    {
                        texture.OnDestroyed();
                        childrenTextures.Add(texture);
                    }
                }
            }

            return childrenTextures;
        }

        private IDXGISwapChain* CreateSwapChain()
        {
            // Check for Window Handle parameter
            if (Description.DeviceWindowHandle == null)
            {
                throw new ArgumentException("DeviceWindowHandle cannot be null");
            }

#if STRIDE_PLATFORM_UWP
            return CreateSwapChainForUWP();
#else
            return CreateSwapChainForWindows();
#endif
        }

#if STRIDE_PLATFORM_UWP
        private IDXGISwapChain* CreateSwapChainForUWP()
        {
            bufferCount = 2;

            var description = new SwapChainDesc1
            {
                // Automatic sizing
                Width = (uint) Description.BackBufferWidth,
                Height = (uint) Description.BackBufferHeight,
                Format = (Format) Description.BackBufferFormat.ToNonSRgb(),
                Stereo = 0,
                SampleDesc = new SampleDesc(count: (uint) Description.MultisampleCount, quality: 0),
                BufferUsage = USAGE_BACKBUFFER | USAGE_RENDER_TARGET_OUTPUT,
                BufferCount = (uint) bufferCount, // Use two buffers to enable flip effect
                Scaling = Scaling.ScalingStretch,
                SwapEffect = SwapEffect.FlipSequential
            };

            IDXGISwapChain1* swapChain = null;
            var deviceAsIUnknown = (IUnknown*) GraphicsDevice.NativeDevice;

            switch (Description.DeviceWindowHandle.Context)
            {
                case Games.AppContextType.UWPXaml:
                {
                    var hWindow = Description.DeviceWindowHandle.Handle;
                    var nativePanel = GetNativePanelFromHandle(Description.DeviceWindowHandle);

                    var swapChainFactory = (IDXGIFactory2*) GraphicsAdapterFactory.NativeFactory;

                    // Creates the swapchain for XAML composition
                    HResult result = swapChainFactory->CreateSwapChainForHwnd(deviceAsIUnknown, hWindow, &description, pFullscreenDesc: null, pRestrictToOutput: null, &swapChain);

                    // Associate the SwapChainPanel with the swap chain
                    nativePanel.SwapChain = swapChain;

                    break;

                    /// <summary>
                    ///   Gets an <see cref="ISwapChainPanelNative"/> from the handle of the window.
                    /// </summary>
                    static ISwapChainPanelNative GetNativePanelFromHandle(WindowHandle windowHandle)
                    {
                        var nativeWindow = windowHandle.NativeWindow;
                        var comPtr = Marshal.GetIUnknownForObject(nativeWindow);

                        HResult result = Marshal.QueryInterface(comPtr, ref SilkMarshal.GuidOf<ISwapChainPanelNative>(), out var ptrPanel);

                        if (result.IsFailure || ptrPanel == IntPtr.Zero)
                            result.Throw();

                        return new ISwapChainPanelNative(ptrPanel);
                    }
                }
                case Games.AppContextType.UWPCoreWindow:
                {
                    IDXGIDevice2* dxgiDevice;
                    HResult result = GraphicsDevice.NativeDevice->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIDevice2>(), (void**) &dxgiDevice);

                    if (result.IsFailure)
                        result.Throw();

                    // Ensure that DXGI does not queue more than one frame at a time. This both reduces
                    // latency and ensures that the application will only render after each VSync, minimizing
                    // power consumption.
                    dxgiDevice->SetMaximumFrameLatency(1);

                    // Next, get the parent factory from the DXGI Device
                    IDXGIAdapter* dxgiAdapter;
                    dxgiDevice->GetAdapter(&dxgiAdapter);

                    IDXGIFactory2* dxgiFactory;
                    result = dxgiAdapter->GetParent(SilkMarshal.GuidPtrOf<IDXGIFactory2>(), (void**) &dxgiFactory);

                    if (result.IsFailure)
                        result.Throw();

                    // Finally, create the swapchain
                    var coreWindow = (IUnknown*) Marshal.GetIUnknownForObject(Description.DeviceWindowHandle.NativeWindow);

                    result = dxgiFactory->CreateSwapChainForCoreWindow(deviceAsIUnknown, coreWindow, &description, pRestrictToOutput: null, &swapChain);

                    if (result.IsFailure)
                        result.Throw();

                    if (coreWindow != null) coreWindow->Release();
                    if (dxgiFactory != null) dxgiFactory->Release();
                    if (dxgiAdapter != null) dxgiAdapter->Release();
                    if (dxgiDevice != null) dxgiDevice->Release();
                    break;
                }
                default:
                    throw new NotSupportedException($"Window context [{Description.DeviceWindowHandle.Context}] not supported while creating SwapChain");
            }

            return (IDXGISwapChain*) swapChain;
        }
#else
        /// <summary>
        /// Create the SwapChain on Windows.
        /// </summary>
        /// <returns></returns>
        private IDXGISwapChain* CreateSwapChainForWindows()
        {
            var hwndPtr = Description.DeviceWindowHandle.Handle;
            if (hwndPtr != IntPtr.Zero)
            {
                return CreateSwapChainForDesktop(hwndPtr);
            }
            throw new InvalidOperationException($"The {nameof(WindowHandle)}.{nameof(WindowHandle.Handle)} must not be zero.");
        }

        private IDXGISwapChain* CreateSwapChainForDesktop(IntPtr handle)
        {
#if STRIDE_GRAPHICS_API_DIRECT3D12
            useFlipModel = true;
#else
            // https://devblogs.microsoft.com/directx/dxgi-flip-model/#what-do-i-have-to-do-to-use-flip-model
            useFlipModel = Description.MultisampleCount == MultisampleCount.None && flipModelSupport;
#endif

            var swapchainFormat = Description.BackBufferFormat;
            bufferCount = 1;

            if (useFlipModel)
            {
                swapchainFormat = ToSupportedFlipModelFormat(swapchainFormat);
                bufferCount = 2;
            }

            var description = new SwapChainDesc
            {
                BufferDesc = new ModeDesc
                {
                    Width = (uint) Description.BackBufferWidth,
                    Height = (uint) Description.BackBufferHeight,
                    RefreshRate = Description.RefreshRate.ToSilk(),
                    Format = (Format) swapchainFormat
                },
                BufferCount = (uint) bufferCount, // TODO: Do we really need this to be configurable by the user?
                OutputWindow = handle,
                SampleDesc = new SampleDesc(count: (uint) Description.MultisampleCount, quality: 0),
                SwapEffect = useFlipModel ? SwapEffect.FlipDiscard : SwapEffect.Discard,
                BufferUsage = DXGI.UsageBackBuffer | DXGI.UsageRenderTargetOutput,
                Windowed = 1,
                Flags = (uint) GetSwapChainFlags()
            };

            IDXGISwapChain* newSwapChain;

#if STRIDE_GRAPHICS_API_DIRECT3D11
            HResult result = GraphicsAdapterFactory.NativeFactory->CreateSwapChain((IUnknown*) GraphicsDevice.NativeDevice, &description, &newSwapChain);
#elif STRIDE_GRAPHICS_API_DIRECT3D12
            HResult result = GraphicsAdapterFactory.NativeFactory->CreateSwapChain((IUnknown*) GraphicsDevice.NativeCommandQueue, &description, &newSwapChain);
#endif
            if (result.IsFailure)
                result.Throw();

            // We need a IDXGISwapChain3 to enable output color space setting to support HDR outputs
            IDXGISwapChain3* swapChain3;
            result = newSwapChain->QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain3>(), (void**) &swapChain3);

            if (result.IsSuccess)
            {
                swapChain3->SetColorSpace1((Silk.NET.DXGI.ColorSpaceType) Description.OutputColorSpace);
                swapChain3->Release();
            }

            // Prevent switching between windowed and full screen modes by pressing Alt+ENTER
            const uint DXGI_MWA_NO_ALT_ENTER = 2;
            GraphicsAdapterFactory.NativeFactory->MakeWindowAssociation(handle, DXGI_MWA_NO_ALT_ENTER);

            if (Description.IsFullScreen)
            {
                // Before fullscreen switch
                newSwapChain->ResizeTarget(in description.BufferDesc);

                // Switch to full screen
                newSwapChain->SetFullscreenState(Fullscreen: 1, null);

                // This is really important to call ResizeBuffers AFTER switching to IsFullScreen
                newSwapChain->ResizeBuffers((uint) bufferCount, (uint) Description.BackBufferWidth, (uint) Description.BackBufferHeight, NewFormat: default, description.Flags);
            }

            return newSwapChain;
        }

        private SwapChainFlag GetSwapChainFlags()
        {
            SwapChainFlag flags = 0;

            if (Description.IsFullScreen)
                flags |= SwapChainFlag.AllowModeSwitch;

            // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
            // It is recommended to always use the tearing flag when it is supported.
            if (useFlipModel && tearingSupport)
                flags |= SwapChainFlag.AllowTearing;

            return flags;
        }
#endif

        /// <summary>
        ///   The flip model does not support certain formats, this method ensures it is in a supported format.
        /// </summary>
        /// <param name="pixelFormat">The pixel format to convert to a format supported by the flip model.</param>
        /// <exception cref="ArgumentException">
        ///   Will throw if the given <paramref name="pixelFormat"/> does not have a direct analog supported by
        ///   the flip model.
        /// </exception>
        /// <remarks>
        ///   To learn more about the DXGI flip model, see <see href="https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model"/>.
        ///   <br/>
        ///   For more information on HDR output, see <see href="https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range"/>.
        /// </remarks>
        private static PixelFormat ToSupportedFlipModelFormat(PixelFormat pixelFormat)
        {
            var nonSRgb = pixelFormat.ToNonSRgb();
            return nonSRgb switch
            {
                PixelFormat.R16G16B16A16_Float or // scRGB HDR, should use PresenterColorSpace.RgbFullG10NoneP709, gets converted by Windows to display color space
                PixelFormat.R10G10B10A2_UNorm or  // HDR10 / BT.2100 HDR, should use PresenterColorSpace.RgbFullG2084NoneP2020, directly sent to display
                PixelFormat.B8G8R8A8_UNorm or
                PixelFormat.R8G8B8A8_UNorm => nonSRgb,

                _ => throw new ArgumentException($"Format '{pixelFormat}' is not supported when using a flip model swapchain", nameof(pixelFormat))
            };
        }
    }
}

#endif
