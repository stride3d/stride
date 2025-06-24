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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.ReferenceCounting;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Data;
using Utilities = Stride.Core.Utilities;

namespace Stride.Graphics
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
        public ref readonly TextureDescription Description => ref textureDescription;

        /// <summary>
        /// Gets the view description.
        /// </summary>
        /// <value>The view description.</value>
        public ref readonly TextureViewDescription ViewDescription => ref textureViewDescription;

        /// <summary>
        /// The dimension of a texture.
        /// </summary>
        public TextureDimension Dimension
            // TODO: What's the point of storing the dimensions?
            // We could just as well generate the "TextureDimension" based on the "TextureTarget" property,
            // because "TextureDimension" is fully dependent on the "TextureTarget" property.
            // E.g. "Texture2D" and "Texture2DMultisample" both return "TextureDimension.Texture2D".
            // TODO: Stale comment?
            => textureDescription.Dimension;

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
        public PixelFormat ViewFormat => textureViewDescription.Format;

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The type of the view.</value>
        public TextureFlags ViewFlags => textureViewDescription.Flags;

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The type of the view.</value>
        public ViewType ViewType => textureViewDescription.Type;

        /// <summary>
        /// The dimension of the texture view.
        /// </summary>
        public TextureDimension ViewDimension
            => Dimension == TextureDimension.TextureCube && ViewType != ViewType.Full
                ? TextureDimension.Texture2D   // For cube-maps, if not full, the View is over a single cube face
                : Dimension;

        /// <summary>
        /// The miplevel index of this texture view.
        /// </summary>
        /// <value>The mip level.</value>
        public int MipLevel => textureViewDescription.MipLevel;

        /// <summary>
        /// The array index of this texture view.
        /// </summary>
        /// <value>The array slice.</value>
        public int ArraySlice => textureViewDescription.ArraySlice;

        /// <summary>
        /// The width of the texture.
        /// </summary>
        /// <value>The width.</value>
        public int Width => textureDescription.Width;

        /// <summary>
        /// The height of the texture.
        /// </summary>
        /// <value>The height.</value>
        public int Height => textureDescription.Height;

        /// <summary>
        /// The depth of the texture.
        /// </summary>
        /// <value>The depth.</value>
        public int Depth => textureDescription.Depth;

        /// <summary>
        /// Number of textures in the array.
        /// </summary>
        /// <value>The size of the array.</value>
        /// <remarks>This field is only valid for 1D, 2D and Cube <see cref="Texture" />.</remarks>
        public int ArraySize => textureDescription.ArraySize;

        /// <summary>
        /// The maximum number of mipmap levels in the texture.
        /// </summary>
        /// <value>The mip levels.</value>
        public int MipLevelCount => textureDescription.MipLevelCount;

        /// <summary>
        /// Texture format (see <see cref="PixelFormat" />)
        /// </summary>
        /// <value>The format.</value>
        public PixelFormat Format => textureDescription.Format;

        /// <summary>
        /// Structure that specifies multisampling parameters for the texture.
        /// </summary>
        /// <value>The multi sample level.</value>
        /// <remarks>This field is only valid for a 2D <see cref="Texture" />.</remarks>
        public MultisampleCount MultisampleCount => textureDescription.MultisampleCount;

        /// <summary>
        /// Value that identifies how the texture is to be read from and written to.
        /// </summary>
        public GraphicsResourceUsage Usage => textureDescription.Usage;

        /// <summary>
        /// Texture flags.
        /// </summary>
        public TextureFlags Flags => textureDescription.Flags;

        /// <summary>
        /// Resource options for DirectX 11 textures.
        /// </summary>
        public TextureOptions Options => textureDescription.Options;

        /// <summary>
        /// The shared handle if created with TextureOption.Shared or TextureOption.SharedNthandle, IntPtr.Zero otherwise.
        /// </summary>
        public IntPtr SharedHandle { get; private set; } = IntPtr.Zero;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        /// <summary>
        /// Gets the name of the shared Nt handle when created with TextureOption.SharedNthandle.
        /// </summary>
        public string SharedNtHandleName { get; private set; } = string.Empty;
#endif

        /// <summary>
        /// Gets a value indicating whether this instance is a render target.
        /// </summary>
        /// <value><c>true</c> if this instance is render target; otherwise, <c>false</c>.</value>
        public bool IsRenderTarget => ViewFlags.HasFlag(TextureFlags.RenderTarget);

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil; otherwise, <c>false</c>.</value>
        public bool IsDepthStencil => ViewFlags.HasFlag(TextureFlags.DepthStencil);

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil readonly.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil readonly; otherwise, <c>false</c>.</value>
        public bool IsDepthStencilReadOnly => (ViewFlags & TextureFlags.DepthStencilReadOnly) == TextureFlags.DepthStencilReadOnly;

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
        public bool IsShaderResource => ViewFlags.HasFlag(TextureFlags.ShaderResource);

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
        public bool IsUnorderedAccess => ViewFlags.HasFlag(TextureFlags.UnorderedAccess);

        /// <summary>
        /// Gets a value indicating whether this instance is a multi sample texture.
        /// </summary>
        /// <value><c>true</c> if this instance is multi sample texture; otherwise, <c>false</c>.</value>
        public bool IsMultiSampled => MultisampleCount > MultisampleCount.None;

        /// <summary>
        /// Gets a boolean indicating whether this <see cref="Texture"/> is a using a block compress format (BC1, BC2, BC3, BC4, BC5, BC6H, BC7).
        /// </summary>
        public bool IsBlockCompressed { get; private set; }

        /// <summary>
        /// Gets the size of this texture.
        /// </summary>
        /// <value>The size.</value>
        
        public Size3 Size => new(ViewWidth, ViewHeight, ViewDepth);

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


        // Needed for serialization
        public Texture() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        internal Texture(GraphicsDevice device) : base(device) { }

        protected override void Destroy()
        {
            base.Destroy();

            ParentTexture?.ReleaseInternal();
        }

        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            OnRecreateImpl();
            return true;
        }

        private partial void OnRecreateImpl();

        internal Texture InitializeFrom(TextureDescription description, DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture: null, description, new TextureViewDescription(), textureDatas);
        }

#if STRIDE_PLATFORM_ANDROID //&& USE_GLES_EXT_OES_TEXTURE
        internal Texture InitializeForExternalOES()
        {
            InitializeForExternalOESImpl();
            return this;
        }
#endif

        internal Texture InitializeFrom(TextureDescription description, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture: null, description, viewDescription, textureDatas);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture, parentTexture.Description, viewDescription, textureDatas);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureDescription description, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            ParentTexture = parentTexture;
            ParentTexture?.AddReferenceInternal();

            textureDescription = description;
            textureViewDescription = viewDescription;
            IsBlockCompressed = description.Format.IsCompressed();
            RowStride = ComputeRowPitch(0);
            mipmapDescriptions = Image.CalculateMipMapDescription(description);
            SizeInBytes = ArraySize * mipmapDescriptions?.Sum(mip => mip.MipmapSize) ?? 0;

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

            // Check that the Texture View flags are compatible with the parent Texture's flags
            var filterViewFlags = (TextureFlags)((int)ViewFlags & (~DepthStencilReadOnlyFlags));
            if ((Flags & filterViewFlags) != filterViewFlags)
            {
                throw new NotSupportedException(
                    $"Cannot create a Texture View with flags [{ViewFlags}] from the parent Texture with flags [{Flags}]. " +
                    $"The parent Texture must include all the flags defined by the Texture View");
            }

            if (IsMultiSampled)
            {
                var maxCount = GraphicsDevice.Features[Format].MultisampleCountMax;
                if (maxCount < MultisampleCount)
                    throw new NotSupportedException(
                        $"Cannot create a Texture with format {Format} and multi-sample level {MultisampleCount}. " +
                        $"The maximum supported level is {maxCount}");
            }

            InitializeFromImpl(textureDatas);

            return this;
        }

        /// <summary>
        /// Releases the texture data.
        private partial void InitializeFromImpl(DataBox[] dataBoxes);

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
        public ref readonly MipMapDescription GetMipMapDescription(int mipLevel)
        {
            return ref mipmapDescriptions[mipLevel];
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
        public static int CountMipLevels(int width, MipMapCount mipLevels)
        {
            switch (mipLevels.Count)
            {
                case > 1: // Specific number
                {
                    var maxMipLevels = CountMipLevels(width);
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(mipLevels.Count, maxMipLevels, nameof(mipLevels));
                    return mipLevels.Count;
                }
                case 0:
                    return CountMipLevels(width);  // All mips

                default:
                    return 1;  // Single mip
            }
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CountMipLevels(int width)
        {
            int mipLevels = 1;

            while (width > 1)
            {
                ++mipLevels;

                width >>= 1;
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
        public static int CountMipLevels(int width, int height, MipMapCount mipLevels)
        {
            switch (mipLevels.Count)
            {
                case > 1:  // Specific number
                {
                    var maxMipLevels = CountMipLevels(width, height);
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(mipLevels.Count, maxMipLevels, nameof(mipLevels));
                    return mipLevels.Count;
                }
                case 0:
                    return CountMipLevels(width, height);  // All mips

                default:
                    return 1;  // Single mip
            }
        }

        public static int CountMipLevels(int width, int height)
        {
            return CountMipLevels(Math.Max(width, height));
        }

        public static int CountMipLevels(int width, int height, int depth, MipMapCount mipLevels)
        {
            switch (mipLevels.Count)
            {
                case > 1:  // Specific number
                {
                    if (!int.IsPow2(width) || !int.IsPow2(height) || !int.IsPow2(depth))
                        throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                    var maxMipLevels = CountMipLevels(width, height, depth);
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(mipLevels.Count, maxMipLevels, nameof(mipLevels));
                    return mipLevels.Count;
                }
                case 0:
                    if (!int.IsPow2(width) || !int.IsPow2(height) || !int.IsPow2(depth))
                        throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                    return CountMipLevels(width, height, depth);  // All mips

                default:
                    return 1;  // Single mip
            }
        }

        /// <summary>
        /// Gets the absolute sub-resource index from the array and mip slice.
        /// </summary>
        /// <param name="arraySlice">The array slice index.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <returns>A value equals to arraySlice * Description.MipLevels + mipSlice.</returns>
        public static int CountMipLevels(int width, int height, int depth)
        {
            return CountMipLevels(Math.Max(width, Math.Max(height, depth)));
        }

        public int GetSubResourceIndex(int arrayIndex, int mipLevel)
        {
            return arrayIndex * MipLevelCount + mipLevel;
        }

        /// <summary>
        /// Calculates the expected width of a texture using a specified type.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <returns>The expected width</returns>
        /// <exception cref="System.ArgumentException">If the size is invalid</exception>
        public unsafe int CalculateWidth<TData>(int mipLevel = 0) where TData : unmanaged
        {
            var mipWidth = CalculateMipSize(Width, mipLevel);

            var rowStride = mipWidth * Format.SizeInBytes();
            var dataStrideInBytes = mipWidth * sizeof(TData);

            var (width, rem) = Math.DivRem(rowStride * mipWidth, dataStrideInBytes);

            if (rem != 0)
                throw new ArgumentException("sizeof(TData) / sizeof(Format) * Width is not an integer");

            return width;
        }

        /// <summary>
        /// Calculates the number of pixel data this texture is requiring for a particular mip level.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <param name="mipLevel">The mip level.</param>
        /// <returns>The number of pixel data.</returns>
        /// <remarks>This method is used to allocated a texture data buffer to hold pixel datas: var textureData = new T[ texture.CalculatePixelCount&lt;T&gt;() ] ;.</remarks>
        public int CalculatePixelDataCount<TData>(int mipLevel = 0) where TData : unmanaged
        {
            return CalculateWidth<TData>(mipLevel)
                 * CalculateMipSize(Height, mipLevel)
                 * CalculateMipSize(Depth, mipLevel);
        }

        /// <summary>
        /// Gets the content of this texture to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="commandList">The command list.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <returns>The texture data.</returns>
        #region GetData: Reading data from the Texture

        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public TData[] GetData<TData>(CommandList commandList, int arrayIndex = 0, int mipLevel = 0) where TData : unmanaged
        {
            var toData = new TData[CalculatePixelDataCount<TData>(mipLevel)];
            GetData(commandList, toData, arrayIndex, mipLevel);
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
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <paramref name="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public bool GetData<TData>(CommandList commandList, TData[] toData, int arrayIndex = 0, int mipLevel = 0, bool doNotWait = false) where TData : unmanaged
        {
            if (Usage == GraphicsResourceUsage.Staging)
            {
                // Get the data directly if this is a staging Resource
                return GetData(commandList, stagingTexture: this, toData, arrayIndex, mipLevel, doNotWait);
            }
            else
            {
                // Inefficient way to use the Copy method using a dynamic staging Texture
                using var throughStaging = ToStaging();
                return GetData(commandList, throughStaging, toData, arrayIndex, mipLevel, doNotWait);
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
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <paramref name="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>

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
        public unsafe bool GetData<TData>(CommandList commandList, Texture stagingTexture, TData[] toData, int arrayIndex = 0, int mipLevel = 0, bool doNotWait = false) where TData : unmanaged
        {
            return GetData(commandList, stagingTexture, toData.AsSpan(), arrayIndex, mipLevel, doNotWait);
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
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <paramref name="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        [Obsolete("This method is obsolete. Use the Span-based methods instead")]
        public unsafe bool GetData(CommandList commandList, Texture stagingTexture, DataPointer toData, int arrayIndex = 0, int mipLevel = 0, bool doNotWait = false)
        {
            return GetData(commandList, stagingTexture, new Span<byte>((void*)toData.Pointer, toData.Size), arrayIndex, mipLevel, doNotWait);
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
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <paramref name="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe bool GetData<T>(CommandList commandList, Texture stagingTexture, Span<T> toData, int arrayIndex = 0, int mipLevel = 0, bool doNotWait = false) where T : unmanaged
        {
            ArgumentNullException.ThrowIfNull(stagingTexture);

            // Get a description for the specified mip-level
            ref readonly var mipmap = ref GetMipMapDescription(mipLevel);

            int height = mipmap.HeightPacked;
            int depth = mipmap.Depth;
            int rowStride = mipmap.RowStride;
            int textureDepthStride = mipmap.DepthStride;
            int mipMapSize = mipmap.MipmapSize;

            int destLengthInBytes = toData.Length * sizeof(T);

            if (destLengthInBytes > mipMapSize)
                throw new ArgumentException($"The length of the destination buffer ({destLengthInBytes} bytes) is not compatible with " +
                    $"the expected largestSize ({mipMapSize} bytes) : Width * Height * Depth * sizeof(Format) largestSize in bytes");

            // Copy the actual content of the texture to the staging Resource
            if (!ReferenceEquals(this, stagingTexture))
                commandList.Copy(this, stagingTexture);

            int subResourceIndex = GetSubResourceIndex(arrayIndex, mipLevel);

            // Map the staging Resource to CPU-accessible memory
            var mappedResource = commandList.MapSubResource(stagingTexture, subResourceIndex, MapMode.Read, doNotWait);

            // Box can be empty if `doNotWait` is true: Return false if empty
            var box = mappedResource.DataBox;
            if (box.IsEmpty)
                return false;

            // If depth == 1 (like for 1D, 2D, or Cube), then depthStride is not used
            var boxDepthStride = Depth == 1 ? box.SlicePitch : textureDepthStride;

            var isFlippedTexture = IsFlipped();

            // The fast way: If same stride, we can directly copy the whole texture in one shot
            if (box.RowPitch == rowStride && boxDepthStride == textureDepthStride && !isFlippedTexture)
            {
                fixed (void* destPtr = toData)
                    Utilities.CopyWithAlignmentFallback(destPtr, (void*) box.DataPointer, (uint) mipMapSize);
            }
            else
            {
                // Otherwise, the long way by copying each scanline
                var sourcePerDepthPtr = (byte*) box.DataPointer;
                fixed (T* ptr = toData)
                {
                    byte* destPtr = (byte*) ptr;

                    // Iterate on all depths
                    for (int j = 0; j < depth; j++)
                    {
                        var sourcePtr = sourcePerDepthPtr;

                        // Iterate on each line
                        if (isFlippedTexture)
                        {
                            sourcePtr += box.RowPitch * (height - 1);
                            for (int i = height - 1; i >= 0; i--)
                            {
                                // Copy a single row
                                Utilities.CopyWithAlignmentFallback(destPtr, sourcePtr, (uint) rowStride);
                                sourcePtr -= box.RowPitch;
                                destPtr += rowStride;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < height; i++)
                            {
                                // Copy a single row
                                Utilities.CopyWithAlignmentFallback(destPtr, sourcePtr, (uint) rowStride);
                                sourcePtr += box.RowPitch;
                                destPtr += rowStride;
                            }
                        }
                        sourcePerDepthPtr += box.SlicePitch;
                    }
                }
            }

            commandList.UnmapSubResource(mappedResource);

            return true;
        }

        #endregion

        #region SetData: Writing data into the Texture

        /// <summary>
        ///   Copies the contents of an array of data on CPU memory into the Texture in GPU memory.
        /// </summary>
        public unsafe void SetData<TData>(CommandList commandList, TData[] fromData, int arrayIndex = 0, int mipLevel = 0, ResourceRegion? region = null) where TData : unmanaged
        {
            SetData<TData>(commandList, fromData.AsSpan(), arrayIndex, mipLevel, region);
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
        [Obsolete("This method is obsolete. Use the Span-based methods instead")]
        public unsafe void SetData(CommandList commandList, DataPointer fromData, int arrayIndex = 0, int mipLevel = 0, ResourceRegion? region = null)
        {
            SetData(commandList, new ReadOnlySpan<byte>((void*) fromData.Pointer, fromData.Size), arrayIndex, mipLevel, region);
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
        public unsafe void SetData<TData>(CommandList commandList, ReadOnlySpan<TData> fromData, int arrayIndex = 0, int mipLevel = 0, ResourceRegion? region = null) where TData : unmanaged
        {
            ArgumentNullException.ThrowIfNull(commandList);

            if (region.HasValue && Usage != GraphicsResourceUsage.Default)
                throw new ArgumentException($"A region can only be specified for Textures with {nameof(GraphicsResourceUsage)}.{nameof(GraphicsResourceUsage.Default)}", nameof(region));

            // Get a description for the specified mip-level
            ref readonly var mipmap = ref GetMipMapDescription(mipLevel);

            int width = mipmap.Width;
            int height = mipmap.Height;
            int depth = mipmap.Depth;

            // If we are using a region, then check that parameters are fine
            if (region is ResourceRegion regionToCheck)
            {
                if (regionToCheck.Width > width)
                    throw new ArgumentOutOfRangeException(nameof(region), $"The region's width [{regionToCheck.Width}] cannot be greater than the mip-level's width [{width}]");
                if (regionToCheck.Height > height)
                    throw new ArgumentOutOfRangeException(nameof(region), $"The region's height [{regionToCheck.Height}] cannot be greater than the mip-level's height [{height}]");
                if (regionToCheck.Depth > depth)
                    throw new ArgumentOutOfRangeException(nameof(region), $"The region's depth [{regionToCheck.Depth}] cannot be greater than the mip-level's depth [{depth}]");

                width = regionToCheck.Width;
                height = regionToCheck.Height;
                depth = regionToCheck.Depth;
            }

            var sizePerElement = Format.SizeInBytes();

            // Compute actual pitch
            Image.ComputePitch(Format, width, height, out var rowStride, out var textureDepthStride, out width, out height);

            // Size Of actual texture data
            int sizeOfTextureData = textureDepthStride * depth;

            int fromDataSizeInBytes = fromData.Length * sizeof(TData);

            // Check largestSize validity of data to copy from
            if (fromDataSizeInBytes < sizeOfTextureData)
                throw new ArgumentException($"The length of the source data buffer ({fromDataSizeInBytes} bytes) is not compatible with the expected largestSize " +
                    $"of at least {sizeOfTextureData} bytes : Width * Height * Depth * sizeof(Format) largestSize in bytes");

            int subResourceIndex = GetSubResourceIndex(arrayIndex, mipLevel);

            fixed (void* ptrFromData = fromData)
            {
                // If the Texture is declared as Default usage, we use UpdateSubresource that supports sub-Resource region
                if (Usage == GraphicsResourceUsage.Default)
                {
                    if (region is ResourceRegion validRegion)
                    {
                        commandList.UpdateSubResource(resource: this, subResourceIndex, new DataBox((nint) ptrFromData, rowStride, textureDepthStride), validRegion);
                    }
                    else
                    {
                        commandList.UpdateSubResource(resource: this, subResourceIndex, new DataBox((nint) ptrFromData, rowStride, textureDepthStride));
                    }
                }
                else
                {
                    var mappedResource = commandList.MapSubResource(resource: this, subResourceIndex, Usage == GraphicsResourceUsage.Dynamic ? MapMode.WriteDiscard : MapMode.Write);
                    var box = mappedResource.DataBox;

                    // If depth == 1 (like for 1D, 2D, or Cube), then depthStride is not used
                    var boxDepthStride = Depth == 1 ? box.SlicePitch : textureDepthStride;

                    // The fast way: If same stride, we can directly copy the whole texture in one shot
                    if (box.RowPitch == rowStride && boxDepthStride == textureDepthStride)
                    {
                        Utilities.CopyWithAlignmentFallback((void*) box.DataPointer, ptrFromData, (uint) sizeOfTextureData);
                    }
                    else
                    {
                        // Otherwise, the long way by copying each scanline
                        var destPerDepthPtr = (byte*) box.DataPointer;
                        var sourcePtr = (byte*) ptrFromData;

                        // Iterate on all depths
                        for (int z = 0; z < depth; z++)
                        {
                            var destPtr = destPerDepthPtr;

                            // Iterate on each line
                            for (int y = 0; y < height; y++)
                            {
                                Utilities.CopyWithAlignmentFallback(destPtr, sourcePtr, (uint) rowStride);
                                destPtr += box.RowPitch;
                                sourcePtr += rowStride;
                            }
                            destPerDepthPtr += box.SlicePitch;
                        }
                    }
                    commandList.UnmapSubResource(mappedResource);
                }
            }
        }

        #endregion

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
            return new Texture(GraphicsDevice).InitializeFrom(textureDescription.ToStagingDescription(), ViewDescription.ToStagingDescription());
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
        public static Texture Load(GraphicsDevice device, Stream stream, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable, bool loadAsSrgb = false)
        {
            using var image = Image.Load(stream, loadAsSrgb);

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
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(image);

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
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            return new Texture(graphicsDevice).InitializeFrom(description, viewDescription, boxes);
        }

#if STRIDE_PLATFORM_ANDROID //&& USE_GLES_EXT_OES_TEXTURE
        //create a new GL_TEXTURE_EXTERNAL_OES texture which will be managed by external API
        //TODO: check how to integrate this properly in Stride API
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
            ArgumentNullException.ThrowIfNull(stream);

            using var staging = ToStaging();
            Save(commandList, stream, staging, fileType);
        }

        /// <summary>
        /// Gets the GPU content of this texture as an <see cref="Image"/> on the CPU.
        /// </summary>
        public Image GetDataAsImage(CommandList commandList)
        {
            // Directly if this is a staging Resource
            if (Usage == GraphicsResourceUsage.Staging)
                return GetDataAsImage(commandList, stagingTexture: this);

            using var stagingTexture = ToStaging();
            return GetDataAsImage(commandList, stagingTexture);
        }

        /// <summary>
        /// Gets the GPU content of this texture to an <see cref="Image"/> on the CPU.
        /// </summary>
        /// <param name="commandList">The command list.</param>
        /// <param name="stagingTexture">The staging texture used to temporary transfer the image from the GPU to CPU.</param>
        /// <exception cref="ArgumentException">If stagingTexture is not a staging texture.</exception>
        public unsafe Image GetDataAsImage(CommandList commandList, Texture stagingTexture)
        {
            ArgumentNullException.ThrowIfNull(stagingTexture);

            if (stagingTexture.Usage != GraphicsResourceUsage.Staging)
                throw new ArgumentException("Invalid Texture used as staging Resource. It must have GraphicsResourceUsage.Staging", nameof(stagingTexture));

            var image = Image.New(stagingTexture.Description);
            try
            {
                for (int arrayIndex = 0; arrayIndex < image.Description.ArraySize; arrayIndex++)
                {
                    for (int mipLevel = 0; mipLevel < image.Description.MipLevels; mipLevel++)
                    {
                        var pixelBuffer = image.PixelBuffer[arrayIndex, mipLevel];
                        GetData(commandList, stagingTexture, new Span<byte>((byte*) pixelBuffer.DataPointer, pixelBuffer.BufferStride), arrayIndex, mipLevel);
                    }
                }
            }
            catch
            {
                // If there was an exception, free the allocated image to avoid any memory leak
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
            using var image = GetDataAsImage(commandList, stagingTexture);
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
            int largestSize = Math.Max(Math.Max(width, height), depth);

            int maxMipLevelCount = CountMipLevels(largestSize);

            // If all mip-levels requested (0), accept the full count, else limit to `requestedLevel`
            return requestedLevel == 0 ? maxMipLevelCount : Math.Min(requestedLevel, maxMipLevelCount);
        }

        private static unsafe DataBox GetDataBox<TData>(PixelFormat format, int width, int height, int depth, TData[] textureData, IntPtr fixedPointer) where TData : unmanaged
        {
            ArgumentNullException.ThrowIfNull(textureData);

            // Check that the textureData has the correct size for the Texture's data
            Image.ComputePitch(format, width, height, out var rowPitch, out var slicePitch, out _, out _);

            if (sizeof(TData) * textureData.Length != (slicePitch * depth))
                throw new ArgumentException("Invalid Texture data length", nameof(textureData));

            return new DataBox(fixedPointer, rowPitch, slicePitch);
        }

        /// <summary>
        /// Swaps the texture internal data with the other texture.
        /// </summary>
        /// <param name="other">The other texture.</param>
        internal void Swap([NotNull] Texture other)
        {
            (textureDescription, other.textureDescription) = (other.textureDescription, textureDescription);
            (textureViewDescription, other.textureViewDescription) = (other.textureViewDescription, textureViewDescription);
            (mipmapDescriptions, other.mipmapDescriptions) = (other.mipmapDescriptions, mipmapDescriptions);
            (fullQualitySize, other.fullQualitySize) = (other.fullQualitySize, fullQualitySize);

            (other.ViewWidth, ViewWidth) = (ViewWidth, other.ViewWidth);
            (other.ViewHeight, ViewHeight) = (ViewHeight, other.ViewHeight);
            (other.ViewDepth, ViewDepth) = (ViewDepth, other.ViewDepth);
            (other.SizeInBytes, SizeInBytes) = (SizeInBytes, other.SizeInBytes);

            SwapInternal(other);
        }

        internal partial void SwapInternal(Texture other);

        internal void GetViewSliceBounds(ViewType viewType, ref int arrayOrDepthIndex, ref int mipIndex, out int arrayOrDepthCount, out int mipCount)
        {
            int arrayOrDepthSize = Depth > 1 ? Depth : ArraySize;

            switch (viewType)
            {
                case ViewType.Full:
                    arrayOrDepthIndex = 0;
                    mipIndex = 0;
                    arrayOrDepthCount = arrayOrDepthSize;
                    mipCount = MipLevelCount;
                    break;

                case ViewType.Single:
                    arrayOrDepthCount = ViewDimension == TextureDimension.Texture3D ? CalculateMipSize(Depth, mipIndex) : 1;
                    mipCount = 1;
                    break;

                case ViewType.MipBand:
                    arrayOrDepthCount = arrayOrDepthSize - arrayOrDepthIndex;
                    mipCount = 1;
                    break;

                case ViewType.ArrayBand:
                    arrayOrDepthCount = 1;
                    mipCount = MipLevelCount - mipIndex;
                    break;

                default:
                    arrayOrDepthCount = 0;
                    mipCount = 0;
                    break;
            }
        }

        internal int GetViewCount()
        {
            // TODO: This is unused and internal. Should it be kept?

            int arrayOrDepthSize = Depth > 1 ? Depth : ArraySize;
            int viewIndex = (4 * arrayOrDepthSize + arrayOrDepthSize) * MipLevelCount + MipLevelCount;

            return viewIndex;
        }

        internal int GetViewIndex(ViewType viewType, int arrayOrDepthIndex, int mipIndex)
        {
            // TODO: This is unused and internal. Should it be kept?

            int arrayOrDepthSize = Depth > 1 ? Depth : ArraySize;

            return (((int) viewType) * arrayOrDepthSize + arrayOrDepthIndex) * MipLevelCount + mipIndex;
        }

        internal static GraphicsResourceUsage GetUsageWithFlags(GraphicsResourceUsage usage, TextureFlags flags)
        {
            // If we have a Texture supporting Render Target View or Unordered Access View, force GraphicsResourceUsage.Default
            return flags.HasFlag(TextureFlags.RenderTarget) || flags.HasFlag(TextureFlags.UnorderedAccess)
                ? GraphicsResourceUsage.Default
                : usage;
        }

        internal int ComputeSubResourceSize(int subResourceIndex)
        {
            var mipLevel = subResourceIndex % MipLevelCount;

            var slicePitch = ComputeSlicePitch(mipLevel);
            var depth = CalculateMipSize(Description.Depth, mipLevel);

            return (slicePitch * depth + TextureSubresourceAlignment - 1) / TextureSubresourceAlignment * TextureSubresourceAlignment;
        }

        internal int ComputeBufferOffset(int subResourceIndex, int depthSlice)
        {
            int offset = 0;

            for (var i = 0; i < subResourceIndex; ++i)
            {
                offset += ComputeSubResourceSize(i);
            }

            if (depthSlice != 0)
                offset += ComputeSlicePitch(subResourceIndex % Description.MipLevelCount) * depthSlice;

            return offset;
        }

        internal int ComputeSlicePitch(int mipLevel)
        {
            return ComputeRowPitch(mipLevel) * CalculateMipSize(Height, mipLevel);
        }

        internal int ComputeRowPitch(int mipLevel)
        {
            // Round up to 256
            // TODO: Stale comment?
            return ((CalculateMipSize(Width, mipLevel) * TexturePixelSize) + TextureRowPitchAlignment - 1) / TextureRowPitchAlignment * TextureRowPitchAlignment;
        }

        internal int ComputeBufferTotalSize()
        {
            int totalSize = 0;

            for (int i = 0; i < Description.MipLevelCount; ++i)
            {
                totalSize += ComputeSubResourceSize(i);
            }

            return totalSize * Description.ArraySize;
        }

        public static int CountMips(int width)
        {
            // TODO: Efficient calculation without loop. Lzcnt?

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
            var largestDimension = Math.Max(width, height);
            return CountMips(largestDimension);
        }

        public static int CountMips(int width, int height, int depth)
        {
            var largestDimension = Math.Max(width, Math.Max(height, depth));
            return CountMips(largestDimension);
        }
        private partial bool IsFlipped();
    }
}
