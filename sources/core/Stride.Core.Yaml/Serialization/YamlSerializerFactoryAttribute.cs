// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Attribute use to tag a class that is implementing a <see cref="IYamlSerializable"/> or <see cref="IYamlSerializableFactory"/>
    /// and will be used for asset serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class YamlSerializerFactoryAttribute : Attribute
    {
        private readonly string[] profiles;
        /// <summary>
        /// The name of the default profile. Any serializer factory that lacks this attribute will be considered to be part of the default profile.
        /// </summary>
        public const string Default = "Default";

        public YamlSerializerFactoryAttribute(params string[] profiles)
        {
            if (profiles == null || profiles.Length == 0)
            {
                throw new ArgumentException("At least one profile must be specified.");
            }
            this.profiles = profiles;
        }

        public IReadOnlyList<string> Profiles => profiles;

        public bool ContainsProfile(string profile)
        {
            return profiles.Any(x => string.Equals(x, profile, StringComparison.Ordinal));
        }

        /// <summary>
        /// Identifies if two profiles are equal.
        /// </summary>
        /// <param name="profile1">The first profile to compare.</param>
        /// <param name="profile2">The second profile to compare.</param>
        /// <returns>True if profiles are equal, false otherwise.</returns>
        public static bool AreProfilesEqual(string profile1, string profile2)
        {
            return string.Equals(profile1, profile2, StringComparison.Ordinal);
        }
    }
}
