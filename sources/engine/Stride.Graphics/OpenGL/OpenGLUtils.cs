// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Stride.Graphics.OpenGL
{
    /// <summary>
    /// Converts between feature level and opengl versions
    /// </summary>
    internal static class OpenGLUtils
    {
#if STRIDE_GRAPHICS_API_OPENGLES
        public static IEnumerable<int> GetGLVersions(GraphicsProfile[] graphicsProfiles)
        {
            yield return 3;
        }

        public static void GetGLVersion(GraphicsProfile graphicsProfile, out int version)
        {
            switch (graphicsProfile)
            {
                case GraphicsProfile.Level_9_1:
                case GraphicsProfile.Level_9_2:
                case GraphicsProfile.Level_9_3:
                case GraphicsProfile.Level_10_0:
                case GraphicsProfile.Level_10_1:
                    version = 300;
                    return;
                case GraphicsProfile.Level_11_0:
                case GraphicsProfile.Level_11_1:
                case GraphicsProfile.Level_11_2:
                    version = 310;
                    return;
                default:
                    throw new ArgumentOutOfRangeException("graphicsProfile");
            }
        }

        public static GraphicsProfile GetFeatureLevel(int version)
        {
            if (version >= 310)
                return GraphicsProfile.Level_11_0; // missing tessellation and geometry shaders
            return GraphicsProfile.Level_10_0;
        }
#else
        public static void GetGLVersion(GraphicsProfile graphicsProfile, out int version)
        {
            switch (graphicsProfile)
            {
                case GraphicsProfile.Level_9_1:
                case GraphicsProfile.Level_9_2:
                case GraphicsProfile.Level_9_3:
                    version = 330;
                    return;
                case GraphicsProfile.Level_10_0:
                case GraphicsProfile.Level_10_1:
                    version = 410;
                    return;
                case GraphicsProfile.Level_11_0:
                case GraphicsProfile.Level_11_1:
                case GraphicsProfile.Level_11_2:
                    version = 440;
                    return;
                default:
                    throw new ArgumentOutOfRangeException("graphicsProfile");
            }
        }

        public static GraphicsProfile GetFeatureLevel(int version)
        {
            if (version >= 400)
            {
                if (version >= 440)
                    return GraphicsProfile.Level_11_0;
                if (version >= 410)
                    return GraphicsProfile.Level_10_0;
            }
            return GraphicsProfile.Level_9_1;
        }
#endif
#if STRIDE_PLATFORM_ANDROID
        public static GLVersion GetGLVersion(GraphicsProfile graphicsProfile)
        {
            switch (graphicsProfile)
            {
                case GraphicsProfile.Level_9_1:
                case GraphicsProfile.Level_9_2:
                case GraphicsProfile.Level_9_3:
                    return GLVersion.ES2;
                case GraphicsProfile.Level_10_0:
                case GraphicsProfile.Level_10_1:
                case GraphicsProfile.Level_11_0:
                case GraphicsProfile.Level_11_1:
                case GraphicsProfile.Level_11_2:
                    return GLVersion.ES3;
                default:
                    throw new ArgumentOutOfRangeException("graphicsProfile");
            }
        }
#endif
    }
}
#endif
