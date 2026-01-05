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

/// <summary>
///   Contains information about the general features supported by a <see cref="GraphicsDevice"/>, as well as
///   supported features specific to a particular pixel format or data format.
/// </summary>
/// <remarks>
///   To obtain information about the supported features for a particular format, use the <see cref="this[PixelFormat]">indexer</see>.
/// </remarks>
public partial struct GraphicsDeviceFeatures
{
    private readonly FeaturesPerFormat[] mapFeaturesPerFormat;

    /// <summary>
    ///   The requested profile when the <see cref="GraphicsDevice"/> was created.
    /// </summary>
    /// <seealso cref="GraphicsProfile"/>
    public readonly GraphicsProfile RequestedProfile;

    /// <summary>
    ///   The current profile of the current <see cref="GraphicsDevice"/>, which determines its supported features.
    /// </summary>
    /// <remarks>
    ///   This may differ from <see cref="RequestedProfile"/> if the <see cref="GraphicsDevice"/> could not be created
    ///   with that requested profile. This one represents the closest supported profile.
    /// </remarks>
    /// <seealso cref="GraphicsProfile"/>
    public readonly GraphicsProfile CurrentProfile;


    /// <summary>
    ///   The maximum number of miplevels a Texture can have.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumMipLevels;

    /// <summary>
    ///   The maximum size of a resource, in megabytes.
    /// </summary>
    /// <seealso cref="GraphicsResource"/>
    public readonly int ResourceSizeInMegabytes;

    /// <summary>
    ///   The maximum number of slices/array elements for a one-dimensional (1D) Texture Array.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumTexture1DArraySize;

    /// <summary>
    ///   The maximum number of slices/array elements for a two-dimensional (2D) Texture Array.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumTexture2DArraySize;

    /// <summary>
    ///   The maximum size in texels for a one-dimensional (1D) Texture.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumTexture1DSize;

    /// <summary>
    ///   The maximum size (width or height) in texels for a two-dimensional (2D) Texture.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumTexture2DSize;

    /// <summary>
    ///   The maximum size (width, height, or depth) in texels for a three-dimensional (3D) Texture.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumTexture3DSize;

    /// <summary>
    ///   The maximum size (width or height) in texels for a Texture Cube.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly int MaximumTextureCubeSize;


    /// <summary>
    ///   A value indicating if the <see cref="GraphicsDevice"/> supports Compute Shaders, unordered access on Structured Buffers,
    ///   and Raw Structured Buffers.
    /// </summary>
    /// <seealso cref="Buffer.Structured"/>
    /// <seealso cref="Buffer.Raw"/>
    public readonly bool HasComputeShaders;

    /// <summary>
    ///   A value indicating if the <see cref="GraphicsDevice"/> supports double precision operations in shaders.
    /// </summary>
    public readonly bool HasDoublePrecision;

    /// <summary>
    ///   A value indicating if the <see cref="GraphicsDevice"/> supports concurrent Resources in multi-threading scenarios.
    /// </summary>
    public readonly bool HasMultiThreadingConcurrentResources;

    /// <summary>
    ///   A value indicating if the <see cref="GraphicsDevice"/> supports Command Lists in multi-threading scenarios.
    /// </summary>
    /// <seealso cref="CommandList"/>
    public readonly bool HasDriverCommandLists;

    /// <summary>
    ///   A value indicating if the <see cref="GraphicsDevice"/> supports sRGB Textures and Render Targets.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly bool HasSRgb;

    /// <summary>
    ///   A value indicating if the Depth Buffer can also be used as a Shader Resource View and bound for shader passes.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly bool HasDepthAsSRV;

    /// <summary>
    ///   A value indicating if the Depth Buffer can directly be used as a read-only Render Target.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly bool HasDepthAsReadOnlyRT;

    /// <summary>
    ///   A value indicating if a multi-sampled Depth Buffer can directly be used as a Shader Resource View.
    /// </summary>
    /// <seealso cref="Texture"/>
    public readonly bool HasMultiSampleDepthAsSRV;

    /// <summary>
    ///   A value indicating if the graphics API supports resource renaming
    ///   (with either <see cref="MapMode.WriteDiscard"/> or <see cref="CommandList.UpdateSubResource"/> with full size).
    /// </summary>
    public readonly bool HasResourceRenaming;


    /// <summary>
    ///   Queries the features the <see cref="GraphicsDevice"/> supports for the specified <see cref="PixelFormat"/>.
    /// </summary>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <returns>
    ///   A <see cref="FeaturesPerFormat"/> structure indicating the features supported for <paramref name="pixelFormat"/>.
    /// </returns>
    public readonly FeaturesPerFormat this[PixelFormat pixelFormat] => mapFeaturesPerFormat[(int) pixelFormat];

#if STRIDE_GRAPHICS_API_OPENGL
    // Defined here to avoid CS0282 warning if defined in GraphicsDeviceFeatures.OpenGL.cs
    internal string Vendor;
    internal string Renderer;
    internal System.Collections.Generic.IList<string> SupportedExtensions;
#endif

    /// <summary>
    ///   Contains information about the features a <see cref="GraphicsDevice"/> supports for a particular <see cref="PixelFormat"/>.
    /// </summary>
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
        ///   The pixel format.
        /// </summary>
        public readonly PixelFormat Format;

        /// <summary>
        ///   The maximum sample count when multisampling for a particular <see cref="Format"/>.
        /// </summary>
        public readonly MultisampleCount MultisampleCountMax;

        /// <summary>
        ///   The unordered resource support options for a Compute Shader resource using the <see cref="Format"/>.
        /// </summary>
        public readonly ComputeShaderFormatSupport ComputeShaderFormatSupport;

        /// <summary>
        ///   The support flags for a particular <see cref="Format"/>.
        /// </summary>
        public readonly FormatSupport FormatSupport;


        /// <inheritdoc/>
        public override readonly string ToString()
        {
            return $"Format: {Format}, MultisampleCountMax: {MultisampleCountMax}, ComputeShaderFormatSupport: {ComputeShaderFormatSupport}, FormatSupport: {FormatSupport}";
        }
    }

    public override readonly string ToString()
    {
        return $"Level: {RequestedProfile}, HasComputeShaders: {HasComputeShaders}, HasDoublePrecision: {HasDoublePrecision}, HasMultiThreadingConcurrentResources: {HasMultiThreadingConcurrentResources}, HasDriverCommandLists: {HasDriverCommandLists}";
    }
}
