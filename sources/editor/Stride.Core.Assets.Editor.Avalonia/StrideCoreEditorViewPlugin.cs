// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Views;

namespace Stride.Core.Assets.Editor.Avalonia;

public sealed class StrideCoreEditorViewPlugin : AssetsEditorPlugin
{
    public override void InitializePlugin(ILogger logger)
    {
        // nothing for now
    }

    public override void InitializeSession(ISessionViewModel session)
    {
        // nothing for now
    }

    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
        // nothing for now
    }

    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        // nothing for now
    }

    public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, ISessionViewModel session)
    {
        // Same processors as the WPF CoreAssetsEditorPlugin
        pasteProcessors.Add(new AssetPropertyPasteProcessor());
        pasteProcessors.Add(new AssetItemPasteProcessor((SessionViewModel)session));
    }

    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
        // nothing for now
    }

    public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
        foreach (var (_, value) in new DefaultPropertyTemplateProviders())
        {
            if (value is TemplateProviderBase provider)
            {
                templateProviders.Add(provider);
            }
        }
    }
}
