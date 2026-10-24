// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.FFmpeg;
using Stride.Core.Assets;
using Stride.Core.IO;

namespace Stride.Video.Assets
{
    public class RawVideoAssetImporter : RawAssetImporterBase<VideoAsset>
    {
        // Supported file extensions for this importer
        public const string FileExtensions = ".avi,.mkv,.mov,.mp4";
        private static readonly Guid Uid = new Guid("48b6a448-08fd-4db3-bef7-82b9b8b19437");

        public override Guid Id => Uid;

        public override string Description => "Raw video importer for creating Video assets";

        public override string SupportedFileExtensions => FileExtensions;

        /// <summary>
        /// The trimming range covers the whole file. A file without a video stream gives no asset; a file ffmpeg cannot
        /// read (or no ffmpeg) gives one asset with an open range, and its compilation reports the problem.
        /// </summary>
        protected override IEnumerable<AssetItem> Import(UFile rawAssetPath, UFile location, VideoAsset asset)
        {
            var media = FFmpegTool.TryProbe(rawAssetPath.ToOSPath());
            if (media != null && !media.Streams.Any(x => x.Kind == FFmpegStreamKind.Video))
                return [];

            asset.VideoDuration.StartTime = TimeSpan.Zero;
            asset.VideoDuration.EndTime = media?.Duration ?? TimeSpan.MaxValue;
            return base.Import(rawAssetPath, location, asset);
        }
    }
}
