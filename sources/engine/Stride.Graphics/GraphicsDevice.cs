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
        /// <summary>
        ///   Cache of Pipeline States, keyed by their descriptions.
        ///   It optimizes performance by avoiding redundant state creation.
        /// </summary>
        internal readonly Dictionary<PipelineStateDescriptionWithHash, PipelineState> CachedPipelineStates = [];
        /// <summary>
        ///   Cache of Sampler States, keyed by their descriptions.
        ///   It helps optimize performance by avoiding redundant creation of identical Sampler States.
        /// </summary>
        internal readonly Dictionary<SamplerStateDescription, SamplerState> CachedSamplerStates = [];

        /// <summary>
        ///   Gets the features supported by the Graphics Device.
        /// </summary>
        public ref readonly GraphicsDeviceFeatures Features => ref features;
        private GraphicsDeviceFeatures features;

        /// <summary>
        ///   The set of Graphics Resources currently managed by the Graphics Device.
        /// </summary>
        internal HashSet<GraphicsResourceBase> Resources = [];

        /// <summary>
        ///   The currently active Effect being used by the Graphics Device.
        /// </summary>
        internal Effect CurrentEffect;

        // A dictionary of shared data created for this Graphics Device which can be shared between multiple components.
        // For example, the shared 2x2 White Texture, or the Full-screen Triangle primitive.
        private readonly Dictionary<object, IDisposable> sharedDataPerDevice = [];
        private readonly List<IDisposable> sharedDataToDispose = [];

        /// <summary>
        ///   The default Pipeline State object used by the Graphics Device.
        /// </summary>
        internal PipelineState DefaultPipelineState;

        /// <summary>
        ///   The main Command List used by the Graphics Device.
        /// </summary>
        internal CommandList InternalMainCommandList;

        /// <summary>
        ///   A cached primitive object that can be used to draw quads (rectangles composed of two triangles).
        /// </summary>
        internal PrimitiveQuad PrimitiveQuad;  // TODO: This is not a quad, but a fullscreen triangle! Maybe this class should be renamed?

        private ColorSpace colorSpace;

        /// <summary>
        ///   The number of triangles drawn in the last frame.
        /// </summary>
        public uint FrameTriangleCount;  // TODO: Public mutable fields?
        /// <summary>
        ///   The number of draw calls made in the last frame.
        /// </summary>
        public uint FrameDrawCalls;

        private long bufferMemory;
        private long textureMemory;

        /// <summary>
        ///   Gets the amount of GPU memory currently allocated to Buffers, in bytes.
        /// </summary>
        public long BuffersMemory => Interlocked.Read(ref bufferMemory);

        /// <summary>
        ///   Gets the amount of GPU memory currently allocated to Textures, in bytes.
        /// </summary>
        public long TextureMemory => Interlocked.Read(ref textureMemory);

        /// <summary>
        ///   Gets the graphics platform (and the graphics API) the Graphics Device is using.
        /// </summary>
        public static GraphicsPlatform Platform => GraphicPlatform;

        /// <summary>
        ///   Gets a string that identifies the underlying device used by the Graphics Device to render.
        /// </summary>
        /// <remarks>
        ///   In the case of Direct3D and Vulkan, for example, this will return the name of the Graphics Adapter
        ///   (e.g. <c>"nVIDIA GeForce RTX 2080"</c>). Other platforms may return a different string.
        /// </remarks>
        public string RendererName => GetRendererName();

        /// <inheritdoc cref="RendererName"/>
        private partial string GetRendererName();


        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="adapter">The physical Graphics Adapter for which to create a Graphics Device.</param>
        /// <param name="graphicsProfiles">
        ///   <para>
        ///     A list of the graphics profiles to try, in order of preference. This parameter cannot be <see langword="null"/>,
        ///     but if an empty array is passed, the default fallback profiles will be used.
        ///   </para>
        ///   <para>
        ///     The default fallback profiles are: <see cref="GraphicsProfile.Level_11_0"/>, <see cref="GraphicsProfile.Level_10_1"/>,
        ///     <see cref="GraphicsProfile.Level_10_0"/>, <see cref="GraphicsProfile.Level_9_3"/>, <see cref="GraphicsProfile.Level_9_2"/>, and
        ///     <see cref="GraphicsProfile.Level_9_1"/>.
        ///   </para>
        /// </param>
        /// <param name="creationFlags">
        ///   A combination of <see cref="DeviceCreationFlags"/> flags that determines how the Graphics Device will be created.
        /// </param>
        /// <param name="windowHandle">
        ///   The <see cref="WindowHandle"/> specifying the window the Graphics Device will present to,
        ///   or <see langword="null"/> if the device should not depend on a window.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsProfiles"/> is <see langword="null"/>.</exception>
        protected GraphicsDevice(GraphicsAdapter adapter, GraphicsProfile[] graphicsProfiles, DeviceCreationFlags creationFlags, WindowHandle windowHandle)
        {
            Recreate(adapter, graphicsProfiles, creationFlags, windowHandle);

            // Helpers
            PrimitiveQuad = new PrimitiveQuad(this);
        }


        /// <summary>
        ///   Tries to create or reinitialize the Graphics Device.
        /// </summary>
        /// <param name="adapter">The physical Graphics Adapter for which to recreate the Graphics Device.</param>
        /// <param name="graphicsProfiles">
        ///   <para>
        ///     A list of the graphics profiles to try, in order of preference. This parameter cannot be <see langword="null"/>,
        ///     but if an empty array is passed, the default fallback profiles will be used.
        ///   </para>
        ///   <para>
        ///     The default fallback profiles are: <see cref="GraphicsProfile.Level_11_0"/>, <see cref="GraphicsProfile.Level_10_1"/>,
        ///     <see cref="GraphicsProfile.Level_10_0"/>, <see cref="GraphicsProfile.Level_9_3"/>, <see cref="GraphicsProfile.Level_9_2"/>, and
        ///     <see cref="GraphicsProfile.Level_9_1"/>.
        ///   </para>
        /// </param>
        /// <param name="creationFlags">
        ///   A combination of <see cref="DeviceCreationFlags"/> flags that determines how the Graphics Device will be created.
        /// </param>
        /// <param name="windowHandle">
        ///   The <see cref="WindowHandle"/> specifying the window the Graphics Device will present to,
        ///   or <see langword="null"/> if the device should not depend on a window.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsProfiles"/> is <see langword="null"/>.</exception>
        public void Recreate(GraphicsAdapter adapter, GraphicsProfile[] graphicsProfiles, DeviceCreationFlags creationFlags, WindowHandle windowHandle)
        {
            ArgumentNullException.ThrowIfNull(adapter);
            ArgumentNullException.ThrowIfNull(graphicsProfiles); // TODO: Why different from Array.Empty?

            Adapter = adapter;
            IsDebugMode = creationFlags.HasFlag(DeviceCreationFlags.Debug);

            // Default fallback
            if (graphicsProfiles.Length == 0)
                graphicsProfiles = [ GraphicsProfile.Level_11_0, GraphicsProfile.Level_10_1, GraphicsProfile.Level_10_0, GraphicsProfile.Level_9_3, GraphicsProfile.Level_9_2, GraphicsProfile.Level_9_1 ];

            // Initialize this instance
            InitializePlatformDevice(graphicsProfiles, creationFlags, windowHandle);

            // Checks the features supported by the new Graphics Device
            features = new GraphicsDeviceFeatures(this);

            // Initialize the internal states of the new Graphics Device
            SamplerStates = new SamplerStateFactory(this);

            var defaultPipelineStateDescription = new PipelineStateDescription();
            defaultPipelineStateDescription.SetDefaults();
            AdjustDefaultPipelineStateDescription(ref defaultPipelineStateDescription);
            DefaultPipelineState = PipelineState.New(this, defaultPipelineStateDescription);

            InitializePostFeatures();
        }

        /// <summary>
        ///   Initialize the platform-specific implementation of the Graphics Device.
        /// </summary>
        /// <param name="graphicsProfiles">A non-<see langword="null"/> list of the graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private unsafe partial void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle);

        /// <summary>
        ///   Initializes the platform-specific features of the Graphics Device once it has been fully initialized.
        /// </summary>
        private unsafe partial void InitializePostFeatures();

        /// <inheritdoc/>
        protected override void Destroy()
        {
            SamplerStates.Dispose();
            SamplerStates = null;

            DefaultPipelineState.Dispose();

            PrimitiveQuad.Dispose();

            DisposeSharedData();

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

            // Destroy all the associated resources first (Command Lists, Pipeline States, etc)
            base.Destroy();

            DestroyPlatformDevice();

            //
            // Disposes all the shared data created by this Graphics Device.
            //
            void DisposeSharedData()
            {
                // Disposes in reverse order so that the last created data is disposed first
                for (int index = sharedDataToDispose.Count - 1; index >= 0; index--)
                    sharedDataToDispose[index].Dispose();

                sharedDataToDispose.Clear();
                sharedDataPerDevice.Clear();
            }
        }

        /// <summary>
        ///   Releases the platform-specific Graphics Device and all its associated resources.
        /// </summary>
        protected partial void DestroyPlatformDevice();


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
        ///   Because this method is being called from a <see langword="lock"/> region, this method should not be time consuming.
        /// </remarks>
        public delegate T CreateSharedData<out T>(GraphicsDevice device) where T : class, IDisposable;

        /// <summary>
        ///   Gets the physical Graphics Adapter the Graphics Device is attached to.
        /// </summary>
        public GraphicsAdapter Adapter { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether the Graphics Device is in "Debug mode".
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Graphics Device is initialized in "Debug mode"; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsDebugMode { get; private set; }

        /// <summary>
        ///   Gets a value indicating wether the Graphics Device allows for concurrent building and deferred submission
        ///   of <see cref="CommandList"/>s.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Graphics Device allows deferred execution; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsDeferred { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether the Graphics Device supports GPU markers and profiling.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Graphics Device allows profiling and creating GPU markers; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsProfilingSupported { get; private set; }

        /// <summary>
        ///   Gets or sets the default color space of the Graphics Device.
        /// </summary>
        public ColorSpace ColorSpace
        {
            get => features.HasSRgb ? colorSpace : ColorSpace.Gamma;
            set => colorSpace = value;
        }

        /// <summary>
        ///   Gets or sets the current presenter used to display frames with the Graphics Device.
        /// </summary>
        public virtual GraphicsPresenter Presenter { get; set; }

        /// <summary>
        ///   Gets the factory that can be used to retrieve commonly used Sampler States.
        /// </summary>
        public SamplerStateFactory SamplerStates { get; private set; }

        /// <summary>
        ///   Gets the graphics profile the Graphics Device is using, which determines the available features.
        /// </summary>
        internal GraphicsProfile? ShaderProfile { get; set; }


        /// <summary>
        ///   Creates a new <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <param name="creationFlags">
        ///   A combination of <see cref="DeviceCreationFlags"/> flags that determines how the Graphics Device will be created.
        /// </param>
        /// <param name="graphicsProfiles">
        ///   <para>
        ///     A list of the graphics profiles to try, in order of preference. This parameter cannot be <see langword="null"/>,
        ///     but if an empty array is passed, the default fallback profiles will be used.
        ///   </para>
        ///   <para>
        ///     The default fallback profiles are: <see cref="GraphicsProfile.Level_11_0"/>, <see cref="GraphicsProfile.Level_10_1"/>,
        ///     <see cref="GraphicsProfile.Level_10_0"/>, <see cref="GraphicsProfile.Level_9_3"/>, <see cref="GraphicsProfile.Level_9_2"/>, and
        ///     <see cref="GraphicsProfile.Level_9_1"/>.
        ///   </para>
        /// </param>
        /// <returns>The new instance of <see cref="GraphicsDevice"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsProfiles"/> is <see langword="null"/>.</exception>
        public static GraphicsDevice New(DeviceCreationFlags creationFlags = DeviceCreationFlags.None, params GraphicsProfile[] graphicsProfiles)
        {
            return New(GraphicsAdapterFactory.DefaultAdapter, creationFlags, graphicsProfiles);
        }

        /// <summary>
        ///   Creates a new <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <param name="adapter">
        ///   The Graphics Adapter the new device will use, or <see langword="null"/> to use the system's default adapter.
        /// </param>
        /// <param name="creationFlags">
        ///   A combination of <see cref="DeviceCreationFlags"/> flags that determines how the Graphics Device will be created.
        /// </param>
        /// <param name="graphicsProfiles">
        ///   <para>
        ///     A list of the graphics profiles to try, in order of preference. This parameter cannot be <see langword="null"/>,
        ///     but if an empty array is passed, the default fallback profiles will be used.
        ///   </para>
        ///   <para>
        ///     The default fallback profiles are: <see cref="GraphicsProfile.Level_11_0"/>, <see cref="GraphicsProfile.Level_10_1"/>,
        ///     <see cref="GraphicsProfile.Level_10_0"/>, <see cref="GraphicsProfile.Level_9_3"/>, <see cref="GraphicsProfile.Level_9_2"/>, and
        ///     <see cref="GraphicsProfile.Level_9_1"/>.
        ///   </para>
        /// </param>
        /// <returns>The new instance of <see cref="GraphicsDevice"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsProfiles"/> is <see langword="null"/>.</exception>
        public static GraphicsDevice New(GraphicsAdapter adapter, DeviceCreationFlags creationFlags = DeviceCreationFlags.None, params GraphicsProfile[] graphicsProfiles)
        {
            return new GraphicsDevice(adapter ?? GraphicsAdapterFactory.DefaultAdapter, graphicsProfiles, creationFlags, windowHandle: null);
        }

        /// <summary>
        ///   Creates a new <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <param name="adapter">
        ///   The Graphics Adapter the new device will use, or <see langword="null"/> to use the system's default adapter.
        /// </param>
        /// <param name="creationFlags">
        ///   A combination of <see cref="DeviceCreationFlags"/> flags that determines how the Graphics Device will be created.
        /// </param>
        /// <param name="windowHandle">
        ///   The <see cref="WindowHandle"/> specifying the window the Graphics Device will present to,
        ///   or <see langword="null"/> if the device should not depend on a window.
        /// </param>
        /// <param name="graphicsProfiles">
        ///   <para>
        ///     A list of the graphics profiles to try, in order of preference. This parameter cannot be <see langword="null"/>,
        ///     but if an empty array is passed, the default fallback profiles will be used.
        ///   </para>
        ///   <para>
        ///     The default fallback profiles are: <see cref="GraphicsProfile.Level_11_0"/>, <see cref="GraphicsProfile.Level_10_1"/>,
        ///     <see cref="GraphicsProfile.Level_10_0"/>, <see cref="GraphicsProfile.Level_9_3"/>, <see cref="GraphicsProfile.Level_9_2"/>, and
        ///     <see cref="GraphicsProfile.Level_9_1"/>.
        ///   </para>
        /// </param>
        /// <returns>The new instance of <see cref="GraphicsDevice"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsProfiles"/> is <see langword="null"/>.</exception>
        public static GraphicsDevice New(GraphicsAdapter adapter, DeviceCreationFlags creationFlags = DeviceCreationFlags.None, WindowHandle windowHandle = null, params GraphicsProfile[] graphicsProfiles)
        {
            return new GraphicsDevice(adapter ?? GraphicsAdapterFactory.DefaultAdapter, graphicsProfiles, creationFlags, windowHandle);
        }


        /// <summary>
        ///   Gets a shared data object for the Graphics Device context with a delegate to create the shared data if it is not present.
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
        ///   Adds or subtracts to the texture memory amount the Graphics Device has allocated.
        /// </summary>
        /// <param name="memoryChange">The texture memory delta: positive to increase, negative to decrease.</param>
        internal void RegisterTextureMemoryUsage(long memoryChange)
        {
            Interlocked.Add(ref textureMemory, memoryChange);
        }

        /// <summary>
        ///   Adds or subtracts to the buffer memory amount the Graphics Device has allocated.
        /// </summary>
        /// <param name="memoryChange">The buffer memory delta: positive to increase, negative to decrease.</param>
        internal void RegisterBufferMemoryUsage(long memoryChange)
        {
            Interlocked.Add(ref bufferMemory, memoryChange);
        }

        /// <summary>
        ///   Makes platform-specific adjustments to the Pipeline State objects created by the Graphics Device.
        /// </summary>
        /// <param name="pipelineStateDescription">A Pipeline State description that can be modified and adjusted.</param>
        private partial void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription);

        /// <summary>
        ///   Tags a Graphics Resource as no having alive references, meaning it should be safe to dispose it
        ///   or discard its contents during the next <see cref="CommandList.MapSubResource"/> or <c>SetData</c> operation.
        /// </summary>
        /// <param name="resourceLink">
        ///   A <see cref="GraphicsResourceLink"/> object identifying the Graphics Resource along some related allocation information.
        /// </param>
        internal partial void TagResourceAsNotAlive(GraphicsResourceLink resourceLink);
    }
}
