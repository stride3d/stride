// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Markup.Xaml.Styling;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Editor.Avalonia;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.Avalonia.Preview;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Avalonia;

/// <summary>
/// The Avalonia views of the Stride editor layer: the property templates and the component images of this assembly, the
/// control themes of its preview views and the images of their enum values, and the preview service of the session. The
/// editor registers the preview and editor views that this assembly declares.
/// </summary>
public sealed class StrideEditorViewPlugin : StrideAssetsAvaloniaPlugin
{
    /// <summary>
    /// The dictionary of the control themes of this assembly (the preview views).
    /// </summary>
    public static readonly Uri ThemesUri = new("avares://Stride.Assets.Editor.Avalonia/Themes/Generic.axaml");

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        RegisterResourceDictionary(context, new EntityPropertyTemplateProviders());

        // The component images give the icons of the entities in the scene hierarchy, as the type images of the WPF
        // StrideDefaultAssetsPlugin
        var componentImages = new ComponentImages();
        foreach (var key in componentImages.Keys)
        {
            if (key is Type type && componentImages.TryGetValue(key, out var image) && image is not null)
                context.TypeImages.Register(type, image);
        }

        RegisterResourceDictionary(context, new PreviewEnumImages());
        RegisterControlThemes(context.Logger, new ResourceInclude(ThemesUri) { Source = ThemesUri });
    }

    /// <inheritdoc />
    public override void InitializeSession(ISessionPluginContext context)
    {
        base.InitializeSession(context);

        if (GameStudioPreviewService.DisablePreview)
            return;

        // The preview service needs the builder and the game settings of the session, which StrideEditorPlugin registers
        // first: without them (a plugin that failed, a changed order) the session has no preview rather than failing to open
        var session = context.Session;
        if (session.ServiceProvider.TryGet<GameStudioBuilderService>() is null || session.ServiceProvider.TryGet<GameSettingsProviderService>() is null)
        {
            GlobalLogger.GetLogger(nameof(StrideEditorViewPlugin)).Error("The asset preview is not available: the session has no builder or game settings service.");
            return;
        }

        context.Services.Register(new GameStudioPreviewService(session));

        // The editor games (the scene editor) render offscreen into Avalonia controls, like the preview
        context.Services.Register(new OffscreenGameViewportService(session.Dispatcher));
    }
}
