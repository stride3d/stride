// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Stride.Core.Presentation.Behaviors
{
    public sealed class SelectionRectangleBehavior : MouseMoveCaptureBehaviorBase<UIElement>
    {
        public static readonly DependencyProperty CanvasProperty =
            DependencyProperty.Register(nameof(Canvas), typeof(Canvas), typeof(SelectionRectangleBehavior), new PropertyMetadata(OnCanvasChanged));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SelectionRectangleBehavior));

        public static readonly DependencyProperty RectangleStyleProperty =
            DependencyProperty.Register(nameof(RectangleStyle), typeof(Style), typeof(SelectionRectangleBehavior));

        private Point originPoint;
        private Rectangle selectionRectangle;
        
        /// <summary>
        /// Resource Key for the default SelectionRectangleStyle.
        /// </summary>
        public static ResourceKey DefaultRectangleStyleKey { get; } = new ComponentResourceKey(typeof(SelectionRectangleBehavior), nameof(DefaultRectangleStyleKey));

        public Canvas Canvas { get { return (Canvas)GetValue(CanvasProperty); } set { SetValue(CanvasProperty, value); } }

        public ICommand Command { get { return (ICommand)GetValue(CommandProperty); } set { SetValue(CommandProperty, value); } }

        public Style RectangleStyle { get { return (Style)GetValue(RectangleStyleProperty); } set { SetValue(RectangleStyleProperty, value); } }
        
        public bool IsDragging { get; private set; }

        private static void OnCanvasChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (SelectionRectangleBehavior)obj;
            behavior.OnCanvasChanged(e);
        }

        ///  <inheritdoc/>
        protected override void CancelOverride()
        {
            IsDragging = false;
            Canvas.Visibility = Visibility.Collapsed;
        }

        ///  <inheritdoc/>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;
            CaptureMouse();
            
            originPoint = e.GetPosition(AssociatedObject);
        }

        ///  <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.MouseDevice.LeftButton != MouseButtonState.Pressed)
            {
                Cancel();
                return;
            }

            var point = e.GetPosition(AssociatedObject);
            if (IsDragging)
            {
                UpdateDragSelectionRect(originPoint, point);
                e.Handled = true;
            }
            else
            {
                var curMouseDownPoint = e.GetPosition(AssociatedObject);
                var dragDelta = curMouseDownPoint - originPoint;
                if (Math.Abs(dragDelta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(dragDelta.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    IsDragging = true;
                    InitDragSelectionRect(originPoint, curMouseDownPoint);
                }
                e.Handled = true;
            }
        }

        ///  <inheritdoc/>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;
            ReleaseMouseCapture();

            if (!IsDragging)
                return;

            IsDragging = false;
            ApplyDragSelectionRect();
        }

        private void CreateSelectionRectangle()
        {
            selectionRectangle = new Rectangle();
            if (RectangleStyle != null)
            {
                var binding = new Binding
                {
                    Path = new PropertyPath(nameof(RectangleStyle)),
                    Source = this,
                };
                selectionRectangle.SetBinding(FrameworkElement.StyleProperty, binding);
            }
            else
            {
                selectionRectangle.Style = selectionRectangle?.TryFindResource(DefaultRectangleStyleKey) as Style;
            }
            selectionRectangle.IsHitTestVisible = false;
        }

        private void OnCanvasChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldCanvas = e.OldValue as Canvas;
            if (oldCanvas != null && selectionRectangle != null)
            {
                oldCanvas.Children.Remove(selectionRectangle);
            }

            var newCanvas = e.NewValue as Canvas;
            if (newCanvas == null)
                return;
            newCanvas.Visibility = Visibility.Collapsed;

            if (selectionRectangle == null)
                CreateSelectionRectangle();
            if (selectionRectangle != null)
                newCanvas.Children.Add(selectionRectangle);
        }

        /// <summary>
        /// Initialize the rectangle used for drag selection.
        /// </summary>
        private void InitDragSelectionRect(Point pt1, Point pt2)
        {
            UpdateDragSelectionRect(pt1, pt2);
            Canvas.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Update the position and size of the rectangle used for drag selection.
        /// </summary>
        private void UpdateDragSelectionRect(Point pt1, Point pt2)
        {
            double x, y, width, height;

            //
            // Determine x,y,width and height of the rect inverting the points if necessary.
            // 

            if (pt2.X < pt1.X)
            {
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            //
            // Update the coordinates of the rectangle used for drag selection.
            //
            Canvas.SetLeft(selectionRectangle, x);
            Canvas.SetTop(selectionRectangle, y);
            selectionRectangle.Width = width;
            selectionRectangle.Height = height;
        }

        /// <summary>
        /// Select all nodes that are in the drag selection rectangle.
        /// </summary>
        private void ApplyDragSelectionRect()
        {
            Canvas.Visibility = Visibility.Collapsed;

            if (Command == null)
                return;

            var x = Canvas.GetLeft(selectionRectangle);
            var y = Canvas.GetTop(selectionRectangle);
            var width = selectionRectangle.Width;
            var height = selectionRectangle.Height;
            var dragRect = new Rect(x, y, width, height);
            
            if (Command.CanExecute(dragRect))
            {
                Command.Execute(dragRect);
            }
        }
    }
}
