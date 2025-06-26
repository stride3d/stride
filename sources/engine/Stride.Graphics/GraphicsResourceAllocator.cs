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
    ///   A <see cref="GraphicsResource"/> allocator tracking usage references and allowing to recycle unused resources based on a recycle policy.
    /// </summary>
    /// <remarks>
    ///   This class is thread-safe. Accessing any member will acquire a global lock on this instance.
    /// </remarks>
    public class GraphicsResourceAllocator : ComponentBase
    {
        // TODO: Check if we should introduce an enum for the kind of scope (per DrawCore, per Frame...etc.)
        // TODO: Add statistics method (number of objects allocated...etc.)

        #region Internal types

        /// <summary>
        ///   Internal type for a cache of Graphics Resources associated with their description.
        /// </summary>
        /// <typeparam name="TResourceDesc">The type of an object that describes the characteristics of the cached Graphics Resource.</typeparam>
        protected internal sealed class ResourceCache<TResourceDesc> : Dictionary<TResourceDesc, List<GraphicsResourceLink>>;

        /// <summary>
        ///   A description of a GPU queries pool.
        /// </summary>
        /// <param name="QueryType">The type of the pooled GPU queries.</param>
        /// <param name="QueryCount">The number of queries in the pool.</param>
        protected internal record struct QueryPoolDescription(QueryType QueryType, int QueryCount);

        /// <summary>
        ///   Represents a method that creates a Graphics Resource of type <typeparamref name="TResource"/> based on the specified
        ///   description and pixel format.
        /// </summary>
        /// <typeparam name="TResource">The type of the Graphics Resource to be created.</typeparam>
        /// <typeparam name="TDescription">The type of the description used to define the Graphics Resource.</typeparam>
        /// <param name="description">The description that specifies the details of the Graphics Resource to be created.</param>
        /// <param name="viewFormat">The data format to be used for SRVs on the Graphics Resource.</param>
        /// <returns>
        ///   A new instance of <typeparamref name="TResource"/> created based on the provided description and SRV data format.
        /// </returns>
        protected internal delegate TResource CreateResourceDelegate<TResource, TDescription>(TDescription description, PixelFormat viewFormat);

        /// <summary>
        ///   Represents a method that retrieves a description of a Graphics Resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the Graphics Resource for which the description is retrieved.</typeparam>
        /// <typeparam name="TDescription">The type of the description returned for the Graphics Resource.</typeparam>
        /// <param name="resource">The Graphics Resource for which the description is to be retrieved.</param>
        /// <returns>The description of the specified Graphics Resource.</returns>
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
        ///   Initializes a new instance of the <see cref="GraphicsResourceAllocator"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device.</param>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
        public GraphicsResourceAllocator(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));

            // We cache the delegates to avoid allocations on each call. They can't be static because they
            // can be overridden in derived classes
            createTextureDelegate = CreateTexture;
            createBufferDelegate = CreateBuffer;
            createQueryPoolDelegate = CreateQueryPool;
        }


        /// <summary>
        ///   Gets or sets the Graphics Device.
        /// </summary>
        private GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        ///   Gets the services registry.
        /// </summary>
        public IServiceRegistry Services { get; private set; } // TODO: Never set, never read, remove this property?

        /// <summary>
        ///   Gets or sets the default recycle policy.
        /// </summary>
        /// <value>The recycle policy to apply by default by the Graphics Resource Allocator.</value>
        /// <remarks>
        ///   <para>
        ///     A recycle policy is a delegate that inspects a Graphics Resource and some allocation and access
        ///     information, and determines if it should be recycled (disposed) or not.
        ///   </para>
        ///   <para>
        ///     The default recycle policy (<see cref="DefaultRecyclePolicy"/>) is to always recycle a Graphics Resource
        ///     irrespective of its state.
        ///   </para>
        /// </remarks>
        public GraphicsResourceRecyclePolicyDelegate RecyclePolicy { get; set; } = DefaultRecyclePolicy;

        /// <summary>
        ///   The default recycle policy that always removes all allocated Graphics Resources.
        /// </summary>
        private static bool DefaultRecyclePolicy(GraphicsResourceLink resourceLink) => true;


        /// <summary>
        ///   Recycles unused resources (those with a <see cref="GraphicsResourceLink.ReferenceCount"/> of 0)
        ///   with the <see cref="RecyclePolicy"/>.
        /// </summary>
        /// <remarks>
        ///   If no recycle policy is set (it is <see langword="null"/>), this method does not recycle anything.
        /// </remarks>
        public void Recycle()
        {
            if (RecyclePolicy is not null)
            {
                Recycle(RecyclePolicy);
            }
        }

        /// <summary>
        ///   Recycles unused resources (those with a <see cref="GraphicsResourceLink.ReferenceCount"/> of 0)
        ///   with the specified recycle policy.
        /// </summary>
        /// <param name="recyclePolicy">The recycle policy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="recyclePolicy"/> is <see langword="null"/>.</exception>
        public void Recycle(GraphicsResourceRecyclePolicyDelegate recyclePolicy)
        {
            ArgumentNullException.ThrowIfNull(recyclePolicy);

            // Global lock to be thread-safe
            lock (thisLock)
            {
                Recycle(textureCache, recyclePolicy);
                Recycle(bufferCache, recyclePolicy);
            }

            /// <summary>
            ///   Recycles the specified cache.
            /// </summary>
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
        ///   Returns a description for a specified <see cref="Buffer"/>.
        /// </summary>
        private static BufferDescription GetBufferDescription(Buffer buffer) => buffer.Description;
        /// <summary>
        ///   Returns a description for a specified <see cref="Texture"/>.
        /// </summary>
        private static TextureDescription GetTextureDescription(Texture texture) => texture.Description;
        /// <summary>
        ///   Returns a description for a specified <see cref="QueryPool"/>.
        /// </summary>
        private static QueryPoolDescription GetQueryPoolDescription(QueryPool queryPool) => new(queryPool.QueryType, queryPool.QueryCount);


        /// <summary>
        ///   Gets a temporary Texture with the specified description.
        /// </summary>
        /// <param name="description">A description of the needed characteristics of the Texture.</param>
        /// <returns>A temporary Texture with the specified <paramref name="description"/>.</returns>
        public Texture GetTemporaryTexture(TextureDescription description)
        {
            // Global lock to be thread-safe
            lock (thisLock)
            {
                return GetTemporaryResource(textureCache, description, createTextureDelegate, GetTextureDescription, PixelFormat.None);
            }
        }

        /// <summary>
        ///   Gets a temporary Buffer with the specified description.
        /// </summary>
        /// <param name="description">A description of the needed characteristics of the Buffer.</param>
        /// <param name="viewFormat">
        ///   The data format for the View that will be seen by Shaders.
        ///   Specify <see cref="PixelFormat.None"/> to use the default format of the Buffer.
        /// </param>
        /// <returns>A temporary Buffer with the specified <paramref name="description"/>.</returns>
        public Buffer GetTemporaryBuffer(BufferDescription description, PixelFormat viewFormat = PixelFormat.None)
        {
            // Global lock to be thread-safe
            lock (thisLock)
            {
                return GetTemporaryResource(bufferCache, description, createBufferDelegate, GetBufferDescription, viewFormat);
            }
        }

        /// <summary>
        ///   Gets a Query Pool for a specific number of GPU Queries of a given type.
        /// </summary>
        /// <param name="queryType">The type of the Queries.</param>
        /// <param name="queryCount">The needed number of Queries.</param>
        /// <returns>A temporary Query Pool with the specified types and count.</returns>
        public QueryPool GetQueryPool(QueryType queryType, int queryCount)
        {
            // Global lock to be thread-safe
            lock (thisLock)
            {
                return GetTemporaryResource(queryPoolCache, new QueryPoolDescription(queryType, queryCount), createQueryPoolDelegate, GetQueryPoolDescription, PixelFormat.None);
            }
        }


        /// <summary>
        ///   Adds a reference to a Graphics Resource tracked by this allocator.
        /// </summary>
        /// <param name="resource">The Graphics Resource. It's internal reference count will be increased.</param>
        /// <exception cref="ArgumentException">
        ///   Thrown if <paramref name="resource"/> was not allocated by this allocator.
        /// </exception>
        public void AddReference(GraphicsResource resource)
        {
            // Global lock to be thread-safe
            lock (thisLock)
            {
                UpdateReference(resource, +1);
            }
        }

        /// <summary>
        ///   Removes a reference to a Graphics Resource tracked by this allocator.
        /// </summary>
        /// <param name="resource">The Graphics Resource. It's internal reference count will be decreased.</param>
        /// <exception cref="ArgumentException">
        ///   Thrown if <paramref name="resource"/> was not allocated by this allocator.
        /// </exception>
        public void ReleaseReference(GraphicsResourceBase resource)
        {
            // Global lock to be thread-safe
            lock (thisLock)
            {
                UpdateReference(resource, -1);
            }
        }


        /// <summary>
        ///   Creates a Texture.
        /// </summary>
        /// <param name="description">A description of the needed characteristics of the Texture.</param>
        /// <param name="viewFormat">
        ///   The pixel format for the View that will be seen by Shaders.
        ///   Specify <see cref="PixelFormat.None"/> to use the default format of the Texture.
        /// </param>
        /// <returns>A Texture with the specified <paramref name="description"/>.</returns>
        protected virtual Texture CreateTexture(TextureDescription description, PixelFormat viewFormat)
        {
            return Texture.New(GraphicsDevice, description);
        }

        /// <summary>
        ///   Creates a Buffer.
        /// </summary>
        /// <param name="description">A description of the needed characteristics of the Buffer.</param>
        /// <param name="viewFormat">
        ///   The data format for the View that will be seen by Shaders.
        ///   Specify <see cref="PixelFormat.None"/> to use the default format of the Buffer.
        /// </param>
        /// <returns>A Buffer with the specified <paramref name="description"/>.</returns>
        protected virtual Buffer CreateBuffer(BufferDescription description, PixelFormat viewFormat)
        {
            return Buffer.New(GraphicsDevice, description, viewFormat);
        }

        /// <summary>
        ///   Creates a Query Pool for a specific number of GPU Queries of a given type.
        /// </summary>
        /// <param name="description">
        ///   A description of the needed characteristics of the Query Pool, like the number of GPU Queries and their type.
        /// </param>
        /// <param name="viewFormat">Ignored for Query Pools, as they do not have a format.</param>
        /// <returns>A Query Pool with the specified types and count.</returns>
        protected virtual QueryPool CreateQueryPool(QueryPoolDescription description, PixelFormat viewFormat)
        {
            return QueryPool.New(GraphicsDevice, description.QueryType, description.QueryCount);
        }


        /// <summary>
        ///   Disposes the current Graphics Resource allocator by disposing all the associated resources and clearing all the caches.
        /// </summary>
        protected override void Destroy()
        {
            lock (thisLock)
            {
                DisposeCache(textureCache);
                DisposeCache(bufferCache);
                DisposeCache(queryPoolCache);
            }

            base.Destroy();

            /// <summary>
            ///   Disposes and removes all the resources in a resource cache.
            /// </summary>
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


        /// <summary>
        ///   Gets a temporary Graphics Resource with the specified description.
        /// </summary>
        /// <typeparam name="TResource">The type of the Graphics Resource.</typeparam>
        /// <typeparam name="TDescription">The type of an object that describes the characteristics of the Graphics Resource.</typeparam>
        /// <param name="cache">The cache of allocated Graphics Resources of the intended type.</param>
        /// <param name="description">A description of the needed characteristics of the Graphics Resource.</param>
        /// <param name="createResource">
        ///   A delegate that creates a Graphics Resource given a description and a data format for SRVs.
        ///   See <see cref="CreateTexture"/>, <see cref="CreateBuffer"/>, or <see cref="CreateQueryPool"/> for examples.
        /// </param>
        /// <param name="getDescription">
        ///   A delegate that retrieves the actual description of a Graphics Resource.
        ///   See <see cref="GetTextureDescription"/>, <see cref="GetBufferDescription"/>, or <see cref="GetQueryPoolDescription"/> for examples.
        /// </param>
        /// <param name="viewFormat">
        ///   The data format for the View that will be seen by Shaders.
        ///   Specify <see cref="PixelFormat.None"/> to use the default format of the Graphics Resource.
        ///   Some Graphics Resources may not have a View Format, like Query Pools.
        /// </param>
        /// <returns>A temporary Graphics Resource with the specified <paramref name="description"/>.</returns>
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

            // If no resources are available, then create a new one
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

            //
            // Gets or creates a cache for allocated Graphics Resources matching the specified description.
            //
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
        ///   Updates the reference count for a specified Graphics Resource.
        /// </summary>
        /// <param name="resource">
        ///   The Graphics Resource whose reference count is to be updated.
        ///   Must be a <see cref="Texture"/>, <see cref="Buffer"/>, or <see cref="QueryPool"/>.
        /// </param>
        /// <param name="referenceDelta">
        ///   The change in the reference count. Positive values increase the count, while negative values decrease it.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   Thrown if <paramref name="resource"/> is not a <see cref="Texture"/>, <see cref="Buffer"/>, or <see cref="QueryPool"/>,
        ///   or if the Graphics Resource was not allocated by this allocator.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="referenceDelta"/> is invalid. It cannot make the reference count of the <paramref name="resource"/> negative.
        /// </exception>
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

                _ => throw new ArgumentException("Unsupported Graphics Resource. Only Textures, Buffers and QueryPools are supported", nameof(resource))
            };

            if (!resourceFound)
                throw new ArgumentException("The Graphics Resource was not allocated by this allocator", nameof(resource));
        }

        /// <summary>
        ///   Updates the reference count for a specified Graphics Resource.
        /// </summary>
        /// <typeparam name="TResource">The type of the Graphics Resource.</typeparam>
        /// <typeparam name="TDescription">The type of an object that describes the characteristics of the Graphics Resource.</typeparam>
        /// <param name="cache">The cache of allocated Graphics Resources of the intended type.</param>
        /// <param name="resource">The Graphics Resource whose reference count is to be updated.</param>
        /// <param name="getDescription">
        ///   A delegate that retrieves the actual description of a Graphics Resource.
        ///   See <see cref="GetTextureDescription"/>, <see cref="GetBufferDescription"/>, or <see cref="GetQueryPoolDescription"/> for examples.
        /// </param>
        /// <param name="referenceDelta">
        ///   The change in the reference count. Positive values increase the count, while negative values decrease it.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the reference count for <paramref name="resource"/> was updated; <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="referenceDelta"/> is invalid. It cannot make the reference count of the <paramref name="resource"/> negative.
        /// </exception>
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

            // Check if the resource is known by this allocator
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
            // Resource not found in the cache
            return false;
        }

        /// <summary>
        ///   Updates the reference count for a specified Graphics Resource.
        /// </summary>
        /// <param name="resourceLink">The Graphics Resource whose reference count is to be updated.</param>
        /// <param name="referenceDelta">
        ///   The change in the reference count. Positive values increase the count, while negative values decrease it.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="referenceDelta"/> is invalid. It cannot make the reference count of the <paramref name="resource"/> negative.
        /// </exception>
        private void UpdateCounter(GraphicsResourceLink resourceLink, int referenceDelta)
        {
            if ((resourceLink.ReferenceCount + referenceDelta) < 0)
            {
                throw new ArgumentException("Invalid delta on reference count. It must be non-negative after updating. " +
                    $"Current reference count: [{resourceLink.ReferenceCount}] Delta: [{referenceDelta}]");
            }

            resourceLink.ReferenceCount += referenceDelta;
            resourceLink.AccessTotalCount++;
            resourceLink.AccessCountSinceLastRecycle++;
            resourceLink.LastAccessTime = DateTime.Now;

            // If no alive references are left, we can tag the resource for discarding on next Map
            if (resourceLink.ReferenceCount == 0)
                GraphicsDevice.TagResourceAsNotAlive(resourceLink);
        }
    }
}
