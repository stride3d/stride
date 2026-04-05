// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System.Collections.Generic;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Stride.Graphics;

/// <summary>
///   A thread-safe queue of COM resources awaiting deferred release once a GPU fence has been reached.
/// </summary>
internal struct DeferredReleaseQueue
{
    private readonly Queue<(ulong FenceValue, object Resource)> queue;

    public DeferredReleaseQueue()
    {
        queue = new();
    }

    /// <summary>
    ///   Enqueues a resource for deferred release after the specified fence value is reached.
    /// </summary>
    public void Enqueue(ulong fenceValue, object resource)
    {
        lock (queue)
            queue.Enqueue((fenceValue, resource));
    }

    /// <summary>
    ///   Releases all resources whose associated fence value has been reached.
    /// </summary>
    public void ReleaseCompleted(GraphicsDevice.FenceHelper fence)
    {
        lock (queue)
        {
            while (queue.Count > 0 && fence.IsFenceCompleteInternal(queue.Peek().FenceValue))
            {
                ReleaseResource(queue.Dequeue().Resource);
            }
        }
    }

    /// <summary>
    ///   Releases all remaining resources regardless of fence state. Used during device shutdown.
    /// </summary>
    public void ReleaseAll()
    {
        lock (queue)
        {
            while (queue.Count > 0)
            {
                ReleaseResource(queue.Dequeue().Resource);
            }
        }
    }

    private static void ReleaseResource(object resource)
    {
        if (resource is ComPtr<ID3D12Resource> comPtr)
        {
            comPtr.Release();
        }
        else if (resource is GraphicsResourceLink resourceLink)
        {
            resourceLink.ReferenceCount--;
        }
    }
}

#endif
