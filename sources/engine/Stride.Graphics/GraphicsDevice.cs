// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   A virtual adapter that can be used for creating GPU resources (buffers, textures, states, shaders, etc),
    ///   and to manipulate <see cref="CommandList"/>s.
    /// </summary>
    public partial class GraphicsDevice : ComponentBase
    {
        internal readonly Dictionary<PipelineStateDescriptionWithHash, PipelineState> CachedPipelineStates = [];
        internal readonly Dictionary<SamplerStateDescription, SamplerState> CachedSamplerStates = [];

        /// <summary>
        ///   Gets the features supported by this graphics device.
        /// </summary>
        public GraphicsDeviceFeatures Features;

        internal HashSet<GraphicsResourceBase> Resources = [];

        internal readonly bool NeedWorkAroundForUpdateSubResource;
        internal Effect CurrentEffect;

        private readonly List<IDisposable> sharedDataToDispose = [];
        private readonly Dictionary<object, IDisposable> sharedDataPerDevice;

        private GraphicsPresenter presenter;

        internal PipelineState DefaultPipelineState;

        internal CommandList InternalMainCommandList;

        internal PrimitiveQuad PrimitiveQuad;
        private ColorSpace colorSpace;

        public uint FrameTriangleCount;
        public uint FrameDrawCalls;
        private long bufferMemory;
        private long textureMemory;

        /// <summary>
        ///   Gets the amount of GPU memory currently allocated to buffers, in bytes.
        /// </summary>
        public long BuffersMemory => Interlocked.Read(ref bufferMemory);

        /// <summary>
        ///   Gets the amount of GPU memory currently allocated to textures, in bytes.
        /// </summary>
        public long TextureMemory => Interlocked.Read(ref textureMemory);

        /// <summary>
        ///   Gets the platform that graphics device is using.
        /// </summary>
        public static GraphicsPlatform Platform => GraphicPlatform;

        public string RendererName => GetRendererName();

        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="adapter">The graphics adapter.</param>
        /// <param name="graphicsProfiles">The graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        protected GraphicsDevice(GraphicsAdapter adapter, GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, WindowHandle windowHandle)
        {
            // Create shared data
            sharedDataPerDevice = [];

            Recreate(adapter, graphicsProfiles, deviceCreationFlags, windowHandle);

            // Helpers
            PrimitiveQuad = new PrimitiveQuad(this);
        }

        /// <summary>
        ///   Tries to create or reinitialize the graphics device.
        /// </summary>
        /// <param name="adapter">The graphics adapter.</param>
        /// <param name="graphicsProfiles">The graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>, or <paramref name="graphicsProfiles"/> is <see langword="null"/>.
        /// </exception>
        public void Recreate(GraphicsAdapter adapter, GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, WindowHandle windowHandle)
        {
            ArgumentNullException.ThrowIfNull(adapter);
            ArgumentNullException.ThrowIfNull(graphicsProfiles);

            Adapter = adapter;
            IsDebugMode = deviceCreationFlags.HasFlag(DeviceCreationFlags.Debug);

            // Default fallback
            if (graphicsProfiles.Length == 0)
                graphicsProfiles = [ GraphicsProfile.Level_11_0, GraphicsProfile.Level_10_1, GraphicsProfile.Level_10_0, GraphicsProfile.Level_9_3, GraphicsProfile.Level_9_2, GraphicsProfile.Level_9_1 ];

            // Initialize this instance
            InitializePlatformDevice(graphicsProfiles, deviceCreationFlags, windowHandle);

            // Checks the features supported by the new graphics device
            Features = new GraphicsDeviceFeatures(this);

            // Initialize the internal states of the new graphics device
            SamplerStates = new SamplerStateFactory(this);

            var defaultPipelineStateDescription = new PipelineStateDescription();
            defaultPipelineStateDescription.SetDefaults();
            AdjustDefaultPipelineStateDescription(ref defaultPipelineStateDescription);
            DefaultPipelineState = PipelineState.New(this, ref defaultPipelineStateDescription);

            InitializePostFeatures();
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            // Clear shared data
            for (int index = sharedDataToDispose.Count - 1; index >= 0; index--)
                sharedDataToDispose[index].Dispose();
            sharedDataPerDevice.Clear();

            SamplerStates.Dispose();
            SamplerStates = null;

            DefaultPipelineState.Dispose();

            PrimitiveQuad.Dispose();

            // Notify listeners
            Disposing?.Invoke(this, EventArgs.Empty);

            // Destroy resources
            lock (Resources)
            {
                foreach (var resource in Resources)
                {
                    // Destroy leftover resources (NOTE: Thus should not happen if ResumeManager.OnDestroyed has properly been called)
                    if (resource.LifetimeState != GraphicsResourceLifetimeState.Destroyed)
                    {
                        resource.OnDestroyed();
                        resource.LifetimeState = GraphicsResourceLifetimeState.Destroyed;
                    }

                    // Remove Reload code in case it was preventing objects from being GC
                    resource.Reload = null;
                }
                Resources.Clear();
            }

            DestroyPlatformDevice();

            base.Destroy();
        }

        /// <summary>
        ///   Occurs while this component is disposing but before it is disposed.
        /// </summary>
        public event EventHandler<EventArgs> Disposing;

        /// <summary>
        ///   A delegate called to create shareable data.
        /// </summary>
        /// <typeparam name="T">Type of the data to create.</typeparam>
        /// <returns>A new instance of the data to share.</returns>
        /// <remarks>
        ///   Because this method is being called from a lock region, this method should not be time consuming.
        /// </remarks>
        public delegate T CreateSharedData<out T>(GraphicsDevice device) where T : class, IDisposable;

        /// <summary>
        ///   Gets the adapter this instance is attached to.
        /// </summary>
        public GraphicsAdapter Adapter { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether this graphics device is in "Debug mode".
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this graphics device is initialized in "Debug mode"; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsDebugMode { get; private set; }

        /// <summary>
        ///   Gets a value indicating wether this device allows for concurrent building and deferred submission
        ///   of <see cref="CommandList"/>s.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this graphics device allows deferred execution; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsDeferred { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether this instance supports GPU markers and profiling.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if this graphics device allows profiling and creating GPU markers; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsProfilingSupported { get; private set; }

        /// <summary>
        ///   Gets the default color space of this graphics device.
        /// </summary>
        /// <value>The default color space.</value>
        public ColorSpace ColorSpace
        {
            get => Features.HasSRgb ? colorSpace : ColorSpace.Gamma;
            set => colorSpace = value;
        }

        /// <summary>
        ///   Gets or sets the current presenter used to display frames with this graphics device.
        /// </summary>
        /// <value>The current graphics presenter.</value>
        public virtual GraphicsPresenter Presenter
        {
            get => presenter;
            set => presenter = value;
        }

        /// <summary>
        ///   Gets the <see cref="SamplerStateFactory"/> factory.
        /// </summary>
        /// <value>
        ///   The <see cref="SamplerStateFactory"/> factory.
        /// </value>
        public SamplerStateFactory SamplerStates { get; private set; }

        /// <summary>
        /// Gets the index of the thread.
        /// </summary>
        /// <value>The index of the thread.</value>
        public int ThreadIndex { get; internal set; }

        /// <summary>
        ///   Gets the graphics profile this graphics device is using, which determines the available features.
        /// </summary>
        /// <value>The graphics profile.</value>
        internal GraphicsProfile? ShaderProfile { get; set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="creationFlags">A combination of <see cref="DeviceCreationFlags"/> flags that determines how the device will be created.</param>
        /// <param name="graphicsProfiles">The graphics profiles to try, in order of preference.</param>
        /// <returns>The new instance of <see cref="GraphicsDevice"/>.</returns>
        public static GraphicsDevice New(DeviceCreationFlags creationFlags = DeviceCreationFlags.None, params GraphicsProfile[] graphicsProfiles)
        {
            return New(GraphicsAdapterFactory.Default, creationFlags, graphicsProfiles);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="adapter">The graphics adapter the new device will use, or <see langword="null"/> to use the system's default adapter.</param>
        /// <param name="creationFlags">A combination of <see cref="DeviceCreationFlags"/> flags that determines how the device will be created.</param>
        /// <param name="graphicsProfiles">The graphics profiles to try, in order of preference.</param>
        /// <returns>The new instance of <see cref="GraphicsDevice"/>.</returns>
        public static GraphicsDevice New(GraphicsAdapter adapter, DeviceCreationFlags creationFlags = DeviceCreationFlags.None, params GraphicsProfile[] graphicsProfiles)
        {
            return new GraphicsDevice(adapter ?? GraphicsAdapterFactory.Default, graphicsProfiles, creationFlags, windowHandle: null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="adapter">The graphics adapter the new device will use, or <see langword="null"/> to use the system's default adapter.</param>
        /// <param name="creationFlags">A combination of <see cref="DeviceCreationFlags"/> flags that determines how the device will be created.</param>
        /// <param name="windowHandle">
        ///   The <see cref="WindowHandle"/> specifying the window the graphics device will present to,
        ///   or <see langword="null"/> if the device should not depend on a window.
        /// </param>
        /// <param name="graphicsProfiles">The graphics profiles to try, in order of preference.</param>
        /// <returns>The new instance of <see cref="GraphicsDevice"/>.</returns>
        public static GraphicsDevice New(GraphicsAdapter adapter, DeviceCreationFlags creationFlags = DeviceCreationFlags.None, WindowHandle windowHandle = null, params GraphicsProfile[] graphicsProfiles)
        {
            return new GraphicsDevice(adapter ?? GraphicsAdapterFactory.Default, graphicsProfiles, creationFlags, windowHandle);
        }

        /// <summary>
        ///   Gets a shared data object for this device context with a delegate to create the shared data if it is not present.
        /// </summary>
        /// <typeparam name="T">Type of the shared data to get or create.</typeparam>
        /// <param name="key">The key that identifies the shared data.</param>
        /// <param name="sharedDataCreator">A delegate that will be called to create the shared data.</param>
        /// <returns>
        ///   An instance of the shared data. It will be disposed by this <see cref="GraphicsDevice"/> instance.
        /// </returns>
        public T GetOrCreateSharedData<T>(object key, CreateSharedData<T> sharedDataCreator) where T : class, IDisposable
        {
            lock (sharedDataPerDevice)
            {
                if (!sharedDataPerDevice.TryGetValue(key, out IDisposable localValue))
                {
                    localValue = sharedDataCreator(this);
                    if (localValue is null)
                    {
                        return null;
                    }

                    sharedDataToDispose.Add(localValue);
                    sharedDataPerDevice.Add(key, localValue);
                }
                return (T) localValue;
            }
        }

        /// <summary>
        ///   Adds or subtracts to the texture memory amount this graphics device has allocated.
        /// </summary>
        /// <param name="memoryChange">The texture memory delta: positive to increase, negative to decrease.</param>
        internal void RegisterTextureMemoryUsage(long memoryChange)
        {
            Interlocked.Add(ref textureMemory, memoryChange);
        }

        /// <summary>
        ///   Adds or subtracts to the buffer memory amount this graphics device has allocated.
        /// </summary>
        /// <param name="memoryChange">The buffer memory delta: positive to increase, negative to decrease.</param>
        internal void RegisterBufferMemoryUsage(long memoryChange)
        {
            Interlocked.Add(ref bufferMemory, memoryChange);
        }
    }
}
