// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Stride.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
        /// <summary>
        ///   Initializes all the <see cref="GraphicsAdapter"/>s.
        /// </summary>
        internal static void InitializeInternal()
        {
            staticCollector.Dispose();

            var dxgi = DXGI.GetApi(window: null);

#if STRIDE_PLATFORM_UWP || DIRECTX11_1

#if DEBUG
            uint debugFlag = 1;
#else
            uint debugFlag = 0;
#endif

            using var dxgiFactory = dxgi.CreateDXGIFactory2<IDXGIFactory2>();
#else
            using var dxgiFactory = dxgi.CreateDXGIFactory1<IDXGIFactory1>();
#endif

            uint adapterIndex = 0;
            var adapterList = new List<GraphicsAdapter>();
            bool foundValidAdapter;
            ComPtr<IDXGIAdapter1> dxgiAdapter = default;

            do
            {
                HResult result = dxgiFactory.EnumAdapters1(adapterIndex, ref dxgiAdapter);

                foundValidAdapter = result.IsSuccess && result.Code != DxgiConstants.ErrorNotFound;
                if (!foundValidAdapter)
                    break;

                var adapter = new GraphicsAdapter(dxgiAdapter, adapterIndex);
                staticCollector.Add(adapter);
                adapterList.Add(adapter);

                adapterIndex++;

            } while (foundValidAdapter);

            defaultAdapter = adapterList.Count > 0 ? adapterList[0] : null;
            adapters = adapterList.ToArray();
        }
    }
}

#endif
