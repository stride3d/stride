// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;

#nullable enable

namespace Stride.Editor;

/// <summary>
/// The plugin host of the Stride editors: it adds the asset previews and the tools of the game editor views to what
/// <see cref="EditorPluginHost"/> registers.
/// </summary>
public abstract class StrideEditorPluginHost : EditorPluginHost
{
    /// <inheritdoc />
    protected StrideEditorPluginHost(IReadOnlyList<AssetsPlugin>? plugins = null, IDispatcherService? dispatcher = null)
        : base(plugins, dispatcher)
    {
    }

    /// <inheritdoc />
    protected override Type AssetPreviewType => typeof(IAssetPreview);

    /// <inheritdoc />
    protected override Type AssetPreviewViewModelType => typeof(IAssetPreviewViewModel);

    /// <inheritdoc />
    protected override Type AssetPreviewViewType => typeof(IPreviewView);

    /// <inheritdoc />
    /// <remarks>
    /// The types that the assets serialize as references (the content types: textures, models, ...): the property grid shows
    /// them as references, not as the objects they point to.
    /// </remarks>
    protected override IEnumerable<Type> FindPrimitiveTypes(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var serializerType = SerializerSelector.AssetWithReuse.GetSerializer(type)?.GetType();
            if (serializerType is { IsGenericType: true } && serializerType.GetGenericTypeDefinition() == typeof(ReferenceSerializer<>))
                yield return type;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The assembly of a plugin also declares the tools of the views of the game editors (<see cref="EditorViewToolAttribute"/>).
    /// The assembly of a <see cref="StrideAssetsPlugin"/> also declares its preview view models (<see cref="AssetPreviewViewModelAttribute"/>)
    /// and preview views (<see cref="AssetPreviewViewAttribute"/>).
    /// </remarks>
    protected override void ScanPlugin(AssetsPlugin plugin, IEditorPluginContext context)
    {
        base.ScanPlugin(plugin, context);

        var assembly = plugin.GetType().Assembly;
        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(EditorViewToolAttribute)))
        {
            // Each view creates its own instance of a tool
            if (type.IsClass && !type.IsAbstract && !type.IsGenericTypeDefinition && type.GetConstructor(Type.EmptyTypes) is not null)
                context.EditorViewToolTypes.Register(type);
        }

        if (plugin is not StrideAssetsPlugin)
            return;

        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(IAssetPreviewViewModel)))
        {
            if (typeof(IAssetPreviewViewModel).IsAssignableFrom(type) && type.GetCustomAttribute<AssetPreviewViewModelAttribute>() is { } attribute)
                context.AssetPreviewViewModelTypes.Register(attribute.AssetPreviewType, type);
        }

        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(IPreviewView)))
        {
            if (!typeof(IPreviewView).IsAssignableFrom(type))
                continue;

            foreach (var attribute in type.GetCustomAttributes<AssetPreviewViewAttribute>())
                context.AssetPreviewViewTypes.Register(attribute.AssetPreviewType, type);
        }
    }
}
