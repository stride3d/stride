// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.View;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract class AssetsEditorPlugin : AssetsPlugin
{
    protected static readonly Dictionary<Type, object> TypeImages = [];

    // TODO: give access to this differently
    public readonly List<PackageSettingsEntry> ProfileSettings = [];

    public static IReadOnlyDictionary<Type, object> TypeImagesDictionary => TypeImages;

    public void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IAssetEditorViewModel).IsAssignableFrom(type) &&
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
            if (typeof(IEditorView).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetEditorViewAttribute>() is { } attribute)
            {
                assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
            }
        }
    }

    public abstract void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes);

    public abstract void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes);

    public abstract void RegisterEnumImages(IDictionary<object, object> enumImages);

    public abstract void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session);

    public abstract void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session);

    public abstract void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postePasteProcessors, SessionViewModel session);

    public abstract void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders);
}
