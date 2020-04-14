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
    /// <summary>
    /// Curve backed by a <see cref="Color4"/>.
    /// Displays each component of the color in a separate curve.
    /// </summary>
    public sealed class Color4CurveViewModel : CurveViewModelBase<Color4>
    {
        private struct ColorData
        {
            private readonly float t;
            private readonly float r;
            private readonly float g;
            private readonly float b;
            private readonly float a;

            public ColorData(float t, float r, float g, float b, float a)
            {
                this.t = t;
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }

            public float T => t;

            public float this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return r;
                        case 1:
                            return g;
                        case 2:
                            return b;
                        case 3:
                            return a;
                        default:
                            return T;
                    }
                }
            }
        }
        
        private readonly List<ColorData> sampleData = new List<ColorData>();

        public Color4CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Color4> computeCurve, CurveViewModelBase parent = null, string name = null)
            : base(editor, parent, computeCurve, name)
        {
        }

        /// <inheritdoc/>
        protected override void RenderPoints(IDrawingContext drawingContext, ref Rect clippingRect, bool isCurrentCurve)
        {
            for (var i = 0; i < 4; ++i)
            {
                var drawPoints = sampleData.Select(d => TransformPoint(new Vector2(d.T, d[i]))).ToList();
                // Render the curve
                RenderLine(drawingContext, ref clippingRect, drawPoints, Editor.GetColor((VectorComponent)i));
            }
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
            sampleData.Clear();
            // Calculate the new samples
            for (var t = start; t < end; t += increment)
            {
                var ft = (float)t;
                var sample = curve.Evaluate(ft);
                sampleData.Add(new ColorData(ft, sample.R, sample.G, sample.B, sample.A));
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

            foreach (var data in sampleData)
            {
                var x = data.T;
                if (!XAxis?.IsValidValue(x) ?? true)
                    continue;

                if (x < minx)
                    minx = x;

                if (x > maxx)
                    maxx = x;

                for (var i = 0; i < 4; ++i)
                {
                    var y = data[i];
                    if (!YAxis?.IsValidValue(y) ?? true)
                        continue;

                    if (y < miny)
                        miny = y;

                    if (y > maxy)
                        maxy = y;
                }
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
