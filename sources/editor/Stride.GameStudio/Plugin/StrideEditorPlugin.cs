// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Editor.Build;
using Stride.Editor.Preview;
using Stride.Editor.Thumbnails;
using Stride.GameStudio.Debugging;
using Stride.GameStudio.Helpers;
using Stride.GameStudio.ViewModels;

namespace Stride.GameStudio.Plugin
{
    internal sealed class StrideEditorPlugin : StrideAssetsPlugin
    {
        public bool EnableThumbnailService { get; set; } = true;

        protected override void Initialize(ILogger logger)
        {
        }

        public override void InitializeSession(SessionViewModel session)
        {
            var fallbackDirectory = UPath.Combine(EditorSettings.FallbackBuildCacheDirectory, new UDirectory(StrideGameStudio.EditorName));
            var buildDirectory = fallbackDirectory;
            try
            {
                var currentPackage = (session.CurrentProject ?? session.LocalPackages.FirstOrDefault())?.Package;
                if (currentPackage != null)
                {
                    // Editor build DB lives under the shared game library (the package that owns
                    // GameSettings), not the startup platform head — so adding/removing/regenerating a
                    // head can't relocate or corrupt it. FindAsset resolves through dependencies, so
                    // from a head this lands on its game library; falls back to the current package.
                    var buildPackage = currentPackage.FindAsset(Stride.Assets.GameSettingsAsset.GameSettingsLocation)?.Package ?? currentPackage;
                    buildDirectory = new UFile($"{buildPackage.FullPath.GetFullDirectory()}\\obj\\stride\\assetbuild\\data").ToOSPath();
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
            foreach (var stridePlugin in pluginService.Plugins.OfType<StrideAssetsPlugin>())
            {
                var pluginTypes = stridePlugin.GetType().Assembly.GetTypes();
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

            if (EnableThumbnailService)
            {
                var thumbnailService = new GameStudioThumbnailService(session, settingsProvider, builderService);
                session.ServiceProvider.RegisterService(thumbnailService);
            }

            var strideDebugService = new StrideDebugService(session.ServiceProvider);
            session.ServiceProvider.RegisterService(strideDebugService);

            GameStudioViewModel.GameStudio.Preview = new PreviewViewModel(session);
            GameStudioViewModel.GameStudio.Debugging = new DebuggingViewModel(GameStudioViewModel.GameStudio, strideDebugService);
        }

        public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
        {
            // nothing for now
        }
    }
}
