// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Services;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.UI;
using Stride.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Game
{
    partial class UIEditorGameAdornerService
    {
        // selection state
        private readonly HashSet<Guid> selectedAdorners = new HashSet<Guid>();

        // dragging state
        /// <summary>
        /// Current position of dragging.
        /// </summary>
        private Vector3 currentPosition;
        private IResizingAdorner dragAdorner;
        /// <summary>
        /// Indicates that the <see cref="dragAdorner"/> is currently being dragged.
        /// </summary>
        private bool isDragging;
        /// <summary>
        /// Indicate that dragging state is enabled.
        /// </summary>
        private bool isInProgress;
        private float snapValue;

        // shared
        private Vector2 originScreenPosition;
        private Vector3 originWorldPosition;

        private readonly IReadOnlyCollection<Guid> emptyIds = new Guid[0];

        public IReadOnlyCollection<Guid> GetElementIdsAtPosition(ref Vector3 worldPosition)
        {
            var hitResults = GetAdornerVisualsAtPosition(ref worldPosition);
            if (hitResults == null)
                return emptyIds;

            var elementIds = new List<Guid>();
            foreach (var hit in hitResults.OrderBy(r => r.IntersectionPoint.Z))
            {
                var visual = hit.Element;
                Guid elementId;
                if (visual != null && visual.DependencyProperties.TryGetValue(AssociatedElementIdPropertyKey, out elementId))
                {
                    elementIds.Add(elementId);
                }
            }

            return elementIds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ApplyChanges(Guid elementId, IReadOnlyDictionary<string, object> changes)
        {
            var editor = Controller.Editor;
            editor.Dispatcher.InvokeAsync(() => { editor.UpdateProperties(elementId, changes); });
        }

        #region Event Handlers
        private void PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (!Game.Input.IsMouseButtonDown(MouseButton.Left))
                return;

            // Save the current pointer position
            originWorldPosition = currentPosition = e.WorldPosition;
            originScreenPosition = e.ScreenPosition;

            // No prior selection (selection is done on TouchUp)
            if (selectedAdorners.Count == 0)
                return;

            // Get the resizing adorners that are under the pointer position
            var resizingAdorners = (
                from hit in GetAdornerVisualsAtPosition(ref originWorldPosition)
                // only on currently selected elements (retrieve associated game-side element id from hit visual)
                join s in selectedAdorners on hit.Element.DependencyProperties.Get(AssociatedElementIdPropertyKey) equals s
                // order by closest to screen (z goes negatively)
                orderby hit.IntersectionPoint.Z
                // retrieve resizing adorners (MoveAdorner or SizingAdorner), if any
                let r = GetAssociatedAdorner(hit.Element) as IResizingAdorner
                where r != null
                select r).ToList();

            if (resizingAdorners.Count == 0)
                return;

            if (resizingAdorners.Count == 1 && selectedAdorners.Count == 1)
            {
                // If ALT is pressed, we are starting a drag/drop to the outside of the UI editor
                if (Game.Input.IsKeyDown(Keys.LeftAlt) || Game.Input.IsKeyDown(Keys.RightAlt))
                {
                    e.Handled = true;
                    Controller.DoDragDrop(resizingAdorners[0].GameSideElement.Id);
                    return;
                }
            }

            // start drag
            dragAdorner = resizingAdorners[0]; // hit results have been order by Z already
            isInProgress = true;
        }

        private void PreviewTouchMove(object sender, TouchEventArgs e)
        {
            var worldPosition = e.WorldPosition;
            DoHighlightingAtPosition(ref worldPosition);
        }

        private void TouchMove(object sender, TouchEventArgs e)
        {
            // dragging state
            if (isInProgress)
            {
                if (dragAdorner == null || !Game.Input.IsMouseButtonDown(MouseButton.Left))
                {
                    CancelDrag();
                    return;
                }
                if (!isDragging)
                {
                    // Start dragging only if a minimum distance is reached (on the real screen, not the virtual screen).
                    var delta = (e.ScreenPosition - originScreenPosition)*new Vector2(Game.GraphicsDevice.Presenter.BackBuffer.Width, Game.GraphicsDevice.Presenter.BackBuffer.Height);
                    if (Math.Abs(delta.X) > System.Windows.SystemParameters.MinimumHorizontalDragDistance || Math.Abs(delta.Y) > System.Windows.SystemParameters.MinimumVerticalDragDistance)
                    {
                        isDragging = true;
                        Controller.ChangeCursor(dragAdorner.GetCursor());
                        snapValue = Controller.Editor.SnapValue;
                        // snap the current position, to prevent accumulating some delta when starting dragging
                        if (snapValue > float.Epsilon)
                        {
                            currentPosition.X = (float)Math.Round(currentPosition.X / snapValue) * snapValue;
                            currentPosition.Y = (float)Math.Round(currentPosition.Y / snapValue) * snapValue;
                        }
                    }
                }
                if (isDragging)
                {
                    var position = e.WorldPosition;
                    // snap the world position
                    if (snapValue > float.Epsilon)
                    {
                        position.X = (float)Math.Round(position.X / snapValue) * snapValue;
                        position.Y = (float)Math.Round(position.Y / snapValue) * snapValue;
                    }
                    // calculate delta
                    var delta = position - currentPosition;
                    const float magnetDistance = 10; // FIXME: expose this value in settings
                    if (delta.LengthSquared() > float.Epsilon)
                    {
                        var resolution = Controller.GetEntityByName(UIEditorController.UIEntityName).Get<UIComponent>().Resolution;
                        if (dragAdorner.ResizingDirection == ResizingDirection.Center
                            ? UILayoutHelper.Move(dragAdorner.GameSideElement, ref delta, magnetDistance, ref resolution)
                            : UILayoutHelper.Resize(dragAdorner.GameSideElement, dragAdorner.ResizingDirection, ref delta, magnetDistance, ref resolution))
                        {
                            dragAdorner.OnResizingDelta(delta.X, delta.Y);
                            currentPosition += delta; // apply delta
                        }
                    }
                }
                e.Handled = true;
                return;
            }

            if (!Game.Input.IsMouseButtonDown(MouseButton.Left))
                return;

            // other states
            // TODO: special case when trying to move when there is nothing selected: should select and start moving at the same time
        }

        private void TouchUp(object sender, TouchEventArgs e)
        {
            if (!Game.Input.IsMouseButtonReleased(MouseButton.Left))
                return;

            isInProgress = false;
            Controller.ChangeCursor(null);
            e.Handled = true;

            // dragging state
            if (isDragging)
            {
                isDragging = false;
                if (dragAdorner != null)
                {
                    // Create a collection with all properties that might have changed
                    var changes = new Dictionary<string, object>
                    {
                        { nameof(UIElement.Margin), dragAdorner.GameSideElement.Margin},
                        { nameof(UIElement.Width), dragAdorner.GameSideElement.Width },
                        { nameof(UIElement.HorizontalAlignment), dragAdorner.GameSideElement.HorizontalAlignment },
                        { nameof(UIElement.Height), dragAdorner.GameSideElement.Height },
                        { nameof(UIElement.VerticalAlignment), dragAdorner.GameSideElement.VerticalAlignment },
                        //{ nameof(UIElement.Depth), dragAdorner.GameSideElement.Depth },
                        //{ nameof(UIElement.DepthAlignment), dragAdorner.GameSideElement.DepthAlignment },
                    };
                    // Propagate the changes to the asset
                    ApplyChanges(dragAdorner.GameSideElement.Id, changes);
                    dragAdorner.OnResizingCompleted();
                }
                dragAdorner = null;
                return;
            }

            // other states
            var editor = Controller.Editor;
            var worldPosition = e.WorldPosition;

            // Get the id of the game-side element that is under the pointer position
            Guid elementId;
            if (!TryGetElementIdAtPosition(ref worldPosition, out elementId))
            {
                // Nothing, clear the selection
                editor.Dispatcher.InvokeAsync(() => editor.ClearSelection());
                return;
            }

            // Select the corresponding asset-side UIElement, if pointer did not move between down and up events
            var delta = (e.ScreenPosition - originScreenPosition)*new Vector2(Game.GraphicsDevice.Presenter.BackBuffer.Width, Game.GraphicsDevice.Presenter.BackBuffer.Height);
            if (Math.Abs(delta.X) < System.Windows.SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < System.Windows.SystemParameters.MinimumVerticalDragDistance)
            {
                var additiveSelection = Game.Input.IsKeyDown(Keys.LeftCtrl) || Game.Input.IsKeyDown(Keys.RightCtrl);
                editor.Dispatcher.InvokeAsync(() => editor.Select(elementId, additiveSelection)).Forget();
            }
        }
        #endregion // Event Handlers

        /// <summary>
        /// Cancels the current dragging state.
        /// </summary>
        private void CancelDrag()
        {
            if (!isInProgress)
                return;

            isInProgress = false;
            Controller.ChangeCursor(null);

            if (!isDragging)
                return;
            
            dragAdorner?.OnResizingCompleted();
        }

        /// <summary>
        /// Highlights the element at the given <see cref="worldPosition"/>.
        /// </summary>
        /// <param name="worldPosition"></param>
        private void DoHighlightingAtPosition(ref Vector3 worldPosition)
        {
            Guid elementId;
            if (!TryGetElementIdAtPosition(ref worldPosition, out elementId) || selectedAdorners.Contains(elementId))
            {
                DoUnlitAllAdorners();
                return;
            }

            DoHighlightAdorner(elementId);
        }

        private ICollection<UIRenderFeature.HitTestResult> GetAdornerVisualsAtPosition(ref Vector3 worldPosition)
        {
            var uiComponent = Controller.GetEntityByName(UIEditorController.AdornerEntityName).Get<UIComponent>();
            if (Math.Abs(worldPosition.X) > uiComponent.Resolution.X * 0.5f ||
                Math.Abs(worldPosition.Y) > uiComponent.Resolution.Y * 0.5f)
                return null;

            var rootElement = uiComponent.Page?.RootElement;
            if (rootElement == null)
                return null;

            var ray = new Ray(new Vector3(worldPosition.X, worldPosition.Y, uiComponent.Resolution.Z + 1), -Vector3.UnitZ);
            var worldViewProj = Matrix.Identity; // All the calculation is done in UI space
            return UIRenderFeature.GetElementsAtPosition(rootElement, ref ray, ref worldViewProj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IAdornerBase<UIElement> GetAssociatedAdorner(UIElement visual)
        {
            IAdornerBase<UIElement> adorner;
            visual.DependencyProperties.TryGetValue(AssociatedAdornerPropertyKey, out adorner);
            return adorner;
        }

        private bool TryGetElementIdAtPosition(ref Vector3 worldPosition, out Guid elementId)
        {
            var hitResults = GetAdornerVisualsAtPosition(ref worldPosition);
            var visual = hitResults?.OrderBy(r => -r.IntersectionPoint.Z).FirstOrDefault()?.Element;
            if (visual == null || !visual.DependencyProperties.TryGetValue(AssociatedElementIdPropertyKey, out elementId))
            {
                elementId = Guid.Empty;
                return false;
            }
            return true;
        }
    }
}
