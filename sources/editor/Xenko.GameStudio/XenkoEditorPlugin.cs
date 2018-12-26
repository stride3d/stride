// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Packages;
using Xenko.Editor;
using Xenko.Editor.Build;
using Xenko.Editor.Preview;
using Xenko.Editor.Thumbnails;
using Xenko.GameStudio.Debugging;

namespace Xenko.GameStudio
{
    public class XenkoEditorPlugin : XenkoAssetsPlugin
    {
        protected override void Initialize(ILogger logger)
        {
        }

        public override void InitializeSession(SessionViewModel session)
        {
            var fallbackDirectory = UPath.Combine(EditorSettings.FallbackBuildCacheDirectory, new UDirectory(XenkoGameStudio.EditorName));
            var buildDirectory = fallbackDirectory;
            try
            {
                var package = session.CurrentProject ?? session.LocalPackages.First();
                if (package != null)
                {
                    // In package, we override editor build directory to be per-project and be shared with game build directory
                    buildDirectory = $"{package.PackagePath.GetFullDirectory().ToWindowsPath()}\\obj\\xenko\\assetbuild\\data";
                }

                // Attempt to create the directory to ensure it is valid.
                if (!Directory.Exists(buildDirectory))
                    Directory.CreateDirectory(buildDirectory);
            }
            catch (Exception)
            {
                buildDirectory = fallbackDirectory;
            }

            var pluginService = session.ServiceProvider.Get<IAssetsPluginService>();
            var previewFactories = new Dictionary<Type, AssetPreviewFactory>();
            foreach (var xenkoPlugin in pluginService.Plugins.OfType<XenkoAssetsPlugin>())
            {
                var pluginTypes = xenkoPlugin.GetType().Assembly.GetTypes();
                foreach (var type in pluginTypes)
                {
                    var localType = type;
                    if (typeof(IAssetPreview).IsAssignableFrom(type))
                    {
                        var previewAttribute = type.GetCustomAttribute<AssetPreviewAttribute>();
                        if (previewAttribute != null)
                        {
                            previewFactories.Add(previewAttribute.AssetType, (builder, game, asset) => (IAssetPreview)Activator.CreateInstance(localType));
                        }
                    }
                }
            }

            var settingsProvider = new GameSettingsProviderService(session);
            session.ServiceProvider.RegisterService(settingsProvider);

            var builderService = new GameStudioBuilderService(session, settingsProvider, buildDirectory);
            session.ServiceProvider.RegisterService(builderService);
            session.ServiceProvider.RegisterService(builderService.Database); // TODO: this should be removed, the AssetBuilderService is reachable from anywhere now

            var previewService = new GameStudioPreviewService(session);
            previewService.RegisterAssetPreviewFactories(previewFactories);
            session.ServiceProvider.RegisterService(previewService);

            var thumbnailService = new GameStudioThumbnailService(session, settingsProvider, builderService);
            session.ServiceProvider.RegisterService(thumbnailService);

            var xenkoDebugService = new XenkoDebugService(session.ServiceProvider);
            session.ServiceProvider.RegisterService(xenkoDebugService);

            GameStudioViewModel.GameStudio.Preview = new PreviewViewModel(session);
            GameStudioViewModel.GameStudio.Debugging = new DebuggingViewModel(GameStudioViewModel.GameStudio, xenkoDebugService);
        }
    }
}
