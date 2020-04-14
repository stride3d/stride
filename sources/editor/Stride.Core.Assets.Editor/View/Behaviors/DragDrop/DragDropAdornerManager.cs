// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Adorners;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// This class manages activation and deactivation of the drop adorners of elements that
    /// supports drop while dragging over the windows of the application.
    /// </summary>
    internal static class DragDropAdornerManager
    {
        private static readonly Dictionary<Visual, Tuple<AdornerLayer, HighlightBorderAdorner>> DropAdorners = new Dictionary<Visual, Tuple<AdornerLayer, HighlightBorderAdorner>>();
        private static readonly TimeoutDispatcherTimer DragLeaveTimer = new TimeoutDispatcherTimer(100);
        private static readonly DependencyProperty BehaviorsProperty;
        private static bool dropAdornersCreated;
        private static InsertAdorner insertAdorner;
        private static AdornerLayer currentLayer;
        private static DisplayDropAdorner dataType;

        private static readonly Dictionary<FrameworkElement, Window> ElementToWindowLookup = new Dictionary<FrameworkElement, Window>();
        private static readonly Dictionary<Window, HashSet<FrameworkElement>> WindowToElementsLookup = new Dictionary<Window, HashSet<FrameworkElement>>();

        static DragDropAdornerManager()
        {
            // We use a timer to deactivate the adorners, because the DragLeave event is broken and unusable.
            // OnDragOver is called constantly (seems to be once per frame) so it is more or less safe to work with timers.
            DragLeaveTimer.Timeout += (s, e) => Deactivate();
            // Interaction.cs via DotPeek: "This property is not exposed publicly. This forces clients to use the GetBehaviors [...] ensuring the collection exists"
            // There is no public way to get the BehaviorCollection only if it exists, without creating it if it does not.
            var behaviorsPropertyFieldInfo = typeof(Interaction).GetField("BehaviorsProperty", BindingFlags.NonPublic | BindingFlags.Static);
            if (behaviorsPropertyFieldInfo == null)
                throw new MissingFieldException("The BehaviorsProperty static field is missing on the class Microsoft.Xaml.Behaviors. This version of the assembly is not supported.");

            BehaviorsProperty = (DependencyProperty)behaviorsPropertyFieldInfo.GetValue(null);
        }

        /// <summary>
        /// Updates the state of the adorner associated to the given <see cref="FrameworkElement"/>.
        /// </summary>
        /// <param name="element">The element that is associated to the adorner to update.</param>
        /// <param name="adornerState">The new state to set.</param>
        internal static void SetAdornerState([NotNull] FrameworkElement element, HighlightAdornerState adornerState)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(element);
            if (adornerLayer != null)
            {
                Tuple<AdornerLayer, HighlightBorderAdorner> adorner;
                if (DropAdorners.TryGetValue(element, out adorner))
                    adorner.Item2.State = adornerState;
            }
        }

        /// <summary>
        /// Registers the window hosting the given <see cref="FrameworkElement"/>.
        /// </summary>
        /// <param name="element">The element for which to register the hosting window.</param>
        internal static void RegisterElement([NotNull] FrameworkElement element)
        {
            if (element.IsLoaded)
                RegisterElementInWindow(element);
            element.Loaded += OnElementLoaded;
            element.Unloaded += OnElementUnloaded;
        }

        internal static void UnregisterElement([NotNull] FrameworkElement element)
        {
            element.Unloaded -= OnElementUnloaded;
            element.Loaded -= OnElementLoaded;
            UnregisterElementFromWindow(element);
        }

        internal static void ClearInsertAdorner()
        {
            if (currentLayer != null && insertAdorner != null)
                currentLayer.Remove(insertAdorner);
        }

        internal static void UpdateInsertAdorner([NotNull] UIElement container, InsertPosition position)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(container);
            if (adornerLayer != null)
            {
                adornerLayer.IsHitTestVisible = false;
                if (insertAdorner == null || !Equals(insertAdorner.AdornedElement, container))
                    insertAdorner = new InsertAdorner(container);
                insertAdorner.Position = position;
                adornerLayer.Add(insertAdorner);
                currentLayer = adornerLayer;
            }
        }

        private static void OnDragOver(object sender, [NotNull] DragEventArgs e)
        {
            // Unless we're over a control that accepts drop, we're going to reject the drop.
            e.Effects = DragDropEffects.None;
            dataType = DisplayDropAdorner.Never;

            var items = DragDropHelper.GetItemsToDrop(DragDropHelper.GetDragContainer(e.Data), e.Data as DataObject);
            if (items != null)
            {
                var isInternal = DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.InternalOnly, items);
                var isExternal = DragDropHelper.ShouldDisplayDropAdorner(DisplayDropAdorner.ExternalOnly, items);
                dataType = (isInternal ? DisplayDropAdorner.InternalOnly : DisplayDropAdorner.Never)
                         | (isExternal ? DisplayDropAdorner.ExternalOnly : DisplayDropAdorner.Never);
            }

            Activate();
            DragLeaveTimer.Reset();
        }

        private static void OnElementLoaded([NotNull] object sender, RoutedEventArgs e)
        {
            RegisterElementInWindow((FrameworkElement)sender);
        }

        private static void OnElementUnloaded([NotNull] object sender, RoutedEventArgs e)
        {
            UnregisterElementFromWindow((FrameworkElement)sender);
        }

        private static void OnGiveFeedback([NotNull] object sender, [NotNull] GiveFeedbackEventArgs e)
        {
            var window = (Window)sender;
            var localPos = window.GetCursorRelativePosition();
            if (!(localPos.X < 0) && !(localPos.Y < 0) && !(localPos.X > window.ActualWidth) && !(localPos.Y > window.ActualHeight))
            {
                Activate();
                DragLeaveTimer.Reset();
            }
            // This event must be handled to update the mouse cursor to something else that the forbidden sign
            e.Handled = true;
        }

        private static void OnWindowClosed([NotNull] object sender, EventArgs e)
        {
            var window = (Window)sender;
            HashSet<FrameworkElement> elements;
            if (WindowToElementsLookup.TryGetValue(window, out elements))
            {
                elements.ForEach(x => ElementToWindowLookup.Remove(x));
            }
            UnregisterWindow(window);
        }

        private static void RegisterElementInWindow([NotNull] FrameworkElement element)
        {
            var window = Window.GetWindow(element);
            if (window == null)
                return;

            // First unregister element from previous window, if any
            UnregisterElementFromWindow(element);

            ElementToWindowLookup.Add(element, window);
            var elements = WindowToElementsLookup.GetOrCreateValue(window, _ =>
            {
                // DragEnter is needed to ensure the data type is reset immediately when a drag operation starts, otherwise it would reuse the previous one for a while.
                window.GiveFeedback += OnGiveFeedback;
                window.PreviewDragEnter += OnDragOver;
                window.PreviewDragOver += OnDragOver;
                window.Closed += OnWindowClosed;
                return new HashSet<FrameworkElement>();
            });
            elements.Add(element);
        }

        private static void UnregisterElementFromWindow([NotNull] FrameworkElement element)
        {
            Window window;
            if (!ElementToWindowLookup.TryGetValue(element, out window))
                return;

            ElementToWindowLookup.Remove(element);
            HashSet<FrameworkElement> elements;
            if (WindowToElementsLookup.TryGetValue(window, out elements))
            {
                elements.Remove(element);
                if (elements.Count == 0)
                {
                    UnregisterWindow(window);
                }
            }
        }

        private static void UnregisterWindow([NotNull] Window window)
        {
            window.GiveFeedback -= OnGiveFeedback;
            window.PreviewDragOver -= OnDragOver;
            window.PreviewDragEnter -= OnDragOver;
            window.Closed -= OnWindowClosed;
            WindowToElementsLookup.Remove(window);
        }

        private static void Activate()
        {
            var hasModal = HasModalWindow();

            if (!dropAdornersCreated)
            {
                foreach (var window in Application.Current.Windows.Cast<Window>().Where(x => WindowToElementsLookup.ContainsKey(x)))
                {
                    if (!hasModal || IsModal(window))
                    {
                        ActivateWindowAdorners(window);
                    }
                    else
                    {
                        // If the application has a modal window currently open, we do not want to display the adorner (cause dropping will be blocked)
                        // Instead, we want to ensure that they are disabled.
                        var localWindow = window;
                        foreach (var dropAdorner in DropAdorners.Where(x => Equals(Window.GetWindow(x.Key), localWindow)))
                        {
                            dropAdorner.Value.Item1.Remove(dropAdorner.Value.Item2);
                        }
                        DropAdorners.Clear();
                    }
                }
                dropAdornersCreated = true;
            }
        }

        private static void Deactivate()
        {
            if (dropAdornersCreated)
            {
                foreach (var dropAdorner in DropAdorners)
                {
                    dropAdorner.Value.Item1.Remove(dropAdorner.Value.Item2);
                }
                DropAdorners.Clear();
                dropAdornersCreated = false;
            }
        }

        private static bool HasModalWindow()
        {
            return Application.Current.Windows.Cast<Window>().Any(IsModal);
        }

        private static bool IsModal(Window window)
        {
            var fieldInfo = typeof(Window).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic);
            return fieldInfo != null && (bool)fieldInfo.GetValue(window);
        }

        private static void ActivateWindowAdorners([NotNull] Window window)
        {
            var elements = WindowToElementsLookup[window];
            foreach (var element in elements.Where(e => e.IsVisible))
            {
                var behaviors = (BehaviorCollection)element.GetValue(BehaviorsProperty);
                if (behaviors != null)
                {
                    foreach (var behavior in behaviors.OfType<IDragDropBehavior>())
                    {
                        if (!behavior.CanDrop || (behavior.DisplayDropAdorner & dataType) == 0)
                            continue;

                        var adornerLayer = AdornerLayer.GetAdornerLayer(element);
                        if (adornerLayer == null)
                            continue;

                        adornerLayer.IsHitTestVisible = false;
                        var adorner = new HighlightBorderAdorner(element) { State = HighlightAdornerState.Visible };
                        adornerLayer.Add(adorner);
                        DropAdorners[element] = Tuple.Create(adornerLayer, adorner);
                    }
                }
            }
        }
    }
}
