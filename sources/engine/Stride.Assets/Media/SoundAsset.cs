// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Audio;

namespace Stride.Assets.Media
{
    [DataContract("Sound")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Sound))]
    [AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public partial class SoundAsset : AssetWithSource
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="SoundAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdsnd";

        /// <summary>
        /// The track index in the file (i.e. a video files with several tracks).
        /// </summary>
        [DefaultValue(0)]
        [DataMember]
        public int Index { get; set; } = 0;

        [DefaultValue(44100)]
        public int SampleRate { get; set; } = 44100;

        [DefaultValue(10)]
        [DataMemberRange(1, 40, 1, 5, 0)]
        public int CompressionRatio { get; set; } = 10;

        public bool StreamFromDisk { get; set; }

        public bool Spatialized { get; set; }
    }
}
