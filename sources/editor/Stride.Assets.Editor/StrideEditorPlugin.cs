// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Views;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Editor.Annotations;
using Stride.Editor.Build;
using Stride.Editor.Preview.ViewModels;

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
            var package = session.LocalPackages.First();
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

        var settingsProvider = new GameSettingsProviderService((SessionViewModel)session);
        session.ServiceProvider.RegisterService(settingsProvider);

        var builderService = new GameStudioBuilderService((SessionViewModel)session, settingsProvider, buildDirectory);
        session.ServiceProvider.RegisterService(builderService);

        //var thumbnailService = new GameStudioThumbnailService((SessionViewModel)session, settingsProvider, builderService);
        //session.ServiceProvider.RegisterService(thumbnailService);

        if (session is SessionViewModel sessionVm)
        {
            // commands
            sessionVm.ActiveProperties.RegisterNodePresenterCommand(new FetchEntityCommand());
            sessionVm.ActiveProperties.RegisterNodePresenterCommand(new SetComponentReferenceCommand());
            sessionVm.ActiveProperties.RegisterNodePresenterCommand(new SetEntityReferenceCommand());

            // updaters
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new AnimationAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new CameraSlotNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new EntityHierarchyAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new GameSettingsAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new GraphicsCompositorAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new MaterialAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new ModelAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new SkeletonAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new SpriteSheetAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new TextureAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new UIAssetNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new UnloadableObjectPropertyNodeUpdater());
            sessionVm.ActiveProperties.RegisterNodePresenterUpdater(new VideoAssetNodeUpdater());
        }
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

    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
        foreach (var type in AssemblyRegistry.FindAll().SelectMany(x => x.GetTypes()))
        {
            var serializer = SerializerSelector.AssetWithReuse.GetSerializer(type);
            if (serializer?.GetType() is { IsGenericType: true } serializerType && serializerType.GetGenericTypeDefinition() == typeof(ReferenceSerializer<>))
            {
                primitiveTypes.Add(type);
            }
        }

        primitiveTypes.Add(typeof(AssetReference));
        primitiveTypes.Add(typeof(UrlReferenceBase));
    }

    public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
        // nothing for now
    }
}
