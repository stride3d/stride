// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Yaml;
using Stride.Assets.Textures;
using Stride.Graphics;

namespace Stride.Assets.Sprite
{
    /// <summary>
    /// This asset represents a sheet (group) of sprites.
    /// </summary>
    [DataContract("SpriteSheet")]
    [CategoryOrder(10, "Parameters")]
    [CategoryOrder(50, "Atlas Packing")]
    [CategoryOrder(150, "Sprites")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(SpriteSheet))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public partial class SpriteSheetAsset : Asset
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="SpriteSheetAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdsheet";

        /// <summary>
        /// Gets or sets the type of the current sheet
        /// </summary>
        /// <userdoc>
        /// The type of the sprite sheet.
        /// </userdoc>
        [DataMember(10)]
        [Display("Sheet type", category: "Parameters")]
        public SpriteSheetType Type { get; set; }

        /// <summary>
        /// Gets or sets the color key used when color keying for a texture is enabled. When color keying, all pixels of a specified color are replaced with transparent black.
        /// </summary>
        /// <value>The color key.</value>
        /// <userdoc>
        /// The color that should be made transparent in all images of the group.
        /// </userdoc>
        [DataMember(20)]
        [Display("Color key color", category: "Parameters")]
        public Color ColorKeyColor { get; set; } = new Color(255, 0, 255);

        /// <summary>
        /// Gets or sets a value indicating whether to enable color key. Default is false.
        /// </summary>
        /// <value><c>true</c> to enable color key; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Make the color specified in "Color key color" transparent in all images of the group during the asset build
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(false)]
        [Display(category: "Parameters")]
        public bool ColorKeyEnabled { get; set; }

        /// <summary>
        /// If Compressed, the final texture will be compressed to an appropriate format based on the target platform. The final texture size must be a multiple of 4.
        /// </summary>
        /// <userdoc>
        /// Compress the texture to a format based on the target platform. The final texture size will be a multiple of 4.
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(true)]
        [Display("Compress", "Parameters")]
        public bool IsCompressed { get; set; } = true;

        /// <summary>
        /// Indicates if the texture is in sRGB format (standard for color textures). When working in Linear color space the texture will bed converted to linear space when sampling.
        /// </summary>
        /// <userdoc>
        /// Should be checked for all color textures, unless they are explicitly in linear space. When working in Linear color space, the texture will be stored in sRGB format and converted to linear space when sampling.
        /// </userdoc>
        [DataMember(45)]
        [DefaultValue(true)]
        [Display("sRGB sampling")]
        public bool UseSRgbSampling { get; set; } = true;

        public bool IsSRGBTexture(ColorSpace colorSpaceReference) => ((colorSpaceReference == ColorSpace.Linear) && UseSRgbSampling);

        /// <summary>
        /// Gets or sets the alpha format.
        /// </summary>
        /// <value>The alpha format.</value>
        /// <userdoc>
        /// The texture alpha format in which all the images of the group should be converted to.
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(AlphaFormat.Auto)]
        [Display(category: "Parameters")]
        public AlphaFormat Alpha { get; set; } = AlphaFormat.Auto;

        /// <summary>
        /// Gets or sets a value indicating whether [generate mipmaps].
        /// </summary>
        /// <value><c>true</c> if [generate mipmaps]; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Generate mipmaps for all images in the group
        /// </userdoc>
        [DataMember(60)]
        [DefaultValue(false)]
        [Display(category: "Parameters")]
        public bool GenerateMipmaps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to stream texture.
        /// </summary>
        /// <value><c>true</c> if strema texture; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Stream the texture dynamically at runtime. This improves performance and loading times. Not recommended for important textures you always want to be loaded, such as splash screens
        /// </userdoc>
        [DataMember(65)]
        [DefaultValue(false)]
        [Display("Stream")]
        public bool IsStreamable { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to convert the texture in pre-multiply alpha.
        /// </summary>
        /// <value><c>true</c> to convert the texture in pre-multiply alpha.; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Pre-multiply all color components of the images by their alpha-component.
        /// Use this when elements are rendered with standard blending (not transitive blending).
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(true)]
        [Display(category: "Parameters")]
        public bool PremultiplyAlpha { get; set; } = true;

        /// <summary>
        /// Gets or sets the sprites of the sheet.
        /// </summary>
        /// <userdoc>
        /// The parameters used to pack the sprites into atlases
        /// </userdoc>
        [NotNull]
        [DataMember(100)]
        [Category("Atlas Packing")]
        public PackingAttributes Packing { get; set; } = new PackingAttributes();

        /// <summary>
        /// Gets or sets the sprites of the sheet.
        /// </summary>
        /// <userdoc>
        /// The list of sprites in the sheet
        /// </userdoc>
        [DataMember(150)]
        [Category]
        [MemberCollection(NotNullItems = true)]
        public List<SpriteInfo> Sprites { get; set; } = new List<SpriteInfo>();

        /// <summary>
        /// Retrieves Url for a texture given absolute path and sprite index
        /// </summary>
        /// <param name="textureAbsolutePath">Absolute Url of a texture</param>
        /// <param name="spriteIndex">Sprite index</param>
        public static string BuildTextureUrl(UFile textureAbsolutePath, int spriteIndex)
        {
            return textureAbsolutePath + "__IMAGE_TEXTURE__" + spriteIndex;
        }

        /// <summary>
        /// Retrieves Url for an atlas texture given absolute path and atlas index
        /// </summary>
        /// <param name="textureAbsolutePath">Absolute Url of an atlas texture</param>
        /// <param name="atlasIndex">Atlas index</param>
        public static string BuildTextureAtlasUrl(UFile textureAbsolutePath, int atlasIndex)
        {
            return textureAbsolutePath + "__ATLAS_TEXTURE__" + atlasIndex;
        }
    }
}
