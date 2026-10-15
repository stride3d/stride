// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor;

internal sealed class CoreAssetsEditorPlugin : AssetsEditorPlugin
{
    private ResourceDictionary? imageDictionary;

    public override void InitializePlugin(ILogger logger)
    {
        imageDictionary ??= (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Core.Assets.Editor.Wpf;component/View/ImageDictionary.xaml", UriKind.RelativeOrAbsolute));
    }

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

    public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
        pasteProcessors.Add(new AssetPropertyPasteProcessor());
        pasteProcessors.Add(new AssetItemPasteProcessor(session));
    }
}
