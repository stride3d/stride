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
        ///   The resource is in a reduced state (partially or completely destroyed) because application is in the background.
        ///   Context should still be alive.
        /// </summary>
        /// <remarks>
        ///   This is useful for freeing dynamic resources such as Frame Buffers / Render Targets, that could be easily restored when application is resumed.
        /// </remarks>
        Paused = 1,

        /// <summary>
        ///   The resource has been destroyed due to the graphics device being destroyed.
        ///   It will need to be recreated or reloaded when rendering resumes.
        /// </summary>
        Destroyed = 2,

        // Not sure if this one will be useful yet (in case of async reloading?)
        // Reloading = 3,
    }
}
