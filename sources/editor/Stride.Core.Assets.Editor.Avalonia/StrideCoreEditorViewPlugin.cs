// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Views;

namespace Stride.Core.Assets.Editor.Avalonia;

/// <summary>
/// The Avalonia views of the core editor: the default property templates, the paste processors of the properties and of
/// the assets, and the built-in commands of the property grid's context menu.
/// </summary>
public sealed class StrideCoreEditorViewPlugin : AssetsEditorPlugin
{
    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        foreach (var (_, value) in new DefaultPropertyTemplateProviders())
        {
            if (value is TemplateProviderBase provider)
            {
                context.TemplateProviders.Register(provider);
            }
        }

        // The paste processors of the core assets
        context.PasteProcessors.Register(new AssetPropertyPasteProcessor());
        context.PasteProcessors.Register(new AssetItemPasteProcessor((SessionViewModel)context.Session));

        // The nested properties toggle, in the context menu of a property
        context.Commands.Register(PropertyGridCommands.CreateToggleNestedProperties());
    }
}
