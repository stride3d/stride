// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Data
{
    [Flags]
    public enum ConfigPlatforms
    {
        None = 0,
        Windows = 1 << PlatformType.Windows,
        UWP = 1 << PlatformType.UWP,
        iOS = 1 << PlatformType.iOS,
        Android = 1 << PlatformType.Android,
        Linux = 1 << PlatformType.Linux,
        macOS = 1 << PlatformType.macOS
    }

    [DataContract(Inherited = true)]
    public abstract class Configuration
    {
        [DataMemberIgnore]
        public bool OfflineOnly { get; protected set; }
    }

    [DataContract]
    public class ConfigurationOverride
    {
        [DataMember(10)]
        [InlineProperty]
        public ConfigPlatforms Platforms;

        [DataMember(20)]
        [DefaultValue(-1)]
        public int SpecificFilter = -1;

        [DataMember(30)]
        public Configuration Configuration;
    }
}
