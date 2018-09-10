// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Assets
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
            var gameSettingsAsset = package.FindAsset(GameSettingsAsset.GameSettingsLocation);
            if (gameSettingsAsset == null && package.TemporaryAssets.Count > 0)
            {
                gameSettingsAsset = package.TemporaryAssets.Find(x => x.Location == GameSettingsAsset.GameSettingsLocation);
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
