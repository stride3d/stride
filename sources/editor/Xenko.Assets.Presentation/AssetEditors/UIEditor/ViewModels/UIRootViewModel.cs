// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.UI;
using Xenko.UI;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    public abstract class UIRootViewModel : UIHierarchyItemViewModel
    {
        private readonly IObjectNode rootPartsNode;

        protected UIRootViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, IEnumerable<UIElementDesign> rootElements)
            : base(editor, asset, rootElements)
        {
            rootPartsNode = Editor.NodeContainer.GetNode(UIAsset.Hierarchy)[nameof(AssetCompositeHierarchyData<UIElementDesign, UIElement>.RootParts)].Target;
            rootPartsNode.ItemChanged += RootUIElementsChanged;
        }

        [NotNull]
        public IEnumerable<UIElementViewModel> RootElements => Children;

        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(UIRootViewModel));
            rootPartsNode.ItemChanged -= RootUIElementsChanged;
            base.Destroy();
        }

        /// <inheritdoc/>
        public override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.NodeContainer.GetNode(Asset.Asset));
            path.PushMember(nameof(UIAsset.Hierarchy));
            path.PushTarget();
            return path;
        }

        /// <inheritdoc/>
        public override Task NotifyGameSidePartAdded()
        {
            // Manually notify the game-side scene
            foreach (var child in Children.BreadthFirst(x => x.Children))
            {
                child.NotifyGameSidePartAdded().Forget();
            }
            return base.NotifyGameSidePartAdded();
        }

        public abstract void ReplaceRootElement([NotNull] PanelViewModel sourcePanel, [NotNull] AssetCompositeHierarchyData<UIElementDesign, UIElement> hierarchy, Guid targetPanelId);

        /// <inheritdoc/>
        protected override string GetDropLocationName()
        {
            return Asset.Name;
        }

        protected virtual void OnRootUIElementsChanged([NotNull] ItemChangeEventArgs e)
        {
            // default implementation does nothing
        }

        private async void RootUIElementsChanged(object sender, [NotNull] ItemChangeEventArgs e)
        {
            UIElement rootElement;
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionUpdate:
                    break;
                case ContentChangeType.CollectionAdd:
                    rootElement = (UIElement)e.NewValue;
                    Editor.Logger.Verbose($"Add {rootElement.Id} to the RootElements collection");
                    break;
                case ContentChangeType.CollectionRemove:
                    rootElement = (UIElement)e.OldValue;
                    Editor.Logger.Verbose($"Remove {rootElement.Id} from the RootElements collection");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update view model and replicate changes to the game-side objects
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                var element = (UIElement)e.NewValue;
                var childDesign = UIAsset.Hierarchy.Parts[element.Id];
                var viewModel = (UIElementViewModel)Editor.CreatePartViewModel(Asset, childDesign);
                InsertItem(e.Index.Int, viewModel);

                // Collect children that need to be notified
                var childrenToNotify = viewModel.Children.BreadthFirst(x => x.Children).ToList();
                // Add the element to the game, then notify
                await Editor.Controller.AddPart(this, viewModel.AssetSideUIElement);

                // Manually notify the game-side scene
                viewModel.NotifyGameSidePartAdded().Forget();
                foreach (var child in childrenToNotify)
                {
                    child.NotifyGameSidePartAdded().Forget();
                }
            }
            else if (e.ChangeType == ContentChangeType.CollectionRemove)
            {
                var partId = new AbsoluteId(Asset.Id, ((UIElement)e.OldValue).Id);
                var element = (UIElementViewModel)Editor.FindPartViewModel(partId);
                if (element == null) throw new InvalidOperationException($"{nameof(element)} can't be null");
                RemoveChildViewModel(element);
                Editor.Controller.RemovePart(this, element.AssetSideUIElement).Forget();
            }

            OnRootUIElementsChanged(e);
        }
    }
}
