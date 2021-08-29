// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D 
using Silk.NET.Core.Native;

namespace Stride.Graphics
{
    internal static class GraphicsProfileHelper
    {
        /// <summary>
        /// Returns a GraphicsProfile from a FeatureLevel.
        /// </summary>
        /// <returns>associated GraphicsProfile</returns>
        public static D3DFeatureLevel[] ToFeatureLevel(this GraphicsProfile[] profiles)
        {
            if (profiles == null)
            {
                return null;
            }

            var levels = new D3DFeatureLevel[profiles.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = (D3DFeatureLevel)profiles[i];
            }
            return levels;
        }
        
        /// <summary>
        /// Returns a GraphicsProfile from a FeatureLevel.
        /// </summary>
        /// <returns>associated GraphicsProfile</returns>
        public static D3DFeatureLevel ToFeatureLevel(this GraphicsProfile profile)
        {
            return (D3DFeatureLevel)profile;
        }

        /// <summary>
        /// Returns a GraphicsProfile from a FeatureLevel.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>associated GraphicsProfile</returns>
        public static GraphicsProfile FromFeatureLevel(D3DFeatureLevel level)
        {
            return (GraphicsProfile)level;
        }
    }
} 
#endif 
