// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.FFmpeg;
using Stride.Core.Assets;
using Stride.Core.IO;

namespace Stride.Assets.Media
{
    public class RawSoundAssetImporter : RawAssetImporterBase<SoundAsset>
    {
        // Supported file extensions for this importer
        public const string FileExtensions = ".wav,.mp3,.ogg,.aac,.aiff,.flac,.m4a,.wma,.mpc," + RawVideoAssetImporter.FileExtensions;

        private static readonly Guid Uid = new Guid("634842fa-d1db-45c2-b13d-bc11486dae4d");

        public override Guid Id => Uid;

        public override string Description => "Raw sound importer for creating SoundEffect assets";

        public override string SupportedFileExtensions => FileExtensions;

        /// <summary>
        /// One asset per audio track of the file, named after the track when there are several. A file ffmpeg cannot
        /// read (or no ffmpeg) gives one asset with the defaults; its compilation reports the problem.
        /// </summary>
        protected override IEnumerable<AssetItem> Import(UFile rawAssetPath, UFile location, SoundAsset asset)
        {
            var media = FFmpegTool.TryProbe(rawAssetPath.ToOSPath());
            if (media == null)
                return base.Import(rawAssetPath, location, asset);

            asset.Source = rawAssetPath;
            return ImportTracks(location, asset, media.Streams.Where(x => x.Kind == FFmpegStreamKind.Audio).ToList());
        }

        private static IEnumerable<AssetItem> ImportTracks(UFile location, SoundAsset asset, List<FFmpegStreamInfo> audioTracks)
        {
            foreach (var audioTrack in audioTracks)
            {
                var trackAsset = AssetCloner.Clone(asset);
                trackAsset.Index = audioTrack.Index;
                trackAsset.SampleRate = audioTrack.SampleRate ?? trackAsset.SampleRate;
                var trackLocation = audioTracks.Count > 1 ? (UFile)(location + " track " + audioTrack.Index) : location;
                yield return new AssetItem(trackLocation, trackAsset);
            }
        }
    }
}
