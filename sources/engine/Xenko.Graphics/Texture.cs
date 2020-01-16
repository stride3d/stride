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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.ReferenceCounting;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics.Data;
using Utilities = Xenko.Core.Utilities;

namespace Xenko.Graphics
{
    /// <summary>
    /// Class used for all Textures (1D, 2D, 3D, DepthStencil, RenderTargets...etc.)
    /// </summary>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Texture>), Profile = "Content")]
    [ContentSerializer(typeof(TextureContentSerializer))]
    [ContentSerializer(typeof(TextureImageSerializer))]
    [DebuggerDisplay("Texture {ViewWidth}x{ViewHeight}x{ViewDepth} {Format} ({ViewFlags})")]
    [DataSerializer(typeof(TextureSerializer))]
    public sealed partial class Texture : GraphicsResource
    {
        internal const int DepthStencilReadOnlyFlags = 16;

        private TextureDescription textureDescription;
        private TextureViewDescription textureViewDescription;
        private Size3? fullQualitySize;

        /// <summary>
        /// Common description for the original texture. See remarks.
        /// </summary>
        /// <remarks>
        /// This field and the properties in TextureDessciption must be considered as readonly when accessing from this instance.
        /// </remarks>
        public TextureDescription Description
        {
            get
            {
                return textureDescription;
            }
        }

        /// <summary>
        /// Gets the view description.
        /// </summary>
        /// <value>The view description.</value>
        public TextureViewDescription ViewDescription
        {
            get
            {
                return textureViewDescription;
            }
        }

        /// <summary>
        /// The dimension of a texture.
        /// </summary>
        public TextureDimension Dimension
        {
            get
            {
                // TODO: What's the point of storing the dimensions?
                // We could just as well generate the "TextureDimension" based on the "TextureTarget" property,
                // because "TextureDimension" is fully dependent on the "TextureTarget" property.
                // E.g. "Texture2D" and "Texture2DMultisample" both return "TextureDimension.Texture2D".
                return textureDescription.Dimension;
            }
        }

        /// <summary>
        /// The width of this texture view.
        /// </summary>
        /// <value>The width of the view.</value>
        public int ViewWidth { get; private set; }

        /// <summary>
        /// The height of this texture view.
        /// </summary>
        /// <value>The height of the view.</value>
        public int ViewHeight { get; private set; }

        /// <summary>
        /// The depth of this texture view.
        /// </summary>
        /// <value>The view depth.</value>
        public int ViewDepth { get; private set; }

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The view format.</value>
        public PixelFormat ViewFormat
        {
            get
            {
                return textureViewDescription.Format;
            }
        }

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The type of the view.</value>
        public TextureFlags ViewFlags
        {
            get
            {
                return textureViewDescription.Flags;
            }
        }

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The type of the view.</value>
        public ViewType ViewType
        {
            get
            {
                return textureViewDescription.Type;
            }
        }

        /// <summary>
        /// The dimension of the texture view.
        /// </summary>
        public TextureDimension ViewDimension
        {
            get => Dimension == TextureDimension.TextureCube && ViewType != ViewType.Full ? TextureDimension.Texture2D : Dimension;
        }

        /// <summary>
        /// The miplevel index of this texture view.
        /// </summary>
        /// <value>The mip level.</value>
        public int MipLevel
        {
            get
            {
                return textureViewDescription.MipLevel;
            }
        }

        /// <summary>
        /// The array index of this texture view.
        /// </summary>
        /// <value>The array slice.</value>
        public int ArraySlice
        {
            get
            {
                return textureViewDescription.ArraySlice;
            }
        }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        /// <value>The width.</value>
        public int Width
        {
            get
            {
                return textureDescription.Width;
            }
        }

        /// <summary>
        /// The height of the texture.
        /// </summary>
        /// <value>The height.</value>
        public int Height
        {
            get
            {
                return textureDescription.Height;
            }
        }

        /// <summary>
        /// The depth of the texture.
        /// </summary>
        /// <value>The depth.</value>
        public int Depth
        {
            get
            {
                return textureDescription.Depth;
            }
        }

        /// <summary>
        /// Number of textures in the array.
        /// </summary>
        /// <value>The size of the array.</value>
        /// <remarks>This field is only valid for 1D, 2D and Cube <see cref="Texture" />.</remarks>
        public int ArraySize
        {
            get
            {
                return textureDescription.ArraySize;
            }
        }

        /// <summary>
        /// The maximum number of mipmap levels in the texture.
        /// </summary>
        /// <value>The mip levels.</value>
        public int MipLevels
        {
            get
            {
                return textureDescription.MipLevels;
            }
        }

        /// <summary>
        /// Texture format (see <see cref="PixelFormat" />)
        /// </summary>
        /// <value>The format.</value>
        public PixelFormat Format
        {
            get
            {
                return textureDescription.Format;
            }
        }

        /// <summary>
        /// Structure that specifies multisampling parameters for the texture.
        /// </summary>
        /// <value>The multi sample level.</value>
        /// <remarks>This field is only valid for a 2D <see cref="Texture" />.</remarks>
        public MultisampleCount MultisampleCount
        {
            get
            {
                return textureDescription.MultisampleCount;
            }
        }

        /// <summary>
        /// Value that identifies how the texture is to be read from and written to.
        /// </summary>
        public GraphicsResourceUsage Usage
        {
            get
            {
                return textureDescription.Usage;
            }
        }

        /// <summary>
        /// Texture flags.
        /// </summary>
        public TextureFlags Flags
        {
            get
            {
                return textureDescription.Flags;
            }
        }

        /// <summary>
        /// Resource options for DirectX 11 textures.
        /// </summary>
        public TextureOptions Options
        {
            get
            {
                return textureDescription.Options;
            }
        }

        /// <summary>
        /// The shared handle if created with TextureOption.Shared or TextureOption.SharedNthandle, IntPtr.Zero otherwise.
        /// </summary>
        public IntPtr SharedHandle { get; private set; } = IntPtr.Zero;

#if XENKO_GRAPHICS_API_DIRECT3D11
        /// <summary>
        /// Gets the name of the shared Nt handle when created with TextureOption.SharedNthandle.
        /// </summary>
        public string SharedNtHandleName { get; private set; } = string.Empty; 
#endif

        /// <summary>
        /// Gets a value indicating whether this instance is a render target.
        /// </summary>
        /// <value><c>true</c> if this instance is render target; otherwise, <c>false</c>.</value>
        public bool IsRenderTarget
        {
            get
            {
                return (ViewFlags & TextureFlags.RenderTarget) != 0;
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
                return (ViewFlags & TextureFlags.DepthStencil) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil readonly.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil readonly; otherwise, <c>false</c>.</value>
        public bool IsDepthStencilReadOnly
        {
            get
            {
                return (ViewFlags & TextureFlags.DepthStencilReadOnly) == TextureFlags.DepthStencilReadOnly;
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
                return (ViewFlags & TextureFlags.ShaderResource) != 0;
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
                return (ViewFlags & TextureFlags.UnorderedAccess) != 0;
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
        /// Gets a boolean indicating whether this <see cref="Texture"/> is a using a block compress format (BC1, BC2, BC3, BC4, BC5, BC6H, BC7).
        /// </summary>
        public bool IsBlockCompressed { get; private set; }

        /// <summary>
        /// Gets the size of this texture.
        /// </summary>
        /// <value>The size.</value>
        public Size3 Size => new Size3(ViewWidth, ViewHeight, ViewDepth);
        
        /// <summary>
        /// When texture streaming is activated, the size of the texture when loaded at full quality.
        /// </summary>
        public Size3 FullQualitySize
        {
            get => fullQualitySize ?? Size;
            internal set => fullQualitySize = value;
        }

        /// <summary> 
        /// The width stride in bytes (number of bytes per row).
        /// </summary>
        internal int RowStride { get; private set; }

        /// <summary>
        /// The underlying parent texture (if this is a view).
        /// </summary>
        internal Texture ParentTexture { get; private set; }

        /// <summary>
        /// Returns the total memory allocated by the texture in bytes.
        /// </summary>
        internal int SizeInBytes { get; private set; }

        private MipMapDescription[] mipmapDescriptions;

        public Texture()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        internal Texture(GraphicsDevice device) : base(device)
        {
        }

        protected override void Destroy()
        {
            base.Destroy();
            if (ParentTexture != null)
            {
                ParentTexture.ReleaseInternal();
            }
        }

        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            OnRecreateImpl();
            return true;
        }

        internal Texture InitializeFrom(TextureDescription description, DataBox[] textureDatas = null)
        {
            return InitializeFrom(null, description, new TextureViewDescription(), textureDatas);
        }

#if XENKO_PLATFORM_ANDROID //&& USE_GLES_EXT_OES_TEXTURE
        internal Texture InitializeForExternalOES()
        {
            InitializeForExternalOESImpl();
            return this;
        }
#endif

        internal Texture InitializeFrom(TextureDescription description, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(null, description, viewDescription, textureDatas);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture, parentTexture.Description, viewDescription, textureDatas);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureDescription description, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            ParentTexture = parentTexture;
            if (ParentTexture != null)
            {
                ParentTexture.AddReferenceInternal();
            }

            textureDescription = description;
            textureViewDescription = viewDescription;
            IsBlockCompressed = description.Format.IsCompressed();
            RowStride = ComputeRowPitch(0);
            mipmapDescriptions = Image.CalculateMipMapDescription(description);
            SizeInBytes = ArraySize * mipmapDescriptions?.Sum(desc => desc.MipmapSize) ?? 0;

            ViewWidth = Math.Max(1, Width >> MipLevel);
            ViewHeight = Math.Max(1, Height >> MipLevel);
            ViewDepth = Math.Max(1, Depth >> MipLevel);
            if (ViewFormat == PixelFormat.None)
            {
                textureViewDescription.Format = description.Format;
            }
            if (ViewFlags == TextureFlags.None)
            {
                textureViewDescription.Flags = description.Flags;
            }

            // Check that the view is compatible with the parent texture
            var filterViewFlags = (TextureFlags)((int)ViewFlags & (~DepthStencilReadOnlyFlags));
            if ((Flags & filterViewFlags) != filterViewFlags)
            {
                throw new NotSupportedException("Cannot create a texture view with flags [{0}] from the parent texture [{1}] as the parent texture must include all flags defined by the view".ToFormat(ViewFlags, Flags));
            }

            if (IsMultisample)
            {
                var maxCount = GraphicsDevice.Features[Format].MultisampleCountMax;
                if (maxCount < MultisampleCount)
                    throw new NotSupportedException($"Cannot create a texture with format {Format} and multisample level {MultisampleCount}. Maximum supported level is {maxCount}");
            }

            InitializeFromImpl(textureDatas);

            return this;
        }

        /// <summary>
        /// Releases the texture data.
        /// </summary>
        public void ReleaseData()
        {
            // Release GPU data
            OnDestroyed();

            // Clean description
            textureDescription = new TextureDescription();
            textureViewDescription = new TextureViewDescription();
            ViewWidth = ViewHeight = ViewDepth = 0;
            SizeInBytes = 0;
            mipmapDescriptions = null;
        }

        /// <summary>
        /// Gets a view on this texture for a particular <see cref="ViewType" />, array index (or zIndex for Texture3D), and mipmap index.
        /// </summary>
        /// <param name="viewDescription">The view description.</param>
        /// <returns>A new texture object that is bouded to the requested view.</returns>
        public Texture ToTextureView(TextureViewDescription viewDescription)
        {
            return new Texture(GraphicsDevice).InitializeFrom(ParentTexture ?? this, viewDescription);
        }

        /// <summary>
        /// Gets the mipmap description of this instance for the specified mipmap level.
        /// </summary>
        /// <param name="mipmap">The mipmap.</param>
        /// <returns>A description of a particular mipmap for this texture.</returns>
        public MipMapDescription GetMipMapDescription(int mipmap)
        {
            return mipmapDescriptions[mipmap];
        }

        /// <summary>
        /// Calculates the size of a particular mip.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="mipLevel">The mip level.</param>
        /// <returns>System.Int32.</returns>
        public static int CalculateMipSize(int size, int mipLevel)
        {
            mipLevel = Math.Min(mipLevel, Image.CountMips(size));
            return Math.Max(1, size >> mipLevel);
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 1D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                int maxMips = CountMips(width);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException($"MipLevels must be <= {maxMips}");
            }
            else if (mipLevels == 0)
            {
                mipLevels = CountMips(width);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, int height, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                int maxMips = CountMips(width, height);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException($"MipLevels must be <= {maxMips}");
            }
            else if (mipLevels == 0)
            {
                mipLevels = CountMips(width, height);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="depth">The depth of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, int height, int depth, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                if (!MathUtil.IsPow2(width) || !MathUtil.IsPow2(height) || !MathUtil.IsPow2(depth))
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                int maxMips = CountMips(width, height, depth);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException($"MipLevels must be <= {maxMips}");
            }
            else if (mipLevels == 0)
            {
                if (!MathUtil.IsPow2(width) || !MathUtil.IsPow2(height) || !MathUtil.IsPow2(depth))
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                mipLevels = CountMips(width, height, depth);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Gets the absolute sub-resource index from the array and mip slice.
        /// </summary>
        /// <param name="arraySlice">The array slice index.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <returns>A value equals to arraySlice * Description.MipLevels + mipSlice.</returns>
        public int GetSubResourceIndex(int arraySlice, int mipSlice)
        {
            return arraySlice * MipLevels + mipSlice;
        }

        /// <summary>
        /// Calculates the expected width of a texture using a specified type.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <returns>The expected width</returns>
        /// <exception cref="System.ArgumentException">If the size is invalid</exception>
        public int CalculateWidth<TData>(int mipLevel = 0) where TData : struct
        {
            var widthOnMip = CalculateMipSize((int)Width, mipLevel);
            var rowStride = widthOnMip * Format.SizeInBytes();

            var dataStrideInBytes = Utilities.SizeOf<TData>() * widthOnMip;
            var width = ((double)rowStride / dataStrideInBytes) * widthOnMip;
            if (Math.Abs(width - (int)width) > double.Epsilon)
                throw new ArgumentException("sizeof(TData) / sizeof(Format) * Width is not an integer");

            return (int)width;
        }

        /// <summary>
        /// Calculates the number of pixel data this texture is requiring for a particular mip level.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <param name="mipLevel">The mip level.</param>
        /// <returns>The number of pixel data.</returns>
        /// <remarks>This method is used to allocated a texture data buffer to hold pixel datas: var textureData = new T[ texture.CalculatePixelCount&lt;T&gt;() ] ;.</remarks>
        public int CalculatePixelDataCount<TData>(int mipLevel = 0) where TData : struct
        {
            return CalculateWidth<TData>(mipLevel) * CalculateMipSize(Height, mipLevel) * CalculateMipSize(Depth, mipLevel);
        }

        /// <summary>
        /// Gets the content of this texture to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The command list.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <returns>The texture data.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public TData[] GetData<TData>(CommandList commandList, int arraySlice = 0, int mipSlice = 0) where TData : struct
        {
            var toData = new TData[this.CalculatePixelDataCount<TData>(mipSlice)];
            GetData(commandList, toData, arraySlice, mipSlice);
            return toData;
        }

        /// <summary>
        /// Copies the content of this texture to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The command list.</param>
        /// <param name="toData">The destination buffer to receive a copy of the texture datas.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public bool GetData<TData>(CommandList commandList, TData[] toData, int arraySlice = 0, int mipSlice = 0, bool doNotWait = false) where TData : struct
        {
            // Get data from this resource
            if (Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                return GetData(commandList, this, toData, arraySlice, mipSlice, doNotWait);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using (var throughStaging = this.ToStaging())
                    return GetData(commandList, throughStaging, toData, arraySlice, mipSlice, doNotWait);
            }
        }

        /// <summary>
        /// Copies the content of this texture from GPU memory to an array of data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The command list.</param>
        /// <param name="stagingTexture">The staging texture used to transfer the texture to.</param>
        /// <param name="toData">To data.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe bool GetData<TData>(CommandList commandList, Texture stagingTexture, TData[] toData, int arraySlice = 0, int mipSlice = 0, bool doNotWait = false) where TData : struct
        {
            return GetData(commandList, stagingTexture, new DataPointer((IntPtr)Interop.Fixed(toData), toData.Length * Utilities.SizeOf<TData>()), arraySlice, mipSlice, doNotWait);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this texture into GPU memory using the specified <see cref="GraphicsDevice"/> (The graphics device could be deffered).
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The command list.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="region">Destination region</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// See unmanaged documentation for usage and restrictions.
        /// </remarks>
        public unsafe void SetData<TData>(CommandList commandList, TData[] fromData, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null) where TData : struct
        {
            SetData(commandList, new DataPointer((IntPtr)Interop.Fixed(fromData), fromData.Length * Utilities.SizeOf<TData>()), arraySlice, mipSlice, region);
        }

        /// <summary>
        /// Copies the content of this texture from GPU memory to a pointer on CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="commandList">The command list.</param>
        /// <param name="stagingTexture">The staging texture used to transfer the texture to.</param>
        /// <param name="toData">The pointer to data in CPU memory.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe bool GetData(CommandList commandList, Texture stagingTexture, DataPointer toData, int arraySlice = 0, int mipSlice = 0, bool doNotWait = false)
        {
            if (stagingTexture == null) throw new ArgumentNullException("stagingTexture");
            var device = GraphicsDevice;
            //var deviceContext = device.NativeDeviceContext;

            // Get mipmap description for the specified mipSlice
            var mipmap = this.GetMipMapDescription(mipSlice);

            // Copy height, depth
            int height = mipmap.HeightPacked;
            int depth = mipmap.Depth;

            // Calculate depth stride based on mipmap level
            int rowStride = mipmap.RowStride;

            // Depth Stride
            int textureDepthStride = mipmap.DepthStride;

            // MipMap Stride
            int mipMapSize = mipmap.MipmapSize;

            // Check size validity of data to copy to
            if (toData.Size > mipMapSize)
                throw new ArgumentException($"Size of toData ({toData.Size} bytes) is not compatible expected size ({mipMapSize} bytes) : Width * Height * Depth * sizeof(PixelFormat) size in bytes");

            // Copy the actual content of the texture to the staging resource
            if (!ReferenceEquals(this, stagingTexture))
                commandList.Copy(this, stagingTexture);

            // Calculate the subResourceIndex for a Texture
            int subResourceIndex = this.GetSubResourceIndex(arraySlice, mipSlice);

            // Map the staging resource to a CPU accessible memory
            var mappedResource = commandList.MapSubresource(stagingTexture, subResourceIndex, MapMode.Read, doNotWait);

            // Box can be empty if DoNotWait is set to true, return false if empty
            var box = mappedResource.DataBox;
            if (box.IsEmpty)
            {
                return false;
            }

            // If depth == 1 (Texture, Texture or TextureCube), then depthStride is not used
            var boxDepthStride = this.Depth == 1 ? box.SlicePitch : textureDepthStride;

            var isFlippedTexture = IsFlipped();

            // The fast way: If same stride, we can directly copy the whole texture in one shot
            if (box.RowPitch == rowStride && boxDepthStride == textureDepthStride && !isFlippedTexture)
            {
                Utilities.CopyMemory(toData.Pointer, box.DataPointer, mipMapSize);
            }
            else
            {
                // Otherwise, the long way by copying each scanline
                var sourcePerDepthPtr = (byte*)box.DataPointer;
                var destPtr = (byte*)toData.Pointer;

                // Iterate on all depths
                for (int j = 0; j < depth; j++)
                {
                    var sourcePtr = sourcePerDepthPtr;
                    // Iterate on each line

                    if (isFlippedTexture)
                    {
                        sourcePtr = sourcePtr + box.RowPitch * (height - 1);
                        for (int i = height - 1; i >= 0; i--)
                        {
                            // Copy a single row
                            Utilities.CopyMemory(new IntPtr(destPtr), new IntPtr(sourcePtr), rowStride);
                            sourcePtr -= box.RowPitch;
                            destPtr += rowStride;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < height; i++)
                        {
                            // Copy a single row
                            Utilities.CopyMemory(new IntPtr(destPtr), new IntPtr(sourcePtr), rowStride);
                            sourcePtr += box.RowPitch;
                            destPtr += rowStride;
                        }
                    }
                    sourcePerDepthPtr += box.SlicePitch;
                }
            }

            // Make sure that we unmap the resource in case of an exception
            commandList.UnmapSubresource(mappedResource);

            return true;
        }

        /// <summary>
        /// Copies the content an data on CPU memory to this texture into GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="region">Destination region</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// See unmanaged documentation for usage and restrictions.
        /// </remarks>
        public unsafe void SetData(CommandList commandList, DataPointer fromData, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null)
        {
            if (commandList == null) throw new ArgumentNullException("commandList");
            if (region.HasValue && this.Usage != GraphicsResourceUsage.Default)
                throw new ArgumentException("Region is only supported for textures with ResourceUsage.Default");

            // Get mipmap description for the specified mipSlice
            var mipMapDesc = this.GetMipMapDescription(mipSlice);

            int width = mipMapDesc.Width;
            int height = mipMapDesc.Height;
            int depth = mipMapDesc.Depth;

            // If we are using a region, then check that parameters are fine
            if (region.HasValue)
            {
                int newWidth = region.Value.Right - region.Value.Left;
                int newHeight = region.Value.Bottom - region.Value.Top;
                int newDepth = region.Value.Back - region.Value.Front;
                if (newWidth > width)
                    throw new ArgumentException($"Region width [{newWidth}] cannot be greater than mipmap width [{width}]", "region");
                if (newHeight > height)
                    throw new ArgumentException($"Region height [{newHeight}] cannot be greater than mipmap height [{height}]", "region");
                if (newDepth > depth)
                    throw new ArgumentException($"Region depth [{newDepth}] cannot be greater than mipmap depth [{depth}]", "region");

                width = newWidth;
                height = newHeight;
                depth = newDepth;
            }

            // Size per pixel
            var sizePerElement = Format.SizeInBytes();

            // Calculate depth stride based on mipmap level
            int rowStride;

            // Depth Stride
            int textureDepthStride;

            // Compute Actual pitch
            Image.ComputePitch(this.Format, width, height, out rowStride, out textureDepthStride, out width, out height);

            // Size Of actual texture data
            int sizeOfTextureData = textureDepthStride * depth;

            // Check size validity of data to copy to
            if (fromData.Size != sizeOfTextureData)
                throw new ArgumentException($"Size of toData ({fromData.Size} bytes) is not compatible expected size ({sizeOfTextureData} bytes) : Width * Height * Depth * sizeof(PixelFormat) size in bytes");

            // Calculate the subResourceIndex for a Texture
            int subResourceIndex = this.GetSubResourceIndex(arraySlice, mipSlice);

            // If this texture is declared as default usage, we use UpdateSubresource that supports sub resource region.
            if (this.Usage == GraphicsResourceUsage.Default)
            {
                // If using a specific region, we need to handle this case
                if (region.HasValue)
                {
                    var regionValue = region.Value;
                    var sourceDataPtr = fromData.Pointer;

                    // Workaround when using region with a deferred context and a device that does not support CommandList natively
                    // see http://blogs.msdn.com/b/chuckw/archive/2010/07/28/known-issue-direct3d-11-updatesubresource-and-deferred-contexts.aspx
                    if (commandList.GraphicsDevice.NeedWorkAroundForUpdateSubResource)
                    {
                        if (IsBlockCompressed)
                        {
                            regionValue.Left /= 4;
                            regionValue.Right /= 4;
                            regionValue.Top /= 4;
                            regionValue.Bottom /= 4;
                        }
                        sourceDataPtr = new IntPtr((byte*)sourceDataPtr - (regionValue.Front * textureDepthStride) - (regionValue.Top * rowStride) - (regionValue.Left * sizePerElement));
                    }
                    commandList.UpdateSubresource(this, subResourceIndex, new DataBox(sourceDataPtr, rowStride, textureDepthStride), regionValue);
                }
                else
                {
                    commandList.UpdateSubresource(this, subResourceIndex, new DataBox(fromData.Pointer, rowStride, textureDepthStride));
                }
            }
            else
            {
                var mappedResource = commandList.MapSubresource(this, subResourceIndex, this.Usage == GraphicsResourceUsage.Dynamic ? MapMode.WriteDiscard : MapMode.Write);
                var box = mappedResource.DataBox;

                // If depth == 1 (Texture, Texture or TextureCube), then depthStride is not used
                var boxDepthStride = this.Depth == 1 ? box.SlicePitch : textureDepthStride;

                // The fast way: If same stride, we can directly copy the whole texture in one shot
                if (box.RowPitch == rowStride && boxDepthStride == textureDepthStride)
                {
                    Utilities.CopyMemory(box.DataPointer, fromData.Pointer, sizeOfTextureData);
                }
                else
                {
                    // Otherwise, the long way by copying each scanline
                    var destPerDepthPtr = (byte*)box.DataPointer;
                    var sourcePtr = (byte*)fromData.Pointer;

                    // Iterate on all depths
                    for (int j = 0; j < depth; j++)
                    {
                        var destPtr = destPerDepthPtr;
                        // Iterate on each line
                        for (int i = 0; i < height; i++)
                        {
                            Utilities.CopyMemory((IntPtr)destPtr, (IntPtr)sourcePtr, rowStride);
                            destPtr += box.RowPitch;
                            sourcePtr += rowStride;
                        }
                        destPerDepthPtr += box.SlicePitch;
                    }
                }
                commandList.UnmapSubresource(mappedResource);
            }
        }

        /// <summary>
        /// Makes a copy of this texture.
        /// </summary>
        /// <remarks>
        /// This method doesn't copy the content of the texture.
        /// </remarks>
        /// <returns>
        /// A copy of this texture.
        /// </returns>
        public Texture Clone()
        {
            return new Texture(GraphicsDevice).InitializeFrom(textureDescription.ToCloneableDescription(), ViewDescription);
        }

        /// <summary>
        /// Return an equivalent staging texture CPU read-writable from this instance.
        /// </summary>
        /// <returns>The equivalent staging texture.</returns>
        public Texture ToStaging()
        {
            return new Texture(this.GraphicsDevice).InitializeFrom(textureDescription.ToStagingDescription(), ViewDescription.ToStagingDescription());
        }

        /// <summary>
        /// Loads a texture from a stream.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="stream">The stream to load the texture from.</param>
        /// <param name="textureFlags">True to load the texture with unordered access enabled. Default is false.</param>
        /// <param name="usage">Usage of the resource. Default is <see cref="GraphicsResourceUsage.Immutable"/> </param>
        /// <param name="loadAsSRGB">Indicate if the texture should be loaded as an sRGB texture. If false, the texture is load in its default format.</param>
        /// <returns>A texture</returns>
        public static Texture Load(GraphicsDevice device, Stream stream, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable, bool loadAsSRGB = false)
        {
            using (var image = Image.Load(stream, loadAsSRGB))
                return New(device, image, textureFlags, usage);
        }

        /// <summary>
        /// Loads a texture from a stream.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="image">The image.</param>
        /// <param name="textureFlags">True to load the texture with unordered access enabled. Default is false.</param>
        /// <param name="usage">Usage of the resource. Default is <see cref="GraphicsResourceUsage.Immutable" /></param>
        /// <returns>A texture</returns>
        /// <exception cref="System.InvalidOperationException">Dimension not supported</exception>
        public static Texture New(GraphicsDevice device, Image image, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (image == null) throw new ArgumentNullException("image");

            return New(device, image.Description, image.ToDataBox());
        }

        /// <summary>
        /// Creates a new texture with the specified generic texture description.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="description">The description.</param>
        /// <param name="boxes">The data boxes.</param>
        /// <returns>A Texture instance, either a RenderTarget or DepthStencilBuffer or Texture, depending on Binding flags.</returns>
        /// <exception cref="System.ArgumentNullException">graphicsDevice</exception>
        public static Texture New(GraphicsDevice graphicsDevice, TextureDescription description, params DataBox[] boxes)
        {
            return New(graphicsDevice, description, new TextureViewDescription(), boxes);
        }

        /// <summary>
        /// Creates a new texture with the specified generic texture description.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="description">The description.</param>
        /// <param name="viewDescription">The view description.</param>
        /// <param name="boxes">The data boxes.</param>
        /// <returns>A Texture instance, either a RenderTarget or DepthStencilBuffer or Texture, depending on Binding flags.</returns>
        /// <exception cref="System.ArgumentNullException">graphicsDevice</exception>
        public static Texture New(GraphicsDevice graphicsDevice, TextureDescription description, TextureViewDescription viewDescription, params DataBox[] boxes)
        {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }

            return new Texture(graphicsDevice).InitializeFrom(description, viewDescription, boxes);
        }

#if XENKO_PLATFORM_ANDROID //&& USE_GLES_EXT_OES_TEXTURE
        //create a new GL_TEXTURE_EXTERNAL_OES texture which will be managed by external API
        //TODO: check how to integrate this properly in Xenko API
        public static Texture NewExternalOES(GraphicsDevice graphicsDevice)
        {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }

            return new Texture(graphicsDevice).InitializeForExternalOES();
        }
#endif

        /// <summary>
        /// Saves this texture to a stream with a specified format.
        /// </summary>
        /// <param name="commandList">The command list.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="fileType">Type of the image file.</param>
        public void Save(CommandList commandList, Stream stream, ImageFileType fileType)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            using (var staging = ToStaging())
                Save(commandList, stream, staging, fileType);
        }

        /// <summary>
        /// Gets the GPU content of this texture as an <see cref="Image"/> on the CPU.
        /// </summary>
        public Image GetDataAsImage(CommandList commandList)
        {
            if (Usage == GraphicsResourceUsage.Staging)
                return GetDataAsImage(commandList, this); // Directly if this is a staging resource

            using (var stagingTexture = ToStaging())
                return GetDataAsImage(commandList, stagingTexture);
        }

        /// <summary>
        /// Gets the GPU content of this texture to an <see cref="Image"/> on the CPU.
        /// </summary>
        /// <param name="commandList">The command list.</param>
        /// <param name="stagingTexture">The staging texture used to temporary transfer the image from the GPU to CPU.</param>
        /// <exception cref="ArgumentException">If stagingTexture is not a staging texture.</exception>
        public Image GetDataAsImage(CommandList commandList, Texture stagingTexture)
        {
            if (stagingTexture == null) throw new ArgumentNullException("stagingTexture");
            if (stagingTexture.Usage != GraphicsResourceUsage.Staging)
                throw new ArgumentException("Invalid texture used as staging. Must have Usage = GraphicsResourceUsage.Staging", "stagingTexture");

            var image = Image.New(stagingTexture.Description);
            try
            {
                for (int arrayIndex = 0; arrayIndex < image.Description.ArraySize; arrayIndex++)
                {
                    for (int mipLevel = 0; mipLevel < image.Description.MipLevels; mipLevel++)
                    {
                        var pixelBuffer = image.PixelBuffer[arrayIndex, mipLevel];
                        GetData(commandList, stagingTexture, new DataPointer(pixelBuffer.DataPointer, pixelBuffer.BufferStride), arrayIndex, mipLevel);
                    }
                }
            }
            catch (Exception)
            {
                // If there was an exception, free the allocated image to avoid any memory leak.
                image.Dispose();
                throw;
            }
            return image;
        }

        /// <summary>
        /// Saves this texture to a stream with a specified format.
        /// </summary>
        /// <param name="commandList">The command list.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="stagingTexture">The staging texture used to temporary transfer the image from the GPU to CPU.</param>
        /// <param name="fileType">Type of the image file.</param>
        /// <exception cref="ArgumentException">If stagingTexture is not a staging texture.</exception>
        public void Save(CommandList commandList, Stream stream, Texture stagingTexture, ImageFileType fileType)
        {
            using (var image = GetDataAsImage(commandList, stagingTexture))
                image.Save(stream, fileType);
        }

        /// <summary>
        /// Calculates the mip map count from a requested level.
        /// </summary>
        /// <param name="requestedLevel">The requested level.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <returns>The resulting mipmap count (clamp to [1, maxMipMapCount] for this texture)</returns>
        internal static int CalculateMipMapCount(MipMapCount requestedLevel, int width, int height = 0, int depth = 0)
        {
            int size = Math.Max(Math.Max(width, height), depth);
            //int maxMipMap = 1 + (int)Math.Ceiling(Math.Log(size) / Math.Log(2.0));
            int maxMipMap = CountMips(size);

            return requestedLevel == 0 ? maxMipMap : Math.Min(requestedLevel, maxMipMap);
        }

        private static DataBox GetDataBox<T>(PixelFormat format, int width, int height, int depth, T[] textureData, IntPtr fixedPointer) where T : struct
        {
            // Check that the textureData size is correct
            if (textureData == null) throw new ArgumentNullException("textureData");
            Image.ComputePitch(format, width, height, out var rowPitch, out var slicePitch, out _, out _);
            if (Utilities.SizeOf(textureData) != (slicePitch * depth)) throw new ArgumentException("Invalid size for Image");

            return new DataBox(fixedPointer, rowPitch, slicePitch);
        }

        /// <summary>
        /// Swaps the texture internal data with the other texture.
        /// </summary>
        /// <param name="other">The other texture.</param>
        internal void Swap([NotNull] Texture other)
        {
            Utilities.Swap(ref textureDescription, ref other.textureDescription);
            Utilities.Swap(ref textureViewDescription, ref other.textureViewDescription);
            Utilities.Swap(ref mipmapDescriptions, ref other.mipmapDescriptions);
            Utilities.Swap(ref fullQualitySize, ref other.fullQualitySize);

            var temp = ViewWidth;
            ViewWidth = other.ViewWidth;
            other.ViewWidth = temp;

            temp = ViewHeight;
            ViewHeight = other.ViewHeight;
            other.ViewHeight = temp;

            temp = ViewDepth;
            ViewDepth = other.ViewDepth;
            other.ViewDepth = temp;

            temp = SizeInBytes;
            SizeInBytes = other.SizeInBytes;
            other.SizeInBytes = temp;

            SwapInternal(other);
        }

        internal void GetViewSliceBounds(ViewType viewType, ref int arrayOrDepthIndex, ref int mipIndex, out int arrayOrDepthCount, out int mipCount)
        {
            int arrayOrDepthSize = this.Depth > 1 ? this.Depth : this.ArraySize;

            switch (viewType)
            {
                case ViewType.Full:
                    arrayOrDepthIndex = 0;
                    mipIndex = 0;
                    arrayOrDepthCount = arrayOrDepthSize;
                    mipCount = this.MipLevels;
                    break;
                case ViewType.Single:
                    arrayOrDepthCount = 1;
                    mipCount = 1;
                    break;
                case ViewType.MipBand:
                    arrayOrDepthCount = arrayOrDepthSize - arrayOrDepthIndex;
                    mipCount = 1;
                    break;
                case ViewType.ArrayBand:
                    arrayOrDepthCount = 1;
                    mipCount = MipLevels - mipIndex;
                    break;
                default:
                    arrayOrDepthCount = 0;
                    mipCount = 0;
                    break;
            }
        }

        internal int GetViewCount()
        {
            int arrayOrDepthSize = this.Depth > 1 ? this.Depth : this.ArraySize;
            return GetViewIndex((ViewType)4, arrayOrDepthSize, this.MipLevels);
        }

        internal int GetViewIndex(ViewType viewType, int arrayOrDepthIndex, int mipIndex)
        {
            int arrayOrDepthSize = this.Depth > 1 ? this.Depth : this.ArraySize;
            return (((int)viewType) * arrayOrDepthSize + arrayOrDepthIndex) * this.MipLevels + mipIndex;
        }

        internal static GraphicsResourceUsage GetUsageWithFlags(GraphicsResourceUsage usage, TextureFlags flags)
        {
            // If we have a texture supporting render target or unordered access, force to UsageDefault
            if ((flags & TextureFlags.RenderTarget) != 0 || (flags & TextureFlags.UnorderedAccess) != 0)
                return GraphicsResourceUsage.Default;
            return usage;
        }

        internal int ComputeSubresourceSize(int subresource)
        {
            var mipLevel = subresource % MipLevels;

            var slicePitch = ComputeSlicePitch(mipLevel);
            var depth = CalculateMipSize(Description.Depth, mipLevel);

            return (slicePitch * depth + TextureSubresourceAlignment - 1) / TextureSubresourceAlignment * TextureSubresourceAlignment;
        }

        internal int ComputeBufferOffset(int subresource, int depthSlice)
        {
            int offset = 0;

            for (var i = 0; i < subresource; ++i)
            {
                offset += ComputeSubresourceSize(i);
            }

            if (depthSlice != 0)
                offset += ComputeSlicePitch(subresource % Description.MipLevels) * depthSlice;

            return offset;
        }

        internal int ComputeSlicePitch(int mipLevel)
        {
            return ComputeRowPitch(mipLevel) * CalculateMipSize(Height, mipLevel);
        }

        internal int ComputeRowPitch(int mipLevel)
        {
            // Round up to 256
            return ((CalculateMipSize(Width, mipLevel) * TexturePixelSize) + TextureRowPitchAlignment - 1) / TextureRowPitchAlignment * TextureRowPitchAlignment;
        }

        internal int ComputeBufferTotalSize()
        {
            int result = 0;

            for (int i = 0; i < Description.MipLevels; ++i)
            {
                result += ComputeSubresourceSize(i);
            }

            return result * Description.ArraySize;
        }

        public static int CountMips(int width)
        {
            int mipLevels = 1;

            while (width > 1)
            {
                ++mipLevels;

                width >>= 1;
            }

            return mipLevels;
        }

        public static int CountMips(int width, int height)
        {
            return CountMips(Math.Max(width, height));
        }

        public static int CountMips(int width, int height, int depth)
        {
            return CountMips(Math.Max(width, Math.Max(height, depth)));
        }
    }
}
