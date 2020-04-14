// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Rendering;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class GraphicsCompositorAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var asset = node.Asset?.Asset as GraphicsCompositorAsset;
            if (asset == null)
                return;

            if (typeof(IGraphicsRendererBase).IsAssignableFrom(node.Type) || typeof(IList<ISceneRenderer>).IsAssignableFrom(node.Type))
            {
                // Independently of what CollectionPropertyNodeUpdater might have decided, we want to make sure we have Null and the list of inherited types
                IEnumerable<AbstractNodeEntry> types = AbstractNodeEntryNodeUpdater.FillDefaultAbstractNodeEntry(node);
                
                // Remove all shared types
                types = types.Where(x => !(x is AbstractNodeType && typeof(ISharedRenderer).IsAssignableFrom(((AbstractNodeType)x).Type)));
                // Add shared references from asset parts
                var type = node.Descriptor.GetInnerCollectionType();
                types = types.Concat(asset.SharedRenderers.Where(x => type.IsInstanceOfType(x)).Select(x => new AbstractNodeValue(x, GetSharedRendererDisplayName(x), 1)).ToList());
                node.AttachedProperties.Set(AbstractNodeEntryData.Key, types);
                var sharedRenderer = node.Value as ISharedRenderer;
                if (sharedRenderer != null)
                    node.AttachedProperties.Set(DisplayData.AttributeDisplayNameKey, GetSharedRendererDisplayName(sharedRenderer));
            }

            // TODO: Doesn't seem to work: exception "Unable to move this node, a node with the same name already exists."
            // For SceneRendererCollection, we bypass the Children node to avoid unecessary hierarchy
            //if (typeof(SceneRendererCollection).IsAssignableFrom(node.MemberInfo?.DeclaringType)
            //    && node.MemberInfo?.Name == nameof(SceneRendererCollection.Children))
            //{
            //    node.BypassNode();
            //}

            // Everything that is not in GraphicsCompositorAsset.RenderStages should become a reference
            if (node.Type == typeof(RenderStage))
            {
                var types = asset.RenderStages.Select(x => new AbstractNodeValue(x, x.Name, 1));
                if (AbstractNodeEntryNodeUpdater.IsAllowingNull(node))
                    types = AbstractNodeValue.Null.Yield().Concat(types);
                node.AttachedProperties.Set(AbstractNodeEntryData.Key, types.ToList());
                var renderStage = (RenderStage)node.Value;
                if (renderStage != null)
                {
                    node.AttachedProperties.Set(DisplayData.AttributeDisplayNameKey, renderStage.Name);
                }
            }

            if (node.Type == typeof(SceneCameraSlot))
            {
                var i = 0;
                var entries = asset.Cameras.Select(x => new AbstractNodeValue(x, x.Name, i++));
                if (AbstractNodeEntryNodeUpdater.IsAllowingNull(node))
                    entries = AbstractNodeValue.Null.Yield().Concat(entries);
                node.AttachedProperties.Set(AbstractNodeEntryData.Key, entries.ToList());
            }
        }

        protected override void FinalizeTree(IAssetNodePresenter root)
        {
            if (root.PropertyProvider is GraphicsCompositorViewModel)
            {
                root.Children.Where(x => x.Name != ArchetypeNodeUpdater.ArchetypeNodeName).ForEach(x => x.IsVisible = false);
            }
            base.FinalizeTree(root);
        }
        private static string GetSharedRendererDisplayName(ISharedRenderer sharedRenderer) => $"Shared: {DisplayAttribute.GetDisplayName(sharedRenderer.GetType())}";
    }
}
