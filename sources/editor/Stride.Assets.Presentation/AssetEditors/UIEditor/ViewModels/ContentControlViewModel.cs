// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.UI;
using Stride.UI.Controls;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    internal sealed class ContentControlViewModel : UIElementViewModel
    {
        private readonly IMemberNode contentNode;

        // Note: constructor needed by UIElementViewModelFactory
        public ContentControlViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign)
            : base(editor, asset, elementDesign, GetOrCreateChildPartDesigns((UIAssetBase)asset.Asset, elementDesign))
        {
            contentNode = editor.NodeContainer.GetOrCreateNode(AssetSideUIElement)[nameof(ContentControl.Content)];
            contentNode.ValueChanged += ContentChanged;
        }

        public ContentControl AssetSideControl => (ContentControl)AssetSideUIElement;

        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(ContentControlViewModel));
            contentNode.ValueChanged -= ContentChanged;
            base.Destroy();
        }

        /// <inheritdoc/>
        protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, ref string message)
        {
            if (AssetSideControl.Content != null)
            {
                message = $"{GetDropLocationName()} already has a content";
                return false;
            }
            if (children.Count > 1)
            {
                message = $"{GetDropLocationName()} can only have one child as content";
                return false;
            }

            return true;
        }

        private static IEnumerable<UIElementDesign> GetOrCreateChildPartDesigns([NotNull] UIAssetBase asset, [NotNull] UIElementDesign partDesign)
        {
            var assetControl = (ContentControl)partDesign.UIElement;

            if (assetControl.Content != null)
            {
                if (!asset.Hierarchy.Parts.TryGetValue(assetControl.Content.Id, out UIElementDesign elementDesign))
                {
                    elementDesign = new UIElementDesign(assetControl.Content);
                }
                if (assetControl.Content != elementDesign.UIElement) throw new InvalidOperationException();
                yield return elementDesign;
            }
        }

        private async void ContentChanged(object sender, MemberNodeChangeEventArgs e)
        {
            var oldElement = e.OldValue as UIElement;
            var newElement = e.NewValue as UIElement;
            switch (e.ChangeType)
            {
                case ContentChangeType.ValueChange:
                    if (oldElement != null)
                    {
                        var partId = new AbsoluteId(Asset.Id, oldElement.Id);
                        var viewModel = (UIElementViewModel)Editor.FindPartViewModel(partId);
                        RemoveChildViewModel(viewModel);
                    }
                    if (newElement != null)
                    {
                        var elementDesign = UIAsset.Hierarchy.Parts[newElement.Id];
                        var viewModel = (UIElementViewModel)Editor.CreatePartViewModel(Asset, elementDesign);
                        AddItem(viewModel);

                        // Collect children that need to be notified
                        var childrenToNotify = viewModel.Children.BreadthFirst(x => x.Children).ToList();
                        await Editor.Controller.AddPart(this, viewModel.AssetSideUIElement);

                        // Manually notify the game-side scene
                        viewModel.NotifyGameSidePartAdded().Forget();
                        foreach (var child in childrenToNotify)
                        {
                            child.NotifyGameSidePartAdded().Forget();
                        }
                    }
                    else
                    {
                        Editor.Controller.InvokeAsync(() => ((ContentControl)Editor.Controller.FindGameSidePart(Id)).Content = null).Forget();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
