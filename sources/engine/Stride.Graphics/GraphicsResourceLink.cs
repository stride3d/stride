// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

public sealed class GraphicsResourceLink
{
    /// <summary>
    /// A resource allocated by <see cref="GraphicsResourceAllocator"/> providing allocation informations.
    /// </summary>
    internal GraphicsResourceLink(GraphicsResourceBase resource)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceLink"/> class.
        /// </summary>
        /// <param name="resource">The graphics resource.</param>
        /// <exception cref="System.ArgumentNullException">resource</exception>
        /// <summary>
        /// The graphics resource.
        /// </summary>
        /// <summary>
        /// Gets the last time this resource was accessed.
        /// </summary>
        /// <value>The last access time.</value>
        /// <summary>
        /// Gets the total count of access to this resource (include Increment and Decrement)
        /// </summary>
        /// <value>The access total count.</value>
        /// <summary>
        /// Gets the access count since last recycle policy was run.
        /// </summary>
        /// <value>The access count since last recycle.</value>
        /// <summary>
        /// The number of active reference to this resource.
        /// </summary>
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }


    public GraphicsResourceBase Resource { get; }

    public DateTime LastAccessTime { get; internal set; }

    public long AccessTotalCount { get; internal set; }

    public long AccessCountSinceLastRecycle { get; internal set; }

    public int ReferenceCount { get; internal set; }
}
