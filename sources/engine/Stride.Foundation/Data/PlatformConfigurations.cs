// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stride.Core;

namespace Stride.Data
{
    [DataContract]
    public class PlatformConfigurations
    {
        /// <summary>String identifying the rendering device, used for configuration overrides.</summary>
        public static string RendererName { get; set; } = string.Empty;

        [DataMember]
        public List<ConfigurationOverride> Configurations = [];

        [DataMember]
        public List<string> PlatformFilters = [];

        public T Get<T>() where T : Configuration, new()
        {
            // Find the default for all platforms and devices
            var config = Configurations.Where(x => x.Platforms == ConfigPlatforms.None).LastOrDefault(x => x.Configuration is T);

            // Try finding one for the specific platform or hardware configuration

            var platform = Platform.Type switch
            {
                PlatformType.Shared => ConfigPlatforms.None,
                PlatformType.Windows => ConfigPlatforms.Windows,
                PlatformType.Android => ConfigPlatforms.Android,
                PlatformType.iOS => ConfigPlatforms.iOS,
                PlatformType.UWP => ConfigPlatforms.UWP,
                PlatformType.Linux => ConfigPlatforms.Linux,
                PlatformType.macOS => ConfigPlatforms.macOS,
                _ => throw new ArgumentOutOfRangeException(),
            };

            // Find per platform if available
            if (Configurations.Where(x => x.Platforms.HasFlag(platform) && x.SpecificFilter == -1)
                .LastOrDefault(x => x.Configuration is T) is { } platformConfig)
            {
                config = platformConfig;
            }

            // Find per specific renderer
            if (Configurations.Where(x => x.Platforms.HasFlag(platform) && x.SpecificFilter != -1 && new Regex(PlatformFilters[x.SpecificFilter], RegexOptions.IgnoreCase).IsMatch(RendererName))
                .LastOrDefault(x => x.Configuration is T) is { } rendererConfig)
            {
                config = rendererConfig;
            }

            if (config == null)
            {
                // If the requested configuration doesn't exist, create and add a new one
                var newInstance = new T();
                Configurations.Add(new()
                {
                    Configuration = newInstance,
                });

                return newInstance;
            }

            return (T)config.Configuration;
        }
    }
}
