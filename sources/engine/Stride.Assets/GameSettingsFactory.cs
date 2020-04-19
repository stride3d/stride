// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Assets.Textures;
using Stride.Audio;
using Stride.Graphics;
using Stride.Navigation;
using Stride.Physics;
using Stride.Streaming;

namespace Stride.Assets
{
    public class GameSettingsFactory : AssetFactory<GameSettingsAsset>
    {
        [NotNull]
        public static GameSettingsAsset Create()
        {
            var asset = new GameSettingsAsset();

            asset.SplashScreenTexture = AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("d26edb11-10bd-403c-b3c2-9c7fcccf25e5"), "StrideDefaultSplashScreen");
            asset.SplashScreenColor = Color.Black;

            //add default filters, todo maybe a config file somewhere is better
            asset.PlatformFilters.Add("PowerVR SGX 54[0-9]");
            asset.PlatformFilters.Add("Adreno \\(TM\\) 2[0-9][0-9]");
            asset.PlatformFilters.Add("Adreno (TM) 320");
            asset.PlatformFilters.Add("Adreno (TM) 330");
            asset.PlatformFilters.Add("Adreno \\(TM\\) 4[0-9][0-9]");
            asset.PlatformFilters.Add("NVIDIA Tegra");
            asset.PlatformFilters.Add("Intel(R) HD Graphics");
            asset.PlatformFilters.Add("^Mali\\-4");
            asset.PlatformFilters.Add("^Mali\\-T6");
            asset.PlatformFilters.Add("^Mali\\-T7");

            asset.GetOrCreate<AudioEngineSettings>();
            asset.GetOrCreate<EditorSettings>();
            asset.GetOrCreate<RenderingSettings>();
            asset.GetOrCreate<StreamingSettings>();
            asset.GetOrCreate<TextureSettings>();

            return asset;
        }

        /// <inheritdoc/>
        [NotNull]
        public override GameSettingsAsset New()
        {
            return Create();
        }
    }
}
