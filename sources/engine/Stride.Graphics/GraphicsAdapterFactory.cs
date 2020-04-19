// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Factory for <see cref="GraphicsAdapter"/>.
    /// </summary>
    public static partial class GraphicsAdapterFactory
    {
        private static readonly object StaticLock = new object();
        private static ObjectCollector staticCollector = new ObjectCollector();
        private static bool isInitialized = false;
        private static GraphicsAdapter[] adapters;
        private static GraphicsAdapter defaultAdapter;

        /// <summary>
        /// Initializes the GraphicsAdapter. On Desktop and WinRT, this is done statically.
        /// </summary>
        public static void Initialize()
        {
            lock (StaticLock)
            {
                if (!isInitialized)
                {
                    InitializeInternal();
                    isInitialized = true;
                }
            }
        }

        /// <summary>
        /// Perform a <see cref="Dispose"/> and <see cref="Initialize"/> to re-initialize all adapters informations.
        /// </summary>
        public static void Reset()
        {
            lock (StaticLock)
            {
                Dispose();
                Initialize();
            }
        }

        /// <summary>
        /// Dispose all statically cached value by this instance.
        /// </summary>
        public static void Dispose()
        {
            lock (StaticLock)
            {
                staticCollector.Dispose();
                adapters = null;
                defaultAdapter = null;
                isInitialized = false;
            }
        }

        /// <summary>
        /// Collection of available adapters on the system.
        /// </summary>
        public static GraphicsAdapter[] Adapters
        {
            get
            {
                lock (StaticLock)
                {
                    Initialize();
                    return adapters;
                }
            }
        }

        /// <summary>
        /// Gets the default adapter. This property can be <c>null</c>.
        /// </summary>
        public static GraphicsAdapter Default
        {
            get
            {
                lock (StaticLock)
                {
                    Initialize();
                    return defaultAdapter;
                }
            }
        }
    }
}
