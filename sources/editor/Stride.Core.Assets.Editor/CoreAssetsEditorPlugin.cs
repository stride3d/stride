// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;

namespace Stride.Core.Assets.Editor;

internal sealed class CoreAssetsEditorPlugin : AssetsEditorPlugin
{
    public override void InitializePlugin(ILogger logger)
    {
        // nothing for now
    }

    public override void InitializeSession(ISessionViewModel session)
    {
        if (session.ServiceProvider.TryGet<ICopyPasteService>() is { } copyPasteService)
        {
            copyPasteService.RegisterProcessor(new AssetItemPasteProcessor(session));
            copyPasteService.RegisterProcessor(new AssetPropertyPasteProcessor());
        }
    }

    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
        // nothing for now
    }

    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        // nothing for now
    }

    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
        // nothing for now
    }

    public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
        // nothing for now
    }
}
