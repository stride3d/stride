// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;

namespace Xenko.Assets.Media
{
    public class MusicSoundFactory : AssetFactory<SoundAsset>
    {
        public override SoundAsset New()
        {
            return new SoundAsset { CompressionRatio = 10, SampleRate = 44100, Spatialized = false, StreamFromDisk = true };
        }
    }

    public class SpatializedSoundFactory : AssetFactory<SoundAsset>
    {
        public override SoundAsset New()
        {
            return new SoundAsset { CompressionRatio = 15, SampleRate = 44100, Spatialized = true, StreamFromDisk = false };
        }
    }

    public class DefaultSoundFactory : AssetFactory<SoundAsset>
    {
        public override SoundAsset New()
        {
            return new SoundAsset { CompressionRatio = 15, SampleRate = 44100, Spatialized = false, StreamFromDisk = false };
        }
    }
}
