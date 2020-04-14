// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Services;
using Stride.Assets.Presentation.ViewModel;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Panels;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Game
{
    internal sealed partial class UIEditorGameAdornerService : EditorGameServiceBase, IEditorGameViewModelService
    {
        /// <summary>
        /// Represents a layer of adorners associated to one UIElement.
        /// </summary>
        /// <remarks>
        /// Some adorners (e.g. margin) need to be outside of the canvas representing the render area of the associated UIElement,
        /// but this is not currently possible because of canvas limitation (see XK-3353). Therefore we need to maintain two canvas:
        /// * A canvas that is sized to the parent of the associated UIElement (if it has one).
        /// * A canvas that is sized to the associated UIElement.
        /// 
        /// When adding a adorner, the second parameter specifiy in which canvas goes the adorner visual.
        /// </remarks>
        private class AdornerLayer
        {
            private readonly List<IAdornerBase<UIElement>> adorners = new List<IAdornerBase<UIElement>>();
            private readonly UIElement gameSideElement;
            private readonly Canvas canvas;
            private readonly Canvas parentCanvas;

            private Thickness prevMargin;
            private Guid? prevParentId;
            private Vector3 prevRenderSize;
            private Matrix prevWorldMatrix;

            private HighlightAdorner highlightAdorner;
#if DEBUG
            private bool isVisible;
#endif

            public AdornerLayer(UIElement gameSideElement)
            {
                if (gameSideElement == null) throw new ArgumentNullException(nameof(gameSideElement));

                this.gameSideElement = gameSideElement;
                canvas = new Canvas();
                parentCanvas = new Canvas
                {
                    Children = { canvas },
                    Name = $"Adorner layer of {gameSideElement.Name ?? gameSideElement.Id.ToString()}",
                };
            }

            public bool IsEnabled { get; private set; }

            public bool IsHighlighted => highlightAdorner?.IsHighlighted ?? false;

            public UIElement Visual => parentCanvas;

            public void Add(IAdornerBase<UIElement> adorner, bool addToParentLayer = false)
            {
                if (addToParentLayer)
                {
                    parentCanvas.Children.Add(adorner.Visual);
                }
                else
                {
                    canvas.Children.Add(adorner.Visual);
                }
                adorners.Add(adorner);
            }

            public bool CheckValidity()
            {
                // compare some properties involved in layout with their previous "seen" values
                if (prevMargin == gameSideElement.Margin &&
                    prevParentId == gameSideElement.VisualParent?.Id &&
                    prevRenderSize == gameSideElement.RenderSize &&
                    prevWorldMatrix == gameSideElement.WorldMatrix)
                {
                    // still valid
                    return true;
                }
                // remember current values
                prevMargin = gameSideElement.Margin;
                prevParentId = gameSideElement.VisualParent?.Id;
                prevRenderSize = gameSideElement.RenderSize;
                prevWorldMatrix = gameSideElement.WorldMatrix;
                return false;
            }

            public void Disable()
            {
                IsEnabled = false;
                foreach (var adorner in adorners)
                {
                    adorner.Disable();
                }
                highlightAdorner?.Disable();
            }

            public void Enable()
            {
                IsEnabled = true;
                foreach (var adorner in adorners)
                {
                    adorner.Enable();
                }
                highlightAdorner?.Enable();
            }

            public void Hide()
            {
#if DEBUG
                isVisible = false;
#endif
                foreach (var adorner in adorners)
                {
                    adorner.Hide();
                }
                highlightAdorner?.Show();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Highlight()
            {
                highlightAdorner?.Highlight();
            }

            public void Invalidate()
            {
                prevMargin = Thickness.UniformCuboid(float.NaN);
                prevParentId = null;
                prevRenderSize = new Vector3(float.NaN);
                prevWorldMatrix = new Matrix(float.NaN);
            }

            public void SetHighlightAdorner(HighlightAdorner value)
            {
                if (highlightAdorner != null)
                {
                    canvas.Children.Remove(highlightAdorner.Visual);
                }
                highlightAdorner = value;
                if (highlightAdorner != null)
                {
                    canvas.Children.Add(highlightAdorner.Visual);
                }
            }

            public void Show()
            {
#if DEBUG
                isVisible = true;
#endif
                foreach (var adorner in adorners)
                {
                    adorner.Show();
                }
                highlightAdorner?.Hide();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Unlit()
            {
                highlightAdorner?.Unlit();
            }

            public void Update(ref Vector3 availableSize)
            {
                Vector3 parentPosition;
                Vector3 parentSize;
                Matrix parentMatrixInv;
                var parent = gameSideElement.VisualParent;
                if (parent != null)
                {
                    // parentCanvas is sized to the parent of the associated UIElement
                    var parentMatrix = parent.WorldMatrix;
                    parentPosition = parentMatrix.TranslationVector + availableSize * 0.5f;
                    parentSize = parent.RenderSize;
                    parentMatrixInv = Matrix.Invert(parent.WorldMatrix);
                }
                else
                {
                    // or to the total available size if it doesn't have a parent.
                    parentPosition = availableSize * 0.5f;
                    parentSize = availableSize;
                    parentMatrixInv = Matrix.Identity;
                }
                parentCanvas.Size = parentSize;
                parentCanvas.SetCanvasAbsolutePosition(parentPosition);
                parentCanvas.SetCanvasPinOrigin(0.5f * Vector3.One); // centered on origin

                var diffMatrix = Matrix.Multiply(parentMatrixInv, gameSideElement.WorldMatrix);
                var position = diffMatrix.TranslationVector + parentSize * 0.5f;
                // canvas is sized to the associated UIElement
                canvas.Size = gameSideElement.RenderSize;
                // canvas is z-offset by depth bias (+1 to differentiate with the adorner root canvas)
                canvas.Margin = new Thickness(0, 0, 0, 0, 0, -1*(gameSideElement.DepthBias + 1)); // because we are inside a canvas, only Left, Top and Front margins can be used.

                canvas.SetCanvasAbsolutePosition(position);
                canvas.SetCanvasPinOrigin(0.5f * Vector3.One); // centered on origin

                adorners.ForEach(a => a.Update(position));
                highlightAdorner?.Update(position);
            }
        }

#if DEBUG
        /// <summary>
        /// The key to the AssociatedElement dependency property.
        /// </summary>
        public static readonly PropertyKey<UIElement> AssociatedElementPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(AssociatedElementPropertyKey), typeof(UIEditorGameAdornerService), (UIElement)null);
#endif

        /// <summary>
        /// The key to the AssociatedAdorner dependency property.
        /// </summary>
        public static readonly PropertyKey<IAdornerBase<UIElement>> AssociatedAdornerPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(AssociatedAdornerPropertyKey), typeof(UIEditorGameAdornerService), (IAdornerBase<UIElement>)null);

        /// <summary>
        /// The key to the AssociatedElementId dependency property.
        /// </summary>
        public static readonly PropertyKey<Guid> AssociatedElementIdPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(AssociatedElementIdPropertyKey), typeof(UIEditorGameAdornerService), Guid.Empty);

        /// <remarks>
        /// <list type="bullet">
        /// <item>Key: game-side element Id</item>
        /// <item>Value: adorner layer</item>
        /// </list>
        /// </remarks>
        private readonly Dictionary<Guid, AdornerLayer> adornerLayers = new Dictionary<Guid, AdornerLayer>();

        public UIEditorGameAdornerService(UIEditorController controller)
        {
            Controller = controller;
        }

        internal EditorServiceGame Game { get; private set; }

        internal UIEditorController Controller { get; }

        /// <inheritdoc/>
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(UIEditorGameAdornerService));

            adornerLayers.Clear();
            GetAdornerCanvas()?.Children.Clear();

            return base.DisposeAsync();
        }

        [NotNull]
        public Task AddAdorner(Guid elementId, bool enable = false)
        {
            return Controller.InvokeAsync(() =>
            {
                DoAddAdorner(elementId);
                if (enable)
                {
                    DoEnableAdorner(elementId);
                }
            });
        }

        [NotNull]
        public Task ClearSelection()
        {
            return Controller.InvokeAsync(DoHideAllAdorners);
        }

        [NotNull]
        public Task DisableAdorner(Guid elementId)
        {
            return Controller.InvokeAsync(() =>
            {
                DoDisableAdorner(elementId);
            });
        }

        [NotNull]
        public Task EnableAdorner(Guid elementId)
        {
            return Controller.InvokeAsync(() =>
            {
                DoEnableAdorner(elementId);
            });
        }

        [NotNull]
        public Task HighlightAdorner(Guid elementId)
        {
            return Controller.InvokeAsync(() =>
            {
                DoHighlightAdorner(elementId);
            });
        }

        [NotNull]
        public Task Refresh()
        {
            return Controller.InvokeAsync(() =>
            {
                // Invalidate all adorners
                adornerLayers.Values.ForEach(l => l.Invalidate());
            });
        }

        [NotNull]
        public Task RemoveAdorner(Guid elementId)
        {
            return Controller.InvokeAsync(() =>
            {
                DoRemoveAdorner(elementId);
            });
        }

        [NotNull]
        public Task SelectElement(Guid elementId)
        {
            return Controller.InvokeAsync(() =>
            {
                // Hide all adorners
                DoHideAllAdorners();
                // Show and enable the adorners corresponding to the selected element
                DoSelectElement(elementId);
            });
        }

        [NotNull]
        public Task SelectElements(IEnumerable<Guid> elementIds)
        {
            return Controller.InvokeAsync(() =>
            {
                // Hide all adorners
                DoHideAllAdorners();
                // Show and enable the adorners corresponding to the selection
                foreach (var elementId in elementIds)
                {
                    DoSelectElement(elementId);
                }
            });
        }

        [NotNull]
        public Task UnlitAllAdorners()
        {
            return Controller.InvokeAsync(DoUnlitAllAdorners);
        }

        /// <inheritdoc/>
        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            Game = editorGame;
            Game.Script.AddTask(Update);
            return Task.FromResult(true);
        }

        private AdornerLayer CreateLayer(UIElement gameSideElement)
        {
            var canvas = GetAdornerCanvas();
            if (canvas == null)
                return null;

            var font = Controller.DefaultFont;
            var layer = new AdornerLayer(gameSideElement);

            // Create margins adorners
            layer.Add(new MarginAdorner(this, gameSideElement, MarginEdge.Left, font), true);
            layer.Add(new MarginAdorner(this, gameSideElement, MarginEdge.Right, font), true);
            layer.Add(new MarginAdorner(this, gameSideElement, MarginEdge.Top, font), true);
            layer.Add(new MarginAdorner(this, gameSideElement, MarginEdge.Bottom, font), true);

            // Create highlight adorner
            layer.SetHighlightAdorner(new HighlightAdorner(this, gameSideElement));

            // Create move adorner
            layer.Add(new MoveAdorner(this, gameSideElement));

            // Create sizing adorners
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.TopLeft));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.Top));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.TopRight));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.Right));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.BottomRight));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.Bottom));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.BottomLeft));
            layer.Add(new SizingAdorner(this, gameSideElement, ResizingDirection.Left));

            // Add to collection and canvas
            adornerLayers.Add(gameSideElement.Id, layer);
            canvas.Children.Add(layer.Visual);

            // Make sure adorners will be rendered correctly the next time
            layer.Invalidate();
            return layer;
        }

        private void DoAddAdorner(Guid elementId)
        {
            AdornerLayer layer;
            if (!adornerLayers.TryGetValue(elementId, out layer))
            {
                // Doesn't exist yet, add it
                var element = (UIElement)Controller.FindGameSidePart(new AbsoluteId(Controller.Editor.Asset.Id, elementId));
                if (element == null) throw new ArgumentException(@"No game-side part corresponds to the given id", nameof(elementId));
                layer = CreateLayer(element);
            }

            layer.Hide();
        }

        private void DoDisableAdorner(Guid elementId)
        {
            AdornerLayer layer;
            if (!adornerLayers.TryGetValue(elementId, out layer))
                return;

            layer.Hide();
            layer.Disable();
        }

        private void DoEnableAdorner(Guid elementId)
        {
            var layer = GetAdornerLayer(elementId);
            // Make sure adorners will be rendered correctly the next time
            layer.Invalidate();
            layer.Enable();
        }

        private void DoHideAllAdorners()
        {
            adornerLayers.Values.ForEach(l => l.Hide());
            selectedAdorners.Clear();
        }

        private void DoHighlightAdorner(Guid elementId)
        {
            var layer = GetAdornerLayer(elementId);
            if (layer.IsHighlighted)
                return;

            DoUnlitAllAdorners();
            // Make sure adorners will be rendered correctly the next time
            layer.Invalidate();
            // Highlight the adorners corresponding to the given element Id
            layer.Highlight();
        }

        private void DoRemoveAdorner(Guid elementId)
        {
            AdornerLayer layer;
            if (!adornerLayers.TryGetValue(elementId, out layer))
            {
                return;
            }
            adornerLayers.Remove(elementId);

            var canvas = GetAdornerCanvas();
            canvas?.Children.Remove(layer.Visual);
        }

        private void DoSelectElement(Guid elementId)
        {
            var layer = GetAdornerLayer(elementId);
            selectedAdorners.Add(elementId);

            // Make sure adorners will be rendered correctly the next time
            layer.Invalidate();
            layer.Show();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoUnlitAllAdorners()
        {
            // Unlit all adorner layers
            adornerLayers.Values.ForEach(l => l.Unlit());
        }

        private Canvas GetAdornerCanvas()
        {
            return Controller.GetEntityByName(UIEditorController.AdornerEntityName)?.Get<UIComponent>().Page.RootElement as Canvas;
        }

        private AdornerLayer GetAdornerLayer(Guid elementId)
        {
            AdornerLayer layer;
            if (!adornerLayers.TryGetValue(elementId, out layer))
            {
                throw new KeyNotFoundException("The adorner for the given element id is not available.");
            }

            // If the layer doesn't exist add it
            return layer;
        }

        /// <summary>
        /// Update all the adorners of the scene.
        /// </summary>
        private async Task Update()
        {
            Canvas adornerCanvas;
            // initialize
            while (true)
            {
                if (IsDisposed)
                    return;
                await Game.Script.NextFrame();

                adornerCanvas = GetAdornerCanvas();
                if (adornerCanvas == null)
                    continue;

                adornerCanvas.PreviewTouchDown += PreviewTouchDown;
                adornerCanvas.PreviewTouchMove += PreviewTouchMove;
                adornerCanvas.TouchMove += TouchMove;
                adornerCanvas.TouchUp += TouchUp;
                break;
            }

            // add a scene delegate renderer to update all adorners.
            var compositor = Game.SceneSystem.GraphicsCompositor;
            ((EditorTopLevelCompositor)compositor.Game).PostGizmoCompositors.Add(new DelegateSceneRenderer(context =>
            {
                var canvasRenderSize = adornerCanvas.RenderSize;
                foreach (var layer in adornerLayers.Values)
                {
                    if (!layer.IsEnabled || layer.CheckValidity())
                        continue;

                    // Always update adorners (even if there are hidden)
                    layer.Update(ref canvasRenderSize);
                }
            }));
        }
    }
}
