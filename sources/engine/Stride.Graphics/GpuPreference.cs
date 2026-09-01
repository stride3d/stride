// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics
{
    /// <summary>
    ///   Which GPU the adapter enumeration puts first on machines that have more than one,
    ///   and with it which one <see cref="GraphicsAdapterFactory.DefaultAdapter"/> names.
    ///   See <see cref="GraphicsAdapterFactory.GpuPreference"/>.
    /// </summary>
    public enum GpuPreference
    {
        /// <summary>
        ///   The fastest GPU first. On a hybrid laptop this is the discrete GPU, even though
        ///   every display is usually wired to the integrated one. This is the default.
        /// </summary>
        HighPerformance = 0,

        /// <summary>
        ///   The most power-efficient GPU first - on a hybrid laptop, the integrated one.
        /// </summary>
        MinimumPower,

        /// <summary>
        ///   Whatever order the system reports, unaltered.
        /// </summary>
        Unspecified,
    }
}
