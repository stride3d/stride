// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_PLATFORM_UWP

using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Holographic;
using Windows.UI.Core;

namespace Stride.Graphics
{
    public class WindowsMixedRealityGraphicsPresenter : GraphicsPresenter
    {
        private static readonly Guid ID3D11Resource = new Guid("DC8E63F3-D12B-4952-B47B-5E45026A862D");

        private readonly HolographicSpace holographicSpace;
        private Texture backBuffer;

        public WindowsMixedRealityGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            holographicSpace = HolographicSpace.CreateForCoreWindow(CoreWindow.GetForCurrentThread());
            CoreWindow.GetForCurrentThread().Activate();

            Device3 d3DDevice = device.NativeDevice.QueryInterface<Device3>();
            IDirect3DDevice d3DInteropDevice = null;

            // Acquire the DXGI interface for the Direct3D device.
            using (var dxgiDevice = d3DDevice.QueryInterface<SharpDX.DXGI.Device3>())
            {
                // Wrap the native device using a WinRT interop object.
                uint hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);

                if (hr == 0)
                {
                    d3DInteropDevice = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DDevice;
                    Marshal.Release(pUnknown);
                }
            }

            holographicSpace.SetDirect3D11Device(d3DInteropDevice);

            BeginDraw(null);
            ResizeDepthStencilBuffer(backBuffer.Width, backBuffer.Height, 0);

            // Set a dummy back buffer as we use a seperate one for each eye.
            BackBuffer = Texture.New(GraphicsDevice, backBuffer.Description, null);
        }

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IDirect3DDxgiInterfaceAccess : IDisposable
        {
            IntPtr GetInterface([In] ref Guid iid);
        }

        public override Texture BackBuffer { get; }

        public override object NativePresenter => holographicSpace;

        public override bool IsFullScreen { get; set; }

        internal HolographicFrame HolographicFrame { get; set; }

        internal static IDirect3DSurface CreateDirect3DSurface(IntPtr dxgiSurface)
        {
            uint hr = CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface, out IntPtr inspectableSurface);

            IDirect3DSurface d3DSurface = null;

            if (hr == 0)
            {
                d3DSurface = Marshal.GetObjectForIUnknown(inspectableSurface) as IDirect3DSurface;
                Marshal.Release(inspectableSurface);
            }

            return d3DSurface;
        }

        public override void BeginDraw(CommandList commandList)
        {
            HolographicFrame = holographicSpace.CreateNextFrame();
            UpdateBackBuffer();
        }

        public override void Present()
        {
            HolographicFrame.PresentUsingCurrentPrediction();
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            UpdateBackBuffer();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            TextureDescription newTextureDescription = DepthStencilBuffer.Description;
            newTextureDescription.Width = backBuffer.Width;
            newTextureDescription.Height = backBuffer.Height;

            // Manually update the texture.
            DepthStencilBuffer.OnDestroyed();

            // Put it in our back buffer texture.
            DepthStencilBuffer.InitializeFrom(newTextureDescription);
        }

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
            SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface",
            SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr direct3DSurface);

        private void UpdateBackBuffer()
        {
            IDirect3DSurface surface = HolographicFrame.GetRenderingParameters(HolographicFrame.CurrentPrediction.CameraPoses[0]).Direct3D11BackBuffer;
            IDirect3DDxgiInterfaceAccess surfaceDxgiInterfaceAccess = surface as IDirect3DDxgiInterfaceAccess;
            IntPtr resource = surfaceDxgiInterfaceAccess.GetInterface(ID3D11Resource);

            if (backBuffer == null || backBuffer.NativeResource.NativePointer != resource)
            {
                // Clean up references to previous resources.
                backBuffer?.Dispose();
                LeftEyeBuffer?.Dispose();
                RightEyeBuffer?.Dispose();

                // This can change every frame as the system moves to the next buffer in the
                // swap chain. This mode of operation will occur when certain rendering modes
                // are activated.
                Texture2D d3DBackBuffer = new Texture2D(resource);

                backBuffer = new Texture(GraphicsDevice).InitializeFromImpl(d3DBackBuffer, false);

                LeftEyeBuffer = backBuffer.ToTextureView(new TextureViewDescription() { ArraySlice = 0, Type = ViewType.Single });
                RightEyeBuffer = backBuffer.ToTextureView(new TextureViewDescription() { ArraySlice = 1, Type = ViewType.Single });
            }

            Description.BackBufferFormat = backBuffer.Format;
            Description.BackBufferWidth = backBuffer.Width;
            Description.BackBufferHeight = backBuffer.Height;
        }
    }
}

#endif
