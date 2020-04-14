// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenTK.Graphics;
#if XENKO_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Xenko.Graphics.OpenGL
{
    /// <summary>
    /// Converts between feature level and opengl versions
    /// </summary>
    internal static class OpenGLUtils
    {
#if XENKO_GRAPHICS_API_OPENGLES
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
#if XENKO_PLATFORM_ANDROID
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

        private static readonly Regex MatchOpenGLVersion = new Regex(@"OpenGL\s+ES\s+([0-9\.]+)");

        /// <summary>
        /// Gets current GL version.
        /// </summary>
        /// <param name="version">OpenGL version encoded as major * 100 + minor * 10.</param>
        /// <returns></returns>
        public static bool GetCurrentGLVersion(out int version)
        {
            version = 0;

#if XENKO_GRAPHICS_API_OPENGLES
            var versionVendorText = GL.GetString(StringName.Version);
            var match = MatchOpenGLVersion.Match(versionVendorText);
            if (!match.Success)
                return false;

            var versionText = match.Groups[1].Value;
            var dotIndex = versionText.IndexOf(".");

            int versionMajor = 0;
            int versionMinor = 0;
            if (!int.TryParse(dotIndex != -1 ? versionText.Substring(0, dotIndex) : versionText, out versionMajor))
            {
                return false;
            }

            if (dotIndex == -1)
            {
                version = versionMajor * 100;
                return true;
            }
            if (!int.TryParse(versionText.Substring(dotIndex + 1), out versionMinor))
                return false;

            version = versionMajor * 100 + versionMinor * 10;
            return true;
#else
            int versionMajor = 0;
            int versionMinor = 0;

            GL.GetInteger(GetPName.MajorVersion, out versionMajor);
            GL.GetInteger(GetPName.MinorVersion, out versionMinor);

            version = versionMajor * 100 + versionMinor * 10;
            return true;
#endif
        }
    }
}
#endif
