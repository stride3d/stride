// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Graphics;

namespace Stride.Heightfield.Assets;

[Display((int)AssetDisplayPriority.Textures, "Normal From Height")]
[DataContract("NormalFromHeightAsset")]
[AssetDescription(FileExtension, AllowArchetype = false)]
[AssetContentType(typeof(Texture))]
[AssetFormatVersion(StrideConfig.LogicalPackageName, CurrentVersion, "1.0.0.0")]
public sealed class NormalFromHeightAsset : Asset
{
    private const string CurrentVersion = "1.0.0.0";
    public const string FileExtension = ".sdhnrm";

    [Display(null, "Input")]
    public required Texture HeightInput { get; set; }

    /// <userdoc>
    /// Is the texture expected to tile or fit
    /// </userdoc>
    [DefaultValue(WrapModes.Repeat)]
    [Display(null, "Input")]
    public WrapModes WrapMode { get; set; } = WrapModes.Repeat;

    /// <userdoc>
    /// The height of the topmost peak used in game, changing this value changes the intensity of the generated normals.
    /// Providing accurate values would result in more accurate normals.
    /// </userdoc>
    [DefaultValue(1f)]
    [Display(null, "Input")]
    public float HeightInUnits { get; set; } = 1f;

    /// <userdoc>
    /// The width or side length of the quad the heightmap is laid on in game, changing this value changes the intensity of the generated normals.
    /// Providing accurate values would result in more accurate normals.
    /// </userdoc>
    [DefaultValue(1f)]
    [Display(null, "Input")]
    public float SizeInUnits { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    /// <userdoc>
    /// The width of the texture in-game. The value is a percentage or the actual pixel size depending on whether Use percentages is enabled.
    /// </userdoc>
    [DefaultValue(100.0f)]
    [DataMemberRange(0, 100, 1, 10, 1)]
    [Display(null, "Resolution")]
    public float Width { get; set; } = 100.0f;

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    /// <userdoc>
    /// The height of the texture in-game. The value is a percentage or the actual pixel size depending on whether Use percentages is enabled.
    /// </userdoc>
    [DefaultValue(100.0f)]
    [DataMemberRange(0, 100, 1, 10, 1)]
    [Display(null, "Resolution")]
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
    [DefaultValue(true)]
    [Display("Use percentages", "Resolution")]
    public bool IsSizeInPercentage { get; set; } = true;

    /// <summary>
    /// Indicating whether the X-component of normals should be inverted.
    /// </summary>
    /// <userdoc>
    /// Indicates that a positive X-component (red) faces right in tangent space.
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Invert X", "Format")]
    public bool InvertX { get; set; } = true;

    /// <summary>
    /// Indicating whether the Y-component of normals should be inverted, to compensate for a flipped tangent-space.
    /// </summary>
    /// <userdoc>
    /// Indicates that a positive Y-component (green) faces up in tangent space.
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Invert Y", "Format")]
    public bool InvertY { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate mipmaps.
    /// </summary>
    /// <value><c>true</c> if mipmaps are generated; otherwise, <c>false</c>.</value>
    /// <userdoc>
    /// Generate mipmaps for the texture
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Generate mipmaps", "Format")]
    public bool GenerateMipmaps { get; set; } = true;

    /// <summary>
    /// Compress the final texture to a format based on the target platform and usage. The final texture must be a multiple of 4
    /// </summary>
    /// <userdoc>
    /// Compress the final texture to a format based on the target platform and usage. The final texture must be a multiple of 4.
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Compress", "Format")]
    public bool Compress { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to stream texture.
    /// </summary>
    /// <value><c>true</c> if stream texture; otherwise, <c>false</c>.</value>
    /// <userdoc>
    /// Stream the texture dynamically at runtime. This improves performance and loading times. Not recommended for important textures you always want to be loaded, such as splash screens
    /// </userdoc>
    [DefaultValue(true)]
    [Display("Stream", "Format")]
    public bool IsStreamable { get; set; } = true;

    public enum WrapModes
    {
        Repeat,
        Clamp
    }
}
