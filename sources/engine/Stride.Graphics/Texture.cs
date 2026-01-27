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

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.ReferenceCounting;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Data;

namespace Stride.Graphics
{
    /// <summary>
    ///   All-in-one <strong>GPU Texture</strong> that is able to represent many types of textures (1D, 2D, 3D, Depth-Stencil Buffers, Render Targets, etc),
    ///   as well as <strong>Texture Views</strong> over a parent Texture.
    /// </summary>
    /// <remarks>
    ///   <para><see cref="Texture"/> constains static methods for creating new Textures by specifying all their characteristics.</para>
    ///   <para>
    ///     Also look for the following static methods that aid in the creation of specific kinds of buffers:
    ///     <see cref="New1D"/> (for <strong>one-dimensional Textures</strong>), <see cref="New2D"/> (for <strong>two-dimensional Textures</strong>),
    ///     <see cref="New3D"/> (for <strong>three-dimensional Textures</strong>), and <see cref="NewCube"/> (for <strong>six-sided two-dimensional Cube-maps</strong>).
    ///   </para>
    ///   <para>Consult the documentation of your graphics API for more information on each kind of Texture.</para>
    /// </remarks>
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
        ///   Gets the common description for the original Texture.
        /// </summary>
        public ref readonly TextureDescription Description => ref textureDescription;

        /// <summary>
        ///   Gets the view description.
        /// </summary>
        public ref readonly TextureViewDescription ViewDescription => ref textureViewDescription;

        /// <summary>
        ///   Gets the type of the Texture.
        /// </summary>
        public TextureDimension Dimension
            // TODO: What's the point of storing the dimensions?
            // We could just as well generate the "TextureDimension" based on the "TextureTarget" property,
            // because "TextureDimension" is fully dependent on the "TextureTarget" property.
            // E.g. "Texture2D" and "Texture2DMultisample" both return "TextureDimension.Texture2D".
            // TODO: Stale comment?
            => textureDescription.Dimension;

        /// <summary>
        ///   Gets the width of the Texture View.
        /// </summary>
        public int ViewWidth { get; private set; }

        /// <summary>
        ///   Gets the height of the Texture View.
        /// </summary>
        public int ViewHeight { get; private set; }

        /// <summary>
        ///   Gets the depth of the Texture View.
        /// </summary>
        public int ViewDepth { get; private set; }

        /// <summary>
        ///   Gets the pixel format of the Texture View.
        /// </summary>
        public PixelFormat ViewFormat => textureViewDescription.Format;

        /// <summary>
        ///   Gets a combination of flags describing the type of Texture View and how it can be bound to the graphics pipeline.
        /// </summary>
        public TextureFlags ViewFlags => textureViewDescription.Flags;

        /// <summary>
        ///   Gets a value indicating which sub-resources of the Texture are accessible through the Texture View.
        /// </summary>
        public ViewType ViewType => textureViewDescription.Type;

        /// <summary>
        ///   Gets the type of the Texture View.
        /// </summary>
        public TextureDimension ViewDimension
            => Dimension == TextureDimension.TextureCube && ViewType != ViewType.Full
                ? TextureDimension.Texture2D   // For cube-maps, if not full, the View is over a single cube face
                : Dimension;

        /// <summary>
        ///   Gets the index of the mip-level the Texture View is referencing.
        /// </summary>
        /// <value>The index of the mip-level the Texture View references. The first (largest) mipLevel is always index 0.</value>
        /// <remarks>
        ///   See <see cref="Graphics.ViewType"/> for more information on how <see cref="MipLevel"/> and <see cref="ArraySlice"/>
        ///   determines which sub-resources to select based on <see cref="ViewType"/>.
        /// </remarks>
        public int MipLevel => textureViewDescription.MipLevel;

        /// <summary>
        ///   Gets the index of the array slice the Texture View is referencing.
        /// </summary>
        /// <value>
        ///   The array index the Texture View references.
        ///   If the parent Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0.
        /// </value>
        /// <remarks>
        ///   See <see cref="Graphics.ViewType"/> for more information on how <see cref="MipLevel"/> and <see cref="ArraySlice"/>
        ///   determines which sub-resources to select based on <see cref="ViewType"/>.
        /// </remarks>
        public int ArraySlice => textureViewDescription.ArraySlice;

        /// <summary>
        ///   Gets the width of the Texture.
        /// </summary>
        /// <value>The width of the Texture in pixels.</value>
        public int Width => textureDescription.Width;

        /// <summary>
        ///   Gets the height of the Texture.
        /// </summary>
        /// <value>The height of the Texture in pixels.</value>
        public int Height => textureDescription.Height;

        /// <summary>
        ///   Gets the depth of the Texture.
        /// </summary>
        /// <value>The depth of the Texture in pixels.</value>
        public int Depth => textureDescription.Depth;

        /// <summary>
        ///   Gets the number of Textures in the array (if this is a Texture Array).
        /// </summary>
        /// <value>The largestSize of the array. If this is not a Texture Array, it will return 1.</value>
        /// <remarks>This field is only valid for 1D, 2D and Cube <see cref="Texture"/>s.</remarks>
        public int ArraySize => textureDescription.ArraySize;

        /// <summary>
        ///   Gets the maximum number of mip-Levels in the Texture.
        /// </summary>
        public int MipLevelCount => textureDescription.MipLevelCount;

        /// <summary>
        ///   Gets the pixel format of the Texture.
        /// </summary>
        public PixelFormat Format => textureDescription.Format;

        /// <summary>
        ///   Gets the multisampling for the Texture.
        /// </summary>
        /// <value>A value of <see cref="Graphics.MultisampleCount"/> specifying the number of samples per pixel.</value>
        /// <remarks>This field is only valid for 2D <see cref="Texture"/>s.</remarks>
        public MultisampleCount MultisampleCount => textureDescription.MultisampleCount;

        /// <summary>
        ///   Gets the intended usage of the Texture.
        /// </summary>
        /// <value>A value that identifies how the Texture is to be read from and written to.</value>
        public GraphicsResourceUsage Usage => textureDescription.Usage;

        /// <summary>
        ///   Gets a combination of flags indicating how the Texture can be bound to the graphics pipeline.
        /// </summary>
        public TextureFlags Flags => textureDescription.Flags;

        /// <summary>
        ///   Gets a combination of flags indicating special options for the Texture, like sharing.
        /// </summary>
        public TextureOptions Options => textureDescription.Options;

        /// <summary>
        ///   Gets a handle that identifies the Texture as a shared resource when it has the
        ///   option <see cref="TextureOptions.Shared"/> or <see cref="TextureOptions.SharedNtHandle"/>.
        /// </summary>
        /// <value>
        ///   A handle that identifies the shared Texture, or <see cref="IntPtr.Zero"/> if it is not a shared resource.
        /// </value>
        public IntPtr SharedHandle { get; private set; } = IntPtr.Zero;

#if STRIDE_GRAPHICS_API_DIRECT3D11
        /// <summary>
        ///   Gets the name of the shared NT handle that identifies the Texture as a shared resource
        ///   when it has the option <see cref="TextureOptions.SharedNtHandle"/>.
        /// </summary>
        /// <value>
        ///   The name of the NT handle of the shared Texture, or <see cref="string.Empty"/> if it is not a shared resource.
        /// </value>
        public string SharedNtHandleName { get; private set; } = string.Empty;
#endif

        /// <summary>
        ///   Gets a value indicating if the Texture View is a Render Target View.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Texture View is a Render Target View; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsRenderTarget => ViewFlags.HasFlag(TextureFlags.RenderTarget);

        /// <summary>
        ///   Gets a value indicating if the Texture View is a Depth-Stencil View.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Texture View is a Depth-Stencil View; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsDepthStencil => ViewFlags.HasFlag(TextureFlags.DepthStencil);

        /// <summary>
        ///   Gets a value indicating if the Texture View is a read-only Depth-Stencil View.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Texture View is a read-only Depth-Stencil View; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsDepthStencilReadOnly => (ViewFlags & TextureFlags.DepthStencilReadOnly) == TextureFlags.DepthStencilReadOnly;

        /// <summary>
        ///   Gets a value indicating if the Texture View is a Shader Resource View.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Texture View is a Shader Resource View; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsShaderResource => ViewFlags.HasFlag(TextureFlags.ShaderResource);

        /// <summary>
        ///   Gets a value indicating if the Texture View is an Unordered Access View.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Texture View is an Unordered Access View; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsUnorderedAccess => ViewFlags.HasFlag(TextureFlags.UnorderedAccess);

        /// <summary>
        ///   Gets a value indicating if the Texture is a multi-sampled Texture.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Texture is multi-sampled; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsMultiSampled => MultisampleCount > MultisampleCount.None;

        /// <summary>
        ///   Gets a value indicating if the Texture is a using a block compress format (BC1, BC2, BC3, BC4, BC5, BC6H, BC7).
        /// </summary>
        /// <seealso cref="Format"/>
        public bool IsBlockCompressed => Description.Format.IsCompressed;

        /// <summary>
        ///   Gets the largestSize of the Texture or Texture View.
        /// </summary>
        /// <value>The largestSize of the Texture or Texture View as (Width, Height, Depth).</value>
        public Size3 Size => new(ViewWidth, ViewHeight, ViewDepth);

        /// <summary>
        ///   Gets the largestSize of the Texture when loaded at full quality when <em>texture streaming</em> is enabled.
        /// </summary>
        public Size3 FullQualitySize
        {
            get => fullQualitySize ?? Size;
            internal set => fullQualitySize = value;
        }

        /// <summary>
        ///   Gets the width stride of the Texture in bytes (y.e. the number of bytes per row).
        /// </summary>
        internal int RowStride { get; private set; }

        /// <summary>
        ///   Gets the underlying parent Texture if this is a Texture View.
        /// </summary>
        internal Texture ParentTexture { get; private set; }

        /// <summary>
        ///   Gets the total amount of memory allocated by the Texture in bytes.
        /// </summary>
        internal int SizeInBytes { get; private set; }

        private MipMapDescription[] mipmapDescriptions;


        // Needed for serialization
        public Texture() { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        internal Texture(GraphicsDevice device) : base(device) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="name">
        ///   A name that can be used to identify the Texture.
        ///   Specify <see langword="null"/> to use the type's name instead.
        /// </param>
        internal Texture(GraphicsDevice device, string? name) : base(device, name) { }


        /// <inheritdoc/>
        protected override void Destroy()
        {
            base.Destroy();

            ParentTexture?.ReleaseInternal();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            OnRecreateImpl();
            return true;
        }

        /// <summary>
        ///   Perform platform-specific recreation of the Texture.
        /// </summary>
        private partial void OnRecreateImpl();

        /// <summary>
        ///   Initializes the Texture from a <see cref="TextureDescription"/> and, optionally, initial data.
        /// </summary>
        /// <param name="description">The description of the Texture's characteristics.</param>
        /// <param name="textureDatas">Initial data to upload to the Texture, or <see langword="null"/> if no initial data is provided.</param>
        /// <returns>The current Texture already initialized.</returns>
        /// <exception cref="NotSupportedException">
        ///   The Texture's <see cref="Flags"/> and the <see cref="ViewFlags"/> are not compatible. The parent Texture must include all
        ///   the flags defined by the Texture View.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="MultisampleCount"/> is not supported for the specified <see cref="Format"/>. Check the
        ///   <see cref="GraphicsDevice.Features"/> for information about supported pixel formats and the compatible
        ///   multi-sample counts.
        /// </exception>
        /// <exception cref="NotSupportedException">Multi-sampling is only supported for 2D Textures.</exception>
        /// <exception cref="NotSupportedException">A Texture Cube must have an array size greater than 1.</exception>
        /// <exception cref="NotSupportedException">Texture Arrays are not supported for 3D Textures.</exception>
        /// <exception cref="NotSupportedException"><see cref="ViewType.MipBand"/> is not supported for Render Targets.</exception>
        /// <exception cref="NotSupportedException">Multi-sampling is not supported for Unordered Access Views.</exception>
        /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
        /// <exception cref="NotSupportedException">Cannot create a read-only Depth-Stencil View because the device does not support it.</exception>
        internal Texture InitializeFrom(TextureDescription description, DataBox[] textureDatas = null)
        {
            var viewDescription = new TextureViewDescription();
            return InitializeFrom(parentTexture: null, in description, in viewDescription, textureDatas);
        }

#if STRIDE_PLATFORM_ANDROID //&& USE_GLES_EXT_OES_TEXTURE
        internal Texture InitializeForExternalOES()
        {
            InitializeForExternalOESImpl();
            return this;
        }
#endif

        /// <summary>
        ///   Initializes the Texture View from a Texture's <see cref="TextureDescription"/> and, optionally, initial data.
        ///   Also initializes a Texture View over the resource.
        /// </summary>
        /// <param name="description">The description of the Texture's characteristics.</param>
        /// <param name="viewDescription">The description of the Texture View's characteristics.</param>
        /// <param name="textureDatas">
        ///   Initial data to upload through the Texture View, or <see langword="null"/> if no initial data is provided.
        /// </param>
        /// <returns>The current Texture already initialized.</returns>
        /// <exception cref="NotSupportedException">
        ///   <para>
        ///     The Texture's <see cref="Flags"/> and the <see cref="ViewFlags"/> are not compatible. The parent Texture must include all
        ///     the flags defined by the Texture View, or
        ///   </para>
        ///   <para>
        ///     The <see cref="MultisampleCount"/> is not supported for the specified <see cref="Format"/>. Check the
        ///     <see cref="GraphicsDevice.Features"/> for information about supported pixel formats and the compatible
        ///     multi-sample counts.
        ///   </para>
        /// </exception>
        internal Texture InitializeFrom(ref readonly TextureDescription description,
                                        ref readonly TextureViewDescription viewDescription,
                                        DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture: null, in description, in viewDescription, textureDatas);
        }

        /// <summary>
        ///   Initializes the Texture View for a Texture and, optionally, uploads initial data.
        /// </summary>
        /// <param name="parentTexture">The parent Texture.</param>
        /// <param name="viewDescription">The description of the Texture View's characteristics.</param>
        /// <param name="textureDatas">
        ///   Initial data to upload through the Texture View, or <see langword="null"/> if no initial data is provided.
        /// </param>
        /// <returns>The current Texture View already initialized.</returns>
        /// <exception cref="NotSupportedException">
        ///   <para>
        ///     The Texture's <see cref="Flags"/> and the <see cref="ViewFlags"/> are not compatible. The parent Texture must include all
        ///     the flags defined by the Texture View, or
        ///   </para>
        ///   <para>
        ///     The <see cref="MultisampleCount"/> is not supported for the specified <see cref="Format"/>. Check the
        ///     <see cref="GraphicsDevice.Features"/> for information about supported pixel formats and the compatible
        ///     multi-sample counts.
        ///   </para>
        /// </exception>
        internal Texture InitializeFrom(Texture parentTexture, ref readonly TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture, in parentTexture.Description, in viewDescription, textureDatas);
        }

        /// <summary>
        ///   Initializes the Texture or Texture View and, optionally, uploads initial data.
        /// </summary>
        /// <param name="parentTexture">
        ///   The parent Texture to initialize a Texture View over, or <see langword="null"/> if no parent Texture is specified,
        ///   in which case this may be a "root" resource and a view over it all-in-one.
        /// </param>
        /// <param name="description">
        ///   The description of the characteristics of the Texture to create,
        ///   or the parent Texture's characteristics if this is a Texture View.
        /// </param>
        /// <param name="viewDescription">The description of the Texture View's characteristics.</param>
        /// <param name="textureDatas">
        ///   Initial data to upload through the Texture View, or <see langword="null"/> if no initial data is provided.
        /// </param>
        /// <returns>The current Texture View already initialized.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Invalid Texture share options (<see cref="TextureOptions"/>) specified.</exception>
        /// <exception cref="NotSupportedException">
        ///   The Texture's <see cref="Flags"/> and the <see cref="ViewFlags"/> are not compatible. The parent Texture must include all
        ///   the flags defined by the Texture View.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The <see cref="MultisampleCount"/> is not supported for the specified <see cref="Format"/>. Check the
        ///   <see cref="GraphicsDevice.Features"/> for information about supported pixel formats and the compatible
        ///   multi-sample counts.
        /// </exception>
        /// <exception cref="NotSupportedException">Multi-sampling is only supported for 2D Textures.</exception>
        /// <exception cref="NotSupportedException">A Texture Cube must have an array size greater than 1.</exception>
        /// <exception cref="NotSupportedException">Texture Arrays are not supported for 3D Textures.</exception>
        /// <exception cref="NotSupportedException"><see cref="ViewType.MipBand"/> is not supported for Render Targets.</exception>
        /// <exception cref="NotSupportedException">Multi-sampling is not supported for Unordered Access Views.</exception>
        /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
        /// <exception cref="NotSupportedException">Cannot create a read-only Depth-Stencil View because the device does not support it.</exception>
        internal Texture InitializeFrom(Texture parentTexture,
                                        ref readonly TextureDescription description,
                                        ref readonly TextureViewDescription viewDescription,
                                        DataBox[] textureDatas = null)
        {
            ParentTexture = parentTexture;
            ParentTexture?.AddReferenceInternal();

            textureDescription = description;
            textureViewDescription = viewDescription;
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
        ///   Initializes the Texture with no initial data.
        /// </summary>
        private void InitializeFromImpl() => InitializeFromImpl(dataBoxes: null);

        /// <summary>
        ///   Performs platform-dependent initialization of the Texture.
        /// </summary>
        /// <param name="dataBoxes">
        ///   An array of <see cref="DataBox"/> pointing to the data to initialize the Texture's sub-resources.
        /// </param>
        private partial void InitializeFromImpl(DataBox[] dataBoxes);

        /// <summary>
        ///   Releases the Texture data.
        /// </summary>
        public void ReleaseData()
        {
            // Release GPU data
            OnDestroyed();

            // Clean description
            textureDescription = default;
            textureViewDescription = default;
            ViewWidth = ViewHeight = ViewDepth = 0;
            SizeInBytes = 0;
            mipmapDescriptions = null;
        }


        /// <summary>
        ///   Gets a Texture View on this Texture.
        /// </summary>
        /// <param name="viewDescription">The description of the Texture View to create.</param>
        /// <returns>A new <see cref="Texture"/> that represents the requested Texture View.</returns>
        /// <exception cref="NotSupportedException">
        ///   <para>
        ///     The Texture's <see cref="Flags"/> and the <see cref="ViewFlags"/> are not compatible. The parent Texture must include all
        ///     the flags defined by the Texture View, or
        ///   </para>
        ///   <para>
        ///     The <see cref="MultisampleCount"/> is not supported for the specified <see cref="Format"/>. Check the
        ///     <see cref="GraphicsDevice.Features"/> for information about supported pixel formats and the compatible
        ///     multi-sample counts.
        ///   </para>
        /// </exception>
        public Texture ToTextureView(TextureViewDescription viewDescription)
        {
            var texture = GraphicsDevice.IsDebugMode
                ? new Texture(GraphicsDevice, Name + " " + GetViewDebugName(in viewDescription))
                : new Texture(GraphicsDevice);

            return texture.InitializeFrom(ParentTexture ?? this, in viewDescription);
        }

        /// <summary>
        ///   Gets the description a specific mipLevel of the Texture.
        /// </summary>
        /// <param name="mipLevel">The mipmap level to query.</param>
        /// <returns>A <see cref="MipMapDescription"/> describing the requested mipmap.</returns>
        public ref readonly MipMapDescription GetMipMapDescription(int mipLevel)
        {
            return ref mipmapDescriptions[mipLevel];
        }

        /// <summary>
        ///   Calculates the largestSize of a specific mip-level.
        /// </summary>
        /// <param name="size">The original full largestSize from which to calculate mip-level largestSize, in texels.</param>
        /// <param name="mipLevel">The mip-level index.</param>
        /// <returns>The largestSize of the specified <paramref name="mipLevel"/>, in texels.</returns>
        /// <remarks>
        ///   Each mip-level becomes progressively smaller as <paramref name="mipLevel"/> grows.
        /// </remarks>
        public static int CalculateMipSize(int size, int mipLevel)
        {
            mipLevel = Math.Min(mipLevel, Image.CountMips(size));
            return Math.Max(1, size >> mipLevel);
        }

        /// <summary>
        ///   Counts the number of mip-levels for a one-dimensional Texture.
        /// </summary>
        /// <param name="width">The width of the Texture, in texels.</param>
        /// <param name="mipLevels">
        ///   <para>
        ///     A <see cref="MipMapCount"/> structure describing the number of mipmaps for the Texture.
        ///     Specify <see cref="MipMapCount.Auto"/> to have <strong>all mipmaps</strong>, or
        ///     <see cref="MipMapCount.One"/> to indicate a <strong>single mipmap</strong>, or
        ///     any number greater than 1 for a particular mipmap count.
        ///   </para>
        ///   <para>
        ///     You can also specify a number (which will be converted implicitly) or a <see cref="bool"/>.
        ///     See <see cref="MipMapCount"/> for more information about accepted values.
        ///   </para>
        /// </param>
        /// <returns>The number of mip-levels that can be created for <paramref name="width"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="mipLevels"/> is greater than the maximum number of possible mip-levels for the provided <paramref name="width"/>.
        /// </exception>
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
        ///   Counts the number of mip-levels for a one-dimensional Texture.
        /// </summary>
        /// <param name="width">The width of the Texture, in texels.</param>
        /// <returns>The number of mip-levels that can be created for <paramref name="width"/>.</returns>
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
        ///   Counts the number of mip-levels for a two-dimensional Texture.
        /// </summary>
        /// <param name="width">The width of the Texture, in texels.</param>
        /// <param name="height">The height of the Texture, in texels.</param>
        /// <param name="mipLevels">
        ///   <para>
        ///     A <see cref="MipMapCount"/> structure describing the number of mipmaps for the Texture.
        ///     Specify <see cref="MipMapCount.Auto"/> to have <strong>all mipmaps</strong>, or
        ///     <see cref="MipMapCount.One"/> to indicate a <strong>single mipmap</strong>, or
        ///     any number greater than 1 for a particular mipmap count.
        ///   </para>
        ///   <para>
        ///     You can also specify a number (which will be converted implicitly) or a <see cref="bool"/>.
        ///     See <see cref="MipMapCount"/> for more information about accepted values.
        ///   </para>
        /// </param>
        /// <returns>The number of mip-levels that can be created for <paramref name="width"/> and <paramref name="height"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="mipLevels"/> is greater than the maximum number of possible mip-levels for the provided
        ///   <paramref name="width"/> and <paramref name="height"/>.
        /// </exception>
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

        /// <summary>
        ///   Counts the number of mip-levels for a two-dimensional Texture.
        /// </summary>
        /// <param name="width">The width of the Texture, in texels.</param>
        /// <param name="height">The height of the Texture, in texels.</param>
        /// <returns>The number of mip-levels that can be created for <paramref name="width"/> and <paramref name="height"/>.</returns>
        public static int CountMipLevels(int width, int height)
        {
            return CountMipLevels(Math.Max(width, height));
        }

        /// <summary>
        ///   Counts the number of mip-levels for a three-dimensional Texture.
        /// </summary>
        /// <param name="width">The width of the Texture, in texels.</param>
        /// <param name="height">The height of the Texture, in texels.</param>
        /// <param name="depth">The depth of the Texture, in texels.</param>
        /// <param name="mipLevels">
        ///   <para>
        ///     A <see cref="MipMapCount"/> structure describing the number of mipmaps for the Texture.
        ///     Specify <see cref="MipMapCount.Auto"/> to have <strong>all mipmaps</strong>, or
        ///     <see cref="MipMapCount.One"/> to indicate a <strong>single mipmap</strong>, or
        ///     any number greater than 1 for a particular mipmap count.
        ///   </para>
        ///   <para>
        ///     You can also specify a number (which will be converted implicitly) or a <see cref="bool"/>.
        ///     See <see cref="MipMapCount"/> for more information about accepted values.
        ///   </para>
        /// </param>
        /// <returns>
        ///   The number of mip-levels that can be created for <paramref name="width"/>, <paramref name="height"/>,
        ///   and <paramref name="depth"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="mipLevels"/> is greater than the maximum number of possible mip-levels for the provided
        ///   <paramref name="width"/>, <paramref name="height"/>, and <paramref name="depth"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="width"/>, <paramref name="height"/>, and <paramref name="depth"/> must all be
        ///   a power of two.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The dimensions must be a <strong>power of two (2^n)</strong>.
        /// </exception>
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
        ///   Counts the number of mip-levels for a three-dimensional Texture.
        /// </summary>
        /// <param name="width">The width of the Texture, in texels.</param>
        /// <param name="height">The height of the Texture, in texels.</param>
        /// <param name="depth">The depth of the Texture, in texels.</param>
        /// <returns>
        ///   The number of mip-levels that can be created for <paramref name="width"/>, <paramref name="height"/>,
        ///   and <paramref name="depth"/>.
        /// </returns>
        public static int CountMipLevels(int width, int height, int depth)
        {
            return CountMipLevels(Math.Max(width, Math.Max(height, depth)));
        }

        /// <summary>
        ///   Returns the absolute sub-resource index from an array slice and mip-level.
        /// </summary>
        /// <param name="arrayIndex">The array index.</param>
        /// <param name="mipLevel">The mip slice index.</param>
        /// <returns>The sub-resource absolute index, calculated as <c>arrayIndex * Description.MipLevelCount + mipLevel</c>.</returns>
        public int GetSubResourceIndex(int arrayIndex, int mipLevel)
        {
            return arrayIndex * MipLevelCount + mipLevel;
        }

        /// <summary>
        ///   Calculates the expected width of the Texture for a specific mip-level, in <typeparamref name="TData"/> elements.
        /// </summary>
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="mipLevel">
        ///   The mip-level for which to calculate the width.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <returns>
        ///   The expected width of the Texture for the mip-level specified by <paramref name="mipLevel"/>, y.e. the
        ///   number of <typeparamref name="TData"/> elements across the width of the Texture.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     The expected width of the Texture depends both on the largestSize of <see cref="Format"/> and the largestSize
        ///     of <typeparamref name="TData"/>. It's relation (which must be an integer ratio) defines how they
        ///     alter the original mip-level width.
        ///   </para>
        ///   <para>
        ///     For example, for a Texture with a width of 100 pixels and format <see cref="PixelFormat.R8G8B8A8_UNorm"/> (4 bytes per pixel),
        ///     this method allows to interpret the width as different types:
        ///     <code>
        ///       int widthAsUInts = texture.CalculateWidth&lt;uint>();   // 100 uints
        ///       int widthAsBytes = texture.CalculateWidth&lt;byte>();   // 400 bytes
        ///       int widthAsFloats = texture.CalculateWidth&lt;float>(); // 100 floats
        ///     </code>
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   The largestSize of <see cref="Format"/> and the largestSize of <typeparamref name="TData"/> does not match.
        ///   The ratio between the two must be an integer, or else there would be remaining bytes.
        /// </exception>
        public unsafe int CalculateWidth<TData>(int mipLevel = 0) where TData : unmanaged
        {
            var mipWidth = CalculateMipSize(Width, mipLevel);

            var rowStride = mipWidth * Format.SizeInBytes;
            var dataStrideInBytes = mipWidth * sizeof(TData);

            var (width, rem) = Math.DivRem(rowStride * mipWidth, dataStrideInBytes);

            if (rem != 0)
                throw new ArgumentException("sizeof(TData) / sizeof(Format) * Width is not an integer");

            return width;
        }

        /// <summary>
        ///   Calculates the number of pixel elements of type <typeparamref name="TData"/> the Texture requires
        ///   for a particular mip-level.
        /// </summary>
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="mipLevel">
        ///   The mip-level for which to calculate the width.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <returns>
        ///   The expected number of <typeparamref name="TData"/> elements of the Texture for the mip-level specified by <paramref name="mipLevel"/>.
        /// </returns>
        /// <remarks>
        ///   This method can be used to allocate a Texture data buffer to hold pixel data of type <typeparamref name="TData"/> as follows:
        ///   <code>
        ///     var textureData = new TData[ texture.CalculatePixelDataCount&lt;TData&gt;() ];
        ///   </code>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   The largestSize of <see cref="Format"/> and the largestSize of <typeparamref name="TData"/> does not match.
        ///   The ratio between the two must be an integer, or else there would be remaining bytes.
        /// </exception>
        /// <seealso cref="CalculateWidth{TData}(int)"/>
        public int CalculatePixelDataCount<TData>(int mipLevel = 0) where TData : unmanaged
        {
            return CalculateWidth<TData>(mipLevel)
                 * CalculateMipSize(Height, mipLevel)
                 * CalculateMipSize(Depth, mipLevel);
        }

        #region Debug

        /// <summary>
        ///   Generates a debug-friendly name for the Texture based on its usage, flags, etc.
        /// </summary>
        /// <param name="textureDescription">The description of the Texture.</param>
        /// <returns>A string representing the debug name of the Texture.</returns>
        private static string GetDebugName(ref readonly TextureDescription textureDescription)
        {
            var textureUsage = textureDescription.Usage;
            var arraySize = textureDescription.ArraySize;
            var textureDimension = textureDescription.Dimension;
            var multiSampleCount = textureDescription.MultisampleCount;
            var flags = textureDescription.Flags;
            var width = textureDescription.Width;
            var height = textureDescription.Height;
            var depth = textureDescription.Depth;
            var format = textureDescription.Format;

            return GetDebugName(textureUsage, textureDimension, width, height, depth, arraySize, flags, format, multiSampleCount);
        }

        /// <summary>
        ///   Generates a debug-friendly name for the Texture based on its usage, flags, etc.
        /// </summary>
        /// <param name="textureUsage">The usage of the Texture.</param>
        /// <param name="textureDimension">The dimension of the Texture.</param>
        /// <param name="width">The width of the Texture.</param>
        /// <param name="height">The height of the Texture.</param>
        /// <param name="depth">The depth of the Texture.</param>
        /// <param name="arraySize">The number of array slices in the Texture.</param>
        /// <param name="textureFlags">The flags of the Texture.</param>
        /// <param name="format">The pixel format of the Texture.</param>
        /// <param name="multiSampleCount">The multi-sample count of the Texture.</param>
        /// <returns>A string representing the debug name of the Texture.</returns>
        private static string GetDebugName(GraphicsResourceUsage textureUsage, TextureDimension textureDimension,
                                           int width, int height = 1, int depth = 1, int arraySize = 1,
                                           TextureFlags textureFlags = TextureFlags.None, PixelFormat format = PixelFormat.None,
                                           MultisampleCount multiSampleCount = MultisampleCount.None)
        {
            var usage = textureUsage != GraphicsResourceUsage.Default
                ? $"{textureUsage} "
                : string.Empty;

            var dimension = textureDimension switch
            {
                TextureDimension.Texture1D when arraySize > 1 => $"1D Texture Array ({arraySize} elements)",
                TextureDimension.Texture1D => "1D Texture",
                TextureDimension.Texture2D when arraySize > 1 => $"2D Texture Array ({arraySize} elements)",
                TextureDimension.Texture2D => "2D Texture",
                TextureDimension.Texture3D => "3D Texture",
                TextureDimension.TextureCube when arraySize > 1 => $"Cube Texture Array ({arraySize} elements)",
                TextureDimension.TextureCube => "Cube Texture",

                _ => "Texture"
            };

            var msaa = multiSampleCount > MultisampleCount.None
                ? $" MSAA {multiSampleCount}"
                : string.Empty;

            var flags = textureFlags switch
            {
                TextureFlags.ShaderResource => "SRV ",
                TextureFlags.RenderTarget => "Render Target ",
                TextureFlags.UnorderedAccess => "UAV ",
                TextureFlags.DepthStencil => "Depth-Stencil ",

                _ => string.Empty
            };

            var size = string.Empty;
            if (textureDimension is TextureDimension.Texture1D)
                size = $"{width}";
            else if (textureDimension is TextureDimension.Texture2D or TextureDimension.TextureCube)
                size = $"{width}x{height}";
            else if (textureDimension is TextureDimension.Texture3D)
                size = $"{width}x{height}x{depth}";

            return $"{usage}{flags}{dimension}{msaa} ({size}, {format})";
        }

        /// <summary>
        ///   Generates a debug-friendly name for a View on a Texture based on its type, flags, etc.
        /// </summary>
        /// <param name="viewDescription">The description of the Texture View.</param>
        /// <returns>A string representing the debug name of the Texture View.</returns>
        private static string GetViewDebugName(ref readonly TextureViewDescription viewDescription)
        {
            var viewType = viewDescription.Type;
            var flags = viewDescription.Flags;
            var format = viewDescription.Format;
            var arraySlice = viewDescription.ArraySlice;
            var mipLevel = viewDescription.MipLevel;

            return GetViewDebugName(viewType, flags, format, arraySlice, mipLevel);
        }

        /// <summary>
        ///   Generates a debug-friendly name for a View on a Texture based on its type, flags, etc.
        /// </summary>
        /// <param name="viewType">The type of the Texture View.</param>
        /// <param name="viewFlags">Flags describing how the Texture View can be bound to the graphics pipeline.</param>
        /// <param name="format">The pixel format of the Texture View.</param>
        /// <param name="arraySlice">The index of the array slice the Texture View is referencing.</param>
        /// <param name="mipLevel">The index of the mip-level the Texture View is referencing.</param>
        /// <returns>A string representing the debug name of the Texture View.</returns>
        private static string GetViewDebugName(ViewType viewType, TextureFlags viewFlags, PixelFormat format, int arraySlice, int mipLevel)
        {
            var type = viewType switch
            {
                ViewType.MipBand => "Mip Band ",
                ViewType.ArrayBand => "Array Band ",
                ViewType.Single => "Single ",

                _ => string.Empty
            };

            var flags = viewFlags switch
            {
                TextureFlags.ShaderResource => "ShaderResourceView",
                TextureFlags.RenderTarget => "RenderTargetView",
                TextureFlags.UnorderedAccess => "UnorderedAccessView",
                TextureFlags.DepthStencil => "DepthStencilView",

                _ => string.Empty
            };

            var mipAndSlice = viewType != ViewType.Full
                ? $" (Mip {mipLevel}, Slice {arraySlice})"
                : string.Empty;

            var viewFormat = format != PixelFormat.None
                ? $" [{format}]"
                : string.Empty;

            return $"{type}{flags}{mipAndSlice}{viewFormat}";
        }

        /// <summary>
        ///   Generates a debug-friendly name for a View on a Texture based on its type, flags, etc.
        /// </summary>
        /// <param name="textureDescription">The description of the Texture.</param>
        /// <param name="viewDescription">The description of the Texture View.</param>
        /// <returns>A string representing the debug name of the Texture View.</returns>
        private static string GetViewDebugName(ref readonly TextureDescription textureDescription,
                                               ref readonly TextureViewDescription viewDescription)
        {
            var debugName = GetDebugName(in textureDescription);
            var viewDebugName = GetViewDebugName(in viewDescription);

            return string.IsNullOrWhiteSpace(viewDebugName)
                ? debugName
                : debugName + " " + viewDebugName;
        }

        #endregion

        #region GetData: Reading data from the Texture

        // TODO: Some methods are marked as main-thread only. This is true for some platforms, but for all?

        /// <summary>
        ///   Copies the contents of the Texture from GPU memory to CPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to get the data from.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <returns>An array of <typeparamref name="TData"/> with the Texture's data.</returns>
        /// <remarks>
        ///   This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        ///   <para>
        ///     This method creates internally a <em>staging Resource</em>, copies the data into it, and maps it to CPU memory.
        ///     You can use one of the methods that specify an explicit staging Resource for better performance.
        ///   </para>
        /// </remarks>
        public TData[] GetData<TData>(CommandList commandList, int arrayIndex = 0, int mipLevel = 0) where TData : unmanaged
        {
            var toData = new TData[CalculatePixelDataCount<TData>(mipLevel)];
            GetData(commandList, toData, arrayIndex, mipLevel);
            return toData;
        }

        /// <summary>
        ///   Copies the contents of the Texture from GPU memory to CPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="toData">The destination buffer to copy the Texture data into.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to get the data from.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="doNotWait">
        ///   If <see langword="true"/> this method will return immediately if the resource is still being used by the GPU for writing.
        ///   <see langword="false"/> makes this method wait until the operation is complete. The default value is <see langword="false"/> (wait).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if data was correctly retrieved;
        ///   <see langword="false"/> if <paramref name="doNotWait"/> flag was <see langword="true"/> and the resource is still being used by the GPU for writing.
        /// </returns>
        /// <remarks>
        ///   This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        ///   <para>
        ///     This method creates internally a <em>staging Resource</em>, copies the data into it, and maps it to CPU memory.
        ///     You can use one of the methods that specify an explicit staging Resource for better performance.
        ///   </para>
        /// </remarks>
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
        ///   Copies the contents of the Texture from GPU memory to CPU memory using a specific staging Resource.
        /// </summary>
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="stagingTexture">The staging Texture used to transfer the Texture contents to.</param>
        /// <param name="toData">The destination buffer to copy the Texture data into.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to get the data from.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="doNotWait">
        ///   If <see langword="true"/> this method will return immediately if the resource is still being used by the GPU for writing.
        ///   <see langword="false"/> makes this method wait until the operation is complete. The default value is <see langword="false"/> (wait).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if data was correctly retrieved;
        ///   <see langword="false"/> if <paramref name="doNotWait"/> flag was <see langword="true"/> and the resource is still being used by the GPU for writing.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The length of the destination buffer <paramref name="toData"/> is not compatible with the expected largestSize for the data
        ///   at <paramref name="arrayIndex"/> and <paramref name="mipLevel"/>.
        /// </exception>
        public unsafe bool GetData<TData>(CommandList commandList, Texture stagingTexture, TData[] toData, int arrayIndex = 0, int mipLevel = 0, bool doNotWait = false) where TData : unmanaged
        {
            return GetData(commandList, stagingTexture, toData.AsSpan(), arrayIndex, mipLevel, doNotWait);
        }

        /// <summary>
        ///   Copies the contents of the Texture from GPU memory to CPU memory using a specific staging Resource.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="stagingTexture">The staging Texture used to transfer the Texture contents to.</param>
        /// <param name="toData">A ptrFromData to the data buffer in CPU memory to copy the Texture contents into.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to get the data from.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="doNotWait">
        ///   If <see langword="true"/> this method will return immediately if the resource is still being used by the GPU for writing.
        ///   <see langword="false"/> makes this method wait until the operation is complete. The default value is <see langword="false"/> (wait).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if data was correctly retrieved;
        ///   <see langword="false"/> if <paramref name="doNotWait"/> flag was <see langword="true"/> and the resource is still being used by the GPU for writing.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The length of the destination buffer <paramref name="toData"/> is not compatible with the expected largestSize for the data
        ///   at <paramref name="arrayIndex"/> and <paramref name="mipLevel"/>.
        /// </exception>
        [Obsolete("This method is obsolete. Use the Span-based methods instead")]
        public unsafe bool GetData(CommandList commandList, Texture stagingTexture, DataPointer toData, int arrayIndex = 0, int mipLevel = 0, bool doNotWait = false)
        {
            return GetData(commandList, stagingTexture, new Span<byte>((void*)toData.Pointer, toData.Size), arrayIndex, mipLevel, doNotWait);
        }

        /// <summary>
        ///   Copies the contents of the Texture from GPU memory to CPU memory using a specific staging Resource.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="stagingTexture">The staging Texture used to transfer the Texture contents to.</param>
        /// <param name="toData">The data buffer in CPU memory to copy the Texture contents into.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to get the data from.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="doNotWait">
        ///   If <see langword="true"/> this method will return immediately if the resource is still being used by the GPU for writing.
        ///   <see langword="false"/> makes this method wait until the operation is complete. The default value is <see langword="false"/> (wait).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if data was correctly retrieved;
        ///   <see langword="false"/> if <paramref name="doNotWait"/> flag was <see langword="true"/> and the resource is still being used by the GPU for writing.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The length of the destination buffer <paramref name="toData"/> is not compatible with the expected largestSize for the data
        ///   at <paramref name="arrayIndex"/> and <paramref name="mipLevel"/>.
        /// </exception>
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
                    MemoryUtilities.CopyWithAlignmentFallback(destPtr, (void*) box.DataPointer, (uint) mipMapSize);
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
                                MemoryUtilities.CopyWithAlignmentFallback(destPtr, sourcePtr, (uint) rowStride);
                                sourcePtr -= box.RowPitch;
                                destPtr += rowStride;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < height; i++)
                            {
                                // Copy a single row
                                MemoryUtilities.CopyWithAlignmentFallback(destPtr, sourcePtr, (uint) rowStride);
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
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="fromData">The data buffer to copy from.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to copy the data to.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="region">
        ///   An optional <see cref="ResourceRegion"/> describing the region of data of the Texture to copy into.
        ///   Specify <see langword="null"/> to copy to the whole sub-Resource at <paramref name="arrayIndex"/>/<paramref name="mipLevel"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The length of <paramref name="fromData"/> is not compatible with the expected largestSize for the data
        ///   at <paramref name="arrayIndex"/> and <paramref name="mipLevel"/>.
        ///   This can also occur when the stride is different from the optimal stride, and <typeparamref name="TData"/> is not the same largestSize as
        ///   the largestSize of <see cref="Format"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="region"/> can only be specified (non-<see langword="null"/>) for Textures with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="region"/> largestSize (in any of its dimensions) cannot be greater than the mip-level largestSize.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        public unsafe void SetData<TData>(CommandList commandList, TData[] fromData, int arrayIndex = 0, int mipLevel = 0, ResourceRegion? region = null) where TData : unmanaged
        {
            SetData<TData>(commandList, fromData.AsSpan(), arrayIndex, mipLevel, region);
        }

        /// <summary>
        ///   Copies the contents of a data buffer on CPU memory into the Texture in GPU memory.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="fromData">A ptrFromData to the data buffer to copy from.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to copy the data to.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="region">
        ///   An optional <see cref="ResourceRegion"/> describing the region of data of the Texture to copy into.
        ///   Specify <see langword="null"/> to copy to the whole sub-Resource at <paramref name="arrayIndex"/>/<paramref name="mipLevel"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The length of <paramref name="fromData"/> is not compatible with the expected largestSize for the data
        ///   at <paramref name="arrayIndex"/> and <paramref name="mipLevel"/>.
        ///   This can also occur when the stride is different from the optimal stride, and <typeparamref name="TData"/> is not the same largestSize as
        ///   the largestSize of <see cref="Format"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="region"/> can only be specified (non-<see langword="null"/>) for Textures with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="region"/> largestSize (in any of its dimensions) cannot be greater than the mip-level largestSize.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
        /// </remarks>
        [Obsolete("This method is obsolete. Use the Span-based methods instead")]
        public unsafe void SetData(CommandList commandList, DataPointer fromData, int arrayIndex = 0, int mipLevel = 0, ResourceRegion? region = null)
        {
            SetData(commandList, new ReadOnlySpan<byte>((void*) fromData.Pointer, fromData.Size), arrayIndex, mipLevel, region);
        }

        /// <summary>
        ///   Copies the contents of a data buffer on CPU memory into the Texture in GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the pixel data.</typeparam>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="fromData">The data buffer to copy from.</param>
        /// <param name="arrayIndex">
        ///   <para>
        ///     The array index.
        ///     If the Texture is not a Texture Array or a Cube-map, only a single array slice will exist with index 0, which is the default value.
        ///   </para>
        ///   <para>This index must be 0 for a three-dimensional Texture.</para>
        /// </param>
        /// <param name="mipLevel">
        ///   The mip-level to copy the data to.
        ///   By default, the first mip-level at index 0 is selected, which is the most detailed one.
        /// </param>
        /// <param name="region">
        ///   An optional <see cref="ResourceRegion"/> describing the region of data of the Texture to copy into.
        ///   Specify <see langword="null"/> to copy to the whole sub-Resource at <paramref name="arrayIndex"/>/<paramref name="mipLevel"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The length of <paramref name="fromData"/> is not compatible with the expected largestSize for the data
        ///   at <paramref name="arrayIndex"/> and <paramref name="mipLevel"/>.
        ///   This can also occur when the stride is different from the optimal stride, and <typeparamref name="TData"/> is not the same largestSize as
        ///   the largestSize of <see cref="Format"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="region"/> can only be specified (non-<see langword="null"/>) for Textures with <see cref="GraphicsResourceUsage.Default"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The <paramref name="region"/> largestSize (in any of its dimensions) cannot be greater than the mip-level largestSize.
        /// </exception>
        /// <remarks>
        ///   See <see cref="CommandList.MapSubResource"/> and <see cref="CommandList.UpdateSubResource"/> for more information about
        ///   usage and restrictions.
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

            var sizePerElement = Format.SizeInBytes;

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
                        MemoryUtilities.CopyWithAlignmentFallback((void*) box.DataPointer, ptrFromData, (uint) sizeOfTextureData);
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
                                MemoryUtilities.CopyWithAlignmentFallback(destPtr, sourcePtr, (uint) rowStride);
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
        ///   Creates a copy of the Texture.
        /// </summary>
        /// <returns>A copy of the Texture.</returns>
        /// <remarks>
        ///   This method creates a new Texture with the exact same characteristics, but <strong>does not copy the contents</strong>
        ///   of the Texture to the new one.
        /// </remarks>
        public Texture Clone()
        {
            var cloneableDescription = textureDescription.ToCloneableDescription();

            var texture = GraphicsDevice.IsDebugMode
                ? new Texture(GraphicsDevice, Name)
                : new Texture(GraphicsDevice);

            return texture.InitializeFrom(in cloneableDescription, in ViewDescription);
        }

        /// <summary>
        ///   Creates a new Texture with the needed changes to serve as a staging Texture that can be read / written by the CPU.
        /// </summary>
        /// <returns>The equivalent staging Texture.</returns>
        public Texture ToStaging()
        {
            var stagingDescription = textureDescription.ToStagingDescription();
            var stagingViewDescription = ViewDescription.ToStagingDescription();

            var texture = GraphicsDevice.IsDebugMode
                ? new Texture(GraphicsDevice, Name)
                : new Texture(GraphicsDevice);

            return texture.InitializeFrom(in stagingDescription, in stagingViewDescription);
        }

        /// <summary>
        ///   Loads a Texture from a stream.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="stream">The stream to load the Texture from.</param>
        /// <param name="textureFlags">
        ///   A combination of flags determining what kind of Texture and how the is should behave
        ///   (i.e. how it is bound, how can it be read / written, etc.).
        ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
        /// </param>
        /// <param name="usage">
        ///   A combination of flags determining how the Texture will be used during rendering.
        ///   The default is <see cref="GraphicsResourceUsage.Immutable"/>, meaning it will need read access by the GPU.
        /// </param>
        /// <param name="loadAsSrgb">
        ///   <see langword="true"/> if the Texture should be loaded as an sRGB Texture;
        ///   <see langword="false"/> to load it in its default format.
        /// </param>
        /// <returns>The loaded Texture.</returns>
        public static Texture Load(GraphicsDevice device, Stream stream, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable, bool loadAsSrgb = false)
        {
            using var image = Image.Load(stream, loadAsSrgb);

            return New(device, image, textureFlags, usage);
        }

        /// <summary>
        ///   Creates a new Texture from an <see cref="Image"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="image">An <see cref="Image"/> to create the Texture from.</param>
        /// <param name="textureFlags">
        ///   A combination of flags determining what kind of Texture and how the is should behave
        ///   (i.e. how it is bound, how can it be read / written, etc.).
        ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
        /// </param>
        /// <param name="usage">
        ///   A combination of flags determining how the Texture will be used during rendering.
        ///   The default is <see cref="GraphicsResourceUsage.Immutable"/>, meaning it will need read access by the GPU.
        /// </param>
        /// <returns>The loaded Texture.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="device"/> is <see langword="null"/>, or
        ///   <paramref name="image"/> is <see langword="null"/>.
        /// </exception>
        public static Texture New(GraphicsDevice device, Image image, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(image);

            return New(device, image.Description, image.ToDataBox());
        }

        /// <summary>
        ///   Creates a new Texture with the specified description.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="description">A <see cref="TextureDescription"/> for the new Texture.</param>
        /// <param name="boxes">
        ///   An optional array of <see cref="DataBox"/> structures describing the initial data for all the sub-Resources of the new Texture.
        /// </param>
        /// <returns>The new Texture.</returns>
        /// <exception cref="ArgumentNullException">graphicsDevice</exception>
        public static Texture New(GraphicsDevice graphicsDevice, TextureDescription description, params DataBox[] boxes)
        {
            return New(graphicsDevice, description, new TextureViewDescription(), boxes);
        }

        /// <summary>
        ///   Creates a new Texture with the specified description.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="description">A <see cref="TextureDescription"/> for the new Texture.</param>
        /// <param name="viewDescription">A <see cref="TextureViewDescription"/> describing a Texture View that will be created the same time.</param>
        /// <param name="boxes">
        ///   An optional array of <see cref="DataBox"/> structures describing the initial data for all the sub-Resources of the new Texture.
        /// </param>
        /// <returns>The new Texture.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="graphicsDevice"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        ///   <para>
        ///     The Texture's <see cref="Flags"/> and the <see cref="ViewFlags"/> are not compatible. The parent Texture must include all
        ///     the flags defined by the Texture View, or
        ///   </para>
        ///   <para>
        ///     The <see cref="MultisampleCount"/> is not supported for the specified <see cref="Format"/>. Check the
        ///     <see cref="GraphicsDevice.Features"/> for information about supported pixel formats and the compatible
        ///     multi-sample counts.
        ///   </para>
        /// </exception>
        public static Texture New(GraphicsDevice graphicsDevice, TextureDescription description, TextureViewDescription viewDescription, params DataBox[] boxes)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);

            var texture = graphicsDevice.IsDebugMode
                ? new Texture(graphicsDevice, GetViewDebugName(in description, in viewDescription))
                : new Texture(graphicsDevice);

            return texture.InitializeFrom(in description, in viewDescription, boxes);
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
        ///   Saves the Texture to a stream with the specified image format.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="stream">The stream to write the Texture contents to.</param>
        /// <param name="fileType">The type of the image file to create.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        public void Save(CommandList commandList, Stream stream, ImageFileType fileType)
        {
            ArgumentNullException.ThrowIfNull(stream);

            using var staging = ToStaging();
            Save(commandList, stream, staging, fileType);
        }

        /// <summary>
        ///   Copies the contents of the Texture on GPU memory to an <see cref="Image"/> on the CPU.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <returns>The Image on CPU memory.</returns>
        public Image GetDataAsImage(CommandList commandList)
        {
            // Directly if this is a staging Resource
            if (Usage == GraphicsResourceUsage.Staging)
                return GetDataAsImage(commandList, stagingTexture: this);

            using var stagingTexture = ToStaging();
            return GetDataAsImage(commandList, stagingTexture);
        }

        /// <summary>
        ///   Gets the contents of the Texture on GPU memory to an <see cref="Image"/> on the CPU.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="stagingTexture">
        ///   The staging Texture used to temporarily transfer the image from GPU memory to CPU memory.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="stagingTexture"/> is not a staging Texture.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stagingTexture"/> is <see langword="null"/>.</exception>
        /// <returns>The Image on CPU memory.</returns>
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
        ///   Saves the Texture to a stream with a specified format.
        /// </summary>
        /// <param name="commandList">The <see cref="CommandList"/> where to register the command.</param>
        /// <param name="stream">The stream to write the Texture to.</param>
        /// <param name="stagingTexture">
        ///   The staging Texture used to temporarily transfer the image from GPU memory to CPU memory.
        /// </param>
        /// <param name="fileType">The type of the image file to create.</param>
        /// <exception cref="ArgumentException"><paramref name="stagingTexture"/> is not a staging Texture.</exception>
        public void Save(CommandList commandList, Stream stream, Texture stagingTexture, ImageFileType fileType)
        {
            using var image = GetDataAsImage(commandList, stagingTexture);
            image.Save(stream, fileType);
        }

        /// <summary>
        ///   Calculates the mipmap count for a Texture with the specified dimensions up to a requested mip-level.
        /// </summary>
        /// <param name="requestedLevel">The requested mip-level.</param>
        /// <param name="width">The width of the Texture, in pixels.</param>
        /// <param name="height">The height of the Texture, in pixels.</param>
        /// <param name="depth">The depth of the Texture, in pixels.</param>
        /// <returns>The computed mip-level count.</returns>
        internal static int CalculateMipMapCount(MipMapCount requestedLevel, int width, int height = 0, int depth = 0)
        {
            int largestSize = Math.Max(Math.Max(width, height), depth);

            int maxMipLevelCount = CountMipLevels(largestSize);

            // If all mip-levels requested (0), accept the full count, else limit to `requestedLevel`
            return requestedLevel == 0 ? maxMipLevelCount : Math.Min(requestedLevel, maxMipLevelCount);
        }

        /// <summary>
        ///   Computes and validates the memory layout of the data of a Texture that corresponds to a specific
        ///   pixel format and dimensions.
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="format">The pixel format of the Texture to calculate the <see cref="DataBox"/> for.</param>
        /// <param name="width">The width in pixels of the Texture to calculate the <see cref="DataBox"/> for.</param>
        /// <param name="height">The height in pixels of the Texture to calculate the <see cref="DataBox"/> for.</param>
        /// <param name="depth">The depth in pixels of the Texture to calculate the <see cref="DataBox"/> for.</param>
        /// <param name="textureData">The data buffer to upload to the Texture.</param>
        /// <param name="fixedPointer">A pointer to the data buffer to upload to the Texture.</param>
        /// <returns>A <see cref="DataBox"/> structure describing the data and its memory layout.</returns>
        /// <exception cref="ArgumentException">
        ///   The length of <paramref name="textureData"/> and data type <typeparamref name="TData"/> are incorrect for
        ///   the specified size and pixel <paramref name="format"/>.
        /// </exception>
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
        ///   Swaps the Texture's internal data with another Texture.
        /// </summary>
        /// <param name="other">The other Texture.</param>
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

        /// <summary>
        ///   Computes the bounds of a Texture View based on the View type, which determines what sub-Resources
        ///   the Texture View can access.
        /// </summary>
        /// <param name="viewType">The View type.</param>
        /// <param name="arrayOrDepthIndex">
        ///   The index of the selected array slice (if a Texture Array or Cube) or depth slice (if a three-dimensional Texture).
        ///   When this method returns, it will contain the adjusted index based on the <paramref name="viewType"/>.
        /// </param>
        /// <param name="mipIndex">
        ///   The index of the selected mip-level.
        ///   When this method returns, it will contain the adjusted index based on the <paramref name="viewType"/>.
        /// </param>
        /// <param name="arrayOrDepthCount">
        ///   When this method returns, it will contain the number of the selected array slices (if a Texture Array or Cube)
        ///   or depth slices (if a three-dimensional Texture).
        /// </param>
        /// <param name="mipCount">
        ///   When this method returns, it will contain the number of the selected mip-levels.
        /// </param>
        /// <seealso cref="Graphics.ViewType"/>
        internal void GetViewSliceBounds(ViewType viewType, ref int arrayOrDepthIndex, ref int mipIndex, out int arrayOrDepthCount, out int mipCount)
        {
            int arrayOrDepthSize = Depth > 1 ? Depth : ArraySize;

            switch (viewType)
            {
                //      Array slice
                //       0   1   2
                //     ┌───┬───┬───┐
                //   0 │ ▓ │ ▓ │ ▓ │   ■ = Selected
                // M   ├───┼───┼───┤   □ = Not selected
                // i 1 │ ▓ │ ▓ │ ▓ │
                // p   ├───┼───┼───┤
                //   2 │ ▓ │ ▓ │ ▓ │
                //     └───┴───┴───┘
                //
                case ViewType.Full:
                    arrayOrDepthIndex = 0;
                    mipIndex = 0;
                    arrayOrDepthCount = arrayOrDepthSize;
                    mipCount = MipLevelCount;
                    break;

                //      Array slice
                //       0   1   2
                //     ┌───┬───┬───┐
                //   0 │   │   │   │   ■ = Selected
                // M   ├───┼───┼───┤   □ = Not selected
                // i 1 │   │ ▓ │   │
                // p   ├───┼───┼───┤
                //   2 │   │   │   │
                //     └───┴───┴───┘
                //
                case ViewType.Single:
                    arrayOrDepthCount = ViewDimension == TextureDimension.Texture3D ? CalculateMipSize(Depth, mipIndex) : 1;
                    mipCount = 1;
                    break;

                //      Array slice
                //       0   1   2
                //     ┌───┬───┬───┐
                //   0 │   │   │   │   ■ = Selected
                // M   ├───┼───┼───┤   □ = Not selected
                // i 1 │   │ ▓ │ ▓ │
                // p   ├───┼───┼───┤
                //   2 │   │   │   │
                //     └───┴───┴───┘
                //
                case ViewType.MipBand:
                    arrayOrDepthCount = arrayOrDepthSize - arrayOrDepthIndex;
                    mipCount = 1;
                    break;

                //      Array slice
                //       0   1   2
                //     ┌───┬───┬───┐
                //   0 │   │   │   │   ■ = Selected
                // M   ├───┼───┼───┤   □ = Not selected
                // i 1 │   │ ▓ │   │
                // p   ├───┼───┼───┤
                //   2 │   │ ▓ │   │
                //     └───┴───┴───┘
                //
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

        /// <summary>
        ///   Returns the number of Views the Texture can have.
        /// </summary>
        /// <returns>The number of Views the Texture can have.</returns>
        internal int GetViewCount()
        {
            // TODO: This is unused and internal. Should it be kept?

            int arrayOrDepthSize = Depth > 1 ? Depth : ArraySize;
            int viewIndex = (4 * arrayOrDepthSize + arrayOrDepthSize) * MipLevelCount + MipLevelCount;

            return viewIndex;
        }

        /// <summary>
        ///   Returns the sub-Resource index for a view of a specific type.
        /// </summary>
        /// <param name="viewType">The View type, indicating which sub-Resources the View can select.</param>
        /// <param name="arrayOrDepthIndex">The depth or array slice index.</param>
        /// <param name="mipIndex">The mip-level index.</param>
        /// <returns>The index of the View.</returns>
        internal int GetViewIndex(ViewType viewType, int arrayOrDepthIndex, int mipIndex)
        {
            // TODO: This is unused and internal. Should it be kept?

            int arrayOrDepthSize = Depth > 1 ? Depth : ArraySize;

            return (((int) viewType) * arrayOrDepthSize + arrayOrDepthIndex) * MipLevelCount + mipIndex;
        }

        /// <summary>
        ///   Determines the correct <see cref="GraphicsResourceUsage"/> for the provided Texture flags combination.
        /// </summary>
        /// <param name="usage">The current Texture's intended usage.</param>
        /// <param name="flags">The Texture's flags.</param>
        /// <returns>The adjusted Texture flags.</returns>
        internal static GraphicsResourceUsage GetUsageWithFlags(GraphicsResourceUsage usage, TextureFlags flags)
        {
            // If we have a Texture supporting Render Target View or Unordered Access View, force GraphicsResourceUsage.Default
            return flags.HasFlag(TextureFlags.RenderTarget) || flags.HasFlag(TextureFlags.UnorderedAccess)
                ? GraphicsResourceUsage.Default
                : usage;
        }

        /// <summary>
        ///   Computes the size of a specific Texture's sub-Resource.
        /// </summary>
        /// <param name="subResourceIndex">The index of the sub-Resource.</param>
        /// <returns>The size of the sub-Resource, in bytes.</returns>
        internal int ComputeSubResourceSize(int subResourceIndex)
        {
            var mipLevel = subResourceIndex % MipLevelCount;

            var slicePitch = ComputeSlicePitch(mipLevel);
            var depth = CalculateMipSize(Description.Depth, mipLevel);

            return (slicePitch * depth + TextureSubresourceAlignment - 1) / TextureSubresourceAlignment * TextureSubresourceAlignment;
        }

        /// <summary>
        ///   Computes the offset of a specific sub-Resource in the Texture's data buffer.
        /// </summary>
        /// <param name="subResourceIndex">The index of the sub-Resource.</param>
        /// <param name="depthSlice">The depth slice.</param>
        /// <returns>The offset of the sub-Resource at <paramref name="subResourceIndex"/>, in bytes.</returns>
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

        /// <summary>
        ///   Computes the slice pitch for a specific mip-level, i.e. the number of bytes a depth slice of that mip-level occupies in memory,
        ///   including memory alignment considerations.
        /// </summary>
        /// <param name="mipLevel">The mip-level index.</param>
        /// <returns>The slice pitch of the <paramref name="mipLevel"/>, in bytes.</returns>
        internal int ComputeSlicePitch(int mipLevel)
        {
            return ComputeRowPitch(mipLevel) * CalculateMipSize(Height, mipLevel);
        }

        /// <summary>
        ///   Computes the row pitch for a specific mip-level, i.e. the number of bytes a row of that mip-level occupies in memory,
        ///   including memory alignment considerations.
        /// </summary>
        /// <param name="mipLevel">The mip-level index.</param>
        /// <returns>The row pitch of the <paramref name="mipLevel"/>, in bytes.</returns>
        internal int ComputeRowPitch(int mipLevel)
        {
            // Round up to 256
            // TODO: Stale comment?
            return ((CalculateMipSize(Width, mipLevel) * TexturePixelSize) + TextureRowPitchAlignment - 1) / TextureRowPitchAlignment * TextureRowPitchAlignment;
        }

        /// <summary>
        ///   Computes the total size of the Texture, taking into account all mip-levels and array slices.
        /// </summary>
        /// <returns>The total size of the Texture.</returns>
        internal int ComputeBufferTotalSize()
        {
            int totalSize = 0;

            for (int i = 0; i < Description.MipLevelCount; ++i)
            {
                totalSize += ComputeSubResourceSize(i);
            }

            return totalSize * Description.ArraySize;
        }

        /// <summary>
        ///   Counts the number of mip-levels a Texture with the specified size can have.
        /// </summary>
        /// <param name="width">The width of the Texture, in pixels.</param>
        /// <returns>The maximum number of mip-levels for the given size.</returns>
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

        /// <summary>
        ///   Counts the number of mip-levels a Texture with the specified size can have.
        /// </summary>
        /// <param name="width">The width of the Texture, in pixels.</param>
        /// <param name="height">The height of the Texture, in pixels.</param>
        /// <returns>The maximum number of mip-levels for the given size.</returns>
        public static int CountMips(int width, int height)
        {
            var largestDimension = Math.Max(width, height);
            return CountMips(largestDimension);
        }

        /// <summary>
        ///   Counts the number of mip-levels a Texture with the specified size can have.
        /// </summary>
        /// <param name="width">The width of the Texture, in pixels.</param>
        /// <param name="height">The height of the Texture, in pixels.</param>
        /// <param name="depth">The depth of the Texture, in pixels.</param>
        /// <returns>The maximum number of mip-levels for the given size.</returns>
        public static int CountMips(int width, int height, int depth)
        {
            var largestDimension = Math.Max(width, Math.Max(height, depth));
            return CountMips(largestDimension);
        }

        /// <summary>
        ///   Indicates if the Texture is flipped vertically, i.e. if the rows are ordered bottom-to-top instead of top-to-bottom.
        /// </summary>
        /// <returns><see langword="true"/> if the Texture is flipped; <see langword="false"/> otherwise.</returns>
        private partial bool IsFlipped();
    }
}
