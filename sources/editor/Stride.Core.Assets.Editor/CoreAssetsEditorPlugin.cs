// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.View;

#nullable enable

namespace Stride.Core.Assets.Editor;

internal sealed class CoreAssetsEditorPlugin : AssetsEditorPlugin
{
    private ResourceDictionary? imageDictionary;

    /// <inheritdoc />
    public override void InitializePlugin(ILogger logger)
    {
        imageDictionary ??= (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Core.Assets.Editor;component/View/ImageDictionary.xaml", UriKind.RelativeOrAbsolute));
    }

    /// <inheritdoc />
    public override void InitializeSession(SessionViewModel session)
    {
    }

    /// <inheritdoc />
    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
    }

    /// <inheritdoc />
    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
    }

    /// <inheritdoc />
    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
    }

    /// <inheritdoc />
    public override void RegisterEnumImages(IDictionary<object, object> enumImages)
    {
        if (imageDictionary is null) return;

        foreach (var entry in imageDictionary.Keys)
        {
            if (entry is Enum && imageDictionary[entry] is { } image)
            {
                enumImages.Add(entry, image);
            }
        }
    }

    /// <inheritdoc />
    public override void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
    {
    }

    /// <inheritdoc />
    public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
        pasteProcessors.Add(new AssetPropertyPasteProcessor());
        pasteProcessors.Add(new AssetItemPasteProcessor(session));
    }

    /// <inheritdoc />
    public override void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postePasteProcessors, SessionViewModel session)
    {
    }

    /// <inheritdoc />
    public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
    }
}
