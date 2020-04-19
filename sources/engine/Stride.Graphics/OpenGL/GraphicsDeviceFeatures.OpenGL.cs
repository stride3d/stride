// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using Stride.Graphics.OpenGL;
using Stride.Core.Diagnostics;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#else
using OpenTK.Graphics.OpenGL;
#endif

namespace Stride.Graphics
{
    /// <summary>
    /// Features supported by a <see cref="GraphicsDevice"/>.
    /// </summary>
    /// <remarks>
    /// This class gives also features for a particular format, using the operator this[dxgiFormat] on this structure.
    /// </remarks>
    public partial struct GraphicsDeviceFeatures
    {
        private const GetPName GL_MAX_SAMPLES = (GetPName)36183;    // We define this constant here because it is not contained within OpenTK...
    
        private static Logger logger = GlobalLogger.GetLogger(nameof(GraphicsDeviceFeatures));

        private void EnumerateMSAASupportPerFormat(GraphicsDevice deviceRoot)
        {
            // Query OpenGL for the highest supported multisample count:
            int globalMaxMSAASamples;
            GL.GetInteger(GL_MAX_SAMPLES, out globalMaxMSAASamples);

            // Now select the highest engine-supported multisample mode:    // TODO: Adjust comment.
            MultisampleCount actualMultisampleCount = MultisampleCount.None;

            // Technically we could just cast "globalMaxMSAASamples" to "actualMultisampleCount",
            // but AFAIK nothing prevents an implementation from exposing things like 6x MSAA or some other uncommon mode.
            if (globalMaxMSAASamples >= 8)
            {
                // If the maximum supported MSAA mode by the OpenGL implementation is higher than the maximum supported by the engine (8xMSAA), we clamp it.
                actualMultisampleCount = MultisampleCount.X8;
            }
            else if (globalMaxMSAASamples >= 4)
            {
                // If the maximum supported MSAA mode by the OpenGL implementation is between 4 and 7 samples, we fall back to the next lowest engine-supported one (4x).
                actualMultisampleCount = MultisampleCount.X4; // 4-7 x MSAA => 4 x MSAA (next lowest)
            }
            else if (globalMaxMSAASamples == 2)
            {
                // If the maximum supported MSAA mode by the OpenGL implementation is between 2 and 3 samples, we fall back to the next lowest engine-supported one (2x).
                actualMultisampleCount = MultisampleCount.X2;
            }

            for (int i = 0; i < mapFeaturesPerFormat.Length; i++)
            {
                // TODO: This ignores the supported multisample capabilities of each render target format. But I don't know how to query this in OpenGL (assuming it's even possible at all).
                mapFeaturesPerFormat[i] = new FeaturesPerFormat((PixelFormat)i, actualMultisampleCount, FormatSupport.None);
            }
        }

        internal GraphicsDeviceFeatures(GraphicsDevice deviceRoot)
        {
            mapFeaturesPerFormat = new FeaturesPerFormat[256];

            HasSRgb = true;

            using (deviceRoot.UseOpenGLCreationContext())
            {
                Vendor = GL.GetString(StringName.Vendor);
                Renderer = GL.GetString(StringName.Renderer);
#if STRIDE_GRAPHICS_API_OPENGLES
                SupportedExtensions = GL.GetString(StringName.Extensions).Split(' ');
#else
                int numExtensions;
                GL.GetInteger(GetPName.NumExtensions, out numExtensions);
                SupportedExtensions = new string[numExtensions];
                for (int extensionIndex = 0; extensionIndex < numExtensions; ++extensionIndex)
                {
                    SupportedExtensions[extensionIndex] = GL.GetString(StringNameIndexed.Extensions, extensionIndex);
                }
#endif
            }

#if STRIDE_GRAPHICS_API_OPENGLES
            deviceRoot.HasExtTextureFormatBGRA8888 = SupportedExtensions.Contains("GL_EXT_texture_format_BGRA8888")
                                       || SupportedExtensions.Contains("GL_APPLE_texture_format_BGRA8888");
            deviceRoot.HasKhronosDebug = deviceRoot.currentVersion >= 320 || SupportedExtensions.Contains("GL_KHR_debug");
            deviceRoot.HasTimerQueries = SupportedExtensions.Contains("GL_EXT_disjoint_timer_query");

            // Either 3.2+, or 3.1+ with GL_EXT_texture_buffer
            // TODO: For now we don't have proper ES3 bindings on Android (and possibly iOS)
            deviceRoot.HasTextureBuffers = false;
            //deviceRoot.HasTextureBuffers = (deviceRoot.version >= 320)
            //                            || (deviceRoot.version >= 310 && SupportedExtensions.Contains("GL_EXT_texture_buffer"));

            // Compute shaders available in OpenGL ES 3.1
            HasComputeShaders = deviceRoot.currentVersion >= 310;
            HasDoublePrecision = false;

            HasDepthAsSRV = true;
            HasDepthAsReadOnlyRT = true;
            HasMultisampleDepthAsSRV = true;

            deviceRoot.HasDepthClamp = SupportedExtensions.Contains("GL_ARB_depth_clamp");
  
            // TODO: from 3.1: draw indirect, separate shader object
            // TODO: check tessellation & geometry shaders: GL_ANDROID_extension_pack_es31a
#else
            deviceRoot.HasDXT = SupportedExtensions.Contains("GL_EXT_texture_compression_s3tc");
            deviceRoot.HasTextureBuffers = true;
            deviceRoot.HasKhronosDebug = deviceRoot.currentVersion >= 430 || SupportedExtensions.Contains("GL_KHR_debug");
            deviceRoot.HasTimerQueries = deviceRoot.version >= 320;

            // Compute shaders available in OpenGL 4.3
            HasComputeShaders = deviceRoot.version >= 430;
            HasDoublePrecision = SupportedExtensions.Contains("GL_ARB_vertex_attrib_64bit");

            HasDepthAsSRV = true;
            HasDepthAsReadOnlyRT = true;
            HasMultisampleDepthAsSRV = true;

            deviceRoot.HasDepthClamp = true;

            // TODO: from 4.0: tessellation, draw indirect
            // TODO: from 4.1: separate shader object
#endif

            deviceRoot.HasAnisotropicFiltering = SupportedExtensions.Contains("GL_EXT_texture_filter_anisotropic");

            HasResourceRenaming = true;

            HasDriverCommandLists = false;
            HasMultiThreadingConcurrentResources = false;

            // Find shader model based on OpenGL version (might need to check extensions more carefully)
            RequestedProfile = deviceRoot.requestedGraphicsProfile;
            CurrentProfile = OpenGLUtils.GetFeatureLevel(deviceRoot.currentVersion);

            EnumerateMSAASupportPerFormat(deviceRoot);
        }
    }
}
#endif
