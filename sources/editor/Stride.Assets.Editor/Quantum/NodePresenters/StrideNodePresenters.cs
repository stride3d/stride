// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Assets.Editor.Quantum.NodePresenters;

/// <summary>
/// The property grid's commands and updaters for Stride's own assets and components, which both editors register: each editor's
/// default plugin calls <see cref="Register"/> when a session opens, then adds those of its own toolkit.
/// </summary>
internal static class StrideNodePresenters
{
    /// <summary>
    /// Registers the shared node presenter commands and updaters in the session.
    /// </summary>
    /// <param name="context">The session's context of the plugin that registers them.</param>
    public static void Register(ISessionPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // commands
        context.NodePresenterCommands.Register(new FetchEntityCommand());
        context.NodePresenterCommands.Register(new SetEntityReferenceCommand());
        context.NodePresenterCommands.Register(new SetComponentReferenceCommand());
        context.NodePresenterCommands.Register(new SetSymbolReferenceCommand());
        context.NodePresenterCommands.Register(new SkeletonNodePreserveAllCommand());
        // Show the entity picker of the editor, if it has one (IEntityPickerService)
        context.NodePresenterCommands.Register(new PickupEntityCommand());
        context.NodePresenterCommands.Register(new PickupEntityComponentCommand());
        // Opens the curve editor of the editor, if it has one (ICurveEditorService)
        context.NodePresenterCommands.Register(new EditCurveCommand(context.Session));

        // updaters
        context.NodePresenterUpdaters.Register(new AnimationAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new CameraSlotNodeUpdater());
        context.NodePresenterUpdaters.Register(new EntityHierarchyAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new GameSettingsAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new GraphicsCompositorAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new FXAAEffectNodeUpdater());
        context.NodePresenterUpdaters.Register(new MaterialAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new ModelAssetNodeUpdater());
        // The material slots and morph weight sliders of the model components
        context.NodePresenterUpdaters.Register(new ModelComponentNodeUpdater());
        context.NodePresenterUpdaters.Register(new ModelNodeLinkNodeUpdater());
        context.NodePresenterUpdaters.Register(new SkeletonAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new SpriteSheetAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new TextureAssetNodeUpdater());
        context.NodePresenterUpdaters.Register(new UnloadableObjectPropertyNodeUpdater());
    }
}
