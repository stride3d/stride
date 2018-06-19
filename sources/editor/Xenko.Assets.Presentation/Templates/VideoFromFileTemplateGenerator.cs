// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Annotations;
using Xenko.Core.IO;
using Xenko.Assets.Media;
using Xenko.Video.FFmpeg;

namespace Xenko.Assets.Presentation.Templates
{
    internal sealed class VideoFromFileTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static readonly VideoFromFileTemplateGenerator Default = new VideoFromFileTemplateGenerator();

        public static Guid Id = new Guid("B660A65C-3B61-4AE3-9DC4-E34E62E524C9");

        private VideoFromFileTemplateGenerator()
        {
            // Initialize ffmpeg
            FFmpegUtils.PreloadLibraries();
            FFmpegUtils.Initialize();
        }

        /// <inheritdoc />
        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            return base.IsSupportingTemplate(templateDescription) && templateDescription.Id == Id;
        }

        /// <inheritdoc />
        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var files = parameters.Tags.Get(SourceFilesPathKey);
            if (files == null)
                return base.CreateAssets(parameters);

            var importedAssets = new List<AssetItem>();
            foreach (var file in files)
            {
                using (var media = new FFmpegMedia())
                {
                    media.Open(file.ToWindowsPath());
                    
                    var videoStream = media.Streams.OfType<VideoStream>().FirstOrDefault();
                    if (videoStream != null)
                    {
                        var videoItem = ImportVideo(file, videoStream);
                        importedAssets.Add(videoItem);
                    }
                }
            }

            return MakeUniqueNames(importedAssets);
        }

        [NotNull]
        private static AssetItem ImportVideo([NotNull] UFile sourcePath, [NotNull] VideoStream videoStream)
        {
            var videoAsset = new VideoAsset
            {
                Source = sourcePath,
            };
            videoAsset.VideoDuration.StartTime = TimeSpan.Zero;
            videoAsset.VideoDuration.EndTime = videoStream.Duration;

            var videoUrl = new UFile(sourcePath.GetFileNameWithoutExtension());
            return new AssetItem(videoUrl, videoAsset);
        }
    }
}
