// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using System.Collections.Generic;
using System.Diagnostics;

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

            CreateDxgiFactory();

            staticCollector.Add(ComPtrHelpers.ToComPtr(dxgiFactory));  // To avoid circular references and stack overflow on Initialize()

            var adapterList = dxgiFactoryVersion >= 6
                ? EnumerateAdaptersPrefer(GpuPreference.HighPerformance)  // TODO: Make GPU preference configurable?
                : EnumerateAdapters();

            adapters = adapterList.ToArray();
            defaultAdapter = adapterList.Count > 0 ? adapterList[0] : null;


            //
            // Creates the DXGI factory.
            //
            static void CreateDxgiFactory()
            {
                var dxgi = DXGI.GetApi(window: null);

                HResult result;
                IDXGIFactory1* factory = null;
#if DEBUG
                const uint FactoryFlags = DxgiConstants.CreateFactoryDebug;
#else
                const uint FactoryFlags = 0;
#endif
                if (((HResult) dxgi.CreateDXGIFactory2<IDXGIFactory2>(FactoryFlags, out var dxgiFactory2)).IsSuccess)
                {
                    factory = (IDXGIFactory1*) dxgiFactory2.Handle;
                }
                else if ((result = dxgi.CreateDXGIFactory1<IDXGIFactory1>(out var dxgiFactory1)).IsSuccess)
                {
                    factory = dxgiFactory1.Handle;
                }
                else result.Throw(); // No valid DXGI factory found

                // Determine the latest DXGI factory version supported
                uint factoryVersion = GetLatestDxgiFactoryVersion(factory);

                dxgiFactory = factory;
                dxgiFactoryVersion = factoryVersion;
            }

            //
            // Queries the latest DXGI adapter version supported.
            //
            static uint GetLatestDxgiFactoryVersion(IDXGIFactory1* factory)
            {
                uint factoryVersion;

                if (((HResult) factory->QueryInterface<IDXGIFactory7>(out _)).IsSuccess)
                {
                    factoryVersion = 7;
                    factory->Release();
                }
                else if (((HResult) factory->QueryInterface<IDXGIFactory6>(out _)).IsSuccess)
                {
                    factoryVersion = 6;
                    factory->Release();
                }
                else if (((HResult) factory->QueryInterface<IDXGIFactory5>(out _)).IsSuccess)
                {
                    factoryVersion = 5;
                    factory->Release();
                }
                else if (((HResult) factory->QueryInterface<IDXGIFactory4>(out _)).IsSuccess)
                {
                    factoryVersion = 4;
                    factory->Release();
                }
                else if (((HResult) factory->QueryInterface<IDXGIFactory3>(out _)).IsSuccess)
                {
                    factoryVersion = 3;
                    factory->Release();
                }
                else if (((HResult) factory->QueryInterface<IDXGIFactory2>(out _)).IsSuccess)
                {
                    factoryVersion = 2;
                    factory->Release();
                }
                else
                {
                    // We are assuming a minimum of IDXGIFactory1 support (Windows 7+)
                    factoryVersion = 1;
                }

                return factoryVersion;
            }

            //
            // Enumerates all the Graphics Adapters in the system.
            //
            static List<GraphicsAdapter> EnumerateAdapters()
            {
                uint adapterIndex = 0;
                var adapterList = new List<GraphicsAdapter>();
                ComPtr<IDXGIAdapter1> dxgiAdapter = default;

                do
                {
                    HResult result = dxgiFactory->EnumAdapters1(adapterIndex, ref dxgiAdapter);

                    bool foundValidAdapter = result.IsSuccess && result.Code != DxgiConstants.ErrorNotFound;
                    if (!foundValidAdapter)
                        break;

                    var adapter = new GraphicsAdapter(dxgiAdapter, adapterIndex);
                    staticCollector.Add(adapter);
                    adapterList.Add(adapter);

                    adapterIndex++;

                } while (true);

                return adapterList;
            }

            //
            // Enumerates all the Graphics Adapters in the system, using GPU preference (DXGI 1.6+).
            //
            static List<GraphicsAdapter> EnumerateAdaptersPrefer(GpuPreference gpuPreference)
            {
                Debug.Assert(dxgiFactoryVersion >= 6);
                var dxgiFactory6 = (IDXGIFactory6*) dxgiFactory;

                uint adapterIndex = 0;
                var adapterList = new List<GraphicsAdapter>();

                do
                {
                    HResult result = dxgiFactory6->EnumAdapterByGpuPreference(adapterIndex, gpuPreference, out ComPtr<IDXGIAdapter1> dxgiAdapter);

                    bool foundValidAdapter = result.IsSuccess && result.Code != DxgiConstants.ErrorNotFound;
                    if (!foundValidAdapter)
                        break;

                    var adapter = new GraphicsAdapter(dxgiAdapter, adapterIndex);
                    staticCollector.Add(adapter);
                    adapterList.Add(adapter);

                    adapterIndex++;

                } while (true);

                return adapterList;
            }
        }
    }
}

#endif
