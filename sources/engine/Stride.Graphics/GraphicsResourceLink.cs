// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   An object linking a Graphics Resource allocated by a <see cref="GraphicsResourceAllocator"/>
///   and providing allocation information.
/// </summary>
public sealed class GraphicsResourceLink
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsResourceLink"/> class.
    /// </summary>
    /// <param name="resource">The allocated Graphics Resource.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
    internal GraphicsResourceLink(GraphicsResourceBase resource)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }


    /// <summary>
    ///   Gets the allocated Graphics Resource.
    /// </summary>
    public GraphicsResourceBase Resource { get; }

    /// <summary>
    ///   Gets the last time the Graphics Resource was accessed.
    /// </summary>
    public DateTime LastAccessTime { get; internal set; }

    /// <summary>
    ///   Gets the total number of times the Graphics Resource has been accessed (including Increment and Decrement).
    /// </summary>
    public long AccessTotalCount { get; internal set; }

    /// <summary>
    ///   Gets the number of times the Graphics Resource has been accessed since the last recycle policy was run.
    /// </summary>
    public long AccessCountSinceLastRecycle { get; internal set; }

    /// <summary>
    ///   Gets the number of active references to the Graphics Resource.
    /// </summary>
    public int ReferenceCount { get; internal set; }
}
