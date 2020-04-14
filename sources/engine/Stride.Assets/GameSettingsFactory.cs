// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Assets.Textures;
using Xenko.Audio;
using Xenko.Graphics;
using Xenko.Navigation;
using Xenko.Physics;
using Xenko.Streaming;

namespace Xenko.Assets
{
    public class GameSettingsFactory : AssetFactory<GameSettingsAsset>
    {
        [NotNull]
        public static GameSettingsAsset Create()
        {
            var asset = new GameSettingsAsset();

            asset.SplashScreenTexture = AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("d26edb11-10bd-403c-b3c2-9c7fcccf25e5"), "XenkoDefaultSplashScreen");
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
            asset.GetOrCreate<NavigationSettings>();
            asset.GetOrCreate<PhysicsSettings>();
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
