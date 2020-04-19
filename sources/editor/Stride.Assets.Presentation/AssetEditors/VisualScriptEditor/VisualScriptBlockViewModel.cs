// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public class VisualScriptBlockViewModel : DispatcherViewModel, IAssetPropertyProviderViewModel, IAddChildViewModel
    {
        private readonly Block block;
        private readonly IObjectNode blockNode;
        private readonly GraphNodeChangeListener graphNodeListener;

        public VisualScriptBlockViewModel(VisualScriptMethodEditorViewModel method, Block block) : base(method.SafeArgument(nameof(method)).ServiceProvider)
        {
            this.Method = method;
            this.block = block;
            this.blockNode = method.Editor.Session.AssetNodeContainer.GetOrCreateNode(block);

            var propertyGraph = method.Editor.Session.GraphContainer.TryGetGraph(method.Editor.Asset.Id);

            // If anything changes in the block, trigger a PropertyChanged on Title to force it to refresh
            graphNodeListener = new AssetGraphNodeChangeListener(blockNode, propertyGraph.Definition);
            graphNodeListener.Initialize();
            graphNodeListener.ValueChanging += GraphNodeListener_Changing;
            graphNodeListener.ValueChanged += GraphNodeListener_Changed;
        }

        public override void Destroy()
        {
            graphNodeListener.ValueChanging -= GraphNodeListener_Changing;
            graphNodeListener.ValueChanged -= GraphNodeListener_Changed;
            graphNodeListener.Dispose();
            base.Destroy();
        }

        public VisualScriptMethodEditorViewModel Method { get; }

        /// <summary>
        /// Gets the title of this block.
        /// </summary>
        public string Title => block.Title;

        public Int2 Position
        {
            get { return (Int2)blockNode[nameof(Block.Position)].Retrieve(); }
            set { blockNode[nameof(Block.Position)].Update(value); }
        }

        public event EventHandler<MemberNodeChangeEventArgs> PositionChanged
        {
            add { blockNode[nameof(Block.Position)].ValueChanged += value; }
            remove { blockNode[nameof(Block.Position)].ValueChanged -= value; }
        }

        public Dictionary<Slot, VisualScriptSlotViewModel> Slots { get; } = new Dictionary<Slot, VisualScriptSlotViewModel>();

        public ObservableCollection<VisualScriptEditorViewModel.Diagnostic> Diagnostics { get; } = new ObservableCollection<VisualScriptEditorViewModel.Diagnostic>();

        internal Block Block => block;

        internal IGraphNode BlockNode => blockNode;

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Method.Editor.Asset;

        /// <inheritdoc/>
        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            var asset = Method.Editor.Asset.Asset;
            var path = new GraphNodePath(Method.Editor.Session.AssetNodeContainer.GetNode(asset));
            path.PushMember(nameof(VisualScriptAsset.Methods));
            path.PushTarget();
            path.PushIndex(new NodeIndex(asset.Methods.IndexOf(Method.Method.Method)));
            path.PushMember(nameof(Scripts.Method.Blocks));
            path.PushTarget();
            path.PushIndex(new NodeIndex(block.Id));
            path.PushTarget();
            return path;
        }

        IObjectNode IPropertyProviderViewModel.GetRootNode() => blockNode;

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;

        private void GraphNodeListener_Changing(object sender, MemberNodeChangeEventArgs e)
        {
            // If member has RegenerateTitleAttribute, let's trigger a view update
            var memberContent = e.Member;
            if (memberContent?.MemberDescriptor.GetCustomAttributes<RegenerateTitleAttribute>(true).Any() ?? false)
                OnPropertyChanging(nameof(Title));
        }

        private async void GraphNodeListener_Changed(object sender, MemberNodeChangeEventArgs e)
        {
            var memberContent = e.Member;

            // If member has RegenerateTitleAttribute, let's trigger a view update
            if (memberContent?.MemberDescriptor.GetCustomAttributes<RegenerateTitleAttribute>(true).Any() ?? false)
                OnPropertyChanged(nameof(Title));

            // If member has RegenerateSlotsAttribute, let's Regenerate slots
            if (memberContent?.MemberDescriptor.GetCustomAttributes<RegenerateSlotsAttribute>(true).Any() ?? false)
            {
                using (Method.Editor.Session.UndoRedoService?.CreateTransaction(TransactionFlags.KeepParentsAlive))
                {
                    await Method.Method.RegenerateSlots(block);
                }
            }
        }

        #region Drag & Drop

        public bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            if (children.Count != 1)
            {
                message = "Multiple elements selected.";
                return false;
            }

            var child = children.Single();

            var droppedObject = GetDroppedUnderlyingObject(child);
            if (droppedObject == null)
            {
                message = "Can't drop an object of this type here.";
                return false;
            }

            var selectedMember = FindDropProperty(droppedObject);

            if (selectedMember == null)
            {
                message = "Couldn't find any property to set on this block.";
                return false;
            }

            message = $"Replace property {selectedMember.Name} with [{droppedObject}]";
            return true;
        }

        public void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            // This shouldn't fail if CanAddChildren succeeded
            var child = children.Single();
            var droppedObject = GetDroppedUnderlyingObject(child);
            var selectedMember = FindDropProperty(droppedObject);

            selectedMember.Update(droppedObject);
        }

        private static object GetDroppedUnderlyingObject(object child)
        {
            // Get the underlying object behind this view model
            // Note: not sure if that's OK or if we should make another dedicated interface for that?
            var droppedUnderlyingObject = (child as IPropertyProviderViewModel)?.GetRootNode().Retrieve();
            return droppedUnderlyingObject;
        }

        private IMemberNode FindDropProperty(object droppedObject)
        {
            // Check each property having the attribute BlockDropTargetAttribute
            foreach (var targetMember in blockNode.Members.Where(x => x?.MemberDescriptor.GetCustomAttributes<BlockDropTargetAttribute>(true).Any() ?? false))
            {
                // Is dropped type compatible?
                if (targetMember.Type.IsInstanceOfType(droppedObject))
                {
                    // If yes, return it
                    return targetMember;
                }
            }

            return null;
        }

        #endregion
    }
}
