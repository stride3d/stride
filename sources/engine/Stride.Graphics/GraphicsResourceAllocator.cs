// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core;

namespace Stride.Graphics
{
    #region Convenience aliases

    // Convenience aliases to aid readability and reduce verbosity

    using TextureCache = GraphicsResourceAllocator.ResourceCache<TextureDescription>;
    using BufferCache = GraphicsResourceAllocator.ResourceCache<BufferDescription>;
    using QueryPoolCache = GraphicsResourceAllocator.ResourceCache<GraphicsResourceAllocator.QueryPoolDescription>;

    using CreateTextureDelegate = GraphicsResourceAllocator.CreateResourceDelegate<Texture, TextureDescription>;
    using CreateBufferDelegate = GraphicsResourceAllocator.CreateResourceDelegate<Buffer, BufferDescription>;
    using CreateQueryPoolDelegate = GraphicsResourceAllocator.CreateResourceDelegate<QueryPool, GraphicsResourceAllocator.QueryPoolDescription>;

    #endregion


    /// <summary>
    /// A <see cref="GraphicsResource"/> allocator tracking usage reference and allowing to recycle unused resources based on a recycle policy. 
    /// </summary>
    /// <remarks>
    /// This class is threadsafe. Accessing a member will lock globally this instance.
    /// </remarks>
    public class GraphicsResourceAllocator : ComponentBase
    {
        // TODO: Check if we should introduce an enum for the kind of scope (per DrawCore, per Frame...etc.)
        // TODO: Add statistics method (number of objects allocated...etc.)

        #region Internal types

        protected internal sealed class ResourceCache<TResourceDesc> : Dictionary<TResourceDesc, List<GraphicsResourceLink>>;

        protected internal record struct QueryPoolDescription(QueryType QueryType, int QueryCount);

        protected internal delegate TResource CreateResourceDelegate<TResource, TDescription>(TDescription description, PixelFormat viewFormat);

        protected internal delegate TDescription GetDescriptionDelegate<TResource, TDescription>(TResource resource);

        #endregion

        private readonly object thisLock = new();

        private readonly TextureCache textureCache = [];      // Cache for Textures by their TextureDescription
        private readonly BufferCache bufferCache = [];        // Cache for Buffers by their BufferDescription
        private readonly QueryPoolCache queryPoolCache = [];  // Cache for QueryPools by their QueryPoolDescription

        private readonly CreateTextureDelegate createTextureDelegate;
        private readonly CreateBufferDelegate createBufferDelegate;
        private readonly CreateQueryPoolDelegate createQueryPoolDelegate;


        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceAllocator" /> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        public GraphicsResourceAllocator(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            createTextureDelegate = CreateTexture;
            createBufferDelegate = CreateBuffer;
            createQueryPoolDelegate = CreateQueryPool;
        }


        /// <summary>
        /// Gets or sets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        private GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the services registry.
        /// </summary>
        /// <value>The services registry.</value>
        public IServiceRegistry Services { get; private set; } // TODO: Never set, never read, remove this property?

        /// <summary>
        /// Gets or sets the default recycle policy. Default is always recycle no matter the state of the resources.
        /// </summary>
        /// <value>The default recycle policy.</value>
        public GraphicsResourceRecyclePolicyDelegate RecyclePolicy { get; set; } = DefaultRecyclePolicy;

        /// <summary>
        /// Recycles unused resources (with a  <see cref="GraphicsResourceLink.ReferenceCount"/> == 0 ) with the <see cref="RecyclePolicy"/>. By Default, no recycle policy installed.
        /// </summary>
        private static bool DefaultRecyclePolicy(GraphicsResourceLink resourceLink) => true;


        public void Recycle()
        {
            if (RecyclePolicy is not null)
            {
                Recycle(RecyclePolicy);
            }
        }

        /// <summary>
        /// Recycles unused resource with the specified recycle policy. 
        /// </summary>
        /// <param name="recyclePolicy">The recycle policy.</param>
        /// <exception cref="System.ArgumentNullException">recyclePolicy</exception>
        public void Recycle(GraphicsResourceRecyclePolicyDelegate recyclePolicy)
        {
            ArgumentNullException.ThrowIfNull(recyclePolicy);

            // Global lock to be threadsafe
            lock (thisLock)
            {
                Recycle(textureCache, recyclePolicy);
                Recycle(bufferCache, recyclePolicy);
            }

            static void Recycle<TKey>(ResourceCache<TKey> cache, GraphicsResourceRecyclePolicyDelegate recyclePolicy)
            {
                foreach (var resourceList in cache.Values)
                {
                    for (int i = resourceList.Count - 1; i >= 0; i--)
                    {
                        var resourceLink = resourceList[i];
                        if (resourceLink.ReferenceCount == 0)
                        {
                            if (recyclePolicy(resourceLink))
                            {
                                resourceLink.Resource.Dispose();
                                resourceList.RemoveAt(i);
                            }
                            // Reset the access count
                            resourceLink.AccessCountSinceLastRecycle = 0;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Gets a texture for the specified description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>A texture</returns>
        private static BufferDescription GetBufferDescription(Buffer buffer) => buffer.Description;
        private static TextureDescription GetTextureDescription(Texture texture) => texture.Description;
        private static QueryPoolDescription GetQueryPoolDescription(QueryPool queryPool) => new(queryPool.QueryType, queryPool.QueryCount);


        public Texture GetTemporaryTexture(TextureDescription description)
        {
            // Global lock to be threadsafe
            lock (thisLock)
            {
                return GetTemporaryResource(textureCache, description, createTextureDelegate, GetTextureDescription, PixelFormat.None);
            }
        }

        /// <summary>
        /// Gets a texture for the specified description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFormat">The pixel format seen by the shader</param>
        /// <returns>A texture</returns>
        public Buffer GetTemporaryBuffer(BufferDescription description, PixelFormat viewFormat = PixelFormat.None)
        {
            // Global lock to be threadsafe
            lock (thisLock)
            {
                return GetTemporaryResource(bufferCache, description, createBufferDelegate, GetBufferDescription, viewFormat);
            }
        }

        public QueryPool GetQueryPool(QueryType queryType, int queryCount)
        {
            // Global lock to be threadsafe
            lock (thisLock)
            {
                return GetTemporaryResource(queryPoolCache, new QueryPoolDescription(queryType, queryCount), createQueryPoolDelegate, GetQueryPoolDescription, PixelFormat.None);
            }
        }

        /// <summary>
        /// Increments the reference to a temporary resource.
        /// </summary>
        /// <param name="resource"></param>
        public void AddReference(GraphicsResource resource)
        {
            // Global lock to be threadsafe
            lock (thisLock)
            {
                UpdateReference(resource, +1);
            }
        }

        /// <summary>
        /// Decrements the reference to a temporary resource.
        /// </summary>
        /// <param name="resource"></param>
        public void ReleaseReference(GraphicsResourceBase resource)
        {
            // Global lock to be threadsafe
            lock (thisLock)
            {
                UpdateReference(resource, -1);
            }
        }

        /// <summary>
        /// Creates a texture for output.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFormat">The pixel format seen by the shader</param>
        /// <returns>Texture.</returns>
        protected virtual Texture CreateTexture(TextureDescription description, PixelFormat viewFormat)
        {
            return Texture.New(GraphicsDevice, description);
        }

        /// <summary>
        /// Creates a temporary buffer.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFormat">The shader view format on the buffer</param>
        /// <returns>Buffer.</returns>
        protected virtual Buffer CreateBuffer(BufferDescription description, PixelFormat viewFormat)
        {
            return Buffer.New(GraphicsDevice, description, viewFormat);
        }

        protected virtual QueryPool CreateQueryPool(QueryPoolDescription description, PixelFormat viewFormat)
        {
            return QueryPool.New(GraphicsDevice, description.QueryType, description.QueryCount);
        }

        protected override void Destroy()
        {
            lock (thisLock)
            {
                DisposeCache(textureCache);
                DisposeCache(bufferCache);
                DisposeCache(queryPoolCache);
            }

            base.Destroy();

            static void DisposeCache<TKey>(ResourceCache<TKey> cache)
            {
                foreach (var resourceList in cache.Values)
                foreach (var resource in resourceList)
                {
                    resource.Resource.Dispose();
                }
                cache.Clear();
            }
        }


        private TResource GetTemporaryResource<TResource, TDescription>(
            ResourceCache<TDescription> cache,
            TDescription description,
            CreateResourceDelegate<TResource, TDescription> createResource,
            GetDescriptionDelegate<TResource, TDescription> getDescription,
            PixelFormat viewFormat)

            where TResource : GraphicsResourceBase
            where TDescription : struct
        {
            // For a specific description, get allocated resources
            List<GraphicsResourceLink> resourceLinks = GetOrCreateCache(description);

            // Find an available resource (non-referenced, but not disposed)
            foreach (var resourceLink in resourceLinks)
            {
                if (resourceLink.ReferenceCount == 0)
                {
                    UpdateCounter(resourceLink, +1);
                    return (TResource) resourceLink.Resource;
                }
            }

            // If no resources are available, then creates a new one
            var newResource = createResource(description, viewFormat);

            string allocatorName = string.IsNullOrWhiteSpace(Name) ? string.Empty : $"{Name}-";
            string resourceName = string.IsNullOrWhiteSpace(newResource.Name) ? newResource.GetType().Name : Name;

            newResource.Name = $"{allocatorName}{resourceName}-{resourceLinks.Count}";

            // Description may be altered when creating a resource (based on hardware limitations, etc.)
            // We get here its actual final description
            var realDescription = getDescription(newResource);

            // Get or create the resource cache for the new description
            resourceLinks = GetOrCreateCache(realDescription);

            // Add the resource to the allocated resources
            //   Start with RefCount == 1, because we don't want this resource to be available if a post-FX processor is calling
            //   several times this GetTemporaryTexture method.
            var newResourceLink = new GraphicsResourceLink(newResource) { ReferenceCount = 1 };
            resourceLinks.Add(newResourceLink);

            return newResource;

            List<GraphicsResourceLink> GetOrCreateCache(TDescription description)
            {
                // For a specific description, get allocated resources
                if (!cache.TryGetValue(description, out List<GraphicsResourceLink> resourceLinks))
                {
                    // If no resources are allocated for this description, create a new list
                    cache.Add(description, resourceLinks = []);
                }
                return resourceLinks;
            }
        }


        /// <summary>
        /// Recycles the specified cache.
        /// </summary>
        /// <typeparam name="TKey">The type of the t key.</typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="recyclePolicy">The recycle policy.</param>
        private void UpdateReference(GraphicsResourceBase resource, int referenceDelta)
        {
            if (resource is null)
                return;

            bool resourceFound = false;

            resourceFound = resource switch
            {
                Texture texture => UpdateReferenceCount(textureCache, texture, GetTextureDescription, referenceDelta),
                Buffer buffer => UpdateReferenceCount(bufferCache, buffer, GetBufferDescription, referenceDelta),
                QueryPool queryPool => UpdateReferenceCount(queryPoolCache, queryPool, GetQueryPoolDescription, referenceDelta),

                _ => throw new ArgumentException("Unsupported graphics resource. Only Textures, Buffers and QueryPools are supported", nameof(resource))
            };

            if (!resourceFound)
                throw new ArgumentException("The resource was not allocated by this allocator");
        }

        private bool UpdateReferenceCount<TDescription, TResource>(
            ResourceCache<TDescription> cache,
            TResource resource,
            GetDescriptionDelegate<TResource, TDescription> getDescription,
            int referenceDelta)

            where TResource : GraphicsResourceBase
            where TDescription : struct
        {
            if (resource is null || referenceDelta == 0)
            {
                return false;
            }

            if (cache.TryGetValue(getDescription(resource), out List<GraphicsResourceLink> resourceLinks))
            {
                foreach (var resourceLink in resourceLinks)
                {
                    if (resourceLink.Resource == resource)
                    {
                        UpdateCounter(resourceLink, referenceDelta);
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateCounter(GraphicsResourceLink resourceLink, int referenceDelta)
        {
            if ((resourceLink.ReferenceCount + referenceDelta) < 0)
            {
                throw new InvalidOperationException("Invalid decrement on reference count (must be >=0 after decrement). Current reference count: [{0}] Decrement: [{1}]".ToFormat(resourceLink.ReferenceCount, deltaCount));
            }

            resourceLink.ReferenceCount += referenceDelta;
            resourceLink.AccessTotalCount++;
            resourceLink.AccessCountSinceLastRecycle++;
            resourceLink.LastAccessTime = DateTime.Now;

            if (resourceLink.ReferenceCount == 0)
                GraphicsDevice.TagResource(resourceLink);
        }
    }
}
