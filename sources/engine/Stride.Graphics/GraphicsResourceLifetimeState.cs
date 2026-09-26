// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics
{
    /// <summary>
    ///   Describes the lifetime state of a graphics resource.
    /// </summary>
    public enum GraphicsResourceLifetimeState
    {
        /// <summary>
        ///   The resource is active and available for use.
        /// </summary>
        Active = 0,

        /// <summary>
        ///   The native resource has been released, by its own disposal or with the graphics device.
        /// </summary>
        Destroyed = 1,
    }
}
