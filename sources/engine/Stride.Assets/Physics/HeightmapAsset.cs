// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Physics;

namespace Stride.Assets.Physics
{
    [DataContract("HeightmapAsset")]
    [CategoryOrder(10, "Image File", Expand = ExpandRule.Always)]
    [CategoryOrder(20, "Convert", Expand = ExpandRule.Always)]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Heightmap))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "3.0.0.0")]
    public partial class HeightmapAsset : AssetWithSource
    {
        private const string CurrentVersion = "3.0.0.0";

        public const string FileExtension = ".sdhmap";

        /// <summary>
        /// Parameters to convert pixels to heights.
        /// </summary>
        [DataMember(10)]
        [Display("Height", category: "Convert", Expand = ExpandRule.Always)]
        [NotNull]
        public IHeightmapHeightConversionParameters HeightConversionParameters { get; set; } = new FloatHeightmapHeightConversionParamters();

        /// <summary>
        /// The size of the heightmap.
        /// </summary>
        /// <remarks>
        /// X is width and Y is length.
        /// They should be greater than or equal to 2.
        /// The heightmap size will be same to the image size, if disabled.
        /// </remarks>
        [DataMember(20, "Resize")]
        [Display(category: "Convert")]
        public HeightmapResizingParameters Resizing { get; set; } = new HeightmapResizingParameters
        {
            Enabled = false,
            Size = new Int2(1024, 1024),
        };

        /// <summary>
        /// Enable if needed to load the image file as sRGB.
        /// </summary>
        [DataMember(50, "sRGB sampling")]
        [Display(category: "Image File")]
        public bool IsSRgb { get; set; } = false;

        /// <summary>
        /// If enabled, scale each of the heights to the height range before they are stored as heightmap.
        /// </summary>
        /// <remarks>
        /// By the default, they are considered to be in [-1 .. 1] when the pixel format of the image file is floating-point component format.
        /// Match FloatingPointComponentRange to actual range.
        /// </remarks>
        [DataMember(30)]
        [Display(category: "Convert")]
        public bool ScaleToHeightRange { get; set; } = true;

        /// <summary>
        /// The range of the floating-point component.
        /// </summary>
        /// <remarks>
        /// Determine the range of the floating-point components.
        /// This property affects nothing when not floating-point component.
        /// </remarks>
        [DataMember(40)]
        [Display(category: "Image File")]
        public Vector2 FloatingPointComponentRange { get; set; } = new Vector2(-1, 1);

        /// <summary>
        /// The range of the signed short component.
        /// </summary>
        /// <remarks>
        /// Enable if the R components are signed short integer in [-32767 .. 32767].
        /// This property affects nothing when not signed short integer component.
        /// </remarks>
        [DataMember(41)]
        [Display(category: "Image File")]
        public bool IsSymmetricShortComponent { get; set; } = false;
    }
}
