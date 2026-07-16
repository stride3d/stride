// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.IO;
using Stride.Graphics;

namespace Stride.Assets
{
    public static class GameSettingsAssetExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="GameSettingsAsset"/> from the given <see cref="Package"/> if available, or null otherwise.
        /// </summary>
        /// <param name="package">The package from which to retrieve the game settings.</param>
        /// <returns>The <see cref="GameSettingsAsset"/> from the given <see cref="Package"/> if available. Null otherwise.</returns>
        public static GameSettingsAsset GetGameSettingsAsset(this Package package)
        {
            // Searched per package: namespaced packages root their asset locations /Namespace/...
            static AssetItem FindInPackage(Package candidate)
            {
                var location = new UFile(candidate.Container?.AssetNamespace is { } assetNamespace
                    ? $"/{assetNamespace}/{GameSettingsAsset.GameSettingsLocation}"
                    : GameSettingsAsset.GameSettingsLocation);
                return candidate.Assets.Find(location) ?? candidate.TemporaryAssets.Find(x => x.Location == location);
            }

            var gameSettingsAsset = FindInPackage(package);
            if (gameSettingsAsset == null && package.Container != null)
            {
                foreach (var dependency in package.Container.FlattenedDependencies)
                {
                    if (dependency.Package != null && (gameSettingsAsset = FindInPackage(dependency.Package)) != null)
                        break;
                }
            }
            return gameSettingsAsset?.Asset as GameSettingsAsset;
        }

        /// <summary>
        /// Retrieves the <see cref="GameSettingsAsset"/> from the given <see cref="Package"/> if available,
        /// or otherwise attempts to retrieve it from the from the <see cref="PackageSession.CurrentPackage"/> of the session.
        /// If none is available, this method returns a new default instance.
        /// </summary>
        /// <param name="package">The package from which to retrieve the game settings.</param>
        /// <returns>The <see cref="GameSettingsAsset"/> from either the given package or the session if available. A new default instance otherwise.</returns>
        public static GameSettingsAsset GetGameSettingsAssetOrDefault(this Package package)
        {
            var gameSettings = package.GetGameSettingsAsset();
            if (gameSettings == null)
            {
                gameSettings = package.Session.CurrentProject?.Package.GetGameSettingsAsset();
            }
            return gameSettings ?? GameSettingsFactory.Create();
        }

        /// <summary>
        /// Retrieves the <see cref="GameSettingsAsset"/> from the <see cref="PackageSession.CurrentPackage"/> of the given session if available,
        /// or a new default instance otherwise.
        /// </summary>
        /// <param name="session">The package session from which to retrieve the game settings.</param>
        /// <returns>The <see cref="GameSettingsAsset"/> from the given session if available. A new default instance otherwise.</returns>
        private static GameSettingsAsset GetGameSettingsAssetOrDefault(this PackageSession session)
        {
            return session.CurrentProject?.Package.GetGameSettingsAsset() ?? GameSettingsFactory.Create();
        }

        /// <summary>
        /// Retrieves the reference <see cref="ColorSpace"/> to use according to the <see cref="PackageSession.CurrentPackage"/> of the given package.
        /// If the current package is null, this method returns the value of <see cref="RenderingSettings.DefaultColorSpace"/>.
        /// </summary>
        /// <param name="session">The package session from which to retrieve the color space.</param>
        /// <param name="platform">The platform for which to return the color space.</param>
        /// <returns>The color space of the current package of the session, or <see cref="RenderingSettings.DefaultColorSpace"/>.</returns>
        public static ColorSpace GetReferenceColorSpace(this PackageSession session, PlatformType platform)
        {
            return GetGameSettingsAssetOrDefault(session).GetOrCreate<RenderingSettings>(platform).ColorSpace;
        }
    }
}
