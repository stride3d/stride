// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Editor.Avalonia;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.Avalonia.Preview;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Avalonia;

/// <summary>
/// The Avalonia views of the Stride editor layer: the preview views, the editor views and the property templates of this
/// assembly, and the preview service of the session.
/// </summary>
public sealed class StrideEditorViewPlugin : StrideAssetsAvaloniaPlugin
{
    /// <inheritdoc />
    protected override void Initialize(ILogger logger)
    {
        RegisterResourceDictionary(new EntityPropertyTemplateProviders());
    }

    /// <inheritdoc />
    public override void InitializeSession(ISessionViewModel session)
    {
        if (GameStudioPreviewService.DisablePreview)
            return;

        // The preview service needs the builder and the game settings of the session, which StrideEditorPlugin registers
        // first: without them (a plugin that failed, a changed order) the session has no preview rather than failing to open
        if (session.ServiceProvider.TryGet<GameStudioBuilderService>() is null || session.ServiceProvider.TryGet<GameSettingsProviderService>() is null)
        {
            GlobalLogger.GetLogger(nameof(StrideEditorViewPlugin)).Error("The asset preview is not available: the session has no builder or game settings service.");
            return;
        }

        var previewService = new GameStudioPreviewService(session);
        session.ServiceProvider.RegisterService(previewService);

        // The editor games (the scene editor) render offscreen into Avalonia controls, like the preview
        session.ServiceProvider.RegisterService(new OffscreenGameViewportService(session.Dispatcher));
    }

    /// <inheritdoc />
    public override void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
        base.RegisterAssetEditorViewTypes(assetEditorViewTypes);

        // The editor views of the cross-platform editor are Avalonia controls implementing IAssetEditorView
        foreach (var type in AssemblyRegistry.GetScanTypes(GetType().Assembly, typeof(IAssetEditorView)))
        {
            if (typeof(IAssetEditorView).IsAssignableFrom(type) && !type.IsAbstract &&
                type.GetCustomAttributes(typeof(AssetEditorViewAttribute), false) is [AssetEditorViewAttribute attribute, ..])
            {
                assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
            }
        }
    }
}
