// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    public abstract class UIHierarchyItemViewModel : AssetCompositeItemViewModel<UIBaseViewModel, UIHierarchyItemViewModel, UIElementViewModel>, IAddChildViewModel, IInsertChildViewModel
    {
        protected UIHierarchyItemViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [CanBeNull] IEnumerable<UIElementDesign> children)
            : base(editor, asset)
        {
            if (children != null)
            {
                AddItems(children.Select(child => (UIElementViewModel)Editor.CreatePartViewModel(Asset, child)));
            }
        }

        [NotNull]
        public new UIEditorBaseViewModel Editor => (UIEditorBaseViewModel)base.Editor;

        [NotNull]
        protected UIAssetBase UIAsset => (UIAssetBase)Asset.Asset;

        /// <summary>
        /// Indicates whether this instance can add or insert the given children.
        /// </summary>
        /// <param name="children">The children to add.</param>
        /// <param name="message">The feedback message that can be used in the user interface.</param>
        /// <returns><c>true</c> if this instance can add the given children, <c>false</c> otherwise.</returns>
        /// <seealso cref="IAddChildViewModel.CanAddChildren"/>
        /// <seealso cref="IInsertChildViewModel.CanInsertChildren"/>
        protected abstract bool CanAddOrInsertChildren([NotNull] IReadOnlyCollection<object> children, [NotNull] ref string message);

        /// <summary>
        /// Returns a user-friendly name used for drop operation.
        /// </summary>
        /// <returns>A user-friendly name used for drop operation</returns>
        [NotNull]
        protected virtual string GetDropLocationName()
        {
            return string.IsNullOrWhiteSpace(Name) ? "this location" : Name;
        }

        // FIXME: consider using the cut/paste mechanism for moving entities
        protected virtual void MoveChildren([NotNull] IReadOnlyCollection<object> children, int index)
        {
            if (children.Count == 0)
                return;

            var moved = false;

            // Save the selection to restore it after the operation
            var selection = Editor.SelectedContent.ToList();
            // Clear the selection
            Editor.ClearSelection();

            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                foreach (var child in children)
                {
                    if (child is UIElementViewModel element)
                    {
                        // Some of the elements we're moving might already be children of this object, let's count for their removal in the insertion index.
                        var elementIndex = Children.IndexOf(element);
                        if (elementIndex >= 0 && elementIndex < index)
                            --index;

                        // Hierarchy must be cloned before removing the elements!
                        // Note: if the source asset is different than the current asset, we need to generate new ids.
                        var flags = element.Asset == Asset ? SubHierarchyCloneFlags.None : SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
                        var hierarchy = UIAssetPropertyGraph.CloneSubHierarchies(element.Asset.Session.AssetNodeContainer, element.Asset.Asset, element.Id.ObjectId.Yield(), flags, out Dictionary<Guid, Guid> idRemapping);
                        // Remove from previous asset
                        element.Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(element.UIElementDesign);
                        // Get the id of the new element
                        if (!idRemapping.TryGetValue(element.Id.ObjectId, out Guid partId))
                            partId = element.Id.ObjectId;
                        // Insert it in the new parent, or as root if the new parent is null.
                        Asset.AssetHierarchyPropertyGraph.AddPartToAsset(hierarchy.Parts, hierarchy.Parts[partId], (this as UIElementViewModel)?.AssetSideUIElement, index++);
                        moved = true;
                        continue;
                    }
                    if (child is IUIElementFactory factory)
                    {
                        var childHierarchy = factory.Create(UIAsset);
                        Asset.AssetHierarchyPropertyGraph.AddPartToAsset(childHierarchy.Parts, childHierarchy.Parts[childHierarchy.RootParts.Single().Id], (this as UIElementViewModel)?.AssetSideUIElement, index++);
                    }
                }
                Editor.UndoRedoService.SetName(transaction, $"{(moved ? "Move" : "Add")} {children.Count} element{(children.Count > 1 ? "s" : string.Empty)} to {GetDropLocationName()}");
            }

            // Fixup selection since adding/inserting may create new viewmodels
            Editor.FixupAndRestoreSelection(selection, children);
        }

        protected static void RemoveChildViewModel([NotNull] UIElementViewModel child)
        {
            if (child.Parent == null) throw new InvalidOperationException($"{nameof(child)}.{nameof(child.Parent)} cannot be null");
            // Remove the view model from its parent
            child.Parent.RemoveItem(child);
            child.Destroy();
        }

        private bool CanAddOrInsertChildrenPrivate([NotNull] IReadOnlyCollection<object> children, [NotNull] out string message)
        {
            message = "Empty selection";
            var parentName = GetDropLocationName();

            foreach (var child in children)
            {
                if (child is UIElementViewModel element)
                {
                    if (ReferenceEquals(element, this))
                    {
                        message = "Can't drop a UI element on itself or its children";
                        return false;
                    }
                    var currentParent = Parent;
                    while (currentParent != null)
                    {
                        if (currentParent == element)
                        {
                            message = "Can't drop a UI element on itself or its children";
                            return false;
                        }
                        currentParent = currentParent.Parent;
                    }
                    // Check base
                    if (Editor.GatherAllBasePartAssets(element, true).Contains(Asset.Id))
                    {
                        message = "Element depends on this asset";
                        return false;
                    }
                    // Accepting UIElementViewModel
                    message = $"Add to {parentName}";
                    continue;
                }
                if (child is IUIElementFactory factory)
                {
                    message = $"Create a {factory.Name} under {parentName}";
                    continue;
                }

                message = DragDropBehavior.InvalidDropAreaMessage;
                return false;
            }

            return CanAddOrInsertChildren(children, ref message);
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            return CanAddOrInsertChildrenPrivate(children, out message);
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            MoveChildren(children, Children.Count);
        }

        bool IInsertChildViewModel.CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
        {
            message = "This UI element has no parent.";

            var parent = Parent as UIElementViewModel;
            if (parent == null || !parent.CanAddOrInsertChildrenPrivate(children, out message))
                return false;

            if (children.Any(x => x == this))
            {
                message = "Cannot insert before or after one of the selected element";
                return false;
            }
            message = $"Insert {(position == InsertPosition.Before ? "before" : "after")} {GetDropLocationName()}";
            return true;
        }

        void IInsertChildViewModel.InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
        {
            var parent = Parent as UIElementViewModel;
            if (parent == null) throw new NotSupportedException($"{nameof(Parent)} can't be null");

            var index = parent.Children.IndexOf(this);
            if (position == InsertPosition.After)
                ++index;
            parent.MoveChildren(children, index);
        }
    }
}
