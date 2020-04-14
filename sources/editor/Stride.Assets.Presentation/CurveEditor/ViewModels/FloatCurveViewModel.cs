// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Drawing;
using Stride.Animations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    public sealed class FloatCurveViewModel : CurveViewModelBase<float>
    {
        private readonly List<Vector2> samplePoints = new List<Vector2>();

        public FloatCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<float> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        /// <inheritdoc/>
        protected override void RenderPoints([NotNull] IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            var drawPoints = samplePoints.Select(TransformPoint).ToList();
            // Render the curve
            RenderLine(drawingContext, ref clippingRect, drawPoints, Color);
        }

        /// <inheritdoc/>
        protected internal override void UpdateData()
        {
            var curve = UnderlyingCurve;
            if (curve == null)
                return;

            double start, end, increment;
            if (!GetSamplingParameters(XAxis, out start, out end, out increment))
                return;

            // Limit to float precision
            //if (increment < 1E-6f)
            //    increment = 1E-6f;

            // Make sure the curve is up-to-date
            curve.UpdateChanges();
            // Clear the previous samples
            samplePoints.Clear();
            // Calculate the new samples
            for (var t = start; t < end; t += increment)
            {
                var ft = (float)t;
                samplePoints.Add(new Vector2(ft, curve.Evaluate(ft)));
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            var minx = MinX;
            var miny = MinY;
            var maxx = MaxX;
            var maxy = MaxY;

            if (double.IsNaN(minx))
                minx = double.MaxValue;

            if (double.IsNaN(miny))
                miny = double.MaxValue;

            if (double.IsNaN(maxx))
                maxx = double.MinValue;

            if (double.IsNaN(maxy))
                maxy = double.MinValue;

            foreach (var point in samplePoints)
            {
                var x = point.X;
                var y = point.Y;

                if (!IsValidPoint(x, y))
                    continue;

                if (x < minx)
                    minx = x;

                if (x > maxx)
                    maxx = x;

                if (y < miny)
                    miny = y;

                if (y > maxy)
                    maxy = y;
            }

            if (minx < double.MaxValue)
                MinX = minx;

            if (miny < double.MaxValue)
                MinY = miny;

            if (maxx > double.MinValue)
                MaxX = maxx;

            if (maxy > double.MinValue)
                MaxY = maxy;
        }
    }
}
