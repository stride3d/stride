// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#region Copyright and license
// Some parts of this file were inspired by OxyPlot (https://github.com/oxyplot/oxyplot)
/*
The MIT license (MTI)
https://opensource.org/licenses/MIT

Copyright (c) 2014 OxyPlot contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.Controls
{
    [TemplatePart(Name = GridPartName, Type = typeof(Grid))]
    public sealed class CanvasView : Control, IDrawingView
    {
        /// <summary>
        /// The name of the part for the <see cref="Canvas"/>.
        /// </summary>
        private const string GridPartName = "PART_Grid";

        /// <summary>
        /// Identifies the <see cref="Model"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(nameof(Model), typeof(IDrawingModel), typeof(CanvasView), new PropertyMetadata(null, OnModelPropertyChanged));

        /// <summary>
        /// The grid.
        /// </summary>
        private Grid grid;
        /// <summary>
        /// The renderer.
        /// </summary>
        private CanvasRenderer renderer;
        /// <summary>
        /// Invalidation flag (0: no update, 1: update visual elements).
        /// </summary>
        private int isDrawingInvalidated;

        static CanvasView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CanvasView), new FrameworkPropertyMetadata(typeof(CanvasView)));
        }

        public CanvasView()
        {
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        public IDrawingModel Model { get { return (IDrawingModel)GetValue(ModelProperty); } set { SetValue(ModelProperty, value); } }

        private static void OnModelPropertyChanged([NotNull] DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var view = (CanvasView)sender;

            var model = e.OldValue as IDrawingModel;
            model?.Detach(view);

            model = e.NewValue as IDrawingModel;
            model?.Attach(view);

            view.InvalidateDrawing();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            grid = GetTemplateChild(GridPartName) as Grid;
            if (grid == null)
                throw new InvalidOperationException($"A part named '{GridPartName}' must be present in the ControlTemplate, and must be of type '{typeof(Grid).FullName}'.");

            var canvas = new Canvas();
            grid.Children.Add(canvas);
            canvas.UpdateLayout();
            renderer = new CanvasRenderer(canvas);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (ActualWidth > 0 && ActualHeight > 0)
            {
                if (Interlocked.CompareExchange(ref this.isDrawingInvalidated, 0, 1) == 1)
                {
                    UpdateVisuals();
                }
            }

            return base.ArrangeOverride(arrangeBounds);
        }

        /// <summary>
        /// Invalidates the canvas (not blocking the UI thread). The <see cref="Model"/> will render it only once, after
        /// all non-idle operations are completed (<see cref="DispatcherPriority.Background"/> priority).
        /// Thus it is safe to call it every time the canvas should be redraw even when other operations are coming.
        /// </summary>
        public void InvalidateDrawing()
        {
            if (IsLoaded)
                DoInvalidateDrawing();
        }

        private void DoInvalidateDrawing()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0)
                return;

            if (renderer == null)
                return;

            if (Interlocked.CompareExchange(ref isDrawingInvalidated, 1, 0) == 0)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    // Updates the model before rendering
                    UpdateModel(true);
                    // Invalidate the arrange state for the element.
                    // After the invalidation, the element will have its layout updated,
                    // which will occur asynchronously unless subsequently forced by UpdateLayout.
                    InvalidateArrange();
                });
            }
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Make sure InvalidateArrange is called when the canvas is invalidated
            Interlocked.Exchange(ref isDrawingInvalidated, 0);
            DoInvalidateDrawing();
        }

        private void OnSizeChanged(object sender, [NotNull] SizeChangedEventArgs e)
        {
            if (e.NewSize.Height > 0 && e.NewSize.Width > 0)
            {
                InvalidateDrawing();
            }
        }

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <param name="updateData"></param>
        private void UpdateModel(bool updateData)
        {
            Model?.Update(updateData);
        }

        private void UpdateVisuals()
        {
            if (renderer == null)
                return;

            // Clear the canvas
            renderer.Clear();
            // Render the model
            Model?.Render(renderer, ActualWidth, ActualHeight);
        }
    }
}
