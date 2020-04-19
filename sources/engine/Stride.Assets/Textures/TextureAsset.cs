// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.Graphics;

namespace Stride.Assets.Textures
{
    /// <summary>
    /// Describes a texture asset.
    /// </summary>
    [DataContract("Texture")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Texture))]
    [CategoryOrder(10, "Size")]
    [CategoryOrder(20, "Format")]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed partial class TextureAsset : AssetWithSource
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="TextureAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdtex";

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        /// <userdoc>
        /// The width of the texture in-game. The value is a percentage or the actual pixel size depending on whether Use percentages is enabled.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(100.0f)]
        [DataMemberRange(0, 100, 1, 10, 1)]
        [Display(null, "Size")]
        public float Width { get; set; } = 100.0f;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        /// <userdoc>
        /// The height of the texture in-game. The value is a percentage or the actual pixel size depending on whether Use percentages is enabled.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(100.0f)]
        [DataMemberRange(0, 100, 1, 10, 1)]
        [Display(null, "Size")]
        public float Height { get; set; } = 100.0f;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is using size in percentage. Default is true. See remarks.
        /// </summary>
        /// <value><c>true</c> if this instance is dimension absolute; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// When this property is true (by default), <see cref="Width"/> and <see cref="Height"/> are epxressed 
        /// in percentage, with 100.0f being 100% of the current size, and 50.0f half of the current size, otherwise
        /// the size is in absolute pixels.
        /// </remarks>
        /// <userdoc>
        /// Use percentages for width and height instead of actual pixel size
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(true)]
        [Display("Use percentages", "Size")]
        public bool IsSizeInPercentage { get; set; } = true;

        /// <summary>
        /// Compress the final texture to a format based on the target platform and usage. The final texture must be a multiple of 4
        /// </summary>
        /// <userdoc>
        /// Compress the final texture to a format based on the target platform and usage. The final texture must be a multiple of 4.
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(true)]
        [Display("Compress")]
        public bool IsCompressed { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to generate mipmaps.
        /// </summary>
        /// <value><c>true</c> if mipmaps are generated; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Generate mipmaps for the texture
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(true)]
        [Display("Generate mipmaps", "Format")]
        public bool GenerateMipmaps { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to stream texture.
        /// </summary>
        /// <value><c>true</c> if strema texture; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// Stream the texture dynamically at runtime. This improves performance and loading times. Not recommended for important textures you always want to be loaded, such as splash screens
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(true)]
        [Display("Stream")]
        public bool IsStreamable { get; set; } = true;

        /// <summary>
        /// The description of the data contained in the texture. See remarks.
        /// </summary>
        /// <remarks>This description helps the texture compressor to select the appropriate format based on the HW Level and 
        /// platform.</remarks>
        /// <userdoc>Select Color for textures you want to display as images, Normal map for normal maps, and Greyscale to provide values for other things (eg specular maps, metalness maps, roughness maps)</userdoc>
        [DataMember(60)]
        [NotNull]
        [Display(null, "Format", Expand = ExpandRule.Always)]
        public ITextureType Type { get; set; } = new ColorTextureType();
    }
}
