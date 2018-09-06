// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Diagnostics;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Serialization;
using Xenko.Assets.Entities;
using Xenko.Engine;

namespace Xenko.Assets.Analysis
{
    /// <summary>
    /// Analyses a game package, checks the default scene exists.
    /// </summary>
    public sealed class GamePackageSessionAnalysis : PackageSessionAnalysisBase
    {
        /// <summary>
        /// Checks if a default scene exists for this game package.
        /// </summary>
        /// <param name="log">The log to output the result of the validation.</param>
        public override void Run(ILogger log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            foreach (var project in Session.Projects.OfType<SolutionProject>())
            {
                // Make sure package has its assets loaded
                var package = project.Package;
                if (package == null || package.State < PackageState.AssetsReady)
                    continue;

                var hasGameExecutable = project.FullPath != null && project.Type == ProjectType.Executable;
                if (!hasGameExecutable)
                {
                    continue;
                }

                // Find game settings
                var gameSettingsAssetItem = package.Assets.Find(GameSettingsAsset.GameSettingsLocation);
                AssetItem defaultScene = null;

                // If game settings is found, try to find default scene inside
                var defaultSceneRuntime = ((GameSettingsAsset)gameSettingsAssetItem?.Asset)?.DefaultScene;
                var defaultSceneReference = AttachedReferenceManager.GetAttachedReference(defaultSceneRuntime);
                if (defaultSceneReference != null)
                {
                    // Find it either by Url or Id
                    defaultScene = package.Assets.Find(defaultSceneReference.Id) ?? package.Assets.Find(defaultSceneReference.Url);

                    // Check it is actually a scene asset
                    if (defaultScene != null && !(defaultScene.Asset is SceneAsset))
                        defaultScene = null;
                }

                // Find or create default scene
                if (defaultScene == null)
                {
                    defaultScene = package.Assets.Find(GameSettingsAsset.DefaultSceneLocation);
                    if (defaultScene != null && !(defaultScene.Asset is SceneAsset))
                        defaultScene = null;
                }

                // Otherwise, try to find any scene
                if (defaultScene == null)
                    defaultScene = package.Assets.FirstOrDefault(x => x.Asset is SceneAsset);

                // Create game settings if not done yet
                if (gameSettingsAssetItem == null)
                {
                    log.Error(package, null, AssetMessageCode.AssetForPackageNotFound, GameSettingsAsset.GameSettingsLocation, package.FullPath.GetFileNameWithoutExtension());

                    var gameSettingsAsset = GameSettingsFactory.Create();

                    if (defaultScene != null)
                        gameSettingsAsset.DefaultScene = AttachedReferenceManager.CreateProxyObject<Scene>(defaultScene.Id, defaultScene.Location);

                    gameSettingsAssetItem = new AssetItem(GameSettingsAsset.GameSettingsLocation, gameSettingsAsset);
                    package.Assets.Add(gameSettingsAssetItem);

                    gameSettingsAssetItem.IsDirty = true;
                }
            }
        }
   }
}
