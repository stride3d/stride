// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Graphics;
using Stride.Rendering.RenderTextures;

namespace Stride.Assets.Textures
{
    [DataContract("RenderTexture")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Texture))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed partial class RenderTextureAsset : Asset
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="RenderTextureAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdrendertex";

        /// <summary>
        /// The width in pixel.
        /// </summary>
        [DefaultValue(512)]
        [Display(null, "Size")]
        public int Width { get; set; } = 512;

        /// <summary>
        /// The height in pixel.
        /// </summary>
        [DefaultValue(512)]
        [Display(null, "Size")]
        public int Height { get; set; } = 512;

        /// <summary>
        /// The format.
        /// </summary>
        [DefaultValue(RenderTextureFormat.LDR)]
        [Display("Format", "Format")]
        public RenderTextureFormat Format { get; set; } = RenderTextureFormat.LDR;

        /// <summary>
        /// Texture will be stored in sRGB format (standard for color textures) and converted to linear space when sampled. Only relevant when working in Linear color space.
        /// </summary>
        /// <userdoc>
        /// Should be checked for all color textures, unless they are explicitly in linear space. Texture will be stored in sRGB format (standard for color textures) and converted to linear space when sampled. Only relevant when working in Linear color space.
        /// </userdoc>
        [DefaultValue(true)]
        [Display("sRGB sampling")]
        public bool UseSRgbSampling { get; set; } = true;

        public bool IsSRgb(ColorSpace colorSpaceReference) => ((colorSpaceReference == ColorSpace.Linear) && UseSRgbSampling);
    }
}
