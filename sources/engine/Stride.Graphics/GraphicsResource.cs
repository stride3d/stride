// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics
{
    /// <summary>
    ///   Represents an abstract resource that depends on a <see cref="GraphicsDevice"/>.
    /// </summary>
    public abstract partial class GraphicsResource : GraphicsResourceBase
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsResource"/> class.
        /// </summary>
        protected GraphicsResource() { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsResource"/> class attached to a graphics device.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> this resource belongs to.</param>
        protected GraphicsResource(GraphicsDevice device) : base(device) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsResource"/> class attached to a graphics device.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> this resource belongs to.</param>
        /// <param name="name">
        ///   A string to use as a name for identifying the resource. Useful when debugging.
        ///   Specify <see langword="null"/> to use the type's name instead.
        /// </param>
        protected GraphicsResource(GraphicsDevice device, string? name) : base(device, name) { }
    }
}
