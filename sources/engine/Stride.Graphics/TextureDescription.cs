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
using System.Runtime.InteropServices;

namespace Xenko.Graphics
{
    /// <summary>
    /// A Common description for all textures.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public partial struct TextureDescription : IEquatable<TextureDescription>
    {
        /// <summary>
        /// The dimension of a texture.
        /// </summary>
        public TextureDimension Dimension;

        /// <summary>
        /// <dd> <p>Texture width (in texels). The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture1DSize"/> (16384). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is valid for all textures: <see cref="Texture1D"/>, <see cref="Texture2D"/>, <see cref="Texture3D"/> and <see cref="TextureCube"/>.
        /// </remarks>
        public int Width;

        /// <summary>
        /// <dd> <p>Texture height (in texels). The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture3DSize"/> (2048). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="Texture2D"/>, <see cref="Texture3D"/> and <see cref="TextureCube"/>.
        /// </remarks>
        public int Height;

        /// <summary>
        /// <dd> <p>Texture depth (in texels). The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture3DSize"/> (2048). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="Texture3D"/>.
        /// </remarks>
        public int Depth;

        /// <summary>
        /// <dd> <p>Number of textures in the array. The  range is from 1 to <see cref="SharpDX.Direct3D11.Resource.MaximumTexture1DArraySize"/> (2048). However, the range is actually constrained by the feature level at which you create the rendering device. For more information about restrictions, see Remarks.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="Texture1D"/>, <see cref="Texture2D"/> and <see cref="TextureCube"/>
        /// </remarks>
        /// <remarks>
        /// This field is only valid for textures: <see cref="Texture1D"/>, <see cref="Texture2D"/> and <see cref="TextureCube"/>.
        /// </remarks>
        public int ArraySize;

        /// <summary>
        /// <dd> <p>The maximum number of mipmap levels in the texture. See the remarks in <strong><see cref="SharpDX.Direct3D11.ShaderResourceViewDescription.Texture1DResource"/></strong>. Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.</p> </dd>
        /// </summary>
        public int MipLevels;

        /// <summary>
        /// <dd> <p>Texture format (see <strong><see cref="SharpDX.DXGI.Format"/></strong>).</p> </dd>
        /// </summary>
        public PixelFormat Format;

        /// <summary>
        /// <dd> <p>Structure that specifies multisampling parameters for the texture. See <strong><see cref="SharpDX.DXGI.SampleDescription"/></strong>.</p> </dd>
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="Texture2D"/>.
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
        public bool IsRenderTarget
        {
            get
            {
                return (Flags & TextureFlags.RenderTarget) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil; otherwise, <c>false</c>.</value>
        public bool IsDepthStencil
        {
            get
            {
                return (Flags & TextureFlags.DepthStencil) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
        public bool IsShaderResource
        {
            get
            {
                return (Flags & TextureFlags.ShaderResource) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
        public bool IsUnorderedAccess
        {
            get
            {
                return (Flags & TextureFlags.UnorderedAccess) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a multi sample texture.
        /// </summary>
        /// <value><c>true</c> if this instance is multi sample texture; otherwise, <c>false</c>.</value>
        public bool IsMultisample
        {
            get
            {
                return this.MultisampleCount > MultisampleCount.None;
            }
        }

        /// <summary>
        /// Gets the staging description for this instance..
        /// </summary>
        /// <returns>A Staging description</returns>
        public TextureDescription ToStagingDescription()
        {
            var copy = this;
            copy.Flags = TextureFlags.None;
            copy.Usage = GraphicsResourceUsage.Staging;
            return copy;
        }

        /// <summary>
        /// Gets a clone description of this instance (if texture is immutable, it is switched to default).
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public TextureDescription ToCloneableDescription()
        {
            var description = this;
            if (description.Usage == GraphicsResourceUsage.Immutable)
                description.Usage = GraphicsResourceUsage.Default;
            return description;
        }

        /// <summary>
        /// Creates a new description from another description but overrides <see cref="Flags"/> and <see cref="Usage"/>.
        /// </summary>
        /// <param name="desc">The desc.</param>
        /// <param name="textureFlags">The texture flags.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>TextureDescription.</returns>
        public static TextureDescription FromDescription(TextureDescription desc, TextureFlags textureFlags, GraphicsResourceUsage usage)
        {
            desc.Flags = textureFlags;
            desc.Usage = usage;
            if ((textureFlags & TextureFlags.UnorderedAccess) != 0)
                desc.Usage = GraphicsResourceUsage.Default;
            return desc;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="ImageDescription"/> to <see cref="TextureDescription"/>.
        /// </summary>
        /// <param name="description">The image description.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator TextureDescription(ImageDescription description)
        {
            return new TextureDescription()
            {
                Dimension = description.Dimension,
                Width = description.Width,
                Height = description.Height,
                Depth = description.Depth,
                ArraySize = description.ArraySize,
                MipLevels = description.MipLevels,
                Format = description.Format,
                Flags = TextureFlags.ShaderResource,
                MultisampleCount = MultisampleCount.None,
            };
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="ImageDescription"/> to <see cref="TextureDescription"/>.
        /// </summary>
        /// <param name="description">The image description.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator ImageDescription(TextureDescription description)
        {
            return new ImageDescription()
            {
                Dimension = description.Dimension,
                Width = description.Width,
                Height = description.Height,
                Depth = description.Depth,
                ArraySize = description.ArraySize,
                MipLevels = description.MipLevels,
                Format = description.Format,
            };
        }

        public bool Equals(TextureDescription other)
        {
            return Dimension == other.Dimension && Width == other.Width && Height == other.Height && Depth == other.Depth && ArraySize == other.ArraySize && MipLevels == other.MipLevels && Format == other.Format && MultisampleCount == other.MultisampleCount && Usage == other.Usage && Flags == other.Flags;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TextureDescription && Equals((TextureDescription)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Dimension;
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ Depth;
                hashCode = (hashCode * 397) ^ ArraySize;
                hashCode = (hashCode * 397) ^ MipLevels;
                hashCode = (hashCode * 397) ^ (int)Format;
                hashCode = (hashCode * 397) ^ (int)MultisampleCount;
                hashCode = (hashCode * 397) ^ (int)Usage;
                hashCode = (hashCode * 397) ^ (int)Flags;
                return hashCode;
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(TextureDescription left, TextureDescription right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(TextureDescription left, TextureDescription right)
        {
            return !left.Equals(right);
        }
    }
}
