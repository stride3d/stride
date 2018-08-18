// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Assets.Media;
using Xenko.Video.FFmpeg;

namespace Xenko.Assets.Presentation.Templates
{
    internal sealed class SoundFromFileTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static readonly SoundFromFileTemplateGenerator Default = new SoundFromFileTemplateGenerator();

        public static Guid DefaultSoundId = new Guid("FE4CC415-19A4-4F6D-9FF1-1DB00D94BD05");
        public static Guid MusicSoundId = new Guid("1EB85AB4-B652-4A32-AD7B-9B7423380872");
        public static Guid SpatializedSoundId = new Guid("62124263-A299-42CF-A724-F962AE8773E8");

        private SoundFromFileTemplateGenerator()
        {
            // Initialize ffmpeg
            FFmpegUtils.PreloadLibraries();
            FFmpegUtils.Initialize();
        }

        /// <inheritdoc />
        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            return base.IsSupportingTemplate(templateDescription) &&
                (templateDescription.Id == DefaultSoundId ||
                 templateDescription.Id == MusicSoundId ||
                 templateDescription.Id == SpatializedSoundId);
        }

        /// <inheritdoc />
        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var importedAssets = new List<AssetItem>();
            using (var media = new FFmpegMedia())
            {
                foreach (var assetItem in base.CreateAssets(parameters))
                {
                    if (assetItem.Asset is SoundAsset soundAsset)
                    {
                        media.Open(soundAsset.Source.ToWindowsPath());
                        foreach( var audioTrack in media.Streams.OfType<AudioStream>().ToList())
                        {
                            var assetCopy = AssetCloner.Clone(soundAsset);
                            assetCopy.Index = audioTrack.Index;
                            assetCopy.SampleRate = audioTrack.SampleRate;

                            importedAssets.Add(new AssetItem(assetItem.Location + " track " + audioTrack.Index, assetCopy));
                        }
                    }
                }
            }

            return MakeUniqueNames(importedAssets);
        }
    }
}
