// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Xenko.Data;
using Xenko.Engine;
using Xenko.Engine.Design;
using Xenko.Rendering.Compositing;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Assets
{
    /// <summary>
    /// Settings for a game with the default scene, resolution, graphics profile...
    /// </summary>
    [DataContract("GameSettingsAsset")]
    [AssetDescription(FileExtension, AlwaysMarkAsRoot = true, AllowArchetype = false)]
    [ContentSerializer(typeof(DataContentSerializer<GameSettingsAsset>))]
    [AssetContentType(typeof(GameSettings))]
    [CategoryOrder(4050, "Splash screen")]
    [NonIdentifiableCollectionItems]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.1.0.3")]
    [AssetUpgrader(XenkoConfig.PackageName, "2.1.0.3", "3.1.0.1", typeof(RenderingSplitUpgrader))]
    public partial class GameSettingsAsset : Asset
    {
        private const string CurrentVersion = "3.1.0.1";

        /// <summary>
        /// The default file extension used by the <see cref="GameSettingsAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkgamesettings";

        public const string GameSettingsLocation = GameSettings.AssetUrl;

        public const string DefaultSceneLocation = "MainScene";

        /// <summary>
        /// Gets or sets the default scene
        /// </summary>
        /// <userdoc>The default scene loaded when the game starts</userdoc>
        [DataMember(1000)]
        public Scene DefaultScene { get; set; }

        [DataMember(1500)]
        public GraphicsCompositor GraphicsCompositor { get; set; }

        /// <userdoc>
        /// The image (eg company logo) displayed as the splash screen
        /// </userdoc>
        [Display("Texture", "Splash screen")]
        [DataMember(5000)]
        public Texture SplashScreenTexture { get; set; }

        /// <userdoc>
        /// The color the splash screen fades in on top of
        /// </userdoc>
        [Display("Color", "Splash screen")]
        [DataMember(5050)]
        public Color SplashScreenColor { get; set; } = Color.Black;

        /// <userdoc>
        /// If checked, the splash screen is display in VR double view.
        /// </userdoc>
        [DefaultValue(false)]
        [Display("Double screen", "Splash screen")]
        [DataMember(5100)]
        public bool DoubleViewSplashScreen { get; set; } = false;

        [DataMember(2000)]
        [MemberCollection(ReadOnly = true, NotNullItems = true)]
        public List<Configuration> Defaults { get; } = new List<Configuration>();

        [DataMember(3000)]
        [Category]
        public List<ConfigurationOverride> Overrides { get; } = new List<ConfigurationOverride>();

        [DataMember(4000)]
        [Category]
        public List<string> PlatformFilters { get; } = new List<string>();

        /// <summary>
        /// Tries to get the requested <see cref="Configuration"/>, returns null if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The <see cref="Configuration"/> to get</typeparam>
        /// <param name="profile">If not null, will filter the results by profile first</param>
        /// <returns></returns>
        public T TryGet<T>() where T : Configuration
        {
            foreach (var x in Defaults)
            {
                if (x?.GetType() == typeof(T))
                    return (T)x;
            }

            return null;
        }

        public T GetOrCreate<T>() where T : Configuration, new()
        {
            Configuration first = null;
            foreach (var x in Defaults)
            {
                if (x != null && x.GetType() == typeof(T))
                {
                    first = x;
                    break;
                }
            }
            var settings = (T)first;
            if (settings != null) return settings;
            settings = ObjectFactoryRegistry.NewInstance<T>();
            Defaults.Add(settings);
            return settings;
        }

        public T GetOrCreate<T>(PlatformType platform) where T : Configuration, new()
        {
            ConfigPlatforms configPlatform;
            switch (platform)
            {
                case PlatformType.Windows:
                    configPlatform = ConfigPlatforms.Windows;
                    break;
                case PlatformType.Android:
                    configPlatform = ConfigPlatforms.Android;
                    break;
                case PlatformType.iOS:
                    configPlatform = ConfigPlatforms.iOS;
                    break;
                case PlatformType.UWP:
                    configPlatform = ConfigPlatforms.UWP;
                    break;
                case PlatformType.Linux:
                    configPlatform = ConfigPlatforms.Linux;
                    break;
                case PlatformType.macOS:
                    configPlatform = ConfigPlatforms.macOS;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
            var platVersion = Overrides.FirstOrDefault(x => x != null && x.Platforms.HasFlag(configPlatform) && x.Configuration is T);
            if (platVersion != null)
            {
                return (T)platVersion.Configuration;
            }

            return GetOrCreate<T>();
        }

        // In 3.1, Xenko.Engine was splitted into a sub-assembly Xenko.Rendering
        private class RenderingSplitUpgrader : AssetUpgraderBase
        {
            protected override void UpgradeAsset(AssetMigrationContext context, PackageVersion currentVersion, PackageVersion targetVersion, dynamic asset, PackageLoadingAssetFile assetFile, OverrideUpgraderHint overrideHint)
            {
                YamlNode assetNode = asset.Node;
                foreach (var node in assetNode.AllNodes)
                {
                    if (node.Tag == "!Xenko.Streaming.StreamingSettings,Xenko.Engine")
                    {
                        node.Tag = node.Tag.Replace(",Xenko.Engine", ",Xenko.Rendering");
                    }
                }
            }
        }
    }
}
