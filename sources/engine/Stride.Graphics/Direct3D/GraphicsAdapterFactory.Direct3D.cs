// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D
using System.Collections.Generic;
using Silk.NET.DXGI;
using Stride.Graphics.Direct3D;

namespace Stride.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
#if STRIDE_PLATFORM_UWP || DIRECTX11_1
        internal static Factory2 NativeFactory;
#else
        internal static IDXGIFactory1 NativeFactory;
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
            NativeFactory = new IDXGIFactory1();
#endif

            staticCollector.Add(NativeFactory);
            int countAdapters = 0;
            unsafe
            {
                var a = new IDXGIAdapter1();
                var pA = &a;
                do { countAdapters += 1; }
                while ((ulong)NativeFactory.EnumAdapters1((uint)countAdapters, &pA) == (ulong)ReturnCodes.S_OK);
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
        internal static IDXGIFactory1 Factory
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
