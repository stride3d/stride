// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview;
using Stride.Editor.Preview;
using Stride.Editor.Preview.Views;

namespace Stride.Assets.Editor.Avalonia;

public sealed class StrideEditorViewPlugin : AssetsEditorPlugin
{
    public override void InitializePlugin(ILogger logger)
    {
        // nothing for now
    }

    public override void InitializeSession(ISessionViewModel session)
    {
        var pluginService = session.ServiceProvider.Get<IAssetsPluginService>();
        var previewFactories = new Dictionary<Type, AssetPreviewFactory>();
        foreach (var stridePlugin in pluginService.Plugins.OfType<AssetsEditorPlugin>())
        {
            var pluginAssembly = stridePlugin.GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IAssetPreview).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<AssetPreviewAttribute>() is { } attribute)
                {
                    previewFactories.Add(attribute.AssetType, (_, _, _) => (IAssetPreview)Activator.CreateInstance(type)!);
                }
            }
        }

        var previewService = new GameStudioPreviewService(session);
        previewService.RegisterAssetPreviewFactories(previewFactories);
        session.ServiceProvider.RegisterService(previewService);
    }

    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
        // nothing for now
    }

    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IPreviewView).IsAssignableFrom(type))
            {
                foreach (var attribute in type.GetCustomAttributes<AssetPreviewViewAttribute>())
                {
                    assetPreviewViewTypes.Add(attribute.AssetPreviewType, type);
                }
            }
        }
    }
}
