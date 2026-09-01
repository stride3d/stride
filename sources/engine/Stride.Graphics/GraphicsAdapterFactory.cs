// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   Static factory for obtaining the available <see cref="GraphicsAdapter"/>s in the system.
    /// </summary>
    public static partial class GraphicsAdapterFactory
    {
        private static readonly object StaticLock = new();

        private static ObjectCollector staticCollector;

        private static bool isInitialized;
        private static GraphicsAdapter[] adapters;
        private static GraphicsAdapter defaultAdapter;
        private static GpuPreference gpuPreference = GpuPreference.HighPerformance;

        /// <summary>
        ///   Gets or sets which GPU the enumeration puts first on machines that have several -
        ///   and with it, which one <see cref="DefaultAdapter"/> names and the engine picks when
        ///   nothing else (such as <c>GameGraphicsParameters.RequiredAdapterUid</c>) chooses one.
        /// </summary>
        /// <remarks>
        ///   Set it before the first use of <see cref="Adapters"/> or <see cref="DefaultAdapter"/>
        ///   (in practice: before creating the game); to change it afterwards, call
        ///   <see cref="Reset"/> - an already created device stays on its adapter either way.
        ///   The <c>STRIDE_GPU_PREFERENCE</c> environment variable
        ///   (<c>high-performance</c>, <c>minimum-power</c> or <c>unspecified</c>) overrides this
        ///   property, so a machine can steer a game that never sets it.
        /// </remarks>
        public static GpuPreference GpuPreference
        {
            get
            {
                lock (StaticLock)
                    return gpuPreference;
            }
            set
            {
                lock (StaticLock)
                    gpuPreference = value;
            }
        }

        /// <summary>
        ///   Initializes the <see cref="GraphicsAdapterFactory"/>. On Desktop and WinRT, this is done statically.
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
        ///   <see cref="Dispose"/>s and <see cref="Initialize"/>s the <see cref="GraphicsAdapterFactory"/>
        ///   to re-initialize all adapters informations.
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
        ///   Dispose all statically cached adapter information in the <see cref="GraphicsAdapterFactory"/>.
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
        ///   Gets a collection of the available <see cref="GraphicsAdapter"/>s on the system.
        /// </summary>
        public static ReadOnlySpan<GraphicsAdapter> Adapters
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
        ///   Gets the default <see cref="GraphicsAdapter"/>.
        /// </summary>
        /// <value>
        ///   The default <see cref="GraphicsAdapter"/>. This property can be <see langword="null"/>.
        /// </value>
        public static GraphicsAdapter DefaultAdapter
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
