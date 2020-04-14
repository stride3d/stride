// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public abstract class SharedRendererBlockBaseViewModel : GraphicsCompositorBlockViewModel
    {
        private readonly Dictionary<SharedRendererReferenceKey, IGraphicsCompositorSlotViewModel> outputSlotMap = new Dictionary<SharedRendererReferenceKey, IGraphicsCompositorSlotViewModel>();

        protected SharedRendererBlockBaseViewModel([NotNull] GraphicsCompositorEditorViewModel editor)
            : base(editor)
        {
        }

        public override bool UpdateSlots()
        {
            var externalReferences = SharedRendererReferenceVisitor.GetExternalReferences(Editor.Asset.PropertyGraph.Definition, GetNodesContainingReferences());

            // Check if our list of slots has changed
            var shouldRegenerate = outputSlotMap.Any(x => !externalReferences.ContainsKey(x.Key)) || externalReferences.Any(x => !outputSlotMap.ContainsKey(x.Key));
            if (shouldRegenerate)
            {
                // If so, regenerate all of them (to easily handle insertion, index changes, etc. - in the future we could handle fine-update)
                foreach (var slot in OutputSlots.Cast<GraphicsCompositorSlotViewModel>().ToList())
                {
                    slot.Destroy();
                }
                OutputSlots.Clear();
                outputSlotMap.Clear();
                foreach (var externalReference in externalReferences)
                {
                    var name = SharedRendererOutputSlotViewModel.ComputeName(externalReference.Key);
                    var slot = new SharedRendererOutputSlotViewModel(this, name, externalReference.Key);
                    outputSlotMap.Add(externalReference.Key, slot);
                    OutputSlots.Add(slot);
                }
            }
            return shouldRegenerate;
        }

        protected abstract IEnumerable<IGraphNode> GetNodesContainingReferences();

        private class SharedRendererReferenceVisitor : AssetGraphVisitorBase
        {
            private readonly Dictionary<SharedRendererReferenceKey, ISharedRenderer> externalReferences = new Dictionary<SharedRendererReferenceKey, ISharedRenderer>();

            private SharedRendererReferenceVisitor(AssetPropertyGraphDefinition propertyGraphDefinition)
                : base(propertyGraphDefinition)
            {
            }

            public static Dictionary<SharedRendererReferenceKey, ISharedRenderer> GetExternalReferences(AssetPropertyGraphDefinition propertyGraphDefinition, IEnumerable<IGraphNode> rootNodes)
            {
                var visitor = new SharedRendererReferenceVisitor(propertyGraphDefinition);
                foreach (var rootNode in rootNodes)
                    visitor.Visit(rootNode);
                return visitor.externalReferences;
            }

            protected override void VisitMemberTarget(IMemberNode node)
            {
                ProcessReferences(node, NodeIndex.Empty, node.Type);
                base.VisitMemberTarget(node);
            }

            protected override void VisitItemTargets(IObjectNode node)
            {
                node.ItemReferences?.ForEach(x => ProcessReferences(node, x.Index, node.ItemReferences.ElementType));
                base.VisitItemTargets(node);
            }

            private void ProcessReferences(IGraphNode node, NodeIndex index, Type type)
            {
                if (node == null)
                    return;

                if (typeof(IGraphicsRendererBase).IsAssignableFrom(type))
                {
                    // Check if there is a setter
                    if (!((node as IMemberNode)?.MemberDescriptor.HasSet ?? true))
                        return;

                    var value = index == NodeIndex.Empty ? node.Retrieve() : node.Retrieve(index);

                    // Hide inlined renderers which don't have nodes
                    if ((value != null) && ((value as ISharedRenderer) == null))
                        return;

                    var path = CurrentPath.Clone();
                    if (index != NodeIndex.Empty)
                        path.PushIndex(index);
                    externalReferences.Add(new SharedRendererReferenceKey(path, node.Type), value as ISharedRenderer);
                }
            }
        }
    }
}
