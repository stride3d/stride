// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Quantum;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Quantum;
using Xenko.Assets.Presentation.Extensions;
using Xenko.Assets.Presentation.Quantum;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.UI;
using Xenko.UI;
using Xenko.UI.Panels;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    public sealed class PanelViewModel : UIElementViewModel
    {
        private readonly IObjectNode childrenNode;

        // Note: constructor needed by UIElementViewModelFactory
        public PanelViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign)
            : base(editor, asset, elementDesign, GetOrCreateChildPartDesigns((UIAssetBase)asset.Asset, elementDesign))
        {
            childrenNode = editor.NodeContainer.GetOrCreateNode(AssetSidePanel)[nameof(Panel.Children)].Target;
            childrenNode.ItemChanged += ChildrenContentChanged;

            ChangeLayoutTypeCommand = new AnonymousCommand<IUIElementFactory>(ServiceProvider, ChangeLayoutType);
            UngroupCommand = new AnonymousCommand(ServiceProvider, Ungroup);
        }

        /// <summary>
        /// Gets the command to change the current layout to another type of layout.
        /// </summary>
        public ICommandBase ChangeLayoutTypeCommand { get; }

        /// <summary>
        /// Gets the command to move all children to the parent panel and remove this panel.
        /// </summary>
        public ICommandBase UngroupCommand { get; }

        private Panel AssetSidePanel => (Panel)AssetSideUIElement;

        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(PanelViewModel));
            childrenNode.ItemChanged -= ChildrenContentChanged;
            base.Destroy();
        }

        public void ChangeChildElementLayoutProperties([NotNull] UIElement child, PanelCommandMode mode)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));

            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                var canvas = AssetSidePanel as Canvas;
                if (canvas != null)
                {
                    var pinOrigin = GetDependencyPropertyValue(child, Canvas.PinOriginPropertyKey);
                    switch (mode)
                    {
                        case PanelCommandMode.PinTopLeft:
                            pinOrigin.X = 0.0f;
                            pinOrigin.Y = 0.0f;
                            break;

                        case PanelCommandMode.PinTop:
                            pinOrigin.X = 0.5f;
                            pinOrigin.Y = 0.0f;
                            break;

                        case PanelCommandMode.PinTopRight:
                            pinOrigin.X = 1.0f;
                            pinOrigin.Y = 0.0f;
                            break;

                        case PanelCommandMode.PinLeft:
                            pinOrigin.X = 0.0f;
                            pinOrigin.Y = 0.5f;
                            break;

                        case PanelCommandMode.PinCenter:
                            pinOrigin.X = 0.5f;
                            pinOrigin.Y = 0.5f;
                            break;

                        case PanelCommandMode.PinRight:
                            pinOrigin.X = 1.0f;
                            pinOrigin.Y = 0.5f;
                            break;

                        case PanelCommandMode.PinBottomLeft:
                            pinOrigin.X = 0.0f;
                            pinOrigin.Y = 1.0f;
                            break;

                        case PanelCommandMode.PinBottom:
                            pinOrigin.X = 0.5f;
                            pinOrigin.Y = 1.0f;
                            break;

                        case PanelCommandMode.PinBottomRight:
                            pinOrigin.X = 1.0f;
                            pinOrigin.Y = 1.0f;
                            break;

                        case PanelCommandMode.PinFront:
                            pinOrigin.Z = 1.0f;
                            break;

                        case PanelCommandMode.PinMiddle:
                            pinOrigin.Z = 0.5f;
                            break;

                        case PanelCommandMode.PinBack:
                            pinOrigin.Z = 0.0f;
                            break;

                        default:
                            throw new ArgumentException($"{mode} is not a supported mode.", nameof(mode));
                    }
                    SetDependencyPropertyValue(child, Canvas.PinOriginPropertyKey, pinOrigin);
                    Editor.UndoRedoService.SetName(transaction, $"Change Pin Origin of {UIEditorBaseViewModel.GetDisplayName(child)}");
                    return;
                }

                var grid = AssetSidePanel as GridBase;
                if (grid != null)
                {
                    PropertyKey<int> propertyKey;
                    int offset;
                    switch (mode)
                    {
                        case PanelCommandMode.MoveUp:
                            propertyKey = GridBase.RowPropertyKey;
                            offset = -1;
                            break;

                        case PanelCommandMode.MoveDown:
                            propertyKey = GridBase.RowPropertyKey;
                            offset = 1;
                            break;

                        case PanelCommandMode.MoveLeft:
                            propertyKey = GridBase.ColumnPropertyKey;
                            offset = -1;
                            break;

                        case PanelCommandMode.MoveRight:
                            propertyKey = GridBase.ColumnPropertyKey;
                            offset = 1;
                            break;

                        case PanelCommandMode.MoveBack:
                            propertyKey = GridBase.LayerPropertyKey;
                            offset = -1;
                            break;

                        case PanelCommandMode.MoveFront:
                            propertyKey = GridBase.LayerPropertyKey;
                            offset = 1;
                            break;

                        default:
                            throw new ArgumentException($"{mode} is not a supported mode.", nameof(mode));
                    }

                    var currentValue = GetDependencyPropertyValue(child, propertyKey);
                    var newValue = Math.Max(0, currentValue + offset);
                    SetDependencyPropertyValue(child, propertyKey, newValue);
                    Editor.UndoRedoService.SetName(transaction, $"Move {UIEditorBaseViewModel.GetDisplayName(child)}");
                    return;
                }

                var stackPanel = AssetSidePanel as StackPanel;
                if (stackPanel != null)
                {
                    var collection = AssetSidePanel.Children;
                    var index = collection.IndexOf(child);
                    if (index == -1)
                        throw new InvalidOperationException("The given element is not a child of this panel.");

                    int newIndex;
                    switch (mode)
                    {
                        case PanelCommandMode.MoveDown:
                            newIndex = index + 1;
                            if (newIndex >= collection.Count)
                                return;
                            break;

                        case PanelCommandMode.MoveUp:
                            newIndex = index - 1;
                            if (newIndex < 0)
                                return;
                            break;

                        default:
                            throw new ArgumentException($"{mode} is not a supported mode.", nameof(mode));
                    }

                    // FIXME: review if this is fine doing it that way or if we need to do it the same way as when moving elements around
                    childrenNode.Remove(child, new Index(index));
                    childrenNode.Add(child, new Index(newIndex));
                    Editor.UndoRedoService.SetName(transaction, $"Move {UIEditorBaseViewModel.GetDisplayName(child)}");
                }
            }
        }

        /// <inheritdoc/>
        protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, ref string message)
        {
            return true;
        }

        private static IEnumerable<UIElementDesign> GetOrCreateChildPartDesigns([NotNull] UIAssetBase asset, [NotNull] UIElementDesign elementDesign)
        {
            var assetPanel = (Panel)elementDesign.UIElement;

            foreach (var child in assetPanel.Children)
            {
                if (!asset.Hierarchy.Parts.TryGetValue(child.Id, out UIElementDesign childDesign))
                {
                    childDesign = new UIElementDesign(child);
                }
                if (child != childDesign.UIElement) throw new InvalidOperationException();
                yield return childDesign;
            }
        }

        private void ChangeLayoutType([NotNull] IUIElementFactory factory)
        {
            var targetType = (factory as UIElementFromSystemLibrary)?.Type;
            if (targetType == null)
                throw new NotSupportedException("Changing the panel from a user library type is currently not supported.");
            if (!typeof(Panel).IsAssignableFrom(targetType))
                throw new ArgumentException(@"The target type is not a panel", nameof(targetType));

            // If the target panel type is the same as the current panel type, do nothing
            if (targetType == ElementType)
                return;

            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                Panel targetPanel = null;
                // Try to maintain the layout order depending on the combinaison of currentType/targetType.
                //
                // Notes:
                // - from any panel to a Canvas (tricky case)
                //    - for now don't do anything smart
                //        + later we could try to calculate Canvas absolute or relative position so that elements appear at the same position
                var stackPanel = AssetSidePanel as StackPanel;
                if (stackPanel != null)
                {
                    // from a StackPanel to a Grid or UniformGrid:
                    //   - if the StackPanel's orientation was horizontal, put each element in a different column
                    //   - if the StackPanel's orientation was in-depth, put each element in a different layer
                    //   - if the StackPanel's orientation was vertical, put each element in a different row
                    //   - use StripType.Auto for the Strip definitions of the Grid
                    if (typeof(GridBase).IsAssignableFrom(targetType))
                    {
                        var colums = 1;
                        var rows = 1;
                        var layers = 1;
                        var childrenCount = stackPanel.Children.Count;
                        if (childrenCount > 0)
                        {
                            switch (stackPanel.Orientation)
                            {
                                case Orientation.Horizontal:
                                    colums = childrenCount;
                                    break;

                                case Orientation.Vertical:
                                    rows = childrenCount;
                                    break;

                                case Orientation.InDepth:
                                    layers = childrenCount;
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        if (typeof(Grid).IsAssignableFrom(targetType))
                        {
                            targetPanel = CreateGrid(colums, rows, layers, StripType.Auto);
                        }
                        else if (typeof(UniformGrid).IsAssignableFrom(targetType))
                        {
                            targetPanel = CreateUniformGrid(colums, rows, layers);
                        }

                        if (targetPanel != null)
                        {
                            CopySwapExchange(this, new UIElementDesign(targetPanel));
                            // Set dependency properties
                            PropertyKey<int> propertyKey;
                            switch (stackPanel.Orientation)
                            {
                                case Orientation.Horizontal:
                                    propertyKey = GridBase.ColumnPropertyKey;
                                    break;

                                case Orientation.Vertical:
                                    propertyKey = GridBase.RowPropertyKey;
                                    break;

                                case Orientation.InDepth:
                                    propertyKey = GridBase.LayerPropertyKey;
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            for (var i = 0; i < childrenCount; i++)
                            {
                                SetDependencyPropertyValue(targetPanel.Children[i], propertyKey, i);
                            }
                        }
                    }
                    else if (typeof(Canvas).IsAssignableFrom(targetType))
                    {
                        // fallback to default case (for now)
                    }
                }

                var gridBase = AssetSidePanel as GridBase;
                if (gridBase != null)
                {
                    var grid = gridBase as Grid;
                    var uniformGrid = gridBase as UniformGrid;
                    if (typeof(StackPanel).IsAssignableFrom(targetType))
                    {
                        // from a GridBase to a StackPanel
                        //   - determine the StackPanel's orientation from the GridBase largest dimension (Colums, Rows, Layers)
                        //   - order elements by Rows, Colums and Layers
                        if (grid != null)
                        {
                            targetPanel = new StackPanel
                            {
                                Orientation = GetOrientation(grid.ColumnDefinitions.Count, grid.RowDefinitions.Count, grid.LayerDefinitions.Count),
                            };
                        }
                        else if (uniformGrid != null)
                        {
                            targetPanel = new StackPanel
                            {
                                Orientation = GetOrientation(uniformGrid.Columns, uniformGrid.Rows, uniformGrid.Layers),
                            };
                        }
                        else
                        {
                            // Unknown GridBase implementation
                            targetPanel = new StackPanel();
                        }
                        // Order children in Western reading order: left to right, top to bottom, front to back)
                        CopySwapExchange(this, new UIElementDesign(targetPanel), x => x.OrderBy(e => e.AssetSideUIElement.DependencyProperties.Get(GridBase.RowPropertyKey)).ThenBy(e => e.AssetSideUIElement.DependencyProperties.Get(GridBase.ColumnPropertyKey)).ThenBy(e => e.AssetSideUIElement.DependencyProperties.Get(GridBase.LayerPropertyKey)));
                        // Remove the GridBase related dependency properties
                        foreach (var child in targetPanel.Children)
                        {
                            RemoveDependencyProperty(child, GridBase.ColumnPropertyKey);
                            RemoveDependencyProperty(child, GridBase.ColumnSpanPropertyKey);
                            RemoveDependencyProperty(child, GridBase.RowPropertyKey);
                            RemoveDependencyProperty(child, GridBase.RowSpanPropertyKey);
                            RemoveDependencyProperty(child, GridBase.LayerPropertyKey);
                            RemoveDependencyProperty(child, GridBase.LayerSpanPropertyKey);
                        }
                    }
                    else if (typeof(Grid).IsAssignableFrom(targetType) && uniformGrid != null)
                    {
                        // from a Grid to a UniformGrid
                        //   - keep the same column/layer/row dependency properties
                        //   - create ColumDefinitions, RowDefinitions, LayerDefinitions from Colums, Rows, Layers resp.
                        //   - use StripType.Star
                        targetPanel = CreateGrid(uniformGrid.Columns, uniformGrid.Rows, uniformGrid.Layers, StripType.Star);
                        CopySwapExchange(this, new UIElementDesign(targetPanel));
                    }
                    else if (typeof(UniformGrid).IsAssignableFrom(targetType) && grid != null)
                    {
                        // from a UniformGrid to a Grid
                        //   - keep the same column/layer/row dependency properties
                        //   - set Colums, Rows, Layers by counting ColumDefinitions, RowDefinitions, LayerDefinitions resp.
                        targetPanel = CreateUniformGrid(grid.ColumnDefinitions.Count, grid.RowDefinitions.Count, grid.LayerDefinitions.Count);
                        CopySwapExchange(this, new UIElementDesign(targetPanel));
                    }
                    else if (typeof(Canvas).IsAssignableFrom(targetType))
                    {
                        // from a Canvas to any other panel (tricky case)
                        //   - just add the children with default behavior
                        //     + finding the arrangement based on the relative position of children is a difficult problem (we will need some heuristic)
                        //     + plus the user might want something else anyway, so let's not make its work more complicated

                        // Remove the GridBase related dependency properties
                        foreach (var child in gridBase.Children)
                        {
                            RemoveDependencyProperty(child, GridBase.ColumnPropertyKey);
                            RemoveDependencyProperty(child, GridBase.ColumnSpanPropertyKey);
                            RemoveDependencyProperty(child, GridBase.RowPropertyKey);
                            RemoveDependencyProperty(child, GridBase.RowSpanPropertyKey);
                            RemoveDependencyProperty(child, GridBase.LayerPropertyKey);
                            RemoveDependencyProperty(child, GridBase.LayerSpanPropertyKey);
                        }
                        // fallback to default case (for now)
                    }
                }

                var canvas = AssetSidePanel as Canvas;
                if (canvas != null)
                {
                    // Remove the Canvas related dependency properties
                    foreach (var child in canvas.Children)
                    {
                        RemoveDependencyProperty(child, Canvas.AbsolutePositionPropertyKey);
                        RemoveDependencyProperty(child, Canvas.PinOriginPropertyKey);
                        RemoveDependencyProperty(child, Canvas.RelativePositionPropertyKey);
                        RemoveDependencyProperty(child, Canvas.RelativeSizePropertyKey);
                    }
                    // fallback to default case (for now)
                }

                if (targetPanel == null)
                {
                    // default
                    targetPanel = (Panel)Activator.CreateInstance(targetType);
                    CopySwapExchange(this, new UIElementDesign(targetPanel));
                }

                Editor.UndoRedoService.SetName(transaction, $"Change layout type to {targetType.Name}");
            }
        }

        private void Ungroup()
        {
            // Move all children of this panel into the parent panel, if available:
            //   - if parent is a root, this action should be ignored
            //   - if parent is ContentControl and this panel has more than one child, this action should be ignored
            if (Parent is UIRootViewModel || (Parent is ContentControlViewModel && Children.Count > 1))
                return;

            var parentElement = (Parent as UIElementViewModel)?.AssetSideUIElement;
            if (parentElement == null)
                return;

            // FIXME: should be similar to "move all children" then "delete previous panel"
            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                var children = Children.ToList();
                var hierarchy = UIAssetPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, children.Select(c => c.Id.ObjectId), SubHierarchyCloneFlags.None, out _);
                // Remove all children from this panel.
                foreach (var child in children)
                {
                    Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(child.UIElementDesign);
                }
                // Remove the current panel.
                Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(UIElementDesign);
                // Add the children into the parent panel.
                foreach (var child in children)
                {
                    // TODO: might need smart rules for dependency properties (maybe use the same rules than when changing the layout type)
                    Asset.InsertUIElement(hierarchy.Parts, hierarchy.Parts[child.Id.ObjectId], parentElement);
                }
                Editor.UndoRedoService.SetName(transaction, $"Ungroup {children.Count} elements");
            }
        }

        private async void ChildrenContentChanged(object sender, ItemChangeEventArgs e)
        {
            UIElement childElement;
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionUpdate:
                    return;

                case ContentChangeType.CollectionAdd:
                    childElement = (UIElement)e.NewValue;
                    Editor.Logger.Debug($"Add {childElement.Name ?? childElement.GetType().Name} ({childElement.Id}) to the {nameof(Children)} collection");
                    break;

                case ContentChangeType.CollectionRemove:
                    childElement = (UIElement)e.OldValue;
                    Editor.Logger.Debug($"Remove {childElement.Name ?? childElement.GetType().Name} ({childElement.Id}) from the {nameof(Children)} collection");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update view model
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                var elementDesign = UIAsset.Hierarchy.Parts[childElement.Id];
                var viewModel = (UIElementViewModel)Editor.CreatePartViewModel(Asset, elementDesign);
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
                var partId = new AbsoluteId(Asset.Id, childElement.Id);
                var element = (UIElementViewModel)Editor.FindPartViewModel(partId);
                if (element == null) throw new InvalidOperationException($"{nameof(element)} cannot be null");
                RemoveChildViewModel(element);
                Editor.Controller.RemovePart(this, element.AssetSideUIElement).Forget();
            }
        }

        /// <summary>
        /// Successively copy the properties from the <paramref name="sourcePanel"/> to the <paramref name="targetPanel"/>, then swap the panels in the parent
        /// and finally move the children from the <paramref name="sourcePanel"/> to the <paramref name="targetPanel"/>.
        /// </summary>
        private void CopySwapExchange([NotNull] PanelViewModel sourcePanel, [NotNull] UIElementDesign targetPanel,
            [CanBeNull] Func<IEnumerable<UIElementViewModel>, IEnumerable<UIElementViewModel>> childSorter = null)
        {
            var targetPanelElement = targetPanel.UIElement as Panel;
            if (targetPanelElement == null)
                throw new ArgumentException(@"The target element must be a Panel", nameof(targetPanel));

            // Clone common properties
            CopyCommonProperties(Editor.NodeContainer, sourcePanel.AssetSidePanel, targetPanelElement);

            // Initialize the new hierarchy of elements that starts from the target and contains all the children
            IEnumerable <UIElementViewModel> children = sourcePanel.Children.ToList();
            var hierarchy = UIAssetPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, children.Select(c => c.Id.ObjectId), SubHierarchyCloneFlags.None, out _);
            hierarchy.RootParts.Add(targetPanel.UIElement);
            hierarchy.Parts.Add(targetPanel);
            // Remove all children from the source panel.
            foreach (var child in children)
            {
                Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(child.UIElementDesign);
            }

            // Swap panels in the parent
            var elementParent = Parent as UIElementViewModel;
            var rootParent = Parent as UIRootViewModel;
            if (rootParent != null)
            {
                // special case of RootElement
                rootParent.ReplaceRootElement(sourcePanel, hierarchy, targetPanelElement.Id);
            }
            else if (elementParent != null)
            {
                // Remove current panel from Parent
                var index = elementParent.Children.IndexOf(sourcePanel);
                Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(sourcePanel.UIElementDesign);
                Asset.AssetHierarchyPropertyGraph.AddPartToAsset(hierarchy.Parts, targetPanel, elementParent.AssetSideUIElement, index);
            }
            else
            {
                throw new InvalidOperationException();
            }

            // Sort the children list before re-inserting them in the target panel.
            children = childSorter?.Invoke(children) ?? children;

            // Then populate the target panel
            foreach (var child in children)
            {
                Asset.InsertUIElement(hierarchy.Parts, hierarchy.Parts[child.Id.ObjectId], targetPanelElement);
            }
        }

        [NotNull]
        private static Grid CreateGrid(int colums, int rows, int layers, StripType stripType)
        {
            var generator = (Func<StripDefinition>)(() => new StripDefinition { Type = stripType });
            var grid = new Grid();
            grid.ColumnDefinitions.AddRange(generator.Repeat(colums));
            grid.RowDefinitions.AddRange(generator.Repeat(rows));
            grid.LayerDefinitions.AddRange(generator.Repeat(layers));
            return grid;
        }

        [NotNull]
        private static UniformGrid CreateUniformGrid(int colums, int rows, int layers)
        {
            return new UniformGrid
            {
                Columns = Math.Max(1, colums),
                Rows = Math.Max(1, rows),
                Layers = Math.Max(1, layers),
            };
        }

        /// <summary>
        /// Gets a <see cref="Orientation"/> depending of the numbers of <paramref name="colums"/>, <paramref name="rows"/> and <paramref name="layers"/>.
        /// </summary>
        /// <remarks><see cref="Orientation.Vertical"/> is favored over <see cref="Orientation.Horizontal"/> which in turn is favored over <see cref="Orientation.InDepth"/>.</remarks>
        /// <param name="colums">The number of colums.</param>
        /// <param name="rows">The number of rows.</param>
        /// <param name="layers">The number of layers.</param>
        /// <returns></returns>
        private static Orientation GetOrientation(int colums, int rows, int layers)
        {
            if (colums > rows)
            {
                return layers > colums ? Orientation.InDepth : Orientation.Horizontal;
            }
            return layers > rows ? Orientation.InDepth : Orientation.Vertical;
        }

        /// <summary>
        /// Copies the common properties between the <paramref name="sourcePanel"/> and the <paramref name="targetPanel"/>.
        /// </summary>
        /// <remarks>The children are not copied.</remarks>
        /// <param name="nodeContainer">The node container.</param>
        /// <param name="sourcePanel">The source panel.</param>
        /// <param name="targetPanel">The target panel.</param>
        private static void CopyCommonProperties([NotNull] INodeContainer nodeContainer, Panel sourcePanel, Panel targetPanel)
        {
            var sourceNode = nodeContainer.GetOrCreateNode(sourcePanel);
            var targetNode = nodeContainer.GetOrCreateNode(targetPanel);

            foreach (var targetChild in targetNode.Members.Where(x => x.Name != nameof(Panel.Children) && x.Name != nameof(UIElement.Id)))
            {
                var name = targetChild.Name;
                var sourceChild = sourceNode.TryGetChild(name);
                if (sourceChild != null)
                {
                    targetChild.Update(AssetCloner.Clone(sourceChild.Retrieve()));
                }
            }
        }
    }
}
