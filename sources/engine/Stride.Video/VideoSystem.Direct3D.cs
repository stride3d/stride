// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D11

using SharpDX.Direct3D;
using SharpDX.MediaFoundation;
using Xenko.Games;

namespace Xenko.Video
{
    public partial class VideoSystem
    {
        public DXGIDeviceManager DxgiDeviceManager;

        public override void Initialize()
        {
            base.Initialize();

            var graphicsDevice = Services.GetService<IGame>().GraphicsDevice;

            DxgiDeviceManager = new DXGIDeviceManager();
            DxgiDeviceManager.ResetDevice(graphicsDevice.NativeDevice);

            //Add multi thread protection on device
            var mt = graphicsDevice.NativeDevice.QueryInterface<DeviceMultithread>();
            mt.SetMultithreadProtected(true);

            MediaManager.Startup();
        }
    }
}

#endif
