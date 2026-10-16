// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Data;

namespace Stride.Assets
{
    /// <summary>
    /// Declares that compiling an asset of the attributed type reads the <see cref="ConfigurationType"/> section of
    /// the game settings, so the editor recompiles those assets when that section changes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class GameSettingsDependencyAttribute : Attribute
    {
        public GameSettingsDependencyAttribute(Type configurationType)
        {
            if (!typeof(Configuration).IsAssignableFrom(configurationType))
                throw new ArgumentException($"[{configurationType}] is not a {nameof(Configuration)}", nameof(configurationType));

            ConfigurationType = configurationType;
        }

        public Type ConfigurationType { get; }

        /// <summary>
        /// The game settings sections compiling an asset of the given type reads.
        /// </summary>
        public static IEnumerable<Type> GetSections(Type assetType)
        {
            return assetType.GetCustomAttributes<GameSettingsDependencyAttribute>().Select(x => x.ConfigurationType);
        }

        /// <summary>
        /// The game settings sections the registered asset types read.
        /// </summary>
        public static IEnumerable<Type> GetDeclaredSections()
        {
            return AssetRegistry.GetPublicTypes().SelectMany(GetSections).Distinct();
        }
    }
}
