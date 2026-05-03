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

using System;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

/// <summary>
///   A structure providing a common description for all kinds of <see cref="Texture"/>s.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct TextureDescription : IEquatable<TextureDescription>
{
    /// <summary>
    ///   The dimension (type) of the Texture.
    /// </summary>
    public TextureDimension Dimension;

    /// <summary>
    ///   The Texture width in texels.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The range of this value is from 1 to the maximum supported size, which is constrained by the graphics profile of the device.
    ///   </para>
    ///   <para>
    ///     This field is valid for all textures: <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/>, <see cref="TextureDimension.Texture3D"/>,
    ///     and <see cref="TextureDimension.TextureCube"/>.
    ///   </para>
    /// </remarks>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture1DSize"/>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture2DSize"/>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture3DSize"/>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTextureCubeSize"/>
    public int Width;

    /// <summary>
    ///   The Texture height in texels.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The range of this value is from 1 to the maximum supported size, which is constrained by the graphics profile of the device.
    ///   </para>
    ///   <para>
    ///     This field is valid for <see cref="TextureDimension.Texture2D"/>, <see cref="TextureDimension.Texture3D"/>, and <see cref="TextureDimension.TextureCube"/>.
    ///   </para>
    /// </remarks>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture2DSize"/>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture3DSize"/>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTextureCubeSize"/>
    public int Height;

    /// <summary>
    ///   The Texture depth in texels.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The range of this value is from 1 to the maximum supported size, which is constrained by the graphics profile of the device.
    ///   </para>
    ///   <para>
    ///     This field is valid for <see cref="TextureDimension.Texture3D"/>.
    ///   </para>
    /// </remarks>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture3DSize"/>
    public int Depth;

    /// <summary>
    ///   The number of Textures in the Texture Array.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The range of this value is from 1 to the maximum supported array size, which is constrained by the graphics profile of the device.
    ///   </para>
    ///   <para>
    ///     This field is valid for <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/>, and <see cref="TextureDimension.TextureCube"/>.
    ///   </para>
    /// </remarks>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture1DArraySize"/>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumTexture2DArraySize"/>
    public int ArraySize;

    /// <summary>
    ///   The number of mipmap levels in the Texture.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The range of this value is from 1 to <see cref="GraphicsDeviceFeatures.MaximumMipLevels"/>, which is constrained by the graphics profile of the device.
    ///   </para>
    ///   <para>
    ///     Use 1 for a multisampled Texture; or 0 to generate a full set of subtextures.
    ///   </para>
    /// </remarks>
    /// <seealso cref="GraphicsDeviceFeatures.MaximumMipLevels"/>
    public int MipLevelCount;

    /// <summary>
    ///   The format of the Texture.
    /// </summary>
    public PixelFormat Format;

    /// <summary>
    ///   The level of multisampling for the Texture.
    /// </summary>
    /// <remarks>
    ///   This field is only valid for <see cref="TextureDimension.Texture2D"/>.
    /// </remarks>
    public MultisampleCount MultisampleCount;

    /// <summary>
    ///   A value that indicates how the Texture is to be read from and written to.
    /// </summary>
    /// <remarks>
    ///    The most common value is <see cref="GraphicsResourceUsage.Default"/>.
    /// </remarks>
    public GraphicsResourceUsage Usage;

    /// <summary>
    ///   A combination of flags describing how the Texture is to be bound to the stages of the graphics pipeline.
    /// </summary>
    public TextureFlags Flags;

    /// <summary>
    ///   A combination of flags specifying options for Textures, like creating them as shared resources.
    /// </summary>
    /// <remarks>
    ///   This field must be <see cref="TextureOptions.None"/> when creating Textures with CPU access flags.
    /// </remarks>
    public TextureOptions Options;

    /// <summary>
    ///   Gets a value indicating whether the Texture is a <strong>Render Target</strong>.
    /// </summary>
    /// <value><see langword="true"/> if the Texture is a Render Target; otherwise, <see langword="false"/>.</value>
    public readonly bool IsRenderTarget => Flags.HasFlag(TextureFlags.RenderTarget);

    /// <summary>
    ///   Gets a value indicating whether the Texture is a <strong>Depth-Stencil buffer</strong>.
    /// </summary>
    /// <value><see langword="true"/> if the Texture is a Depth-Stencil buffer; otherwise, <see langword="false"/>.</value>
    public readonly bool IsDepthStencil => Flags.HasFlag(TextureFlags.DepthStencil);

    /// <summary>
    ///   Gets a value indicating whether the Texture is a <strong>Shader Resource</strong>.
    /// </summary>
    /// <value><see langword="true"/> if the Texture is a Shader Resource; otherwise, <see langword="false"/>.</value>
    public readonly bool IsShaderResource => Flags.HasFlag(TextureFlags.ShaderResource);

    /// <summary>
    ///   Gets a value indicating whether the Texture is a created to allow <strong>Unordered Access</strong>
    ///   when used as Shader Resource.
    /// </summary>
    /// <value><see langword="true"/> if the Texture allows Unordered Access; otherwise, <see langword="false"/>.</value>
    public readonly bool IsUnorderedAccess => Flags.HasFlag(TextureFlags.UnorderedAccess);

    /// <summary>
    ///   Gets a value indicating whether the Texture is a <strong>multi-sampled</strong> Texture.
    /// </summary>
    /// <value><see langword="true"/> if the Texture is multi-sampled; otherwise, <see langword="false"/>.</value>
    public readonly bool IsMultiSampled => MultisampleCount > MultisampleCount.None;

    /// <summary>
    ///   Returns a copy of this Texture Description modified to describe a <strong>staging Texture</strong>.
    /// </summary>
    /// <returns>A staging Texture Description.</returns>
    public readonly TextureDescription ToStagingDescription()
    {
        return this with
        {
            Flags = TextureFlags.None,
            Usage = GraphicsResourceUsage.Staging
        };
    }

    /// <summary>
    ///   Returns a copy of this Texture Description modified so if the Texture is <see cref="GraphicsResourceUsage.Immutable"/>,
    ///   it is switched to <see cref="GraphicsResourceUsage.Default"/>.
    /// </summary>
    /// <returns>A modified copy of this Texture Description.</returns>
    public readonly TextureDescription ToCloneableDescription()
    {
        return this with
        {
            Usage = Usage is GraphicsResourceUsage.Immutable ? GraphicsResourceUsage.Default : Usage
        };
    }


    /// <summary>
    ///   Creates a new description from another description but overrides <see cref="Flags"/> and <see cref="Usage"/>.
    /// </summary>
    /// <param name="description">The Texture Description to copy.</param>
    /// <param name="textureFlags">The new Texture flags.</param>
    /// <param name="usage">The new usage.</param>
    /// <returns>A modified copy of the specified <paramref name="description"/>.</returns>
    public static TextureDescription FromDescription(TextureDescription description, TextureFlags textureFlags, GraphicsResourceUsage usage)
    {
        return description with
        {
            Flags = textureFlags,
            Usage = textureFlags.HasFlag(TextureFlags.UnorderedAccess)
                ? GraphicsResourceUsage.Default
                : usage
        };
    }

    /// <summary>
    ///   Performs an explicit conversion from <see cref="ImageDescription"/> to <see cref="TextureDescription"/>.
    /// </summary>
    /// <param name="description">The Image Description to convert.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator TextureDescription(ImageDescription description)
    {
        return new TextureDescription
        {
            Dimension = description.Dimension,
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            ArraySize = description.ArraySize,
            MipLevelCount = description.MipLevels,
            Format = description.Format,
            Flags = TextureFlags.ShaderResource,
            MultisampleCount = MultisampleCount.None
        };
    }

    /// <summary>
    ///   Performs an implicit conversion from <see cref="TextureDescription"/> to <see cref="ImageDescription"/>.
    /// </summary>
    /// <param name="description">The Texture Description to convert.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator ImageDescription(TextureDescription description)
    {
        return new ImageDescription
        {
            Dimension = description.Dimension,
            Width = description.Width,
            Height = description.Height,
            Depth = description.Depth,
            ArraySize = description.ArraySize,
            MipLevels = description.MipLevelCount,
            Format = description.Format
        };
    }

    /// <inheritdoc/>
    public readonly bool Equals(TextureDescription other)
    {
        return Dimension == other.Dimension
            && Width == other.Width
            && Height == other.Height
            && Depth == other.Depth
            && ArraySize == other.ArraySize
            && MipLevelCount == other.MipLevelCount
            && Format == other.Format
            && MultisampleCount == other.MultisampleCount
            && Usage == other.Usage
            && Flags == other.Flags;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        if (obj is null)
            return false;

        return obj is TextureDescription description && Equals(description);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Dimension);
        hash.Add(Width);
        hash.Add(Height);
        hash.Add(Depth);
        hash.Add(ArraySize);
        hash.Add(MipLevelCount);
        hash.Add(Format);
        hash.Add(MultisampleCount);
        hash.Add(Usage);
        hash.Add(Flags);
        return hash.ToHashCode();
    }

    public static bool operator ==(TextureDescription left, TextureDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextureDescription left, TextureDescription right)
    {
        return !left.Equals(right);
    }
}
