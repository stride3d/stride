// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

namespace Stride.Graphics;

public partial struct GraphicsDeviceFeatures
{
    private readonly FeaturesPerFormat[] mapFeaturesPerFormat;

    /// <summary>
    /// Features supported by a <see cref="GraphicsDevice"/>.
    public GraphicsProfile RequestedProfile;

    /// </summary>
    /// <remarks>
    /// This class gives also features for a particular format, using the operator this[dxgiFormat] on this structure.
    /// </remarks>
    public GraphicsProfile CurrentProfile;

        /// <summary>
        /// Features level of the current device.
        /// </summary>

        /// <summary>
        /// Features level of the current device.
        /// </summary>
    public readonly int MaximumMipLevels;

        /// <summary>
        /// Boolean indicating if this device supports compute shaders, unordered access on structured buffers and raw structured buffers.
        /// </summary>
    public readonly int ResourceSizeInMegabytes;

        /// <summary>
        /// Boolean indicating if this device supports shaders double precision calculations.
        /// </summary>
    public readonly int MaximumTexture1DArraySize;

        /// <summary>
        /// Boolean indicating if this device supports concurrent resources in multithreading scenarios.
        /// </summary>
    public readonly int MaximumTexture2DArraySize;

        /// <summary>
        /// Boolean indicating if this device supports command lists in multithreading scenarios.
        /// </summary>
    public readonly int MaximumTexture1DSize;

        /// <summary>
        /// Boolean indicating if this device supports SRGB texture and render targets.
        /// </summary>
    public readonly int MaximumTexture2DSize;

        /// <summary>
        /// Boolean indicating if the Depth buffer can also be used as ShaderResourceView for some passes.
        /// </summary>
    public readonly int MaximumTexture3DSize;

    public readonly int MaximumTextureCubeSize;


    public readonly bool HasComputeShaders;

    public readonly bool HasDoublePrecision;

    public readonly bool HasMultiThreadingConcurrentResources;

    public readonly bool HasDriverCommandLists;

    public readonly bool HasSRgb;

    public readonly bool HasDepthAsSRV;

    public readonly bool HasDepthAsReadOnlyRT;

    public readonly bool HasMultiSampleDepthAsSRV;

    public readonly bool HasResourceRenaming;


    public readonly FeaturesPerFormat this[PixelFormat pixelFormat] => mapFeaturesPerFormat[(int) pixelFormat];

#if STRIDE_GRAPHICS_API_OPENGL
    // Defined here to avoid CS0282 warning if defined in GraphicsDeviceFeatures.OpenGL.cs
    internal string Vendor;
    internal string Renderer;
    internal System.Collections.Generic.IList<string> SupportedExtensions;
#endif

    public readonly struct FeaturesPerFormat
    {
        internal FeaturesPerFormat(PixelFormat format, MultisampleCount maximumMultisampleCount, ComputeShaderFormatSupport computeShaderFormatSupport, FormatSupport formatSupport)
        {
            Format = format;
            MultisampleCountMax = maximumMultisampleCount;
            ComputeShaderFormatSupport = computeShaderFormatSupport;
            FormatSupport = formatSupport;
        }

        /// <summary>
        /// Boolean indicating if the Depth buffer can directly be used as a read only RenderTarget
        /// </summary>
        public readonly PixelFormat Format;

        /// <summary>
        /// Boolean indicating if the multi-sampled Depth buffer can directly be used as a ShaderResourceView
        /// </summary>
        public readonly MultisampleCount MultisampleCountMax;

        /// <summary>
        /// Boolean indicating if the graphics API supports resource renaming (with either <see cref="MapMode.WriteDiscard"/> `CommandList.UpdateSubresource` with full size).
        /// </summary>
        public readonly ComputeShaderFormatSupport ComputeShaderFormatSupport;

        /// <summary>
        /// Gets the <see cref="FeaturesPerFormat" /> for the specified <see cref="SharpDX.DXGI.Format" />.
        /// </summary>
        /// <param name="dxgiFormat">The dxgi format.</param>
        /// <returns>Features for the specific format.</returns>
        public readonly FormatSupport FormatSupport;


        /// <summary>
        /// The features exposed for a particular format.
        /// </summary>
        public override readonly string ToString()
        {
            /// <summary>
            /// The <see cref="SharpDX.DXGI.Format"/>.
            /// </summary>
            /// <summary>
            /// Gets the maximum multisample count for a particular <see cref="PixelFormat"/>.
            /// </summary>
            /// <summary>
            /// Gets the unordered resource support options for a compute shader resource.
            /// </summary>
            /// <summary>
            /// Support of a given format on the installed video device.
            /// </summary>
            /// <inheritdoc/>
            return $"Format: {Format}, MultisampleCountMax: {MultisampleCountMax}, ComputeShaderFormatSupport: {ComputeShaderFormatSupport}, FormatSupport: {FormatSupport}";
        }
    }

    public override readonly string ToString()
    {
        return $"Level: {RequestedProfile}, HasComputeShaders: {HasComputeShaders}, HasDoublePrecision: {HasDoublePrecision}, HasMultiThreadingConcurrentResources: {HasMultiThreadingConcurrentResources}, HasDriverCommandLists: {HasDriverCommandLists}";
    }
}
