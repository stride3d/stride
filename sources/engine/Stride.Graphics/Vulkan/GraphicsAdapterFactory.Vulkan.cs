// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System.Collections.Generic;
using Silk.NET.Vulkan;
using Stride.Core;
using static Silk.NET.Vulkan.Vk;
using System;


namespace Stride.Graphics
{
    public static partial class GraphicsAdapterFactory
    {
        private static GraphicsAdapterFactoryInstance defaultInstance;
        private static GraphicsAdapterFactoryInstance debugInstance;
        internal static Vk vk = GetApi();

        /// <summary>
        /// Initializes all adapters with the specified factory.
        /// </summary>
        internal static unsafe void InitializeInternal()
        {
            // Create the default instance to enumerate physical devices
            defaultInstance = new GraphicsAdapterFactoryInstance(false);

            uint deviceCount = 0;
            vk.EnumeratePhysicalDevices(defaultInstance.NativeInstance, ref deviceCount, null);

            Span<PhysicalDevice> nativePhysicalDevices = stackalloc PhysicalDevice[(int)deviceCount];
            vk.EnumeratePhysicalDevices(defaultInstance.NativeInstance, &deviceCount, nativePhysicalDevices);

            var adapterList = new List<GraphicsAdapter>();
            for (int i = 0; i < nativePhysicalDevices.Length; i++)
            {
                var adapter = new GraphicsAdapter(nativePhysicalDevices[i], i);
                staticCollector.Add(adapter);
                adapterList.Add(adapter);
            }

            defaultAdapter = adapterList.Count > 0 ? adapterList[0] : null;
            adapters = adapterList.ToArray();

            staticCollector.Add(new AnonymousDisposable(Cleanup));
        }

        private static void Cleanup()
        {
            if (defaultInstance != null)
            {
                defaultInstance.Dispose();
                defaultInstance = null;
            }

            if (debugInstance != null)
            {
                debugInstance.Dispose();
                debugInstance = null;
            }
        }

        /// <summary>
        /// Gets the <see cref="GraphicsAdapterFactoryInstance"/> used by all GraphicsAdapter.
        /// </summary>
        internal static GraphicsAdapterFactoryInstance GetInstance(bool enableValidation)
        {
            lock (StaticLock)
            {
                Initialize();

                if (enableValidation)
                {
                    return debugInstance ??= new GraphicsAdapterFactoryInstance(true);
                }
                else
                {
                    return defaultInstance;
                }
            }
        }
    }
}
#endif 
