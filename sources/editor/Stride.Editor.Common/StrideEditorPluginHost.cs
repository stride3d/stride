// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;

#nullable enable

namespace Stride.Editor;

/// <summary>
/// The plugin host of the Stride editors: it adds the asset previews to what <see cref="EditorPluginHost"/> registers.
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
    /// The assembly of a <see cref="StrideAssetsPlugin"/> also declares its preview view models (<see cref="AssetPreviewViewModelAttribute"/>)
    /// and preview views (<see cref="AssetPreviewViewAttribute"/>).
    /// </remarks>
    protected override void ScanPlugin(AssetsPlugin plugin, IEditorPluginContext context)
    {
        base.ScanPlugin(plugin, context);

        if (plugin is not StrideAssetsPlugin)
            return;

        var assembly = plugin.GetType().Assembly;
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
