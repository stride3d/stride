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
using System.Reflection;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Stride.Core.Collections;
#if STRIDE_GRAPHICS_API_DIRECT3D11
using BackBufferResourceType = SharpDX.Direct3D11.Texture2D;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
using BackBufferResourceType = SharpDX.Direct3D12.Resource;
#endif

namespace Stride.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private readonly Texture backBuffer;

        private readonly bool flipModelSupport;

        private readonly bool tearingSupport;

        private SwapChain swapChain;

        private int bufferCount;

        private bool useFlipModel;

#if STRIDE_GRAPHICS_API_DIRECT3D12
        private int bufferSwapIndex;
#endif

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            PresentInterval = presentationParameters.PresentationInterval;

            flipModelSupport = CheckFlipModelSupport(device);
            tearingSupport = CheckTearingSupport(device);

            // Initialize the swap chain
            swapChain = CreateSwapChain();

            backBuffer = new Texture(device).InitializeFromImpl(swapChain.GetBackBuffer<BackBufferResourceType>(0), Description.BackBufferFormat.IsSRgb());

            // Reload should get backbuffer from swapchain as well
            //backBufferTexture.Reload = graphicsResource => ((Texture)graphicsResource).Recreate(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture>(0));

            static bool CheckFlipModelSupport(GraphicsDevice device)
            {
                try
                {
                    // From https://github.com/walbourn/directx-vs-templates/blob/main/d3d11game_win32_dr/DeviceResources.cpp#L138
                    using var dxgiDevice = device.NativeDevice.QueryInterface<SharpDX.DXGI.Device>();
                    using var dxgiAdapter = dxgiDevice.Adapter;
                    using var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory4>();
                    return dxgiFactory != null;
                }
                catch
                {
                    // The requested interfaces need at least Windows 8
                    return false;
                }
            }

            static unsafe bool CheckTearingSupport(GraphicsDevice device)
            {
                try
                {
                    // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
                    using var dxgiDevice = device.NativeDevice.QueryInterface<SharpDX.DXGI.Device>();
                    using var dxgiAdapter = dxgiDevice.Adapter;
                    using var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory5>();
                    if (dxgiFactory is null)
                        return false;

                    int allowTearing = 0;
                    dxgiFactory.CheckFeatureSupport(Feature.PresentAllowTearing, new IntPtr(&allowTearing), sizeof(int));
                    return allowTearing != 0;
                }
                catch
                {
                    // The requested interfaces need at least Windows 10
                    return false;
                }
            }
        }

        public override Texture BackBuffer => backBuffer;

        public override object NativePresenter => swapChain;

        public override bool IsFullScreen
        {
            get
            {
#if STRIDE_PLATFORM_UWP
                return false;
#else
                return swapChain.IsFullScreen;
#endif
            }

            set
            {
#if !STRIDE_PLATFORM_UWP
                if (swapChain == null)
                    return;

                var outputIndex = Description.PreferredFullScreenOutputIndex;

                // no outputs connected to the current graphics adapter
                var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length ? GraphicsDevice.Adapter.Outputs[outputIndex] : null;

                Output currentOutput = null;

                try
                {
                    RawBool isCurrentlyFullscreen;
                    swapChain.GetFullscreenState(out isCurrentlyFullscreen, out currentOutput);

                    // check if the current fullscreen monitor is the same as new one
                    // If not fullscreen, currentOutput will be null but output won't be, so don't compare them
                    if (isCurrentlyFullscreen == value && (isCurrentlyFullscreen == false || (output != null && currentOutput != null && currentOutput.NativePointer == output.NativeOutput.NativePointer)))
                        return;
                }
                finally
                {
                    currentOutput?.Dispose();
                }

                bool switchToFullScreen = value;
                // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
                var description = new ModeDescription(backBuffer.ViewWidth, backBuffer.ViewHeight, Description.RefreshRate.ToSharpDX(), (SharpDX.DXGI.Format)Description.BackBufferFormat);
                if (switchToFullScreen)
                {
                    OnDestroyed();

                    Description.IsFullScreen = true;

                    OnRecreated();
                }
                else
                {
                    Description.IsFullScreen = false;
                    swapChain.IsFullScreen = false;

                    // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                    Resize(backBuffer.ViewWidth, backBuffer.ViewHeight, backBuffer.ViewFormat);
                }

                // If going to window mode: 
                if (!switchToFullScreen)
                {
                    // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                    description.RefreshRate = new SharpDX.DXGI.Rational(0, 0);
                    swapChain.ResizeTarget(ref description);
                }
#endif
            }
        }

        public override void BeginDraw(CommandList commandList)
        {
        }

        public override void EndDraw(CommandList commandList, bool present)
        {
        }

        public override void Present()
        {
            try
            {
                var presentInterval = GraphicsDevice.Tags.Get(ForcedPresentInterval) ?? PresentInterval;

                // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
                // DXGI_PRESENT_ALLOW_TEARING can only be used with sync interval 0. It is recommended to always pass this
                // tearing flag when using sync interval 0 if CheckFeatureSupport reports that tearing is supported and the
                // app is in a windowed mode - including border-less fullscreen mode.
                var presentFlags = useFlipModel && tearingSupport && presentInterval == PresentInterval.Immediate && !Description.IsFullScreen
                    ? PresentFlags.AllowTearing 
                    : PresentFlags.None;

                swapChain.Present((int)presentInterval, presentFlags);
#if STRIDE_GRAPHICS_API_DIRECT3D12
                // Manually swap back buffer
                backBuffer.NativeResource.Dispose();
                backBuffer.InitializeFromImpl(swapChain.GetBackBuffer<BackBufferResourceType>((++bufferSwapIndex) % bufferCount), Description.BackBufferFormat.IsSRgb());
#endif
            }
            catch (SharpDXException sharpDxException)
            {
                var deviceStatus = GraphicsDevice.GraphicsDeviceStatus;
                throw new GraphicsException($"Unexpected error on Present (device status: {deviceStatus})", sharpDxException, deviceStatus);
            }
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
            if (Name != null && GraphicsDevice != null && GraphicsDevice.IsDebugMode && swapChain != null)
            {
                swapChain.DebugName = Name;
            }
        }

        protected internal override void OnDestroyed()
        {
            // Manually update back buffer texture
            backBuffer.OnDestroyed();
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Destroyed;

            swapChain.Dispose();
            swapChain = null;

            base.OnDestroyed();
        }

        public override void OnRecreated()
        {
            base.OnRecreated();

            // Recreate swap chain
            swapChain = CreateSwapChain();

            // Get newly created native texture
            var backBufferTexture = swapChain.GetBackBuffer<BackBufferResourceType>(0);

            // Put it in our back buffer texture
            // TODO: Update new size
            backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb());
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Active;
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            // Manually update back buffer texture
            backBuffer.OnDestroyed();

            // Manually update all children textures
            var fastList = DestroyChildrenTextures(backBuffer);

#if STRIDE_PLATFORM_UWP
            var swapChainPanel = Description.DeviceWindowHandle.NativeWindow as Windows.UI.Xaml.Controls.SwapChainPanel;
            if (swapChainPanel != null)
            {
                var swapChain2 = swapChain.QueryInterface<SwapChain2>();
                if (swapChain2 != null)
                {
                    swapChain2.MatrixTransform = new RawMatrix3x2 { M11 = 1f / swapChainPanel.CompositionScaleX, M22 = 1f / swapChainPanel.CompositionScaleY };
                    swapChain2.Dispose();
                }
            }
#endif

            // If format is same as before, using Unknown (None) will keep the current
            // We do that because on Win10/RT, actual format might be the non-srgb one and we don't want to switch to srgb one by mistake (or need #ifdef)
            if (format == backBuffer.Format)
                format = PixelFormat.None;

            swapChain.ResizeBuffers(bufferCount, width, height, (SharpDX.DXGI.Format)format, GetSwapChainFlags());

            // Get newly created native texture
            var backBufferTexture = swapChain.GetBackBuffer<BackBufferResourceType>(0);

            // Put it in our back buffer texture
            backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb());

            foreach (var texture in fastList)
            {
                texture.InitializeFrom(backBuffer, texture.ViewDescription);
            }
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescription = DepthStencilBuffer.Description;
            newTextureDescription.Width = width;
            newTextureDescription.Height = height;

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
        private FastList<Texture> DestroyChildrenTextures(Texture parentTexture)
        {
            var fastList = new FastList<Texture>();
            foreach (var resource in GraphicsDevice.Resources)
            {
                var texture = resource as Texture;
                if (texture != null && texture.ParentTexture == parentTexture)
                {
                    texture.OnDestroyed();
                    fastList.Add(texture);
                }
            }

            return fastList;
        }

        private SwapChain CreateSwapChain()
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
        private SwapChain CreateSwapChainForUWP()
        {
            bufferCount = 2;
            var description = new SwapChainDescription1
            {
                // Automatic sizing
                Width = Description.BackBufferWidth,
                Height = Description.BackBufferHeight,
                Format = (SharpDX.DXGI.Format)Description.BackBufferFormat.ToNonSRgb(),
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription((int)Description.MultisampleCount, 0),
                Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                // Use two buffers to enable flip effect.
                BufferCount = bufferCount,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
            };

            SwapChain swapChain = null;
            switch (Description.DeviceWindowHandle.Context)
            {
                case Games.AppContextType.UWPXaml:
                {
                    var nativePanel = ComObject.As<ISwapChainPanelNative>(Description.DeviceWindowHandle.NativeWindow);

                    // Creates the swap chain for XAML composition
                    swapChain = new SwapChain1(GraphicsAdapterFactory.NativeFactory, GraphicsDevice.NativeDevice, ref description);

                    // Associate the SwapChainPanel with the swap chain
                    nativePanel.SwapChain = swapChain;

                    break;
                }

                case Games.AppContextType.UWPCoreWindow:
                {
                    using (var dxgiDevice = GraphicsDevice.NativeDevice.QueryInterface<SharpDX.DXGI.Device2>())
                    {
                        // Ensure that DXGI does not queue more than one frame at a time. This both reduces
                        // latency and ensures that the application will only render after each VSync, minimizing
                        // power consumption.
                        dxgiDevice.MaximumFrameLatency = 1;

                        // Next, get the parent factory from the DXGI Device.
                        using (var dxgiAdapter = dxgiDevice.Adapter)
                        using (var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>())
                            // Finally, create the swap chain.
                        using (var coreWindow = new SharpDX.ComObject(Description.DeviceWindowHandle.NativeWindow))
                        {
                            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory
                                , GraphicsDevice.NativeDevice, coreWindow, ref description);
                        }
                    }

                    break;
                }
                default:
                    throw new NotSupportedException(string.Format("Window context [{0}] not supported while creating SwapChain", Description.DeviceWindowHandle.Context));
            }

            return swapChain;
        }
#else
        /// <summary>
        /// Create the SwapChain on Windows.
        /// </summary>
        /// <returns></returns>
        private SwapChain CreateSwapChainForWindows()
        {
            var hwndPtr = Description.DeviceWindowHandle.Handle;
            if (hwndPtr != IntPtr.Zero)
            {
                return CreateSwapChainForDesktop(hwndPtr);
            }
            throw new InvalidOperationException($"The {nameof(WindowHandle)}.{nameof(WindowHandle.Handle)} must not be zero.");
        }

        private SwapChain CreateSwapChainForDesktop(IntPtr handle)
        {
#if STRIDE_GRAPHICS_API_DIRECT3D12
            useFlipModel = true;
#else
            // https://devblogs.microsoft.com/directx/dxgi-flip-model/#what-do-i-have-to-do-to-use-flip-model
            useFlipModel = Description.MultisampleCount == MultisampleCount.None && flipModelSupport;
#endif

            var backbufferFormat = Description.BackBufferFormat;
            bufferCount = 1;

            if (useFlipModel)
            {
                backbufferFormat = backbufferFormat.ToNonSRgb();
                bufferCount = 2;
            }

            var description = new SwapChainDescription
            {
                ModeDescription = new ModeDescription(Description.BackBufferWidth, Description.BackBufferHeight, Description.RefreshRate.ToSharpDX(), (SharpDX.DXGI.Format)backbufferFormat), 
                BufferCount = bufferCount, // TODO: Do we really need this to be configurable by the user?
                OutputHandle = handle,
                SampleDescription = new SampleDescription((int)Description.MultisampleCount, 0),
                SwapEffect = useFlipModel ? SwapEffect.FlipDiscard : SwapEffect.Discard,
                Usage = Usage.BackBuffer | SharpDX.DXGI.Usage.RenderTargetOutput,
                IsWindowed = true,
                Flags = GetSwapChainFlags(), 
            };

#if STRIDE_GRAPHICS_API_DIRECT3D11
            var newSwapChain = new SwapChain(GraphicsAdapterFactory.NativeFactory, GraphicsDevice.NativeDevice, description);
#elif STRIDE_GRAPHICS_API_DIRECT3D12
            var newSwapChain = new SwapChain(GraphicsAdapterFactory.NativeFactory, GraphicsDevice.NativeCommandQueue, description);
#endif

            //prevent normal alt-tab
            GraphicsAdapterFactory.NativeFactory.MakeWindowAssociation(handle, WindowAssociationFlags.IgnoreAltEnter);

            if (Description.IsFullScreen)
            {
                // Before fullscreen switch
                newSwapChain.ResizeTarget(ref description.ModeDescription);

                // Switch to full screen
                newSwapChain.IsFullScreen = true;

                // This is really important to call ResizeBuffers AFTER switching to IsFullScreen 
                newSwapChain.ResizeBuffers(bufferCount, Description.BackBufferWidth, Description.BackBufferHeight, newFormat: default, description.Flags);
            }

            return newSwapChain;
        }

        private SwapChainFlags GetSwapChainFlags()
        {
            var flags = SwapChainFlags.None;
            if (Description.IsFullScreen)
                flags |= SwapChainFlags.AllowModeSwitch;

            // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
            // It is recommended to always use the tearing flag when it is supported.
            if (useFlipModel && tearingSupport)
                flags |= SwapChainFlags.AllowTearing;

            return flags;
        }
#endif
    }
}
#endif
