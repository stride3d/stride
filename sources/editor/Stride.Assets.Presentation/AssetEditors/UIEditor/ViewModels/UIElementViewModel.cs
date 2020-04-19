// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    public enum PanelCommandMode
    {
        MoveBack,
        MoveDown,
        MoveFront,
        MoveLeft,
        MoveRight,
        MoveUp,
        PinTopLeft,
        PinTop,
        PinTopRight,
        PinLeft,
        PinCenter,
        PinRight,
        PinBottomLeft,
        PinBottom,
        PinBottomRight,
        PinFront,
        PinMiddle,
        PinBack,
    }

    /// <summary>
    /// View model for <see cref="UIElement"/>.
    /// </summary>
    [DebuggerDisplay("{Name} [{ElementType}]")]
    public class UIElementViewModel : UIHierarchyItemViewModel, IEditorDesignPartViewModel<UIElementDesign, UIElement>, IEditorGamePartViewModel, IIsEditableViewModel, IAssetPropertyProviderViewModel
    {
        private readonly GameEditorChangePropagator<UIElementDesign, UIElement, UIElementViewModel> propagator;
        private readonly MemberGraphNodeBinding<string> nameNodeBinding;
        private UILibraryViewModel sourceLibrary;
        private bool isEditing;

        // Note: constructor needed by UIElementViewModelFactory
        public UIElementViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign)
            : this(editor, asset, elementDesign, null)
        { }

        protected UIElementViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign, [CanBeNull] IEnumerable<UIElementDesign> children)
            : base(editor, asset, children)
        {
            AssetSideUIElement = elementDesign.UIElement;
            ElementType = AssetSideUIElement.GetType();
            UIElementDesign = elementDesign;
            var assetNode = editor.NodeContainer.GetOrCreateNode(elementDesign.UIElement);
            nameNodeBinding = new MemberGraphNodeBinding<string>(assetNode[nameof(UIElement.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService);

            propagator = new GameEditorChangePropagator<UIElementDesign, UIElement, UIElementViewModel>(Editor, this, elementDesign.UIElement);

            PanelCommand = new AnonymousCommand<PanelCommandMode>(ServiceProvider, PanelCommandImpl);
            RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);

            UpdateSourceLibrary();
            var basePrefabNode = Editor.NodeContainer.GetNode(UIElementDesign)[nameof(UIElementDesign.Base)];
            basePrefabNode.ValueChanged += BaseElementChanged;
        }

        public UIElement AssetSideUIElement { get; }

        public Type ElementType { get; }

        public AbsoluteId Id => new AbsoluteId(Asset.Id, AssetSideUIElement.Id);

        public bool IsEditable => true;

        public bool IsEditing { get => isEditing; set => SetValue(ref isEditing, value); }

        /// <inheritdoc/>
        public override string Name { get => nameNodeBinding.Value; set => nameNodeBinding.Value = value; }

        public UILibraryViewModel SourceLibrary { get => sourceLibrary; private set => SetValue(ref sourceLibrary, value); }

        /// <summary>
        /// Gets the command to change the layout properties of this element inside its parent panel.
        /// </summary>
        /// <seealso cref="PanelViewModel.ChangeChildElementLayoutProperties"/>
        public ICommandBase PanelCommand { get; }

        public ICommandBase RenameCommand { get; }

        internal UIElementDesign UIElementDesign { get; }

        UIElementDesign IEditorDesignPartViewModel<UIElementDesign, UIElement>.PartDesign => UIElementDesign;

        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

        /// <inheritdoc/>
        public override async Task NotifyGameSidePartAdded()
        {
            // Add adorner
            await Editor.Controller.AdornerService.AddAdorner(Id.ObjectId, GetRootElementViewModel() == Editor.ActiveRoot);
            propagator.NotifyGameSidePartAdded();
            await base.NotifyGameSidePartAdded();
        }

        public bool CanDuplicate()
        {
            return Parent is PanelViewModel;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            nameNodeBinding.Dispose();
            propagator.Destroy();
            // Remove adorner
            Editor.Controller.AdornerService.RemoveAdorner(Id.ObjectId).Forget();
            var basePrefabNode = Editor.NodeContainer.GetNode(UIElementDesign)[nameof(UIElementDesign.Base)];
            basePrefabNode.ValueChanged -= BaseElementChanged;
            base.Destroy();
        }

        [NotNull]
        public UIElementViewModel Duplicate()
        {
            var parentPanel = Parent as PanelViewModel;
            if (parentPanel == null) throw new InvalidOperationException("This operation can only be executed on a child of a panel.");

            var subTreeRoot = AssetSideUIElement.Id;
            var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
            var clonedHierarchy = UIAssetPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, subTreeRoot.Yield(), flags, out _);
            AssetPartsAnalysis.GenerateNewBaseInstanceIds(clonedHierarchy);

            var addedRoot = clonedHierarchy.Parts[clonedHierarchy.RootParts.Single().Id];
            // Inside a library, rename the element to avoid having the same name
            if (Asset is UILibraryViewModel && !string.IsNullOrEmpty(addedRoot.UIElement.Name))
                addedRoot.UIElement.Name = NamingHelper.ComputeNewName(addedRoot.UIElement.Name, Parent.Children, x => x.Name);
            Asset.AssetHierarchyPropertyGraph.AddPartToAsset(clonedHierarchy.Parts, addedRoot, parentPanel.AssetSideUIElement, parentPanel.Children.IndexOf(this) + 1);
            var cloneId = addedRoot.UIElement.Id;

            // The view model should already exist at that point
            var partId = new AbsoluteId(Asset.Id, cloneId);
            var viewModel = (UIElementViewModel)Editor.FindPartViewModel(partId);
            if (viewModel == null) throw new InvalidOperationException($"{nameof(viewModel)} can't be null.");
            return viewModel;
        }

        /// <inheritdoc />
        public override GraphNodePath GetNodePath()
        {
            var node = new GraphNodePath(Editor.NodeContainer.GetNode(Asset.Asset));
            node.PushMember(nameof(UIAsset.Hierarchy));
            node.PushTarget();
            node.PushMember(nameof(UIAsset.Hierarchy.Parts));
            node.PushTarget();
            node.PushIndex(new NodeIndex(Id.ObjectId));
            node.PushMember(nameof(UIElementDesign.UIElement));
            node.PushTarget();
            return node;
        }

        /// <summary>
        /// Gets the root element view model of this element.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public UIElementViewModel GetRootElementViewModel()
        {
            UIElementViewModel root = null;
            var source = this;
            while (source != null)
            {
                root = source;
                source = source.Parent as UIElementViewModel;
            }
            return root;
        }

        /// <summary>
        /// Disables the adorners corresponding to this element.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="Game.UIEditorGameAdornerService.DisableAdorner"/>
        internal Task DisableAdorner()
        {
            return Editor.Controller.AdornerService.DisableAdorner(Id.ObjectId);
        }

        /// <summary>
        /// Enables the adorners corresponding to this element.
        /// </summary>
        /// <seealso cref="Game.UIEditorGameAdornerService.EnableAdorner"/>
        internal async Task EnableAdorner()
        {
            // This method has to be called from the main thread: this await will fail if called from the controller (microthread)
            Dispatcher.EnsureAccess();
            // Make sure the game side is ready
            await propagator.Initialized;
            await Editor.Controller.AdornerService.EnableAdorner(Id.ObjectId);
        }

        /// <summary>
        /// Selects the adorners corresponding to this element.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="Game.UIEditorGameAdornerService.SelectElement"/>
        internal async Task SelectAdorner()
        {
            // This method has to be called from the main thread: this await will fail if called from the controller (microthread)
            Dispatcher.EnsureAccess();
            // Make sure the game side is ready
            await propagator.Initialized;
            await Editor.Controller.AdornerService.SelectElement(Id.ObjectId);
        }

        /// <inheritdoc />
        // ReSharper disable once RedundantAssignment
        protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, ref string message)
        {
            // By default a UIElement cannot have children.
            message = $"{GetDropLocationName()} does not support adding or inserting children";
            return false;
        }

        /// <inheritdoc/>
        protected override string GetDropLocationName()
        {
            return UIEditorBaseViewModel.GetDisplayName(AssetSideUIElement);
        }

        protected T GetDependencyPropertyValue<T>(UIElement element, PropertyKey<T> property)
        {
            var dependencyPropertiesNode = Editor.NodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)];
            return (T)dependencyPropertiesNode.Retrieve(new NodeIndex(property));
        }

        protected bool RemoveDependencyProperty(UIElement element, PropertyKey property)
        {
            var dependencyPropertiesNode = Editor.NodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)].Target;
            var propertyIndex = new NodeIndex(property);
            if (!dependencyPropertiesNode.Indices.Contains(propertyIndex))
                return false;

            var value = dependencyPropertiesNode.Retrieve(propertyIndex);
            dependencyPropertiesNode.Remove(value, propertyIndex);
            return true;
        }

        protected void SetDependencyPropertyValue<T>(UIElement element, PropertyKey<T> property, T value)
        {
            var dependencyPropertiesNode = Editor.NodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)].Target;
            var propertyIndex = new NodeIndex(property);
            if (!dependencyPropertiesNode.Indices.Contains(propertyIndex))
            {
                // Note: update would probably work, but we want to remove the property when Undo
                dependencyPropertiesNode.Add(value, propertyIndex);
            }
            else
            {
                dependencyPropertiesNode.Update(value, propertyIndex);
            }
        }

        private void UpdateSourceLibrary()
        {
            SourceLibrary = UIElementDesign.Base != null ? Editor.Session.GetAssetById(UIElementDesign.Base.BasePartAsset.Id) as UILibraryViewModel : null;
        }

        private void BaseElementChanged(object sender, MemberNodeChangeEventArgs e)
        {
            UpdateSourceLibrary();
        }

        private void PanelCommandImpl(PanelCommandMode mode)
        {
            (Parent as PanelViewModel)?.ChangeChildElementLayoutProperties(AssetSideUIElement, mode);
        }

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            return Editor.NodeContainer.GetNode(AssetSideUIElement);
        }

        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return GetNodePath();
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member)
        {
            if (typeof(PropertyContainerClass).IsAssignableFrom(member.Type))
            {
                // Do not show property container and attached properties in the property grid.
                // Note: when relevant those properties will be available through virtual nodes.
                return false;
            }
            var assetPropertyProvider = (IPropertyProviderViewModel)Asset;
            return assetPropertyProvider.ShouldConstructMember(member);
        }

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);
    }
}
