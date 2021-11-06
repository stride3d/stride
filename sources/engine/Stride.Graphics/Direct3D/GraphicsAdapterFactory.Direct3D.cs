// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D
using System;
using System.Collections.Generic;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Stride.Graphics.Direct3D;

namespace Stride.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
#if STRIDE_PLATFORM_UWP
        internal static Factory2 NativeFactory;
#else
        internal static ComPtr<IDXGIFactory> NativeFactory = new();
#endif

        /// <summary>
        /// Initializes all adapters with the specified factory.
        /// </summary>
        internal static void InitializeInternal()
        {
            staticCollector.Dispose();

#if DIRECTX11_1
            using (var factory = new Factory1())
            NativeFactory = factory.QueryInterface<Factory2>();
#elif STRIDE_PLATFORM_UWP
            // Maybe this will become default code for everybody if we switch to DX 11.1/11.2 SharpDX dll?
            NativeFactory = new Factory2();
#else
            unsafe
            {
                var riid = new Span<Guid>(new Guid[] { IDXGIFactory.Guid });
                var dxgi = DXGI.GetApi();
                ComPtr<IDXGIFactory> factory;
                var x = dxgi.CreateDXGIFactory(SilkMarshal.GuidPtrOf<IDXGIFactory>(), (void**)&factory.Handle);
                SilkMarshal.ThrowHResult(x);
                NativeFactory = factory;
            }

#endif

            staticCollector.Add(NativeFactory);
            int countAdapters = 0;
            unsafe
            {
                IDXGIAdapter a = new();
                IDXGIAdapter* pa = &a;
                while ((ulong)NativeFactory.Get().EnumAdapters((uint)countAdapters, &pa) == (ulong)ReturnCodes.S_OK)
                    countAdapters += 1;
            }
             
            var adapterList = new List<GraphicsAdapter>();
            for (int i = 0; i < countAdapters; i++)
            {
                var adapter = new GraphicsAdapter(NativeFactory, i);
                staticCollector.Add(adapter);
                adapterList.Add(adapter);
            }

            defaultAdapter = adapterList.Count > 0 ? adapterList[0] : null;
            adapters = adapterList.ToArray();
        }

        /// <summary>
        /// Gets the <see cref="Factory1"/> used by all GraphicsAdapter.
        /// </summary>
        internal static ComPtr<IDXGIFactory> Factory
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
