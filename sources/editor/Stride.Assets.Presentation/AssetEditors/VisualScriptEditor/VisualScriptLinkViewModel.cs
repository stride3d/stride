// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.ObjectModel;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
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
