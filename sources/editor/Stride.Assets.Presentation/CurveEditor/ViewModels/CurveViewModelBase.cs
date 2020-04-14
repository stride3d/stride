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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;
    using WindowsVector = System.Windows.Vector;
    using WindowsRect = System.Windows.Rect;

    /// <summary>
    /// Base class for all curves inside a curve editor.
    /// </summary>
    public abstract class CurveViewModelBase : DispatcherViewModel
    {
        private readonly ObservableList<CurveViewModelBase> children = new ObservableList<CurveViewModelBase>();
        private AxisBase xAxis;
        private AxisBase yAxis;
        private Color color = Color.HotPink;
        private bool isSelected;

        protected CurveViewModelBase([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent)
           : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            Editor = editor;
            Parent = parent;
            children.CollectionChanged += ChildrenCollectionChanged;
        }

        public virtual IObservableList<CurveViewModelBase> Children => children;

        /// <summary>
        /// Gets or sets the color of the curve line.
        /// </summary>
        /// <remarks>
        /// Changing the color will not trigger a refresh of the curve.
        /// </remarks>
        /// <value>The color of the curve line.</value>
        public Color Color { get { return color; } protected internal set { SetValue(ref color, value); } }

        public abstract string DisplayName { get; }

        public bool HasChildren => Children?.Count > 0;

        /// <summary>
        /// Gets or sets whether this curve is currently part of a selection.
        /// </summary>
        /// <value><c>true</c> if this curve is currently part of a selection; otherwise, <c>false</c>.</value>
        public bool IsSelected { get { return isSelected; } set { SetValue(ref isSelected, value); } }

        public CurveViewModelBase Parent { get; }

        /// <summary>
        /// Gets or sets the maximum x-coordinate of the dataset.
        /// </summary>
        /// <value>The maximum x-coordinate.</value>
        public double MaxX { get; protected set; }

        /// <summary>
        /// Gets or sets the maximum y-coordinate of the dataset.
        /// </summary>
        /// <value>The maximum y-coordinate.</value>
        public double MaxY { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum x-coordinate of the dataset.
        /// </summary>
        /// <value>The minimum x-coordinate.</value>
        public double MinX { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum y-coordinate of the dataset.
        /// </summary>
        /// <value>The minimum y-coordinate.</value>
        public double MinY { get; protected set; }

        /// <summary>
        /// Gets the x-axis.
        /// </summary>
        /// <value>The x-axis.</value>
        public AxisBase XAxis
        {
            get { return xAxis; }
            internal set
            {
                if (SetValue(ref xAxis, value))
                {
                    foreach (var child in Children)
                    {
                        child.XAxis = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the y-axis.
        /// </summary>
        /// <value>The y-axis.</value>
        public AxisBase YAxis
        {
            get { return yAxis; }
            internal set
            {
                if (SetValue(ref yAxis, value))
                {
                    foreach (var child in Children)
                    {
                        child.YAxis = value;
                    }
                }
            }
        }

        protected CurveEditorViewModel Editor { get; }

        internal event EventHandler AxisTransformChanged;
        
        protected static void RenderLine([NotNull] IDrawingContext drawingContext, ref WindowsRect clippingRect, IList<WindowsPoint> points, Color color)
        {
            // FIXME: calculate (optimistic) clipped line beforehand so that points that are eventually outside of the clipped area are not given to the rendering context.
            // FIXME: note that in the end the curve is still clipped by the canvas Clip property.
            drawingContext.DrawPolyline(points, color);
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            // Clear children first
            foreach (var child in children)
            {
                child.Destroy();
            }
            children.Clear();

            if (xAxis != null)
                xAxis.TransformChanged -= AxisTransformChanged;
            xAxis = null;
            if (yAxis != null)
                yAxis.TransformChanged -= AxisTransformChanged;
            yAxis = null;

            base.Destroy();
        }

        public void EnsureAxes()
        {
            if (XAxis != null && YAxis != null)
                return;

            // x-axis
            if (XAxis == null)
            {
                XAxis = new LinearAxis { Position = AxisPosition.Bottom, Layer = AxisLayer.BelowCurves };
            }
            // y-axis
            if (YAxis == null)
            {
                YAxis = new LinearAxis { Position = AxisPosition.Left, Layer = AxisLayer.BelowCurves };
            }
        }

        /// <summary>
        /// Renders the curve.
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="isCurrentCurve"></param>
        // FIXME: refactor isCurrentCurve and use the IsSelected property
        public virtual void Render(IDrawingContext drawingContext, bool isCurrentCurve)
        {
            if (!CheckAxes())
                return;

            var clippingRect = GetClippingRect();
            drawingContext.SetClip(clippingRect);

            RenderPoints(drawingContext, ref clippingRect, isCurrentCurve);

            drawingContext.ResetClip();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector2 InverseTransformPoint(WindowsPoint point)
        {
            return (CheckAxes() ? XAxis.InverseTransform(point.X, point.Y, YAxis) : point).ToVector2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector2 InverseTransformVector(WindowsVector vector)
        {
            return (CheckAxes() ? (WindowsVector)XAxis.InverseTransform(vector.X, vector.Y, YAxis, isVector: true) : vector).ToVector2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WindowsPoint TransformPoint(Vector2 point)
        {
            return CheckAxes() ? XAxis.Transform(point.X, point.Y, YAxis) : point.ToWindowsPoint();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal WindowsVector TransformVector(Vector2 point)
        {
            return CheckAxes() ? (WindowsVector)XAxis.Transform(point.X, point.Y, YAxis, isVector: true) : point.ToWindowsVector();
        }

        /// <summary>
        /// Updates the axes to include the max and min of this series.
        /// </summary>
        protected internal void UpdateAxisMaxMin()
        {
            XAxis.Include(MinX);
            XAxis.Include(MaxX);
            YAxis.Include(MinY);
            YAxis.Include(MaxY);
            foreach (var child in Children)
            {
                child.UpdateAxisMaxMin();
            }
        }
        
        /// <summary>
        /// Updates the data of the curve.
        /// </summary>
        protected internal virtual void UpdateData()
        {
            
        }

        /// <summary>
        /// Updates the maximum and minimum values of the series.
        /// </summary>
        protected internal virtual void UpdateMaxMin()
        {
            MinX = MinY = MaxX = MaxY = double.NaN;
            foreach (var child in Children)
            {
                child.UpdateMaxMin();
            }
        }

        /// <summary>
        /// Checks wether both axes are defined.
        /// </summary>
        protected bool CheckAxes()
        {
            return xAxis != null && yAxis != null;
        }

        /// <summary>
        /// Gets the clipping rectangle.
        /// </summary>
        /// <returns>The clipping rectangle.</returns>
        protected WindowsRect GetClippingRect()
        {
            var minX = Math.Min(XAxis.ScreenMin.X, XAxis.ScreenMax.X);
            var minY = Math.Min(YAxis.ScreenMin.Y, YAxis.ScreenMax.Y);
            var maxX = Math.Max(XAxis.ScreenMin.X, XAxis.ScreenMax.X);
            var maxY = Math.Max(YAxis.ScreenMin.Y, YAxis.ScreenMax.Y);

            return new WindowsRect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Determines whether the specified point is valid.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns><c>true</c> if the point is valid; otherwise, <c>false</c> . </returns>
        protected bool IsValidPoint(double x, double y)
        {
            return (XAxis?.IsValidValue(x) ?? false) &&
                   (YAxis?.IsValidValue(y) ?? false);
        }
        
        protected virtual void Refresh()
        {
            Editor.InvalidateView();
        }

        /// <summary>
        /// Actually renders the points of the curve.
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="clippingRect"></param>
        /// <param name="isCurrentCurve"></param>
        protected virtual void RenderPoints(IDrawingContext drawingContext, ref WindowsRect clippingRect, bool isCurrentCurve)
        {
            // Default implementation does nothing
        }

        private void ChildrenCollectionChanged(object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
                return;

            if (e.OldItems != null)
            {
                foreach (CurveViewModelBase child in e.OldItems)
                {
                    child.XAxis = null;
                    child.YAxis = null;
                }
            }

            if (e.NewItems != null)
            {
                foreach (CurveViewModelBase child in e.NewItems)
                {
                    child.XAxis = XAxis;
                    child.YAxis = YAxis;
                }
            }
        }
    }

    /// <summary>
    /// Base class for all computed curves inside a curve editor.
    /// </summary>
    public abstract class CurveViewModelBase<TValue> : CurveViewModelBase
        where TValue : struct
    {
        private readonly string name;

        protected CurveViewModelBase([NotNull] CurveEditorViewModel editor, CurveViewModelBase parent, [NotNull] IComputeCurve<TValue> computeCurve, string name = null)
            : base(editor, parent)
        {
            if (computeCurve == null) throw new ArgumentNullException(nameof(computeCurve));

            CurveNode = editor.Session.AssetNodeContainer.GetOrCreateNode(computeCurve);
            this.name = name;
        }

        public override string DisplayName => !string.IsNullOrEmpty(name) ? name : CurveNode.Type.Name;

        public Guid CurveId => CurveNode.Guid;

        protected IObjectNode CurveNode { get; }

        protected IComputeCurve<TValue> UnderlyingCurve => CurveNode.Retrieve() as IComputeCurve<TValue>;

        /// <summary>
        /// Retrieves the parameters to use for sampling.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="start">The starting value.</param>
        /// <param name="end">The ending value.</param>
        /// <param name="increment">The increment between values.</param>
        /// <returns></returns>
        protected static bool GetSamplingParameters([NotNull] AxisBase axis, out double start, out double end, out double increment)
        {
            // Typical range is [0,1]
            start = double.IsNaN(axis.ViewMinimum) ? 0 : (float)Math.Max(0, axis.ViewMinimum);
            end = double.IsNaN(axis.ViewMaximum) ? 1 : (float)Math.Min(1, axis.ViewMaximum);

            // Get x-axis increment
            var screenPixels = (axis.ScreenMax - axis.ScreenMin).X;
            if (!double.IsNaN(screenPixels) && screenPixels > 1)
                increment = (end - start)/screenPixels; // i.e. exactly one point per pixels
            else
                increment = (axis.ActualMajorStep/ axis.IntervalLength); // i.e. about one point per pixels
            return !double.IsNaN(increment) && !(increment < float.Epsilon);
        }

        public virtual void Initialize()
        {
            // default implementation does nothing
        }
    }
}
