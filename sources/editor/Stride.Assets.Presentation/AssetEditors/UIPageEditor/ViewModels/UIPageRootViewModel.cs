// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIPageEditor.ViewModels
{
    public sealed class UIPageRootViewModel : UIRootViewModel, IAddChildViewModel
    {
        /// <summary>
        /// A UIPage asset cannot have more than one root.
        /// </summary>
        internal static readonly string OneRootOnly = "A UIPage asset can't have more than one root.";

        public UIPageRootViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIPageViewModel asset, UIElementDesign rootElement)
            : base(editor, asset, rootElement?.Yield())
        {
            NotifyGameSidePartAdded().Forget();
        }

        /// <inheritdoc />
        [NotNull]
        public override string Name { get => "UI Page"; set => throw new NotSupportedException("Can't change the name of a UIPage object."); }

        [CanBeNull]
        public UIElementViewModel RootElement => RootElements.SingleOrDefault();

        /// <inheritdoc />
        public override void ReplaceRootElement(PanelViewModel sourcePanel, AssetCompositeHierarchyData<UIElementDesign, UIElement> hierarchy, Guid targetPanelId)
        {
            if (sourcePanel == null) throw new ArgumentNullException(nameof(sourcePanel));
            if (sourcePanel.Id.ObjectId != Asset.Asset.Hierarchy.RootParts.Single().Id)
                throw new ArgumentException(@"The given source panel does not match the currently set root element", nameof(sourcePanel));

            Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(sourcePanel.UIElementDesign);
            Asset.AssetHierarchyPropertyGraph.AddPartToAsset(hierarchy.Parts, hierarchy.Parts[targetPanelId], null, Asset.Asset.Hierarchy.RootParts.Count);
        }

        /// <inheritdoc />
        // ReSharper disable once RedundantAssignment
        protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, ref string message)
        {
            if (RootElement != null)
                return ((IAddChildViewModel)RootElement).CanAddChildren(children, AddChildModifiers.None, out message);

            var count = children.Count;
            if (count == 0)
            {
                message = "Empty selection";
                return false;
            }

            if (count == 1)
                return true;

            message = OneRootOnly;
            return false;
        }

        /// <inheritdoc />
        protected override void OnRootUIElementsChanged(ItemChangeEventArgs e)
        {
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    if (UIAsset.Hierarchy.RootParts.Count != 1)
                        throw new InvalidOperationException(OneRootOnly);
                    Editor.ActiveRoot = RootElement;
                    break;

                case ContentChangeType.CollectionRemove:
                    if (UIAsset.Hierarchy.RootParts.Count != 0)
                        throw new InvalidOperationException(OneRootOnly);
                    break;
            }
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            if (children.Count != 1)
                return;

            if (RootElement != null)
            {
                ((IAddChildViewModel)RootElement).AddChildren(children, modifiers);
                return;
            }

            MoveChildren(children, Children.Count);
        }
    }
}
