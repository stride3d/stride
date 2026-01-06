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
using System.Diagnostics;
using System.Runtime.InteropServices;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;

using Feature = Silk.NET.DXGI.Feature;

#if STRIDE_GRAPHICS_API_DIRECT3D11
using BackBufferResourceType = Silk.NET.Direct3D11.ID3D11Texture2D;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
using BackBufferResourceType = Silk.NET.Direct3D12.ID3D12Resource;
#endif

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    /// <summary>
    ///   A <see cref="GraphicsPresenter"/> wrapping a <strong>DirectX Swap-Chain</strong>
    ///   (<see cref="IDXGISwapChain"/>).
    /// </summary>
    /// <inheritdoc path="/remarks"/>
    public unsafe class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private readonly Texture backBuffer;

        /// <inheritdoc/>
        public override Texture BackBuffer => backBuffer;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        private readonly bool flipModelSupport;
#elif STRIDE_GRAPHICS_API_DIRECT3D12

        // From MSDN: https://learn.microsoft.com/en-us/windows/win32/api/dxgi/ne-dxgi-dxgi_swap_effect

        //   DXGI_SWAP_EFFECT_DISCARD or DXGI_SWAP_EFFECT_SEQUENTIAL:
        //     This enumeration value is never supported. D3D12 apps must use DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL
        //     or DXGI_SWAP_EFFECT_FLIP_DISCARD.
        private readonly bool flipModelSupport = true;
#endif
        private readonly bool tearingSupport;

        private bool useFlipModel;

        // We assume a minimum of IDXGISwapChain1 support (DXGI 1.2, Windows 7+ / UWP)
        private IDXGISwapChain1* swapChain;
        private uint swapChainVersion;

        /// <summary>
        ///   Gets the internal DXGI Swap-Chain.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<IDXGISwapChain1> NativeSwapChain => ToComPtr(swapChain);

        /// <summary>
        ///   Gets the version number of the native DXGI Swap-Chain supported.
        /// </summary>
        /// <value>
        ///   This indicates the latest DXGI Swap-Chain interface version supported by this Swap-Chain.
        ///   For example, if the value is 4, then this Swap-Chain supports up to <see cref="IDXGISwapChain4"/>.
        /// </value>
        internal uint NativeSwapChainVersion => swapChainVersion;

        private int bufferCount;
        private uint bufferSwapIndex;

        // TODO: This boxes the ComPtr, which is not ideal
        /// <inheritdoc/>
        public override object NativePresenter => NativeSwapChain;

        /// <inheritdoc/>
        public override bool IsFullScreen
        {
            get => GetFullScreenState();
            set => SetFullscreenState(value);
        }


        /// <summary>
        ///   Initializes a new instance of the <see cref="SwapChainGraphicsPresenter"/> class.
        /// </summary>
        /// <param name="device">The Graphics Device.</param>
        /// <param name="presentationParameters">
        ///   The parameters describing the buffers the <paramref name="device"/> will present to.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="presentationParameters"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        ///   <see cref="PresentationParameters.DeviceWindowHandle"/> is <see langword="null"/> or
        ///   the <see cref="WindowHandle.Handle"/> is invalid or zero.
        /// </exception>
        /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            PresentInterval = presentationParameters.PresentationInterval;

            CheckDeviceFeatures(out flipModelSupport, out tearingSupport);

            // Initialize the Swap-Chain
            CreateSwapChain();

            // Gets the native Back-Buffer from the Swap-Chain.
            //   This increments the reference count of the COM object,
            //   so we need to Release() it when discarding or swapping it.
            var nativeBackBuffer = GetBackBuffer<BackBufferResourceType>();

            backBuffer = GraphicsDevice.IsDebugMode
                ? new Texture(device, "SwapChain Back-Buffer")
                : new Texture(device);

            // Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
            // compensate with Release() to return the reference count to its previous value
            backBuffer.InitializeFromImpl(nativeBackBuffer, Description.BackBufferFormat.IsSRgb);
            nativeBackBuffer.Release();

            // Reload should get Back-Buffer from Swap-Chain as well
            // TODO: Stale statement/comment?
            //backBuffer.Reload = graphicsResource => ((Texture)graphicsResource).Recreate(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture>(0));

            //
            // Determines if the Graphics Device supports the flip model and tearing.
            //
            static void CheckDeviceFeatures(out bool supportsFlipModel, out bool supportsTearing)
            {
                // TODO: Should we move this to GraphicsAdapterFactory? It's system-wide after all, not adapter-specific

                var dxgiFactory = GraphicsAdapterFactory.NativeFactory;
                var dxgiFactoryVersion = GraphicsAdapterFactory.NativeFactoryVersion;

                supportsFlipModel = CheckFlipModelSupport(dxgiFactoryVersion);
                supportsTearing = CheckTearingSupport(dxgiFactoryVersion, dxgiFactory);
            }

#if STRIDE_GRAPHICS_API_DIRECT3D11
            //
            // Determines if the DXGI adapter and the system supports the flip model.
            // From https://github.com/walbourn/directx-vs-templates/blob/main/d3d11game_win32_dr/DeviceResources.cpp#L138
            //
            static bool CheckFlipModelSupport(uint dxgiFactoryVersion)
            {
                // The requested interfaces need at least Windows 8 and IDXGIFactory4
                return dxgiFactoryVersion >= 4;
            }
#elif STRIDE_GRAPHICS_API_DIRECT3D12

            // From MSDN: https://learn.microsoft.com/en-us/windows/win32/api/dxgi/ne-dxgi-dxgi_swap_effect

            //   DXGI_SWAP_EFFECT_DISCARD or DXGI_SWAP_EFFECT_SEQUENTIAL:
            //     This enumeration value is never supported. D3D12 apps must use DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL
            //     or DXGI_SWAP_EFFECT_FLIP_DISCARD.

            static bool CheckFlipModelSupport(uint dxgiFactoryVersion) => true;
#endif
            //
            // Determines if the DXGI adapter and the system supports tearing, also known as "vsync-off".
            // This flag is particularly useful for variable refresh rate displays.
            // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
            //
            static unsafe bool CheckTearingSupport(uint dxgiFactoryVersion, ComPtr<IDXGIFactory1> dxgiFactory1)
            {
                // The requested interfaces need at least Windows 10 and IDXGIFactory5
                if (dxgiFactoryVersion < 5)
                    return false;

                var dxgiFactory5 = dxgiFactory1.AsComPtrUnsafe<IDXGIFactory1, IDXGIFactory5>();

                int allowTearing = 0;
                HResult result = dxgiFactory5.CheckFeatureSupport(Feature.PresentAllowTearing, ref allowTearing, sizeof(int));

                return result.IsSuccess && allowTearing != 0;
            }
        }

        /// <summary>
        ///   Gets one of the Swap-Chain Back-Buffers.
        /// </summary>
        /// <typeparam name="TD3DResource">The interface of the surface to resolve from the Back-Buffer.</typeparam>
        /// <param name="index">
        ///   A zero-based buffer index.
        ///   If the swap effect is not <see cref="SwapEffect.Sequential"/>, this method only has
        ///   access to the first Buffer; for this case (which is the default), set the index to zero.
        /// </param>
        /// <returns>Returns a reference to a Back-Buffer Texture.</returns>
        private ComPtr<TD3DResource> GetBackBuffer<TD3DResource>(uint index = 0) where TD3DResource : unmanaged, IComVtbl<TD3DResource>
        {
            // NOTE: The Swap-Chain Back-Buffer is a COM object, so this AddRef()s.
            //       It must be released when swapping or discarding the reference.

            swapChain->GetBuffer(index, out ComPtr<TD3DResource> resource);
            return resource;
        }

        /// <summary>
        ///   Determines if the Swap-Chain is presenting in fullscreen mode, and to which output.
        /// </summary>
        /// <param name="fullScreenOutput">
        ///   When this method returns,
        ///   <list type="bullet">
        ///     <item>If the Swap-Chain is presenting in fullscreen mode, contains the output (screen) to which it is presenting.</item>
        ///     <item>If the Swap-Chain is presenting to a window, contains a <see langword="null"/> pointer.</item>
        ///   </list>
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Swap-Chain is in fullscreen mode; <see langword="false"/> otherwise.
        /// </returns>
        private bool GetFullScreenState(out ComPtr<IDXGIOutput> fullScreenOutput)
        {
            int isFullScreen = default;
            fullScreenOutput = default;
            swapChain->GetFullscreenState(ref isFullScreen, ref fullScreenOutput);

            return isFullScreen != 0;
        }

        /// <summary>
        ///   Determines if the Swap-Chain is presenting in fullscreen mode.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if the Swap-Chain is in fullscreen mode; <see langword="false"/> otherwise.
        /// </returns>
        private bool GetFullScreenState()
        {
#if STRIDE_PLATFORM_UWP
            // In UWP, SwapChains are always windowed. The system controls full-screen mode
            return false;
#else
            SkipInit(out int isFullScreen);
            swapChain->GetFullscreenState(ref isFullScreen, ppTarget: null);

            return isFullScreen != 0;
#endif
        }

        /// <summary>
        ///   Sets the presentation mode of the Graphics Presenter.
        /// </summary>
        /// <param name="isFullScreen">
        ///   A value indicating whether the presentation will be in full screen.
        ///   <list type="bullet">
        ///     <item><see langword="true"/> if the presentation will be in full screen.</item>
        ///     <item><see langword="false"/> if the presentation will be in a window.</item>
        ///   </list>
        /// </param>
        private void SetFullscreenState(bool isFullScreen)
        {
#if !STRIDE_PLATFORM_UWP
            if (swapChain is null)
                return;

            var outputIndex = Description.PreferredFullScreenOutputIndex;

            var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length
                    ? GraphicsDevice.Adapter.Outputs[outputIndex]
                    // There are no outputs connected to the current Graphics Adapter
                    : null;

            bool isCurrentlyFullscreen = GetFullScreenState(out var currentOutput);

            if (currentOutput.IsNotNull())
                currentOutput.Release();

            // Check if the current fullscreen monitor is the same as the new one.
            // If not fullscreen, currentOutput will be null but output won't be, so don't compare them
            if (isCurrentlyFullscreen == isFullScreen &&
                (isCurrentlyFullscreen is false || (output is not null && currentOutput.IsNotNull() && currentOutput.Handle == output.NativeOutput.Handle)))
                return;

            bool switchToFullScreen = isFullScreen;

            // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
            var description = new ModeDesc
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

        /// <inheritdoc/>
        public override void BeginDraw(CommandList commandList)
        {
        }

        /// <inheritdoc/>
        public override void EndDraw(CommandList commandList, bool present)
        {
        }

        /// <inheritdoc/>
        /// <exception cref="GraphicsDeviceException">
        ///   An unexpected error occurred while presenting the Swap-Chain. Check the status of the Graphics Device
        ///   for more information (<see cref="GraphicsDeviceException.Status"/>).
        /// </exception>
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
                throw new GraphicsDeviceException($"Unexpected error on Present (device status: {deviceStatus})", exception, deviceStatus);
            }

#if STRIDE_GRAPHICS_API_DIRECT3D12
            // Manually swap the Back-Buffers

            // Gets the native Back-Buffer from the Swap-Chain.
            //   This increments the reference count of the COM object,
            //   so we need to Release() it when discarding or swapping it.
            bufferSwapIndex = (uint)((++bufferSwapIndex) % bufferCount);
            var nextBackBuffer = GetBackBuffer<BackBufferResourceType>(bufferSwapIndex);

            // TODO: Maybe we should have a lighter Texture.SwapImpl method for this?
            //       InitializeFromImpl() is quite heavy for just swapping the internal resource pointer.
            //       It recreates the internal description and other things that for presenting should not have changed.

            // Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
            // compensate with Release() to return the reference count to its previous value
            backBuffer.InitializeFromImpl(nextBackBuffer, Description.BackBufferFormat.IsSRgb);
            nextBackBuffer.Release();
#endif
        }

        /// <inheritdoc/>
        protected override void OnNameChanged()
        {
            base.OnNameChanged();

            if (GraphicsDevice.IsDebugMode is true && Name is not null && swapChain is not null)
            {
                ToComPtr(swapChain).SetDebugName(Name);
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            // Manually update Back-Buffer Texture
            backBuffer.OnDestroyed(immediately);
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Destroyed;

            SafeRelease(ref swapChain);

            base.OnDestroyed(immediately);
        }

        /// <inheritdoc/>
        public override void OnRecreated()
        {
            base.OnRecreated();

            // Recreate the Swap-Chain
            CreateSwapChain();

            // Get the newly created native Texture
            //   This increments the reference count of the COM object,
            //   so we need to Release() it when discarding or swapping it.
            var backBufferTexture = GetBackBuffer<BackBufferResourceType>();
            bufferSwapIndex = 0;

            // Put it in our Back-Buffer Texture
            //   Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
            //   compensate with Release() to return the reference count to its previous value
            backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb);
            backBufferTexture.Release();

            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Active;
        }

        /// <inheritdoc/>
        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            HResult result;

            // Manually update the Back-Buffer Texture
            backBuffer.OnDestroyed(immediately: true);

            // Manually update all children Textures (Views)
            var childrenTextures = DestroyChildrenTextures(backBuffer);

#if STRIDE_PLATFORM_UWP
            if (Description.DeviceWindowHandle.NativeWindow is Windows.UI.Xaml.Controls.SwapChainPanel swapChainPanel)
            {
                if (swapChainVersion >= 2)
                {
                    Matrix3X2F transform = new()
                    {
                        DXGI11 = 1f / swapChainPanel.CompositionScaleX,
                        DXGI22 = 1f / swapChainPanel.CompositionScaleY
                    };

                    var swapChain2 = NativeSwapChain.AsComPtrUnsafe<IDXGISwapChain1, IDXGISwapChain2>();
                    swapChain2.SetMatrixTransform(ref transform);
                }
            }
#endif

            if (useFlipModel)
                format = ToSupportedFlipModelFormat(format); // See CreateSwapChainForDesktop

            // If format is same as before, using Unknown (None) will keep the current
            // We do that because on Win10/RT, actual format might be the non-sRGB one and we don't want to switch to sRGB one by mistake (or need #ifdef)
            // Eideren: the comment above isn't very clear, I think they mean that we don't want to swap to sRGB because it'll crash with flip model
            //          I've added the flip model check above because the previous logic wasn't enough, see issue #1770
            //          Testing against swapChain format instead of the backbuffer as they may not match.

            SkipInit(out SwapChainDesc swapChainDesc);
            result = swapChain->GetDesc(ref swapChainDesc);

            if (result.IsFailure)
                result.Throw();

            if ((Format) format == swapChainDesc.BufferDesc.Format)
                format = PixelFormat.None;

            result = swapChain->ResizeBuffers((uint) bufferCount, (uint) width, (uint) height, (Format) format, (uint) GetSwapChainFlags());

            if (result.IsFailure)
                result.Throw();

            // Get the newly created native Texture
            //   This increments the reference count of the COM object,
            //   so we need to Release() it when discarding or swapping it.
            var backBufferTexture = GetBackBuffer<BackBufferResourceType>();
            bufferSwapIndex = 0;

            // Put it in our Back-Buffer Texture
            //   Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
            //   compensate with Release() to return the reference count to its previous value
            backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb);
            backBufferTexture.Release();

            foreach (var childTexture in childrenTextures)
            {
                childTexture.InitializeFrom(parentTexture: backBuffer, in childTexture.ViewDescription);
            }
        }

        /// <inheritdoc/>
        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescription = DepthStencilBuffer.Description with
            {
                Width = width,
                Height = height
            };

            // Manually update the Depth-Stencil Buffer
            DepthStencilBuffer.OnDestroyed(immediately: true);

            // Manually update all children Textures (Views)
            var childrenTextures = DestroyChildrenTextures(DepthStencilBuffer);

            // Put it in our Depth-Stencil Buffer
            DepthStencilBuffer.InitializeFrom(newTextureDescription);

            foreach (var childTexture in childrenTextures)
            {
                childTexture.InitializeFrom(parentTexture: DepthStencilBuffer, in childTexture.ViewDescription);
            }
        }

        /// <summary>
        ///   Creates or reinitializes the Swap-Chain with the current configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   <see cref="PresentationParameters.DeviceWindowHandle"/> is <see langword="null"/> or
        ///   the <see cref="WindowHandle.Handle"/> is invalid or zero.
        /// </exception>
        private void CreateSwapChain()
        {
            if (Description.DeviceWindowHandle is null)
                throw new InvalidOperationException("DeviceWindowHandle cannot be null");

#if STRIDE_PLATFORM_UWP
            CreateSwapChainForUWP();
#else
            CreateSwapChainForWindows();
#endif
        }

#if STRIDE_PLATFORM_UWP
        /// <summary>
        ///   Creates or reinitializes the Swap-Chain on the Universal Windows Platform (UWP).
        /// </summary>
        private void CreateSwapChainForUWP()
        {
            // Use two buffers to enable flip effect
            bufferCount = 2;

            // UWP does automatic sizing of the Swap-Chain based on the XAML element containing it.
            // We don't control it, we just can react to size changes.
            var description = new SwapChainDesc1
            {
                Width = (uint) Description.BackBufferWidth,
                Height = (uint) Description.BackBufferHeight,
                Format = (Format) Description.BackBufferFormat.ToNonSRgb(),
                Stereo = 0,
                SampleDesc = new SampleDesc(count: (uint) Description.MultisampleCount, quality: 0),
                BufferUsage = DXGI.UsageRenderTargetOutput,
                BufferCount = (uint) bufferCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential
            };

            var deviceAsIUnknown = GraphicsDevice.NativeDevice.AsIUnknown();
            var noOutput = NullComPtr<IDXGIOutput>();

            ComPtr<IDXGISwapChain1> swapChain = default;

            switch (Description.DeviceWindowHandle.Context)
            {
                case Games.AppContextType.UWPXaml:
                {
                    var hWindow = Description.DeviceWindowHandle.Handle;
                    var nativePanel = GetNativePanelFromHandle(Description.DeviceWindowHandle);

                    // If we are in UWP, we assume a minimum of IDXGIFactory2 support (DXGI 1.2, Windows 8+ / UWP)
                    var swapChainFactory = GraphicsAdapterFactory.NativeFactory.AsComPtrUnsafe<IDXGIFactory1, IDXGIFactory2>();

                    ref readonly var noFullscreenDesc = ref NullRef<SwapChainFullscreenDesc>();

                    // Creates the Swap-Chain for XAML composition
                    // TODO: Why not CreateSwapChainForCoreWindow / CreateSwapChainForComposition?
                    HResult result = swapChainFactory.CreateSwapChainForHwnd(deviceAsIUnknown, hWindow, in description, in noFullscreenDesc, noOutput, ref swapChain);

                    if (result.IsFailure)
                        result.Throw();

                    // Associate the SwapChainPanel with the Swap-Chain
                    nativePanel.SwapChain = swapChain;

                    break;

                    /// <summary>
                    ///   Gets an <see cref="ISwapChainPanelNative"/> from the handle of the window.
                    /// </summary>
                    static ISwapChainPanelNative GetNativePanelFromHandle(WindowHandle windowHandle)
                    {
                        var nativeWindow = windowHandle.NativeWindow;
                        var comPtr = Marshal.GetIUnknownForObject(nativeWindow);

                        HResult result = Marshal.QueryInterface(comPtr, in SilkMarshal.GuidOf<ISwapChainPanelNative>(), out var ptrPanel);

                        if (result.IsFailure || ptrPanel == IntPtr.Zero)
                            result.Throw();

                        return new ISwapChainPanelNative(ptrPanel);
                    }
                }
                case Games.AppContextType.UWPCoreWindow:
                {
                    HResult result = GraphicsDevice.NativeDevice.QueryInterface<IDXGIDevice2>(out var dxgiDevice2);

                    if (result.IsFailure)
                        result.Throw();

                    // Ensure that DXGI does not queue more than one frame at a time.
                    //   This both reduces latency and ensures that the application will only render after each VSync,
                    //   minimizing power consumption.
                    dxgiDevice2.SetMaximumFrameLatency(1);

                    // Next, get the parent factory from the DXGI Device
                    ComPtr<IDXGIAdapter> dxgiAdapter = default;
                    dxgiDevice2.GetAdapter(ref dxgiAdapter);

                    result = dxgiAdapter.GetParent(out ComPtr<IDXGIFactory2> dxgiFactory2);

                    if (result.IsFailure)
                        result.Throw();

                    // Finally, create the Swap-Chain
                    var coreWindow = ToComPtr((IUnknown*) Marshal.GetIUnknownForObject(Description.DeviceWindowHandle.NativeWindow));

                    result = dxgiFactory2.CreateSwapChainForCoreWindow(deviceAsIUnknown, coreWindow, in description, noOutput, ref swapChain);

                    if (result.IsFailure)
                        result.Throw();

                    SafeRelease(ref coreWindow);
                    SafeRelease(ref dxgiFactory2);
                    SafeRelease(ref dxgiAdapter);
                    SafeRelease(ref dxgiDevice2);
                    break;
                }
                default:
                    throw new NotSupportedException($"Window context [{Description.DeviceWindowHandle.Context}] not supported while creating SwapChain");
            }

            this.swapChain = swapChain;
            swapChainVersion = GetLatestDxgiSwapChainVersion(swapChain);
        }
#else
        /// <summary>
        ///   Creates or reinitializes the Swap-Chain on the desktop Windows platform.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   <see cref="PresentationParameters.DeviceWindowHandle"/> is <see langword="null"/> or
        ///   the <see cref="WindowHandle.Handle"/> is invalid or zero.
        /// </exception>
        private void CreateSwapChainForWindows()
        {
            var hwndPtr = Description.DeviceWindowHandle.Handle;
            if (hwndPtr == 0)
                throw new InvalidOperationException($"The {nameof(WindowHandle)}.{nameof(WindowHandle.Handle)} must not be zero.");

            CreateSwapChainForDesktop(hwndPtr);
        }

        private void CreateSwapChainForDesktop(IntPtr handle)
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
                // Mandatory for DXGI flip model and Direct3D 12
                swapchainFormat = ToSupportedFlipModelFormat(swapchainFormat);
                bufferCount = 2;
            }

            var modeDescription = new ModeDesc
            {
                Width = (uint) Description.BackBufferWidth,
                Height = (uint) Description.BackBufferHeight,
                RefreshRate = Description.RefreshRate.ToSilk(),
                Format = (Format) swapchainFormat,

                ScanlineOrdering = ModeScanlineOrder.Unspecified,  // TODO: Make this configurable?
                Scaling = ModeScaling.Unspecified  // TODO: Make this configurable?
            };

            var description = new SwapChainDesc1
            {
                BufferCount = (uint) bufferCount, // TODO: Do we really need this to be configurable by the user?
                BufferUsage = DXGI.UsageRenderTargetOutput,
                SwapEffect = useFlipModel ? SwapEffect.FlipDiscard : SwapEffect.Discard,

                Width = modeDescription.Width,
                Height = modeDescription.Height,
                Format = modeDescription.Format,

                SampleDesc = new SampleDesc(count: (uint) Description.MultisampleCount, quality: 0),
                Scaling = Scaling.Stretch,  // TODO: Make this configurable
                Stereo = 0,  // TODO: Make this configurable for VR
                AlphaMode = AlphaMode.Unspecified,  // TODO: Make this configurable
                Flags = (uint) GetSwapChainFlags()
            };
            var fullscreenDescription = new SwapChainFullscreenDesc
            {
                Windowed = !Description.IsFullScreen,
                RefreshRate = modeDescription.RefreshRate,
                Scaling = modeDescription.Scaling,
                ScanlineOrdering = modeDescription.ScanlineOrdering
            };

            ComPtr<IDXGIOutput> doNotRestrictOutput = default;

            // We assume at least IDXGISwapChain1 support (DXGI 1.2, Windows 7+ / UWP)
            Debug.Assert(GraphicsAdapterFactory.NativeFactoryVersion >= 2);
            var nativeFactory = GraphicsAdapterFactory.NativeFactory.AsComPtrUnsafe<IDXGIFactory1, IDXGIFactory2>();

#if STRIDE_GRAPHICS_API_DIRECT3D11
            ComPtr<IUnknown> device = GraphicsDevice.NativeDevice.AsIUnknown();
#elif STRIDE_GRAPHICS_API_DIRECT3D12
            ComPtr<IUnknown> device = GraphicsDevice.NativeCommandQueue.AsIUnknown();
#endif
            ComPtr<IDXGISwapChain1> newSwapChain = default;

            HResult result = nativeFactory.CreateSwapChainForHwnd(device, handle, in description, in fullscreenDescription, doNotRestrictOutput, ref newSwapChain);

            if (result.IsFailure)
                result.Throw();

            swapChain = newSwapChain;
            swapChainVersion = GetLatestDxgiSwapChainVersion(newSwapChain);

            // We need a IDXGISwapChain3 to enable output color space setting to support HDR outputs
            if (swapChainVersion >= 3)
            {
                var swapChain3 = newSwapChain.AsComPtrUnsafe<IDXGISwapChain1, IDXGISwapChain3>();
                swapChain3.SetColorSpace1((Silk.NET.DXGI.ColorSpaceType) Description.OutputColorSpace);
            }

            // Prevent switching between windowed and fullscreen modes by pressing Alt+ENTER
            nativeFactory.MakeWindowAssociation(handle, DxgiConstants.WindowAssociation_NoAltEnter);

            if (Description.IsFullScreen)
            {
                // Before fullscreen switch
                newSwapChain.ResizeTarget(in modeDescription);

                // Switch to fullscreen
                newSwapChain.SetFullscreenState(Fullscreen: 1, pTarget: ref NullRef<IDXGIOutput>());

                // It's really important to call ResizeBuffers AFTER switching to IsFullScreen
                newSwapChain.ResizeBuffers((uint) bufferCount,
                                           (uint) Description.BackBufferWidth,
                                           (uint) Description.BackBufferHeight,
                                           NewFormat: default,
                                           description.Flags);
            }
        }

        /// <summary>
        ///   Returns the appropriate flags for the Swap-Chain given the configuration and system capabilities.
        /// </summary>
        /// <returns>The most appropriate <see cref="SwapChainFlag"/>s.</returns>
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
        ///   Ensures the provided pixel format is supported for a flip model Swap-Chain, as
        ///   certain formats are not supported when using the flip model.
        /// </summary>
        /// <param name="pixelFormat">The pixel format to convert to a format supported for a flip model Swap-Chain.</param>
        /// <exception cref="ArgumentException">
        ///   The given <paramref name="pixelFormat"/> does not have a direct analog supported by
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

        /// <summary>
        ///   Queries the latest DXGI Swap-Chain version supported.
        /// </summary>
        private static uint GetLatestDxgiSwapChainVersion(IDXGISwapChain1* dxgiSwapChain)
        {
            HResult result;
            uint dxgiSwapChainVersion;

            if ((result = dxgiSwapChain->QueryInterface<IDXGISwapChain4>(out _)).IsSuccess)
            {
                dxgiSwapChainVersion = 4;
                dxgiSwapChain->Release();
            }
            else if ((result = dxgiSwapChain->QueryInterface<IDXGISwapChain3>(out _)).IsSuccess)
            {
                dxgiSwapChainVersion = 3;
                dxgiSwapChain->Release();
            }
            else if ((result = dxgiSwapChain->QueryInterface<IDXGISwapChain2>(out _)).IsSuccess)
            {
                dxgiSwapChainVersion = 2;
                dxgiSwapChain->Release();
            }
            else
            {
                dxgiSwapChainVersion = 1;
            }

            return dxgiSwapChainVersion;
        }
    }
}

#endif
