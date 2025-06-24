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

[StructLayout(LayoutKind.Sequential)]
public partial struct TextureDescription : IEquatable<TextureDescription>
{
    /// <summary>
    /// A Common description for all textures.
    /// </summary>
        /// <summary>
        /// The dimension of a texture.
        /// </summary>
    public TextureDimension Dimension;

        /// <summary>
        /// <dd> <p>Texture width (in texels). The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture1DSize"/> (16384). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is valid for all textures: <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/>, <see cref="TextureDimension.Texture3D"/> and <see cref="TextureDimension.TextureCube"/>.
        /// </remarks>
    public int Width;

        /// <summary>
        /// <dd> <p>Texture height (in texels). The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture3DSize"/> (2048). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture2D"/>, <see cref="TextureDimension.Texture3D"/> and <see cref="TextureDimension.TextureCube"/>.
        /// </remarks>
    public int Height;

        /// <summary>
        /// <dd> <p>Texture depth (in texels). The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture3DSize"/> (2048). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture3D"/>.
        /// </remarks>
    public int Depth;

        /// <summary>
        /// <dd> <p>Number of textures in the array. The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture1DArraySize"/> (2048). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/> and <see cref="TextureDimension.TextureCube"/>
        /// </remarks>
        /// <remarks>
        /// This field is only valid for textures: <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/> and <see cref="TextureDimension.TextureCube"/>.
        /// </remarks>
    public int ArraySize;

        /// <summary>
        /// <dd> <p>The maximum number of mipmap levels in the texture. See the remarks in <strong><see cref="SharpDX.Direct3D11.ShaderResourceViewDescription.Texture1DResource"/></strong>. Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.</p> </dd>
        /// </summary>
    public int MipLevelCount;

        /// <summary>
        /// <dd> <p>Texture format (see <strong><see cref="SharpDX.DXGI.Format"/></strong>).</p> </dd>
        /// </summary>
    public PixelFormat Format;

        /// <summary>
        /// <dd> <p>Structure that specifies multisampling parameters for the texture. See <strong><see cref="SharpDX.DXGI.SampleDescription"/></strong>.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture2D"/>.
        /// </remarks>
    public MultisampleCount MultisampleCount;

        /// <summary>
        /// <dd> <p>Value that identifies how the texture is to be read from and written to. The most common value is <see cref="SharpDX.Direct3D11.ResourceUsage.Default"/>; see <strong><see cref="SharpDX.Direct3D11.ResourceUsage"/></strong> for all possible values.</p> </dd>
        /// </summary>
    public GraphicsResourceUsage Usage;

        /// <summary>
        /// <dd> <p>Flags (see <strong><see cref="SharpDX.Direct3D11.BindFlags"/></strong>) for binding to pipeline stages. The flags can be combined by a logical OR. For a 1D texture, the allowable values are: <see cref="SharpDX.Direct3D11.BindFlags.ShaderResource"/>, <see cref="SharpDX.Direct3D11.BindFlags.RenderTarget"/> and <see cref="SharpDX.Direct3D11.BindFlags.DepthStencil"/>.</p> </dd>
        /// </summary>
    public TextureFlags Flags;

        /// <summary>
        /// Resource options for DirectX 11 textures.
        /// </summary>
    public TextureOptions Options;

        /// <summary>
        /// Gets a value indicating whether this instance is a render target.
        /// </summary>
        /// <value><c>true</c> if this instance is render target; otherwise, <c>false</c>.</value>
    public readonly bool IsRenderTarget => Flags.HasFlag(TextureFlags.RenderTarget);

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil; otherwise, <c>false</c>.</value>
    public readonly bool IsDepthStencil => Flags.HasFlag(TextureFlags.DepthStencil);

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
    public readonly bool IsShaderResource => Flags.HasFlag(TextureFlags.ShaderResource);

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
    public readonly bool IsUnorderedAccess => Flags.HasFlag(TextureFlags.UnorderedAccess);

        /// <summary>
        /// Gets a value indicating whether this instance is a multi sample texture.
        /// </summary>
        /// <value><c>true</c> if this instance is multi sample texture; otherwise, <c>false</c>.</value>
    public readonly bool IsMultiSampled => MultisampleCount > MultisampleCount.None;

        /// <summary>
        /// Gets the staging description for this instance..
        /// </summary>
        /// <returns>A Staging description</returns>
    public readonly TextureDescription ToStagingDescription()
    {
        return this with
        {
            Flags = TextureFlags.None,
            Usage = GraphicsResourceUsage.Staging
        };
    }

        /// <summary>
        /// Gets a clone description of this instance (if texture is immutable, it is switched to default).
        /// </summary>
        /// <returns>A clone of this instance.</returns>
    public readonly TextureDescription ToCloneableDescription()
    {
        return this with
        {
            Usage = Usage is GraphicsResourceUsage.Immutable ? GraphicsResourceUsage.Default : Usage
        };
    }


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

    public override readonly bool Equals(object obj)
    {
        if (obj is null)
            return false;

        return obj is TextureDescription description && Equals(description);
    }

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
