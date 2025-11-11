// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;

#if STRIDE_PLATFORM_UWP || DIRECTX11_1
using DxgiFactoryType = Silk.NET.DXGI.IDXGIFactory2;
#else
using DxgiFactoryType = Silk.NET.DXGI.IDXGIFactory1;
#endif

namespace Stride.Graphics
{
    public static unsafe partial class GraphicsAdapterFactory
    {
        private static DxgiFactoryType* dxgiFactory;

        /// <summary>
        ///   Gets the native DXGI factory object.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal static ComPtr<DxgiFactoryType> NativeFactory
        {
            get
            {
                lock (StaticLock)
                {
                    Initialize();
                    return ComPtrHelpers.ToComPtr(dxgiFactory);
                }
            }
        }


        /// <summary>
        ///   Initializes all the <see cref="GraphicsAdapter"/>s.
        /// </summary>
        internal static void InitializeInternal()
        {
            staticCollector.Dispose();

            var dxgi = DXGI.GetApi(window: null);

            HResult result = default;

#if STRIDE_PLATFORM_UWP || DIRECTX11_1

#if DEBUG
            uint factoryFlags = DxgiConstants.CreateFactoryDebug;
#else
            uint factoryFlags = 0;
#endif
            result = dxgi.CreateDXGIFactory2<DxgiFactoryType>(factoryFlags, out var factory);
#else
            result = dxgi.CreateDXGIFactory1<DxgiFactoryType>(out var factory);
#endif
            if (result.IsFailure)
                result.Throw();

            dxgiFactory = factory.Handle;
            staticCollector.Add(factory);

            uint adapterIndex = 0;
            var adapterList = new List<GraphicsAdapter>();
            bool foundValidAdapter;
            ComPtr<IDXGIAdapter1> dxgiAdapter = default;

            do
            {
                result = dxgiFactory->EnumAdapters1(adapterIndex, ref dxgiAdapter);

                foundValidAdapter = result.IsSuccess && result.Code != DxgiConstants.ErrorNotFound;
                if (!foundValidAdapter)
                    break;

                var adapter = new GraphicsAdapter(ComPtrHelpers.ToComPtr(dxgiFactory), dxgiAdapter, adapterIndex);
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
