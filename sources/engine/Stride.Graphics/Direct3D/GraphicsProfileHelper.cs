// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D

using Silk.NET.Core.Native;

using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics;

internal static class GraphicsProfileHelper
{
    public static D3DFeatureLevel[] ToFeatureLevel(this GraphicsProfile[] profiles)
    {
        /// <summary>
        ///   Converts an array of <see cref="GraphicsProfile"/>s to an array of corresponding <see cref="D3DFeatureLevel"/>s.
        /// </summary>
        /// <param name="profiles">An array of <see cref="GraphicsProfile"/>s to convert.</param>
        /// <returns>An array of Direct3D <see cref="D3DFeatureLevel"/>s.</returns>
        if (profiles is null or [])
            return null;

        /// <summary>
        ///   Converts a <see cref="GraphicsProfile"/> to its corresponding <see cref="D3DFeatureLevel"/>.
        /// </summary>
        /// <param name="profile">A <see cref="GraphicsProfile"/> to convert.</param>
        /// <returns>A Direct3D <see cref="D3DFeatureLevel"/>.</returns>
        /// <summary>
        ///   Converts a <see cref="D3DFeatureLevel"/> to its corresponding <see cref="GraphicsProfile"/>.
        /// </summary>
        /// <param name="level">A <see cref="D3DFeatureLevel"/> to convert.</param>
        /// <returns>A Stride <see cref="GraphicsProfile"/>.</returns>
        var featureLevels = profiles.AsReadOnlySpan<GraphicsProfile, D3DFeatureLevel>().ToArray();
        return featureLevels;
    }

    public static D3DFeatureLevel ToFeatureLevel(this GraphicsProfile profile) => (D3DFeatureLevel) profile;

    public static GraphicsProfile FromFeatureLevel(D3DFeatureLevel level) => (GraphicsProfile) level;
}

#endif
