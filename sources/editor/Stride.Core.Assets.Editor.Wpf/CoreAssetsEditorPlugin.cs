// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;

#nullable enable

namespace Stride.Core.Assets.Editor;

/// <summary>
/// The WPF views of the core editor: the enum images of its image dictionary, and the paste processors of the properties
/// and of the assets.
/// </summary>
internal sealed class CoreAssetsEditorPlugin : AssetsEditorPlugin
{
    private ResourceDictionary? imageDictionary;

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        imageDictionary ??= (ResourceDictionary)Application.LoadComponent(new Uri("/Stride.Core.Assets.Editor.Wpf;component/View/ImageDictionary.xaml", UriKind.RelativeOrAbsolute));
        foreach (var entry in imageDictionary.Keys)
        {
            if (entry is Enum && imageDictionary[entry] is { } image)
            {
                context.EnumImages.Register(entry, image);
            }
        }

        context.PasteProcessors.Register(new AssetPropertyPasteProcessor());
        context.PasteProcessors.Register(new AssetItemPasteProcessor((SessionViewModel)context.Session));
    }
}
