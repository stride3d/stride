// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.ObjectModel;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Core.Quantum.References;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.Scripts;

namespace Xenko.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public class VisualScriptLinkViewModel : DispatcherViewModel, IAssetPropertyProviderViewModel
    {
        private readonly Link link;

        public VisualScriptLinkViewModel(VisualScriptMethodEditorViewModel method, Link link, VisualScriptSlotViewModel sourceSlot, VisualScriptSlotViewModel targetSlot) : base(method.SafeArgument(nameof(method)).ServiceProvider)
        {
            this.Method = method;
            this.Editor = method.Editor;
            this.link = link;
            this.SourceSlot = sourceSlot;
            this.TargetSlot = targetSlot;
        }

        public VisualScriptMethodEditorViewModel Method { get; }

        public VisualScriptSlotViewModel SourceSlot { get; }

        public VisualScriptSlotViewModel TargetSlot { get; }

        public VisualScriptEditorViewModel Editor { get; }

        // TODO: Listen for changes? Probably not necessary since a link shouldn't be able to change type (if we disallow connecting to incompatible slots)
        public SlotKind Kind => Link.Source.Kind;

        public ObservableCollection<VisualScriptEditorViewModel.Diagnostic> Diagnostics { get; } = new ObservableCollection<VisualScriptEditorViewModel.Diagnostic>();

        internal Link Link => link;

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Editor.Asset;

        /// <inheritdoc/>
        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            var asset = Method.Editor.Asset.Asset;
            var path = new GraphNodePath(Method.Editor.Session.AssetNodeContainer.GetNode(asset));
            path.PushMember(nameof(VisualScriptAsset.Methods));
            path.PushTarget();
            path.PushIndex(new NodeIndex(asset.Methods.IndexOf(Method.Method.Method)));
            path.PushMember(nameof(Scripts.Method.Links));
            path.PushTarget();
            path.PushIndex(new NodeIndex(link.Id));
            path.PushTarget();
            return path;
        }

        IObjectNode IPropertyProviderViewModel.GetRootNode() => Editor.Session.AssetNodeContainer.GetNode(link);

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;
        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
    }
}
