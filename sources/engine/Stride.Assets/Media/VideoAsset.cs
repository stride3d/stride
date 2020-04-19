// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Audio;

namespace Stride.Assets.Media
{
    [DataContract("Video")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Video.Video))]
    [CategoryOrder(10, "Size")]
    [CategoryOrder(15, "Trimming")]
    [CategoryOrder(20, "Audio")]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.1.0.0")]
    public partial class VideoAsset : Asset, IAssetWithSource
    {
        public VideoAsset()
        {
            VideoDuration.EndTime = System.TimeSpan.MaxValue;
        }

        private const string CurrentVersion = "2.1.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="VideoAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdvid";

        /// <summary>
        /// The source file of this asset.
        /// </summary>
        /// <userdoc>
        /// The source file of this asset.
        /// </userdoc>
        [DataMember(-50)]
        [DefaultValue(null)]
        [SourceFileMember(true)]
        public UFile Source { get; set; } = new UFile("");

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        /// <userdoc>
        /// The width of the video asset. The value is a percentage or the actual pixel size depending on whether Use percentages is enabled.
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
        /// The height of the video asset. The value is a percentage or the actual pixel size depending on whether Use percentages is enabled.
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
        /// Gets or set the start and end time of the video.
        /// </summary>
        /// <userdoc>Trim the video by specifying the start and end times.</userdoc>
        [DataMember(45)]
        [InlineProperty]
        [Display("Trimming", "Trimming")]
        public VideoAssetDuration VideoDuration;

        /// <summary>
        /// If <c>true</c>, the compiler will re-encode the video's audio track to a mono channel.
        /// </summary>
        /// <userdoc>
        /// If true, the compiler will re-encode the video's audio track to a mono channel.
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(false)]
        [Display("Force Mono channel", "Audio")]
        public bool IsAudioChannelMono { get; set; } = false;
    }
}
