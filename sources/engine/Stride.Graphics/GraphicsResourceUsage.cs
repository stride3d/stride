// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   Identifies the intended use of a resource during rendering. The usage directly reflects whether a resource
    ///   is accessible by the CPU and/or the GPU.
    /// </summary>
    [DataContract]
    public enum GraphicsResourceUsage
    {
        /// <summary>
        ///   A resource that requires read and write access by the GPU. This is likely to be the most common usage choice.
        /// </summary>
        Default = 0,

        /// <summary>
        ///   A resource that can only be read by the GPU.
        ///   It cannot be written by the GPU, and cannot be accessed at all by the CPU.
        /// </summary>
        /// <remarks>
        ///   This type of resource <b>must be initialized</b> when it is created, since it cannot be changed after creation.
        /// </remarks>
        Immutable = 1,

        /// <summary>
        ///   A resource that is accessible by both the GPU (read-only) and the CPU (write-only).
        /// </summary>
        /// <remarks>
        ///   A dynamic resource is a good choice for a resource that will be updated by the CPU at least
        ///   once per frame.
        ///   <para/>
        ///   To update a dynamic resource, use a <b>Map</b> method.
        /// </remarks>
        Dynamic = 2,

        /// <summary>
        ///   A resource that supports data transfer (copy) from the GPU to the CPU.
        /// </summary>
        Staging = 3
    }
}
