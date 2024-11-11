// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

namespace Stride.Graphics
{
    public static unsafe partial class GraphicsAdapterFactory
    {
#if STRIDE_PLATFORM_UWP || DIRECTX11_1
        internal static ComPtr<IDXGIFactory2> NativeFactory;
#else
        internal static ComPtr<IDXGIFactory1> NativeFactory;
#endif

        /// <summary>
        /// Initializes all adapters with the specified factory.
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

            NativeFactory = dxgi.CreateDXGIFactory2<IDXGIFactory2>();
#else
            NativeFactory = dxgi.CreateDXGIFactory1<IDXGIFactory1>();
#endif

            staticCollector.Add(NativeFactory);

            const int DXGI_ERROR_NOT_FOUND = unchecked((int) 0x887A0002);

            uint adapterIndex = 0;
            var adapterList = new List<GraphicsAdapter>();
            bool foundValidAdapter;

            do
            {
                ComPtr<IDXGIAdapter1> dxgiAdapter = default;
                HResult result = NativeFactory.EnumAdapters1(adapterIndex, ref dxgiAdapter);

                foundValidAdapter = result.IsSuccess && result.Code != DXGI_ERROR_NOT_FOUND;
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

        /// <summary>
        ///   Gets the <see cref="IDXGIFactory"/> used by all <see cref="GraphicsAdapter"/>s.
        /// </summary>
#if STRIDE_PLATFORM_UWP || DIRECTX11_1
        internal static IDXGIFactory2* Factory
#else
        internal static IDXGIFactory1* Factory
#endif
        {
            get
            {
                lock (StaticLock)
                {
                    Initialize();
                    return NativeFactory;
                }
            }
        }
    }
}

#endif
