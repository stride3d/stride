//#define USE_TEST_MANUAL_QUALITY
// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Streaming;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Rendering;

namespace Stride.Streaming
{
    /// <summary>
    /// Performs content streaming.
    /// </summary>
    /// <seealso cref="Stride.Games.GameSystemBase" />
    /// <seealso cref="Stride.Graphics.Data.ITexturesStreamingProvider" />
    public class StreamingManager : GameSystemBase, IStreamingManager, ITexturesStreamingProvider
    {
        private readonly List<StreamableResource> resources = new List<StreamableResource>(512);
        private readonly ConcurrentDictionary<object, StreamableResource> resourcesLookup = new ConcurrentDictionary<object, StreamableResource>();
        private readonly List<StreamableResource> activeStreaming = new List<StreamableResource>(8); // Important: alwasy use inside lock(resources)
        private int lastUpdateResourcesIndex;
        private bool isDisposing;
        private int frameIndex;
        private long currentlyAllocatedMemory; // in bytes
        private TimeSpan lastUpdate;
#if USE_TEST_MANUAL_QUALITY
        private int testQuality = 100;
#endif

        private bool HasActiveTaskSlotFree => activeStreaming.Count < MaxResourcesPerUpdate;

        /// <summary>
        /// The interval between <see cref="StreamingManager"/> updates.
        /// </summary>
        public TimeSpan ManagerUpdatesInterval = TimeSpan.FromMilliseconds(33);

        /// <summary>
        /// The <see cref="StreamableResource"/> live timeout. Resources that aren't used for a while are downscaled in quality.
        /// </summary>
        public TimeSpan ResourceLiveTimeout = TimeSpan.FromSeconds(8);

        /// <summary>
        /// The maximum number of resources updated per streaming manager tick. Used to balance performance/streaming speed.
        /// </summary>
        public int MaxResourcesPerUpdate = 8;

        /// <summary>
        /// The targeted memory budget of the streaming system in MB. If the memory allocated by streaming system is under this budget it will not try to unload not visible resources.
        /// </summary>
        public int TargetedMemoryBudget = 512;

        /// <summary>
        /// Gets the memory currently allocated by streamable resources in MB.
        /// </summary>
        public float AllocatedMemory => AllocatedMemoryBytes / (float)0x100000;

        /// <summary>
        /// Gets the extact memory amount allocated by streamable resources in bytes.
        /// </summary>
        public long AllocatedMemoryBytes => Interlocked.Read(ref currentlyAllocatedMemory);

        /// <summary>
        /// Gets the content streaming service.
        /// </summary>
        public ContentStreamingService ContentStreaming { get; }

        /// <summary>
        /// List with all registered streamable resources.
        /// </summary>
        public ICollection<StreamableResource> Resources => resources;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingManager"/> class.
        /// </summary>
        /// <param name="services">The servicies registry.</param>
        /// <remarks>
        /// The GameSystem expects the following services to be registered: <see cref="T:Stride.Games.IGame" /> and <see cref="T:Stride.Core.Serialization.Contents.IContentManager" />.
        /// </remarks>
        public StreamingManager([NotNull] IServiceRegistry services)
            : base(services)
        {
            services.AddService(this);
            services.AddService<IStreamingManager>(this);
            services.AddService<ITexturesStreamingProvider>(this);

            ContentStreaming = new ContentStreamingService();

            Enabled = true;
            EnabledChanged += OnEnabledChanged;
        }

        /// <inheritdoc />
        public void SetStreamingSettings(StreamingSettings streamingSettings)
        {
            ManagerUpdatesInterval = streamingSettings.ManagerUpdatesInterval;
            MaxResourcesPerUpdate = streamingSettings.MaxResourcesPerUpdate;
            ResourceLiveTimeout = streamingSettings.ResourceLiveTimeout;
            Enabled = streamingSettings.Enabled;
            TargetedMemoryBudget = streamingSettings.TargetedMemoryBudget;
        }

        /// <inheritdoc />
        protected override void Destroy()
        {
            isDisposing = true;

            if (Services.GetService<StreamingManager>() == this)
            {
                Services.RemoveService<StreamingManager>();
            }
            if (Services.GetService<IStreamingManager>() == this)
            {
                Services.RemoveService<IStreamingManager>();
            }
            if (Services.GetService<ITexturesStreamingProvider>() == this)
            {
                Services.RemoveService<ITexturesStreamingProvider>();
            }

            lock (resources)
            {
                resources.ForEach(x => x.Release());
                resources.Clear();
                resourcesLookup.Clear();
                activeStreaming.Clear();
                currentlyAllocatedMemory = 0;
            }

            ContentStreaming.Dispose();

            base.Destroy();
        }

        /// <summary>
        /// Gets the <see cref="StreamableResource"/> corresponding to the given resource object.
        /// </summary>
        /// <typeparam name="T">The type of the streamable resource.</typeparam>
        /// <param name="obj">The object.</param>
        /// <returns>Streamable resource, or null if it can't be found.</returns>
        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(object obj) where T : StreamableResource
        {
            StreamableResource result;
            resourcesLookup.TryGetValue(obj, out result);
            return result as T;
        }

        /// <summary>
        /// Gets the <see cref="StreamingTexture"/> corresponding to the given texture object.
        /// </summary>
        /// <param name="obj">The texture object.</param>
        /// <returns>Streamable texture, or null if it can't be found.</returns>
        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StreamingTexture Get(Texture obj)
        {
            return Get<StreamingTexture>(obj);
        }

        /// <summary>
        /// Called when render mesh is submitted to rendering. Registers referenced resources to stream them.
        /// </summary>
        /// <param name="renderMesh">The render mesh.</param>
        /// <param name="options">The streaming options when streaming those resources</param>
        public void StreamResources(RenderMesh renderMesh, StreamingOptions? options = null)
        {
            if (renderMesh.MaterialPass != null)
            {
                StreamResources(renderMesh.MaterialPass.Parameters, options);
            }
        }

        /// <summary>
        /// Called when material parameters are submitted to rendering. Registers referenced resources to stream them.
        /// </summary>
        /// <param name="parameters">The material parameters.</param>
        /// <param name="options">The streaming options when streaming those resources</param>
        public void StreamResources(ParameterCollection parameters, StreamingOptions? options = null)
        {
            if (parameters.ObjectValues == null)
                return;

            // Register all binded textures
            foreach (var e in parameters.ObjectValues)
            {
                if (e is Texture t)
                    StreamResources(t, options);
            }
        }

        /// <summary>
        /// Called when texture is submitted to rendering. Registers referenced resources to stream them.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="options">The streaming options when streaming those resources</param>
        public void StreamResources(Texture texture, StreamingOptions? options = null)
        {
            if (texture == null)
                return;

            var resource = Get(texture);
            if (resource != null)
            {
                resource.LastTimeUsed = frameIndex;
                if (options.HasValue)
                    SetResourceStreamingOptions(resource, options.Value, true);
            }
        }

        /// <summary>
        /// Set the streaming options for the resources
        /// </summary>
        /// <param name="renderMesh">The render mesh.</param>
        /// <param name="options">The streaming options when streaming those resources</param>
        public void SetResourceStreamingOptions(RenderMesh renderMesh, StreamingOptions? options = null)
        {
            if (renderMesh.MaterialPass != null)
            {
                SetResourceStreamingOptions(renderMesh.MaterialPass.Parameters, options);
            }
        }

        /// <summary>
        /// Set the streaming options for the resources
        /// </summary>
        /// <param name="parameters">The material parameters.</param>
        /// <param name="options">The streaming options when streaming those resources</param>
        public void SetResourceStreamingOptions(ParameterCollection parameters, StreamingOptions? options = null)
        {
            if (parameters.ObjectValues == null)
                return;

            // Register all binded textures
            foreach (var e in parameters.ObjectValues)
            {
                if (e is Texture t)
                    StreamResources(t, options);
            }
        }

        /// <summary>
        /// Set the streaming options for the resources
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="options">The streaming options when streaming those resources</param>
        public void SetResourceStreamingOptions(Texture texture, StreamingOptions? options = null)
        {
            if (texture == null)
                return;

            var resource = Get(texture);
            if (resource != null)
            {
                if (options.HasValue)
                    SetResourceStreamingOptions(resource, options.Value, false);
            }
        }

        /// <summary>
        /// Notify the streaming manager of the change memory used by the resources.
        /// </summary>
        /// <param name="memoryUsedChange">The change of memory used in bytes. This value can be positive of negative.</param>
        public void RegisterMemoryUsage(long memoryUsedChange)
        {
            Interlocked.Add(ref currentlyAllocatedMemory, memoryUsedChange);
        }

        private void SetResourceStreamingOptions(StreamingTexture resource, StreamingOptions options, bool combineOptions)
        {
            var alreadyHasOptions = resource.StreamingOptions.HasValue;
            var newOptions = combineOptions && alreadyHasOptions ? options.CombineWith(resource.StreamingOptions.Value) : options;

            lock (resources)
            {
                resource.StreamingOptions = newOptions;

                if (newOptions.LoadImmediately)
                {
                    // ensure that the resource is not currently streaming
                    if (!resource.CanBeUpdated)
                    {
                        resource.StopStreaming();
                        FlushSync();
                    }

                    // Stream resource to the maximum level
                    FullyLoadResource(resource);
                }
            }
        }

        internal void RegisterResource(StreamableResource resource)
        {
            Debug.Assert(resource != null && isDisposing == false, $"resource[{resource}] != null && isDisposing[{isDisposing}] == false");

            lock (resources)
            {
                Debug.Assert(!resources.Contains(resource), "!resources.Contains(resource)");

                resources.Add(resource);
                if(resourcesLookup.TryAdd(resource.Resource, resource) == false)
                    throw new InvalidOperationException();
            }
        }

        internal void UnregisterResource(StreamableResource resource)
        {
            if (isDisposing)
                return;

            Debug.Assert(resource != null, "resource != null");

            lock (resources)
            {
                Debug.Assert(resources.Contains(resource), "resources.Contains(resource)");

                resources.Remove(resource);
                resourcesLookup.TryRemove(resource.Resource, out _);
                activeStreaming.Remove(resource);
            }
        }

        private StreamingTexture CreateStreamingTexture(Texture obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader)
        {
            // Get content storage container
            var storage = ContentStreaming.GetStorage(ref storageHeader);
            if (storage == null)
                throw new ContentStreamingException("Missing content storage.");

            // Find resource or create new
            var resource = Get(obj);
            if (resource == null)
            {
                resource = new StreamingTexture(this, obj);
                RegisterResource(resource);
            }

            // Update resource storage/description information (may be modified on asset rebuilding)
            resource.Init(Services.GetSafeServiceAs<IDatabaseFileProviderService>(), storage, ref imageDescription);

            // Check if cannot use streaming
            if (!Enabled)
            {
                FullyLoadResource(resource);
            }

            return resource;
        }

        /// <inheritdoc />
        void ITexturesStreamingProvider.FullyLoadTexture(Texture obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader)
        {
            lock (resources)
            {
                // Get streaming object
                var resource = CreateStreamingTexture(obj, ref imageDescription, ref storageHeader);

                // Stream resource to the maximum level
                FullyLoadResource(resource);

                // Release streaming object
                resource.Release();
            }
        }

        /// <inheritdoc />
        void ITexturesStreamingProvider.RegisterTexture(Texture obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader)
        {
            lock (resources)
            {
                CreateStreamingTexture(obj, ref imageDescription, ref storageHeader);
            }
        }

        /// <inheritdoc />
        void ITexturesStreamingProvider.UnregisterTexture(Texture obj)
        {
            Debug.Assert(obj != null, "obj != null");

            lock (resources)
            {
                var resource = Get(obj);
                resource?.Release();
            }
        }

        /// <inheritdoc />
        void IStreamingManager.FullyLoadResource(object obj)
        {
            StreamableResource resource;
            lock (resources)
            {
                resource = Get<StreamableResource>(obj);
            }

            if (resource != null)
                FullyLoadResource(resource);
        }

        private void FullyLoadResource(StreamableResource resource)
        {
            if (resource.AllocatedResidency == resource.MaxResidency)
                return;

            // Stream resource to the maximum level
            // Note: this does not care about MaxTasksRunningSimultaneously limit
            if (resource.CurrentResidency != resource.MaxResidency)
            {
                var task = StreamAsync(resource, resource.MaxResidency);
                task.Wait();
            }

            // Synchronize
            FlushSync();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Make sure enough time passed
            if (gameTime.Total < lastUpdate + ManagerUpdatesInterval)
                return;

            // Update resources
            if (Enabled)
            {
                lock (resources)
                {
                    // Perform synchronization
                    FlushSync();

#if USE_TEST_MANUAL_QUALITY // Temporary testing code used for testing quality changing using K/L keys
                    if (((Game)Game).Input.IsKeyPressed(Stride.Input.Keys.K))
                    {
                        testQuality = Math.Min(testQuality + 5, 100);
                    }
                    if (((Game)Game).Input.IsKeyPressed(Stride.Input.Keys.L))
                    {
                        testQuality = Math.Max(testQuality - 5, 0);
                    }
#endif
                    int resourcesCount = Resources.Count;
                    if (resourcesCount > 0)
                    {
                        var now = DateTime.UtcNow;
                        var resourcesChecks = resourcesCount;

                        while (resourcesChecks-- > 0 && HasActiveTaskSlotFree)
                        {
                            // Move forward
                            // Note: we update resources like in a ring buffer
                            lastUpdateResourcesIndex++;
                            if (lastUpdateResourcesIndex >= resourcesCount)
                                lastUpdateResourcesIndex = 0;

                            // Update resource
                            var resource = resources[lastUpdateResourcesIndex];
                            var ignoreResource = resource.StreamingOptions?.IgnoreResource ?? false;
                            if (resource.CanBeUpdated && !ignoreResource)
                            {
                                Update(resource, ref now);
                            }
                        }
                        // TODO: add StreamingManager stats, update time per frame, updates per frame, etc.
                    }
                }
            }

            ContentStreaming.Update();

            frameIndex++;

            lastUpdate = gameTime.Total;
        }

        private void Update(StreamableResource resource, ref DateTime now)
        {
            Debug.Assert(resource != null && resource.CanBeUpdated, $"resource[{resource}] != null && resource.CanBeUpdated[{resource?.CanBeUpdated}]");

            var options = resource.StreamingOptions ?? StreamingOptions.Default;
            var isUnderBudget = AllocatedMemory < TargetedMemoryBudget;

            // Calculate target quality for that asset
            StreamingQuality targetQuality = StreamingQuality.Mininum;
            if (isUnderBudget || resource.LastTimeUsed > 0 || options.KeepLoaded)
            {
                var lastUsageTimespan = new TimeSpan((frameIndex - resource.LastTimeUsed) * ManagerUpdatesInterval.Ticks);
                if (isUnderBudget || lastUsageTimespan <= ResourceLiveTimeout || options.KeepLoaded)
                {
                    targetQuality = StreamingQuality.Maximum;
#if USE_TEST_MANUAL_QUALITY
                    targetQuality = (testQuality / 100.0f); // apply quality scale for testing
#endif
                    // TODO: here we should apply resources group master scale (based on game settings quality level and memory level)
                }
            }
            targetQuality.Normalize();

            // Calculate target residency level (discrete value)
            var currentResidency = resource.CurrentResidency;
            var allocatedResidency = resource.AllocatedResidency;
            var targetResidency = resource.CalculateTargetResidency(targetQuality);
            Debug.Assert(allocatedResidency >= currentResidency && allocatedResidency >= 0, $"allocatedResidency[{allocatedResidency}] >= currentResidency[{currentResidency}] && allocatedResidency[{allocatedResidency}] >= 0");

            // Update target residency smoothing
            // TODO: use move quality samples and use max or avg value - make that input it smooth - or use PID
            //resource.QualitySamples.Add(targetResidency);
            //targetResidency = resource.QualitySamples.Maximum();

            // Assign last update time
            resource.LastUpdate = now;

            // Check if a target residency level has been changed
            if (targetResidency != resource.TargetResidency)
            {
                // Register change
                resource.TargetResidency = targetResidency;
                resource.TargetResidencyChange = now;
            }

            // Check if need to change resource current residency
            if (targetResidency != currentResidency)
            {
                // Calculate residency level to stream in (resources may want to incease/decrease it's quality in steps rather than at once)
                var requestedResidency = resource.CalculateRequestedResidency(targetResidency);
                if (options.ForceHighestQuality)
                    requestedResidency = targetResidency;

                // Create streaming task (resource type specific)
                StreamAsync(resource, requestedResidency);
            }
        }

        private Task StreamAsync(StreamableResource resource, int residency)
        {
            activeStreaming.Add(resource);
            var task = resource.StreamAsyncInternal(residency);
            task.Start();

            return task;
        }

        private void FlushSync()
        {
            for (int i = activeStreaming.Count - 1; i >= 0; i--)
            {
                var resource = activeStreaming[i];
                if (resource.IsTaskActive)
                    continue;

                resource.FlushSync();
                activeStreaming.RemoveAt(i);
            }
        }

        private void OnEnabledChanged(object sender, EventArgs e)
        {
            if (!Enabled)
            {
                lock (resources)
                {
                    activeStreaming.ForEach(stream => stream.StopStreaming());
                    FlushSync();

                    resources.ForEach(FullyLoadResource);
                }
            }
        }
    }
}
