// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;
using Xenko.Core.Mathematics;
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

        [DataMember(20)]
        [Display(category: "Convert")]
        [NotNull]
        public HeightfieldTypes HeightType { get; set; } = HeightfieldTypes.Float;

        [DataMember(30)]
        [Display(category: "Convert")]
        public float HeightScale { get; set; } = 1f;

        [DataMember(40, "Resize")]
        [Display(category: "Convert", Expand = ExpandRule.Always)]
        [NotNull]
        public CustomHeightmapSize Size { get; set; } = new CustomHeightmapSize();

        [DataMember(50, "sRGB sampling")]
        [Display(category: "Convert")]
        public bool IsSRgb { get; set; } = false;

        #region "HeightmapSize"
        [DataContract]
        public class CustomHeightmapSize
        {
            [DataMember(0)]
            [DefaultValue(false)]
            public bool Enabled { get; set; }

            [DataMember(10)]
            [InlineProperty]
            public Int2 Size { get; set; }
        }
        #endregion
    }
}
