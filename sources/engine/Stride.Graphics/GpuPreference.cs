// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics
{
    /// <summary>
    ///   Which GPU is enumerated first, and therefore becomes <see cref="GraphicsAdapterFactory.DefaultAdapter"/>,
    ///   on machines that have more than one. See <see cref="GraphicsAdapterFactory.GpuPreference"/>.
    /// </summary>
    public enum GpuPreference
    {
        /// <summary>
        ///   The fastest GPU first (the discrete GPU of a hybrid laptop). This is the default.
        /// </summary>
        HighPerformance = 0,

        /// <summary>
        ///   The most power-efficient GPU first (the integrated GPU of a hybrid laptop).
        /// </summary>
        MinimumPower,

        /// <summary>
        ///   Whatever order the system reports, unaltered.
        /// </summary>
        Unspecified,
    }
}
