// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
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

namespace Xenko.Graphics
{
    /// <summary>
    /// Features supported by a <see cref="GraphicsDevice"/>.
    /// </summary>
    /// <remarks>
    /// This class gives also features for a particular format, using the operator this[dxgiFormat] on this structure.
    /// </remarks>
    public partial struct GraphicsDeviceFeatures
    {
        private readonly FeaturesPerFormat[] mapFeaturesPerFormat;

        /// <summary>
        /// Features level of the current device.
        /// </summary>
        public GraphicsProfile RequestedProfile;

        /// <summary>
        /// Features level of the current device.
        /// </summary>
        public GraphicsProfile CurrentProfile;

        /// <summary>
        /// Boolean indicating if this device supports compute shaders, unordered access on structured buffers and raw structured buffers.
        /// </summary>
        public readonly bool HasComputeShaders;

        /// <summary>
        /// Boolean indicating if this device supports shaders double precision calculations.
        /// </summary>
        public readonly bool HasDoublePrecision;

        /// <summary>
        /// Boolean indicating if this device supports concurrent resources in multithreading scenarios.
        /// </summary>
        public readonly bool HasMultiThreadingConcurrentResources;

        /// <summary>
        /// Boolean indicating if this device supports command lists in multithreading scenarios.
        /// </summary>
        public readonly bool HasDriverCommandLists;

        /// <summary>
        /// Boolean indicating if this device supports SRGB texture and render targets.
        /// </summary>
        public readonly bool HasSRgb;

        /// <summary>
        /// Boolean indicating if the Depth buffer can also be used as ShaderResourceView for some passes.
        /// </summary>
        public readonly bool HasDepthAsSRV;

        /// <summary>
        /// Boolean indicating if the Depth buffer can directly be used as a read only RenderTarget
        /// </summary>
        public readonly bool HasDepthAsReadOnlyRT;

        /// <summary>
        /// Boolean indicating if the multi-sampled Depth buffer can directly be used as a ShaderResourceView
        /// </summary>
        public readonly bool HasMultisampleDepthAsSRV;

        /// <summary>
        /// Boolean indicating if the graphics API supports resource renaming (with either <see cref="MapMode.WriteDiscard"/> or <see cref="CommandList.UpdateSubresource"/> with full size).
        /// </summary>
        public readonly bool HasResourceRenaming;

        /// <summary>
        /// Gets the <see cref="FeaturesPerFormat" /> for the specified <see cref="SharpDX.DXGI.Format" />.
        /// </summary>
        /// <param name="dxgiFormat">The dxgi format.</param>
        /// <returns>Features for the specific format.</returns>
        public FeaturesPerFormat this[PixelFormat dxgiFormat]
        {
            get { return this.mapFeaturesPerFormat[(int)dxgiFormat]; }
        }

#if XENKO_GRAPHICS_API_OPENGL
        // Defined here to avoid CS0282 warning if defined in GraphicsDeviceFeatures.OpenGL.cs
        internal string Vendor;
        internal string Renderer;
        internal IList<string> SupportedExtensions;
#endif

        /// <summary>
        /// The features exposed for a particular format.
        /// </summary>
        public struct FeaturesPerFormat
        {
            //internal FeaturesPerFormat(PixelFormat format, MultisampleCount maximumMultisampleCount, ComputeShaderFormatSupport computeShaderFormatSupport, FormatSupport formatSupport)
            internal FeaturesPerFormat(PixelFormat format, MultisampleCount maximumMultisampleCount, FormatSupport formatSupport)
            {
                Format = format;
                this.MultisampleCountMax = maximumMultisampleCount;
                //ComputeShaderFormatSupport = computeShaderFormatSupport;
                FormatSupport = formatSupport;
            }

            /// <summary>
            /// The <see cref="SharpDX.DXGI.Format"/>.
            /// </summary>
            public readonly PixelFormat Format;

            /// <summary>
            /// Gets the maximum multisample count for a particular <see cref="PixelFormat"/>.
            /// </summary>
            public readonly MultisampleCount MultisampleCountMax;

            /// <summary>
            /// Gets the unordered resource support options for a compute shader resource.
            /// </summary>
            //public readonly ComputeShaderFormatSupport ComputeShaderFormatSupport;

            /// <summary>
            /// Support of a given format on the installed video device.
            /// </summary>
            public readonly FormatSupport FormatSupport;

            public override string ToString()
            {
                //return string.Format("Format: {0}, MultisampleCountMax: {1}, ComputeShaderFormatSupport: {2}, FormatSupport: {3}", Format, this.MSAALevelMax, ComputeShaderFormatSupport, FormatSupport);
                return string.Format("Format: {0}, MultisampleCountMax: {1}, FormatSupport: {2}", Format, this.MultisampleCountMax, FormatSupport);
            }
        }

        public override string ToString()
        {
            return string.Format("Level: {0}, HasComputeShaders: {1}, HasDoublePrecision: {2}, HasMultiThreadingConcurrentResources: {3}, HasDriverCommandLists: {4}", RequestedProfile, HasComputeShaders, HasDoublePrecision, HasMultiThreadingConcurrentResources, this.HasDriverCommandLists);
        }
    }
}
