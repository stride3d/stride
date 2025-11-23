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
        // We are assuming a minimum of IDXGIFactory1 support (Windows 7+)
        private static IDXGIFactory1* dxgiFactory;
        private static uint dxgiFactoryVersion;

        /// <summary>
        ///   Gets the native DXGI factory object.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal static ComPtr<IDXGIFactory1> NativeFactory
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
        ///   Gets the version number of the native DXGI factory supported.
        /// </summary>
        /// <value>
        ///   This indicates the latest DXGI factory interface version supported by the system.
        ///   For example, if the value is 4, then this adapter supports up to <see cref="IDXGIFactory4"/>.
        /// </value>
        internal static uint NativeFactoryVersion => dxgiFactoryVersion;


        /// <summary>
        ///   Initializes all the <see cref="GraphicsAdapter"/>s.
        /// </summary>
        internal static void InitializeInternal()
        {
            staticCollector.Dispose();

            var dxgi = DXGI.GetApi(window: null);

            CreateDxgiFactory();

            staticCollector.Add(ComPtrHelpers.ToComPtr(dxgiFactory));  // To avoid circular references and stack overflow on Initialize()

            uint adapterIndex = 0;
            var adapterList = new List<GraphicsAdapter>();
            bool foundValidAdapter;
            ComPtr<IDXGIAdapter1> dxgiAdapter = default;

            do
            {
                HResult result = dxgiFactory->EnumAdapters1(adapterIndex, ref dxgiAdapter);

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

            //
            // Creates the DXGI factory.
            //
            static void CreateDxgiFactory()
            {
                var dxgi = DXGI.GetApi(window: null);

                HResult result = default;
                IDXGIFactory1* dxgiFactory = null;
                uint dxgiFactoryVersion = 0;
#if DEBUG
                uint factoryFlags = DxgiConstants.CreateFactoryDebug;
#else
                uint factoryFlags = 0;
#endif
                if ((result = dxgi.CreateDXGIFactory2<IDXGIFactory2>(factoryFlags, out var dxgiFactory2)).IsSuccess)
                {
                    dxgiFactory = (IDXGIFactory1*) dxgiFactory2.Handle;
                    dxgiFactoryVersion = 2;
                }
                else if ((result = dxgi.CreateDXGIFactory1<IDXGIFactory1>(out var dxgiFactory1)).IsSuccess)
                {
                    dxgiFactory = (IDXGIFactory1*) dxgiFactory1.Handle;
                    dxgiFactoryVersion = 1;
                }
                else result.Throw(); // No valid DXGI factory found

                // Determine the latest DXGI factory version supported
                dxgiFactoryVersion = GetLatestDxgiFactoryVersion(dxgiFactory);

                GraphicsAdapterFactory.dxgiFactory = (IDXGIFactory1*) dxgiFactory;
                GraphicsAdapterFactory.dxgiFactoryVersion = dxgiFactoryVersion;
            }

            //
            // Queries the latest DXGI adapter version supported.
            //
            static uint GetLatestDxgiFactoryVersion(IDXGIFactory1* dxgiFactory)
            {
                HResult result;
                uint dxgiFactoryVersion;

                if ((result = dxgiFactory->QueryInterface<IDXGIFactory7>(out _)).IsSuccess)
                {
                    dxgiFactoryVersion = 7;
                    dxgiFactory->Release();
                }
                else if ((result = dxgiFactory->QueryInterface<IDXGIFactory6>(out _)).IsSuccess)
                {
                    dxgiFactoryVersion = 6;
                    dxgiFactory->Release();
                }
                else if ((result = dxgiFactory->QueryInterface<IDXGIFactory5>(out _)).IsSuccess)
                {
                    dxgiFactoryVersion = 5;
                    dxgiFactory->Release();
                }
                else if ((result = dxgiFactory->QueryInterface<IDXGIFactory4>(out _)).IsSuccess)
                {
                    dxgiFactoryVersion = 4;
                    dxgiFactory->Release();
                }
                else if ((result = dxgiFactory->QueryInterface<IDXGIFactory3>(out _)).IsSuccess)
                {
                    dxgiFactoryVersion = 3;
                    dxgiFactory->Release();
                }
                else if ((result = dxgiFactory->QueryInterface<IDXGIFactory2>(out _)).IsSuccess)
                {
                    dxgiFactoryVersion = 2;
                    dxgiFactory->Release();
                }
                else
                {
                    // We are assuming a minimum of IDXGIFactory1 support (Windows 7+)
                    dxgiFactoryVersion = 1;
                }

                return dxgiFactoryVersion;
            }
        }
    }
}

#endif
