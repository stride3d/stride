// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Editor.Annotations;
using Stride.Editor.Build;
using Stride.Editor.Preview.ViewModels;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Editor;

public sealed class StrideEditorPlugin : AssetsEditorPlugin
{
    public override void InitializePlugin(ILogger logger)
    {
        // nothing for now
    }

    public override void InitializeSession(ISessionViewModel session)
    {
        // FIXME xplat-editor
        //var fallbackDirectory = UPath.Combine(EditorSettings.FallbackBuildCacheDirectory, new UDirectory(StrideGameStudio.EditorName));
        var fallbackDirectory = UPath.Combine(Path.Combine(Path.GetTempPath(), "Stride", "BuildCache"), new UDirectory("Stride Game Studio"));
        var buildDirectory = fallbackDirectory;
        try
        {
            // FIXME xplat-editor
            var package = session.CurrentProject /*?? session.LocalPackages.First()*/;
            if (package != null)
            {
                // In package, we override editor build directory to be per-project and be shared with game build directory
                buildDirectory = $"{package.PackagePath.GetFullDirectory().ToOSPath()}\\obj\\stride\\assetbuild\\data";
            }

            // Attempt to create the directory to ensure it is valid.
            if (!Directory.Exists(buildDirectory))
                Directory.CreateDirectory(buildDirectory);
        }
        catch (Exception)
        {
            buildDirectory = fallbackDirectory;
        }

        var settingsProvider = new GameSettingsProviderService(session);
        session.ServiceProvider.RegisterService(settingsProvider);

        var builderService = new GameStudioBuilderService(session, settingsProvider, buildDirectory);
        session.ServiceProvider.RegisterService(builderService);

        var thumbnailService = new GameStudioThumbnailService(session, settingsProvider, builderService);
        session.ServiceProvider.RegisterService(thumbnailService);
    }

    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IAssetPreviewViewModel).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetPreviewViewModelAttribute>() is { } attribute)
            {
                assetPreviewViewModelTypes.Add(attribute.AssetPreviewType, type);
            }
        }
    }

    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        // nothing for now
    }
}
