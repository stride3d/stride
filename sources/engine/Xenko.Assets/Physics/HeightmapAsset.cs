// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;
using Xenko.Physics;

namespace Xenko.Assets.Physics
{
    [DataContract("HeightmapAsset")]
    [CategoryOrder(20, "Convert", Expand = ExpandRule.Always)]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Heightmap))]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "3.0.0.0")]
    public partial class HeightmapAsset : AssetWithSource
    {
        private const string CurrentVersion = "3.0.0.0";

        public const string FileExtension = ".xkhmap";

        [DataMember(10)]
        [Display("Height", category: "Convert", Expand = ExpandRule.Always)]
        [NotNull]
        public IHeightmapHeightConversionParameters HeightConversionParameters { get; set; } = new FloatHeightmapHeightConversionParamters();

        [DataMember(20, "Resize")]
        [Display(category: "Convert")]
        [NotNull]
        public HeightmapResizingParameters Resizing { get; set; } = new HeightmapResizingParameters();

        [DataMember(50, "sRGB sampling")]
        [Display(category: "Convert")]
        public bool IsSRgb { get; set; } = false;
    }
}
