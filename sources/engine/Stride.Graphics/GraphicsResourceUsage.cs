// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Identifies expected resource use during rendering. The usage directly reflects whether a resource is accessible by the CPU and/or the GPU.
    /// </summary>
    [DataContract]
    public enum GraphicsResourceUsage
    {
        /// <summary>
        /// A resource that requires read and write access by the GPU. This is likely to be the most common usage choice.
        /// </summary>
        Default = unchecked((int)0),

        /// <summary>
        /// A resource that can only be read by the GPU. It cannot be written by the GPU, and cannot be accessed at all by the CPU. This type of resource must be initialized when it is created, since it cannot be changed after creation.
        /// </summary>
        Immutable = unchecked((int)1),

        /// <summary>
        /// A resource that is accessible by both the GPU (read only) and the CPU (write only). A dynamic resource is a good choice for a resource that will be updated by the CPU at least once per frame. To update a dynamic resource, use a <strong>Map</strong> method.
        /// </summary>
        Dynamic = unchecked((int)2),

        /// <summary>
        /// A resource that supports data transfer (copy) from the GPU to the CPU.
        /// </summary>
        Staging = unchecked((int)3),
    }
}
