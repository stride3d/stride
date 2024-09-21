// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;

namespace Stride.Core.Assets.Editor;

public abstract class AssetsEditorPlugin : AssetsPlugin
{
    public void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(AssetEditorViewModel).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } attribute)
            {
                assetEditorViewModelTypes.Add(attribute.ViewModelType, type);
            }
        }
    }

    public void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IAssetEditorView).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetEditorViewAttribute>() is { } attribute)
            {
                assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
            }
        }
    }

    public abstract void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes);

    public abstract void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes);
}
